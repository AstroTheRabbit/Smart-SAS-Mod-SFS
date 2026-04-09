using System;
using System.Collections.Generic;
using SFS.UI;
using SFS.UI.ModGUI;
using SFS.World;
using UITools;
using UnityEngine;
using UnityEngine.SceneManagement;
using ModButton = SFS.UI.ModGUI.Button;
using Object = UnityEngine.Object;
using LayoutType = SFS.UI.ModGUI.Type;

namespace SmartSASMod
{
    public static class GUI
    {
        private static GameObject holder;
        private static readonly int MainWindowID = Builder.GetRandomID();
        public static TextInput angleInput;
        public static Dictionary<DirectionMode, ModButton> buttons;

        public static void Init()
        {
            if (holder)
                Object.Destroy(holder);

            if (SceneManager.GetActiveScene().name != "World_PC")
                return;

            const int windowWidth = 400;
            const int innerWidth = windowWidth - 10;
            const int halfWidth = (innerWidth - 5) / 2;
            const int offsetWidth = (innerWidth - 5*5) / 6;
            
            const int windowHeight = 300;
            const int buttonHeight = 50;
            const int inputHeight = 50;
                
            holder = Builder.CreateHolder(Builder.SceneToAttach.CurrentScene, "Smart SAS - GUI Holder");
            Window window = Builder.CreateWindow
            (
                holder.transform,
                MainWindowID,
                windowWidth,
                windowHeight,
                draggable: true,
                savePosition: true,
                titleText: "Smart SAS"
            );
            window.RegisterPermanentSaving(Entrypoint.Main.ModNameID);

            window.CreateLayoutGroup
            (
                LayoutType.Vertical,
                TextAnchor.UpperCenter,
                spacing: 5,
                new RectOffset(5, 5, 5, 5)
            );

            Container buttons_top = Builder.CreateContainer(window);
            Container buttons_bottom = Builder.CreateContainer(window);
            buttons_top.CreateLayoutGroup(LayoutType.Horizontal, spacing: 5);
            buttons_bottom.CreateLayoutGroup(LayoutType.Horizontal, spacing: 5);

            buttons = new Dictionary<DirectionMode, ModButton>
            {
                {DirectionMode.Prograde, Builder.CreateButton(buttons_top, halfWidth, buttonHeight, onClick: ToggleDirection(DirectionMode.Prograde), text: "Prograde")},
                {DirectionMode.Target, Builder.CreateButton(buttons_top, halfWidth, buttonHeight, onClick: ToggleDirection(DirectionMode.Target), text: "Target")},
                {DirectionMode.Surface, Builder.CreateButton(buttons_bottom, halfWidth, buttonHeight, onClick: ToggleDirection(DirectionMode.Surface), text: "Surface")},
                {DirectionMode.None, Builder.CreateButton(buttons_bottom, halfWidth, buttonHeight, onClick: ToggleDirection(DirectionMode.None), text: "None")},
            };

            Builder.CreateSeparator(window, innerWidth);
            
            angleInput = Builder.CreateTextInput(window, innerWidth, inputHeight, text: "0.00");
            angleInput.field.onEndEdit.AddListener(VerifyOffsetInput);

            Container buttons_offset = Builder.CreateContainer(window);
            buttons_offset.CreateLayoutGroup(LayoutType.Horizontal, spacing: 5);

            Builder.CreateButton(buttons_offset, offsetWidth, buttonHeight, onClick: AddOffset(() => -Settings.settings.OffsetLarge), text: "<<<");
            Builder.CreateButton(buttons_offset, offsetWidth, buttonHeight, onClick: AddOffset(() => -Settings.settings.OffsetMedium), text: "<<");
            Builder.CreateButton(buttons_offset, offsetWidth, buttonHeight, onClick: AddOffset(() => -Settings.settings.OffsetSmall), text: "<");
            
            Builder.CreateButton(buttons_offset, offsetWidth, buttonHeight, onClick: AddOffset(() => Settings.settings.OffsetSmall), text: ">");
            Builder.CreateButton(buttons_offset, offsetWidth, buttonHeight, onClick: AddOffset(() => Settings.settings.OffsetMedium), text: ">>");
            Builder.CreateButton(buttons_offset, offsetWidth, buttonHeight, onClick: AddOffset(() => Settings.settings.OffsetLarge), text: ">>>");

            window.gameObject.transform.localScale = Settings.settings.WindowScale * Vector3.one;
            PlayerController.main.player.OnChange += OnPlayerChange;
        }

        private static void OnPlayerChange(Player player)
        {
            if (player is Rocket rocket)
            {
                SASComponent sas = rocket.GetSAS();
                OnDirectionChange(sas);
                OnOffsetChange(sas);
            }
        }
        
        internal static void OnDirectionChange(SASComponent sas)
        {
            foreach (KeyValuePair<DirectionMode, ModButton> kvp in buttons)
            {
                kvp.Value.SetSelected(kvp.Key == sas.Direction);
            }
        }
        
        internal static void OnOffsetChange(SASComponent sas)
        {
            angleInput.Text = sas.Offset.FloatToString();
        }

        private static Action CheckRocketControl(Action<SASComponent> onControl)
        {
            return () =>
            {
                if (PlayerController.main.player.Value is Rocket rocket)
                {
                    if (rocket.hasControl.Value)
                    {
                        onControl(rocket.GetSAS());
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
            };
        }

        public static Action SetDirection(DirectionMode direction)
        {
            return CheckRocketControl
            (
                sas =>
                {
                    sas.Direction = direction;
                }
            );
        }
        
        public static Action ToggleDirection(DirectionMode direction)
        {
            return CheckRocketControl
            (
                sas =>
                {
                    sas.Direction = sas.Direction != direction ? direction : DirectionMode.Default;
                }
            );
        }

        public static Action SetOffset(Func<float> offset)
        {
            return CheckRocketControl
            (
                sas =>
                {
                    sas.Offset = offset().NormaliseAngle();
                }
            );
        }

        public static Action AddOffset(Func<float> offset)
        {
            return CheckRocketControl
            (
                sas =>
                {
                    sas.Offset = (sas.Offset + offset()).NormaliseAngle();
                }
            );
        }

        private static void VerifyOffsetInput(string input)
        {
            if (PlayerController.main.player.Value is Rocket rocket)
            {
                rocket.GetSAS().Offset = input.StringToFloat() ?? 0;
            }
        }
    }
}