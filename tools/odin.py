"""Tree model for Odin `SerializationNodes` in Endless Legend 2 JSON assets.

The game stores polymorphic fields as a flat node stream (Entry 7 start / 8 end / 12 array start /
13 array end / 1,3,4,5,6 scalars). `parse` turns the stream into a tree, `serialize` flattens it back
and re-numbers the `N|Type` ids in document order for reference types only — struct types (Guid,
FixedPoint, DatatableElementReference, ...) never carry an id in base data, and `has_id` is copied
from the exemplar subtree so no struct table is needed.
"""
from __future__ import annotations

import copy
import json
import sys
from dataclasses import dataclass, field
from pathlib import Path
from typing import Iterator

ENTRY_STRING, ENTRY_INT, ENTRY_FLOAT, ENTRY_BOOL, ENTRY_NULL = 1, 3, 4, 5, 6
ENTRY_NODE, ENTRY_END, ENTRY_ARRAY, ENTRY_ARRAY_END = 7, 8, 12, 13


@dataclass
class Value:
    name: str
    entry: int
    data: str


@dataclass
class Node:
    name: str
    type: str
    has_id: bool
    children: list = field(default_factory=list)

    def child(self, name: str):
        for item in self.children:
            if getattr(item, "name", None) == name:
                return item
        raise KeyError(f"{self.type.split(',')[0].rsplit('.', 1)[-1]} has no child {name!r}")

    @property
    def array(self) -> "Array":
        """Collection nodes (Choices, Prerequisites, Steps, ...) wrap exactly one Array child."""
        if len(self.children) != 1 or not isinstance(self.children[0], Array):
            raise TypeError(f"{self.name!r} is not a collection node")
        return self.children[0]

    def set(self, name: str, data) -> None:
        item = self.child(name)
        item.data = _to_data(data)
        if item.entry == ENTRY_NULL and data is not None:
            item.entry = _entry_for(data)
        if data is None:
            item.entry, item.data = ENTRY_NULL, ""


@dataclass
class Array:
    name: str
    children: list = field(default_factory=list)


Item = Value | Node | Array


def _to_data(data) -> str:
    if data is None:
        return ""
    if isinstance(data, bool):
        return "true" if data else "false"
    return str(data)


def _entry_for(data) -> int:
    if isinstance(data, bool):
        return ENTRY_BOOL
    if isinstance(data, int):
        return ENTRY_INT
    if isinstance(data, float):
        return ENTRY_FLOAT
    return ENTRY_STRING


def parse(nodes: list[dict]) -> list[Item]:
    """Flat stream -> list of top-level items."""
    stack: list[list] = [[]]
    for raw in nodes:
        entry = raw["Entry"]
        if entry == ENTRY_NODE:
            type_id, _, type_name = raw["Data"].partition("|")
            has_id = bool(type_name)
            node = Node(raw["Name"], type_name if has_id else raw["Data"], has_id)
            stack[-1].append(node)
            stack.append(node.children)
        elif entry == ENTRY_ARRAY:
            array = Array(raw["Name"])
            stack[-1].append(array)
            stack.append(array.children)
        elif entry in (ENTRY_END, ENTRY_ARRAY_END):
            if len(stack) == 1:
                raise ValueError("unbalanced node stream: end without start")
            stack.pop()
        else:
            stack[-1].append(Value(raw["Name"], entry, raw["Data"]))
    if len(stack) != 1:
        raise ValueError("unbalanced node stream")
    return stack[0]


def serialize(items: list[Item]) -> list[dict]:
    """Tree -> flat stream with ids renumbered 0..k-1 in document order."""
    out: list[dict] = []
    counter = iter(range(1_000_000))

    def emit(item: Item) -> None:
        if isinstance(item, Value):
            out.append({"Name": item.name, "Entry": item.entry, "Data": item.data})
        elif isinstance(item, Array):
            out.append({"Name": item.name, "Entry": ENTRY_ARRAY, "Data": str(len(item.children))})
            for child in item.children:
                emit(child)
            out.append({"Name": "", "Entry": ENTRY_ARRAY_END, "Data": ""})
        else:
            data = f"{next(counter)}|{item.type}" if item.has_id else item.type
            out.append({"Name": item.name, "Entry": ENTRY_NODE, "Data": data})
            for child in item.children:
                emit(child)
            out.append({"Name": "", "Entry": ENTRY_END, "Data": ""})

    for item in items:
        emit(item)
    return out


def walk(items: list[Item]) -> Iterator[Item]:
    for item in items:
        yield item
        if not isinstance(item, Value):
            yield from walk(item.children)


def find_node(items: list[Item], type_suffix: str) -> Node:
    """First LIVE node whose type name ends with `type_suffix`; deepcopy it yourself when grafting elsewhere."""
    for item in walk(items):
        if isinstance(item, Node) and item.type.split(",")[0].endswith(type_suffix):
            return item
    raise KeyError(f"no node of type *{type_suffix}")


def top(items: list[Item], name: str):
    for item in items:
        if getattr(item, "name", None) == name:
            return item
    raise KeyError(f"no top-level item {name!r}")


class Asset:
    """One exported JSON asset: header + plain fields + node tree."""

    def __init__(self, raw: dict):
        self.raw = raw
        self.nodes: list[Item] = parse(raw.get("serializationData", {}).get("SerializationNodes", []))

    @classmethod
    def load(cls, path: Path) -> "Asset":
        return cls(json.loads(Path(path).read_text(encoding="utf-8-sig")))

    @property
    def type_name(self) -> str:
        return self.raw["AMP_SERIALIZATION_HEADER"]["AssetTypeName"]

    def as_clone_of(self, source_element: str) -> "Asset":
        self.raw["AMP_SERIALIZATION_HEADER"]["SourceElementName"] = source_element
        return self

    def to_json(self) -> dict:
        out = copy.deepcopy(self.raw)
        if "serializationData" in out:
            out["serializationData"]["SerializationNodes"] = serialize(self.nodes)
        return out

    def save(self, path: Path) -> None:
        path = Path(path)
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(self.to_json(), indent=4, ensure_ascii=False) + "\n", encoding="utf-8")


def roundtrip_equal(path: Path) -> bool:
    raw = json.loads(Path(path).read_text(encoding="utf-8-sig"))
    return Asset(copy.deepcopy(raw)).to_json() == raw


if __name__ == "__main__":
    # python tools/odin.py <export-dir> <relative file>... -> round-trip check
    export = Path(sys.argv[1])
    failures = [f for f in sys.argv[2:] if not roundtrip_equal(export / f)]
    for f in sys.argv[2:]:
        print(("FAIL " if f in failures else "ok   ") + f)
    sys.exit(1 if failures else 0)
