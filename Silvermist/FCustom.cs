﻿using UnityEngine;

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
            Matrix2x2 matrix = new Matrix2x2(Mathf.Cos(r), Mathf.Sin(r) * ((ang > 0) ? -1 : 1), Mathf.Sin(r) * ((ang > 0) ? 1 : -1), Mathf.Cos(r));
            return matrix * vector;
        }

        public struct Matrix2x2
        {
            public Vector2 upper, lower;
            private readonly float _a, _b, _c, _d;
            public float A => _a;
            public float B => _b;
            public float C => _c;
            public float D => _d;

            public Matrix2x2(Vector2 upper, Vector2 lower) : this(upper.x, upper.y, lower.x, lower.y)
            {
            }
            public Matrix2x2(float a, float b, float c, float d)
            {
                (_a, _b, _c, _d) = (a, b, c, d);
                upper = new Vector2(a, b);
                lower = new Vector2(c, d);
            }

            public static Vector2 operator *(Matrix2x2 matrix, Vector2 vector) => new Vector2(vector.x * matrix.A + vector.y * matrix.B, vector.x * matrix.C + vector.y * matrix.D);
            public static Vector2 operator *(Vector2 vector, Matrix2x2 matrix) => new Vector2(vector.x * matrix.A + vector.y * matrix.B, vector.x * matrix.C + vector.y * matrix.D);
            public static Matrix2x2 operator *(Matrix2x2 matrix, float d) => new Matrix2x2(matrix.A * d, matrix.B * d, matrix.C * d, matrix.D * d);
            public static Matrix2x2 operator *(float d, Matrix2x2 matrix) => new Matrix2x2(matrix.A * d, matrix.B * d, matrix.C * d, matrix.D * d);
            public static Matrix2x2 operator *(Matrix2x2 m1, Matrix2x2 m2) => new Matrix2x2(m1.A * m2.A + m1.B * m2.C, m1.A * m2.B + m1.C * m2.D, m1.C * m2.A + m1.D * m2.C, m1.C * m2.B + m1.D * m2.D);
        }
    }
}
