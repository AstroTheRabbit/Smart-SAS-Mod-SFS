using SFS.World;

namespace SmartSASMod
{
    public static class Extensions
    {
        public static SASComponent GetSAS(this Rocket rocket)
        {
            return rocket.GetOrAddComponent<SASComponent>();
        }
        
        public static float NormaliseAngle(this float input)
        {
            float m = (input + 180f) % 360f;
            return m < 0 ? m + 180f : m - 180f;
        }
        
        public static float InputToFloat(this string input)
        {
            if (float.TryParse(input, out float output) && !(float.IsNaN(output) || float.IsInfinity(output)))
                return output;
            else
                return 0;
        }
        
        public static float GetStopRotationTurnAxis(this Rocket rocket, float torque)
        {
            return Patches.Rocket_GetStopRotationTurnAxis.OriginalMethod(rocket, torque);
        }
    }
}