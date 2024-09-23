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
            int skipint = -1;
            CodeInstruction cache = null;
            for (int i = 0; i < codes.Count; i++)
            {
                CodeInstruction code = codes[i];
                if (!skip2 && code.opcode == OpCodes.Ldfld && code.ToString().EndsWith("::p"))
                {
                    cache = code;
                    skip2 = true;
                }
                if (code.opcode == OpCodes.Call && code.operand.ToString().Contains("AndReach"))
                {
                    yield return code;
                    yield return codes[i + 1];
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 14);
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return cache;
                    yield return new CodeInstruction(OpCodes.Beq_S, codes[i + 1].operand);
                    skipint = i + 1;
                    skip = true;
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
            return false;
        }
    }
    [HarmonyPatch]
    public static class AncientUrbanRuins_MainTabWindow_LevelSchedule_Patch
    {
        [HarmonyPrepare]
        public static bool Prepare()
        {
            if (ModsConfig.IsActive("xmb.AncientUrbanrUins.mo"))
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
            if (ModsConfig.IsActive("xmb.ancienturbanruins.mo"))
            {
                return true;
            }
            return false;
        }
        [HarmonyAfter(new string[] { "AM_Patch.Patch_Rest.postfix" })]
        [HarmonyPostfix]
        public static void SelectPawn_patch_2(Pawn pawn, ref Job __result)
        {
            if ((!pawn.IsHardworkingPawn()) && (pawn.Downed || pawn.Crawling || __result.targetA.Thing != null))
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
            if (ModsConfig.IsActive("xmb.ancienturbanruins.mo"))
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
        public static IEnumerable<CodeInstruction> EnterAllowedLevel_code(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            bool skip = false;
            bool skip2 = false;
            int skipint = -1;
            CodeInstruction cache = null;
            for (int i = 0; i < codes.Count; i++)
            {
                CodeInstruction code = codes[i];
                if (!skip2 && code.opcode == OpCodes.Ldfld && code.operand.ToString().EndsWith("AICrossLevel"))
                {
                    cache = codes[i + 1];
                    skip2 = true;
                }
                if (code.opcode == OpCodes.Callvirt && code.operand.ToString().EndsWith("IsColonist()"))
                {
                    yield return code;
                    yield return codes[i + 1];
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, typeof(HardworkingUtility).Method("IsHardworkingPawn", new Type[] { typeof(Pawn) }));
                    yield return cache;
                    skipint = i + 1;
                    skip = true;
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
