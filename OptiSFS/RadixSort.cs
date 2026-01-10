using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using SFS.World.Drag;
using UnityEngine;

namespace OptiSFS
{
    public static class SurfaceEndXRadixSort
    {
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
        struct FloatUIntUnion
        {
            [System.Runtime.InteropServices.FieldOffset(0)]
            public float FloatValue;

            [System.Runtime.InteropServices.FieldOffset(0)]
            public uint UIntValue;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint FloatToSortableUint(float f)
        {
            var u = new FloatUIntUnion { FloatValue = f };
            uint bits = u.UIntValue;
            return (bits & 0x80000000) != 0 ? ~bits : bits ^ 0x80000000;
        }
        
        public static Surface[] Sort(List<Surface> arr)
        {
            int n = arr.Count;
            uint[] keys = new uint[n];
            for (int i = 0; i < n; i++)
            {
                //uint bits = BitConverter.ToUInt32(BitConverter.GetBytes(arr[i].line.end.x), 0);
                //keys[i] = (bits & 0x80000000) != 0 ? ~bits : bits ^ 0x80000000; // This makes positive zero less than negative zero, but should be fine

                keys[i] = FloatToSortableUint(arr[i].line.end.x);
            }

            var a = arr.ToArray();
            RadixSort(keys, a);
            return a;
        }

        public static bool Test()
        {
            List<Surface> test = new List<Surface>()
            {
                new Surface()
                {
                    line = new Line2(Vector2.zero, Vector2.right * 2f)
                },
                new Surface()
                {
                    line = new Line2(Vector2.zero, Vector2.right * 1f)
                },
                new Surface()
                {
                    line = new Line2(Vector2.zero, Vector2.right * 3f)
                },
                new Surface()
                {
                    line = new Line2(Vector2.zero, Vector2.right * 7f)
                },
                new Surface()
                {
                    line = new Line2(Vector2.zero, Vector2.right * -5f)
                },
                new Surface()
                {
                    line = new Line2(Vector2.zero, Vector2.right * 0f)
                },
                new Surface()
                {
                    line = new Line2(Vector2.zero, Vector2.right * -80f)
                },
            };

            int count = test.Count;
            
            var arr = Sort(test);
            
            float max = float.NegativeInfinity;

            if (count != arr.Length)
                return false;
            
            for (int i = 0; i < count; i++)
            {
                if (arr[i].line.end.x < max) return false; // Isn't the new highest?
                max = arr[i].line.end.x;
            }

            return true;
        }

        private static void RadixSort(uint[] keys, Surface[] values)
        {
            int n = keys.Length;
            uint[] auxKeys = new uint[n];
            Surface[] auxVals = new Surface[n];

            const int BITS = 32;
            const int RADIX = 8;
            const int BUCKETS = 1 << RADIX;
            const uint mask = BUCKETS - 1;
            
            int[] count = new int[BUCKETS];
            
            for (int shift = 0; shift < BITS; shift += RADIX)
            {
                Array.Clear(count, 0, BUCKETS);
                
                for (int i = 0; i < n; i++)
                    count[(int)((keys[i] >> shift) & mask)]++;

                for (int i = 1; i < BUCKETS; i++)
                    count[i] += count[i - 1];

                for (int i = n - 1; i >= 0; i--)
                {
                    int bucket = (int)((keys[i] >> shift) & mask);
                    int pos = --count[bucket];
                    auxKeys[pos] = keys[i];
                    auxVals[pos] = values[i];
                }

                (keys, auxKeys) = (auxKeys, keys);
                (values, auxVals) = (auxVals, values);
            }
            
            Array.Copy(keys, auxKeys, n);
            Array.Copy(values, auxVals, n);
        }
    }
}