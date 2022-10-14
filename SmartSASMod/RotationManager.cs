using System;
using SFS.UI;
using SFS.World;
using SFS.World.Maps;
using UnityEngine;
using static SmartSASMod.GUI;

namespace SmartSASMod
{
    public class RotationManager : MonoBehaviour
    {
        public bool disableSAS;
        public bool useDefault;
        public float deltaTheta;
        public DirectionMode currentMode = DirectionMode.Default;
        public Rocket rocket;
        SelectableObject previousTarget;

        private void FixedUpdate()
        {
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
                             (target as MapRocket).rocket.location.Value.GetSolarSystemPosition((WorldTime.main != null) ? WorldTime.main.worldTime : 0.0)
                                 + (Vector2)(target as MapRocket).rocket.rb2d.transform.TransformVector((target as MapRocket).rocket.mass.GetCenterOfMass())
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

