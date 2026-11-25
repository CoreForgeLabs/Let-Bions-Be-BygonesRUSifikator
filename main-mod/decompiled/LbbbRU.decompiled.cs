using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Permissions;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Game.UI;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;
using Il2CppSystem.Collections.Generic;
using Microsoft.CodeAnalysis;
using PixelCrushers;
using PixelCrushers.DialogueSystem;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: TargetFramework(".NETCoreApp,Version=v6.0", FrameworkDisplayName = ".NET 6.0")]
[assembly: AssemblyCompany("LbbbRU")]
[assembly: AssemblyConfiguration("Release")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.0.0")]
[assembly: AssemblyProduct("LbbbRU")]
[assembly: AssemblyTitle("LbbbRU")]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: AssemblyVersion("1.0.0.0")]
[module: UnverifiableCode]
[module: RefSafetyRules(11)]
namespace Microsoft.CodeAnalysis
{
	[CompilerGenerated]
	[Embedded]
	internal sealed class EmbeddedAttribute : Attribute
	{
	}
}
namespace System.Runtime.CompilerServices
{
	[CompilerGenerated]
	[Embedded]
	[AttributeUsage(AttributeTargets.Module, AllowMultiple = false, Inherited = false)]
	internal sealed class RefSafetyRulesAttribute : Attribute
	{
		public readonly int Version;

		public RefSafetyRulesAttribute(int P_0)
		{
			Version = P_0;
		}
	}
}
namespace LbbbRU
{
	public static class Core
	{
		private static Dictionary<string, string> tr;

		public static Dictionary<string, string> uiTr;

		private static Dictionary<string, string> enToRu;

		private static Dictionary<string, string> normToRu;

		private static float lastReload = 0f;

		private static DateTime trStamp = DateTime.MinValue;

		private static DateTime uiStamp = DateTime.MinValue;

		private static int uiApplied = 0;

		private static float timer = 0f;

		private static float uiTick = 0f;

		private static int fontAttempts = 0;

		private static bool ruFontIsTemp = false;

		private static TMP_FontAsset ruFont;

		private static int textReplacements;

		private static int fontSwaps = 0;

		private static float lastLog = 0f;

		private static bool cfgApply = true;

		private static bool cfgFps = true;

		private static float fpsAcc = 0f;

		private static int fpsFrames = 0;

		private static float fpsWorst = 0f;

		private static double sumDbMs;

		private static double sumFontMs;

		private static double sumUiMs;

		private static int burstCount = 0;

		private static long maxBurstMs = 0L;

		private static int lastTexts = 0;

		private static readonly HashSet<IntPtr> translatedConvs = new HashSet<IntPtr>();

		private static bool langProbed = false;

		private static int langChecks = 0;

		private static long lastTablesTick = 0L;

		private static string lastSeenLang = null;

		private static bool menuInstalled = false;

		private static float refillAt = 0f;

		private static bool dbRuLost = false;

		private static bool cfgDump = false;

		private static float menuRefreshUntil = 0f;

		private static TMP_Text[] uiTexts;

		private static float uiTextsAt;

		private static bool uiFrameBusy;

		private static readonly HashSet<IntPtr> dumpedDbs = new HashSet<IntPtr>();

		private static int dumpIndex = 0;

		private static bool probed = false;

		private static readonly string RU_CHARS = "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя№";

		private static bool inFontSwap = false;

		private static bool atlasHeated = false;

		private static int fallbackPass = 0;

		private static Font ruUnityFont;

		private static Dictionary<string, string> ruToEn;

		private static string ModsDir => Path.GetDirectoryName(typeof(Core).Assembly.Location) ?? ".";

		private static string GameDir => Paths.GameRootPath;

		public static string GameDirPublic => GameDir;

		public static TMP_FontAsset RuFont => ruFont;

		public static string ModsDirPublic => ModsDir;

		public static string Csv(string s)
		{
			return CsvEscape(s);
		}

		public static void RequestMenuRefresh()
		{
			menuRefreshUntil = Time.time + 3f;
		}

		private static void NativeLanguagePass()
		{
			if (!cfgApply || tr == null || tr.Count == 0)
			{
				return;
			}
			if (!langProbed)
			{
				Il2CppArrayBase<DialogueDatabase> val = Resources.FindObjectsOfTypeAll<DialogueDatabase>();
				DialogueDatabase val2 = null;
				for (int i = 0; i < val.Length; i++)
				{
					if ((Object)(object)val[i] != (Object)null)
					{
						val2 = val[i];
						break;
					}
				}
				if ((Object)(object)val2 == (Object)null)
				{
					return;
				}
				langProbed = true;
				LocalizationRU.NativeWorks = LocalizationRU.Probe(val2);
				Log("родной язык: движок " + (LocalizationRU.NativeWorks ? "понимает поле ru" : "НЕ понимает поле ru — заливка в базы не выполняется"));
			}
			if (!LocalizationRU.NativeWorks)
			{
				return;
			}
			langChecks++;
			try
			{
				string language = Localization.language;
				if (language != lastSeenLang)
				{
					Log("язык сейчас: " + (language ?? "null") + " (было " + (lastSeenLang ?? "—") + ")");
					if (lastSeenLang == "ru" && language != "ru")
					{
						RestoreEnglishTexts();
					}
					lastSeenLang = language;
					refillAt = Time.time + 2f;
				}
			}
			catch
			{
			}
			long num = 0L;
			try
			{
				string path = Path.Combine(ModsDir, "LBBB_RU", "ui_tables.csv");
				if (File.Exists(path))
				{
					num = File.GetLastWriteTimeUtc(path).Ticks;
				}
			}
			catch
			{
			}
			bool flag = num != lastTablesTick && num != 0;
			if (flag)
			{
				lastTablesTick = num;
			}
			bool flag2 = (langChecks == 1) | flag;
			if (!flag2 && refillAt > 0f && Time.time >= refillAt)
			{
				refillAt = 0f;
				bool num2 = dbRuLost || RuMissing() || !LocalizationRU.TablesHaveRu();
				dbRuLost = false;
				if (num2)
				{
					flag2 = true;
					Log("смена языка: перевод пропал — заливаю заново");
				}
			}
			if (!flag2 && langChecks >= 4 && langChecks % 4 == 0)
			{
				if (dbRuLost)
				{
					dbRuLost = false;
					flag2 = true;
					Log("перевод пропал из баз — заливаю заново");
				}
				else if (RuMissing())
				{
					flag2 = true;
					Log("перевод пропал из баз — заливаю заново");
				}
				else if (!LocalizationRU.TablesHaveRu())
				{
					flag2 = true;
					Log("перевод пропал из таблиц — заливаю заново");
				}
			}
			if (flag2)
			{
				Il2CppArrayBase<DialogueDatabase> val3 = Resources.FindObjectsOfTypeAll<DialogueDatabase>();
				int num3 = 0;
				for (int j = 0; j < val3.Length; j++)
				{
					DialogueDatabase val4 = val3[j];
					if (!((Object)(object)val4 == (Object)null))
					{
						try
						{
							num3 += LocalizationRU.FillDatabase(val4, tr);
						}
						catch
						{
						}
					}
				}
				if ((Object)(object)ruFont != (Object)null)
				{
					LocalizationRU.FillFonts(ruFont);
				}
				LocalizationRU.FillTables(uiTr);
				LocalizationRU.SetupLanguage();
				refillAt = 0f;
				dbRuLost = false;
				Log("родной язык: полей ru записано " + num3 + ", язык игры=" + (Localization.language ?? "?") + " (перевод доступен как отдельный язык \"ru\")");
			}
			if (langChecks % 3 == 0)
			{
				LocalizationRU.MaintainMenu();
			}
			if (cfgDump && langChecks >= 2 && langChecks % 5 == 0)
			{
				LocalizationRU.DumpDropdowns();
				LocalizationRU.DumpSettingsTree();
				LocalizationRU.DumpLanguagePanels();
				LocalizationRU.DumpLanguageFields();
				LocalizationRU.ProbeLanguageUI();
				LocalizationRU.DumpTables();
			}
		}

		private static bool RuMissing()
		{
			try
			{
				Il2CppArrayBase<DialogueDatabase> val = Resources.FindObjectsOfTypeAll<DialogueDatabase>();
				if (val == null)
				{
					return false;
				}
				for (int i = 0; i < val.Length; i++)
				{
					DialogueDatabase val2 = val[i];
					if ((Object)(object)val2 == (Object)null)
					{
						continue;
					}
					List<Conversation> conversations = val2.conversations;
					if (conversations == null)
					{
						continue;
					}
					for (int j = 0; j < conversations.Count; j++)
					{
						Conversation val3 = conversations[j];
						if (val3 == null)
						{
							continue;
						}
						List<DialogueEntry> dialogueEntries = val3.dialogueEntries;
						if (dialogueEntries == null)
						{
							continue;
						}
						for (int k = 0; k < dialogueEntries.Count; k++)
						{
							DialogueEntry val4 = dialogueEntries[k];
							if (val4 != null)
							{
								string key = ((Asset)val3).id + "_" + val4.id + "_dlg";
								if (tr != null && tr.ContainsKey(key))
								{
									return !LocalizationRU.HasTranslation(val4);
								}
							}
						}
					}
				}
			}
			catch
			{
			}
			return false;
		}

		public static void Init()
		{
			Log("mod dir: " + ModsDir);
			ReadConfig();
			ReloadTranslations();
			Directory.CreateDirectory(Path.Combine(GameDir, "UserData", "LBBB_RU", "dump"));
			try
			{
				EnsureFont();
			}
			catch (Exception ex)
			{
				Log("font at start err: " + ex.Message);
			}
			try
			{
				PreheatAtlas();
			}
			catch
			{
			}
			try
			{
				AttachFallback();
			}
			catch
			{
			}
			Log("шрифт на старте: " + (((Object)(object)ruFont == (Object)null) ? "нет, пробую дальше" : ((Object)ruFont).name));
		}

		public static void Log(string s)
		{
			try
			{
				ManualLogSource logger = Plugin.Logger;
				if (logger != null)
				{
					logger.LogInfo((object)("[RU] " + s));
				}
			}
			catch
			{
			}
		}

		private static List<string> ParseCsvLine(string line)
		{
			List<string> list = new List<string>();
			StringBuilder stringBuilder = new StringBuilder();
			bool flag = false;
			for (int i = 0; i < line.Length; i++)
			{
				char c = line[i];
				if (flag)
				{
					if (c == '"')
					{
						if (i + 1 < line.Length && line[i + 1] == '"')
						{
							stringBuilder.Append('"');
							i++;
						}
						else
						{
							flag = false;
						}
					}
					else
					{
						stringBuilder.Append(c);
					}
					continue;
				}
				switch (c)
				{
				case '"':
					flag = true;
					break;
				case ';':
					list.Add(stringBuilder.ToString());
					stringBuilder.Clear();
					break;
				default:
					stringBuilder.Append(c);
					break;
				}
			}
			list.Add(stringBuilder.ToString());
			return list;
		}

		private static void ReadConfig()
		{
			try
			{
				string path = Path.Combine(ModsDir, "LBBB_RU", "config.txt");
				if (!File.Exists(path))
				{
					return;
				}
				string[] array = File.ReadAllLines(path, Encoding.UTF8);
				for (int i = 0; i < array.Length; i++)
				{
					string text = array[i].Trim();
					if (text.Length == 0 || text[0] == '#')
					{
						continue;
					}
					int num = text.IndexOf('=');
					if (num >= 0)
					{
						string text2 = text.Substring(0, num).Trim().ToLowerInvariant();
						string text3 = text.Substring(num + 1).Trim();
						bool flag = text3 == "1" || text3.Equals("true", StringComparison.OrdinalIgnoreCase) || text3.Equals("yes", StringComparison.OrdinalIgnoreCase);
						switch (text2)
						{
						case "apply":
							cfgApply = flag;
							break;
						case "fps":
							cfgFps = flag;
							break;
						case "dump":
						case "дампы":
							cfgDump = flag;
							break;
						}
					}
				}
				Log("config: apply=" + cfgApply + " fps=" + cfgFps + " dump=" + cfgDump);
			}
			catch (Exception ex)
			{
				Log("config err: " + ex.Message);
			}
		}

		private static void CheckReload()
		{
			try
			{
				if (!(Time.time - lastReload < 20f))
				{
					lastReload = Time.time;
					if (Changed(Path.Combine(ModsDir, "LBBB_RU", "ru.csv"), ref trStamp))
					{
						ReloadTranslations();
					}
					string path = Path.Combine(ModsDir, "LBBB_RU", "ui.csv");
					if (Changed(path, ref uiStamp))
					{
						ReloadUi(path);
					}
				}
			}
			catch (Exception ex)
			{
				Log("reload err: " + ex.Message);
			}
		}

		private static bool Changed(string path, ref DateTime stamp)
		{
			try
			{
				if (!File.Exists(path))
				{
					return false;
				}
				DateTime lastWriteTimeUtc = File.GetLastWriteTimeUtc(path);
				if (lastWriteTimeUtc == stamp)
				{
					return false;
				}
				stamp = lastWriteTimeUtc;
				return true;
			}
			catch
			{
				return false;
			}
		}

		private static void ReloadUi(string path)
		{
			try
			{
				Dictionary<string, string> dictionary = new Dictionary<string, string>();
				string[] array = File.ReadAllLines(path, Encoding.UTF8);
				for (int i = 1; i < array.Length; i++)
				{
					if (string.IsNullOrWhiteSpace(array[i]))
					{
						continue;
					}
					List<string> list = ParseCsvLine(array[i]);
					if (list.Count >= 2)
					{
						string text = list[0].Replace("\\n", "\n");
						string value = list[1].Replace("\\n", "\n");
						if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(value))
						{
							dictionary[text] = value;
						}
					}
				}
				uiTr = dictionary;
				Log("ui strings loaded: " + dictionary.Count);
			}
			catch (Exception ex)
			{
				Log("ui load fail: " + ex.Message);
			}
		}

		public static void LateUpdate()
		{
			if (uiFrameBusy)
			{
				return;
			}
			uiFrameBusy = true;
			try
			{
				UiFramePass();
			}
			catch (Exception ex)
			{
				Log("ui frame err: " + ex.Message);
			}
			finally
			{
				uiFrameBusy = false;
			}
		}

		private static void UiFramePass()
		{
			if ((uiTr == null || uiTr.Count == 0) && (enToRu == null || enToRu.Count == 0))
			{
				return;
			}
			try
			{
				if (Localization.language != "ru")
				{
					return;
				}
			}
			catch
			{
				return;
			}
			if (uiTexts == null || Time.time >= uiTextsAt)
			{
				uiTextsAt = Time.time + 0.15f;
				try
				{
					uiTexts = Il2CppArrayBase<TMP_Text>.op_Implicit(Resources.FindObjectsOfTypeAll<TMP_Text>());
				}
				catch
				{
					uiTexts = null;
					return;
				}
			}
			TMP_Text[] array = uiTexts;
			foreach (TMP_Text val in array)
			{
				if ((Object)(object)val == (Object)null)
				{
					continue;
				}
				string text = null;
				try
				{
					text = val.text;
				}
				catch
				{
				}
				if (string.IsNullOrEmpty(text))
				{
					continue;
				}
				if (HasCyrillic(text))
				{
					EnsureFontFor(val, text);
					continue;
				}
				string text2 = TranslateVisible(text);
				if (!(text2 == text) && !string.IsNullOrEmpty(text2))
				{
					EnsureFontFor(val, text2);
					try
					{
						val.text = text2;
						uiApplied++;
					}
					catch
					{
					}
				}
			}
		}

