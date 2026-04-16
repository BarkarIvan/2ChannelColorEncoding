using UnityEngine;

namespace TwoChannelColorEncoding
{
    public static class ColorEncoding
    {
        public static float EncodeLuminance(float luminance)
        {
            return Mathf.Sqrt(Mathf.Max(luminance, 0f));
        }

        public static float DecodeLuminance(float encodedLuminance)
        {
            return encodedLuminance * encodedLuminance;
        }

        public static float ComputeHueFactor(
            Vector3 pixelLinear, Vector3 bc1, Vector3 bc2,
            Vector3 fx, Vector3 fy, bool clamp)
        {
            Vector2 p2 = Project2D(pixelLinear, fx, fy);
            Vector2 a2 = Project2D(bc1, fx, fy);
            Vector2 b2 = Project2D(bc2, fx, fy);

            Vector2 b2ma = b2 - a2;
            float crossP2B2mA = Cross2D(p2, b2ma);

            if (Mathf.Abs(crossP2B2mA) < EncodingConstants.Epsilon_Degenerate)
                return 0f;

            float crossA2P2 = Cross2D(a2, p2);
            float t = crossA2P2 / crossP2B2mA;

            if (clamp) t = Mathf.Clamp01(t);
            return t;
        }

        public static Vector3 DecodeColor(float encodedLum, float t, Vector3 bc1, Vector3 bc2)
        {
            Vector3 color = Vector3.Lerp(bc1, bc2, Mathf.Clamp01(t));
            float colorLum = ColorSpace.Luminance(color);
            float targetLum = DecodeLuminance(encodedLum);
            if (colorLum > EncodingConstants.Epsilon_LuminanceFloor)
                color *= targetLum / colorLum;
            else
                color = Vector3.zero;
            return color;
        }

        public static bool AreBaseColorsDegenerate(Vector3 bc1, Vector3 bc2, float threshold = 0.01f)
        {
            return Vector3.Cross(bc1, bc2).magnitude < threshold;
        }

        static Vector2 Project2D(Vector3 v, Vector3 fx, Vector3 fy)
        {
            return new Vector2(Vector3.Dot(v, fx), Vector3.Dot(v, fy));
        }

        static float Cross2D(Vector2 u, Vector2 v)
        {
            return u.x * v.y - u.y * v.x;
        }
    }

    public struct ErrorMetrics
    {
        public float rmsError;
        public float maxError;
        public float avgLuminanceError;
        public float avgHueRange;
    }
}
