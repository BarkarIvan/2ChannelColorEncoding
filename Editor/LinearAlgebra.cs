using UnityEngine;

namespace TwoChannelColorEncoding
{
    public static class LinearAlgebra
    {
        public static Vector3 SmallestEigenvector(ScatterMatrix m)
        {
            return SmallestEigenvector(m.rr, m.gg, m.bb, m.rg, m.rb, m.gb);
        }

        public static Vector3 SmallestEigenvector(
            float sumRR, float sumGG, float sumBB,
            float sumRG, float sumRB, float sumGB)
        {
            float[,] a = new float[3, 3];
            a[0, 0] = sumRR; a[0, 1] = sumRG; a[0, 2] = sumRB;
            a[1, 0] = sumRG; a[1, 1] = sumGG; a[1, 2] = sumGB;
            a[2, 0] = sumRB; a[2, 1] = sumGB; a[2, 2] = sumBB;

            float[,] v = Identity3x3();

            const int maxIterations = 64;
            for (int iter = 0; iter < maxIterations; iter++)
            {
                float maxVal = 0f;
                int pi = 0, qi = 1;
                for (int i = 0; i < 3; i++)
                    for (int j = i + 1; j < 3; j++)
                        if (Mathf.Abs(a[i, j]) > maxVal)
                        {
                            maxVal = Mathf.Abs(a[i, j]);
                            pi = i;
                            qi = j;
                        }

                if (maxVal < EncodingConstants.Epsilon_Convergence) break;

                float app = a[pi, pi];
                float aqq = a[qi, qi];
                float apq = a[pi, qi];

                float theta;
                float denom = app - aqq;
                if (Mathf.Abs(denom) < EncodingConstants.Epsilon_Convergence)
                    theta = Mathf.PI * 0.25f;
                else
                    theta = 0.5f * Mathf.Atan2(2f * apq, denom);

                float c = Mathf.Cos(theta);
                float s = Mathf.Sin(theta);

                float[,] an = (float[,])a.Clone();
                an[pi, pi] = c * c * app + 2f * s * c * apq + s * s * aqq;
                an[qi, qi] = s * s * app - 2f * s * c * apq + c * c * aqq;
                an[pi, qi] = 0f;
                an[qi, pi] = 0f;

                for (int r = 0; r < 3; r++)
                {
                    if (r == pi || r == qi) continue;
                    float arp = a[r, pi];
                    float arq = a[r, qi];
                    an[r, pi] = c * arp + s * arq;
                    an[pi, r] = an[r, pi];
                    an[r, qi] = -s * arp + c * arq;
                    an[qi, r] = an[r, qi];
                }

                a = an;

                for (int r = 0; r < 3; r++)
                {
                    float vrp = v[r, pi];
                    float vrq = v[r, qi];
                    v[r, pi] = c * vrp + s * vrq;
                    v[r, qi] = -s * vrp + c * vrq;
                }
            }

            int minIdx = 0;
            float minVal = Mathf.Abs(a[0, 0]);
            for (int i = 1; i < 3; i++)
                if (Mathf.Abs(a[i, i]) < minVal)
                {
                    minVal = Mathf.Abs(a[i, i]);
                    minIdx = i;
                }

            return new Vector3(v[0, minIdx], v[1, minIdx], v[2, minIdx]).normalized;
        }

        public static Vector3 BruteForceNormal(ScatterMatrix m, int sampleCount = 4096)
        {
            Vector3 bestNormal = Vector3.up;
            float bestError = float.MaxValue;
            float goldenAngle = Mathf.PI * (3f - Mathf.Sqrt(5f));

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / (sampleCount - 1);
                float inclination = Mathf.Acos(1f - 2f * t);
                float azimuth = goldenAngle * i;

                float sn = Mathf.Sin(inclination);
                Vector3 n = new Vector3(
                    sn * Mathf.Cos(azimuth),
                    sn * Mathf.Sin(azimuth),
                    Mathf.Cos(inclination)
                );

                float err = m.QuadForm(n);
                if (err < bestError)
                {
                    bestError = err;
                    bestNormal = n;
                }
            }

            return bestNormal.normalized;
        }

        public static Vector3 UndoWeightDistortion(Vector3 normal, Vector3 weights)
        {
            return new Vector3(
                normal.x * weights.x,
                normal.y * weights.y,
                normal.z * weights.z
            ).normalized;
        }

        public static Vector3 GetOrthogonal(Vector3 v)
        {
            Vector3 ortho = Vector3.Cross(v, Vector3.up);
            if (ortho.sqrMagnitude < EncodingConstants.Epsilon_Normalized * EncodingConstants.Epsilon_Normalized)
                ortho = Vector3.Cross(v, Vector3.right);
            return ortho.normalized;
        }

        static float[,] Identity3x3()
        {
            return new float[,]
            {
                { 1, 0, 0 },
                { 0, 1, 0 },
                { 0, 0, 1 }
            };
        }
    }
}