		private static void ReloadTranslations()
		{
			string[] array = new string[2]
			{
				Path.Combine(ModsDir, "LBBB_RU", "ru.csv"),
				Path.Combine(GameDir, "UserData", "LBBB_RU", "ru.csv")
			};
			foreach (string text in array)
			{
				if (!File.Exists(text))
				{
					continue;
				}
				try
				{
					Dictionary<string, string> dictionary = new Dictionary<string, string>();
					Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
					string[] array2 = File.ReadAllLines(text, Encoding.UTF8);
					for (int j = 1; j < array2.Length; j++)
					{
						if (string.IsNullOrWhiteSpace(array2[j]))
						{
							continue;
						}
						List<string> list = ParseCsvLine(array2[j]);
						if (list.Count >= 5)
						{
							string key = list[0] + "_" + list[1] + "_" + list[2];
							string value = list[4].Replace("\\n", "\n");
							if (!string.IsNullOrEmpty(value))
							{
								dictionary[key] = value;
							}
							string text2 = list[3].Replace("\\n", "\n");
							if (!string.IsNullOrEmpty(text2) && !string.IsNullOrEmpty(value))
							{
								dictionary2[text2] = value;
							}
						}
					}
					Dictionary<string, string> dictionary3 = new Dictionary<string, string>();
					foreach (KeyValuePair<string, string> item in dictionary2)
					{
						string text3 = Norm(item.Key);
						if (!string.IsNullOrEmpty(text3))
						{
							dictionary3[text3] = item.Value;
						}
					}
					tr = dictionary;
					enToRu = dictionary2;
					normToRu = dictionary3;
					translatedConvs.Clear();
					try
					{
						trStamp = File.GetLastWriteTimeUtc(text);
					}
					catch
					{
					}
					Log("translations loaded: " + dictionary.Count + " from " + text);
					return;
				}
				catch (Exception ex)
				{
					Log("load fail: " + ex.Message);
				}
			}
			Log("translations NOT found (looked in BepInEx/plugins/LBBB_RU/ru.csv)");
			tr = new Dictionary<string, string>();
		}

		public static void Update()
		{
			float unscaledDeltaTime = Time.unscaledDeltaTime;
			if (cfgFps)
			{
				fpsAcc += unscaledDeltaTime;
				fpsFrames++;
				if (unscaledDeltaTime > fpsWorst)
				{
					fpsWorst = unscaledDeltaTime;
				}
				if (fpsAcc >= 5f)
				{
					Log("[perf] fps=" + ((float)fpsFrames / fpsAcc).ToString("F1") + " avg=" + (fpsAcc / (float)fpsFrames * 1000f).ToString("F1") + "ms worst=" + (fpsWorst * 1000f).ToString("F0") + "ms меню=" + Credits.MenuWorldEnabled() + "/" + Credits.MenuWorldCount() + " игра=" + Credits.GameUiEnabled() + " texts=" + lastTexts);
					fpsAcc = 0f;
					fpsFrames = 0;
					fpsWorst = 0f;
				}
			}
			uiTick += unscaledDeltaTime;
			if (uiTick >= 0.25f)
			{
				uiTick = 0f;
				if (cfgApply)
				{
					EnsureFont();
					PreheatAtlas();
					if (!menuInstalled)
					{
						menuInstalled = LocalizationRU.InstallIntoMenu(verbose: false);
					}
					Credits.Tick();
					ApplyTextMap();
					if (menuRefreshUntil > 0f)
					{
						if (Time.time < menuRefreshUntil)
						{
							LocalizationRU.MaintainMenu();
						}
						else
						{
							menuRefreshUntil = 0f;
						}
					}
				}
			}
			timer += unscaledDeltaTime;
			if (timer < 2f)
			{
				return;
			}
			timer = 0f;
			try
			{
				CheckReload();
				if (tr == null)
				{
					ReloadTranslations();
				}
				string path = Path.Combine(ModsDir, "LBBB_RU", "шрифты.txt");
				if (File.Exists(path))
				{
					try
					{
						File.Delete(path);
					}
					catch
					{
					}
					DumpFonts();
				}
				string path2 = Path.Combine(ModsDir, "LBBB_RU", "texts_dump.txt");
				if (File.Exists(path2))
				{
					try
					{
						File.Delete(path2);
					}
					catch
					{
					}
					DumpTexts();
				}
				string path3 = Path.Combine(ModsDir, "LBBB_RU", "dump.txt");
				if (File.Exists(path3))
				{
					foreach (DialogueDatabase item in Resources.FindObjectsOfTypeAll<DialogueDatabase>())
					{
						if ((Object)(object)item != (Object)null)
						{
							DumpDatabase(item);
							DumpStructure(item);
						}
					}
					try
					{
						File.Delete(path3);
					}
					catch
					{
					}
				}
				if (!cfgApply)
				{
					return;
				}
				Stopwatch stopwatch = Stopwatch.StartNew();
				FontProbe();
				EnsureFont();
				PreheatAtlas();
				NativeLanguagePass();
				AttachFallback();
				long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
				long elapsedMilliseconds2 = stopwatch.ElapsedMilliseconds;
				SwapFonts();
				long elapsedMilliseconds3 = stopwatch.ElapsedMilliseconds;
				long elapsedMilliseconds4 = stopwatch.ElapsedMilliseconds;
				sumDbMs += elapsedMilliseconds2 - elapsedMilliseconds;
				sumFontMs += elapsedMilliseconds3 - elapsedMilliseconds2;
				sumUiMs += elapsedMilliseconds4 - elapsedMilliseconds3;
				burstCount++;
				if (elapsedMilliseconds4 > maxBurstMs)
				{
					maxBurstMs = elapsedMilliseconds4;
				}
				if (Time.time - lastLog > 30f)
				{
					lastLog = Time.time;
					Log("fontSwaps=" + fontSwaps + " ui=" + uiApplied);
					if (burstCount > 0)
					{
						Log("[perf] burst: db=" + (sumDbMs / (double)burstCount).ToString("F1") + "ms fonts=" + (sumFontMs / (double)burstCount).ToString("F1") + "ms ui=" + (sumUiMs / (double)burstCount).ToString("F1") + "ms max=" + maxBurstMs + "ms n=" + burstCount + " язык=" + (Localization.language ?? "?"));
					}
					sumDbMs = (sumFontMs = (sumUiMs = 0.0));
					burstCount = 0;
					maxBurstMs = 0L;
				}
			}
			catch (Exception ex)
			{
				Log("ERR " + ex);
			}
		}

		private static void DumpDatabase(DialogueDatabase db)
		{
			try
			{
				string text = Path.Combine(GameDir, "UserData", "LBBB_RU", "dump");
				Directory.CreateDirectory(text);
				dumpIndex++;
				string path = Path.Combine(text, "live_" + dumpIndex.ToString("00") + ".csv");
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append("conv;entry;field;en;ru\n");
				int num = 0;
				List<Conversation> conversations = db.conversations;
				if (conversations != null)
				{
					for (int i = 0; i < conversations.Count; i++)
					{
						Conversation val = conversations[i];
						if (val == null)
						{
							continue;
						}
						List<DialogueEntry> dialogueEntries = val.dialogueEntries;
						if (dialogueEntries == null)
						{
							continue;
						}
						for (int j = 0; j < dialogueEntries.Count; j++)
						{
							DialogueEntry val2 = dialogueEntries[j];
							if (val2 != null)
							{
								string field = GetField(val2, "Dialogue Text");
								if (!string.IsNullOrEmpty(field))
								{
									stringBuilder.Append(((Asset)val).id + ";" + val2.id + ";dlg;" + Csv(field) + ";\n");
									num++;
								}
								string field2 = GetField(val2, "Menu Text");
								if (!string.IsNullOrEmpty(field2))
								{
									stringBuilder.Append(((Asset)val).id + ";" + val2.id + ";mnu;" + Csv(field2) + ";\n");
									num++;
								}
							}
						}
					}
				}
				File.WriteAllText(path, stringBuilder.ToString(), Encoding.UTF8);
				Log("dump: " + num + " строк -> " + Path.GetFileName(path));
			}
			catch (Exception ex)
			{
				Log("dump err: " + ex.Message);
			}
		}

		private static string GetField(DialogueEntry ent, string title)
		{
			try
			{
				List<Field> fields = ent.fields;
				if (fields == null)
				{
					return null;
				}
				for (int i = 0; i < fields.Count; i++)
				{
					Field val = fields[i];
					if (val != null && string.Equals(val.title ?? "", title, StringComparison.OrdinalIgnoreCase))
					{
						return val.value;
					}
				}
			}
			catch
			{
			}
			return null;
		}

		private static void DumpStructure(DialogueDatabase db)
		{
			try
			{
				string text = Path.Combine(GameDir, "UserData", "LBBB_RU");
				Directory.CreateDirectory(text);
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendLine("db: " + ((Object)db).name);
				try
				{
					Type type = ((object)db).GetType();
					stringBuilder.AppendLine("тип базы: " + type.FullName);
					PropertyInfo[] properties = type.GetProperties();
					foreach (PropertyInfo propertyInfo in properties)
					{
						if (propertyInfo.Name.IndexOf("lang", StringComparison.OrdinalIgnoreCase) >= 0)
						{
							stringBuilder.AppendLine("  свойство: " + propertyInfo.Name + " : " + propertyInfo.PropertyType.Name);
						}
					}
				}
				catch (Exception ex)
				{
					stringBuilder.AppendLine("рефлексия: " + ex.GetType().Name + " " + ex.Message);
				}
				Dictionary<string, int> dictionary = new Dictionary<string, int>();
				List<Conversation> conversations = db.conversations;
				int num = 0;
				if (conversations != null)
				{
					for (int j = 0; j < conversations.Count; j++)
					{
						Conversation val = conversations[j];
						if (val == null)
						{
							continue;
						}
						List<DialogueEntry> dialogueEntries = val.dialogueEntries;
						if (dialogueEntries == null)
						{
							continue;
						}
						for (int k = 0; k < dialogueEntries.Count; k++)
						{
							DialogueEntry val2 = dialogueEntries[k];
							if (val2 == null)
							{
								continue;
							}
							List<Field> fields = val2.fields;
							if (fields == null)
							{
								continue;
							}
							for (int l = 0; l < fields.Count; l++)
							{
								Field val3 = fields[l];
								if (val3 != null)
								{
									string key = val3.title ?? "(без имени)";
									if (!dictionary.ContainsKey(key))
									{
										dictionary[key] = 0;
									}
									dictionary[key]++;
								}
							}
							if (num >= 3 || k >= 2)
							{
								continue;
							}
							num++;
							stringBuilder.AppendLine("--- пример: беседа " + ((Asset)val).id + " вход " + val2.id);
							for (int m = 0; m < fields.Count; m++)
							{
								Field val4 = fields[m];
								if (val4 != null)
								{
									string text2 = val4.value ?? "";
									if (text2.Length > 90)
									{
										text2 = text2.Substring(0, 90) + "…";
									}
									stringBuilder.AppendLine("   [" + val4.title + "] = " + text2);
								}
							}
						}
					}
				}
				try
				{
					stringBuilder.AppendLine("Localization.language = \"" + (Localization.language ?? "null") + "\"");
				}
				catch (Exception ex2)
				{
					stringBuilder.AppendLine("Localization err: " + ex2.GetType().Name);
				}
				try
				{
					Il2CppArrayBase<TextTable> val5 = Resources.FindObjectsOfTypeAll<TextTable>();
					stringBuilder.AppendLine("TextTable найдено: " + val5.Length);
					for (int n = 0; n < val5.Length && n < 5; n++)
					{
						TextTable val6 = val5[n];
						if ((Object)(object)val6 == (Object)null)
						{
							continue;
						}
						stringBuilder.AppendLine("  таблица \"" + ((Object)val6).name + "\"");
						try
						{
							Il2CppStringArray languageNames = val6.GetLanguageNames();
							stringBuilder.Append("     языки: ");
							if (languageNames != null)
							{
								for (int num2 = 0; num2 < ((Il2CppArrayBase<string>)(object)languageNames).Count; num2++)
								{
									stringBuilder.Append("[" + ((Il2CppArrayBase<string>)(object)languageNames)[num2] + "] ");
								}
							}
							stringBuilder.AppendLine();
						}
						catch (Exception ex3)
						{
							stringBuilder.AppendLine("     языки err: " + ex3.GetType().Name);
						}
						try
						{
							Il2CppStringArray fieldNames = val6.GetFieldNames();
							stringBuilder.AppendLine("     ключей: " + (((Il2CppArrayBase<string>)(object)fieldNames)?.Count ?? 0));
							if (fieldNames == null)
							{
								continue;
							}
							for (int num3 = 0; num3 < ((Il2CppArrayBase<string>)(object)fieldNames).Count && num3 < 3; num3++)
							{
								stringBuilder.Append("       " + ((Il2CppArrayBase<string>)(object)fieldNames)[num3] + " = ");
								try
								{
									Il2CppStringArray languageNames2 = val6.GetLanguageNames();
									for (int num4 = 0; num4 < ((Il2CppArrayBase<string>)(object)languageNames2).Count; num4++)
									{
										string text3 = val6.GetFieldTextForLanguage(((Il2CppArrayBase<string>)(object)fieldNames)[num3], ((Il2CppArrayBase<string>)(object)languageNames2)[num4]);
										if (text3 != null && text3.Length > 40)
										{
											text3 = text3.Substring(0, 40) + "…";
										}
										stringBuilder.Append("{" + ((Il2CppArrayBase<string>)(object)languageNames2)[num4] + ":" + text3 + "} ");
									}
								}
								catch
								{
								}
								stringBuilder.AppendLine();
							}
						}
						catch (Exception ex4)
						{
							stringBuilder.AppendLine("     ключи err: " + ex4.GetType().Name);
						}
					}
				}
				catch (Exception ex5)
				{
					stringBuilder.AppendLine("TextTable err: " + ex5.Message);
				}
				try
				{
					Il2CppArrayBase<LocalizedFonts> val7 = Resources.FindObjectsOfTypeAll<LocalizedFonts>();
					stringBuilder.AppendLine("LocalizedFonts найдено: " + val7.Length);
					for (int num5 = 0; num5 < val7.Length && num5 < 2; num5++)
					{
						LocalizedFonts val8 = val7[num5];
						if (!((Object)(object)val8 == (Object)null))
						{
							TMP_FontAsset defaultTextMeshProFont = val8.defaultTextMeshProFont;
							stringBuilder.AppendLine("  \"" + ((Object)val8).name + "\" default=" + (((Object)(object)defaultTextMeshProFont == (Object)null) ? "null" : ((Object)defaultTextMeshProFont).name));
							stringBuilder.AppendLine("     языков со шрифтом: " + (val8.fontsForLanguages?.Count ?? 0));
						}
					}
				}
				catch (Exception ex6)
				{
					stringBuilder.AppendLine("LocalizedFonts err: " + ex6.Message);
				}
				stringBuilder.AppendLine("--- все имена полей (сколько раз встречаются)");
				List<string> list = new List<string>(dictionary.Keys);
				list.Sort();
				foreach (string item in list)
				{
					stringBuilder.AppendLine("   " + item + " = " + dictionary[item]);
				}
				File.WriteAllText(Path.Combine(text, "structure.txt"), stringBuilder.ToString(), Encoding.UTF8);
				Log("structure: записан UserData/LBBB_RU/structure.txt");
			}
			catch (Exception ex7)
			{
				Log("structure err: " + ex7.Message);
			}
		}

		public static bool HasCyrillic(string s)
		{
			if (string.IsNullOrEmpty(s))
			{
				return false;
			}
			foreach (char c in s)
			{
				if (c >= 'Ѐ' && c <= 'ӿ')
				{
					return true;
				}
				if (c == '№')
				{
					return true;
				}
			}
			return false;
		}

