using SFS;
using UnityEngine;

namespace OptiSFS
{
    public struct Circle
    {
        public Vector2 center;
        public float radius;

        public Circle(Vector2 center, float radius)
        {
            this.center = center;
            this.radius = radius;
        }
        
        public bool Intersect(Circle b)
        {
            float dx = center.x - b.center.x;
            float dy = center.y - b.center.y;

            float r = radius + b.radius;
            return dx * dx + dy * dy <= r * r;
        }
        
        public static Circle CreateFrom(ConvexPolygon poly)
        {
            Vector2 center = Vector2.zero;
            float radiusSq = 0f;
            var pts = poly.points;
            int n = pts.Length;
            
            for (int i = 0; i < n; i++)
            {
                center += pts[i];
            }
            center /= n;

            for (int i = 0; i < n; i++)
            {
                float dx = pts[i].x - center.x;
                float dy = pts[i].y - center.y;
                float newRsq = dx * dx + dy * dy;
                
                if (newRsq > radiusSq)
                    radiusSq = newRsq;
            }
            
            return new Circle(center, Mathf.Sqrt(radiusSq));
        }
    }
}