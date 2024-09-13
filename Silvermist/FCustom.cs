using System;
using UnityEngine;

namespace Silvermist
{
    public static class FCustom
    {
        public static Vector2 TrimmedAnchors(FAtlasElement element)
        {
            Vector2 anchors = element.sourceRect.center / element.sourceSize;
            anchors.y = 1f - anchors.y;
            return anchors;
        }

        public static Vector2 RotateVector(Vector2 v, float ang)
        {
            float r = ang * Mathf.PI / 180f;
            return new Vector2(
                v.x * Mathf.Cos(r) - v.y * Mathf.Sin(r),
                v.x * Mathf.Sin(r) + v.y * Mathf.Cos(r));
        }

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

        public static Vector3 MinZ(this Vector3[] vs)
        {
            float min = float.MaxValue;
            int ind = -1;
            for (int i = 0; i < vs.Length; i++)
            {
                if (vs[i].z < min)
                {
                    min = vs[i].z;
                    ind = i;
                }
            }
            return vs[ind];
        }
        public static Vector3 MaxZ(this Vector3[] vs)
        {
            float max = float.MinValue;
            int ind = -1;
            for (int i = 0; i < vs.Length; i++)
            {
                if (vs[i].z > max)
                {
                    max = vs[i].z;
                    ind = i;
                }
            }
            return vs[ind];
        }

        public static Quaternion Сonjugate(this Quaternion q) => new Quaternion(-q.x, -q.y, -q.z, q.w);
        public static Quaternion ToQuaternion(this Vector3 v) => new Quaternion(v.x, v.y, v.z, 0f);
        public static Vector3 ToVector3(this Quaternion q) => new Vector3(q.x, q.y, q.z);
    }
}
