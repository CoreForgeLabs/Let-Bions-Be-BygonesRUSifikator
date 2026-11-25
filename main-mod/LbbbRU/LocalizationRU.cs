using ButtonClickedEvent = UnityEngine.UI.Button.ButtonClickedEvent;
using Object = UnityEngine.Object;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using PixelCrushers;
using PixelCrushers.DialogueSystem;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LbbbRU;

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
						string dlgEn = GetFieldValue(val2, "Dialogue Text");
						string src;
						string ruValue = Core.RuForEntry(id + "_" + val2.id + "_dlg", "dlg", dlgEn, tr, out src);
						if (!string.IsNullOrEmpty(ruValue) && SetOrAddField(val2, "ru", ruValue))
						{
							num++;
							Core.CountFill(src);
						}
						string mnuEn = GetFieldValue(val2, "Menu Text");
						string ruValue2 = Core.RuForEntry(id + "_" + val2.id + "_mnu", "mnu", mnuEn, tr, out src);
						if (!string.IsNullOrEmpty(ruValue2) && SetOrAddField(val2, "Menu Text ru", ruValue2))
						{
							num++;
							Core.CountFill(src);
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
					try
					{
						item.AddLanguage("ru");
					}
					catch (Exception exAdd)
					{
						Core.Log("AddLanguage ru в \"" + ((Object)item).name + "\": " + exAdd.GetType().Name + ": " + exAdd.Message);
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
