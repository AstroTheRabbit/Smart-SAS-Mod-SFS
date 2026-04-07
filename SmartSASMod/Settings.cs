using System;
using ModLoader;
using ModLoader.Helpers;
using SFS.Input;
using SFS.IO;
using SFS.UI.ModGUI;
using UITools;
using UnityEngine;
using LayoutType = SFS.UI.ModGUI.Type;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace SmartSASMod
{
    public class Settings : ModSettings<SettingsData>
    {
        public static Settings Main { get; private set; }
        protected override FilePath SettingsFile => new FolderPath(Entrypoint.Main.ModFolder).ExtendToFile("Settings.txt");
        private static readonly Color DefaultColor = new Color(0.008f, 0.090f, 0.180f, 0.941f);

        public static void Init()
        {
            Main = new Settings();
            Main.Initialize();
            AddUI();
        }
        
        protected override void RegisterOnVariableChange(Action onChange)
        {
            Application.quitting += onChange;
        }

        private static void AddUI()
        {
            ConfigurationMenu.Add
            (
                null,
                new (string, Func<Transform, GameObject>)[]
                {
                    (Entrypoint.Main.DisplayName, CreateUI)
                }
            );
        }

        private static GameObject CreateUI(Transform holder)
        {
            Vector2Int size = ConfigurationMenu.ContentSize;
            int width = size.x - 20;
            int half_width = (width - 20) / 2;

            Box box = Builder.CreateBox(holder, size.x, size.y);
            box.CreateLayoutGroup
            (
                LayoutType.Vertical,
                TextAnchor.UpperCenter,
                20,
                new RectOffset(10, 10, 10, 10)
            );
            
            CreateInput
            (
                "Window Scale",
                settings.WindowScale,
                v =>
                {
                    settings.WindowScale = v;
                    GUI.Init();
                }
            );
            CreateInput("Prograde Minimum Speed", settings.ProgradeMinimumSpeed, v => settings.ProgradeMinimumSpeed = v);

            ToggleWithLabel _ = Builder.CreateToggleWithLabel
            (
                box,
                width, 
                40,
                () => settings.UseManeuvers, 
                () => settings.UseManeuvers = !settings.UseManeuvers,
                labelText: "Use Maneuvers"
            );

            Builder.CreateSeparator(box, width - 20);
            Builder.CreateLabel(box, width - 20, 50, text: "Angle Offset Modifiers");
            
            CreateInput("Small", settings.OffsetSmall, v => settings.OffsetSmall = v);
            CreateInput("Medium", settings.OffsetMedium, v => settings.OffsetMedium = v);
            CreateInput("Large", settings.OffsetLarge, v => settings.OffsetLarge = v);

            return box.gameObject;

            void CreateInput(string label, float get, Action<float> set)
            {
                Container container = Builder.CreateContainer(box);
                container.CreateLayoutGroup(LayoutType.Horizontal);

                Builder.CreateLabel(container, half_width, 40, text: label);
                TextInput input = Builder.CreateTextInput(container, half_width, 40, text: get.FloatToString());
                AddOnChange(input, set);
            }

            void AddOnChange(TextInput input, Action<float> onValid)
            {
                input.OnChange += text =>
                {
                    if (text.StringToFloat() is float result && result > 0)
                    {
                        input.FieldColor = DefaultColor;
                        onValid(result);
                    }
                    input.FieldColor = Color.red;
                };
            }
        }
    }
    
    public class SettingsData
    {
        /// <summary>
        /// Scale of the Smart SAS controls window.
        /// </summary>
        public float WindowScale { get; set; } = 1;
        /// <summary>
        /// Minimum speed for <c>DirectionMode.Prograde</c>, below which Smart SAS switches back to <c>DirectionMode.Default</c>.
        /// </summary>
        public float ProgradeMinimumSpeed { get; set; } = 3;
        /// <summary>
        /// Determines whether <c>DirectionMode.Target</c> should use ANAIS's transfer and approach maneuvers.
        /// </summary>
        public bool UseManeuvers { get; set; } = true;

        public float OffsetSmall { get; set; } = 1;
        public float OffsetMedium { get; set; } = 10;
        public float OffsetLarge { get; set; } = 90;
    }

    public class KeybindsManager : ModKeybindings
    {
        public static KeybindsManager Main { get; private set; }
        
        public KeybindingsPC.Key Key_Prograde { get; set; } = KeyCode.None;
        public KeybindingsPC.Key Key_Target { get; set; } = KeyCode.None;
        public KeybindingsPC.Key Key_Surface { get; set; } = KeyCode.None;
        public KeybindingsPC.Key Key_None { get; set; } = KeyCode.None;

        public KeybindingsPC.Key Key_Retrograde { get; set; } = KeyCode.None;
        public KeybindingsPC.Key Key_Default { get; set; } = KeyCode.None;
        
        public KeybindingsPC.Key Key_Offset_Negative_Small { get; set; } = KeyCode.None;
        public KeybindingsPC.Key Key_Offset_Positive_Small { get; set; } = KeyCode.None;
        public KeybindingsPC.Key Key_Offset_Negative_Medium { get; set; } = KeyCode.None;
        public KeybindingsPC.Key Key_Offset_Positive_Medium { get; set; } = KeyCode.None;
        public KeybindingsPC.Key Key_Offset_Negative_Large { get; set; } = KeyCode.None;
        public KeybindingsPC.Key Key_Offset_Positive_Large { get; set; } = KeyCode.None;

        public KeybindingsPC.Key Key_Reset_Offset { get; set; } = KeyCode.None;
        public KeybindingsPC.Key Key_Flip_Offset { get; set; } = KeyCode.None;

		public static void Init()
        {
            Main = SetupKeybindings<KeybindsManager>(Entrypoint.Main);
            SceneHelper.OnWorldSceneLoaded += AssignFunctions;
        }
        
        public override void CreateUI()
        {
            KeybindsManager defaults = new KeybindsManager();
			CreateUI_Text("Smart SAS");

			CreateUI_Keybinding(Key_Prograde, defaults.Key_Prograde, "Toggle prograde");
			CreateUI_Keybinding(Key_Target, defaults.Key_Target, "Toggle target");
			CreateUI_Keybinding(Key_Surface, defaults.Key_Surface, "Toggle surface");
			CreateUI_Keybinding(Key_None, defaults.Key_None, "Toggle none");
            CreateUI_Space();

			CreateUI_Keybinding(Key_Retrograde, defaults.Key_Retrograde, "Set retrograde");
			CreateUI_Keybinding(Key_Default, defaults.Key_Default, "Set default");
            CreateUI_Space();

            CreateUI_Keybinding(
                new [] {
                    Key_Offset_Negative_Small,
                    Key_Offset_Positive_Small,
                },
                new [] {
                    defaults.Key_Offset_Negative_Small,
                    defaults.Key_Offset_Positive_Small,
                },
                "Change offset (small)"
            );
            CreateUI_Keybinding(
                new [] {
                    Key_Offset_Negative_Medium,
                    Key_Offset_Positive_Medium,
                },
                new [] {
                    defaults.Key_Offset_Negative_Medium,
                    defaults.Key_Offset_Positive_Medium,
                },
                "Change offset (medium)"
            );
            CreateUI_Keybinding(
                new [] {
                    Key_Offset_Negative_Large,
                    Key_Offset_Positive_Large,
                },
                new [] {
                    defaults.Key_Offset_Negative_Large,
                    defaults.Key_Offset_Positive_Large,
                },
                "Change offset (large)"
            );
            
            CreateUI_Keybinding(
                new [] {
                    Key_Reset_Offset,
                    Key_Flip_Offset,
                },
                new [] {
                    defaults.Key_Reset_Offset,
                    defaults.Key_Flip_Offset,
                },
                "Reset/Flip offset"
            );
        }

        public static void AssignFunctions()
        {
            AddOnKeyDown_World(Main.Key_Prograde, GUI.ToggleDirection(DirectionMode.Prograde));
            AddOnKeyDown_World(Main.Key_Target, GUI.ToggleDirection(DirectionMode.Target));
            AddOnKeyDown_World(Main.Key_Surface, GUI.ToggleDirection(DirectionMode.Surface));
            AddOnKeyDown_World(Main.Key_None, GUI.ToggleDirection(DirectionMode.None));
            
            AddOnKeyDown_World(Main.Key_Retrograde, (Action) Delegate.Combine
            (
                GUI.SetDirection(DirectionMode.Prograde),
                GUI.SetOffset(() => 180))
            );
            AddOnKeyDown_World(Main.Key_Default, GUI.SetDirection(DirectionMode.Default));
            
            AddOnKeyDown_World(Main.Key_Offset_Negative_Small, GUI.AddOffset(() => -Settings.settings.OffsetSmall));
            AddOnKeyDown_World(Main.Key_Offset_Positive_Small, GUI.AddOffset(() => Settings.settings.OffsetSmall));
            
            AddOnKeyDown_World(Main.Key_Offset_Negative_Medium, GUI.AddOffset(() => -Settings.settings.OffsetMedium));
            AddOnKeyDown_World(Main.Key_Offset_Positive_Medium, GUI.AddOffset(() => Settings.settings.OffsetMedium));
            
            AddOnKeyDown_World(Main.Key_Offset_Negative_Large, GUI.AddOffset(() => -Settings.settings.OffsetLarge));
            AddOnKeyDown_World(Main.Key_Offset_Positive_Large, GUI.AddOffset(() => Settings.settings.OffsetLarge));

            AddOnKeyDown_World(Main.Key_Reset_Offset, GUI.SetOffset(() => 0));
            AddOnKeyDown_World(Main.Key_Flip_Offset, GUI.AddOffset(() => 180));
        }
    }
}
