using System;
using ModLoader;
using ModLoader.Helpers;
using SFS.IO;
using SFS.Input;
using SFS.Parsers.Json;
using UnityEngine;

namespace SmartSASMod
{
    [Serializable]
    public class UserSettings
    {
        public float windowScale = 1f;
        public Vector2Int windowPosition = new Vector2Int( -850, -500 );
    }

    public static class SettingsManager
    {
        public static readonly FilePath Path = Main.modFolder.ExtendToFile("settings.txt");
        static readonly FilePath oldPath = Main.modFolder.ExtendToFile("Config.txt");
        public static UserSettings settings;
        public static KeybindsManager keybindsManager;

        public static void Load()
        {
            UserSettings oldSettings = null;
            if (oldPath.FileExists())
            {
                if (!JsonWrapper.TryLoadJson(oldPath, out oldSettings))
                {
                    Debug.Log("Smart SAS: Converting old Config.txt to Settings.txt.");
                }
                oldPath.DeleteFile();
            }
            else if (oldSettings == null && !JsonWrapper.TryLoadJson(Path, out settings) && Path.FileExists())
            {
                Debug.Log("Smart SAS: Settings file couldn't be loaded correctly or doesn't exist, reverting to defaults.");
            }

            settings = oldSettings ?? settings ?? new UserSettings();
            Save();

            KeybindsManager.Setup();

        }

        public static void Save()
        {
            if (settings == null)
                Load();
            Path.WriteText(JsonWrapper.ToJson(settings, true));
        }
    }

    public class KeybindsManager : ModKeybindings
    {
        public KeybindingsPC.Key Key_Prograde = KeyCode.None;
        public KeybindingsPC.Key Key_Target = KeyCode.None;
        public KeybindingsPC.Key Key_Surface = KeyCode.None;
        public KeybindingsPC.Key Key_None = KeyCode.None;
        public KeybindingsPC.Key Key_Offset_Negative = KeyCode.None;
        public KeybindingsPC.Key Key_Offset_Positive = KeyCode.None;
        public KeybindingsPC.Key Key_Offset_Negative_Small = KeyCode.None;
        public KeybindingsPC.Key Key_Offset_Positive_Small = KeyCode.None;
        public KeybindingsPC.Key Key_Reset_Offset = KeyCode.None;
        public KeybindingsPC.Key Key_Flip_Offset = KeyCode.None;

        public static KeybindsManager keybindsManager;
		public static void Setup()
        {
            keybindsManager = SetupKeybindings<KeybindsManager>(Main.mod);
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

            CreateUI_Keybinding(
                new KeybindingsPC.Key[] {
                    Key_Offset_Negative,
                    Key_Offset_Positive
                },
                new KeybindingsPC.Key[] {
                    defaults.Key_Offset_Negative,
                    defaults.Key_Offset_Positive
                },
                "Change offset by ±10"
            );
            CreateUI_Keybinding(
                new KeybindingsPC.Key[] {
                    Key_Offset_Negative_Small,
                    Key_Offset_Positive_Small
                },
                new KeybindingsPC.Key[] {
                    defaults.Key_Offset_Negative_Small,
                    defaults.Key_Offset_Positive_Small
                },
                "Change offset by ±1"
            );
            CreateUI_Keybinding(
                new KeybindingsPC.Key[] {
                    Key_Reset_Offset,
                    Key_Flip_Offset
                },
                new KeybindingsPC.Key[] {
                    defaults.Key_Reset_Offset,
                    defaults.Key_Flip_Offset
                },
                "Reset/Flip offset"
            );
        }

        public static class KeyFunctions
        {
            public static void ToggleButton(DirectionMode direction)
            {
                GUI.FollowDirection(direction);
            }
            public static void AddOffset(float offset)
            {
                GUI.AddOffsetValue(ref GUI.angleInput, offset);
            }
            public static void SetOffset(float offset)
            {
                GUI.SetOffsetValue(ref GUI.angleInput, offset);
            }

            public static void AssignFunctions()
            {
                AddOnKeyDown_World(keybindsManager.Key_Prograde, () => ToggleButton(DirectionMode.Prograde));
                AddOnKeyDown_World(keybindsManager.Key_Target, () => ToggleButton(DirectionMode.Target));
                AddOnKeyDown_World(keybindsManager.Key_Surface, () => ToggleButton(DirectionMode.Surface));
                AddOnKeyDown_World(keybindsManager.Key_None, () => ToggleButton(DirectionMode.None));

                AddOnKeyDown_World(keybindsManager.Key_Offset_Negative, () => AddOffset(-10));
                AddOnKeyDown_World(keybindsManager.Key_Offset_Positive, () => AddOffset(10));
                AddOnKeyDown_World(keybindsManager.Key_Offset_Negative_Small, () => AddOffset(-1));
                AddOnKeyDown_World(keybindsManager.Key_Offset_Positive_Small, () => AddOffset(1));

                AddOnKeyDown_World(keybindsManager.Key_Reset_Offset, () => SetOffset(0));
                AddOnKeyDown_World(keybindsManager.Key_Flip_Offset, () => AddOffset(180));
            }
        }
    }
}
