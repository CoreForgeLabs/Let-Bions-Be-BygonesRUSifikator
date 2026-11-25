using Object = UnityEngine.Object;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using PixelCrushers;
using PixelCrushers.DialogueSystem;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace LbbbRU;

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

	private static readonly HashSet<int> filledDbs = new HashSet<int>();

	private static Dictionary<string, string> idEn;

	private static int fillByText;

	private static int fillById;

	private static int fillSkipped;

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
				filledDbs.Clear();
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
		if (!flag2)
		{
			Il2CppArrayBase<DialogueDatabase> valEarly = Resources.FindObjectsOfTypeAll<DialogueDatabase>();
			if (valEarly != null)
			{
				for (int j = 0; j < valEarly.Length; j++)
				{
					DialogueDatabase valDb = valEarly[j];
					if (!((Object)(object)valDb == (Object)null) && !filledDbs.Contains(((Object)(object)valDb).GetInstanceID()))
					{
						flag2 = true;
						Dg("новая база бесед — дозаполняю перевод");
						break;
					}
				}
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
					filledDbs.Add(((Object)(object)val4).GetInstanceID());
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
			Dg("заливка: полей=" + num3 + " баз=" + filledDbs.Count + " " + FillStats());
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

	public static void Dg(string s)
	{
		try
		{
			string dir = Path.Combine(GameDir, "UserData", "LBBB_RU");
			Directory.CreateDirectory(dir);
			string file = Path.Combine(dir, "diag.log");
			try
			{
				if (new FileInfo(file).Length > 4000000L)
				{
					File.Delete(file);
				}
			}
			catch
			{
			}
			File.AppendAllText(file, DateTime.Now.ToString("MM-dd HH:mm:ss") + " " + s + "\n", Encoding.UTF8);
		}
		catch
		{
		}
	}

	public static void Log(string s)
	{
		try
		{
			Dg(s);
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
				Dictionary<string, string> dictionary4 = new Dictionary<string, string>();
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
							dictionary4[key] = text2;
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
				idEn = dictionary4;
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

	internal static string RuForEntry(string key, string kind, string english, Dictionary<string, string> tr, out string source)
	{
		source = null;
		if (!string.IsNullOrEmpty(english))
		{
			string ru;
			if (enToRu != null && enToRu.TryGetValue(english, out ru) && !string.IsNullOrEmpty(ru))
			{
				source = "text";
				return ru;
			}
			string norm = Norm(english);
			if (!string.IsNullOrEmpty(norm) && normToRu != null && normToRu.TryGetValue(norm, out ru) && !string.IsNullOrEmpty(ru))
			{
				source = "text";
				return ru;
			}
		}
		string value;
		if (tr != null && tr.TryGetValue(key, out value) && !string.IsNullOrEmpty(value))
		{
			string en;
			if (idEn != null && idEn.TryGetValue(key, out en) && !string.IsNullOrEmpty(en))
			{
				if (string.IsNullOrEmpty(english) || Norm(en) == Norm(english))
				{
					source = "id";
					return value;
				}
				fillSkipped++;
				Dg("расхождение id " + key + ": ru.csv=\"" + Trim60(en) + "\" база=\"" + Trim60(english) + "\" — пропуск");
				return null;
			}
			source = "id";
			return value;
		}
		return null;
	}

	private static string Trim60(string s)
	{
		if (string.IsNullOrEmpty(s))
		{
			return "";
		}
		s = s.Replace("\n", " ");
		return s.Length <= 60 ? s : s.Substring(0, 60) + "…";
	}

	internal static void CountFill(string kind)
	{
		if (kind == "text")
		{
			fillByText++;
		}
		else
		{
			fillById++;
		}
	}

	internal static string FillStats()
	{
		string result = "по_тексту=" + fillByText + " по_id=" + fillById + " пропущено=" + fillSkipped;
		fillByText = 0;
		fillById = 0;
		fillSkipped = 0;
		return result;
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
