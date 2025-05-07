using System;

namespace Rasterizer.Core;

public static class RasterizerLogic
{
    
    private const double Epsilon = 0.0001;
    public static double[] Interpolate(double i0, double d0, double i1, double d1)
    {
        int o0 = (int)Math.Floor(i0);
        int o1 = (int)Math.Floor(i1);
        if (o1 == o0) { return [d0]; }
        double[] values = new double[o1 - o0+1];
        double a = (d1 - d0) / (i1 - i0);
        double d = d0;
        for (int i = o0; i < o1; i++)
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
}