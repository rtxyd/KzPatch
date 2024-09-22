using HarmonyLib;
using Verse;

namespace KzPatch
{
    [StaticConstructorOnStartup]
    public static class Init
    {
        static Init()
        {
            Harmony harmony = new Harmony("KzPatch");
            harmony.PatchAll();
        }
    }
}

