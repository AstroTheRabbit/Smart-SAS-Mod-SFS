using HarmonyLib;
using ModLoader;
using ModLoader.Helpers;
using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using SFS;
using static SFS.Base;
using SFS.Parts;
using SFS.Parts.Modules;
using SFS.UI.ModGUI;
using SFS.UI;
using SFS.World;
using SFS.World.Maps;
using UnityEngine;

namespace SmartSASMod
{
    public class Main : Mod
    {
        public override void Load()
        {
            SceneHelper.OnWorldSceneLoaded += GUI.SpawnGUI;
        }
        public override void Early_Load()
        {
            new Harmony("smartsasmod").PatchAll();
        }

        [HarmonyPatch(typeof(Rocket), "GetStopRotationTurnAxis")]
        class DisableSAS
        {
            [HarmonyPostfix]
            static float Postfix(float result, Rocket __instance)
            {
                if (GUI.rotManager.rocket != null)
                {
                    GUI.rotManager.rocket.rb2d.angularDrag = 0.05f;
                }
                if (__instance != PlayerController.main.player.Value as Rocket)
                {
                    return result;
                }
                else if (GUI.rotManager.disableSAS)
                {
                    if (GUI.rotManager.rocket != null)
                    {
                        GUI.rotManager.rocket.rb2d.angularDrag = 0f;
                    }
                    return 0f;
                }
                else if (!GUI.rotManager.useDefault)
                {
                    return GUI.TorqueDirection(GUI.rotManager.deltaTheta, GUI.rotManager.rocket);
                }
                else
                {
                    return result;
                }
            }
        }

        public override string ModNameID => "smartsas";
        public override string DisplayName => "Smart SAS";
        public override string Author => "pixelgaming579";
        public override string MinimumGameVersionNecessary => "1.5.7";
        public override string ModVersion => "1.0";
        public override string Description => "Adds a variety of control options for the stability assist system (SAS).";
    }

    public static class GUI
    {
        static GameObject holder;
        public static RotationManager rotManager;
        static readonly int MainWindowID = Builder.GetRandomID();
        static TextInput angleInput;
        public static Dictionary<DirectionMode, SFS.UI.ModGUI.Button> buttons;

        public static void SpawnGUI()
        {
            holder = Builder.CreateHolder(Builder.SceneToAttach.CurrentScene, "SASMod GUI Holder");
            rotManager = holder.AddComponent<RotationManager>();
            Window window = Builder.CreateWindow(holder.transform, MainWindowID, 360, 290, -850, -500, true, true, 0.95f, "Smart SAS");

            buttons = new Dictionary<DirectionMode, SFS.UI.ModGUI.Button>()
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
        }

        public enum DirectionMode
        {
            Prograde,
            Target,
            Surface,
            None,
            Default
        }

        static void FollowDirection(DirectionMode direction)
        {
            if (!(PlayerController.main.player.Value is Rocket))
            {
                MsgDrawer.main.Log("You aren't controlling a rocket...");
                return;
            }
            else if (!(PlayerController.main.player.Value as Rocket).hasControl.Value)
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

        static void AddOffsetValue(ref TextInput textbox, float offset)
        {
            if (!(PlayerController.main.player.Value is Rocket))
                return;
            (float value, bool flag) currentValue = StringToFloat(textbox.Text);
            textbox.Text = NormaliseAngle(!currentValue.flag ? currentValue.value + offset : currentValue.value).ToString("0.00");
        }

        static (float value, bool flag) StringToFloat(string input)
        {
            try
            {
                float output = float.Parse(input, NumberStyles.Any, CultureInfo.InvariantCulture);
                return (output, false);
            }
            catch
            {
                return (0f, true);
            }
        }

        static float NormaliseAngle(float input)
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
                angleInput.Text = (0f).ToString("0.00");
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
            {
                return 0;
            }
            else if (Mathf.Abs(deltaAngle) <= 0.1f)
            {
                return -Mathf.Sign(angularVelocity);
            }
            else if (rocket.location.velocity.Value.magnitude <= 3 && GUI.rotManager.currentMode == DirectionMode.Prograde)
            {
                return Mathf.Clamp(angularVelocity / maxPossibleChangePerPhysicsStep, -1f, 1f);
            }
            else
            {
                if (Mathf.Abs(deltaAngle) > 5)
                {
                    return -Mathf.Sign(-angularVelocity - (Mathf.Sign(deltaAngle) * (25 - ((25 * 15) / (Mathf.Pow(Mathf.Abs(deltaAngle), 1.5f) + 15)))));
                }
                else
                {
                    return -Mathf.Sign(-angularVelocity - (Mathf.Sign(deltaAngle) * (25 - ((25 * 15) / (Mathf.Pow(Mathf.Abs(deltaAngle), 1.5f) + 15))))) / 2;
                }
            }
        }

