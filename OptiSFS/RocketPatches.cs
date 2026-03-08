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

namespace OptiSFS
{
    [HarmonyPatch(typeof(Rocket))]
    public static class RocketPatches
    {
        [HarmonyPatch("ApplyTorque")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> ApplyTorque(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            Label label_ret = generator.DefineLabel();
            Label label_cont = generator.DefineLabel();
            Label label_mass = generator.DefineLabel();

            LocalBuilder loc_rb2d = generator.DeclareLocal(typeof(Rigidbody2D));
            LocalBuilder loc_arrowkeys = generator.DeclareLocal(typeof(Arrowkeys));
            LocalBuilder loc_output_turnAxisTorque = generator.DeclareLocal(typeof(Float_Local));
            LocalBuilder loc_torque = generator.DeclareLocal(typeof(float));

            MethodInfo info_float_get = AccessTools.PropertyGetter(typeof(Float_Local), nameof(Float_Local.Value));
            MethodInfo info_float_set = AccessTools.PropertySetter(typeof(Float_Local), nameof(Float_Local.Value));
            MethodInfo info_rb2d_mass_get = AccessTools.PropertyGetter(typeof(Rigidbody2D), nameof(Rigidbody2D.mass));
            MethodInfo info_rb2d_simulated_get = AccessTools.PropertyGetter(typeof(Rigidbody2D), nameof(Rigidbody2D.simulated));
            MethodInfo info_time_fixedDeltaTime_get = AccessTools.PropertyGetter(typeof(Time), nameof(Time.fixedDeltaTime));
            MethodInfo info_rb2d_angularVelocity_get = AccessTools.PropertyGetter(typeof(Rigidbody2D), nameof(Rigidbody2D.angularVelocity));
            MethodInfo info_rb2d_angularVelocity_set = AccessTools.PropertySetter(typeof(Rigidbody2D), nameof(Rigidbody2D.angularVelocity));

            CodeInstruction ldloc_rb2d = new CodeInstruction(OpCodes.Ldloc, loc_rb2d);
            CodeInstruction ldloc_arrowkeys = new CodeInstruction(OpCodes.Ldloc, loc_arrowkeys);
            CodeInstruction ldloc_output_turnAxisTorque = new CodeInstruction(OpCodes.Ldloc, loc_output_turnAxisTorque);
            CodeInstruction ldloc_torque = new CodeInstruction(OpCodes.Ldloc, loc_torque);
            
            CodeInstruction call_float_get = new CodeInstruction(OpCodes.Call, info_float_get);
            CodeInstruction call_float_set = new CodeInstruction(OpCodes.Call, info_float_set);
            CodeInstruction call_abs = CodeInstruction.Call(typeof(Mathf), nameof(Mathf.Abs), new[] { typeof(float) });

            return new[]
            {
                // if (!Entrypoint.PatchEnabled)
                //     return;
                CodeInstruction.LoadField(typeof(Entrypoint), nameof(Entrypoint.PatchEnabled)),
                new CodeInstruction(OpCodes.Brfalse, label_ret),
                
                // Rigidbody2D rb2d = __instance.rb2d;
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.LoadField(typeof(Rocket), nameof(Rocket.rb2d)),
                new CodeInstruction(OpCodes.Stloc, loc_rb2d),
                
                // Arrowkeys arrowkeys = __instance.arrowkeys;
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.LoadField(typeof(Rocket), nameof(Rocket.arrowkeys)),
                new CodeInstruction(OpCodes.Stloc, loc_arrowkeys),
                
                // Float_Local output_turnAxisTorque = __instance.output_TurnAxisTorque;
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.LoadField(typeof(Rocket), nameof(Rocket.output_TurnAxisTorque)),
                new CodeInstruction(OpCodes.Stloc, loc_output_turnAxisTorque),
                
                // __instance.output_TurnAxisWheels.Value = arrowkeys.turnAxis;
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.LoadField(typeof(Rocket), nameof(Rocket.output_TurnAxisWheels)),
                ldloc_arrowkeys.Clone(),
                CodeInstruction.LoadField(typeof(Arrowkeys), nameof(Arrowkeys.turnAxis)),
                call_float_get.Clone(),
                call_float_set.Clone(),
                
                // if (Mathf.Abs(arrowkeys.turnAxis.Value) >= 0.000001f) goto label_cont;
                ldloc_arrowkeys.Clone(),
                CodeInstruction.LoadField(typeof(Arrowkeys), nameof(Arrowkeys.turnAxis)),
                call_float_get.Clone(),
                call_abs.Clone(),
                new CodeInstruction(OpCodes.Ldc_R4, 0.000001f),
                new CodeInstruction(OpCodes.Bge, label_cont),
                
                // if (Mathf.Abs(rb2d.angularVelocity) >= 0.0001f) goto label_cont;
                ldloc_rb2d.Clone(),
                new CodeInstruction(OpCodes.Call, info_rb2d_angularVelocity_get),
                call_abs.Clone(),
                new CodeInstruction(OpCodes.Ldc_R4, 0.0001f),
                new CodeInstruction(OpCodes.Bge, label_cont),
                
                // output_turnAxisTorque.Value = 0f;
                // return;
                ldloc_output_turnAxisTorque.Clone(),
                new CodeInstruction(OpCodes.Ldc_R4, 0f),
                call_float_set.Clone(),
                new CodeInstruction(OpCodes.Br, label_ret),
                
                // label_cont:
                // float torque = __instance.GetTorque();
                new CodeInstruction(OpCodes.Nop).WithLabels(label_cont),
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.Call(typeof(Rocket), "GetTorque"),
                new CodeInstruction(OpCodes.Stloc, loc_torque),
                
                // if (rb2d.mass <= 200f) goto label_mass;
                ldloc_rb2d.Clone(),
                new CodeInstruction(OpCodes.Call, info_rb2d_mass_get),
                new CodeInstruction(OpCodes.Ldc_R4, 200f),
                new CodeInstruction(OpCodes.Ble, label_mass),
                
                // torque /= Mathf.Pow(rb2d.mass / 200f, 0.35f);
                ldloc_torque.Clone(),
                ldloc_rb2d.Clone(),
                new CodeInstruction(OpCodes.Call, info_rb2d_mass_get),
                new CodeInstruction(OpCodes.Ldc_R4, 200f),
                new CodeInstruction(OpCodes.Div),
                new CodeInstruction(OpCodes.Ldc_R4, 0.35f),
                CodeInstruction.Call(typeof(Mathf), nameof(Mathf.Pow)),
                new CodeInstruction(OpCodes.Div),
                new CodeInstruction(OpCodes.Stloc, loc_torque),
                
                // label_mass:
                // output_turnAxisTorque.Value = __instance.GetTurnAxis(torque, true);
                new CodeInstruction(OpCodes.Nop).WithLabels(label_mass),
                ldloc_output_turnAxisTorque.Clone(),
                new CodeInstruction(OpCodes.Ldarg_0),
                ldloc_torque.Clone(),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                CodeInstruction.Call(typeof(Rocket), "GetTurnAxis"),
                call_float_set.Clone(),
                
                // if (output_turnAxisTorque.Value == 0f) goto label_ret;
                ldloc_output_turnAxisTorque.Clone(),
                call_float_get.Clone(),
                new CodeInstruction(OpCodes.Ldc_R4, 0f),
                new CodeInstruction(OpCodes.Beq, label_ret),
                
                // TODO: This `rb2d.simulated` check could probably occur at near the top of this method, but it's a very specific edge-case and probably doesn't affect performance much.
                // if (!rb2d.simulated) goto label_ret;
                ldloc_rb2d.Clone(),
                new CodeInstruction(OpCodes.Call, info_rb2d_simulated_get),
                new CodeInstruction(OpCodes.Brfalse, label_ret),
                
                // rb2d.angularVelocity -= (torque * Mathf.Rad2Deg / rb2d.mass) * output_turnAxisTorque.Value * Time.fixedDeltaTime;
                ldloc_rb2d.Clone(),
                ldloc_rb2d.Clone(),
                new CodeInstruction(OpCodes.Call, info_rb2d_angularVelocity_get),
                
                ldloc_torque.Clone(),
                new CodeInstruction(OpCodes.Ldc_R4, Mathf.Rad2Deg),
                new CodeInstruction(OpCodes.Mul),
                
                ldloc_rb2d.Clone(),
                new CodeInstruction(OpCodes.Call, info_rb2d_mass_get),
                new CodeInstruction(OpCodes.Div),
                
                ldloc_output_turnAxisTorque.Clone(),
                call_float_get.Clone(),
                new CodeInstruction(OpCodes.Mul),
                
                new CodeInstruction(OpCodes.Call, info_time_fixedDeltaTime_get),
                new CodeInstruction(OpCodes.Mul),
                
                new CodeInstruction(OpCodes.Sub),
                new CodeInstruction(OpCodes.Call, info_rb2d_angularVelocity_set),
                
                // label_ret:
                // return;
                new CodeInstruction(OpCodes.Nop).WithLabels(label_ret),
                new CodeInstruction(OpCodes.Ret),
            };
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
}