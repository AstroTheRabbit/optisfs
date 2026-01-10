using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace OptiSFS
{
    public static class FastGL
    {
        public static void Batched_DrawLines(List<Vector3> starts, List<Vector3> ends, float width, Color color,
            float sortingOrder)
        {
            bool treat = Entrypoint.TreatmentGroup;
            
            GL.Begin(GL.QUADS);
            
            new Traverse(GLDrawer.main).Method("GetMaterial", sortingOrder).GetValue<Material>().SetPass(0);
            
            if (starts.Count != ends.Count) throw new ArgumentException("Start and end points do not match.");
            
            GL.Color(color);
            
            float w = width * 0.5f;
            
            Rect worldRect = Utility.GetCameraBounds();
            worldRect.xMin -= w;
            worldRect.yMin -= w;
            worldRect.xMax += w;
            worldRect.yMax += w;
            
            Vector2[] corners = Array.Empty<Vector2>();
            
            for (int i = 0; i < starts.Count; i++)
            {
                var start  = starts[i];
                var end = ends[i];

                if (!treat && !LineIntersectsRect(start, end, worldRect, ref corners)) continue;
                //if (treat && )
                
                Vector3 vector = Vector2.Perpendicular((start - end).normalized) * width * 0.5f;
                GL.Vertex(start - vector);
                GL.Vertex(end - vector);
                GL.Vertex(end + vector);
                GL.Vertex(start + vector);
            }

            GL.End();
        }

        private static bool LineIntersectsRect(Vector3 p1, Vector3 p2, Rect r, ref Vector2[] corners)
        {
            Vector2 p1v = new Vector2(p1.x, p1.y);
            Vector2 p2v = new Vector2(p2.x, p2.y);
            if (r.Contains(p1v) || r.Contains(p2v)) return true;

            if (corners.Length == 0)
            {
                corners = new Vector2[4];
                corners[0] = new Vector2(r.xMin, r.yMin);
                corners[1] = new Vector2(r.xMax, r.yMin);
                corners[2] = new Vector2(r.xMax, r.yMax);
                corners[3] = new Vector2(r.xMin, r.yMax);
            }

            // Check intersection with each edge

            for (int i = 0; i < 4; i++)
            {
                Vector2 a = corners[i];
                Vector2 b = corners[(i + 1) % 4];
                if (LinesIntersect(p1, p2, a, b)) return true;
            }

            return false;
        }

        private static bool LinesIntersect(Vector2 A, Vector2 B, Vector2 C, Vector2 D)
        {
            return ccw(A, C, D) != ccw(B, C, D) && ccw(A, B, C) != ccw(A, B, D);
            
            bool ccw(Vector2 a, Vector2 b, Vector2 c)
            {
                return (c.y - a.y) * (b.x - a.x) > (b.y - a.y) * (c.x - a.x);
            }
        }

        public static void Batched_DrawCircles(
            List<Vector3> positions,
            float radius,
            int resolution,
            Color color,
            float sortingOrder)
        {
            if (positions == null || positions.Count == 0) return;

            var mat = new Traverse(GLDrawer.main)
                .Method("GetMaterial", sortingOrder)
                .GetValue<Material>();
            mat.SetPass(0);

            GL.Begin(GL.TRIANGLES);
            GL.Color(color);
            
            Vector3[] verts = new Vector3[resolution];
            float step = Mathf.PI * 2f / resolution;
            for (int i = 0; i < resolution; i++)
                verts[i] = new Vector3(Mathf.Cos(i * step), Mathf.Sin(i * step), 0f) * radius;

            Rect worldRect = Utility.GetCameraBounds();
            worldRect.xMin -= radius;
            worldRect.yMin -= radius;
            worldRect.xMax += radius;
            worldRect.yMax += radius;

            foreach (var pos in positions)
            {
                if (!worldRect.Contains(pos)) continue;
                
                for (int i = 0; i < resolution; i++)
                {
                    Vector3 v0 = pos;
                    Vector3 v1 = pos + verts[i];
                    Vector3 v2 = pos + verts[(i + 1) % resolution];

                    GL.Vertex(v0);
                    GL.Vertex(v2);
                    GL.Vertex(v1);
                }
            }

            GL.End();
        }
    }
}