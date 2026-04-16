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

        public static Vector3 ToLinearRGB(float r, float g, float b, float gamma)
        {
            return new Vector3(Mathf.Pow(r, gamma), Mathf.Pow(g, gamma), Mathf.Pow(b, gamma));
        }

        public static Vector3 ToLinearRGB(Color pixel, float gamma)
        {
            return new Vector3(
                Mathf.Pow(pixel.r, gamma),
                Mathf.Pow(pixel.g, gamma),
                Mathf.Pow(pixel.b, gamma)
            );
        }

        public static Color LinearToSRGB(Vector3 linear)
        {
            return new Color(
                Mathf.Pow(Mathf.Clamp01(linear.x), 1f / 2.2f),
                Mathf.Pow(Mathf.Clamp01(linear.y), 1f / 2.2f),
                Mathf.Pow(Mathf.Clamp01(linear.z), 1f / 2.2f),
                1f
            );
        }
    }
}