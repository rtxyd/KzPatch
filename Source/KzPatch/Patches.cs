using AncientMarket_Libraray;
using AncientMarketAI_Libraray;
using HarmonyLib;
using Kz;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using Verse.AI;

namespace KzPatch
{
    [HarmonyPatch(typeof(EnterPortalUtility), "FindThingToLoad")]
    public static class EnterPortalUtility_FindThingToLoad_Patch
    {
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> FindThingToLoad_code(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            bool skip = false;
            bool skip2 = false;
            bool skip3 = false;
            object loopEnd = null;
            CodeInstruction cache = null;
            for (int i = 0; i < codes.Count; i++)
            {
                CodeInstruction code = codes[i];
                if (!skip && code.opcode == OpCodes.Isinst && code.operand.ToString().EndsWith("Pawn"))
                {
                    for (int j = i - 1; j >= 0; j--)
                    {
                        CodeInstruction code2 = codes[j];
                        if (code2.opcode == OpCodes.Br)
                        {
                            loopEnd = code2.operand;
                            break;
                        }
                        if (code2.operand.ToString().EndsWith("r()"))
                        {
                            Log.Error("KzPatch not applied #KzPatch");
                            throw new Exception("Can't find loop end");
                        }
                    }
                    skip = true;
                }
                if (!skip2 && code.opcode == OpCodes.Ldfld && code.ToString().EndsWith("::p"))
                {
                    cache = code;
                    skip2 = true;
                }
                if (!skip3 && code.opcode == OpCodes.Callvirt && code.operand.ToString().EndsWith("nist()"))
                {
                    var loc = codes[i - 1].operand;
                    yield return code;
                    yield return codes[i + 1];
                    yield return new CodeInstruction(OpCodes.Ldloc_S, loc);
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return cache;
                    yield return new CodeInstruction(OpCodes.Beq, loopEnd);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, loc);
                    yield return new CodeInstruction(OpCodes.Call, typeof(HardworkingUtility).Method("IsHardworkingPawn", new Type[] { typeof(Pawn) }));
                    yield return new CodeInstruction(OpCodes.Brfalse_S, codes[i + 1].operand);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, loc);
                    yield return new CodeInstruction(OpCodes.Callvirt, typeof(Pawn).PropertyGetter("Downed"));
                    yield return new CodeInstruction(OpCodes.Brfalse, loopEnd);
                    skip3 = true;
                    i++;
                    continue;
                }
                yield return code;
            }
        }

    }

    [HarmonyPatch]
    public static class EnterPortalUtility_MakeLordsAsAppropriate_Patch
    {
        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            return AccessTools.Method("RimWorld.EnterPortalUtility+<>c:<MakeLordsAsAppropriate>b__10_0");
        }
        [HarmonyPrefix]
        public static bool SelectPawn_patch(Pawn x, ref bool __result)
        {
            if (x.IsHardworkingPawn() && !x.Downed)
            {
                __result = true;
                return x.needs == null ? x.Spawned : false;
            }
            return true;
        }
    }
    [HarmonyPatch]
    public static class AncientUrbanRuins_MainTabWindow_LevelSchedule_Patch
    {
        [HarmonyPrepare]
        public static bool Prepare()
        {
            if (Init.check)
            {
                return true;
            }
            return false;
        }

        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            var targetType = typeof(MainTabWindow_LevelSchedule);
            var targetProperty = targetType.GetProperty("Pawns", BindingFlags.NonPublic | BindingFlags.Instance);
            var getMethod = targetProperty.GetGetMethod(true);
            return getMethod;
        }
        [HarmonyPostfix]
        public static void SelectPawn_patch_2(ref IEnumerable<Pawn> __result)
        {
            __result = __result.Concat(Find.CurrentMap.mapPawns.AllPawnsSpawned.Where((p) => p.IsHardworkingPawn()));
        }
    }
    [HarmonyPatch(typeof(JobGiver_GetRest), "TryGiveJob")]
    public static class JobGiver_GetRest_Patch
    {
        [HarmonyPrepare]
        public static bool Prepare()
        {
            if (Init.check)
            {
                return true;
            }
            return false;
        }
        [HarmonyAfter(new string[] { "AM_Patch.Patch_Rest.postfix" })]
        [HarmonyPostfix]
        public static void SelectPawn_patch_2(Pawn pawn, ref Job __result)
        {
            if (!pawn.IsHardworkingPawn())
            {
                return;
            }
            if (__result == null || !AM_ModSetting.setting.enableAICrossLevel && pawn.Downed || pawn.Crawling || __result.targetA.Thing != null)
            {
                return;
            }
            LevelSchedule schedule = GameComponent_AncientMarket.GetComp.GetSchedule(pawn);
            if (schedule != null && schedule.timeSchedule[GenLocalDate.HourOfDay(pawn.Map)])
            {
                List<AMMapPortal> available = new List<AMMapPortal>();
                MapComponent_Submap.GetComp(pawn.Map).Submaps.FindAll((MapParent_Custom m) => m.entrance != null && pawn.CanReach(m.entrance, PathEndMode.Touch, Danger.Deadly) && m.entrance.IsAvailable(pawn)).ForEach(delegate (MapParent_Custom m)
                {
                    available.Add(m.entrance);
                });
                if (pawn.Map.Parent is MapParent_Custom { Exit: not null } mapParent_Custom && mapParent_Custom.Exit.IsAvailable(pawn) && pawn.CanReach(mapParent_Custom.Exit, PathEndMode.Touch, Danger.Deadly))
                {
                    available.Add(mapParent_Custom.Exit);
                }
                if (available.Any())
                {
                    __result = JobMaker.MakeJob(JobDefOf.EnterPortal, available.RandomElement());
                }
            }
        }

    }
    [HarmonyPatch]
    public static class JobGiver_EnterAllowedLevelPatch
    {
        [HarmonyPrepare]
        public static bool Prepare()
        {
            if (Init.check)
            {
                return true;
            }
            return false;
        }
        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            var targetType = typeof(JobGiver_EnterAllowedLevel);
            var getMethod = targetType.Method("TryGiveJob");
            return getMethod;
        }
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> EnterAllowedLevel_code(ILGenerator iLGenerator, IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            bool skip = false;
            bool skip2 = false;
            bool skip3 = false;
            bool skip4 = false;
            int skipint = -2;
            CodeInstruction cache = null;
            Label labelTrue = iLGenerator.DefineLabel();
            for (int i = 0; i < codes.Count; i++)
            {
                CodeInstruction code = codes[i];
                if (!skip2 && code.opcode == OpCodes.Ldfld && code.operand.ToString().EndsWith("AICrossLevel"))
                {
                    cache = codes[i + 1];
                    skip2 = true;
                }
                if (!skip3 && code.opcode == OpCodes.Callvirt && code.operand.ToString().EndsWith("IsColonist()"))
                {
                    yield return code;
                    yield return new CodeInstruction(OpCodes.Brtrue_S, labelTrue);
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, typeof(HardworkingUtility).Method("IsHardworkingPawn", new Type[] { typeof(Pawn) }));
                    yield return cache;
                    skipint = i + 1;
                    skip = true;
                    skip3 = true;
                }
                if (!skip4 && skip2 && code.labels.Contains((Label)cache.operand))
                {
                    code.labels.Add(labelTrue);
                    skip4 = true;
                }
                if (!skip && i != skipint)
                {
                    yield return code;
                }
                else
                {
                    skip = false;
                }
            }
        }
    }
}
