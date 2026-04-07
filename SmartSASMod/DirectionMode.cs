using System;
using System.Runtime.CompilerServices;
using HarmonyLib;
using SFS.UI;
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
        Default,
    }

    public static class DirectionModeImpl
    {
        public static float? GetTurnAxis(this DirectionMode mode, Rocket rocket, float torque, float offset)
        {
            if (mode != DirectionMode.None)
                rocket.rb2d.angularDamping = 0.05f;
            
            switch (mode)
            {
                case DirectionMode.Prograde:
                    return GetTurnAxis_Prograde(rocket, torque, offset);
                case DirectionMode.Target:
                    return GetTurnAxis_Target(rocket, torque, offset);
                case DirectionMode.Surface:
                    return GetTurnAxis_Surface(rocket, torque, offset);
                case DirectionMode.None:
                    return GetTurnAxis_None(rocket);
                case DirectionMode.Default:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }

        public static float TargetAngleToTurnAxis(float targetAngle, Rocket rocket, float torque, float offset)
        {
            float mass = rocket.rb2d.mass;
            float currentAngle = rocket.GetRotation();
            float angularVelocity = rocket.rb2d.angularVelocity;
            
            float deltaAngle = (targetAngle - currentAngle - offset).NormaliseAngle();
            float maxAcceleration = torque * Mathf.Rad2Deg / mass;

            float stoppingTime = Mathf.Abs(angularVelocity / maxAcceleration);
            float currentTime = Mathf.Abs(deltaAngle / angularVelocity);

            return stoppingTime > currentTime ? Mathf.Sign(angularVelocity) : -Mathf.Sign(deltaAngle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float TargetVectorToTurnAxis(Double2 targetVector, Rocket rocket, float torque, float offset)
        {
            float angle = (float) targetVector.AngleRadians * Mathf.Rad2Deg;
            return TargetAngleToTurnAxis(angle, rocket, torque, offset);
        }

        private static float? GetTurnAxis_Prograde(Rocket rocket, float torque, float offset)
        {
            Double2 vel = rocket.location.velocity.Value;
            if (vel.magnitude < Settings.settings.ProgradeMinimumSpeed)
            {
                MsgDrawer.main.Log("Speed too low, switching to default SAS");
                rocket.GetSAS().Direction = DirectionMode.Default;
                return null;
            }
            return TargetVectorToTurnAxis(vel, rocket, torque, offset);
        }

        private static float? GetTurnAxis_Target(Rocket rocket, float torque, float offset)
        {
            SASComponent sas = rocket.GetSAS();
            if (!sas.Target)
            {
                if (rocket.isPlayer)
                    MsgDrawer.main.Log("No target selected, switching to default SAS");
                sas.Direction = DirectionMode.Default;
                return null;
            }

            if (Settings.settings.UseManeuvers && rocket.isPlayer)
            {
                if (Entrypoint.ANAISTraverse is Traverse traverse)
                {
                    if (traverse.Field("_navState").GetValue().ToString() == "ANAIS_TRANSFER_PLANNED")
                    {
                        Double2 dv = traverse.Field<Double2>("_relativeVelocity").Value;
                        return TargetVectorToTurnAxis(dv, rocket, torque, offset);
                    }
                }
                else
                {
                    // TODO: Vanilla maneuver system? Seems really annoying to get the 𝚫V vector from the vanilla system.
                }
            }

            Double2 currentPos, targetPos;
            if (rocket.location.Value.planet == sas.Target.Location.planet)
            {
                currentPos = rocket.location.Value.position;
                targetPos = sas.Target.Location.position;
            }
            else
            {
                double time = WorldTime.main.worldTime;
                currentPos = rocket.location.Value.GetSolarSystemPosition(time);
                targetPos = sas.Target.Location.GetSolarSystemPosition(time);
            }
            Double2 dir = targetPos - currentPos;
            return TargetVectorToTurnAxis(dir, rocket, torque, offset);
        }

        private static float? GetTurnAxis_Surface(Rocket rocket, float torque, float offset)
        {
            Double2 pos = rocket.location.position.Value;
            return TargetVectorToTurnAxis(pos, rocket, torque, offset);
        }

        private static float? GetTurnAxis_None(Rocket rocket)
        {
            rocket.rb2d.angularDamping = 0;
            return 0;
        }
    }
}