		public static string TranslateVisible(string s)
		{
			if (string.IsNullOrEmpty(s) || s.Length < 4)
			{
				return s;
			}
			if (enToRu == null && uiTr == null)
			{
				return s;
			}
			if (uiTr != null && uiTr.TryGetValue(s, out var value) && !string.IsNullOrEmpty(value))
			{
				return value;
			}
			if (enToRu != null && enToRu.TryGetValue(s, out value) && !string.IsNullOrEmpty(value))
			{
				return value;
			}
			string text = Norm(s);
			if (!string.IsNullOrEmpty(text) && normToRu != null && normToRu.TryGetValue(text, out value) && !string.IsNullOrEmpty(value))
			{
				return value;
			}
			return s;
		}

		private static string Norm(string s)
		{
			if (string.IsNullOrEmpty(s))
			{
				return "";
			}
			StringBuilder stringBuilder = new StringBuilder(s.Length);
			int num = 0;
			int num2 = 0;
			foreach (char c in s)
			{
				switch (c)
				{
				case '[':
					num++;
					continue;
				case ']':
					if (num > 0)
					{
						num--;
					}
					continue;
				case '{':
					num2++;
					continue;
				case '}':
					if (num2 > 0)
					{
						num2--;
					}
					continue;
				}
				if (num <= 0 && num2 <= 0)
				{
					if (char.IsWhiteSpace(c))
					{
						stringBuilder.Append(' ');
					}
					else
					{
						stringBuilder.Append(char.ToLowerInvariant(c));
					}
				}
			}
			string text = stringBuilder.ToString().Trim();
			while (text.IndexOf("  ", StringComparison.Ordinal) >= 0)
			{
				text = text.Replace("  ", " ");
			}
			return text.Trim('"', '«', '»', '“', '”', ' ');
		}

		private static string CsvEscape(string s)
		{
			return "\"" + (s ?? "").Replace("\"", "\"\"").Replace("\r", "").Replace("\n", "\\n") + "\"";
		}

		private static void FontProbe()
		{
			//IL_0127: Unknown result type (might be due to invalid IL or missing references)
			//IL_012d: Invalid comparison between Unknown and I4
			if (probed)
			{
				return;
			}
			probed = true;
			try
			{
				Il2CppArrayBase<Font> val = Resources.FindObjectsOfTypeAll<Font>();
				Log("UnityEngine.Font assets: " + val.Length);
				foreach (Font item in val)
				{
					if (!((Object)(object)item == (Object)null))
					{
						string text = "";
						try
						{
							bool flag = item.HasCharacter('А');
							bool flag2 = item.HasCharacter('я');
							text = "hasA=" + flag + " hasYa=" + flag2;
						}
						catch (Exception ex)
						{
							text = "hasCharacter err " + ex.GetType().Name;
						}
						Log("  Font '" + ((Object)item).name + "' " + text);
					}
				}
			}
			catch (Exception ex2)
			{
				Log("font list err: " + ex2.Message);
			}
			try
			{
				foreach (TMP_FontAsset item2 in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
				{
					if ((Object)(object)item2 == (Object)null)
					{
						continue;
					}
					bool flag3 = false;
					try
					{
						flag3 = (int)item2.atlasPopulationMode == 1;
					}
					catch
					{
					}
					if (flag3)
					{
						string text2 = "";
						bool flag4 = false;
						try
						{
							flag4 = item2.TryAddCharacters("АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя", ref text2, false);
						}
						catch (Exception ex3)
						{
							text2 = "err:" + ex3.GetType().Name;
						}
						Log("  dynamic TMP '" + ((Object)item2).name + "' tryAdd=" + flag4 + " missing=" + ((text2 == null) ? "" : text2.Substring(0, Math.Min(20, text2.Length))));
					}
				}
			}
			catch (Exception ex4)
			{
				Log("tmp font list err: " + ex4.Message);
			}
		}

		private static void EnsureFont()
		{
			//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f9: Invalid comparison between Unknown and I4
			if (((Object)(object)ruFont != (Object)null && !ruFontIsTemp) || fontAttempts > 400)
			{
				return;
			}
			fontAttempts++;
			try
			{
				Font val = null;
				Il2CppArrayBase<Font> val2 = Resources.FindObjectsOfTypeAll<Font>();
				foreach (Font item in val2)
				{
					if (!((Object)(object)item == (Object)null) && ((Object)item).name == "3270SemiCondensed-Regular")
					{
						val = item;
						break;
					}
				}
				if ((Object)(object)val == (Object)null && val2.Length > 0)
				{
					val = val2[0];
				}
				if ((Object)(object)val != (Object)null)
				{
					TMP_FontAsset val3 = TMP_FontAsset.CreateFontAsset(val, 90, 9, (GlyphRenderMode)4165, 1024, 1024, (AtlasPopulationMode)1, true);
					if ((Object)(object)val3 != (Object)null)
					{
						string text = "";
						bool flag = val3.TryAddCharacters(RU_CHARS, ref text, false);
						bool flag2 = val3.HasCharacter('А', false, false);
						bool flag3 = val3.HasCharacter('я', false, false);
						Log("created from '" + ((Object)val).name + "': tryAdd=" + flag + " hasA=" + flag2 + " hasYa=" + flag3 + " missing=" + text);
						if (flag2 & flag3)
						{
							ruFont = val3;
							((Object)ruFont).name = "RU_3270_Dynamic";
							ruFontIsTemp = false;
							atlasHeated = false;
						}
					}
				}
			}
			catch (Exception ex)
			{
				Log("create from game font fail: " + ex.GetType().Name + " " + ex.Message);
			}
			if ((Object)(object)ruFont == (Object)null)
			{
				try
				{
					foreach (TMP_FontAsset item2 in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
					{
						if ((Object)(object)item2 == (Object)null)
						{
							continue;
						}
						bool flag4 = false;
						try
						{
							flag4 = (int)item2.atlasPopulationMode == 1;
						}
						catch
						{
						}
						if (flag4)
						{
							string value = "";
							bool flag5 = false;
							try
							{
								flag5 = item2.TryAddCharacters(RU_CHARS, ref value, false);
							}
							catch
							{
							}
							if (flag5 && string.IsNullOrEmpty(value))
							{
								ruFont = item2;
								Log("fallback dynamic font: " + ((Object)item2).name);
								break;
							}
						}
					}
				}
				catch (Exception ex2)
				{
					Log("fallback font err: " + ex2.Message);
				}
			}
			if ((Object)(object)ruFont == (Object)null)
			{
				string[] array = new string[4] { "Consolas", "Segoe UI", "Arial", "Tahoma" };
				string text3 = default;
				foreach (string text2 in array)
				{
					try
					{
						Font val4 = Font.CreateDynamicFontFromOSFont(text2, 90);
						if (!((Object)(object)val4 == (Object)null))
						{
							TMP_FontAsset val5 = TMP_FontAsset.CreateFontAsset(val4, 90, 9, (GlyphRenderMode)4165, 1024, 1024, (AtlasPopulationMode)1, true);
							if (!((Object)(object)val5 == (Object)null) && val5.TryAddCharacters(RU_CHARS, ref text3, false))
							{
								ruFont = val5;
								((Object)ruFont).name = "RU_os_" + text2;
								ruFontIsTemp = true;
								atlasHeated = false;
								Log("временный шрифт из системы: " + text2);
								break;
							}
						}
					}
					catch
					{
					}
				}
				if ((Object)(object)ruFont == (Object)null)
				{
					Log("NO RUSSIAN FONT AVAILABLE");
				}
			}
			if ((Object)(object)ruFont != (Object)null && !ruFontIsTemp)
			{
				Log("ru font set: " + ((Object)ruFont).name);
			}
		}

		public static void EnsureFontFor(TMP_Text t, string text)
		{
			if ((Object)(object)t == (Object)null || (Object)(object)ruFont == (Object)null || inFontSwap || !HasCyrillic(text))
			{
				return;
			}
			TMP_FontAsset val = null;
			try
			{
				val = t.font;
			}
			catch
			{
			}
			if ((Object)(object)val == (Object)(object)ruFont)
			{
				return;
			}
			inFontSwap = true;
			try
			{
				t.font = ruFont;
				try
				{
					t.fontSharedMaterial = ((TMP_Asset)ruFont).material;
				}
				catch
				{
				}
				fontSwaps++;
			}
			catch
			{
			}
			finally
			{
				inFontSwap = false;
			}
		}

		private static void PreheatAtlas()
		{
			if ((Object)(object)ruFont == (Object)null || atlasHeated)
			{
				return;
			}
			atlasHeated = true;
			try
			{
				HashSet<char> hashSet = new HashSet<char>();
				if (tr != null)
				{
					foreach (string value in tr.Values)
					{
						if (value == null)
						{
							continue;
						}
						string text = value;
						foreach (char c in text)
						{
							if (c >= ' ')
							{
								hashSet.Add(c);
							}
						}
					}
				}
				if (uiTr != null)
				{
					foreach (string value2 in uiTr.Values)
					{
						if (value2 == null)
						{
							continue;
						}
						string text = value2;
						foreach (char c2 in text)
						{
							if (c2 >= ' ')
							{
								hashSet.Add(c2);
							}
						}
					}
				}
				StringBuilder stringBuilder = new StringBuilder(hashSet.Count);
				foreach (char item in hashSet)
				{
					stringBuilder.Append(item);
				}
				bool flag = false;
				string text2 = default;
				try
				{
					flag = ruFont.TryAddCharacters(stringBuilder.ToString(), ref text2, false);
				}
				catch (Exception ex)
				{
					Log("preheat call err: " + ex.Message);
					return;
				}
				Log("atlas preheat: символов=" + hashSet.Count + " ok=" + flag + " осталось=" + ((text2 == null) ? "0" : text2.Length.ToString()));
			}
			catch (Exception ex2)
			{
				Log("preheat err: " + ex2.Message);
			}
		}

		private static void AttachFallback()
		{
			if ((Object)(object)ruFont == (Object)null || fallbackPass > 30)
			{
				return;
			}
			fallbackPass++;
			try
			{
				Il2CppArrayBase<TMP_FontAsset> val = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
				if (val == null)
				{
					return;
				}
				int num = 0;
				foreach (TMP_FontAsset item in val)
				{
					if ((Object)(object)item == (Object)null || (Object)(object)item == (Object)(object)ruFont)
					{
						continue;
					}
					try
					{
						List<TMP_FontAsset> val2 = item.fallbackFontAssetTable;
						if (val2 == null)
						{
							val2 = (item.fallbackFontAssetTable = new List<TMP_FontAsset>());
						}
						bool flag = false;
						for (int i = 0; i < val2.Count; i++)
						{
							if ((Object)(object)val2[i] == (Object)(object)ruFont)
							{
								flag = true;
								break;
							}
						}
						if (!flag)
						{
							val2.Add(ruFont);
							num++;
						}
					}
					catch
					{
					}
				}
				if (num > 0 && fallbackPass < 3)
				{
					Log("fallback: наш шрифт подключён к " + num + " шрифтам игры");
				}
			}
			catch
			{
			}
		}

		private static void DumpFonts()
		{
			try
			{
				Il2CppArrayBase<TMP_Text> val = Resources.FindObjectsOfTypeAll<TMP_Text>();
				int num = 0;
				int num2 = 0;
				int num3 = 0;
				StringBuilder stringBuilder = new StringBuilder();
				HashSet<int> hashSet = new HashSet<int>();
				foreach (TMP_Text item in val)
				{
					if ((Object)(object)item == (Object)null)
					{
						continue;
					}
					string text = null;
					try
					{
						text = item.text;
					}
					catch
					{
					}
					if (string.IsNullOrEmpty(text) || !HasCyrillic(text))
					{
						continue;
					}
					num++;
					TMP_FontAsset val2 = null;
					try
					{
						val2 = item.font;
					}
					catch
					{
					}
					if ((Object)(object)val2 == (Object)(object)ruFont)
					{
						num2++;
						continue;
					}
					num3++;
					string text2 = "null";
					string text3 = "?";
					try
					{
						text2 = (((Object)(object)val2 == (Object)null) ? "null" : ((Object)val2).name);
					}
					catch
					{
					}
					try
					{
						text3 = ((Object)((Component)item).gameObject).name;
					}
					catch
					{
					}
					bool flag = false;
					try
					{
						flag = (Object)(object)val2 != (Object)null && val2.HasCharacter('А', false, false);
					}
					catch
					{
					}
					if (hashSet.Add(text2.GetHashCode()) && stringBuilder.Length < 900)
					{
						stringBuilder.Append("\n  чужой шрифт " + text2 + " (есть А=" + flag + "), пример " + text3 + ": " + ((text.Length > 40) ? text.Substring(0, 40) : text));
					}
				}
				Il2CppArrayBase<Text> val3 = Resources.FindObjectsOfTypeAll<Text>();
				int num4 = 0;
				StringBuilder stringBuilder2 = new StringBuilder();
				foreach (Text item2 in val3)
				{
					if ((Object)(object)item2 == (Object)null)
					{
						continue;
					}
					string text4 = null;
					try
					{
						text4 = item2.text;
					}
					catch
					{
					}
					if (string.IsNullOrEmpty(text4) || !HasCyrillic(text4))
					{
						continue;
					}
					num4++;
					if (stringBuilder2.Length < 200)
					{
						try
						{
							stringBuilder2.Append(" " + ((Object)((Component)item2).gameObject).name);
						}
						catch
						{
						}
					}
				}
				Log("диагностика шрифтов: TMP с кириллицей=" + num + ", на нашем=" + num2 + ", на чужом=" + num3 + ", legacy Text с кириллицей=" + num4 + stringBuilder2?.ToString() + stringBuilder);
			}
			catch (Exception ex)
			{
				Log("dump fonts err: " + ex.Message);
			}
		}

		private static Font GetUnityFont()
		{
			if ((Object)(object)ruUnityFont != (Object)null)
			{
				return ruUnityFont;
			}
			try
			{
				foreach (Font item in Resources.FindObjectsOfTypeAll<Font>())
				{
					if (!((Object)(object)item == (Object)null))
					{
						bool flag = false;
						try
						{
							flag = item.HasCharacter('А') && item.HasCharacter('я');
						}
						catch
						{
						}
						if (flag)
						{
							ruUnityFont = item;
							return ruUnityFont;
						}
					}
				}
			}
			catch
			{
			}
			try
			{
				ruUnityFont = Font.CreateDynamicFontFromOSFont("Consolas", 32);
			}
			catch
			{
			}
			if ((Object)(object)ruUnityFont == (Object)null)
			{
				try
				{
					ruUnityFont = Font.CreateDynamicFontFromOSFont("Arial", 32);
				}
				catch
				{
				}
			}
			return ruUnityFont;
		}

		private static int FixLegacyTexts()
		{
			int num = 0;
			try
			{
				foreach (Text item in Resources.FindObjectsOfTypeAll<Text>())
				{
					if ((Object)(object)item == (Object)null)
					{
						continue;
					}
					string text = null;
					try
					{
						text = item.text;
					}
					catch
					{
					}
					if (string.IsNullOrEmpty(text) || !HasCyrillic(text))
					{
						continue;
					}
					Font font = item.font;
					bool flag = false;
					try
					{
						flag = (Object)(object)font != (Object)null && font.HasCharacter('А') && font.HasCharacter('я');
					}
					catch
					{
					}
					if (flag)
					{
						continue;
					}
					Font unityFont = GetUnityFont();
					if (!((Object)(object)unityFont == (Object)null))
					{
						try
						{
							item.font = unityFont;
							num++;
						}
						catch
						{
						}
					}
				}
			}
			catch
			{
			}
			return num;
		}

		private static void DumpTexts()
		{
			//IL_0108: Unknown result type (might be due to invalid IL or missing references)
			//IL_010d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0114: Unknown result type (might be due to invalid IL or missing references)
			//IL_0130: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				Dictionary<string, string> dictionary = LocalizationRU.TableMap();
				StringBuilder stringBuilder = new StringBuilder();
				Il2CppArrayBase<TMP_Text> val = Resources.FindObjectsOfTypeAll<TMP_Text>();
				int num = 0;
				int num2 = 0;
				foreach (TMP_Text item in val)
				{
					if ((Object)(object)item == (Object)null)
					{
						continue;
					}
					string text = null;
					try
					{
						text = item.text;
					}
					catch
					{
						continue;
					}
					if (string.IsNullOrEmpty(text))
					{
						continue;
					}
					bool flag = false;
					try
					{
						flag = ((Component)item).gameObject.activeInHierarchy;
					}
					catch
					{
					}
					if (flag)
					{
						num++;
					}
					bool flag2 = HasCyrillic(text);
					bool flag3 = false;
					try
					{
						flag3 = dictionary.ContainsKey(text) || dictionary.ContainsKey(text.Trim());
					}
					catch
					{
					}
					bool flag4 = false;
					string text2 = text;
					foreach (char c in text2)
					{
						if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
						{
							flag4 = true;
							break;
						}
					}
					if (!flag4 && !flag2)
					{
						continue;
					}
					if (flag4)
					{
						num2++;
					}
					string text3 = "";
					try
					{
						Vector3 position = item.transform.position;
						text3 = " @" + (int)position.x + "," + (int)((float)Screen.height - position.y);
					}
					catch
					{
					}
					string text4 = "";
					try
					{
						Transform val2 = item.transform;
						int num3 = 0;
						while ((Object)(object)val2 != (Object)null && num3++ < 6)
						{
							text4 = ((Object)val2).name + "/" + text4;
							val2 = val2.parent;
						}
					}
					catch
					{
					}
					stringBuilder.AppendLine((flag ? "[видно] " : "[скрыто] ") + (flag3 ? "[есть перевод] " : "[НЕТ перевода] ") + text.Replace("\n", " / ") + text3 + " :: " + text4);
				}
				string text5 = Path.Combine(ModsDir, "LBBB_RU", "texts_on_screen.txt");
				File.WriteAllText(text5, "видимых текстов: " + num + ", латиницей: " + num2 + "\n\n" + stringBuilder, Encoding.UTF8);
				Log("снимок текстов: видимых " + num + ", латиницей " + num2 + " -> " + text5);
			}
			catch (Exception ex)
			{
				Log("снимок текстов: " + ex.Message);
			}
		}

		private static Dictionary<string, string> ReverseUiMap()
		{
			if (ruToEn != null)
			{
				return ruToEn;
			}
			Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
			try
			{
				if (uiTr != null)
				{
					foreach (KeyValuePair<string, string> item in uiTr)
					{
						if (!string.IsNullOrEmpty(item.Value) && !dictionary.ContainsKey(item.Value))
						{
							dictionary[item.Value] = item.Key;
						}
					}
				}
				if (enToRu != null)
				{
					foreach (KeyValuePair<string, string> item2 in enToRu)
					{
						if (!string.IsNullOrEmpty(item2.Value) && !dictionary.ContainsKey(item2.Value))
						{
							dictionary[item2.Value] = item2.Key;
						}
					}
				}
				Dictionary<string, string> dictionary2 = LocalizationRU.TableMap();
				if (dictionary2 != null)
				{
					foreach (KeyValuePair<string, string> item3 in dictionary2)
					{
						if (!string.IsNullOrEmpty(item3.Value) && !dictionary.ContainsKey(item3.Value))
						{
							dictionary[item3.Value] = item3.Key;
						}
					}
				}
			}
			catch
			{
			}
			ruToEn = dictionary;
			return dictionary;
		}

		private static void RestoreEnglishTexts()
		{
			try
			{
				Dictionary<string, string> dictionary = ReverseUiMap();
				if (dictionary.Count == 0)
				{
					return;
				}
				int num = 0;
				foreach (TMP_Text item in Resources.FindObjectsOfTypeAll<TMP_Text>())
				{
					if ((Object)(object)item == (Object)null)
					{
						continue;
					}
					string text;
					try
					{
						text = item.text;
					}
					catch
					{
						continue;
					}
					if (!string.IsNullOrEmpty(text) && text.Length <= 300 && dictionary.TryGetValue(text, out var value) && !string.IsNullOrEmpty(value))
					{
						try
						{
							item.text = value;
							num++;
						}
						catch
						{
						}
					}
				}
				if (num > 0)
				{
					Log("ушли с русского: вернул английских строк " + num);
				}
			}
			catch (Exception ex)
			{
				Log("restore err: " + ex.Message);
			}
		}

		private static void ApplyTextMap()
		{
			try
			{
				if (Localization.language != "ru")
				{
					return;
				}
			}
			catch
			{
				return;
			}
			try
			{
				Dictionary<string, string> dictionary = LocalizationRU.TableMap();
				if (dictionary == null || dictionary.Count == 0)
				{
					return;
				}
				int num = 0;
				foreach (TMP_Text item in Resources.FindObjectsOfTypeAll<TMP_Text>())
				{
					if ((Object)(object)item == (Object)null)
					{
						continue;
					}
					string text = null;
					try
					{
						text = item.text;
					}
					catch
					{
						continue;
					}
					if (string.IsNullOrEmpty(text) || text.Length > 300)
					{
						continue;
					}
					if (!dictionary.TryGetValue(text, out var value))
					{
						string text2 = text.Trim();
						if (text2.Length == text.Length || !dictionary.TryGetValue(text2, out value))
						{
							continue;
						}
					}
					if (!string.IsNullOrEmpty(value) && !(value == text))
					{
						try
						{
							item.text = value;
							num++;
						}
						catch
						{
						}
					}
				}
				textReplacements += num;
			}
			catch
			{
			}
		}

		private static void SwapFonts()
		{
			if ((Object)(object)ruFont == (Object)null)
			{
				return;
			}
			try
			{
				Il2CppArrayBase<TMP_Text> val = Resources.FindObjectsOfTypeAll<TMP_Text>();
				lastTexts = val.Length;
				foreach (TMP_Text item in val)
				{
					if ((Object)(object)item == (Object)null)
					{
						continue;
					}
					TMP_FontAsset font = item.font;
					if ((Object)(object)font == (Object)null || (Object)(object)font == (Object)(object)ruFont || !HasCyrillic(item.text))
					{
						continue;
					}
					bool flag = false;
					try
					{
						flag = font.HasCharacter('А', false, false) && font.HasCharacter('а', false, false);
					}
					catch
					{
					}
					if (!flag)
					{
						item.font = ruFont;
						try
						{
							item.fontSharedMaterial = ((TMP_Asset)ruFont).material;
						}
						catch
						{
						}
						fontSwaps++;
					}
				}
				int num = FixLegacyTexts();
				if (num > 0)
				{
					Log("старый UI: шрифт с кириллицей подставлен в " + num + " текстов");
				}
			}
			catch (Exception ex)
			{
				Log("swap err: " + ex.Message);
			}
		}
	}
	internal static class Credits
	{
		private const string BoostyUrl = "https://boosty.to/coreforgelabs";

