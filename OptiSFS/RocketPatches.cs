using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SFS.Parts;
using SFS.Parts.Modules;
using SFS.Variables;
using SFS.World;
using SFS.World.Maps;
using UnityEngine;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace OptiSFS
{
    [HarmonyPatch(typeof(Rocket))]
    public static class RocketPatches
    {
        [HarmonyPatch("ApplyTorque")]
        [HarmonyPrefix]
        public static void ApplyTorque(Rocket __instance, ref bool __runOriginal)
        {
            if (!__runOriginal)
                return;
            
            if (!Entrypoint.PatchEnabled)
                return;

            __runOriginal = false;
            
            Rigidbody2D rb2d = __instance.rb2d;
            Arrowkeys arrowkeys = __instance.arrowkeys;
            
            Float_Local output_turnAxisTorque = __instance.output_TurnAxisTorque;
            __instance.output_TurnAxisWheels.Value = arrowkeys.turnAxis;
            
            if (Mathf.Abs(arrowkeys.turnAxis.Value) < 0.000001f && Mathf.Abs(rb2d.angularVelocity) < 0.0001f)
            {
                output_turnAxisTorque.Value = 0f;
                return;
            }
            
            float torque = __instance.GetTorque();
            
            if (rb2d.mass > 200f)
            {
                torque /= Mathf.Pow(rb2d.mass / 200f, 0.35f);
            }
            
            output_turnAxisTorque.Value = __instance.GetTurnAxis(torque, true);
            
            if (output_turnAxisTorque.Value != 0f && rb2d.simulated)
            {
                rb2d.angularVelocity -= torque * 57.29578f / rb2d.mass * output_turnAxisTorque.Value * Time.fixedDeltaTime;
            }
        }

        [HarmonyPatch("UpdateMapIconRotation")]
        [HarmonyPrefix]
        public static bool UpdateMapIconRotation(Rocket __instance)
        {
            if (!Entrypoint.PatchEnabled) return true;
            if (!Map.manager.mapMode.Value) return false;
            
            __instance.mapIcon.SetRotation(__instance.GetRotation());

            return false;
        }

        public static FieldInfo dirty = AccessTools.Field(typeof(Mass_Calculator), "dirty");
        
        [HarmonyPatch("UpdateMass")]
        [HarmonyPrefix]
        public static bool UpdateMass(Rocket __instance)
        {
            if (!Entrypoint.PatchEnabled) return true;
            //if (!(bool)dirty.GetValue(__instance.mass)) return false;

            float mass = __instance.mass.GetMass();
            Vector2 cg = __instance.mass.GetCenterOfMass();
            
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (mass != __instance.rb2d.mass)
                __instance.rb2d.mass = mass;
            
            if (cg != __instance.rb2d.centerOfMass)
                __instance.rb2d.centerOfMass = cg;

            return false;
        }

        [HarmonyPatch("FixedUpdate")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> FixedUpdate(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> code = instructions.ToList();

            Label continueLabel = il.DefineLabel();
            
            code.InsertRange(code.Count - 4, new[]
            {
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Entrypoint), "PatchEnabled")),
                new CodeInstruction(OpCodes.Brfalse_S, continueLabel),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Rocket), "pipeFlows")),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<ValueTuple<ResourceModule[], ResourceModule>>), "get_Count")),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Bgt, continueLabel),
                new CodeInstruction(OpCodes.Ret),
                new CodeInstruction(OpCodes.Nop) { labels = new List<Label> { continueLabel }},
            });
            
            return code.AsEnumerable();
        }
    }

    public static class PrivateMethods
    {
        private static readonly MethodInfo info_GetTorque = AccessTools.Method(typeof(Rocket), "GetTorque");
        private static readonly MethodInfo info_GetTurnAxis = AccessTools.Method(typeof(Rocket), "GetTurnAxis");
        
        private static readonly Func<Rocket, float> func_GetTorque = AccessTools.MethodDelegate<Func<Rocket, float>>(info_GetTorque);
        private static readonly Func<Rocket, float, bool, float> func_GetTurnAxis = AccessTools.MethodDelegate<Func<Rocket, float, bool, float>>(info_GetTurnAxis);

        public static float GetTorque(this Rocket rocket) => func_GetTorque(rocket);
        public static float GetTurnAxis(this Rocket rocket, float torque, bool useStopRotation) => func_GetTurnAxis(rocket, torque, useStopRotation);
    }
}