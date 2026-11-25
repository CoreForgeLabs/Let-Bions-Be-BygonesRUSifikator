using Object = UnityEngine.Object;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Game.UI;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LbbbRU;

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
