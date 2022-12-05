using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SFS.Parts.Modules;
using SFS.UI;
using SFS.UI.ModGUI;
using SFS.World;
using UnityEngine;

namespace SmartSASMod
{
    public static class GUI
    {
        static GameObject holder;
        public static RotationManager rotManager;
        static readonly int MainWindowID = Builder.GetRandomID();
        public static TextInput angleInput;
        public static Dictionary<DirectionMode, SFS.UI.ModGUI.Button> buttons;
        public enum DirectionMode
        {
            Prograde,
            Target,
            Surface,
            None,
            Default
        }

        public static void SpawnGUI()
        {
            holder = Builder.CreateHolder(Builder.SceneToAttach.CurrentScene, "SASMod GUI Holder");
            rotManager = holder.AddComponent<RotationManager>();

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
            if (!(PlayerController.main.player.Value is Rocket))
            {
                MsgDrawer.main.Log("You aren't controlling a rocket...");
                return;
            }

            if (!(PlayerController.main.player.Value as Rocket).hasControl.Value)
            {
                MsgDrawer.main.Log("Rocket is uncontrollable, cannot change SAS");
                return;
            }

            if (rotManager.currentMode == direction)
            {
                buttons[direction].gameObject.GetComponent<ButtonPC>().SetSelected(false);
                rotManager.currentMode = DirectionMode.Default;
            }
            else if (rotManager.currentMode != DirectionMode.Default)
            {
                buttons[rotManager.currentMode].gameObject.GetComponent<ButtonPC>().SetSelected(false);
                buttons[direction].gameObject.GetComponent<ButtonPC>().SetSelected(true);
                rotManager.currentMode = direction;
            }
            else
            {
                buttons[direction].gameObject.GetComponent<ButtonPC>().SetSelected(true);
                rotManager.currentMode = direction;
            }

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
        public static float NormaliseAngle(float input)
        {
            while (!(input < 360 && input >= 0))
            {
                if (input < 0)
                {
                    input += 360;
                }
                else if (input >= 360)
                {
                    input -= 360;
                }
            }
            return input;
        }

        static void VerifyAngleInput(string input)
        {
            if (StringToFloat(input).flag)
            {
                angleInput.Text = 0f.ToString("0");
            }
        }

        public static float TorqueDirection(float deltaAngle, Rocket rocket)
        {
            float angularVelocity = rocket.rb2d.angularVelocity;
            float torque = (from x in rocket.partHolder.GetModules<TorqueModule>()
                            where x.enabled.Local || x.enabled.Value
                            select x).Sum((TorqueModule torqueModule) => torqueModule.torque.Value);
            float maxPossibleChangePerPhysicsStep = torque * 57.29578f / rocket.rb2d.mass * Time.fixedDeltaTime;

            if (!WorldTime.main.realtimePhysics.Value)
                return 0;

            if (Mathf.Abs(deltaAngle) <= 0.1f)
                return -Mathf.Sign(angularVelocity);

            if (rocket.location.velocity.Value.magnitude <= 3 && rotManager.currentMode == DirectionMode.Prograde)
                return Mathf.Clamp(angularVelocity / maxPossibleChangePerPhysicsStep, -1f, 1f);

            if (Mathf.Abs(deltaAngle) > 5)
                return -Mathf.Sign(-angularVelocity - (Mathf.Sign(deltaAngle) * (25 - (25 * 15 / (Mathf.Pow(Mathf.Abs(deltaAngle), 1.5f) + 15)))));

            return -Mathf.Sign(-angularVelocity - (Mathf.Sign(deltaAngle) * (25 - (25 * 15 / (Mathf.Pow(Mathf.Abs(deltaAngle), 1.5f) + 15))))) / 2;
        }
    }
}