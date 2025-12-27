using System;
using UnityEngine;
using SFS.Input;
using SFS.Parsers.Json;
using ModLoader;
using ModLoader.Helpers;

namespace SmartSASMod
{
    [Serializable]
    public class UserSettings
    {
        public float windowScale = 1f;
        public Vector2Int windowPosition = new Vector2Int(-850, -500);
        public bool useANAISTargeting = true;
    }

    public static class SettingsManager
    {
        public static IFile Path => new DefaultFolder(Main.main.ModFolder).GetFile("settings.txt");
        public static UserSettings settings;
        public static KeybindsManager keybindsManager;

        public static void Load()
        {
            if (!JsonWrapper.TryLoadJson(Path, out settings))
            {
                Debug.LogWarning("Smart SAS: Settings file couldn't be loaded correctly or doesn't exist, reverting to defaults.");
            }
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

        public KeybindingsPC.Key Key_Retrograde = KeyCode.None;
        public KeybindingsPC.Key Key_Default = KeyCode.None;

        public KeybindingsPC.Key Key_Offset_Negative = KeyCode.None;
        public KeybindingsPC.Key Key_Offset_Positive = KeyCode.None;
        public KeybindingsPC.Key Key_Offset_Negative_Small = KeyCode.None;
        public KeybindingsPC.Key Key_Offset_Positive_Small = KeyCode.None;

        public KeybindingsPC.Key Key_Reset_Offset = KeyCode.None;
        public KeybindingsPC.Key Key_Flip_Offset = KeyCode.None;

        public static KeybindsManager keybindsManager;
		public static void Setup()
        {
            keybindsManager = SetupKeybindings<KeybindsManager>(Main.main);
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
                    Key_Offset_Positive
                },
                new [] {
                    defaults.Key_Offset_Negative,
                    defaults.Key_Offset_Positive
                },
                "Change offset by ±10"
            );
            CreateUI_Keybinding(
                new [] {
                    Key_Offset_Negative_Small,
                    Key_Offset_Positive_Small
                },
                new [] {
                    defaults.Key_Offset_Negative_Small,
                    defaults.Key_Offset_Positive_Small
                },
                "Change offset by ±1"
            );
            CreateUI_Keybinding(
                new [] {
                    Key_Reset_Offset,
                    Key_Flip_Offset
                },
                new [] {
                    defaults.Key_Reset_Offset,
                    defaults.Key_Flip_Offset
                },
                "Reset/Flip offset"
            );
        }

        public static class KeyFunctions
        {

            public static void AssignFunctions()
            {
                AddOnKeyDown_World(keybindsManager.Key_Prograde, () => GUI.ToggleDirection(DirectionMode.Prograde));
                AddOnKeyDown_World(keybindsManager.Key_Target, () => GUI.ToggleDirection(DirectionMode.Target));
                AddOnKeyDown_World(keybindsManager.Key_Surface, () => GUI.ToggleDirection(DirectionMode.Surface));
                AddOnKeyDown_World(keybindsManager.Key_None, () => GUI.ToggleDirection(DirectionMode.None));
                
                AddOnKeyDown_World(keybindsManager.Key_Retrograde, () =>
                {
                    GUI.SetDirection(DirectionMode.Prograde);
                    GUI.SetOffsetValue(180);
                });
                AddOnKeyDown_World(keybindsManager.Key_Default, () => GUI.SetDirection(DirectionMode.Default));

                AddOnKeyDown_World(keybindsManager.Key_Offset_Negative, () => GUI.AddOffsetValue(-10));
                AddOnKeyDown_World(keybindsManager.Key_Offset_Positive, () => GUI.AddOffsetValue(10));
                AddOnKeyDown_World(keybindsManager.Key_Offset_Negative_Small, () => GUI.AddOffsetValue(-1));
                AddOnKeyDown_World(keybindsManager.Key_Offset_Positive_Small, () => GUI.AddOffsetValue(1));

                AddOnKeyDown_World(keybindsManager.Key_Reset_Offset, () => GUI.SetOffsetValue(0));
                AddOnKeyDown_World(keybindsManager.Key_Flip_Offset, () => GUI.AddOffsetValue(180));
            }
        }
    }
}
