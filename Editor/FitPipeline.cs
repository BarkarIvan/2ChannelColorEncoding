using Unity.Collections;
using UnityEngine;
using System.Collections.Generic;

namespace TwoChannelColorEncoding
{
    public static class FitPipeline
    {
        public static EncodingData FitPlane(NativeArray<Color> pixels, int width, int height, EncodingSettings settings)
        {
            var data = new EncodingData
            {
                warnings = new List<string>(),
                width = width,
                height = height
            };

            if (!pixels.IsCreated || pixels.Length == 0)
            {
                data.warnings.Add("No pixels to process.");
                return data;
            }

            ScatterMatrix scatter = ScatterMatrix.Accumulate(pixels, settings.gamma, settings.channelWeights);

            Vector3 normal = LinearAlgebra.SmallestEigenvector(scatter);
            normal = LinearAlgebra.UndoWeightDistortion(normal, settings.channelWeights);
            data.planeNormal = normal;

            PlaneGeometry.ComputeBaseColors(normal, out data.bc1, out data.bc2);

            if (ColorEncoding.AreBaseColorsDegenerate(data.bc1, data.bc2))
                data.warnings.Add("Base colors are nearly collinear with black. Encoding quality may be poor.");

            PlaneGeometry.BuildPlaneBasis(data.bc1, data.bc2, out data.fx, out data.fy);

            return data;
        }

        public static void EncodeAndMeasure(NativeArray<Color> sourcePixels, ref EncodingData data, EncodingSettings settings)
        {
            int len = sourcePixels.Length;
            data.encodedPixels = new NativeArray<Color>(len, Allocator.TempJob);
            data.linearPixels = new NativeArray<Vector3>(len, Allocator.TempJob);
            data.hueValues = new NativeArray<float>(len, Allocator.TempJob);

            Vector3 bc1 = data.bc1;
            Vector3 bc2 = data.bc2;
            Vector3 fx = data.fx;
            Vector3 fy = data.fy;
            float gamma = settings.gamma;

            double sumError2 = 0.0;
            float maxE2 = 0f;
            double sumLumErr = 0.0;
            float hueMin = float.MaxValue;
            float hueMax = float.MinValue;

            for (int i = 0; i < len; i++)
            {
                Color c = sourcePixels[i];
                Vector3 linear = ColorSpace.ToLinearRGB(c, gamma);
                data.linearPixels[i] = linear;

                float lum = ColorSpace.Luminance(linear);
                float encLum = ColorEncoding.EncodeLuminance(lum);
                float t = ColorEncoding.ComputeHueFactor(linear, bc1, bc2, fx, fy);

                data.hueValues[i] = t;
                data.encodedPixels[i] = new Color(encLum, t, 0f, 1f);

                Vector3 decoded = ColorEncoding.DecodeColor(encLum, t, bc1, bc2);
                Vector3 diff = linear - decoded;
                float e2 = diff.x * diff.x + diff.y * diff.y + diff.z * diff.z;
                sumError2 += e2;
                if (e2 > maxE2) maxE2 = e2;

                float decLum = ColorSpace.Luminance(decoded);
                sumLumErr += Mathf.Abs(lum - decLum);

                if (t < hueMin) hueMin = t;
                if (t > hueMax) hueMax = t;
            }

            data.metrics = new ErrorMetrics
            {
                rmsError = Mathf.Sqrt((float)(sumError2 / len)),
                maxError = Mathf.Sqrt(maxE2),
                avgLuminanceError = (float)(sumLumErr / len),
                avgHueRange = hueMax - hueMin
            };

            if (data.metrics.rmsError > EncodingConstants.DefaultRmsWarningThreshold)
                data.warnings.Add($"RMS error ({data.metrics.rmsError:F4}) exceeds {EncodingConstants.DefaultRmsWarningThreshold}. Color gamut may be too wide for 2-channel approximation.");
        }
    }
}
