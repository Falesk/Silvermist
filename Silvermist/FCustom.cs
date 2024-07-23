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

        public static Vector2 RotateVector(Vector2 vector, float ang)
        {
            float r = ang * Mathf.PI / 180f;
            Matrix2x2 matrix = new Matrix2x2(
                Mathf.Cos(r), -Mathf.Sin(r),
                Mathf.Sin(r), Mathf.Cos(r));
            return matrix * vector;
        }

        /// <summary>
        /// A method for determining the Bezier curve in 2 dimensions
        /// </summary>
        /// <param name="segments">The number of Vector2 points to return</param>
        /// <param name="length">The length of the base line in pixels</param>
        /// <param name="point">The position of the third point relative to the zero coordinates</param>
        /// <returns>An array of curve points</returns>
        public static Vector2[] BezierCurve(int segments, float length, Vector2 Tpoint, Vector2? P2 = null)
        {
            Vector2[] points = new Vector2[segments];
            Vector2 P1 = Vector2.zero;
            P2 = P2 ?? new Vector2(length, 0f);
            for (int i = 0; i < segments; i++)
            {
                float val = (i + 1) / (float)segments;
                Vector2 line = Vector2.Lerp(Tpoint, P2.Value, val) - Vector2.Lerp(P1, Tpoint, val);
                points[i] = line * val + Vector2.Lerp(P1, Tpoint, val);
            }
            return points;
        }

        /// <summary>
        /// Bezier curve with 3 regulation points
        /// </summary>
        public static Vector2[] BezierCurve(int segments, float length, Vector2 P1, Vector2 P2, Vector2? P3 = null)
        {
            Vector2[] points = new Vector2[segments];
            Vector2 P0 = Vector2.zero;
            P3 = P3 ?? new Vector2(length, 0f);
            for (int i = 0; i < segments; i++)
            {
                float val = (i + 1) / (float)segments;
                Vector2 Q0 = Vector2.Lerp(P0, P1, val);
                Vector2 Q1 = Vector2.Lerp(P1, P2, val);
                Vector2 Q2 = Vector2.Lerp(P2, P3.Value, val);
                Vector2 R0 = Vector2.Lerp(Q0, Q1, val);
                Vector2 R1 = Vector2.Lerp(Q1, Q2, val);
                points[i] = Vector2.Lerp(R0, R1, val);
            }
            return points;
        }

        /// <summary>
        /// A method for determining the coordinates of the intersection point of two vectors
        /// </summary>
        /// <param name="A">First vector</param>
        /// <param name="B">Second Vector</param>
        /// <param name="ABeg">The starting point of the first vector</param>
        /// <param name="BBeg">The starting point of the second vector</param>
        /// <returns>The coordinates of the intersection point; the zero of the coordinates is determined through the starting points</returns>
        public static Vector2 IntersectionPoint(Vector2 A, Vector2 B, Vector2 ABeg, Vector2 BBeg)
        {
            float ratio = (ABeg - BBeg).magnitude / Mathf.Sin(Vector2.Angle(A, B) * Mathf.PI / 180f);
            float sinB = Mathf.Sin(Vector2.Angle(BBeg - ABeg, A) * Mathf.PI / 180f);
            float lenB = ratio * sinB;
            return BBeg + lenB * B.normalized;
        }

        public struct Matrix2x2
        {
            public Vector2 left, right; 
            private readonly float _a, _b, _c, _d;
            public float A => _a;
            public float B => _b;
            public float C => _c;
            public float D => _d;

            public Matrix2x2(Vector2 left, Vector2 right) : this(left.x, right.y, left.x, right.y)
            {
            }
            public Matrix2x2(float a, float b, float c, float d)
            {
                (_a, _b, _c, _d) = (a, b, c, d);
                left = new Vector2(a, c);
                right = new Vector2(b, d);
            }

            public override bool Equals(object obj) => obj is Matrix2x2 m && m == this;
            public override string ToString() => $"|{A} {B}|\n|{C} {D}|";
            public override int GetHashCode() => base.GetHashCode();

            public static Vector2 operator *(Matrix2x2 matrix, Vector2 vector) => new Vector2(vector.x * matrix.A + vector.y * matrix.B, vector.x * matrix.C + vector.y * matrix.D);
            public static Vector2 operator *(Vector2 vector, Matrix2x2 matrix) => matrix * vector;
            public static Matrix2x2 operator *(Matrix2x2 matrix, float d) => new Matrix2x2(matrix.A * d, matrix.B * d, matrix.C * d, matrix.D * d);
            public static Matrix2x2 operator *(float d, Matrix2x2 matrix) => matrix * d;
            public static Matrix2x2 operator *(Matrix2x2 m1, Matrix2x2 m2) => new Matrix2x2(m1.A * m2.A + m1.B * m2.C, m1.A * m2.B + m1.C * m2.D, m1.C * m2.A + m1.D * m2.C, m1.C * m2.B + m1.D * m2.D);
            public static Matrix2x2 operator +(Matrix2x2 m1, Matrix2x2 m2) => new Matrix2x2(m1.A + m2.A, m1.B + m2.B, m1.C + m2.C, m1.D + m2.D);
            public static Matrix2x2 operator -(Matrix2x2 m1, Matrix2x2 m2) => new Matrix2x2(m1.A - m2.A, m1.B - m2.B, m1.C - m2.C, m1.D - m2.D);
            public static Matrix2x2 operator /(Matrix2x2 matrix, float d) => new Matrix2x2(matrix.A / d, matrix.B / d, matrix.C / d, matrix.D / d);
            public static bool operator ==(Matrix2x2 m1, Matrix2x2 m2) => m1.A == m2.A && m1.B == m2.B && m1.C == m2.C && m1.D == m2.D;
            public static bool operator !=(Matrix2x2 m1, Matrix2x2 m2) => !(m1 == m2);
        }
    }
}
