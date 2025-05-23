using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

namespace Rasterizer.Core;

public class Instance(Vector4 pos, Quaternion rot, double scale, Model model, Color c, double specular)
{
    public Model Model = model;
    public Vector4 Pos = pos;
    public Quaternion Rot = rot;
    public double Scale = scale;
    public Color C = c;
    public double Specular = specular;
    
    public double getRadius()
    {
        return Model.Radius * Scale;
    }
}