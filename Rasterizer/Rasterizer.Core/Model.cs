using Microsoft.Xna.Framework;

namespace Rasterizer.Core;

public class Model
{
    public Vector4[] Vertices;
    public int[][] Triangles;
    public double Radius;
    public Model(Vector4[] vertices, int[][] triangles, double radius)
    {
        this.Vertices = vertices;
        this.Triangles = triangles;
        this.Radius = radius;
    }
}