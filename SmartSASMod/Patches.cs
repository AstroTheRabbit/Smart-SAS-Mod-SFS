using System;
using System.Reflection;
using UnityEngine;
using HarmonyLib;
using SFS.UI;
using SFS.World;
using SFS.World.Maps;

namespace SmartSASMod
{
    public static class Patches
    {
        [HarmonyPatch(typeof(Rocket), "GetStopRotationTurnAxis")]
        public class Rocket_GetStopRotationTurnAxis
        {
            public static float Postfix(float result, Rocket __instance)
            {

                SASComponent sas = __instance.GetOrAddComponent<SASComponent>();
                __instance.rb2d.angularDamping = 0.05f;

                if (!WorldTime.main.realtimePhysics.Value || !__instance.hasControl.Value)
                    return result;

                float angularVelocity = __instance.rb2d.angularVelocity;
                float currentRotation = GUI.NormaliseAngle(__instance.GetRotation());

                float TargetRotationToTorque(float targetAngle)
                {
                    targetAngle -= sas.Offset;
                    float deltaAngle = GUI.NormaliseAngle(targetAngle - currentRotation);

                    float torque = (float) typeof(Rocket).GetMethod("GetTorque", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, null);
                    float mass = __instance.rb2d.mass;
                    if (mass > 200f)
                        torque /= Mathf.Pow(mass / 200f, 0.35f);
                    
                    float maxAcceleration = torque * Mathf.Rad2Deg / mass;
                    float stoppingTime = Mathf.Abs(angularVelocity / maxAcceleration);
                    float currentTime = Mathf.Abs(deltaAngle / angularVelocity);
                    
                    if (stoppingTime > currentTime)
                    {
                        return Mathf.Sign(angularVelocity);
                    }
                    else
                    {
                        return -Mathf.Sign(deltaAngle);
                    }
                }

                float targetRotation;
                switch (sas.Direction)
                {
                    case DirectionMode.Default:
                        return result;

                    case DirectionMode.Prograde:
                        Double2 offset = __instance.location.velocity.Value;
                        if (offset.magnitude <= 3)
                            return result;
                        return TargetRotationToTorque((float)Math.Atan2(offset.y, offset.x) * Mathf.Rad2Deg);

                    case DirectionMode.Target:
                        Rocket rocket = PlayerController.main.player.Value as Rocket;
                        try
                        {
                            SelectableObject target = __instance == rocket ? Map.navigation.target : sas.Target; // Keeps the last selected target if the sas comp.'s rocket isn't the currently controlled rocket.
                            if (Main.ANAISTraverse is Traverse traverse && __instance == rocket)
                            {
                                if (traverse.Field("_navState").GetValue().ToString() == "ANAIS_TRANSFER_PLANNED")
                                {
                                    Double2 dv = traverse.Field<Double2>("_relativeVelocity").Value;
                                    targetRotation = GUI.NormaliseAngle((float)Math.Atan2(dv.y, dv.x) * Mathf.Rad2Deg);
                                    if (target != sas.Target)
                                    {
                                        MsgDrawer.main.Log("Using ANAIS navigation to " + target.Select_DisplayName);
                                        sas.Target = target;
                                    }
                                    return TargetRotationToTorque(targetRotation);
                                }
                            }

                            if (target is MapPlayer player)
                            {
                                if (player != sas.Target)
                                {
                                    MsgDrawer.main.Log("Targeting " + player.Select_DisplayName);
                                    sas.Target = target;
                                }
                                double time = (WorldTime.main != null) ? WorldTime.main.worldTime : 0;

                                Vector2 targetOffset = player.Player.location.Value.GetSolarSystemPosition(time)
                                        - (__instance.location.Value.GetSolarSystemPosition(time)
                                        + (Vector2) __instance.rb2d.transform.TransformVector(__instance.mass.GetCenterOfMass()));

                                if (player is MapRocket mapRocket)
                                    targetOffset += (Vector2) mapRocket.rocket.rb2d.transform.TransformVector(mapRocket.rocket.mass.GetCenterOfMass());

                                return TargetRotationToTorque(Mathf.Atan2(targetOffset.y, targetOffset.x) * Mathf.Rad2Deg);
                            }
                            else if (target is MapPlanet)
                            {
                                if (target != sas.Target)
                                {
                                    MsgDrawer.main.Log("Targeting " + (target as MapPlanet).planet.DisplayName.GetSub(0));
                                    sas.Target = target;
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
                                sas.Direction = DirectionMode.Default;
                                return result;
                            }
                        }
                        catch (NullReferenceException)
                        {
                            if (rocket == __instance)
                                MsgDrawer.main.Log("No target selected, switching to default SAS");
                            sas.Direction = DirectionMode.Default;
                            return result;
                        }

                    case DirectionMode.Surface:
                        targetRotation = GUI.NormaliseAngle((float)Math.Atan2(__instance.location.position.Value.y, __instance.location.position.Value.x) * Mathf.Rad2Deg);
                        return TargetRotationToTorque(targetRotation);

                    case DirectionMode.None:
                        __instance.rb2d.angularDamping = 0;
                        return 0;
                }
                return result;
            }
        }
    }
}