using BepInEx;
using BepInEx.Unity.IL2CPP;

namespace LbbbProbe;

[BepInPlugin("com.coreforgelabs.lbbb.probe", "LBBB Probe", "1.0.0")]
public class Plugin : BasePlugin
{
    public override void Load()
    {
        Log.LogInfo("[probe] загружен");
        AddComponent<ProbeBehaviour>();
    }
}
