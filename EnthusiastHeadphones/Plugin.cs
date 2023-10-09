using BepInEx;
using Bepinject;
using HarmonyLib;

namespace EnthusiastHeadphones
{
    [BepInPlugin(Constants.GUID, Constants.Name, Constants.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public Plugin()
        {
            new Harmony("dev.enthusiastheadphones").PatchAll(typeof(Plugin).Assembly);
            Zenjector.Install<MainInstaller>().OnProject().WithConfig(Config).WithLog(Logger);
        }
    }
}
