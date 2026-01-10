using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using SFS.World;
using HarmonyLib;
using SFS.Parts;
using SFS.Parts.Modules;
using SFS.World.Drag;
using UnityEngine;

using static System.Reflection.BindingFlags;
using Debug = System.Diagnostics.Debug;

namespace OptiSFS
{
    public static class Benchmark
    {
        private static BindingFlags Private => NonPublic;
        private static readonly Dictionary<MethodInfo, string> methods = new Dictionary<MethodInfo, string>()
        {
            // SFS.World.ElementDrawer.RegisterElement(int, Vector2, Vector2, TextMesh, bool)
            { typeof(ElementDrawer).GetMethod("RegisterElement", new[] { typeof(int), typeof(Vector2), typeof(Vector2), typeof(TextMesh), typeof(bool) }), "RegElem" },
            
            // SFS.World.Drag.AeroModule::SortDragSurfacesByEndX()
            // Moved to initializer
            
            { typeof(GLDrawer).GetMethod("OnPostRender", Private | Instance, null, Array.Empty<Type>(), null), "TotGL" },
            
            // SFS.World.Drag.Aero_Rocket::GetDragSurfaces(PartHolder, Matrix2x2)
            { typeof(Aero_Rocket).GetMethod(
                "GetDragSurfaces",
                Public | Static,
                null,
                new[] { typeof(PartHolder), typeof(Matrix2x2) },
                null
            ), "DragSurfs" },
            
            // SFS.World.Drag.AeroModule::RemoveHighSlopeSurfaces(List<Surface>, float)
            { typeof(AeroModule).GetMethod("RemoveHighSlopeSurfaces", Private | Static, null, new[] { typeof(List<Surface>), typeof(float) }, null), "Slope" },
            
            { typeof(DockingPortModule).GetMethod(
                "FixedUpdate",
                Private | Instance,
                null,
                Array.Empty<Type>(),
                null
            ), "PortFixed" },
            
            { typeof(Rocket).GetMethod(
                "FixedUpdate",
                Private | Instance,
                null,
                Array.Empty<Type>(),
                null
            ), "RocketFixed" },
            
            { typeof(FuelPipeModule).GetMethod(
                "FixedUpdate_FuelPipeFlow",
                Public | Static,
                null,
                new[]
                {
                    typeof(List<(ResourceModule[] froms, ResourceModule to)>)
                },
                null
            ), "PipeFixed" },
            
            { typeof(Rocket).GetMethod(
                "UpdateMass",
                Private | Instance,
                null,
                Array.Empty<Type>(),
                null
            ), "RocketMass" },
            
            { typeof(Rocket).GetMethod(
                "ApplyTorque",
                Private | Instance,
                null,
                Array.Empty<Type>(),
                null
            ), "Torque" },
            
            { typeof(Rocket).GetMethod(
                "Inject_Location",
                Private | Instance,
                null,
                Array.Empty<Type>(),
                null
            ), "LocInj" },
            
            { typeof(Rocket).GetMethod(
                "UpdateMapIconRotation",
                Private | Instance,
                null,
                Array.Empty<Type>(),
                null
            ), "IconRot" },
            
            { typeof(FastGL).GetMethod("Batched_DrawLines", Public | Static, null, new[] { typeof(List<Vector3>), typeof(List<Vector3>), typeof(float), typeof(Color), typeof(float) }, null), 
                "FastGL" },
            { typeof(FastGL).GetMethod("Batched_DrawCircles", Public | Static, null, new[] { typeof(List<Vector3>), typeof(float), typeof(int), typeof(Color), typeof(float) }, null), 
                "FastGL" },
        };
        
        public static void ApplyPatches()
        {
            Harmony benchmarkHarmony = new Harmony("moe.verdix.optisfs.bench");

            if (!Entrypoint.VersionHasRadixSort)
            {
                methods[typeof(AeroModule).GetMethod("SortDragSurfacesByEndX", Private | Static)] = "SortDrag";
                methods[
                        typeof(GLDrawer).GetMethod("DrawLine", Public | Static, null,
                            new[] { typeof(Vector3), typeof(Vector3), typeof(Color), typeof(float), typeof(float) },
                            null)]
                    = "VaniGL";

                methods[
                        typeof(GLDrawer).GetMethod("DrawCircles", Public | Static, null,
                            new[] { typeof(List<Vector2>), typeof(float), typeof(int), typeof(Color), typeof(float) },
                            null)]
                    = "VaniGL";
            }
            else
            {
                methods[
                        typeof(GLDrawer).GetMethod("DrawOutline", Public | Static, null,
                            new[] { typeof(Vector2[]), typeof(float), typeof(Color), typeof(float) },
                            null)]
                    = "DrawOut";
                methods[
                        typeof(GLDrawer).GetMethod("FlushLines", Private | Instance, null,
                            Array.Empty<Type>(),
                            null)]
                    = "FlushLin";
                methods[
                        typeof(GLDrawer).GetMethod("FlushCircles", Private | Instance, null,
                            Array.Empty<Type>(),
                            null)]
                    = "FlushCir";
                UnityEngine.Debug.Log("Patched VaniGL!");
            }
            
            foreach (var key in methods.Keys)
            {
                benchmarkHarmony.Patch(key, new HarmonyMethod(typeof(Benchmark).GetMethod(nameof(Prefix))), new HarmonyMethod(typeof(Benchmark).GetMethod(nameof(Postfix))));
            }
        }
        
        [HarmonyPriority(Priority.First)]
        public static void Prefix(ref Stopwatch __state)
        {
            __state = Stopwatch.StartNew();
        }
        
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(ref Stopwatch __state, MethodBase __originalMethod)
        {
            __state.Stop();
            
            if (methods.TryGetValue(__originalMethod as MethodInfo, out string label))
            {
                if (!HUD.times.ContainsKey(label) || !HUD.frameIndexes.ContainsKey(label) || HUD.frameIndexes[label] < HUD.frameIndex)
                {
                    HUD.times[label] = 0;
                    HUD.frameIndexes[label] = HUD.frameIndex;
                }

                HUD.times[label] += __state.Elapsed.TotalMilliseconds;
            }
        }

        public static void ProfileStart(out Stopwatch watch)
        {
            if (!Entrypoint.DevelopmentMode)
            {
                watch = null;
                return;
            }
            watch = Stopwatch.StartNew();
        }
        
        public static void ProfileEnd(string label, Stopwatch watch)
        {
            if (watch == null) return;
            
            watch.Stop();
            
            if (!HUD.times.ContainsKey(label) || !HUD.frameIndexes.ContainsKey(label) || HUD.frameIndexes[label] < HUD.frameIndex)
            {
                HUD.times[label] = 0;
                HUD.frameIndexes[label] = HUD.frameIndex;
            }

            HUD.times[label] += watch.Elapsed.TotalMilliseconds;
        }
    }
}