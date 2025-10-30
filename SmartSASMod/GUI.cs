using System;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine;
using SFS.UI;
using SFS.World;
using SFS.UI.ModGUI;

namespace SmartSASMod
{
    public enum DirectionMode
    {
        Prograde,
        Target,
        Surface,
        None,
        Default
    }
    public static class GUI
    {
        static GameObject holder;
        static readonly int MainWindowID = Builder.GetRandomID();
        public static TextInput angleInput;
        public static Dictionary<DirectionMode, SFS.UI.ModGUI.Button> buttons;

        public static void SpawnGUI()
        {
            holder = Builder.CreateHolder(Builder.SceneToAttach.CurrentScene, "Smart SAS GUI Holder");

            Vector2Int pos = SettingsManager.settings.windowPosition;
            Window window = Builder.CreateWindow(holder.transform, MainWindowID, 360, 290, pos.x, pos.y, true, true, 0.95f, "Smart SAS");

            window.CreateLayoutGroup(SFS.UI.ModGUI.Type.Vertical, UnityEngine.TextAnchor.MiddleCenter,10f);
            window.gameObject.GetComponent<DraggableWindowModule>().OnDropAction += () =>
            {
                SettingsManager.settings.windowPosition = Vector2Int.RoundToInt(window.Position);
                SettingsManager.Save();
            };

            buttons = new Dictionary<DirectionMode, SFS.UI.ModGUI.Button>();

            SFS.UI.ModGUI.Container buttonsTop_Container =  SFS.UI.ModGUI.Builder.CreateContainer(window);
            buttonsTop_Container.CreateLayoutGroup(SFS.UI.ModGUI.Type.Horizontal, UnityEngine.TextAnchor.MiddleCenter,5f);
            buttons.Add(DirectionMode.Prograde, Builder.CreateButton(buttonsTop_Container, 160, 50, 0,0, () => ToggleDirection(DirectionMode.Prograde), "Prograde"));
            buttons.Add(DirectionMode.Target, Builder.CreateButton(buttonsTop_Container, 160, 50, 0, 0, () => ToggleDirection(DirectionMode.Target), "Target"));

            SFS.UI.ModGUI.Container buttonsBottom_Container =  SFS.UI.ModGUI.Builder.CreateContainer(window);
            buttonsBottom_Container.CreateLayoutGroup(SFS.UI.ModGUI.Type.Horizontal, UnityEngine.TextAnchor.MiddleCenter,5f);
            buttons.Add(DirectionMode.Surface, Builder.CreateButton(buttonsBottom_Container, 160, 50, 0, 0, () => ToggleDirection(DirectionMode.Surface), "Surface"));
            buttons.Add(DirectionMode.None, Builder.CreateButton(buttonsBottom_Container, 160, 50, 0, 0, () => ToggleDirection(DirectionMode.None), "None"));

            Builder.CreateLabel(window, 180, 50, 0, -145, "Angle Offset");
//~             angleInput = Builder.CreateTextInput(window, 110, 50, 0, -200, "0.00");
//~             angleInput.field.onEndEdit.AddListener(VerifyOffsetInput);

//~             Builder.CreateButton(window, 50, 50, -140, -200, () => AddOffsetValue(-10), "<<");
//~             Builder.CreateButton(window, 50, 50, -85, -200, () => AddOffsetValue(-1), "<");
//~             Builder.CreateButton(window, 50, 50, 140, -200, () => AddOffsetValue(10), ">>");
//~             Builder.CreateButton(window, 50, 50, 85, -200, () => AddOffsetValue(1), ">");

            SFS.UI.ModGUI.Container offset_Container =  SFS.UI.ModGUI.Builder.CreateContainer(window);
            offset_Container.CreateLayoutGroup(SFS.UI.ModGUI.Type.Horizontal, UnityEngine.TextAnchor.MiddleCenter,5f);

            Builder.CreateButton(offset_Container, 40, 30, 0, 0, () => AddOffsetValue(-90), "<<<");
            Builder.CreateButton(offset_Container, 30, 30, 0, 0, () => AddOffsetValue(-10), "<<");
            Builder.CreateButton(offset_Container, 30, 30, 0, 0, () => AddOffsetValue(-1), "<");

            angleInput = Builder.CreateTextInput(offset_Container, 110, 50, 0, 0, "0.00");
            angleInput.field.onEndEdit.AddListener(VerifyOffsetInput);

            Builder.CreateButton(offset_Container, 30, 30, 0, 0, () => AddOffsetValue(1), ">");
            Builder.CreateButton(offset_Container, 30, 30, 0, 0, () => AddOffsetValue(10), ">>");
            Builder.CreateButton(offset_Container, 40, 30, 0, 0, () => AddOffsetValue(90), ">>>");

            window.gameObject.transform.localScale = new Vector3(SettingsManager.settings.windowScale, SettingsManager.settings.windowScale, 1f);

            PlayerController.main.player.OnChange += OnPlayerChange;
        }

