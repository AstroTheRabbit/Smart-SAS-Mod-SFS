using HarmonyLib;
using SFS.World;

namespace SmartSASMod
{
    [HarmonyPatch(typeof(Rocket), "GetStopRotationTurnAxis")]
    class DisableSAS
    {
        static float Postfix(float result, Rocket __instance)
        {
            if (GUI.rotManager.rocket != null) 
                GUI.rotManager.rocket.rb2d.angularDrag = 0.05f;

            if (__instance != PlayerController.main.player.Value as Rocket) 
                return result;

            if (GUI.rotManager.disableSAS)
            {
                if (GUI.rotManager.rocket != null) 
                    GUI.rotManager.rocket.rb2d.angularDrag = 0f;

                return 0f;
            }

            if (!GUI.rotManager.useDefault)
                return GUI.TorqueDirection(GUI.rotManager.deltaTheta, GUI.rotManager.rocket);

            return result;
        }
    }
}
