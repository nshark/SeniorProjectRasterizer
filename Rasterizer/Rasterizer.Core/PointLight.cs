using Microsoft.Xna.Framework;

namespace Rasterizer.Core;

public class PointLight : Light
{
    Vector3 position;
    public PointLight(double intensity, Vector3 point) : base(intensity)
    {
        position = point;
    }
    
}