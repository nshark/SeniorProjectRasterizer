using Microsoft.Xna.Framework;

namespace Rasterizer.Core;

public class Model(Vector4[] vertices, int[][] triangles, double radius)
{
    public Vector4[] Vertices = vertices;
    public int[][] Triangles = triangles;
    public double Radius = radius;
}