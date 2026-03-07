using System.Collections.Generic;
using HarmonyLib;
using SFS;
using SFS.Parts.Modules;
using UnityEngine;

namespace OptiSFS
{
    [HarmonyPatch(typeof(EngineModule), "FixedUpdate")]
    public static class EngineModule_FixedUpdate
    {
        static Dictionary<EngineModule, Traverse> traverseDict = new Dictionary<EngineModule, Traverse>();
        
        [HarmonyPrefix]
        public static bool Prefix(EngineModule __instance)
        {
            if (!Entrypoint.PatchEnabled) return true;

            var rb = __instance.Rb2d;
            
            if (rb == null) return false;
            if (__instance.throttle_Out.Value == 0) return false;
            
            Vector2 vector = __instance.thrustNormal.Value * (__instance.thrust.Value * 9.8f * __instance.throttle_Out.Value);
            
            Vector2 force = (Base.worldBase.AllowsCheats ? ((Vector2)__instance.transform.TransformVector(vector)) : __instance.transform.TransformVectorUnscaled(vector));
            
            Vector2 relativePoint = rb.GetRelativePoint(Transform_Utility.LocalToLocalPoint(__instance.transform, rb, __instance.thrustPosition.Value));
            
            rb.AddForceAtPosition(force, relativePoint, ForceMode2D.Force);

            if (!traverseDict.TryGetValue(__instance, out Traverse traverse))
            {
                traverse = new Traverse(__instance).Method("PositionFlameHitbox");
                traverseDict.Add(__instance, traverse);
            }
            
            traverse.GetValue();
            return false;
        }
    }
}