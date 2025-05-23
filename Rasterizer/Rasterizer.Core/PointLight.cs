using System;
using Microsoft.Xna.Framework;

namespace Rasterizer.Core;

public class PointLight : Light
{
    Vector3 _position;
    public PointLight(double intensity, Vector3 point) : base(intensity)
    {
        _position = point;
    }

    public override double computeLightingOnPoint(Vector3 p, Vector3 normal, double specular, Vector4 cameraPos,Matrix cameraMatrix)
    {
        var position = Vector4.Transform(_position, cameraMatrix);
        Vector3 direction = new Vector3(position.X-p.X, position.Y-p.Y, position.Z-p.Z);
        Vector3 v = new Vector3(p.X - cameraPos.X, p.Y - cameraPos.Y, p.Z - cameraPos.Z);
        float nDotL = Vector3.Dot(direction, normal);
        double i = 0;
        if (nDotL > 0)
        {
            i += Intensity * nDotL/(normal.Length() * direction.Length());
        }

        if (RasterizerLogic.EpsilonCompare(specular, -1))
        {
            Vector3 r = Vector3.Reflect(direction, normal);
            float rDotV = Vector3.Dot(r, v);
            if (rDotV > 0)
            {
                i += Intensity * Math.Pow(rDotV / (r.Length() * v.Length()), specular);
            }
        }

        return i;
    }
}