        public class RotationManager : MonoBehaviour {
            public bool disableSAS = false;
            public bool useDefault = false;
            public float deltaTheta;
            public DirectionMode currentMode = DirectionMode.Default;
            public Rocket rocket;
            SelectableObject previousTarget;

            private void FixedUpdate() {
                if (!(PlayerController.main.player.Value is Rocket))
                    return;
                if (PlayerController.main.player.Value as Rocket != rocket)
                {
                    if (currentMode != DirectionMode.Default)
                        buttons[currentMode].gameObject.GetComponent<ButtonPC>().SetSelected(false);
                    currentMode = DirectionMode.Default;
                }
                rocket = PlayerController.main.player.Value as Rocket;
                float currentRotation = NormaliseAngle(rocket.GetRotation());
                float targetRotation = 0;
                useDefault = false;
                if (currentMode != DirectionMode.Target)
                {
                    previousTarget = null;
                }
                switch (currentMode)
                {
                    case DirectionMode.Prograde:
                        disableSAS = false;
                        Double2 offset = rocket.location.velocity.Value;
                        targetRotation = NormaliseAngle((float)Math.Atan2(offset.y, offset.x) * Mathf.Rad2Deg);
                        break;

                    case DirectionMode.Target:
                        disableSAS = false;
                        SelectableObject target;
                        try
                        {
                            target = Map.navigation.target;
                        }
                        catch (NullReferenceException)
                        {
                            MsgDrawer.main.Log("No target selected, switching to default SAS");
                            buttons[currentMode].gameObject.GetComponent<ButtonPC>().SetSelected(false);
                            currentMode = DirectionMode.Default;
                            useDefault = true;
                            break;
                        }
                        if (target is MapRocket)
                        {
                            if (target != previousTarget)
                            {
                                MsgDrawer.main.Log("Targeting " + (target as MapRocket).Select_DisplayName);
                                previousTarget = target;
                            }
                            Vector2 targetOffset =
                                 ((target as MapRocket).rocket.location.Value.GetSolarSystemPosition((WorldTime.main != null) ? WorldTime.main.worldTime : 0.0)
                                     + (Vector2)(target as MapRocket).rocket.rb2d.transform.TransformVector((target as MapRocket).rocket.mass.GetCenterOfMass()))
                                 - (rocket.location.Value.GetSolarSystemPosition((WorldTime.main != null) ? WorldTime.main.worldTime : 0.0)
                                     + (Vector2)rocket.rb2d.transform.TransformVector(rocket.mass.GetCenterOfMass()));

                            targetRotation = NormaliseAngle(Mathf.Atan2(targetOffset.y, targetOffset.x) * Mathf.Rad2Deg);
                        }
                        else if (target is MapPlanet)
                        {
                            if (target != previousTarget)
                            {
                                MsgDrawer.main.Log("Targeting " + (target as MapPlanet).planet.DisplayName.GetSub(0));
                                previousTarget = target;
                            }
                            Double2 currentPos = rocket.location.planet.Value.GetSolarSystemPosition() + rocket.location.position.Value + Double2.ToDouble2(rocket.rb2d.transform.TransformVector(rocket.mass.GetCenterOfMass()));
                            Double2 targetOffset = (target as MapPlanet).planet.GetSolarSystemPosition() - currentPos;
                            targetRotation = NormaliseAngle((float)Math.Atan2(targetOffset.y, targetOffset.x) * Mathf.Rad2Deg);
                        }
                        else
                        {
                            if (target == null)
                            {
                                MsgDrawer.main.Log("No target selected, switching to default SAS");
                            }
                            else
                            {
                                MsgDrawer.main.Log("Not a valid target, switching to default SAS");
                            }
                            buttons[currentMode].gameObject.GetComponent<ButtonPC>().SetSelected(false);
                            currentMode = DirectionMode.Default;
                            useDefault = true;
                        }
                        break;

                    case DirectionMode.Surface:
                        disableSAS = false;
                        targetRotation = NormaliseAngle((float)Math.Atan2(rocket.location.position.Value.y, rocket.location.position.Value.x) * Mathf.Rad2Deg);
                        break;

                    case DirectionMode.None:
                        rocket.rb2d.angularDrag = 0;
                        disableSAS = true;
                        useDefault = true;
                        break;

                    case DirectionMode.Default:
                        disableSAS = false;
                        useDefault = true;
                        break;

                    default:
                        Debug.LogError("Incorrect direction given!");
                        break;
                }
                
                if (useDefault)
                    return;

                deltaTheta = NormaliseAngle(currentRotation - (targetRotation - StringToFloat(angleInput.Text).value));
                if (deltaTheta > 180)
                {
                    deltaTheta = -360 + deltaTheta;
                }
            }
        }
    }
}