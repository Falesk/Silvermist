using System;
using UnityEngine;

namespace Silvermist
{
    public static class FCustom
    {
        public const float FI = 0.618034f;
        public const float gAngle = 2f * Mathf.PI * (1f - FI);

        public static Vector2 TrimmedAnchors(FAtlasElement element)
        {
            Vector2 anchors = element.sourceRect.center / element.sourceSize;
            anchors.y = 1f - anchors.y;
            return anchors;
        }

        public static Vector2 RotateVector(Vector2 v, float ang)
        {
            float r = ang * Mathf.PI / 180f;
            float cos = Mathf.Cos(r), sin = Mathf.Sin(r);
            return new Vector2(
                v.x * cos - v.y * sin,
                v.x * sin + v.y * cos);
        }

        public static float AngleX(Vector2 a) => (a.y > 0f ? 1f : -1f) * Mathf.Acos(a.x / a.magnitude) * 180f / Mathf.PI;

        public static Vector2[] BezierCurve(int segments, params Vector2[] Ps)
        {
            Vector2[] curvePoints = new Vector2[segments];
            Vector2[] Points = new Vector2[Ps.Length + 1];
            Array.Copy(Ps, 0, Points, 1, Ps.Length);
            Points[0] = Vector2.zero;
            for (int i = 0; i < segments; i++)
            {
                float t = (i + 1) / (float)segments;
                curvePoints[i] = BezierT(t, Points);
            }
            return curvePoints;
        }
        private static Vector2 BezierT(float t, params Vector2[] Ps)
        {
            Vector2[] pointsNext = new Vector2[Ps.Length - 1];
            for (int i = 0; i < Ps.Length - 1; i++)
                pointsNext[i] = Vector2.Lerp(Ps[i], Ps[i + 1], t);
            if (pointsNext.Length == 1)
                return pointsNext[0];
            return BezierT(t, pointsNext);
        }

        public static Vector3[,] ReverseIfNecessary(Vector3[,] vs, float ang = 0)
        {
            if (ang < Mathf.PI)
                for (int i = 0; i < vs.GetLength(0) / 2; i++)
                    for (int j = 0; j < vs.GetLength(1); j++)
                        (vs[i, j], vs[vs.GetLength(0) - 1 - i, j]) = (vs[vs.GetLength(0) - 1 - i, j], vs[i, j]);
            if (Mathf.Min(vs[0, 0].z, vs[0, 1].z, vs[0, 2].z, vs[0, 3].z) > Mathf.Min(vs[1, 0].z, vs[1, 1].z, vs[1, 2].z, vs[1, 3].z))
                for (int i = 0; i < vs.GetLength(0) - 1; i += 2)
                    for (int j = 0; j < vs.GetLength(1); j++)
                        (vs[i, j], vs[i + 1, j]) = (vs[i + 1, j], vs[i, j]);
            return vs;
        }

        public static Vector3 CrossProduct(Vector3 v1, Vector3 v2)
        {
            float x = v1.y * v2.z - v1.z * v2.y;
            float y = v1.z * v2.x - v1.x * v2.z;
            float z = v1.x * v2.y - v1.y * v2.x;
            return new Vector3(x, y, z);
        }

        public static Quaternion Сonjugate(this Quaternion q) => new (-q.x, -q.y, -q.z, q.w);
        public static Quaternion ToQuaternion(this Vector3 v) => new (v.x, v.y, v.z, 0f);
        public static Vector3 ToVector3(this Quaternion q) => new (q.x, q.y, q.z);
    }
}
