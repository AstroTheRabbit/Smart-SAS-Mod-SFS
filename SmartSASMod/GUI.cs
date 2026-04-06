using System;
using System.Collections.Generic;
using System.Globalization;
using SFS.UI;
using SFS.UI.ModGUI;
using SFS.World;
using UITools;
using UnityEngine;
using ModButton = SFS.UI.ModGUI.Button;

namespace SmartSASMod
{
    public static class GUI
    {
        // ReSharper disable once InconsistentNaming
        private static GameObject holder;
        private static readonly int MainWindowID = Builder.GetRandomID();
        public static TextInput angleInput;
        public static Dictionary<DirectionMode, ModButton> buttons;

        public static void SpawnGUI()
        {
            holder = Builder.CreateHolder(Builder.SceneToAttach.CurrentScene, "Smart SAS GUI Holder");
            Window window = Builder.CreateWindow
            (
                holder.transform,
                MainWindowID,
                360,
                290,
                draggable: true,
                savePosition: true,
                titleText: "Smart SAS"
            );
            window.RegisterPermanentSaving(Entrypoint.Main.ModNameID);

            buttons = new Dictionary<DirectionMode, ModButton>
            {
                {DirectionMode.Prograde, Builder.CreateButton(window, 160, 50, -85, -25, () => ToggleDirection(DirectionMode.Prograde), "Prograde")},
                {DirectionMode.Target, Builder.CreateButton(window, 160, 50, 85, -25, () => ToggleDirection(DirectionMode.Target), "Target")},
                {DirectionMode.Surface, Builder.CreateButton(window, 160, 50, -85, -80, () => ToggleDirection(DirectionMode.Surface), "Surface")},
                {DirectionMode.None, Builder.CreateButton(window, 160, 50, 85, -80, () => ToggleDirection(DirectionMode.None), "None")},
            };

            Builder.CreateLabel(window, 180, 50, 0, -145, "Angle Offset");
            angleInput = Builder.CreateTextInput(window, 110, 50, 0, -200, "0.00");
            angleInput.field.onEndEdit.AddListener(VerifyOffsetInput);

            Builder.CreateButton(window, 50, 50, -140, -200, () => AddOffsetValue(-10), "<<");
            Builder.CreateButton(window, 50, 50, -85, -200, () => AddOffsetValue(-1), "<");
            Builder.CreateButton(window, 50, 50, 140, -200, () => AddOffsetValue(10), ">>");
            Builder.CreateButton(window, 50, 50, 85, -200, () => AddOffsetValue(1), ">");

            window.gameObject.transform.localScale = Settings.settings.WindowScale * Vector3.one;

            PlayerController.main.player.OnChange += OnPlayerChange;
        }

        public static void OnPlayerChange(Player player)
        {
            if (player is Rocket rocket)
            {
                SASComponent sas = rocket.GetSAS();
                OnDirectionChange(sas);
                OnOffsetChange(sas);
            }
        }
        
        public static void OnDirectionChange(SASComponent sas)
        {
            foreach (KeyValuePair<DirectionMode, ModButton> kvp in buttons)
            {
                kvp.Value.SetSelected(kvp.Key == sas.Direction);
            }
        }
        
        public static void OnOffsetChange(SASComponent sas)
        {
            angleInput.Text = sas.Offset.ToString("0.00", CultureInfo.InvariantCulture);
        }

        public static void CheckRocketControl(this Action<Rocket> onControl)
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
            CheckRocketControl(rocket =>
            {
                SASComponent sas = rocket.GetSAS();
                sas.Direction = direction;
            });
        }

        public static void ToggleDirection(DirectionMode direction)
        {
            CheckRocketControl(rocket =>
            {
                SASComponent sas = rocket.GetSAS();
                sas.Direction = sas.Direction != direction ? direction : DirectionMode.Default;
            });
        }

        public static void AddOffsetValue(float offset)
        {
            CheckRocketControl(rocket =>
            {
                SASComponent sas = rocket.GetSAS();
                sas.Offset = (sas.Offset + offset).NormaliseAngle();
            });
        }

        public static void SetOffsetValue(float offset)
        {
            CheckRocketControl(rocket =>
            {
                SASComponent sas = rocket.GetSAS();
                sas.Offset = offset.NormaliseAngle();
            });
        }

        public static void VerifyOffsetInput(string input)
        {
            if (PlayerController.main.player.Value is Rocket rocket)
            {
                rocket.GetSAS().Offset = input.InputToFloat();
            }
        }
    }
}