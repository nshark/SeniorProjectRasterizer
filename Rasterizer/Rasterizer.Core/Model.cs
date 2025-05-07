using Microsoft.Xna.Framework;

namespace Rasterizer.Core;

public class Model
{
    public Vector3[] Vertices;
    public int[][] Triangles;
    public Model(Vector3[] vertices, int[][] triangles)
    {
        this.Vertices = vertices;
        this.Triangles = triangles;
    }
}