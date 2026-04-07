using System.Globalization;
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
        
        public static float? StringToFloat(this string input)
        {
            if (float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out float output))
            {
                if (!float.IsNaN(output) && !float.IsInfinity(output))
                {
                    return output;
                }
            }
            return null;
        }
        
        public static string FloatToString(this float input)
        {
            return input.ToString(CultureInfo.InvariantCulture);
        }
        
        public static float GetStopRotationTurnAxis(this Rocket rocket, float torque)
        {
            return Patches.Rocket_GetStopRotationTurnAxis.OriginalMethod(rocket, torque);
        }
    }
}