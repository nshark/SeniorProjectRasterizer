using System;
using Microsoft.Xna.Framework;

namespace Rasterizer.Core;

public class DirectionalLight : Light
{
    private Vector3 _direction;
    public DirectionalLight(double intensity, Vector3 direction) : base(intensity)
    {
        _direction = direction;
    }

    override public double computeLightingOnPoint(Vector3 p, Vector3 normal, double specular, Vector4 cameraPos,Matrix cameraMatrix)
    {
        Vector3 v = new Vector3(p.X - cameraPos.X, p.Y - cameraPos.Y, p.Z - cameraPos.Z);
        float nDotL = Vector3.Dot(_direction, normal);
        double i = 0;
        if (nDotL > 0)
        {
            i += Intensity * nDotL/(normal.Length() * _direction.Length());
        }

        if (RasterizerLogic.EpsilonCompare(specular, -1))
        {
            Vector3 r = Vector3.Reflect(_direction, normal);
            float rDotV = Vector3.Dot(r, v);
            if (rDotV > 0)
            {
                i += Intensity * Math.Pow(rDotV / (r.Length() * v.Length()), specular);
            }
        }

        return i;
    }
}