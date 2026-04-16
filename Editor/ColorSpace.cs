using UnityEngine;

namespace TwoChannelColorEncoding
{
    public static class ColorSpace
    {
        public static float Luminance(Vector3 rgb)
        {
            return rgb.x * EncodingConstants.LuminanceR
                 + rgb.y * EncodingConstants.LuminanceG
                 + rgb.z * EncodingConstants.LuminanceB;
        }

        public static Vector3 ToLinearRGB(Color pixel, float gamma)
        {
            return new Vector3(
                Mathf.Pow(pixel.r, gamma),
                Mathf.Pow(pixel.g, gamma),
                Mathf.Pow(pixel.b, gamma)
            );
        }

        public static Color LinearToGamma(Vector3 linear, float invGamma)
        {
            return new Color(
                Mathf.Pow(Mathf.Clamp01(linear.x), invGamma),
                Mathf.Pow(Mathf.Clamp01(linear.y), invGamma),
                Mathf.Pow(Mathf.Clamp01(linear.z), invGamma),
                1f
            );
        }
    }
}