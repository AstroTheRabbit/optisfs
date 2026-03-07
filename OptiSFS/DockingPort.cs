using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using SFS.Builds;
using SFS.Parts.Modules;
using UnityEngine;

namespace OptiSFS
{
    [HarmonyPatch(typeof(DockingPortModule), "FixedUpdate")]
    public class DockingPortModule_FixedUpdate
    {
        private static bool inBuild;
        private static int lastUpdateFrame;
        
        public static MethodInfo dockMethod = typeof(DockingPortModule).GetMethod("Dock", BindingFlags.Instance | BindingFlags.NonPublic);
        public static FieldInfo portsInRangeField = typeof(DockingPortModule).GetField("portsInRange", BindingFlags.Instance | BindingFlags.NonPublic);
        
        [HarmonyPrefix]
        public static bool Prefix(DockingPortModule __instance)
        {
            if (!Entrypoint.PatchEnabled) return true;

            if (lastUpdateFrame < HUD.frameIndex)
            {
                lastUpdateFrame = HUD.frameIndex;
                inBuild = BuildManager.main != null;
            }

            if (inBuild) return false;
            
            //Traverse trav = Traverse.Create(__instance);
            if (!__instance.isDockable)
            {
                return false;
            }
            
            float multiplier = __instance.pullForce * __instance.forceMultiplier.Value * 2f;

            if (multiplier <= 1e-8f)
                return false;
            
            Vector3 position = __instance.transform.position;
            
            float ddSquared = __instance.dockDistance * __instance.dockDistance;
            
            Vector3 force = Vector3.zero;
            
            foreach (DockingPortModule item in (List<DockingPortModule>)portsInRangeField.GetValue(__instance))
            {
                if (!item.isDockable.Value) continue;
                
                var otherPosition = item.transform.position;
                if ((position - otherPosition).sqrMagnitude <= ddSquared)
                {
                    if (force.sqrMagnitude > 1e-8f * multiplier)
                        __instance.Rocket.rb2d.AddForceAtPosition(force * multiplier, position); // just in case the forces applied before docking actually matter
                    dockMethod.Invoke(__instance, new object[] { item });
                    return false; // so we don't apply the same force two times if the port just docked
                }
                
                force += (otherPosition - position).normalized;
            }
            
            // (a + b + c + d) * e = a*e + b*e + c*e + d*e
            
            if (force.magnitude > 1e-8f * multiplier)
                __instance.Rocket.rb2d.AddForceAtPosition(force * multiplier * 2f, position);
            return false;
        }
    }
}