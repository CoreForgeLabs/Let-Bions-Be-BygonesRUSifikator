using Object = UnityEngine.Object;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;

namespace LbbbRU;

[BepInPlugin("com.coreforgelabs.lbbb.ru", "Let Bions Be Bygones RU", "1.0.0")]
public class Plugin : BasePlugin
{
	public const string PluginGuid = "com.coreforgelabs.lbbb.ru";

	public const string PluginName = "Let Bions Be Bygones RU";

	public const string PluginVersion = "1.0.0";

	internal static ManualLogSource Logger;

	public override void Load()
	{
		Logger = ((BasePlugin)this).Log;
		Core.Init();
		((BasePlugin)this).AddComponent<ModBehaviour>();
		((BasePlugin)this).Log.LogInfo((object)"[RU] плагин загружен, BepInEx 6");
	}
}
