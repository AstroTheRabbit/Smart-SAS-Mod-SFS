using System.Collections.Generic;
using System.Globalization;
using System;
using SFS.UI;
using SFS.UI.ModGUI;
using SFS.World;
using UnityEngine;

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

        class GUIUpdater : MonoBehaviour
        {
            private void Update()
            {
                try
                {
                    SASComponent sas = (PlayerController.main.player.Value as Rocket).GetOrAddComponent<SASComponent>();
                    foreach (var button in buttons)
                    {
                        button.Value.gameObject.GetComponent<ButtonPC>().SetSelected(button.Key == sas.currentDirection);
                    }
                }
                catch (NullReferenceException) {}
            }
        }

        public static void SpawnGUI()
        {
            holder = Builder.CreateHolder(Builder.SceneToAttach.CurrentScene, "SASMod GUI Holder");
            holder.AddComponent<GUIUpdater>();

            Vector2Int pos = SettingsManager.settings.windowPosition;
            Window window = Builder.CreateWindow(holder.transform, MainWindowID, 360, 290, pos.x, pos.y, true, true, 0.95f, "Smart SAS");

            window.gameObject.GetComponent<DraggableWindowModule>().OnDropAction += () => 
            {
                SettingsManager.settings.windowPosition = Vector2Int.RoundToInt(window.Position);
                SettingsManager.Save();
            };

            buttons = new Dictionary<DirectionMode, SFS.UI.ModGUI.Button>
            {
                {DirectionMode.Prograde, Builder.CreateButton(window, 160, 50, -85, -25, () => FollowDirection(DirectionMode.Prograde), "Prograde")},
                {DirectionMode.Target, Builder.CreateButton(window, 160, 50, 85, -25, () => FollowDirection(DirectionMode.Target), "Target")},
                {DirectionMode.Surface, Builder.CreateButton(window, 160, 50, -85, -80, () => FollowDirection(DirectionMode.Surface), "Surface")},
                {DirectionMode.None, Builder.CreateButton(window, 160, 50, 85, -80, () => FollowDirection(DirectionMode.None), "None")}
            };

            Builder.CreateLabel(window, 180, 50, 0, -145, "Angle Offset");
            angleInput = Builder.CreateTextInput(window, 110, 50, 0, -200, "0.00");
            angleInput.field.onEndEdit.AddListener(VerifyAngleInput);

            Builder.CreateButton(window, 50, 50, -140, -200, () => AddOffsetValue(ref angleInput, -10), "<<");
            Builder.CreateButton(window, 50, 50, -85, -200, () => AddOffsetValue(ref angleInput, -1), "<");
            Builder.CreateButton(window, 50, 50, 140, -200, () => AddOffsetValue(ref angleInput, 10), ">>");
            Builder.CreateButton(window, 50, 50, 85, -200, () => AddOffsetValue(ref angleInput, 1), ">");

            window.gameObject.transform.localScale = new Vector3(SettingsManager.settings.windowScale, SettingsManager.settings.windowScale, 1f);
        }
        public static void FollowDirection(DirectionMode direction)
        {
            var rocket = PlayerController.main.player.Value;
            if (!(rocket is Rocket))
            {
                MsgDrawer.main.Log("You aren't controlling a rocket...");
                return;
            }
            else
            {
                rocket = rocket as Rocket;
            }

            if (!rocket.hasControl.Value)
            {
                MsgDrawer.main.Log("Rocket is uncontrollable, cannot change SAS");
                return;
            }

            SASComponent sas = rocket.GetOrAddComponent<SASComponent>();
            sas.currentDirection = sas.currentDirection != direction ? direction : DirectionMode.Default;
        }

        public static void AddOffsetValue(ref TextInput textbox, float offset)
        {
            if (!(PlayerController.main.player.Value is Rocket))
                return;
            (float value, bool flag) = StringToFloat(textbox.Text);
            textbox.Text = NormaliseAngle(!flag ? value + offset : value).ToString("0.00");
        }
        public static void SetOffsetValue(ref TextInput textbox, float offset)
        {
            if (!(PlayerController.main.player.Value is Rocket))
                return;
            textbox.Text = NormaliseAngle(offset).ToString("0.00");
        }

        public static (float value, bool flag) StringToFloat(string input)
        {
            if (input == "NaN" || input ==  "Infinity" || input ==  "-Infinity")
                return (0f, true);
            try
            {
                float output = float.Parse(input, NumberStyles.Float, CultureInfo.InvariantCulture);
                return (output, false);
            }
            catch
            {
                return (0f, true);
            }
        }
        public static float NormaliseAngle(float input) => input % 360;
        static void VerifyAngleInput(string input)
        {
            if (StringToFloat(input).flag)
                angleInput.Text = 0f.ToString("0.00");
        }
        public static float GetAngleOffsetFloat() => StringToFloat(angleInput.Text).value;
    }
}