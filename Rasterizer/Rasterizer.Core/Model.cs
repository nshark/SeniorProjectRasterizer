using Microsoft.Xna.Framework;

namespace Rasterizer.Core;

public class Model
{
    public Vector4[] Vertices;
    public int[][] Triangles;
    public Model(Vector4[] vertices, int[][] triangles)
    {
        this.Vertices = vertices;
        this.Triangles = triangles;
    }
}