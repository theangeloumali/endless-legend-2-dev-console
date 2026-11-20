using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Amplitude;
using Amplitude.Mercury.Interop;
using Amplitude.Mercury.Simulation;
using Forge.Core;
using Newtonsoft.Json.Linq;

namespace DevConsole
{
    /// <summary>
    /// Converts a live order DTO to an OrderRecord and back by reflection over its public fields, so undo and
    /// recipes work for any order without per-type code. A type or field missing on this build yields null.
    /// </summary>
    internal static class OrderCodec
    {
        private const string InteropNamespace = "Amplitude.Mercury.Interop.";

        private static readonly HashSet<string> EmpireFields = new HashSet<string>
        {
            "EmpireIndex", "MajorEmpireIndex", "OwnerEmpireIndex", "SourceEmpireIndex", "TargetEmpireIndex",
        };

        private static readonly Assembly InteropAssembly = typeof(EditorOrder).Assembly;

        public static OrderRecord Snapshot(object order)
        {
            var record = new OrderRecord { Type = order.GetType().Name };
            foreach (var field in Fields(order.GetType()))
            {
                var value = field.GetValue(order);
                var type = field.FieldType;
                if (type == typeof(int) && OrderRecord.IsTileField(field.Name))
                {
                    record.Tiles[field.Name] = (int)value;
                    continue;
                }
                if (type == typeof(int) && EmpireFields.Contains(field.Name) && Sim.IsLocal((int)value))
                {
                    record.LocalEmpireFields.Add(field.Name);
                }
                if (type == typeof(SimulationEntityGUID))
                {
                    record.Unrecordable = true;  // GUIDs name entities of this game only
                    record.Scalars[field.Name] = new JValue((ulong)(SimulationEntityGUID)value);
                    continue;
                }
                if (type.IsArray)
                {
                    record.Unrecordable = true;
                    continue;
                }
                var encoded = Encode(value);
                if (encoded != null)
                {
                    record.Scalars[field.Name] = encoded;
                }
            }
            return record;
        }

        /// <summary>Null, with a reason, when this build lacks the type or one of the recorded fields.</summary>
        public static object Build(OrderRecord record, out string problem)
        {
            problem = null;
            var type = InteropAssembly.GetType(InteropNamespace + record.Type);
            if (type == null)
            {
                problem = $"{record.Type} is not on this build";
                return null;
            }
            object order;
            try
            {
                // some orders only have a full constructor; every field is set from the record below anyway
                order = type.GetConstructor(Type.EmptyTypes) != null
                    ? Activator.CreateInstance(type)
                    : System.Runtime.Serialization.FormatterServices.GetUninitializedObject(type);
            }
            catch (Exception exception)
            {
                problem = $"{record.Type} could not be created: {exception.Message}";
                return null;
            }

            var fields = Fields(type).ToDictionary(field => field.Name);
            foreach (var pair in record.Tiles)
            {
                if (!fields.TryGetValue(pair.Key, out var field))
                {
                    problem = $"{record.Type}.{pair.Key} is not on this build";
                    return null;
                }
                field.SetValue(order, pair.Value);
            }
            foreach (var pair in record.Scalars)
            {
                if (!fields.TryGetValue(pair.Key, out var field))
                {
                    problem = $"{record.Type}.{pair.Key} is not on this build";
                    return null;
                }
                if (!TryDecode(pair.Value, field.FieldType, out var value))
                {
                    problem = $"{record.Type}.{pair.Key}: cannot read '{pair.Value}' as {field.FieldType.Name}";
                    return null;
                }
                field.SetValue(order, value);
            }
            foreach (var name in record.LocalEmpireFields)
            {
                if (fields.TryGetValue(name, out var field) && field.FieldType == typeof(int))
                {
                    field.SetValue(order, Sim.LocalEmpireIndex);
                }
            }
            return order;
        }

        private static IEnumerable<FieldInfo> Fields(Type type) =>
            type.GetFields(BindingFlags.Public | BindingFlags.Instance).Where(field => !field.IsInitOnly);

        private static JToken Encode(object value)
        {
            switch (value)
            {
                case null:
                    return JValue.CreateNull();
                case StaticString name:
                    return new JValue(name.ToString());
                case Enum enumValue:
                    return new JValue(enumValue.ToString());
                case bool _:
                case int _:
                case long _:
                case float _:
                case double _:
                case string _:
                    return new JValue(value);
                case FixedPoint fixedPoint:
                    return new JValue((int)fixedPoint);
                default:
                    return null;  // a nested struct the codec does not know: left at its default on rebuild
            }
        }

        private static bool TryDecode(JToken token, Type type, out object value)
        {
            value = null;
            try
            {
                if (token.Type == JTokenType.Null)
                {
                    return !type.IsValueType;
                }
                if (type == typeof(StaticString))
                {
                    value = new StaticString(token.Value<string>());
                }
                else if (type.IsEnum)
                {
                    value = Enum.Parse(type, token.Value<string>());
                }
                else if (type == typeof(FixedPoint))
                {
                    value = (FixedPoint)token.Value<int>();
                }
                else if (type == typeof(SimulationEntityGUID))
                {
                    value = GuidFrom(token.Value<ulong>());
                }
                else
                {
                    value = token.ToObject(type);
                }
                return value != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>The GUID's ulong conversion is the only public way back in; resolved once by reflection.</summary>
        private static readonly MethodInfo GuidFromUlong = typeof(SimulationEntityGUID)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(method => (method.Name == "op_Implicit" || method.Name == "op_Explicit")
                                      && method.ReturnType == typeof(SimulationEntityGUID)
                                      && method.GetParameters().Select(p => p.ParameterType).SequenceEqual(new[] { typeof(ulong) }));

        private static object GuidFrom(ulong raw) =>
            GuidFromUlong == null ? (object)SimulationEntityGUID.Zero : GuidFromUlong.Invoke(null, new object[] { raw });
    }
}
