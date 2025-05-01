using System;

namespace Rasterizer.Core;

public static class RasterizerLogic
{
    private const double Epsilon = 0.0001;
    public static double[] Interpolate(double i0, double d0, double i1, double d1)
    {
        if (EpsilonCompare(i0,i1)) { return [d0]; }
        double[] values = new double[(int)double.Round(i1 - i0+1)];
        double a = (d1 - d0) / (i1 - i0);
        double d = d0;
        for (double i = i0; i <= i1; i++)
        {
            values[(int)double.Round(i - i0)] = d;
            d += a;
        }
        return values;
    }

    public static bool EpsilonCompare(double A, double B)
    {
        return Math.Abs(A - B) < Epsilon;
    }
}