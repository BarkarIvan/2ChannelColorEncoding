using Unity.Collections;
using UnityEngine;

namespace TwoChannelColorEncoding
{
    public static class PreviewGenerator
    {
        public static void Generate(ref EncodingData data, ref EncodingAssets assets)
        {
            int w = data.width;
            int h = data.height;
            int len = data.linearPixels.Length;

            var decodedColors = new NativeArray<Color>(len, Allocator.Temp);
            var errorColors = new NativeArray<Color>(len, Allocator.Temp);
            var hueColors = new NativeArray<Color>(len, Allocator.Temp);
            var pixelErrors = new NativeArray<float>(len, Allocator.Temp);

            float maxE = 0.001f;
            Vector3 bc1 = data.bc1;
            Vector3 bc2 = data.bc2;

            for (int i = 0; i < len; i++)
            {
                Vector3 linear = data.linearPixels[i];
                float encLum = data.encodedPixels[i].r;
                float t = data.hueValues[i];

                Vector3 dec = ColorEncoding.DecodeColor(encLum, t, bc1, bc2);
                decodedColors[i] = ColorSpace.LinearToGamma(dec, assets.asset.gamma);

                float err = Vector3.Magnitude(linear - dec);
                pixelErrors[i] = err;
                if (err > maxE) maxE = err;
            }

            for (int i = 0; i < len; i++)
            {
                errorColors[i] = ErrorToColor(pixelErrors[i] / maxE);
                hueColors[i] = HueToColor(data.hueValues[i]);
            }

            assets.decodedPreview = CreateTexture(w, h, decodedColors);
            assets.errorHeatmap = CreateTexture(w, h, errorColors);
            assets.hueVisualization = CreateTexture(w, h, hueColors);

            decodedColors.Dispose();
            errorColors.Dispose();
            hueColors.Dispose();
            pixelErrors.Dispose();

            GeneratePlaneVisualization(data.bc1, data.bc2, assets.asset.gamma, ref assets);
        }

        static Texture2D CreateTexture(int w, int h, NativeArray<Color> pixels)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBAFloat, false);
            tex.SetPixelData(pixels, 0);
            tex.Apply();
            return tex;
        }

        static Color ErrorToColor(float normalized)
        {
            Color ec;
            if (normalized < 0.5f)
                ec = Color.Lerp(new Color(0, 0, 1f), new Color(0, 1f, 0), normalized * 2f);
            else
                ec = Color.Lerp(new Color(0, 1f, 0), new Color(1f, 0, 0), (normalized - 0.5f) * 2f);
            ec.a = 1f;
            return ec;
        }

        static Color HueToColor(float hueValue)
        {
            float ht = Mathf.Clamp01((hueValue + 0.5f) / 1.5f);
            Color c = Color.HSVToRGB(ht * 0.8f, 1f, 1f);
            c.a = 1f;
            return c;
        }

        static void GeneratePlaneVisualization(Vector3 bc1, Vector3 bc2, float gamma, ref EncodingAssets assets)
        {
            const int size = 256;
            int total = size * size;
            var colors = new NativeArray<Color>(total, Allocator.Temp);

            Vector3 fx, fy;
            PlaneGeometry.BuildPlaneBasis(bc1, bc2, out fx, out fy);

            float maxR = Mathf.Max(bc1.magnitude, bc2.magnitude) * 1.2f;

            Vector2 a2 = new Vector2(Vector3.Dot(bc1, fx), Vector3.Dot(bc1, fy));
            Vector2 b2 = new Vector2(Vector3.Dot(bc2, fx), Vector3.Dot(bc2, fy));

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = ((float)x / size - 0.5f) * 2f * maxR;
                    float v = ((float)y / size - 0.5f) * 2f * maxR;

                    Vector3 pt = u * fx + v * fy;
                    Color c = ColorSpace.LinearToGamma(pt, gamma);
                    c.a = 0.3f;

                    if (DistanceToLineSegment(new Vector2(u, v), a2, b2) < maxR * 0.02f)
                        c = Color.Lerp(c, new Color(1f, 1f, 1f, 1f), 0.7f);

                    colors[y * size + x] = c;
                }
            }

            DrawCrosshair(colors, size, bc1, fx, fy, maxR, new Color(1f, 0.3f, 0.3f, 1f));
            DrawCrosshair(colors, size, bc2, fx, fy, maxR, new Color(0.3f, 0.3f, 1f, 1f));

            assets.planeVisualization = new Texture2D(size, size, TextureFormat.RGBAFloat, false);
            assets.planeVisualization.SetPixelData(colors, 0);
            assets.planeVisualization.Apply();
            colors.Dispose();
        }

        static float DistanceToLineSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float len2 = Vector2.Dot(ab, ab);
            if (len2 < EncodingConstants.Epsilon_Degenerate) return Vector2.Distance(p, a);
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len2);
            return Vector2.Distance(p, a + t * ab);
        }

        static void DrawCrosshair(NativeArray<Color> colors, int size, Vector3 pt, Vector3 fx, Vector3 fy, float maxR, Color color)
        {
            int px = (int)((Vector3.Dot(pt, fx) / maxR * 0.5f + 0.5f) * size);
            int py = (int)((Vector3.Dot(pt, fy) / maxR * 0.5f + 0.5f) * size);
            const int r = 3;

            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                {
                    int ix = px + dx;
                    int iy = py + dy;
                    if (ix >= 0 && ix < size && iy >= 0 && iy < size)
                        colors[iy * size + ix] = color;
                }
        }
    }
}
