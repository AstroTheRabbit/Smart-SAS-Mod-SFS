using UnityEngine;
using System;
using HarmonyLib;
using SFS.World;
using SFS.UI;
using SFS.World.Maps;

namespace SmartSASMod
{
    public class SASComponent : MonoBehaviour
    {
        public DirectionMode currentDirection = DirectionMode.Default;
        public SelectableObject previousTarget;
        // public float angularVelocity;
    }

    [HarmonyPatch(typeof(Rocket), "GetStopRotationTurnAxis")]
    class DisableSAS
    {
        static float Postfix(float result, Rocket __instance)
        {
            SASComponent sas = __instance.GetOrAddComponent<SASComponent>();
            __instance.rb2d.angularDrag = 0.05f;

            if (!WorldTime.main.realtimePhysics.Value || !__instance.hasControl.Value)
                return result;

            Traverse traverse = Traverse.Create(__instance);
            float angularVelocity = __instance.rb2d.angularVelocity;
            float torque = traverse.Method("GetTorque").GetValue<float>();
            float maxPossibleChangePerPhysicsStep = traverse.Method("GetMaxPossibleChangePerPhysicsStep", torque).GetValue<float>();
            float angleOffset = GUI.GetAngleOffsetFloat();
            float currentRotation = GUI.NormaliseAngle(__instance.GetRotation());

            float targetRotation = 0;
            switch (sas.currentDirection)
            {
                case DirectionMode.Default:
                    return result;

                case DirectionMode.Prograde:
                    Double2 offset = __instance.location.velocity.Value;
                    if (offset.magnitude <= 3)
                        return result;
                    targetRotation = GUI.NormaliseAngle((float)Math.Atan2(offset.y, offset.x) * Mathf.Rad2Deg);
                    return TargetRotationToTorque(targetRotation);

                case DirectionMode.Target:
                    Rocket rocket = PlayerController.main.player.Value as Rocket;
                    try
                    {
                        SelectableObject target = __instance == rocket ? Map.navigation.target : sas.previousTarget; // Keeps the last selected target if the sas comp.'s rocket isn't the currently controlled rocket.
                        if (target is MapRocket)
                        {
                            if (target != sas.previousTarget)
                            {
                                MsgDrawer.main.Log("Targeting " + (target as MapRocket).Select_DisplayName);
                                sas.previousTarget = target;
                            }
                            Vector2 targetOffset =
                                (target as MapRocket).rocket.location.Value.GetSolarSystemPosition((WorldTime.main != null) ? WorldTime.main.worldTime : 0.0)
                                    + (Vector2)(target as MapRocket).rocket.rb2d.transform.TransformVector((target as MapRocket).rocket.mass.GetCenterOfMass())
                                - (__instance.location.Value.GetSolarSystemPosition((WorldTime.main != null) ? WorldTime.main.worldTime : 0.0)
                                    + (Vector2)__instance.rb2d.transform.TransformVector(__instance.mass.GetCenterOfMass()));

                            targetRotation = GUI.NormaliseAngle(Mathf.Atan2(targetOffset.y, targetOffset.x) * Mathf.Rad2Deg);
                            return TargetRotationToTorque(targetRotation);
                        }
                        else if (target is MapPlanet)
                        {
                            if (target != sas.previousTarget)
                            {
                                MsgDrawer.main.Log("Targeting " + (target as MapPlanet).planet.DisplayName.GetSub(0));
                                sas.previousTarget = target;
                            }
                            Double2 currentPos = __instance.location.planet.Value.GetSolarSystemPosition() + __instance.location.position.Value + Double2.ToDouble2(__instance.rb2d.transform.TransformVector(__instance.mass.GetCenterOfMass()));
                            Double2 targetOffset = (target as MapPlanet).planet.GetSolarSystemPosition() - currentPos;
                            targetRotation = GUI.NormaliseAngle((float)Math.Atan2(targetOffset.y, targetOffset.x) * Mathf.Rad2Deg);
                            return TargetRotationToTorque(targetRotation);
                        }
                        else
                        {
                            if (rocket == __instance)
                            {
                                if (target == null)
                                {
                                    MsgDrawer.main.Log("No target selected, switching to default SAS");
                                }
                                else
                                {
                                    MsgDrawer.main.Log("Not a valid target, switching to default SAS");
                                }
                            }
                            sas.currentDirection = DirectionMode.Default;
                            return result;
                        }
                    }
                    catch (NullReferenceException)
                    {
                        if (rocket == __instance)
                            MsgDrawer.main.Log("No target selected, switching to default SAS");
                        sas.currentDirection = DirectionMode.Default;
                        return result;
                    }

                case DirectionMode.Surface:
                    targetRotation = GUI.NormaliseAngle((float)Math.Atan2(__instance.location.position.Value.y, __instance.location.position.Value.x) * Mathf.Rad2Deg);
                    return TargetRotationToTorque(targetRotation);

                case DirectionMode.None:
                    __instance.rb2d.angularDrag = 0;
                    return 0;
            }

            float TargetRotationToTorque(float rot)
            {
                float deltaAngle = GUI.NormaliseAngle(currentRotation - (rot - angleOffset));
                if (deltaAngle > 180)
                    deltaAngle -= 360;
                float o = -Mathf.Sign(-angularVelocity - (Mathf.Sign(deltaAngle) * (25 - (25 * 15 / (Mathf.Pow(Mathf.Abs(deltaAngle), 1.5f) + 15)))));
                return Mathf.Abs(deltaAngle) > 5 ? o : Mathf.Abs(deltaAngle) > 0.05f ? o / 2 : result;
            }
            return result;
        }
    }

    // [HarmonyPatch(typeof(Rocket), "FixedUpdate")]
    // class AngularVelocityInTimewarp
    // {
    //     static void Postfix(Rocket __instance)
    //     {
    //         SASComponent sas = __instance.GetOrAddComponent<SASComponent>();
    //         Debug.Log(__instance.rb2d.angularVelocity);
    //     }
    // }
}