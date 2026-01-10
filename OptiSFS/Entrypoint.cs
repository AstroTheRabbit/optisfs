using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HarmonyLib;
using ModLoader;
using SFS.IO;
using SFS.World.Drag;
using UITools;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace OptiSFS
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Entrypoint : Mod, IUpdatable
    {
        public override string ModNameID => "moe.verdix.optisfs";

        public override string DisplayName => "OptiSFS";

        public override string Author => "VerdiX094";

        public override string MinimumGameVersionNecessary => "1.6.0.3";

        public override string ModVersion => "v0.1";

        public override string Description => "Various optimizations for Spaceflight Simulator.";

        public Dictionary<string, FilePath> UpdatableFiles => new Dictionary<string, FilePath>
        {
            {"https://github.com/VerdiX094/optisfs/releases/latest/download/OptiSFS.dll", new FolderPath(ModFolder).ExtendToFile("OptiSFS.dll")}
        };
        
        public static bool PatchEnabled = true;
        public static bool TreatmentGroup = false;

        public const bool DevelopmentMode = false;

        public static bool VersionHasRadixSort;

        public static bool ANAISLoaded = false;
        
        public override void Early_Load()
        {
            VersionHasRadixSort = !Application.version.Contains("1.5.");

            if (PatchEnabled)
            {
                new Harmony(ModNameID).PatchAll();
            }

            if (!VersionHasRadixSort)
            {
                new Harmony(ModNameID + ".aero").Patch(typeof(AeroModule).GetMethod("SortDragSurfacesByEndX"), new HarmonyMethod(typeof(SortingPatch).GetMethod("Prefix")));
            }
            
            if (!SurfaceEndXRadixSort.Test()) Debug.Log("SURFACE SORT TEST FAILED");

            new GameObject().AddComponent<HUD>();
            
            if (DevelopmentMode)
                Benchmark.ApplyPatches();
        }

        public override void Load()
        {
            ANAISLoaded = Loader.main.GetLoadedMods().Any(mod => mod.ModNameID == "ANAIS");
        }
    }
}