using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;

namespace Rasterizer.Core;

public class Instance
{
    public Model Model;
    public Vector4 Pos;
    public Quaternion Rot;
    public double Scale;
    public Color C;

    public Instance(Vector4 pos, Quaternion rot, double scale, Model model, Color c)
    {
        this.Model = model;
        this.Pos = pos;
        this.Rot = rot;
        this.Scale = scale;
        this.C = c;
    }

    public double getRadius()
    {
        return Model.Radius * Scale;
    }
}