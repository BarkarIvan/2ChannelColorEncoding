using Unity.Collections;
using UnityEngine;

namespace TwoChannelColorEncoding
{
    public struct ScatterMatrix
    {
        public float rr, gg, bb, rg, rb, gb;

        public static ScatterMatrix Accumulate(NativeArray<Color> pixels, float gamma, Vector3 weights)
        {
            var m = new ScatterMatrix();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];
                float r = Mathf.Pow(c.r, gamma) * weights.x;
                float g = Mathf.Pow(c.g, gamma) * weights.y;
                float b = Mathf.Pow(c.b, gamma) * weights.z;
                m.rr += r * r; m.gg += g * g; m.bb += b * b;
                m.rg += r * g; m.rb += r * b; m.gb += g * b;
            }
            return m;
        }

        public static ScatterMatrix Accumulate(Color[] pixels, float gamma, Vector3 weights)
        {
            var m = new ScatterMatrix();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];
                float r = Mathf.Pow(c.r, gamma) * weights.x;
                float g = Mathf.Pow(c.g, gamma) * weights.y;
                float b = Mathf.Pow(c.b, gamma) * weights.z;
                m.rr += r * r; m.gg += g * g; m.bb += b * b;
                m.rg += r * g; m.rb += r * b; m.gb += g * b;
            }
            return m;
        }

        public float QuadForm(Vector3 n)
        {
            return n.x * n.x * rr
                 + n.y * n.y * gg
                 + n.z * n.z * bb
                 + 2f * n.x * n.y * rg
                 + 2f * n.x * n.z * rb
                 + 2f * n.y * n.z * gb;
        }
    }
}
