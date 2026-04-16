using UnityEngine;
using System.Collections.Generic;

namespace TwoChannelColorEncoding
{
    public static class PlaneGeometry
    {
        static readonly Vector3[] CubeVertices = new Vector3[]
        {
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, 0, 1),
            new Vector3(1, 1, 0),
            new Vector3(1, 0, 1),
            new Vector3(0, 1, 1),
            new Vector3(1, 1, 1)
        };

        static readonly int[,] CubeEdges = new int[,]
        {
            {0, 1}, {0, 2}, {0, 3},
            {1, 4}, {1, 5},
            {2, 4}, {2, 6},
            {3, 5}, {3, 6},
            {4, 7}, {5, 7}, {6, 7}
        };

        public static void ComputeBaseColors(Vector3 normal, out Vector3 bc1, out Vector3 bc2)
        {
            var intersections = IntersectCubeEdges(normal);

            intersections.RemoveAll(p => p.magnitude < EncodingConstants.Epsilon_Normalized);

            if (intersections.Count < 2)
            {
                Debug.LogWarning("[2ChEncode] Degenerate plane: fewer than 2 cube-edge intersections. Using fallback base colors.");
                bc1 = new Vector3(1f, 0f, 0f);
                bc2 = new Vector3(0f, 1f, 0f);
                return;
            }

            if (intersections.Count == 2)
            {
                bc1 = intersections[0];
                bc2 = intersections[1];
                return;
            }

            PickNearestPair(intersections, normal, out bc1, out bc2);
        }

        public static void BuildPlaneBasis(Vector3 bc1, Vector3 bc2, out Vector3 fx, out Vector3 fy)
        {
            fx = bc1.normalized;
            Vector3 proj = Vector3.Dot(fx, bc2) * fx;
            fy = (bc2 - proj).normalized;
            if (fy.magnitude < EncodingConstants.Epsilon_Normalized)
                fy = LinearAlgebra.GetOrthogonal(fx);
        }

        static List<Vector3> IntersectCubeEdges(Vector3 normal)
        {
            var intersections = new List<Vector3>();

            for (int e = 0; e < CubeEdges.GetLength(0); e++)
            {
                Vector3 p1 = CubeVertices[CubeEdges[e, 0]];
                Vector3 p2 = CubeVertices[CubeEdges[e, 1]];
                Vector3 dir = p2 - p1;

                float denom = Vector3.Dot(normal, dir);
                float numer = Vector3.Dot(normal, p1);

                if (Mathf.Abs(denom) < EncodingConstants.Epsilon_Degenerate)
                {
                    if (Mathf.Abs(numer) < EncodingConstants.Epsilon_Degenerate)
                    {
                        TryAddUnique(intersections, p1);
                        TryAddUnique(intersections, p2);
                    }
                    continue;
                }

                float t = -numer / denom;
                if (t < -EncodingConstants.Epsilon_Normalized || t > 1f + EncodingConstants.Epsilon_Normalized)
                    continue;
                t = Mathf.Clamp01(t);

                Vector3 pt = p1 + t * dir;
                TryAddUnique(intersections, pt);
            }

            return intersections;
        }

        static void TryAddUnique(List<Vector3> list, Vector3 pt)
        {
            pt = new Vector3(
                Mathf.Clamp01(pt.x),
                Mathf.Clamp01(pt.y),
                Mathf.Clamp01(pt.z)
            );

            for (int i = 0; i < list.Count; i++)
                if (Vector3.Distance(list[i], pt) < EncodingConstants.Epsilon_DuplicateVertex)
                    return;

            list.Add(pt);
        }

        static void PickNearestPair(List<Vector3> pts, Vector3 normal, out Vector3 bc1, out Vector3 bc2)
        {
            Vector3 planeU = pts[0].normalized;
            Vector3 planeV = Vector3.Cross(normal, planeU).normalized;
            if (planeV.magnitude < EncodingConstants.Epsilon_Normalized)
            {
                planeU = LinearAlgebra.GetOrthogonal(normal);
                planeV = Vector3.Cross(normal, planeU).normalized;
            }

            float[] angles = new float[pts.Count];
            for (int i = 0; i < pts.Count; i++)
            {
                float u = Vector3.Dot(pts[i], planeU);
                float v = Vector3.Dot(pts[i], planeV);
                angles[i] = Mathf.Atan2(v, u);
            }

            int[] sorted = new int[pts.Count];
            for (int i = 0; i < sorted.Length; i++) sorted[i] = i;
            System.Array.Sort(angles, sorted);

            float maxGap = 0f;
            int gapEnd = 0;
            for (int i = 0; i < sorted.Length; i++)
            {
                int next = (i + 1) % sorted.Length;
                float gap = next == 0
                    ? (angles[0] + 2f * Mathf.PI) - angles[sorted.Length - 1]
                    : angles[next] - angles[i];
                if (gap > maxGap)
                {
                    maxGap = gap;
                    gapEnd = next;
                }
            }

            int idx1 = sorted[(gapEnd == 0) ? sorted.Length - 1 : gapEnd - 1];
            int idx2 = sorted[gapEnd];
            bc1 = pts[idx1];
            bc2 = pts[idx2];
        }
    }
}
