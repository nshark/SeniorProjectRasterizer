using Microsoft.Xna.Framework;

namespace Rasterizer.Core;

public class DirectionalLight : Light
{
    private Vector3 _direction;
    public DirectionalLight(double intensity, Vector3 direction) : base(intensity)
    {
        _direction = direction;
    }
}