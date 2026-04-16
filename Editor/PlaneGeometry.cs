using UnityEngine;
using System.Collections.Generic;

namespace TwoChannelColorEncoding
{
    public static class PlaneGeometry
    {
        static readonly Vector3[] SilhouetteVertices = new Vector3[]
        {
            new Vector3(1, 0, 0),
            new Vector3(1, 1, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, 1, 1),
            new Vector3(0, 0, 1),
            new Vector3(1, 0, 1)
        };

        public static void ComputeBaseColors(Vector3 normal, out Vector3 bc1, out Vector3 bc2)
        {
            const float eps = 1e-6f;

            float[] d = new float[SilhouetteVertices.Length];
            for (int i = 0; i < SilhouetteVertices.Length; i++)
                d[i] = Vector3.Dot(normal, SilhouetteVertices[i]);

            int start = -1;
            for (int i = 0; i < SilhouetteVertices.Length; i++)
            {
                if (Mathf.Abs(d[i]) > eps)
                {
                    start = i;
                    break;
                }
            }

            if (start < 0)
            {
                bc1 = new Vector3(1f, 0f, 0f);
                bc2 = new Vector3(0f, 1f, 0f);
                return;
            }

            var hits = new List<Vector3>();

            for (int step = 0; step < SilhouetteVertices.Length; step++)
            {
                int ia = (start + step) % SilhouetteVertices.Length;
                int ib = (ia + 1) % SilhouetteVertices.Length;

                Vector3 a = SilhouetteVertices[ia];
                Vector3 b = SilhouetteVertices[ib];
                float da = d[ia];
                float db = d[ib];

                bool aOn = Mathf.Abs(da) <= eps;
                bool bOn = Mathf.Abs(db) <= eps;

                if (aOn && bOn)
                {
                    TryAddOrderedUnique(hits, a);
                    TryAddOrderedUnique(hits, b);
                    continue;
                }

                if (aOn)
                {
                    TryAddOrderedUnique(hits, a);
                    continue;
                }

                if (bOn)
                {
                    TryAddOrderedUnique(hits, b);
                    continue;
                }

                if (da * db < 0f)
                {
                    float t = da / (da - db);
                    Vector3 p = a + t * (b - a);
                    TryAddOrderedUnique(hits, p);
                }
            }

            if (hits.Count < 2)
            {
                bc1 = new Vector3(1f, 0f, 0f);
                bc2 = new Vector3(0f, 1f, 0f);
                return;
            }

            bc1 = hits[0];
            bc2 = hits[hits.Count - 1];
        }

        public static void BuildPlaneBasis(Vector3 bc1, Vector3 bc2, out Vector3 fx, out Vector3 fy)
        {
            fx = bc1.normalized;
            Vector3 proj = Vector3.Dot(fx, bc2) * fx;
            fy = (bc2 - proj).normalized;
            if (fy.sqrMagnitude < EncodingConstants.Epsilon_Normalized * EncodingConstants.Epsilon_Normalized)
                fy = LinearAlgebra.GetOrthogonal(fx);
        }

        static void TryAddOrderedUnique(List<Vector3> list, Vector3 p)
        {
            p = new Vector3(
                Mathf.Clamp01(p.x),
                Mathf.Clamp01(p.y),
                Mathf.Clamp01(p.z)
            );

            if (list.Count > 0 && Vector3.Distance(list[list.Count - 1], p) < EncodingConstants.Epsilon_DuplicateVertex)
                return;

            for (int i = 0; i < list.Count; i++)
                if (Vector3.Distance(list[i], p) < EncodingConstants.Epsilon_DuplicateVertex)
                    return;

            list.Add(p);
        }
    }
}