        public static void OnPlayerChange(Player player)
        {
            if (player is Rocket rocket)
            {
                SASComponent sas = rocket.GetOrAddComponent<SASComponent>();
                sas.OnDirectionChange();
                sas.OnOffsetChange();
            }
        }

        public static void CheckRocketControl(Action<Rocket> onControl)
        {
            if (PlayerController.main.player.Value is Rocket rocket)
            {
                if (rocket.hasControl.Value)
                {
                    onControl(rocket);
                }
                else
                {
                    MsgDrawer.main.Log("Rocket is uncontrollable, cannot change SAS");
                }
            }
            else
            {
                MsgDrawer.main.Log("You aren't controlling a rocket...");
            }
        }

        public static void SetDirection(DirectionMode direction)
        {
            CheckRocketControl
            (
                rocket =>
                {
                    SASComponent sas = rocket.GetOrAddComponent<SASComponent>();
                    sas.Direction = direction;
                }
            );
        }

        public static void ToggleDirection(DirectionMode direction)
        {
            CheckRocketControl
            (
                rocket =>
                {
                    SASComponent sas = rocket.GetOrAddComponent<SASComponent>();
                    sas.Direction = sas.Direction != direction ? direction : DirectionMode.Default;
                }
            );
        }

        public static void AddOffsetValue(float offset)
        {
            CheckRocketControl
            (
                rocket =>
                {
                    SASComponent sas = rocket.GetOrAddComponent<SASComponent>();
                    sas.Offset = NormaliseAngle(sas.Offset + offset);
                }
            );
        }

        public static void SetOffsetValue(float offset)
        {
            CheckRocketControl
            (
                rocket =>
                {
                    SASComponent sas = rocket.GetOrAddComponent<SASComponent>();
                    sas.Offset = NormaliseAngle(offset);
                }
            );
        }

        public static float NormaliseAngle(float input)
        {
            float m = (input + 180f) % 360f;
            return m < 0 ? m + 180f : m - 180f;
        }

        public static bool StringToFloat(string input, out float result)
        {
            if (input == "NaN" || input ==  "Infinity" || input ==  "-Infinity")
            {
                result = 0f;
                return true;
            }
            try
            {
                result = float.Parse(input, NumberStyles.Float, CultureInfo.InvariantCulture);
                return false;
            }
            catch
            {
                result = 0f;
                return true;
            }
        }

        public static void VerifyOffsetInput(string input)
        {
            if (PlayerController.main.player.Value is Rocket rocket)
            {
                SASComponent sas = rocket.GetOrAddComponent<SASComponent>();
                if (input == "NaN" || input ==  "Infinity" || input ==  "-Infinity")
                {
                    sas.Offset = 0f;
                }
                else
                {
                    try
                    {
                        sas.Offset = float.Parse(input, NumberStyles.Float, CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        sas.Offset = 0f;
                    }
                }
            }
        }
    }
}