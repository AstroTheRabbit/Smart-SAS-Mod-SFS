using System;
using HarmonyLib;
using SFS.World;
using SFS.World.Maps;

namespace SmartSASMod
{
    public static class Patches
    {
        [HarmonyPatch(typeof(Rocket), "GetStopRotationTurnAxis")]
        public static class Rocket_GetStopRotationTurnAxis
        {
            [HarmonyReversePatch]
            public static float OriginalMethod(Rocket __instance, float torque)
            {
                throw new Exception("Harmony Reverse Patch");
            }
        }
        
        [HarmonyPatch(typeof(Rocket), "GetTurnAxis")]
        public class Rocket_GetTurnAxis
        {
            public static bool Prefix(Rocket __instance, ref float __result, float torque, bool useStopRotation)
            {
                if (__instance.arrowkeys.turnAxis != 0)
                {
                    __result = __instance.arrowkeys.turnAxis;
                }
                else if (useStopRotation && __instance.hasControl)
                {
                    SASComponent sas = __instance.GetSAS();
                    float? turnAxis = sas.Direction.GetTurnAxis(__instance, torque, sas.Offset);
                    __result = turnAxis ?? __instance.GetStopRotationTurnAxis(torque) * (__instance.floating ? 0.1f : 1f);
                }
                else
                {
                    __result = 0;
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(MapNavigation), nameof(MapNavigation.SetTarget))]
        public static class MapNavigation_SetTarget
        {
            public static void Prefix(SelectableObject newTarget)
            {
                if (PlayerController.main.player.Value is Rocket rocket)
                {
                    rocket.GetSAS().Target = newTarget;
                }
            }
        }
    }
}