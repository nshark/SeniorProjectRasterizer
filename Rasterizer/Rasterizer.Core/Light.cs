using Microsoft.Xna.Framework;

namespace Rasterizer.Core;

public class Light
{
    public double Intensity;

    public Light(double intensity)
    {
        Intensity = intensity;
    }
    public virtual double computeLightingOnPoint(Vector3 p, Vector3 normal, double specular, Vector4 cameraPos, Matrix cameraMatrix)
    {
        return Intensity;
    }
}