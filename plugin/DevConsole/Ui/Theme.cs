using UnityEngine;

namespace DevConsole.Ui
{
    /// <summary>
    /// Styling for the console. IMGUI's stock skin is translucent grey, which is unreadable over a game map, so
    /// every control gets an opaque, high-contrast style built from generated textures (no assets to ship).
    /// Colours follow the game's HUD: near-black slate panels, teal accents, amber values.
    /// </summary>
    internal static class Theme
    {
        public static readonly Color Background = new Color32(0x10, 0x15, 0x1C, 0xFA);
        public static readonly Color Panel = new Color32(0x19, 0x21, 0x2B, 0xFF);
        public static readonly Color Raised = new Color32(0x24, 0x2F, 0x3D, 0xFF);
        public static readonly Color Hover = new Color32(0x2F, 0x3E, 0x50, 0xFF);
        public static readonly Color Sunken = new Color32(0x0A, 0x0E, 0x13, 0xFF);
        public static readonly Color Accent = new Color32(0x5F, 0xC9, 0xD6, 0xFF);
        public static readonly Color AccentDim = new Color32(0x1E, 0x4A, 0x52, 0xFF);
        public static readonly Color Warm = new Color32(0xE2, 0xA8, 0x54, 0xFF);
        public static readonly Color Text = new Color32(0xD8, 0xE1, 0xEA, 0xFF);
        public static readonly Color TextDim = new Color32(0x8B, 0x99, 0xA8, 0xFF);
        public static readonly Color Line = new Color32(0x33, 0x41, 0x52, 0xFF);

        public static GUIStyle Window, Header, Section, Label, Value, Hint, Button, Danger, Tab, TabActive, Toggle, Field;

        private static GUISkin skin;
        private static bool built;

        /// <summary>Swaps in the console skin for the duration of the window; the caller must End afterwards.</summary>
        public static GUISkin Begin()
        {
            Build();
            var previous = GUI.skin;
            GUI.skin = skin;
            return previous;
        }

        public static void End(GUISkin previous) => GUI.skin = previous;

        private static void Build()
        {
            if (built)
            {
                return;
            }
            built = true;
            skin = Object.Instantiate(GUI.skin);
            skin.hideFlags = HideFlags.HideAndDontSave;

            Window = Style(skin.window, Background, Text, 12, TextAnchor.UpperLeft);
            Window.normal.background = Framed(Background, Line);
            Window.onNormal.background = Window.normal.background;
            Window.border = new RectOffset(1, 1, 1, 1);  // must match the 3x3 source or the frame is stretched
            Window.padding = new RectOffset(10, 10, 10, 10);

            Label = Style(skin.label, Color.clear, Text, 12, TextAnchor.MiddleLeft);
            Value = Style(skin.label, Color.clear, Warm, 12, TextAnchor.MiddleLeft);
            Hint = Style(skin.label, Color.clear, TextDim, 11, TextAnchor.MiddleLeft);
            Hint.wordWrap = true;

            Header = Style(skin.label, Panel, Accent, 13, TextAnchor.MiddleLeft);
            Header.padding = new RectOffset(10, 10, 6, 6);
            Header.fontStyle = FontStyle.Bold;

            Section = Style(skin.label, AccentDim, Accent, 12, TextAnchor.MiddleLeft);
            Section.padding = new RectOffset(10, 10, 5, 5);
            Section.margin = new RectOffset(0, 0, 10, 4);
            Section.fontStyle = FontStyle.Bold;

            Button = Style(skin.button, Raised, Text, 12, TextAnchor.MiddleCenter);
            Button.padding = new RectOffset(10, 10, 6, 6);
            Button.margin = new RectOffset(2, 2, 2, 2);
            Frame(Button, Raised, Hover, AccentDim);
            Button.hover.textColor = Color.white;
            Button.active.textColor = Color.white;

            Danger = new GUIStyle(Button);
            Danger.normal.textColor = new Color32(0xE8, 0x8C, 0x7A, 0xFF);

            Tab = Style(skin.button, Panel, TextDim, 12, TextAnchor.MiddleCenter);
            Tab.padding = new RectOffset(6, 6, 7, 7);
            Tab.margin = new RectOffset(1, 1, 0, 0);
            Frame(Tab, Panel, Hover, Panel, Line, Line);
            Tab.hover.textColor = Text;

            TabActive = new GUIStyle(Tab);
            TabActive.normal.background = Framed(AccentDim, Accent);
            TabActive.border = new RectOffset(1, 1, 1, 1);
            TabActive.normal.textColor = Color.white;
            TabActive.hover.background = TabActive.normal.background;
            TabActive.hover.textColor = Color.white;
            TabActive.fontStyle = FontStyle.Bold;

            Toggle = Style(skin.toggle, Color.clear, Text, 12, TextAnchor.MiddleLeft);
            Toggle.padding = new RectOffset(22, 4, 3, 3);
            Toggle.margin = new RectOffset(4, 4, 3, 3);

            Field = Style(skin.textField, Sunken, Warm, 12, TextAnchor.MiddleLeft);
            Field.padding = new RectOffset(7, 7, 5, 5);
            Field.margin = new RectOffset(2, 2, 2, 2);
            Field.normal.background = Framed(Sunken, Line);
            Field.focused.background = Framed(Sunken, Accent);
            Field.border = new RectOffset(1, 1, 1, 1);
            Field.focused.textColor = Color.white;

            skin.window = Window;
            skin.label = Label;
            skin.button = Button;
            skin.toggle = Toggle;
            skin.textField = Field;
            skin.box = Section;
            skin.scrollView.normal.background = Solid(Sunken);
        }

        /// <summary>Applies the three interaction backgrounds plus the border inset they need.</summary>
        private static void Frame(GUIStyle style, Color normal, Color hover, Color active,
                                  Color normalBorder = default, Color activeBorder = default)
        {
            style.normal.background = Framed(normal, normalBorder.a > 0f ? normalBorder : Line);
            style.hover.background = Framed(hover, activeBorder.a > 0f ? activeBorder : Accent);
            style.active.background = Framed(active, activeBorder.a > 0f ? activeBorder : Accent);
            style.border = new RectOffset(1, 1, 1, 1);
        }

        private static GUIStyle Style(GUIStyle from, Color background, Color text, int size, TextAnchor anchor)
        {
            var style = new GUIStyle(from)
            {
                fontSize = size,
                alignment = anchor,
                wordWrap = false,
            };
            style.normal.textColor = text;
            style.onNormal.textColor = text;
            if (background.a > 0f)
            {
                style.normal.background = Solid(background);
                style.onNormal.background = style.normal.background;
            }
            return style;
        }

        private static Texture2D Solid(Color color)
        {
            var texture = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        /// <summary>A 1px-bordered fill. IMGUI stretches the middle, so a 3x3 with border insets tiles cleanly.</summary>
        private static Texture2D Framed(Color fill, Color border)
        {
            var texture = new Texture2D(3, 3) { hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Point };
            for (var x = 0; x < 3; x++)
            {
                for (var y = 0; y < 3; y++)
                {
                    texture.SetPixel(x, y, x == 1 && y == 1 ? fill : border);
                }
            }
            texture.Apply();
            return texture;
        }
    }
}
