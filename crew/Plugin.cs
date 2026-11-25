using System;
using BepInEx;
using BepInEx.Unity.IL2CPP;

namespace LbbbCrew;

[BepInPlugin("com.coreforgelabs.lbbb.crew", "LBBB Crew Menu", "1.0.0")]
public class Plugin : BasePlugin
{
    public override void Load()
    {
        Log.LogInfo("[crew] плагин загружен");
        try
        {
            AppDomain.CurrentDomain.AssemblyLoad += (s, e) =>
            {
                try { if (e.LoadedAssembly.GetName().Name == "LbbbRU") CrewBehaviour.PatchLog(); } catch { }
            };
        }
        catch { }
        AddComponent<CrewBehaviour>();
    }
}
