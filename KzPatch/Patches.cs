using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Reflection.Emit;

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
                    yield return new CodeInstruction(OpCodes.Brfalse_S, codes[i + 1].operand);
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
}
