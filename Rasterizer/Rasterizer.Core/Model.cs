using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Rasterizer.Core;

public class Model(Vector4[] vertices, int[][] triangles, double radius)
{
    public Vector4[] Vertices = vertices;
    public int[][] Triangles = triangles;
    public double Radius = radius;

    public static Model FromOBJ(string path)
    {
        var fileStream = File.Open(path, FileMode.Open, FileAccess.Read);
        var file = new StreamReader(fileStream);
        IList<Vector4> vertices = new List<Vector4>();
        IList<int[]> triangles = new List<int[]>();
        string lineOfText;
        while ((lineOfText = file.ReadLine()) != null)
        {
            if (lineOfText == "")
            {
                continue;
            }
            if (lineOfText.First() == '#')
            {
                continue;
            }

            if (lineOfText.StartsWith("v "))
            {
                string[] splits = lineOfText.Split(" ");
                vertices.Add(new Vector4(float.Parse(splits[1]), float.Parse(splits[2]), float.Parse(splits[3]), 1));
            }

            if (lineOfText.StartsWith("f "))
            {
                string[] splits = lineOfText.Split(" ");
                int[] poly = new int[splits.Length - 1];
                for (int i = 0; i < poly.Length; i++)
                {
                    poly[i] = int.Parse(splits[i + 1].Split("/")[0]) - 1;
                }
                triangles.Add(poly);
            }
            
        }
        return new Model(vertices.ToArray(), triangles.ToArray(), 100);
    }
        
        
    }
