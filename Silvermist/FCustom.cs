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

        public static Vector2[] BezierCurve(int segments, float length, Vector2 P1, Vector2 P2) => BezierCurve(segments, P1, P2, new Vector2(length, 0f));
        public static Vector2[] BezierCurve(int segments, Vector2 P1, Vector2 P2, Vector2 P3)
        {
            Vector2[] points = new Vector2[segments];
            Vector2 P0 = Vector2.zero;
            for (int i = 0; i < segments; i++)
            {
                float val = (i + 1) / (float)segments;
                Vector2 Q0 = Vector2.Lerp(P0, P1, val);
                Vector2 Q1 = Vector2.Lerp(P1, P2, val);
                Vector2 Q2 = Vector2.Lerp(P2, P3, val);
                Vector2 R0 = Vector2.Lerp(Q0, Q1, val);
                Vector2 R1 = Vector2.Lerp(Q1, Q2, val);
                points[i] = Vector2.Lerp(R0, R1, val);
            }
            return points;
        }
    }
}
