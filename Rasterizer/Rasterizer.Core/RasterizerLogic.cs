using System;
using MathNet.Numerics.LinearAlgebra;
using Microsoft.Xna.Framework;

namespace Rasterizer.Core;

public static class RasterizerLogic
{
    private const double Epsilon = 0.0001;
    public static double[] Interpolate(double i0, double d0, double i1, double d1)
    {
        int o0 = (int)Math.Floor(i0);
        int o1 = (int)Math.Ceiling(i1);
        if (EpsilonCompare(i0,i1)) { return [d0]; }
        double[] values = new double[Math.Abs(o1 - o0)+1];
        double a = (d1 - d0) / (i1 - i0);
        double d = d0;
        for (int i = o0; i <= o1; i++)
        {
            values[i - o0] = d;
            d += a;
        }

        values[^1] = d1;
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
        Vector3 b_a = new Vector3(b.X-a.X, b.Y-a.Y, b.Z-a.Z);
        float t = (-plane.D - Vector3.Dot(plane.Normal, a))/Vector3.Dot(plane.Normal, b_a);
        Vector3 result = new Vector3(a.X+t*(b.X-a.X), a.Y+t*(b.Y-a.Y), a.Z+t*(b.Z-a.Z));
        return new Vector4(result, 1);
    }
    public static Vector3 ComputeNormal(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        // Calculate the two edge vectors
        Vector3 edge1 = v2 - v1;
        Vector3 edge2 = v3 - v1;

        // Calculate the cross product of the two edges
        Vector3 crossProduct = Vector3.Cross(edge1, edge2);

        // Normalize the resulting vector to ensure it is a unit vector
        Vector3 normal = Vector3.Normalize(crossProduct);

        return normal;
    }
    public static Color ScaleColor(Color color, float intensity)
    {
        // Ensure intensity is clamped between 0 and 1
        intensity = MathHelper.Clamp(intensity, 0.0f, 1.0f);

        // Scale each component by the intensity
        byte r = (byte)(color.R * intensity);
        byte g = (byte)(color.G * intensity);
        byte b = (byte)(color.B * intensity);
        byte a = color.A; // You might or might not want to scale alpha

        return new Color(r, g, b, a);
    }
}