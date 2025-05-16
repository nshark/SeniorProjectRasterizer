using System;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Xna.Framework;

namespace Rasterizer.Core;

public static class RasterizerLogic
{
    public static Matrix<double> QuaternionToRotationMatrix(Quaternion q)
    {
        // Extract the components of the quaternion
        double w = q.W;
        double x = q.X;
        double y = q.Y;
        double z = q.Z;

        // Pre-calculate repeated products
        double xx = x * x,   xy = x * y,   xz = x * z,   xw = x * w;
        double yy = y * y,   yz = y * z,   yw = y * w;
        double zz = z * z,   zw = z * w;

        // Create a 3×3 matrix
        var M = Matrix<double>.Build.Dense(3, 3);

        // Fill in elements according to the standard quaternion→rotation formula
        M[0,0] = 1.0 - 2.0 * (yy + zz);
        M[0,1] = 2.0 * (xy - zw);
        M[0,2] = 2.0 * (xz + yw);

        M[1,0] = 2.0 * (xy + zw);
        M[1,1] = 1.0 - 2.0 * (xx + zz);
        M[1,2] = 2.0 * (yz - xw);

        M[2,0] = 2.0 * (xz - yw);
        M[2,1] = 2.0 * (yz + xw);
        M[2,2] = 1.0 - 2.0 * (xx + yy);

        return M;
    }
    private const double Epsilon = 0.0001;
    public static double[] Interpolate(double i0, double d0, double i1, double d1)
    {
        int o0 = (int)Math.Floor(i0);
        int o1 = (int)Math.Floor(i1);
        if (o1 == o0) { return [d0]; }
        double[] values = new double[o1 - o0+1];
        double a = (d1 - d0) / (i1 - i0);
        double d = d0;
        for (int i = o0; i < o1+1; i++)
        {
            values[i - o0] = d;
            d += a;
        }
        return values;
    }

    public static bool EpsilonCompare(double a, double b)
    {
        return Math.Abs(a - b) < Epsilon;
    }

    public static double SignedDistance(Vector4 v, Plane plane)
    {
        return v.X * plane.Normal.X + v.Y * plane.Normal.Y + v.Z * plane.Normal.Z + plane.D;
    }

    public static Vector4 Intersect(Vector3 a, Vector3 b, Plane plane)
    {
        float t = (-plane.D - Vector3.Dot(plane.Normal, a))/Vector3.Dot(plane.Normal, b - a);
        Vector3 b_a = new Vector3(a.X+t*(b.X-a.X), a.Y+t*(b.Y-a.Y), a.Z+t*(b.Z-a.Z));
        return new Vector4(b_a, 1);
    }
}