		private const float TabW = 150f;

		private const float TabH = 44f;

		private const float CardW = 660f;

		private const float CardH = 312f;

		private const float ListW = 520f;

		private const float TabX = 1730f;

		private const float TabY = 518f;

		private const float CardX = 1050f;

		private const float CardY = 384f;

		private static readonly Color CardBg = new Color(0.043f, 0.055f, 0.04f, 0.94f);

		private static readonly Color Line = new Color(0.33f, 0.4f, 0.3f, 0.85f);

		private static readonly Color Accent = new Color(0.8f, 0.67f, 0.45f, 1f);

		private static readonly Color Body = new Color(0.66f, 0.73f, 0.62f, 1f);

		private static readonly Color Muted = new Color(0.52f, 0.6f, 0.48f, 1f);

		private static GameObject canvasGo;

		private static GameObject tabGo;

		private static GameObject cardGo;

		private static GameObject listGo;

		private static TMP_Text[] texts = new TMP_Text[0];

		private static float lastCheck;

		private static float hideAt = -1f;

		private static float builtAt = -1f;

		private static bool built;

		private static bool cardShown;

		private static bool listShown;

		private static bool diagDone;

		private static string crewBody = "";

		private static string menuScene = null;

		private static GameObject menuRoot;

		private static int people;

		private static int listH;

		private static LbbbMainMenuWorld[] menuWorlds;

		private static float lastScan = -1f;

		private static bool menuWorldOn;

		private static bool gameUiOn;

		private static int menuWorldCount;

		private static List<string> menuNames;

		private static Material cachedMat;

		public static void Tick()
		{
			float unscaledTime = Time.unscaledTime;
			if (unscaledTime - lastCheck < 0.25f)
			{
				return;
			}
			lastCheck = unscaledTime;
			try
			{
				string path = Path.Combine(Core.ModsDirPublic, "LBBB_RU");
				if (File.Exists(Path.Combine(path, "credits_show.txt")))
				{
					File.Delete(Path.Combine(path, "credits_show.txt"));
					ShowCard(on: true);
					ShowList(on: true);
					hideAt = unscaledTime + 20f;
				}
				if (File.Exists(Path.Combine(path, "credits_shot.txt")))
				{
					File.Delete(Path.Combine(path, "credits_shot.txt"));
					try
					{
						string text = Path.Combine(path, "shot.png");
						ScreenCapture.CaptureScreenshot(text);
						Core.Log("снимок экрана: " + text);
					}
					catch (Exception ex)
					{
						Core.Log("снимок: " + ex.Message);
					}
				}
			}
			catch
			{
			}
			RefreshState();
			if ((!MenuWorldEnabled() && !MenuIsUpFallback()) || GameUiEnabled())
			{
				if (built)
				{
					Core.Log("плашка «Экипаж»: убрана, сцена=" + SceneName() + ", меню-мир=" + MenuWorldEnabled() + ", найдено=" + MenuWorldCount() + ", игровой-интерфейс=" + GameUiEnabled());
					Destroy();
				}
				return;
			}
			if (!built)
			{
				Build();
				if (!built)
				{
					return;
				}
			}
			TMP_FontAsset ruFont = Core.RuFont;
			if ((Object)(object)ruFont != (Object)null)
			{
				for (int i = 0; i < texts.Length; i++)
				{
					TMP_Text val = texts[i];
					if (!Alive((Object)(object)val))
					{
						continue;
					}
					try
					{
						if ((Object)(object)val.font != (Object)(object)ruFont)
						{
							val.font = ruFont;
							val.fontSharedMaterial = FontMaterial(ruFont);
						}
					}
					catch
					{
					}
				}
			}
			if (!diagDone && builtAt > 0f && unscaledTime > builtAt + 2f)
			{
				diagDone = true;
				Diag();
			}
			if (hideAt > 0f && unscaledTime > hideAt)
			{
				hideAt = -1f;
				ShowCard(on: false);
				ShowList(on: false);
			}
		}

		private static void Diag()
		{
			//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00db: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
			//IL_0139: Unknown result type (might be due to invalid IL or missing references)
			//IL_013e: Unknown result type (might be due to invalid IL or missing references)
			//IL_015e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0163: Unknown result type (might be due to invalid IL or missing references)
			//IL_0183: Unknown result type (might be due to invalid IL or missing references)
			//IL_0188: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				Canvas component = canvasGo.GetComponent<Canvas>();
				RectTransform component2 = canvasGo.GetComponent<RectTransform>();
				RectTransform component3 = tabGo.GetComponent<RectTransform>();
				TMP_FontAsset ruFont = Core.RuFont;
				Core.Log("плашка: карусель=" + LocalizationRU.CarouselPath() + " | меню-мир=" + MenuWorldEnabled() + " найдено=" + MenuWorldCount() + " игровой-интерфейс=" + GameUiEnabled());
				string[] array = new string[30]
				{
					"плашка: экран=",
					Screen.width.ToString(),
					"x",
					Screen.height.ToString(),
					" канва=",
					null,
					null,
					null,
					null,
					null,
					null,
					null,
					null,
					null,
					null,
					null,
					null,
					null,
					null,
					null,
					null,
					null,
					null,
					null,
					null,
					null,
					null,
					null,
					null,
					null
				};
				Rect rect = component2.rect;
				array[5] = rect.width.ToString();
				array[6] = "x";
				rect = component2.rect;
				array[7] = rect.height.ToString();
				array[8] = " scale=";
				array[9] = component.scaleFactor.ToString();
				array[10] = " | плашка рект=";
				rect = component3.rect;
				array[11] = rect.width.ToString();
				array[12] = "x";
				rect = component3.rect;
				array[13] = rect.height.ToString();
				array[14] = " поз=";
				array[15] = ((Transform)component3).position.x.ToString("F0");
				array[16] = ",";
				array[17] = ((Transform)component3).position.y.ToString("F0");
				array[18] = " | шрифт=";
				array[19] = (((Object)(object)ruFont == (Object)null) ? "нет" : ((Object)ruFont).name);
				array[20] = " материал=";
				array[21] = (((Object)(object)ruFont == (Object)null || (Object)(object)((TMP_Asset)ruFont).material == (Object)null) ? "null" : ((Object)((TMP_Asset)ruFont).material).name);
				array[22] = " | текстов=";
				array[23] = texts.Length.ToString();
				array[24] = " ников=";
				array[25] = people.ToString();
				array[26] = " карточка=";
				array[27] = cardShown.ToString();
				array[28] = " список=";
				array[29] = listShown.ToString();
				Core.Log(string.Concat(array));
			}
			catch (Exception ex)
			{
				Core.Log("плашка: диагностика: " + ex.Message);
			}
		}

