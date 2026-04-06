using System;
using ModLoader;
using ModLoader.Helpers;
using SFS.Input;
using SFS.IO;
using UITools;
using UnityEngine;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace SmartSASMod
{
    public class Settings : ModSettings<SettingsData>
    {
        public static Settings Main { get; private set; }
        protected override FilePath SettingsFile => new FolderPath(Entrypoint.Main.ModFolder).ExtendToFile("settings.txt");

        public static void Init()
        {
            Main = new Settings();
            Main.Initialize();
        }
        
        protected override void RegisterOnVariableChange(Action onChange)
        {
            Application.quitting += onChange;
        }
    }
    
    public class SettingsData
    {
        public float WindowScale { get; set; } = 1;
        public bool UseManeuvers { get; set; } = true;
        public float ProgradeMinimumSpeed { get; set; } = 3;
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

        public KeybindingsPC.Key Key_Offset_Negative { get; set; } = KeyCode.None;
        public KeybindingsPC.Key Key_Offset_Positive { get; set; } = KeyCode.None;
        public KeybindingsPC.Key Key_Offset_Negative_Small { get; set; } = KeyCode.None;
        public KeybindingsPC.Key Key_Offset_Positive_Small { get; set; } = KeyCode.None;

        public KeybindingsPC.Key Key_Reset_Offset { get; set; } = KeyCode.None;
        public KeybindingsPC.Key Key_Flip_Offset { get; set; } = KeyCode.None;

		public static void Init()
        {
            Main = SetupKeybindings<KeybindsManager>(Entrypoint.Main);
            SceneHelper.OnWorldSceneLoaded += KeyFunctions.AssignFunctions;
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
                    Key_Offset_Negative,
                    Key_Offset_Positive,
                },
                new [] {
                    defaults.Key_Offset_Negative,
                    defaults.Key_Offset_Positive,
                },
                "Change offset by ±10"
            );
            CreateUI_Keybinding(
                new [] {
                    Key_Offset_Negative_Small,
                    Key_Offset_Positive_Small,
                },
                new [] {
                    defaults.Key_Offset_Negative_Small,
                    defaults.Key_Offset_Positive_Small,
                },
                "Change offset by ±1"
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

        public static class KeyFunctions
        {

            public static void AssignFunctions()
            {
                AddOnKeyDown_World(Main.Key_Prograde, () => GUI.ToggleDirection(DirectionMode.Prograde));
                AddOnKeyDown_World(Main.Key_Target, () => GUI.ToggleDirection(DirectionMode.Target));
                AddOnKeyDown_World(Main.Key_Surface, () => GUI.ToggleDirection(DirectionMode.Surface));
                AddOnKeyDown_World(Main.Key_None, () => GUI.ToggleDirection(DirectionMode.None));
                
                AddOnKeyDown_World(Main.Key_Retrograde, () =>
                {
                    GUI.SetDirection(DirectionMode.Prograde);
                    GUI.SetOffsetValue(180);
                });
                AddOnKeyDown_World(Main.Key_Default, () => GUI.SetDirection(DirectionMode.Default));

                AddOnKeyDown_World(Main.Key_Offset_Negative, () => GUI.AddOffsetValue(-10));
                AddOnKeyDown_World(Main.Key_Offset_Positive, () => GUI.AddOffsetValue(10));
                AddOnKeyDown_World(Main.Key_Offset_Negative_Small, () => GUI.AddOffsetValue(-1));
                AddOnKeyDown_World(Main.Key_Offset_Positive_Small, () => GUI.AddOffsetValue(1));

                AddOnKeyDown_World(Main.Key_Reset_Offset, () => GUI.SetOffsetValue(0));
                AddOnKeyDown_World(Main.Key_Flip_Offset, () => GUI.AddOffsetValue(180));
            }
        }
    }
}
