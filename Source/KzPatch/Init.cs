using HarmonyLib;
using Verse;

namespace KzPatch
{
    [StaticConstructorOnStartup]
    public static class Init
    {
        public static bool check = ModLister.HasActiveModWithName("Ancient urban ruins");
        static Init()
        {
            Harmony harmony = new Harmony("KzPatch");
            harmony.PatchAll();
            Log.Message("inite complete # KzPatch");
        }
    }
}

