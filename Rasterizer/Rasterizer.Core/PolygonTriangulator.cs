using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Rasterizer.Core;

public static class PolygonTriangulator
{
    /// <summary>
    /// Triangulate a simple polygon.
    /// 
    /// vertices      – world or model-space points (only X,Y are used)
    /// polyIndices   – indices that walk once around the polygon
    /// 
    /// Result: List<int[3]>  (each int[ ] is {i0,i1,i2})
    /// </summary>
    public static List<int[]> Triangulate(IList<Vector4> vertices, IList<int> polyIndices)
    {
        if (vertices   == null) throw new ArgumentNullException(nameof(vertices));
        if (polyIndices == null) throw new ArgumentNullException(nameof(polyIndices));

        /* ---- build a clean working copy of the ring ------------------- */
        var loop = new List<int>(polyIndices.Count);
        foreach (var i in polyIndices)
        {
            if (loop.Count == 0 || i != loop[0] || loop.Count == 1)
                loop.Add(i);
        }
        if (loop.Count < 3)
            throw new ArgumentException("Polygon must have at least three distinct vertices");

        var remaining = new List<int>(loop);
        var triangles = new List<int[]>( (remaining.Count - 2) );

        bool ccw = SignedArea(remaining, vertices) > 0f;

        /* ---------------- main ear-clipping loop ----------------------- */
        int watchdog = 0;
        while (remaining.Count > 3 && watchdog++ < 2048)
        {
            bool earFound = false;

            for (int i = 0; i < remaining.Count; ++i)
            {
                int ia = remaining[ (i - 1 + remaining.Count) % remaining.Count ];
                int ib = remaining[i];
                int ic = remaining[ (i + 1) % remaining.Count ];

                Vector2 a = XY(vertices[ia]);
                Vector2 b = XY(vertices[ib]);
                Vector2 c = XY(vertices[ic]);

                if (!IsConvex(a, b, c, ccw)) continue;

                bool containsOther = false;
                for (int j = 0; j < remaining.Count && !containsOther; ++j)
                {
                    int ip = remaining[j];
                    if (ip == ia || ip == ib || ip == ic) continue;

                    if (PointInTriangle(a, b, c, XY(vertices[ip])))
                        containsOther = true;
                }
                if (containsOther) continue;

                /* --- clip this ear --- */
                triangles.Add(new[] { ia, ib, ic });
                remaining.RemoveAt(i);
                earFound = true;
                break;
            }

            if (!earFound)
                throw new InvalidOperationException(
                    "Could not find an ear – polygon is probably self-intersecting or degenerate");
        }

        /* last triangle */
        if (remaining.Count == 3)
            triangles.Add(new[] { remaining[0], remaining[1], remaining[2] });

        return triangles;
    }

    /* ================================================================== */
    /* helpers                                                            */
    /* ================================================================== */

    private static float SignedArea(List<int> poly, IList<Vector4> verts)
    {
        float a = 0f;
        for (int i = 0; i < poly.Count; ++i)
        {
            Vector2 p  = XY(verts[poly[i]]);
            Vector2 pn = XY(verts[poly[(i + 1) % poly.Count]]);
            a += p.X * pn.Y - pn.X * p.Y;
        }
        return 0.5f * a;
    }

    private static bool IsConvex(Vector2 a, Vector2 b, Vector2 c, bool wantCCW)
    {
        float cross = Cross(b - a, c - b);
        return wantCCW ? cross > 0f : cross < 0f;
    }

    private static float Cross(Vector2 u, Vector2 v) => u.X * v.Y - u.Y * v.X;

    private static bool PointInTriangle(Vector2 a, Vector2 b, Vector2 c, Vector2 p)
    {
        Vector2 v0 = c - a, v1 = b - a, v2 = p - a;

        float d00 = Vector2.Dot(v0, v0);
        float d01 = Vector2.Dot(v0, v1);
        float d11 = Vector2.Dot(v1, v1);
        float d20 = Vector2.Dot(v2, v0);
        float d21 = Vector2.Dot(v2, v1);
        float denom = d00 * d11 - d01 * d01;
        if (Math.Abs(denom) < 1e-8f) return false;

        float v = (d11 * d20 - d01 * d21) / denom;
        float w = (d00 * d21 - d01 * d20) / denom;
        float u = 1f - v - w;

        const float eps = 1e-6f;
        return u >= -eps && v >= -eps && w >= -eps;
    }

    private static Vector2 XY(in Vector4 v) => new Vector2(v.X, v.Y);
}