		public static void RefreshState()
		{
			float unscaledTime = Time.unscaledTime;
			if (unscaledTime - lastScan < 1f)
			{
				return;
			}
			lastScan = unscaledTime;
			menuWorldOn = false;
			gameUiOn = false;
			menuWorldCount = 0;
			try
			{
				menuWorlds = Il2CppArrayBase<LbbbMainMenuWorld>.op_Implicit(Resources.FindObjectsOfTypeAll<LbbbMainMenuWorld>());
				if (menuWorlds != null)
				{
					LbbbMainMenuWorld[] array = menuWorlds;
					foreach (LbbbMainMenuWorld val in array)
					{
						if (!((Object)(object)val == (Object)null))
						{
							menuWorldCount++;
							if (((Behaviour)val).enabled && ((Component)val).gameObject.activeInHierarchy)
							{
								menuWorldOn = true;
								break;
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Core.Log("плашка: меню-мир: " + ex.Message);
			}
			try
			{
				Il2CppArrayBase<LbbbPois> val2 = Resources.FindObjectsOfTypeAll<LbbbPois>();
				if (val2 != null)
				{
					foreach (LbbbPois item in val2)
					{
						if ((Object)(object)item != (Object)null && ((Behaviour)item).enabled && ((Component)item).gameObject.activeInHierarchy)
						{
							gameUiOn = true;
							break;
						}
					}
				}
			}
			catch
			{
			}
			try
			{
				Il2CppArrayBase<LbbbTaskPanel> val3 = Resources.FindObjectsOfTypeAll<LbbbTaskPanel>();
				if (val3 == null)
				{
					return;
				}
				foreach (LbbbTaskPanel item2 in val3)
				{
					if ((Object)(object)item2 != (Object)null && ((Behaviour)item2).enabled && ((Component)item2).gameObject.activeInHierarchy)
					{
						gameUiOn = true;
						break;
					}
				}
			}
			catch
			{
			}
		}

		public static bool MenuWorldEnabled()
		{
			return menuWorldOn;
		}

		public static bool GameUiEnabled()
		{
			return gameUiOn;
		}

		public static int MenuWorldCount()
		{
			return menuWorldCount;
		}

		public static bool MenuIsUpFallback()
		{
			if (menuWorldCount == 0)
			{
				return LocalizationRU.MenuIsUp();
			}
			return false;
		}

		private static bool MenuPresent()
		{
			//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				if ((Object)(object)menuRoot != (Object)null && Alive((Object)(object)menuRoot))
				{
					return true;
				}
				menuRoot = null;
				if (menuNames == null)
				{
					menuNames = new List<string>();
				}
				Il2CppArrayBase<Canvas> val = Resources.FindObjectsOfTypeAll<Canvas>();
				if (val == null)
				{
					return false;
				}
				foreach (Canvas item in val)
				{
					if ((Object)(object)item == (Object)null)
					{
						continue;
					}
					string text = null;
					try
					{
						text = ((Object)item).name;
					}
					catch
					{
					}
					if (string.IsNullOrEmpty(text))
					{
						continue;
					}
					if (menuNames.Count < 24 && !menuNames.Contains(text))
					{
						menuNames.Add(text);
					}
					if (text.IndexOf("Main Menu", StringComparison.OrdinalIgnoreCase) < 0)
					{
						continue;
					}
					bool flag = false;
					try
					{
						int num;
						if (((Behaviour)item).isActiveAndEnabled)
						{
							Scene scene = ((Component)item).gameObject.scene;
							num = (scene.IsValid() ? 1 : 0);
						}
						else
						{
							num = 0;
						}
						flag = (byte)num != 0;
					}
					catch
					{
					}
					if (flag)
					{
						menuRoot = ((Component)item).gameObject;
						return true;
					}
				}
			}
			catch
			{
			}
			return false;
		}

		private static string SceneName()
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				Scene activeScene = SceneManager.GetActiveScene();
				return activeScene.name;
			}
			catch
			{
				return "?";
			}
		}

		private static bool Alive(Object o)
		{
			try
			{
				return o != (Object)null && o.GetInstanceID() != 0;
			}
			catch
			{
				return false;
			}
		}

		private static void Destroy()
		{
			try
			{
				if (Alive((Object)(object)canvasGo))
				{
					Object.Destroy((Object)(object)canvasGo);
				}
			}
			catch
			{
			}
			canvasGo = null;
			tabGo = null;
			cardGo = null;
			listGo = null;
			texts = new TMP_Text[0];
			built = false;
			cardShown = false;
			listShown = false;
			hideAt = -1f;
		}

		private static Material FontMaterial(TMP_FontAsset f)
		{
			//IL_0093: Unknown result type (might be due to invalid IL or missing references)
			//IL_009a: Expected Obj, but got Unknown
			if ((Object)(object)cachedMat != (Object)null)
			{
				return cachedMat;
			}
			try
			{
				if ((Object)(object)((TMP_Asset)f).material != (Object)null)
				{
					cachedMat = ((TMP_Asset)f).material;
					return cachedMat;
				}
			}
			catch
			{
			}
			try
			{
				foreach (TMP_Text item in Resources.FindObjectsOfTypeAll<TMP_Text>())
				{
					if ((Object)(object)item == (Object)null)
					{
						continue;
					}
					Material fontSharedMaterial = item.fontSharedMaterial;
					if (!((Object)(object)fontSharedMaterial == (Object)null) && !((Object)(object)fontSharedMaterial.shader == (Object)null) && ((Object)fontSharedMaterial.shader).name.IndexOf("TMP", StringComparison.OrdinalIgnoreCase) >= 0)
					{
						Material val = new Material(fontSharedMaterial);
						if ((Object)(object)f.atlasTexture != (Object)null)
						{
							val.SetTexture("_MainTex", (Texture)(object)f.atlasTexture);
						}
						cachedMat = val;
						return cachedMat;
					}
				}
			}
			catch
			{
			}
			return null;
		}

		private static string[] ReadSupporters(out int total)
		{
			List<string> list = new List<string>();
			List<string> list2 = new List<string>();
			Dictionary<string, List<string>> dictionary = new Dictionary<string, List<string>>();
			total = 0;
			try
			{
				string path = Path.Combine(Core.ModsDirPublic, "LBBB_RU", "boosty.csv");
				if (File.Exists(path))
				{
					string[] array = File.ReadAllLines(path, Encoding.UTF8);
					foreach (string text in array)
					{
						if (string.IsNullOrWhiteSpace(text))
						{
							continue;
						}
						string[] array2 = text.Split(';');
						if (array2.Length < 2)
						{
							continue;
						}
						string text2 = array2[0].Trim();
						string text3 = array2[1].Trim();
						if (text2.Length != 0 && text3.Length != 0 && !text2.Equals("tier", StringComparison.OrdinalIgnoreCase))
						{
							if (!dictionary.ContainsKey(text2))
							{
								dictionary[text2] = new List<string>();
								list2.Add(text2);
							}
							dictionary[text2].Add(text3);
							total++;
						}
					}
				}
			}
			catch (Exception ex)
			{
				Core.Log("boosty.csv: " + ex.Message);
			}
			foreach (string item in list2)
			{
				list.Add("<b><color=#C7A97B>" + item + "</color></b>");
				foreach (string item2 in dictionary[item])
				{
					list.Add("<color=#9CB392>" + item2 + "</color>");
				}
				list.Add(" ");
			}
			return list.ToArray();
		}

		private static void Build()
		{
			//IL_005f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0069: Expected Obj, but got Unknown
			//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
			//IL_0144: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
			//IL_022a: Unknown result type (might be due to invalid IL or missing references)
			//IL_022f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0234: Unknown result type (might be due to invalid IL or missing references)
			//IL_02d0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0309: Unknown result type (might be due to invalid IL or missing references)
			//IL_0342: Unknown result type (might be due to invalid IL or missing references)
			//IL_037b: Unknown result type (might be due to invalid IL or missing references)
			//IL_03c3: Unknown result type (might be due to invalid IL or missing references)
			//IL_03dc: Unknown result type (might be due to invalid IL or missing references)
			//IL_03e1: Unknown result type (might be due to invalid IL or missing references)
			//IL_0460: Unknown result type (might be due to invalid IL or missing references)
			//IL_0465: Unknown result type (might be due to invalid IL or missing references)
			//IL_046a: Unknown result type (might be due to invalid IL or missing references)
			//IL_052b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0550: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				string[] array = ReadSupporters(out people);
				crewBody = ((array.Length != 0) ? string.Join("\n", array) : "<color=#9CB392>список не найден</color>");
				int num = Math.Max(1, crewBody.Split('\n').Length);
				listH = (int)Math.Min(900f, (float)num * 25f + 160f);
				canvasGo = new GameObject("LBBB_RU_Credits");
				Object.DontDestroyOnLoad((Object)(object)canvasGo);
				Canvas val = canvasGo.AddComponent<Canvas>();
				val.renderMode = (RenderMode)0;
				val.sortingOrder = 32760;
				CanvasScaler val2 = canvasGo.AddComponent<CanvasScaler>();
				val2.uiScaleMode = (ScaleMode)1;
				val2.referenceResolution = new Vector2(1920f, 1080f);
				val2.matchWidthOrHeight = 0.5f;
				canvasGo.AddComponent<GraphicRaycaster>();
				float y = 540f - (float)listH / 2f;
				listGo = Box("CrewList", canvasGo.transform, 510f, y, 520f, listH);
				Panel(listGo);
				Label("ListTitle", listGo.transform, 22f, (float)listH - 46f, 476f, 30f, "ЭКИПАЖ", 22f, Accent, wrap: false);
				Label("ListSub", listGo.transform, 22f, (float)listH - 74f, 476f, 24f, people + " " + PeopleWord(people) + " ПОДДЕРЖАЛИ МОД", 15f, Muted, wrap: false);
				Label("ListBody", listGo.transform, 22f, 74f, 476f, (float)listH - 150f, crewBody, 19f, Body, wrap: false);
				((UnityEvent)Button("CrewClose", listGo.transform, 22f, 16f, 476f, 44f, "ЗАКРЫТЬ", new Color(0.1f, 0.14f, 0.09f, 0.96f), Muted, Line).onClick).AddListener(UnityAction.op_Implicit((Action)(() =>
				{
					ShowList(on: false);
					ShowCard(on: false);
				})));
				cardGo = Box("CreditsCard", canvasGo.transform, 1050f, 384f, 660f, 312f);
				Panel(cardGo);
				Label("CardTitle", cardGo.transform, 24f, 258f, 612f, 30f, "CFLABS — РУСИФИКАЦИЯ LET BIONS BE BYGONES", 21f, Accent, wrap: false);
				Label("CardL1", cardGo.transform, 24f, 222f, 612f, 28f, "Подписывайся на Boosty! Мод живёт благодаря вашей поддержке.", 19f, Body, wrap: true);
				Label("CardL2", cardGo.transform, 24f, 164f, 612f, 54f, "Голосуй за проекты, присылай баг-репорты — автор рад любой помощи :)", 19f, Body, wrap: true);
				Label("CardL3", cardGo.transform, 24f, 106f, 612f, 54f, "P.S. Разработкой модификации занимается один человек. Удачи, капитан!", 19f, Muted, wrap: true);
				Button val3 = Button("Boosty", cardGo.transform, 24f, 24f, 372f, 58f, "ПОДДЕРЖАТЬ НА BOOSTY", new Color(0.62f, 0.47f, 0.24f, 0.98f), new Color(0.05f, 0.06f, 0.04f, 1f), Accent);
				((UnityEvent)val3.onClick).AddListener(UnityAction.op_Implicit((Action)(() =>
				{
					Core.Log("Boosty: открываю https://boosty.to/coreforgelabs");
					try
					{
						Application.OpenURL("https://boosty.to/coreforgelabs");
					}
					catch (Exception ex2)
					{
						Core.Log("OpenURL: " + ex2.Message);
					}
				})));
				Hover(((Component)val3).gameObject);
				Button val4 = Button("Crew", cardGo.transform, 412f, 24f, 224f, 58f, "ЭКИПАЖ", new Color(0.1f, 0.14f, 0.09f, 0.96f), Muted, Line);
				((UnityEvent)val4.onClick).AddListener(UnityAction.op_Implicit((Action)(() =>
				{
					ShowList(!listShown);
				})));
				Hover(((Component)val4).gameObject);
				tabGo = Box("CreditsTab", canvasGo.transform, 1730f, 518f, 150f, 44f);
				Panel(tabGo);
				Button val5 = tabGo.AddComponent<Button>();
				((Selectable)val5).targetGraphic = (Graphic)(object)tabGo.GetComponent<Image>();
				TMP_Text val6 = Label("TabText", tabGo.transform, 0f, 0f, 150f, 44f, "ЭКИПАЖ", 18f, Accent, wrap: false);
				val6.alignment = (TextAlignmentOptions)514;
				((Component)val6).GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 0f);
				((UnityEvent)val5.onClick).AddListener(UnityAction.op_Implicit((Action)(() =>
				{
					ShowCard(!cardShown);
					if (!cardShown)
					{
						ShowList(on: false);
					}
				})));
				Hover(tabGo);
				tabGo.transform.SetAsLastSibling();
				ShowCard(on: false);
				ShowList(on: false);
				built = true;
				menuScene = SceneName();
				builtAt = Time.unscaledTime;
				diagDone = false;
				Core.Log("плашка «Экипаж»: построена в сцене " + menuScene + ", ников=" + people + ", список " + listH + "px");
			}
			catch (Exception ex)
			{
				Core.Log("плашка «Экипаж»: ошибка " + ex.Message);
				built = false;
			}
		}

		private static string PeopleWord(int n)
		{
			int num = Math.Abs(n) % 100;
			int num2 = num % 10;
			if (num > 10 && num < 20)
			{
				return "ЧЕЛОВЕК";
			}
			switch (num2)
			{
			case 1:
				return "ЧЕЛОВЕК";
			case 2:
			case 3:
			case 4:
				return "ЧЕЛОВЕКА";
			default:
				return "ЧЕЛОВЕК";
			}
		}

		private static void ShowCard(bool on)
		{
			cardShown = on;
			hideAt = -1f;
			try
			{
				if (Alive((Object)(object)cardGo))
				{
					cardGo.SetActive(on);
				}
			}
			catch
			{
			}
		}

		private static void ShowList(bool on)
		{
			listShown = on;
			hideAt = -1f;
			try
			{
				if (Alive((Object)(object)listGo))
				{
					listGo.SetActive(on);
				}
			}
			catch
			{
			}
		}

		private static void Hover(GameObject go)
		{
			try
			{
				EventTrigger val = go.AddComponent<EventTrigger>();
				if (val.triggers == null)
				{
					val.triggers = new List<Entry>();
				}
				Add(val, (EventTriggerType)0, UnityAction<BaseEventData>.op_Implicit((Action<BaseEventData>)((BaseEventData d) =>
				{
					ShowCard(on: true);
					_ = (Object)(object)go == (Object)(object)tabGo;
				})));
				Add(val, (EventTriggerType)1, UnityAction<BaseEventData>.op_Implicit((Action<BaseEventData>)((BaseEventData d) =>
				{
					hideAt = Time.unscaledTime + 0.5f;
				})));
			}
			catch (Exception ex)
			{
				Core.Log("наведение: " + ex.Message);
			}
		}

		private static void Add(EventTrigger et, EventTriggerType type, UnityAction<BaseEventData> act)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Expected Obj, but got Unknown
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0018: Expected Obj, but got Unknown
			Entry val = new Entry();
			val.eventID = type;
			val.callback = new TriggerEvent();
			((UnityEvent<BaseEventData>)(object)val.callback).AddListener(act);
			et.triggers.Add(val);
		}

		private static GameObject Box(string name, Transform parent, float x, float y, float w, float h)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			//IL_0049: Unknown result type (might be due to invalid IL or missing references)
			//IL_0058: Unknown result type (might be due to invalid IL or missing references)
			//IL_0065: Unknown result type (might be due to invalid IL or missing references)
			//IL_007e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0089: Expected Obj, but got Unknown
			GameObject val = new GameObject(name);
			RectTransform val2 = val.AddComponent<RectTransform>();
			((Transform)val2).SetParent(parent, false);
			val2.anchorMin = new Vector2(0f, 0f);
			val2.anchorMax = new Vector2(0f, 0f);
			val2.pivot = new Vector2(0f, 0f);
			val2.sizeDelta = new Vector2(w, h);
			val2.anchoredPosition = new Vector2(x, y);
			((Transform)val2).localScale = new Vector3(1f, 1f, 1f);
			return val;
		}

		private static void Panel(GameObject go)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			Image val = go.AddComponent<Image>();
			((Graphic)val).color = CardBg;
			((Graphic)val).raycastTarget = true;
			try
			{
				Outline val2 = go.AddComponent<Outline>();
				((Shadow)val2).effectColor = Line;
				((Shadow)val2).effectDistance = new Vector2(1.2f, 1.2f);
			}
			catch
			{
			}
		}

		private static TMP_Text Label(string name, Transform parent, float x, float y, float w, float h, string value, float size, Color col, bool wrap)
		{
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			TextMeshProUGUI val = Box(name, parent, x, y, w, h).AddComponent<TextMeshProUGUI>();
			((TMP_Text)val).text = value;
			((TMP_Text)val).fontSize = size;
			((Graphic)val).color = col;
			((TMP_Text)val).alignment = (TextAlignmentOptions)257;
			((TMP_Text)val).richText = true;
			((Graphic)val).raycastTarget = false;
			try
			{
				((TMP_Text)val).enableWordWrapping = wrap;
			}
			catch
			{
			}
			try
			{
				((TMP_Text)val).overflowMode = (TextOverflowModes)0;
			}
			catch
			{
			}
			ApplyFont((TMP_Text)(object)val);
			texts = new List<TMP_Text>(texts) { (TMP_Text)(object)val }.ToArray();
			return (TMP_Text)(object)val;
		}

		private static Button Button(string name, Transform parent, float x, float y, float w, float h, string value, Color bg2, Color textCol, Color border)
		{
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_007e: Unknown result type (might be due to invalid IL or missing references)
			GameObject val = Box(name, parent, x, y, w, h);
			Image val2 = val.AddComponent<Image>();
			((Graphic)val2).color = bg2;
			((Graphic)val2).raycastTarget = true;
			try
			{
				Outline val3 = val.AddComponent<Outline>();
				((Shadow)val3).effectColor = border;
				((Shadow)val3).effectDistance = new Vector2(1.2f, 1.2f);
			}
			catch
			{
			}
			Button val4 = val.AddComponent<Button>();
			((Selectable)val4).targetGraphic = (Graphic)(object)val2;
			Label(name + "Text", val.transform, 0f, 0f, w, h, value, 20f, textCol, wrap: false).alignment = (TextAlignmentOptions)514;
			return val4;
		}

		private static void ApplyFont(TMP_Text t)
		{
			TMP_FontAsset ruFont = Core.RuFont;
			if ((Object)(object)ruFont == (Object)null)
			{
				return;
			}
			try
			{
				t.font = ruFont;
			}
			catch
			{
			}
			Material val = FontMaterial(ruFont);
			if (!((Object)(object)val != (Object)null))
			{
				return;
			}
			try
			{
				t.fontSharedMaterial = val;
			}
			catch
			{
			}
		}

		static Credits()
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			//IL_0055: Unknown result type (might be due to invalid IL or missing references)
			//IL_006e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0073: Unknown result type (might be due to invalid IL or missing references)
			//IL_008c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		}
	}
	internal static class LocalizationRU
	{
		public const string LANG = "ru";

		public static bool NativeWorks = false;

		public static bool Installed = false;

		private static UnityAction actRight = null;

		private static UnityAction actLeft = null;

		private static readonly HashSet<IntPtr> wiredButtons = new HashSet<IntPtr>();

		private static ButtonClickedEvent keepRight = null;

		private static ButtonClickedEvent keepLeft = null;

		private static ChangeLanguange carouselRef = null;

		private static Dictionary<string, string> tableMapCache;

		private static string GetFieldValue(DialogueEntry ent, string title)
		{
			try
			{
				List<Field> fields = ent.fields;
				if (fields == null)
				{
					return null;
				}
				for (int i = 0; i < fields.Count; i++)
				{
					Field val = fields[i];
					if (val != null && string.Equals(val.title ?? "", title, StringComparison.OrdinalIgnoreCase))
					{
						return val.value;
					}
				}
			}
			catch
			{
			}
			return null;
		}

		public static bool SetOrAddField(DialogueEntry ent, string title, string value)
		{
			//IL_006a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0070: Expected Obj, but got Unknown
			try
			{
				List<Field> fields = ent.fields;
				if (fields == null)
				{
					return false;
				}
				for (int i = 0; i < fields.Count; i++)
				{
					Field val = fields[i];
					if (val != null && string.Equals(val.title ?? "", title, StringComparison.OrdinalIgnoreCase))
					{
						if (val.value == value)
						{
							return false;
						}
						val.value = value;
						return true;
					}
				}
				Field val2 = new Field();
				val2.title = title;
				val2.value = value;
				fields.Add(val2);
				return true;
			}
			catch (Exception ex)
			{
				Core.Log("поле \"" + title + "\": " + ex.GetType().Name + " " + ex.Message);
				return false;
			}
		}

		public static bool HasTranslation(DialogueEntry ent)
		{
			return !string.IsNullOrEmpty(GetFieldValue(ent, "ru"));
		}

		private static bool UnderLanguagePanel(Transform t)
		{
			try
			{
				for (int i = 0; i < 6; i++)
				{
					if (!((Object)(object)t != (Object)null))
					{
						break;
					}
					string text = (((Object)t).name ?? "").ToLowerInvariant();
					if (text.Contains("angua") || text.Contains("angag"))
					{
						return true;
					}
					t = t.parent;
				}
			}
			catch
			{
			}
			return false;
		}

		private static void WireArrowsOn(ChangeLanguange c)
		{
			//IL_0257: Unknown result type (might be due to invalid IL or missing references)
			//IL_025e: Expected Obj, but got Unknown
			try
			{
				if ((Object)(object)c == (Object)null)
				{
					return;
				}
				if ((Delegate)(object)actRight == (Delegate)null)
				{
					actRight = UnityAction.op_Implicit((Action)(() =>
					{
						StepLanguage(1);
					}));
				}
				if ((Delegate)(object)actLeft == (Delegate)null)
				{
					actLeft = UnityAction.op_Implicit((Action)(() =>
					{
						StepLanguage(-1);
					}));
				}
				List<Button> list = new List<Button>();
				HashSet<IntPtr> hashSet = new HashSet<IntPtr>();
				Transform val = ((Component)c).transform;
				for (int num = 0; num < 3; num++)
				{
					if (!((Object)(object)val != (Object)null))
					{
						break;
					}
					Il2CppArrayBase<Button> componentsInChildren = ((Component)val).gameObject.GetComponentsInChildren<Button>(true);
					if (componentsInChildren != null)
					{
						foreach (Button item in componentsInChildren)
						{
							if ((Object)(object)item == (Object)null)
							{
								continue;
							}
							string text = "";
							try
							{
								text = ((Object)((Component)item).gameObject).name ?? "";
							}
							catch
							{
							}
							if (!text.Equals("Left", StringComparison.OrdinalIgnoreCase) && !text.Equals("Right", StringComparison.OrdinalIgnoreCase))
							{
								continue;
							}
							IntPtr intPtr = IntPtr.Zero;
							try
							{
								intPtr = ((Il2CppObjectBase)item).Pointer;
							}
							catch
							{
							}
							if (!(intPtr != IntPtr.Zero) || !hashSet.Contains(intPtr))
							{
								if (intPtr != IntPtr.Zero)
								{
									hashSet.Add(intPtr);
								}
								list.Add(item);
							}
						}
					}
					if (list.Count >= 2)
					{
						break;
					}
					val = val.parent;
				}
				if (list.Count == 0)
				{
					return;
				}
				foreach (Button item2 in list)
				{
					string text2 = "";
					try
					{
						text2 = ((Object)((Component)item2).gameObject).name ?? "";
					}
					catch
					{
					}
					bool flag = text2.Equals("Right", StringComparison.OrdinalIgnoreCase);
					IntPtr intPtr2 = IntPtr.Zero;
					try
					{
						intPtr2 = ((Il2CppObjectBase)item2).Pointer;
					}
					catch
					{
					}
					int num2 = -1;
					try
					{
						num2 = ((UnityEventBase)item2.onClick).GetPersistentEventCount();
					}
					catch
					{
					}
					if (num2 == 0 && intPtr2 != IntPtr.Zero && wiredButtons.Contains(intPtr2))
					{
						continue;
					}
					if (intPtr2 != IntPtr.Zero)
					{
						wiredButtons.Add(intPtr2);
					}
					try
					{
						ButtonClickedEvent onClick = new ButtonClickedEvent();
						if (flag)
						{
							keepRight = onClick;
						}
						else
						{
							keepLeft = onClick;
						}
						item2.onClick = onClick;
					}
					catch (Exception ex)
					{
						Core.Log("стрелки: событие не подменить: " + ex.GetType().Name);
					}
					((UnityEvent)item2.onClick).AddListener(flag ? actRight : actLeft);
					Core.Log("карусель: стрелка \"" + text2 + "\" ведёт наш список языков");
				}
			}
			catch (Exception ex2)
			{
				Core.Log("стрелки err " + ex2.GetType().Name + " " + ex2.Message);
			}
		}

		public static void MaintainMenu()
		{
			try
			{
				ChangeLanguange val = LiveCarousel();
				if ((Object)(object)val == (Object)null)
				{
					return;
				}
				carouselRef = val;
				try
				{
					((MonoBehaviour)val).StopAllCoroutines();
					((MonoBehaviour)val).CancelInvoke();
				}
				catch
				{
				}
				List<LanguangeSO> languanges = val.languanges;
				bool flag = false;
				if (languanges != null)
				{
					for (int i = 0; i < languanges.Count; i++)
					{
						LanguangeSO val2 = languanges[i];
						if (!((Object)(object)val2 == (Object)null))
						{
							string text = null;
							try
							{
								text = val2.LanguangeName;
							}
							catch
							{
							}
							if (text == "ru")
							{
								flag = true;
								break;
							}
						}
					}
				}
				if (!flag)
				{
					Core.Log("карусель: пункт \"ru\" пропал — добавляю снова");
					InstallIntoMenu(verbose: false);
					if ((Object)(object)carouselRef != (Object)null)
					{
						val = carouselRef;
					}
				}
				WireArrowsOn(val);
				SyncMenuLabel();
			}
			catch (Exception ex)
			{
				Core.Log("меню err " + ex.GetType().Name + " " + ex.Message);
			}
		}

		private static ChangeLanguange LiveCarousel()
		{
			try
			{
				Il2CppArrayBase<ChangeLanguange> val = Resources.FindObjectsOfTypeAll<ChangeLanguange>();
				if (val == null || val.Length == 0)
				{
					return null;
				}
				ChangeLanguange val2 = null;
				for (int i = 0; i < val.Length; i++)
				{
					ChangeLanguange val3 = val[i];
					if ((Object)(object)val3 == (Object)null)
					{
						continue;
					}
					if ((Object)(object)val2 == (Object)null)
					{
						val2 = val3;
					}
					List<LanguangeSO> languanges = val3.languanges;
					if (languanges == null)
					{
						continue;
					}
					for (int j = 0; j < languanges.Count; j++)
					{
						LanguangeSO val4 = languanges[j];
						if (!((Object)(object)val4 == (Object)null))
						{
							string text = null;
							try
							{
								text = val4.LanguangeName;
							}
							catch
							{
							}
							if (text == "ru")
							{
								return val3;
							}
						}
					}
				}
				return val2;
			}
			catch
			{
				return null;
			}
		}

		public static bool MenuIsUp()
		{
			try
			{
				ChangeLanguange val = LiveCarousel();
				if ((Object)(object)val == (Object)null)
				{
					return false;
				}
				Transform val2 = ((Component)val).transform;
				for (int i = 0; i < 10; i++)
				{
					if (!((Object)(object)val2 != (Object)null))
					{
						break;
					}
					string text = null;
					try
					{
						text = ((Object)val2).name;
					}
					catch
					{
					}
					if (!string.IsNullOrEmpty(text) && text.IndexOf("Main Menu", StringComparison.OrdinalIgnoreCase) >= 0)
					{
						return true;
					}
					val2 = val2.parent;
				}
			}
			catch
			{
			}
			return false;
		}

		public static string CarouselPath()
		{
			try
			{
				ChangeLanguange val = LiveCarousel();
				if ((Object)(object)val == (Object)null)
				{
					return "нет карусели";
				}
				StringBuilder stringBuilder = new StringBuilder();
				Transform val2 = ((Component)val).transform;
				for (int i = 0; i < 10; i++)
				{
					if (!((Object)(object)val2 != (Object)null))
					{
						break;
					}
					if (stringBuilder.Length > 0)
					{
						stringBuilder.Insert(0, "/");
					}
					stringBuilder.Insert(0, ((Object)val2).name);
					val2 = val2.parent;
				}
				return stringBuilder.ToString();
			}
			catch
			{
				return "?";
			}
		}

		private static void StepLanguage(int dir)
		{
			try
			{
				ChangeLanguange val = LiveCarousel();
				if ((Object)(object)val == (Object)null)
				{
					Core.Log("шаг языка: карусель не найдена");
					return;
				}
				carouselRef = val;
				List<LanguangeSO> languanges = val.languanges;
				if (languanges == null || languanges.Count == 0)
				{
					return;
				}
				string text = null;
				try
				{
					text = Localization.language;
				}
				catch
				{
				}
				int num = -1;
				for (int i = 0; i < languanges.Count; i++)
				{
					LanguangeSO val2 = languanges[i];
					if (!((Object)(object)val2 == (Object)null))
					{
						string text2 = null;
						try
						{
							text2 = val2.LanguangeName;
						}
						catch
						{
						}
						if (text2 == text)
						{
							num = i;
							break;
						}
					}
				}
				if (num < 0)
				{
					num = ((dir > 0) ? (-1) : 0);
				}
				int count = languanges.Count;
				num = ((num + dir) % count + count) % count;
				LanguangeSO val3 = languanges[num];
				string text3 = null;
				try
				{
					text3 = (((Object)(object)val3 == (Object)null) ? null : val3.LanguangeName);
				}
				catch
				{
				}
				if (!string.IsNullOrEmpty(text3))
				{
					Core.Log("шаг языка: " + (text ?? "null") + " (idx=" + num + ") -> " + text3);
					try
					{
						Localization.language = text3;
					}
					catch (Exception ex)
					{
						Core.Log("язык не выставился: " + ex.Message);
					}
					try
					{
						val.currentLanguange = num;
					}
					catch
					{
					}
					try
					{
						val.Language = text3;
					}
					catch
					{
					}
					SetLabel(val, text3);
					Core.Log("язык переключён на \"" + text3 + "\"");
					Core.RequestMenuRefresh();
				}
			}
			catch (Exception ex2)
			{
				Core.Log("шаг языка err " + ex2.GetType().Name + " " + ex2.Message);
			}
		}

		private static string KnownName(string lang)
		{
			return lang switch
			{
				"English" => "English", 
				"Chinese" => "语言", 
				"jp" => "ネイティブ言語", 
				"esp" => "Español", 
				"ger" => "Deutsche Version", 
				"fr" => "Français", 
				"ru" => "Русский", 
				_ => null, 
			};
		}

		private static string LangDisplay(string lang)
		{
			string text = KnownName(lang);
			if (!string.IsNullOrEmpty(text))
			{
				return text;
			}
			try
			{
				string text2 = null;
				try
				{
					text2 = Localization.language;
				}
				catch
				{
				}
				Il2CppArrayBase<TextTable> val = Resources.FindObjectsOfTypeAll<TextTable>();
				if (val != null)
				{
					foreach (TextTable item in val)
					{
						if ((Object)(object)item == (Object)null)
						{
							continue;
						}
						string text3 = null;
						try
						{
							if (!string.IsNullOrEmpty(text2))
							{
								text3 = item.GetFieldTextForLanguage(lang, text2);
							}
						}
						catch
						{
						}
						if (string.IsNullOrEmpty(text3))
						{
							try
							{
								text3 = item.GetFieldTextForLanguage(lang, "English");
							}
							catch
							{
							}
						}
						if (string.IsNullOrEmpty(text3))
						{
							try
							{
								text3 = item.GetFieldTextForLanguage(lang, lang);
							}
							catch
							{
							}
						}
						if (!string.IsNullOrEmpty(text3))
						{
							return text3;
						}
					}
				}
			}
			catch
			{
			}
			return lang;
		}

		public static void SetLabel(ChangeLanguange c, string lang)
		{
			try
			{
				TMP_Text val = FindLabel(c);
				if ((Object)(object)val == (Object)null)
				{
					val = ((Component)c).gameObject.GetComponentInChildren<TMP_Text>(true);
				}
				if ((Object)(object)val != (Object)null)
				{
					val.text = LangDisplay(lang);
				}
			}
			catch
			{
			}
		}

		public static bool HasSampleRu()
		{
			try
			{
				Il2CppArrayBase<DialogueDatabase> val = Resources.FindObjectsOfTypeAll<DialogueDatabase>();
				if (val == null)
				{
					return false;
				}
				for (int i = 0; i < val.Length; i++)
				{
					DialogueDatabase val2 = val[i];
					if ((Object)(object)val2 == (Object)null)
					{
						continue;
					}
					List<Conversation> conversations = val2.conversations;
					if (conversations == null)
					{
						continue;
					}
					for (int j = 0; j < conversations.Count; j++)
					{
						Conversation val3 = conversations[j];
						if (val3 == null)
						{
							continue;
						}
						List<DialogueEntry> dialogueEntries = val3.dialogueEntries;
						if (dialogueEntries == null)
						{
							continue;
						}
						for (int k = 0; k < dialogueEntries.Count; k++)
						{
							if (dialogueEntries[k] != null && HasTranslation(dialogueEntries[k]))
							{
								return true;
							}
						}
					}
				}
			}
			catch
			{
			}
			return false;
		}

		public static bool TablesHaveRu()
		{
			try
			{
				Il2CppArrayBase<TextTable> val = Resources.FindObjectsOfTypeAll<TextTable>();
				if (val == null || val.Length == 0)
				{
					return true;
				}
				for (int i = 0; i < val.Length; i++)
				{
					TextTable val2 = val[i];
					if ((Object)(object)val2 == (Object)null)
					{
						continue;
					}
					try
					{
						Il2CppStringArray languageNames = val2.GetLanguageNames();
						if (languageNames == null)
						{
							continue;
						}
						for (int j = 0; j < ((Il2CppArrayBase<string>)(object)languageNames).Count; j++)
						{
							if (((Il2CppArrayBase<string>)(object)languageNames)[j] == "ru")
							{
								return true;
							}
						}
					}
					catch
					{
					}
				}
				return false;
			}
			catch
			{
				return true;
			}
		}

		public static bool Probe(DialogueDatabase db)
		{
			try
			{
				List<Conversation> conversations = db.conversations;
				if (conversations == null)
				{
					Core.Log("проба: нет бесед");
					return false;
				}
				for (int i = 0; i < conversations.Count; i++)
				{
					Conversation val = conversations[i];
					if (val == null)
					{
						continue;
					}
					List<DialogueEntry> dialogueEntries = val.dialogueEntries;
					if (dialogueEntries == null)
					{
						continue;
					}
					for (int j = 0; j < dialogueEntries.Count; j++)
					{
						DialogueEntry val2 = dialogueEntries[j];
						if (val2 == null)
						{
							continue;
						}
						string fieldValue = GetFieldValue(val2, "Dialogue Text");
						if (!string.IsNullOrEmpty(fieldValue) && fieldValue.Length >= 4)
						{
							SetOrAddField(val2, "ru", "ПРОВЕРКА_ЯЗЫКА_Ж");
							string language = null;
							try
							{
								language = Localization.language;
							}
							catch
							{
							}
							try
							{
								Localization.language = "ru";
							}
							catch (Exception ex)
							{
								Core.Log("проба: смена языка не удалась: " + ex.GetType().Name);
								return false;
							}
							string text = null;
							try
							{
								text = val2.currentLocalizedDialogueText;
							}
							catch (Exception ex2)
							{
								Core.Log("проба: чтение не удалось: " + ex2.GetType().Name);
							}
							try
							{
								Localization.language = language;
							}
							catch
							{
							}
							Core.Log("проба: язык=ru -> \"" + (text ?? "null") + "\"");
							return text != null && text.IndexOf("ПРОВЕРКА", StringComparison.Ordinal) >= 0;
						}
					}
				}
			}
			catch (Exception ex3)
			{
				Core.Log("проба err: " + ex3.Message);
			}
			return false;
		}

		public static int FillDatabase(DialogueDatabase db, Dictionary<string, string> tr)
		{
			int num = 0;
			try
			{
				List<Conversation> conversations = db.conversations;
				if (conversations == null)
				{
					return 0;
				}
				for (int i = 0; i < conversations.Count; i++)
				{
					Conversation val = conversations[i];
					if (val == null)
					{
						continue;
					}
					int id = ((Asset)val).id;
					List<DialogueEntry> dialogueEntries = val.dialogueEntries;
					if (dialogueEntries == null)
					{
						continue;
					}
					for (int j = 0; j < dialogueEntries.Count; j++)
					{
						DialogueEntry val2 = dialogueEntries[j];
						if (val2 != null)
						{
							if (tr.TryGetValue(id + "_" + val2.id + "_dlg", out var value) && !string.IsNullOrEmpty(value) && SetOrAddField(val2, "ru", value))
							{
								num++;
							}
							if (tr.TryGetValue(id + "_" + val2.id + "_mnu", out value) && !string.IsNullOrEmpty(value) && SetOrAddField(val2, "Menu Text ru", value))
							{
								num++;
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Core.Log("заливка err: " + ex.Message);
			}
			return num;
		}

		public static void DumpTables()
		{
			try
			{
				string text = Path.Combine(Core.GameDirPublic, "UserData", "LBBB_RU");
				Directory.CreateDirectory(text);
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendLine("table;key;english;ткуда;ru");
				Il2CppArrayBase<TextTable> val = Resources.FindObjectsOfTypeAll<TextTable>();
				int num = 0;
				foreach (TextTable item in val)
				{
					if ((Object)(object)item == (Object)null)
					{
						continue;
					}
					Il2CppStringArray fieldNames = item.GetFieldNames();
					if (fieldNames == null)
					{
						continue;
					}
					for (int i = 0; i < ((Il2CppArrayBase<string>)(object)fieldNames).Count; i++)
					{
						string text2 = ((Il2CppArrayBase<string>)(object)fieldNames)[i];
						string text3 = null;
						try
						{
							text3 = item.GetFieldTextForLanguage(text2, "English");
						}
						catch
						{
						}
						if (string.IsNullOrEmpty(text3))
						{
							try
							{
								text3 = item.GetFieldTextForLanguage(text2, "Default");
							}
							catch
							{
							}
						}
						if (text3 == null)
						{
							text3 = "";
						}
						string text4 = null;
						try
						{
							text4 = item.GetFieldTextForLanguage(text2, "ru");
						}
						catch
						{
						}
						stringBuilder.Append(Core.Csv(((Object)item).name)).Append(';').Append(Core.Csv(text2))
							.Append(';')
							.Append(Core.Csv(text3))
							.Append(';')
							.Append(Core.Csv(text3))
							.Append(';')
							.Append(Core.Csv(text4 ?? ""))
							.AppendLine();
						num++;
					}
				}
				File.WriteAllText(Path.Combine(text, "tables.csv"), stringBuilder.ToString(), Encoding.UTF8);
				Core.Log("таблицы выгружены: " + num + " строк -> UserData/LBBB_RU/tables.csv");
			}
			catch (Exception ex)
			{
				Core.Log("таблицы дамп err: " + ex.Message);
			}
		}

		public static bool ProbeLanguageUI()
		{
			try
			{
				Il2CppArrayBase<BudzZaItemDropDownaLocalization> val = Resources.FindObjectsOfTypeAll<BudzZaItemDropDownaLocalization>();
				Il2CppArrayBase<LanguageSettings> val2 = Resources.FindObjectsOfTypeAll<LanguageSettings>();
				Il2CppArrayBase<LoadLocalizationSettings> val3 = Resources.FindObjectsOfTypeAll<LoadLocalizationSettings>();
				if ((val == null || val.Length == 0) && (val2 == null || val2.Length == 0) && (val3 == null || val3.Length == 0))
				{
					return false;
				}
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendLine("=== экран языка найден");
				if (val != null)
				{
					stringBuilder.AppendLine("--- пункты (BudzZaItemDropDownaLocalization): " + val.Length);
					foreach (BudzZaItemDropDownaLocalization item in val)
					{
						if ((Object)(object)item == (Object)null)
						{
							continue;
						}
						stringBuilder.AppendLine("  \"" + SafePath(((Component)item).gameObject) + "\" Key=\"" + item.Key + "\" активен=" + ((Component)item).gameObject.activeInHierarchy);
						Il2CppArrayBase<TMP_Text> componentsInChildren = ((Component)item).gameObject.GetComponentsInChildren<TMP_Text>(true);
						if (componentsInChildren == null)
						{
							continue;
						}
						foreach (TMP_Text item2 in componentsInChildren)
						{
							if ((Object)(object)item2 != (Object)null)
							{
								stringBuilder.AppendLine("        текст: \"" + item2.text + "\"");
							}
						}
					}
				}
				if (val2 != null && val2.Length > 0)
				{
					stringBuilder.AppendLine("--- LanguageSettings: " + val2.Length);
					foreach (LanguageSettings item3 in val2)
					{
						if ((Object)(object)item3 == (Object)null)
						{
							continue;
						}
						stringBuilder.AppendLine("  \"" + SafePath(((Component)item3).gameObject) + "\" индекс=" + item3.LanguageSettingsInt);
						Il2CppArrayBase<TMP_Text> componentsInChildren2 = ((Component)item3).gameObject.GetComponentsInChildren<TMP_Text>(true);
						if (componentsInChildren2 == null)
						{
							continue;
						}
						foreach (TMP_Text item4 in componentsInChildren2)
						{
							if ((Object)(object)item4 != (Object)null)
							{
								stringBuilder.AppendLine("        текст: \"" + item4.text + "\"");
							}
						}
					}
				}
				if (val3 != null && val3.Length > 0)
				{
					stringBuilder.AppendLine("--- LoadLocalizationSettings: " + val3.Length);
					foreach (LoadLocalizationSettings item5 in val3)
					{
						if (!((Object)(object)item5 == (Object)null))
						{
							string text = null;
							try
							{
								text = item5.Language;
							}
							catch
							{
							}
							stringBuilder.AppendLine("  \"" + SafePath(((Component)item5).gameObject) + "\" Language=\"" + (text ?? "null") + "\"");
						}
					}
				}
				if ((val == null || val.Length <= 0) && (val2 == null || val2.Length <= 0))
				{
					return false;
				}
				File.WriteAllText(Path.Combine(Core.GameDirPublic, "UserData", "LBBB_RU", "language_ui.txt"), stringBuilder.ToString(), Encoding.UTF8);
				Core.Log("экран языка: снят language_ui.txt (" + stringBuilder.Length + " символов)");
				return true;
			}
			catch (Exception ex)
			{
				Core.Log("экран языка err: " + ex.GetType().Name);
				return false;
			}
		}

		public static bool DumpSettingsTree()
		{
			try
			{
				Il2CppArrayBase<LanguageSettings> val = Resources.FindObjectsOfTypeAll<LanguageSettings>();
				GameObject val2 = null;
				if (val != null)
				{
					foreach (LanguageSettings item in val)
					{
						if (!((Object)(object)item == (Object)null))
						{
							val2 = ((Component)item).gameObject;
							break;
						}
					}
				}
				if ((Object)(object)val2 == (Object)null)
				{
					return false;
				}
				Transform val3 = val2.transform;
				int num = 0;
				while ((Object)(object)val3.parent != (Object)null && num++ < 12 && !(((Object)val3.parent).name == "UI") && !((Object)(object)val3.parent.parent == (Object)null))
				{
					val3 = val3.parent;
				}
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendLine("корень: " + SafePath(((Component)val3).gameObject));
				DumpNode(val3, 0, stringBuilder);
				File.WriteAllText(Path.Combine(Core.GameDirPublic, "UserData", "LBBB_RU", "settings_ui.txt"), stringBuilder.ToString(), Encoding.UTF8);
				Core.Log("настройки: дерево снято (" + stringBuilder.Length + " символов)");
				return true;
			}
			catch (Exception ex)
			{
				Core.Log("дерево настроек err: " + ex.GetType().Name);
				return false;
			}
		}

		private static void DumpNode(Transform tr, int depth, StringBuilder sb)
		{
			if ((Object)(object)tr == (Object)null || depth > 7)
			{
				return;
			}
			string text = new string(' ', depth * 2) + ((Object)tr).name;
			try
			{
				TMP_Text component = ((Component)tr).GetComponent<TMP_Text>();
				if ((Object)(object)component != (Object)null)
				{
					text = text + "  текст=\"" + component.text + "\"";
				}
			}
			catch
			{
			}
			sb.AppendLine(text);
			int num = 0;
			try
			{
				num = tr.childCount;
			}
			catch
			{
			}
			for (int i = 0; i < num; i++)
			{
				Transform tr2 = null;
				try
				{
					tr2 = tr.GetChild(i);
				}
				catch
				{
				}
				DumpNode(tr2, depth + 1, sb);
			}
		}

		public static bool DumpLanguagePanels()
		{
			try
			{
				Il2CppArrayBase<Transform> val = Resources.FindObjectsOfTypeAll<Transform>();
				StringBuilder stringBuilder = new StringBuilder();
				int num = 0;
				foreach (Transform item in val)
				{
					if ((Object)(object)item == (Object)null)
					{
						continue;
					}
					string name;
					try
					{
						name = ((Object)item).name;
					}
					catch
					{
						continue;
					}
					if (!string.IsNullOrEmpty(name) && (name.IndexOf("lang", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("язык", StringComparison.OrdinalIgnoreCase) >= 0))
					{
						num++;
						if (num > 12)
						{
							break;
						}
						stringBuilder.AppendLine("=== \"" + SafePath(((Component)item).gameObject) + "\" активен=" + SafePath(((Component)item).gameObject).Length);
						DumpNode(item, 0, stringBuilder);
					}
				}
				if (num == 0)
				{
					return false;
				}
				File.WriteAllText(Path.Combine(Core.GameDirPublic, "UserData", "LBBB_RU", "language_panel.txt"), stringBuilder.ToString(), Encoding.UTF8);
				Core.Log("панель языков: найдено объектов " + num + ", дерево снято");
				return true;
			}
			catch (Exception ex)
			{
				Core.Log("панель языков err: " + ex.GetType().Name);
				return false;
			}
		}

		public static bool DumpLanguageFields()
		{
			try
			{
				StringBuilder stringBuilder = new StringBuilder();
				int num = 0;
				Il2CppArrayBase<LoadLocalizationSettings> val = Resources.FindObjectsOfTypeAll<LoadLocalizationSettings>();
				stringBuilder.AppendLine("LoadLocalizationSettings: " + (val?.Length ?? 0));
				if (val != null)
				{
					foreach (LoadLocalizationSettings item in val)
					{
						if (!((Object)(object)item == (Object)null))
						{
							num++;
							DumpObjectFields(item, typeof(LoadLocalizationSettings), stringBuilder);
						}
					}
				}
				Il2CppArrayBase<LanguageSettings> val2 = Resources.FindObjectsOfTypeAll<LanguageSettings>();
				stringBuilder.AppendLine("LanguageSettings: " + (val2?.Length ?? 0));
				if (val2 != null)
				{
					foreach (LanguageSettings item2 in val2)
					{
						if (!((Object)(object)item2 == (Object)null))
						{
							num++;
							DumpObjectFields(item2, typeof(LanguageSettings), stringBuilder);
						}
					}
				}
				if (num == 0)
				{
					return false;
				}
				File.WriteAllText(Path.Combine(Core.GameDirPublic, "UserData", "LBBB_RU", "language_fields.txt"), stringBuilder.ToString(), Encoding.UTF8);
				Core.Log("поля языка сняты: language_fields.txt");
				return true;
			}
			catch (Exception ex)
			{
				Core.Log("поля языка err: " + ex.GetType().Name);
				return false;
			}
		}

		private static void DumpObjectFields(object inst, Type type, StringBuilder sb)
		{
			sb.AppendLine("--- экземпляр: " + type.FullName);
			BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			FieldInfo[] fields = type.GetFields(bindingAttr);
			foreach (FieldInfo fieldInfo in fields)
			{
				string text = "?";
				try
				{
					object value = fieldInfo.GetValue(inst);
					if (value == null)
					{
						text = "null";
					}
					else if (value is string text2)
					{
						text = "\"" + text2 + "\"";
					}
					else if (value is IEnumerable enumerable && !(value is string))
					{
						List<string> list = new List<string>();
						foreach (object item in enumerable)
						{
							list.Add((item == null) ? "null" : item.ToString());
							if (list.Count > 12)
							{
								break;
							}
						}
						text = "[" + string.Join(", ", list) + "]";
					}
					else
					{
						text = value.ToString();
					}
				}
				catch (Exception ex)
				{
					text = "<" + ex.GetType().Name + ">";
				}
				sb.AppendLine("   " + fieldInfo.FieldType.Name + " " + fieldInfo.Name + " = " + text);
			}
			PropertyInfo[] properties = type.GetProperties(bindingAttr);
			foreach (PropertyInfo propertyInfo in properties)
			{
				string text3 = "?";
				try
				{
					object value2 = propertyInfo.GetValue(inst);
					text3 = ((value2 == null) ? "null" : value2.ToString());
				}
				catch (Exception ex2)
				{
					text3 = "<" + ex2.GetType().Name + ">";
				}
				sb.AppendLine("   [свойство] " + propertyInfo.PropertyType.Name + " " + propertyInfo.Name + " = " + text3);
			}
		}

		public static void SyncMenuLabel()
		{
			try
			{
				if ((Object)(object)carouselRef == (Object)null)
				{
					return;
				}
				if (Localization.language != "ru")
				{
					carouselRef = null;
					return;
				}
				TMP_Text val = FindLabel(carouselRef);
				if (!((Object)(object)val == (Object)null) && val.text != "Русский")
				{
					val.text = "Русский";
				}
			}
			catch
			{
			}
		}

		private static TMP_Text FindLabel(ChangeLanguange c)
		{
			try
			{
				if ((Object)(object)c == (Object)null)
				{
					return null;
				}
				Il2CppArrayBase<TMP_Text> componentsInChildren = ((Component)c).gameObject.GetComponentsInChildren<TMP_Text>(true);
				if (componentsInChildren == null)
				{
					return null;
				}
				foreach (TMP_Text item in componentsInChildren)
				{
					if (!((Object)(object)item == (Object)null))
					{
						string text = "";
						try
						{
							text = ((Object)((Component)item).gameObject).name;
						}
						catch
						{
						}
						if (text != null && text.IndexOf("Language Text", StringComparison.OrdinalIgnoreCase) >= 0)
						{
							return item;
						}
					}
				}
			}
			catch
			{
			}
			return null;
		}

		public static bool InstallIntoMenu(bool verbose = true)
		{
			try
			{
				Il2CppArrayBase<ChangeLanguange> val = Resources.FindObjectsOfTypeAll<ChangeLanguange>();
				if (val == null || val.Length == 0)
				{
					return false;
				}
				if (verbose)
				{
					Core.Log("карусель языка: найдено компонентов " + val.Length);
				}
				foreach (ChangeLanguange item in val)
				{
					if ((Object)(object)item == (Object)null)
					{
						continue;
					}
					List<LanguangeSO> languanges = item.languanges;
					if (languanges == null)
					{
						Core.Log("карусель: список пуст (null)");
						continue;
					}
					List<string> list = new List<string>();
					bool flag = false;
					for (int i = 0; i < languanges.Count; i++)
					{
						LanguangeSO val2 = languanges[i];
						if ((Object)(object)val2 == (Object)null)
						{
							list.Add("null");
							continue;
						}
						string text = null;
						try
						{
							text = val2.LanguangeName;
						}
						catch
						{
						}
						list.Add(text ?? "?");
						if (text == "ru")
						{
							flag = true;
						}
					}
					if (verbose)
					{
						Core.Log("карусель: сейчас в списке [" + string.Join(", ", list) + "], текущий индекс=" + item.currentLanguange + ", Language=\"" + item.Language + "\"");
					}
					if (!flag)
					{
						LanguangeSO val3 = ScriptableObject.CreateInstance<LanguangeSO>();
						if ((Object)(object)val3 != (Object)null)
						{
							val3.LanguangeName = "ru";
							try
							{
								val3.dropdownplace = languanges.Count;
							}
							catch
							{
							}
							languanges.Add(val3);
							Core.Log("карусель: добавлен пункт \"ru\" (теперь " + languanges.Count + ")");
						}
						else
						{
							Core.Log("карусель: CreateInstance вернул null");
						}
					}
					try
					{
						((MonoBehaviour)item).StopAllCoroutines();
					}
					catch
					{
					}
					try
					{
						((MonoBehaviour)item).CancelInvoke();
					}
					catch
					{
					}
					carouselRef = item;
					int num = -1;
					for (int j = 0; j < languanges.Count; j++)
					{
						LanguangeSO val4 = languanges[j];
						if (!((Object)(object)val4 == (Object)null))
						{
							string text2 = null;
							try
							{
								text2 = val4.LanguangeName;
							}
							catch
							{
							}
							if (text2 == "ru")
							{
								num = j;
								break;
							}
						}
					}
					string text3 = "";
					try
					{
						text3 = Localization.language;
					}
					catch
					{
					}
					if (!(text3 == "ru"))
					{
						continue;
					}
					try
					{
						if (num >= 0)
						{
							item.currentLanguange = num;
						}
					}
					catch
					{
					}
					try
					{
						item.Language = "ru";
					}
					catch
					{
					}
					try
					{
						TMP_Text componentInChildren = ((Component)item).gameObject.GetComponentInChildren<TMP_Text>(true);
						if ((Object)(object)componentInChildren != (Object)null)
						{
							componentInChildren.text = "Русский";
							Core.Log("карусель: надпись переведена на \"Русский\"");
						}
					}
					catch (Exception ex)
					{
						Core.Log("карусель: надпись err " + ex.GetType().Name);
					}
				}
				return true;
			}
			catch (Exception ex2)
			{
				Core.Log("карусель err: " + ex2.GetType().Name + " " + ex2.Message);
				return false;
			}
		}

		public static void DumpDropdowns()
		{
			string path = Path.Combine(Core.GameDirPublic, "UserData", "LBBB_RU", "dropdowns.txt");
			try
			{
				File.WriteAllText(path, "начали\r\n");
			}
			catch
			{
			}
			try
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendLine("=== выпадающие списки (UnityEngine.UI.Dropdown)");
				Il2CppArrayBase<Dropdown> val = Resources.FindObjectsOfTypeAll<Dropdown>();
				stringBuilder.AppendLine("найдено: " + val.Length);
				foreach (Dropdown item in val)
				{
					if ((Object)(object)item == (Object)null)
					{
						continue;
					}
					stringBuilder.Append("- \"" + SafePath(((Component)item).gameObject) + "\" активен=" + ((Component)item).gameObject.activeInHierarchy + " пунктов=");
					List<OptionData> options = item.options;
					stringBuilder.AppendLine((options?.Count ?? 0).ToString());
					if (options != null)
					{
						for (int i = 0; i < options.Count && i < 40; i++)
						{
							stringBuilder.AppendLine("    [" + i + "] \"" + options[i].text + "\"");
						}
					}
				}
				stringBuilder.AppendLine("=== TMP-списки (TMP_Dropdown)");
				Il2CppArrayBase<TMP_Dropdown> val2 = Resources.FindObjectsOfTypeAll<TMP_Dropdown>();
				stringBuilder.AppendLine("найдено: " + val2.Length);
				foreach (TMP_Dropdown item2 in val2)
				{
					if ((Object)(object)item2 == (Object)null)
					{
						continue;
					}
					stringBuilder.Append("- \"" + SafePath(((Component)item2).gameObject) + "\" активен=" + ((Component)item2).gameObject.activeInHierarchy + " пунктов=");
					List<OptionData> options2 = item2.options;
					stringBuilder.AppendLine((options2?.Count ?? 0).ToString());
					if (options2 != null)
					{
						for (int j = 0; j < options2.Count && j < 40; j++)
						{
							stringBuilder.AppendLine("    [" + j + "] \"" + options2[j].text + "\"");
						}
					}
				}
				stringBuilder.AppendLine("=== пункты-переводы (BudzZaItemDropDownaLocalization)");
				Il2CppArrayBase<BudzZaItemDropDownaLocalization> val3 = Resources.FindObjectsOfTypeAll<BudzZaItemDropDownaLocalization>();
				stringBuilder.AppendLine("найдено: " + val3.Length);
				foreach (BudzZaItemDropDownaLocalization item3 in val3)
				{
					if (!((Object)(object)item3 == (Object)null))
					{
						stringBuilder.AppendLine("    \"" + SafePath(((Component)item3).gameObject) + "\" Key=\"" + item3.Key + "\" активен=" + ((Component)item3).gameObject.activeInHierarchy);
					}
				}
				File.WriteAllText(path, stringBuilder.ToString(), Encoding.UTF8);
				Core.Log("списки языков: записан dropdowns.txt");
			}
			catch (Exception ex)
			{
				Core.Log("dropdowns err: " + ex.GetType().Name + " " + ex.Message);
				try
				{
					File.AppendAllText(path, "ОШИБКА: " + ex.GetType().Name + " " + ex.Message + "\r\n" + ex.StackTrace);
				}
				catch
				{
				}
			}
		}

		private static string SafePath(GameObject go)
		{
			try
			{
				StringBuilder stringBuilder = new StringBuilder(((Object)go).name);
				Transform parent = go.transform.parent;
				int num = 0;
				while ((Object)(object)parent != (Object)null && num++ < 8)
				{
					stringBuilder.Insert(0, ((Object)parent).name + "/");
					parent = parent.parent;
				}
				return stringBuilder.ToString();
			}
			catch
			{
				return "?";
			}
		}

		private static Dictionary<string, string> LoadTableMap()
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
			try
			{
				string path = Path.Combine(Core.ModsDirPublic, "LBBB_RU", "ui_tables.csv");
				if (!File.Exists(path))
				{
					return dictionary;
				}
				string[] array = File.ReadAllLines(path);
				foreach (string text in array)
				{
					if (string.IsNullOrEmpty(text))
					{
						continue;
					}
					int num = text.IndexOf(';');
					if (num > 0)
					{
						int num2 = text.IndexOf(';', num + 1);
						string text2 = text.Substring(0, num);
						string text3 = ((num2 < 0) ? text.Substring(num + 1) : text.Substring(num + 1, num2 - num - 1));
						if (!(text2 == "key") && !string.IsNullOrEmpty(text3) && !(text3 == "#skip"))
						{
							dictionary[text2] = text3.Replace("\\n", "\n");
						}
					}
				}
				Core.Log("таблицы: загружено своих строк " + dictionary.Count + " из ui_tables.csv");
			}
			catch (Exception ex)
			{
				Core.Log("ui_tables.csv err: " + ex.GetType().Name);
			}
			return dictionary;
		}

		public static Dictionary<string, string> TableMap()
		{
			if (tableMapCache == null)
			{
				tableMapCache = LoadTableMap();
			}
			return tableMapCache;
		}

		public static void FillTables(Dictionary<string, string> ui)
		{
			try
			{
				Dictionary<string, string> dictionary = LoadTableMap();
				foreach (TextTable item in Resources.FindObjectsOfTypeAll<TextTable>())
				{
					if ((Object)(object)item == (Object)null)
					{
						continue;
					}
					try
					{
						try
						{
							TextTable.useDefaultLanguageForBlankTranslations = true;
						}
						catch
						{
						}
						Il2CppStringArray languageNames = item.GetLanguageNames();
						bool flag = false;
						if (languageNames != null)
						{
							for (int i = 0; i < ((Il2CppArrayBase<string>)(object)languageNames).Count; i++)
							{
								if (((Il2CppArrayBase<string>)(object)languageNames)[i] == "ru")
								{
									flag = true;
								}
							}
						}
						if (!flag)
						{
							item.AddLanguage("ru");
						}
						int num = 0;
						Il2CppStringArray fieldNames = item.GetFieldNames();
						if (fieldNames == null)
						{
							continue;
						}
						for (int j = 0; j < ((Il2CppArrayBase<string>)(object)fieldNames).Count; j++)
						{
							string text = ((Il2CppArrayBase<string>)(object)fieldNames)[j];
							string text2 = null;
							try
							{
								text2 = item.GetFieldTextForLanguage(text, "English");
							}
							catch
							{
							}
							if (string.IsNullOrEmpty(text2))
							{
								try
								{
									text2 = item.GetFieldTextForLanguage(text, "Default");
								}
								catch
								{
								}
							}
							if (string.IsNullOrEmpty(text2))
							{
								text2 = text;
							}
							string value = null;
							if (!dictionary.TryGetValue(text, out value) || string.IsNullOrEmpty(value))
							{
								value = null;
								ui?.TryGetValue(text2, out value);
							}
							if (!string.IsNullOrEmpty(value))
							{
								item.SetFieldTextForLanguage(text, "ru", value);
								num++;
							}
						}
						try
						{
							if (!item.HasField("ru"))
							{
								item.AddField("ru");
								item.SetFieldTextForLanguage("ru", "English", "Russian");
								item.SetFieldTextForLanguage("ru", "ru", "Русский");
								try
								{
									item.SetFieldTextForLanguage("ru", "fr", "Russe");
								}
								catch
								{
								}
								try
								{
									item.SetFieldTextForLanguage("ru", "ger", "Russisch");
								}
								catch
								{
								}
								try
								{
									item.SetFieldTextForLanguage("ru", "esp", "Ruso");
								}
								catch
								{
								}
								try
								{
									item.SetFieldTextForLanguage("ru", "jp", "ロシア語");
								}
								catch
								{
								}
								try
								{
									item.SetFieldTextForLanguage("ru", "Chinese", "俄语");
								}
								catch
								{
								}
								try
								{
									item.SetFieldTextForLanguage("ru", "Default", "Russian");
								}
								catch
								{
								}
								Core.Log("таблица \"" + ((Object)item).name + "\": добавлен ключ ru = Русский");
							}
						}
						catch (Exception ex)
						{
							Core.Log("ключ ru в \"" + ((Object)item).name + "\": " + ex.GetType().Name);
						}
						Core.Log("таблица \"" + ((Object)item).name + "\": язык ru добавлен, переведено " + num);
					}
					catch (Exception ex2)
					{
						Core.Log("таблица \"" + ((Object)item).name + "\" err: " + ex2.GetType().Name);
					}
				}
			}
			catch (Exception ex3)
			{
				Core.Log("таблицы err: " + ex3.Message);
			}
		}

		public static void FillFonts(TMP_FontAsset ruFont)
		{
			//IL_0080: Unknown result type (might be due to invalid IL or missing references)
			//IL_0087: Expected Obj, but got Unknown
			if ((Object)(object)ruFont == (Object)null)
			{
				return;
			}
			try
			{
				foreach (LocalizedFonts item in Resources.FindObjectsOfTypeAll<LocalizedFonts>())
				{
					if ((Object)(object)item == (Object)null)
					{
						continue;
					}
					List<FontForLanguage> fontsForLanguages = item.fontsForLanguages;
					if (fontsForLanguages == null)
					{
						continue;
					}
					bool flag = false;
					for (int i = 0; i < fontsForLanguages.Count; i++)
					{
						FontForLanguage val = fontsForLanguages[i];
						if (val != null && val.language == "ru")
						{
							val.textMeshProFont = ruFont;
							flag = true;
						}
					}
					if (!flag)
					{
						FontForLanguage val2 = new FontForLanguage();
						val2.language = "ru";
						val2.textMeshProFont = ruFont;
						val2.scaleRelativeToInitialFont = 1f;
						fontsForLanguages.Add(val2);
					}
					Core.Log("шрифты: язык ru -> " + ((Object)ruFont).name);
				}
			}
			catch (Exception ex)
			{
				Core.Log("шрифты err: " + ex.GetType().Name);
			}
		}

		public static bool SetupLanguage()
		{
			try
			{
				string language = Localization.language;
				int num;
				switch (language)
				{
				default:
					num = ((language == "Chinese") ? 1 : 0);
					break;
				case "ru":
				case "English":
				case "fr":
				case "ger":
				case "esp":
				case "jp":
					num = 1;
					break;
				}
				if (string.IsNullOrEmpty(language))
				{
					Localization.language = "ru";
				}
				Installed = true;
				if (num == 0)
				{
					Core.Log("язык игры не распознан (\"" + language + "\") — оставляем как есть");
				}
				return true;
			}
			catch (Exception ex)
			{
				Core.Log("язык err: " + ex.GetType().Name);
				return false;
			}
		}
	}
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
	public class ModBehaviour : MonoBehaviour
	{
		private void Update()
		{
			try
			{
				Core.Update();
			}
			catch
			{
			}
		}

		private void LateUpdate()
		{
			try
			{
				Core.LateUpdate();
			}
			catch
			{
			}
		}
	}
}
