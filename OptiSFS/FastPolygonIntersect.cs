using SFS;

namespace OptiSFS
{
    public static class FastPolygon
    {
        public static bool Intersect(ConvexPolygon[] a, ConvexPolygon[] b, float overlapThreshold)
        {
            if (a.Length == 0 || b.Length == 0) return false;
            
            Circle[] bCache = new Circle[b.Length];
            
            for (int i = 0; i < a.Length; i++)
            {
                var circleA = Circle.CreateFrom(a[i]);
                for (int j = 0; j < b.Length; j++)
                {
                    if (i == 0) // first pass, cache needs to be built
                        bCache[j] = Circle.CreateFrom(b[j]);
                    
                    if (circleA.Intersect(bCache[j]))
                    {
                        if (ConvexPolygon.Intersect(a[i], b[j], overlapThreshold)) 
                            return true;
                    }
                }
            }

            return false;
        }
    }
}