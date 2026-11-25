using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace LbbbCrew;

public class CrewBehaviour : MonoBehaviour
{
    private static readonly Color NameColor = new Color(0.87f, 0.95f, 0.88f, 0.9f);
    private static readonly Color TierColor = new Color(1f, 1f, 1f, 0.98f);
    private static readonly Color MutedColor = new Color(0.816f, 0.913f, 0.824f, 0.58f);
    private static readonly Color LineColor = new Color(0.843f, 1f, 0.976f, 0.45f);
    private static readonly Color BackdropColor = new Color(0.017f, 0.022f, 0.018f, 1f);

    private float next;
    private GameObject entry;
    private GameObject overlay;
    private TMP_FontAsset font;
    private TMP_Text template;
    private TMP_Text labelTmp;
    private GameObject layer;
    private bool nativeMode;
    private float lastClick;
    private RectTransform layerRoot;
    private bool labelLogged;
    private static string itemLabel;
    private static bool itemLabelRu;
    private static HarmonyLib.Harmony harmony;

    public static string ItemLabel
    {
        get
        {
            if (itemLabel != null && itemLabelRu == RusActive) return itemLabel;
            string v = null;
            try
            {
                string p = Path.Combine(DataDir, "crew_label.txt");
                if (File.Exists(p))
                {
                    foreach (string l in File.ReadAllLines(p))
                    {
                        string s = (l ?? "").Trim();
                        if (s.Length > 0) { v = s; break; }
                    }
                }
            }
            catch { }
            itemLabelRu = RusActive;
            itemLabel = string.IsNullOrEmpty(v) ? (RusActive ? "БЛАГОДАРНОСТИ" : "CREDITS") : v;
            return itemLabel;
        }
    }

    private static bool platePatchTried;

    private static void TryPatchPlate()
    {
        if (harmony != null) return;
        try
        {
            Type credits = null;
            foreach (System.Reflection.Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (a.GetName().Name != "LbbbRU") continue;
                credits = a.GetType("LbbbRU.Credits");
                break;
            }
            if (credits == null) return;
            System.Reflection.MethodInfo build = credits.GetMethod("Build", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (build == null) return;
            harmony = new HarmonyLib.Harmony("com.coreforgelabs.lbbb.crew.plate");
            harmony.Patch(build, prefix: new HarmonyLib.HarmonyMethod(typeof(CrewBehaviour).GetMethod("SkipOldPlate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)));
            Log("старая плашка отключена через Harmony");
        }
        catch (Exception e) { if (!platePatchTried) Log("Harmony: " + e.Message); }
        platePatchTried = true;
    }

    private static bool SkipOldPlate() { return false; }

    private static System.Collections.Generic.Dictionary<string, string> shortTr;
    private static bool visiblePatchTried;

    private static System.Reflection.PropertyInfo langProp;
    private static bool langSearched;

    private static string GameLang()
    {
        if (!langSearched)
        {
            langSearched = true;
            foreach (System.Reflection.Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] ts;
                try { ts = a.GetTypes(); } catch (System.Reflection.ReflectionTypeLoadException e) { ts = e.Types; } catch { continue; }
                for (int i = 0; i < ts.Length; i++)
                {
                    Type t = ts[i];
                    if (t == null) continue;
                    string n = t.FullName ?? "";
                    if (n != "Localization" && !n.EndsWith(".Localization")) continue;
                    System.Reflection.PropertyInfo pi = t.GetProperty("language", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                    if (pi != null) { langProp = pi; break; }
                }
                if (langProp != null) break;
            }
        }
        try { return langProp == null ? null : (langProp.GetValue(null) as string); } catch { return null; }
    }

    private static bool RusActive { get { return GameLang() == "ru"; } }

    private static void SetGameLang(string lang)
    {
        try { if (langProp != null) langProp.SetValue(null, lang); } catch (Exception e) { Log("set lang: " + e.Message); }
    }

    private static void CallModSetup()
    {
        try
        {
            foreach (System.Reflection.Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (a.GetName().Name != "LbbbRU") continue;
                Type t = a.GetType("LocalizationRU");
                if (t == null) continue;
                System.Reflection.MethodInfo mi = t.GetMethod("SetupLanguage", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (mi != null) mi.Invoke(null, null);
                return;
            }
        }
        catch (Exception e) { Log("mod setup: " + e.Message); }
    }

    private static readonly System.Collections.Generic.Dictionary<int, string> patchedOriginals = new System.Collections.Generic.Dictionary<int, string>();
    private static readonly System.Collections.Generic.Dictionary<int, TMP_Text> patchedRefs = new System.Collections.Generic.Dictionary<int, TMP_Text>();
    private static bool lastRus = true;

    private static void RevertTexts()
    {
        if (patchedOriginals.Count == 0) return;
        int n = 0;
        foreach (System.Collections.Generic.KeyValuePair<int, string> kv in patchedOriginals)
        {
            TMP_Text t;
            if (!patchedRefs.TryGetValue(kv.Key, out t)) continue;
            try
            {
                if (t != null && t.text != kv.Value) { t.text = kv.Value; n++; }
            }
            catch { }
        }
        patchedOriginals.Clear();
        patchedRefs.Clear();
        if (n > 0) Log("вернул исходных строк после ухода с русского: " + n);
    }

    private static bool settingsWired;
    private static float nextSettingsCheck;

    private static void EnsureSettingsLanguage()
    {
        if (Time.unscaledTime < nextSettingsCheck) return;
        nextSettingsCheck = Time.unscaledTime + 2f;
        try
        {
            UnityEngine.UI.Dropdown found = null;
            Il2CppArrayBase<UnityEngine.UI.Dropdown> all = Resources.FindObjectsOfTypeAll<UnityEngine.UI.Dropdown>();
            if (all == null) return;
            for (int i = 0; i < all.Length; i++)
            {
                UnityEngine.UI.Dropdown d = all[i];
                if (d == null) continue;
                bool vis = false;
                try { vis = d.gameObject.activeInHierarchy; } catch { }
                if (!vis) continue;
                string nm = "";
                try { nm = d.gameObject.name ?? ""; } catch { }
                if (nm != "Terrain Quality") continue;
                found = d;
                break;
            }
            if (found == null) return;
            var opts = found.options;
            if (opts == null || opts.Count < 3) return;
            string cur = null;
            try { cur = opts[2].text; } catch { }
            if (cur != "Русский")
            {
                try { opts[2].text = "Русский"; } catch (Exception e) { Log("переименование пункта: " + e.Message); }
                try { found.RefreshShownValue(); } catch { }
                Log("настройки: пункт языка 2 переименован в \"Русский\" (было \"" + (cur ?? "null") + "\")");
            }
            if (!settingsWired)
            {
                settingsWired = true;
                try
                {
                    System.Action<int> handler = OnSettingsLanguage;
                    found.onValueChanged.AddListener(handler);
                    Log("настройки: слушатель смены языка подключён");
                }
                catch (Exception e) { Log("настройки: слушатель не подключился: " + e.Message); }
            }
        }
        catch (Exception e) { Log("ensure settings: " + e.Message); }
    }

    private static void StepModLanguage(int dir)
    {
        try
        {
            foreach (System.Reflection.Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (a.GetName().Name != "LbbbRU") continue;
                Type t = a.GetType("LocalizationRU");
                if (t == null) return;
                System.Reflection.MethodInfo mi = t.GetMethod("StepLanguage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (mi == null) return;
                mi.Invoke(null, new object[] { dir });
                return;
            }
        }
        catch (Exception e) { Log("шаг языка мода: " + e.Message); }
    }

    private static void OnSettingsLanguage(int idx)
    {
        try
        {
            if (idx == 2)
            {
                for (int i = 0; i < 8 && GameLang() != "ru"; i++) StepModLanguage(1);
                if (GameLang() != "ru")
                {
                    SetGameLang("ru");
                    CallModSetup();
                }
                Log("настройки: выбран \"Русский\", язык=" + (GameLang() ?? "?"));
            }
            else
            {
                Log("настройки: выбран пункт " + idx + ", язык " + (GameLang() ?? "?"));
            }
        }
        catch (Exception e) { Log("settings lang: " + e.Message); }
    }

    private static void TryPatchVisible()
    {
        if (visiblePatchTried) return;
        visiblePatchTried = true;
        try
        {
            Type core = null;
            foreach (System.Reflection.Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (a.GetName().Name != "LbbbRU") continue;
                core = a.GetType("LbbbRU.Core");
                break;
            }
            if (core == null) { Log("short: тип LbbbRU.Core не найден"); return; }
            System.Reflection.MethodInfo tv = core.GetMethod("TranslateVisible", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static, null, new Type[] { typeof(string) }, null);
            if (tv == null) { Log("short: TranslateVisible не найден"); return; }
            HarmonyLib.Harmony h = new HarmonyLib.Harmony("com.coreforgelabs.lbbb.crew.short");
            h.Patch(tv, postfix: new HarmonyLib.HarmonyMethod(typeof(CrewBehaviour).GetMethod("TranslatePostfix", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)));
            Log("короткие строки (менее 4 символов) подключены: ключей " + ShortDict.Count + "; словарь реплик: " + TrAll.Count + " строк");
            try
            {
                int ml = 0;
                foreach (var kv in TrAll) if (kv.Key.IndexOf('\n') >= 0) ml++;
                Log("ключей с переводом строки: " + ml);
                string probe = "[ EVALUATING PERSONALITY ] \r\nSCI: PREDOMINANT\r";
                string pv;
                Log("самопроверка CRLF: " + (TryTr(probe, out pv) ? pv.Replace("\r\n", " / ").Replace("\n", " / ") : "НЕ НАЙДЕНО"));
            }
            catch (Exception ex) { Log("probe: " + ex.Message); }
        }
        catch (Exception e) { Log("short: " + e.Message); }
    }

    private static System.Collections.Generic.Dictionary<string, string> trAll;
    private static System.Collections.Generic.Dictionary<string, string> trShort;

    private static System.Collections.Generic.Dictionary<string, string> TrAll
    {
        get
        {
            if (trAll != null) return trAll;
            trAll = new System.Collections.Generic.Dictionary<string, string>();
            trShort = new System.Collections.Generic.Dictionary<string, string>();
            try
            {
                string file = Path.Combine(DataDir, "ru.csv");
                if (File.Exists(file))
                {
                    string[] lines = File.ReadAllLines(file, Encoding.UTF8);
                    for (int i = 1; i < lines.Length; i++)
                    {
                        string line = lines[i];
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        List<string> f = ParseCsvFields(line);
                        if (f.Count < 5) continue;
                        string en = f[3].Replace("\\n", "\n");
                        string ru = f[4].Replace("\\n", "\n");
                        if (en.Length == 0 || ru.Length == 0) continue;
                        if (!trAll.ContainsKey(en)) trAll[en] = ru;
                        string trimmed = en.Trim();
                        if (trimmed.Length > 0 && !trAll.ContainsKey(trimmed)) trAll[trimmed] = ru;
                        // "[ proceed ]" и "[proceed]" — один и тот же пункт, регистрируем оба написания
                        System.Text.RegularExpressions.Match bm = System.Text.RegularExpressions.Regex.Match(trimmed, @"^\[\s*([^\[\]]{1,40}?)\s*\]$");
                        if (bm.Success)
                        {
                            string inner = bm.Groups[1].Value.Trim();
                            string tight = "[" + inner + "]";
                            string loose = "[ " + inner + " ]";
                            if (!trAll.ContainsKey(tight)) trAll[tight] = ru;
                            if (!trAll.ContainsKey(loose)) trAll[loose] = ru;
                        }
                        if (en.Length < 4 && !trShort.ContainsKey(en)) trShort[en] = ru;
                    }
                }
            }
            catch (Exception e) { Log("dict: " + e.Message); }
            return trAll;
        }
    }

    private static System.Collections.Generic.Dictionary<string, string> ShortDict
    {
        get
        {
            if (trAll == null) { System.Collections.Generic.Dictionary<string, string> unused = TrAll; }
            return trShort ?? new System.Collections.Generic.Dictionary<string, string>();
        }
    }
    private static System.Collections.Generic.Dictionary<string, string> assetTr;
    private static bool assetsLocalized;

    private static System.Collections.Generic.Dictionary<string, string> AssetDict
    {
        get
        {
            if (assetTr != null) return assetTr;
            assetTr = new System.Collections.Generic.Dictionary<string, string>();
            try
            {
                string file = Path.Combine(DataDir, "assets.csv");
                if (File.Exists(file))
                {
                    string[] lines = File.ReadAllLines(file, Encoding.UTF8);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i];
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        int p1 = line.IndexOf(';');
                        if (p1 <= 0) continue;
                        int p2 = line.IndexOf(';', p1 + 1);
                        if (p2 < 0) continue;
                        int p3 = line.IndexOf(';', p2 + 1);
                        if (p3 < 0) continue;
                        int p4 = line.IndexOf(';', p3 + 1);
                        if (p4 < 0) continue;
                        string kind = line.Substring(0, p1);
                        if (kind != "actor" && kind != "item" && kind != "loc") continue;
                        string id = line.Substring(p1 + 1, p2 - p1 - 1);
                        string fld = line.Substring(p2 + 1, p3 - p2 - 1);
                        string ru = line.Substring(p4 + 1).Replace("\\n", "\n");
                        if (ru.Length == 0) continue;
                        assetTr[kind + "|" + id + "|" + fld] = ru;
                    }
                }
            }
            catch (Exception e) { Log("asset dict: " + e.Message); }
            return assetTr;
        }
    }

    private static System.Collections.Generic.Dictionary<string, string> tableTr;
    private static float nextTableFill;
    private static float nextTableReset;
    private static readonly HashSet<int> tableDone = new HashSet<int>();

    private static System.Collections.Generic.Dictionary<string, string> TableDict
    {
        get
        {
            if (tableTr != null) return tableTr;
            tableTr = new System.Collections.Generic.Dictionary<string, string>();
            try
            {
                string file = Path.Combine(DataDir, "ui_tables.csv");
                if (File.Exists(file))
                {
                    string[] lines = File.ReadAllLines(file, Encoding.UTF8);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (string.IsNullOrWhiteSpace(lines[i])) continue;
                        int k = lines[i].IndexOf(';');
                        if (k <= 0) continue;
                        int k2 = lines[i].IndexOf(';', k + 1);
                        string key = lines[i].Substring(0, k);
                        string ru = k2 < 0 ? lines[i].Substring(k + 1) : lines[i].Substring(k + 1, k2 - k - 1);
                        if (key == "key" || ru.Length == 0 || ru == "#skip") continue;
                        tableTr[key] = ru.Replace("\\n", "\n");
                    }
                }
            }
            catch (Exception e) { Log("table dict: " + e.Message); }
            return tableTr;
        }
    }

    // заливает язык "ru" в TextTable (журнал "Tasks and Clues" и прочие)
    private static void FillTaskTables()
    {
        if (Time.unscaledTime < nextTableFill) return;
        nextTableFill = Time.unscaledTime + 3f;
        if (Time.unscaledTime > nextTableReset)
        {
            nextTableReset = Time.unscaledTime + 30f;
            tableDone.Clear();
        }
        string file = Path.Combine(DataDir, "ui_tables.csv");
        if (!File.Exists(file)) return;
        var map = TableDict;
        if (map.Count == 0) return;
        var all = Resources.FindObjectsOfTypeAll<PixelCrushers.TextTable>();
        if (all == null) return;
        int tables = 0, fields = 0;
        foreach (var t in all)
        {
            if (t == null) continue;
            int tid = 0;
            try { tid = t.GetInstanceID(); } catch { }
            if (tid != 0 && tableDone.Contains(tid)) continue;
            try
            {
                string tn = null; try { tn = t.name; } catch { }
                bool hasRu = false;
                var langs = t.GetLanguageNames();
                if (langs != null) for (int i = 0; i < langs.Count; i++) if (langs[i] == "ru") { hasRu = true; break; }
                if (!hasRu)
                {
                    try { t.AddLanguage("ru"); }
                    catch (Exception e) { Log("таблица '" + tn + "': AddLanguage " + e.GetType().Name + ": " + e.Message); }
                }
                var fn = t.GetFieldNames();
                if (fn == null) continue;
                int n = 0;
                for (int i = 0; i < fn.Count; i++)
                {
                    string key = fn[i];
                    string ru;
                    if (!map.TryGetValue(key, out ru) || string.IsNullOrEmpty(ru)) continue;
                    string have = null;
                    try { have = t.GetFieldTextForLanguage(key, "ru"); } catch { }
                    if (have == ru) continue;
                    try { t.SetFieldTextForLanguage(key, "ru", ru); n++; } catch { }
                }
                if (tn == "Tasks and Clues")
                {
                    int added = 0;
                    foreach (var kv in map)
                    {
                        string dk = kv.Key;
                        if (dk.Length < 2 || dk[0] != '-') continue;
                        bool exists = false;
                        for (int i = 0; i < fn.Count; i++) if (fn[i] == dk) { exists = true; break; }
                        if (exists) continue;
                        try
                        {
                            t.AddField(dk);
                            try { t.SetFieldTextForLanguage(dk, "English", dk.Substring(1)); } catch { }
                            t.SetFieldTextForLanguage(dk, "ru", kv.Value);
                            added++;
                        }
                        catch { }
                    }
                    if (added > 0) Log("  'Tasks and Clues': добавлено полей с дефисом " + added);
                }
                fields += n;
                tables++;
                if (tid != 0) tableDone.Add(tid);
                Log("таблица '" + tn + "': язык ru " + (hasRu ? "уже был" : "добавлен") + ", переведено " + n);
                if (tn == "Tasks and Clues")
                {
                    try
                    {
                        StringBuilder fb = new StringBuilder();
                        for (int i = 0; i < fn.Count; i++)
                        {
                            string en2 = null; try { en2 = t.GetFieldTextForLanguage(fn[i], "English"); } catch { }
                            fb.Append(fn[i]).Append(';').Append(en2 ?? "").Append((char)10);
                        }
                        File.WriteAllText(Path.Combine(DataDir, "probe_fields.txt"), fb.ToString(), Encoding.UTF8);
                        Log("  поля 'Tasks and Clues' выгружены: " + fn.Count);
                    }
                    catch (Exception e) { Log("  dump fields err " + e.Message); }
                }
            }
            catch (Exception e) { Log("таблица err: " + e.Message); if (tid != 0) tableDone.Add(tid); }
        }
        if (tables > 0) Log("таблицы: обработано " + tables + ", полей записано " + fields);
    }

    private static float nextProbe;
    private static bool probeLogged;

    private static void Probe()
    {
        if (Time.unscaledTime < nextProbe || probeLogged) return;
        nextProbe = Time.unscaledTime + 5f;
        try
        {
            var dbs = Resources.FindObjectsOfTypeAll<PixelCrushers.DialogueSystem.DialogueDatabase>();
            var md = PixelCrushers.DialogueSystem.DialogueManager.masterDatabase;
            Log("поиск базы: FindAll=" + (dbs == null ? -1 : dbs.Length) + " masterDatabase=" + (md == null ? "null" : ("есть, актёров=" + (md.actors == null ? -1 : md.actors.Count))));
        }
        catch (Exception e) { Log("поиск базы: ошибка " + e.Message); }
    }

    private static float nextAssetPass;

    private static void TryLocalizeAssets()
    {
        if (Time.unscaledTime < nextAssetPass) return;
        nextAssetPass = Time.unscaledTime + 5f;
        var dbs = Resources.FindObjectsOfTypeAll<PixelCrushers.DialogueSystem.DialogueDatabase>();
        if (dbs == null || dbs.Length == 0)
        {
            Probe();
            return;
        }
        bool first = !assetsLocalized;
        assetsLocalized = true;
        probeLogged = true;
        int n = 0;
        try
        {
            var dict = AssetDict;
            if (dict.Count == 0) return;
            for (int i = 0; i < dbs.Length; i++)
            {
                var dbi = dbs[i];
                if (dbi == null) continue;
                n += LocalizeAssets(dbi.actors, "actor", dict);
                n += LocalizeAssets(dbi.items, "item", dict);
                n += LocalizeAssets(dbi.locations, "loc", dict);
            }
        }
        catch (Exception e) { Log("localize assets: " + e.Message); }
        if (n > 0 || first) Log("поля ru у ассетов записано: " + n + " (словарь " + AssetDict.Count + ")");
        if (!first) return;
        var db = dbs[0];
        try
        {
            Log("диагностика: актёров=" + db.actors.Count + " предметов=" + db.items.Count + " локаций=" + db.locations.Count);
            Log("диагностика актёры: " + AssetStats(db.actors));
            Log("диагностика предметы: " + AssetStats(db.items));
            Log("диагностика локации: " + AssetStats(db.locations));
        }
        catch (Exception e) { Log("диагностика: " + e.Message); }
        try
        {
            var a2 = FindActor(db.actors, "Chenki Muscles");
            if (a2 != null)
            {
                Log("проверка LookupValue: " + PixelCrushers.DialogueSystem.Field.LookupValue(a2.fields, "Display Name"));
                Log("проверка LookupLocalized: " + PixelCrushers.DialogueSystem.Field.LookupLocalizedValue(a2.fields, "Display Name"));
            }
            try { Log("проверка DialogueLua: " + PixelCrushers.DialogueSystem.DialogueLua.GetActorField("Chenki Muscles", "Display Name")); }
            catch (Exception ex) { Log("DialogueLua: " + ex.Message); }
        }
        catch (Exception e) { Log("localize проверка: " + e.Message); }
    }

    private static string AssetStats<T>(Il2CppSystem.Collections.Generic.List<T> list) where T : PixelCrushers.DialogueSystem.Asset
    {
        if (list == null) return "нет списка";
        int total = 0, withRu = 0;
        List<string> examples = new List<string>();
        for (int i = 0; i < list.Count; i++)
        {
            var a = list[i];
            if (a == null) continue;
            total++;
            var fields = a.fields;
            if (fields == null) continue;
            bool ru = false;
            string dn = "";
            for (int j = 0; j < fields.Count; j++)
            {
                var f = fields[j];
                if (f == null || f.title == null) continue;
                if (f.title.EndsWith(" ru")) ru = true;
                if (f.title == "Display Name") dn = f.value ?? "";
            }
            if (ru) withRu++;
            else if (examples.Count < 4) examples.Add(dn.Length > 50 ? dn.Substring(0, 50) : dn);
        }
        return "всего=" + total + " сRu=" + withRu + " безRu=" + (total - withRu) + (examples.Count > 0 ? " | например: " + string.Join(" / ", examples.ToArray()) : "");
    }

    private static PixelCrushers.DialogueSystem.Actor FindActor(Il2CppSystem.Collections.Generic.List<PixelCrushers.DialogueSystem.Actor> list, string name)
    {
        if (list == null) return null;
        for (int i = 0; i < list.Count; i++)
        {
            var a = list[i];
            if (a != null && a.Name == name) return a;
        }
        return null;
    }

    private static int LocalizeAssets<T>(Il2CppSystem.Collections.Generic.List<T> list, string kind, System.Collections.Generic.Dictionary<string, string> dict) where T : PixelCrushers.DialogueSystem.Asset
    {
        if (list == null) return 0;
        int n = 0;
        for (int i = 0; i < list.Count; i++)
        {
            var a = list[i];
            if (a == null) continue;
            var fields = a.fields;
            if (fields == null) continue;
            int id = a.id;
            for (int j = 0; j < fields.Count; j++)
            {
                var f = fields[j];
                if (f == null) continue;
                string title = f.title ?? "";
                if (title.Length == 0) continue;
                if (title.EndsWith(" fr") || title.EndsWith(" ger") || title.EndsWith(" esp") || title.EndsWith(" jp") || title.EndsWith(" Chinese") || title.EndsWith(" rs") || title.EndsWith(" ru")) continue;
                string ru;
                if (!dict.TryGetValue(kind + "|" + id + "|" + title, out ru) || ru.Length == 0) continue;
                string have = null;
                try { have = PixelCrushers.DialogueSystem.Field.LookupValue(fields, title + " ru"); } catch { }
                if (have == ru) continue;
                try { PixelCrushers.DialogueSystem.Field.SetValue(fields, title + " ru", ru); n++; }
                catch (Exception e) { Log("add field: " + e.Message); return n; }
            }
        }
        return n;
    }

    private static List<string> ParseCsvFields(string line)
    {
        List<string> res = new List<string>();
        StringBuilder sb = new StringBuilder();
        bool q = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (q)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                    else q = false;
                }
                else sb.Append(c);
                continue;
            }
            if (c == '"') q = true;
            else if (c == ';') { res.Add(sb.ToString()); sb.Clear(); }
            else sb.Append(c);
        }
        res.Add(sb.ToString());
        return res;
    }

    private static void TranslatePostfix(string s, ref string __result)
    {
        if (string.IsNullOrEmpty(s)) return;
        if (!string.IsNullOrEmpty(__result) && __result != s) return;
        string v;
        if (TryTr(s, out v) && !string.IsNullOrEmpty(v) && v != s) { __result = v; return; }
        if (s.Length < 4 && ShortDict.TryGetValue(s, out v) && !string.IsNullOrEmpty(v)) { __result = v; return; }
        string composed = TryComposed(s) ?? TryQuotes(s);
        if (composed != null) { __result = composed; return; }
        string templ = TryTemplate(s);
        if (templ != null) { __result = templ; return; }
        string bare = StripNoParse(s);
        if (bare != s && bare.Length >= 3)
        {
            if (TryTr(bare, out v) && !string.IsNullOrEmpty(v) && v != bare) { __result = v; return; }
            string comp2 = TryComposed(bare) ?? TryQuotes(bare);
            if (comp2 != null) { __result = comp2; return; }
        }
    }
    private static bool TryTr(string s, out string v)
    {
        v = null;
        if (string.IsNullOrEmpty(s)) return false;
        if (TrAll.TryGetValue(s, out v) && !string.IsNullOrEmpty(v)) return true;
        string t = s.Trim();
        if (t != s && TrAll.TryGetValue(t, out v) && !string.IsNullOrEmpty(v)) return true;
        string nl = (t.IndexOf('\r') >= 0) ? t.Replace("\r\n", "\n").Replace("\r", "\n") : t;
        if (nl != t && TrAll.TryGetValue(nl, out v) && !string.IsNullOrEmpty(v)) return true;
        if (nl.IndexOf('\n') >= 0)
        {
            string crlf = nl.Replace("\n", "\r\n");
            if (crlf != nl && TrAll.TryGetValue(crlf, out v) && !string.IsNullOrEmpty(v)) return true;
        }
        return false;
    }
    private static string TryComposed(string s)
    {
        string v;
        string whole = s.Trim();
        if (whole.Length >= 3 && TryTr(whole, out v) && !string.IsNullOrEmpty(v) && v != whole) return v;
        int i = 0;
        while (i < s.Length && !char.IsLetterOrDigit(s[i])) i++;
        if (i == 0 || i > 6) return null;
        string head = s.Substring(0, i);
        string tail = s.Substring(i).TrimEnd();
        if (tail.Length < 3) return null;
        if (TryTr(tail, out v) && !string.IsNullOrEmpty(v)) return head + v;
        return null;
    }
    private static string TryQuotes(string s)
    {
        if (s.Length < 5) return null;
        char a = s[0];
        char z = s[s.Length - 1];
        bool pair = (a == '"' && z == '"') || (a == '\u00AB' && z == '\u00BB') || (a == '\u201C' && z == '\u201D');
        if (!pair) return null;
        string inner = s.Substring(1, s.Length - 2).Trim();
        if (inner.Length < 3) return null;
        string v;
        if (TryTr(inner, out v) && !string.IsNullOrEmpty(v) && v != inner) return a + v + z;
        string comp = TryComposed(inner);
        if (comp != null) return a + comp + z;
        return null;
    }

    private static string TryTemplate(string s)
    {
        bool tx = s.IndexOf("> Use of cartridge", StringComparison.Ordinal) >= 0
               || s.IndexOf("> Payment:", StringComparison.Ordinal) >= 0
               || s.IndexOf("> Location:", StringComparison.Ordinal) >= 0
               || s.IndexOf("> Оплата:", StringComparison.Ordinal) >= 0
               || s.IndexOf("> Доступно:", StringComparison.Ordinal) >= 0;
        if (!tx) return null;
        string r = s;
        r = r.Replace("> Use of cartridge", "> Использование картриджа");
        r = r.Replace("> Date:", "> Дата:");
        r = r.Replace("> Payment:", "> Оплата:");
        r = r.Replace("> Available:", "> Доступно:");
        r = r.Replace("> Location:", "> Место:");
        r = Credits(r);
        return r == s ? null : r;
    }

    private static string Credits(string s)
    {
        StringBuilder sb = new StringBuilder(s.Length + 8);
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            bool digitBefore = i > 0 && char.IsDigit(s[i - 1]);
            if (digitBefore && c == 'c' && i + 1 < s.Length && s[i + 1] == 'r' && (i + 2 >= s.Length || !char.IsLetterOrDigit(s[i + 2])))
            {
                sb.Append(" кр");
                i++;
                continue;
            }
            if (digitBefore && c == 'c' && (i + 1 >= s.Length || !char.IsLetterOrDigit(s[i + 1])))
            {
                sb.Append(" кр");
                continue;
            }
            sb.Append(c);
        }
        return sb.ToString();
    }
    private static int missRu, missMru;
    private static void TryDumpFields()
    {
        string trigger = Path.Combine(DataDir, "crew_dump.txt");
        if (!File.Exists(trigger)) return;
        try { File.Delete(trigger); } catch { }
        missRu = 0; missMru = 0;
        StringBuilder sb = new StringBuilder();
        sb.Append("kind;id;name;field;value\n");
        int n = 0;
        try
        {
            var dbs = Resources.FindObjectsOfTypeAll<PixelCrushers.DialogueSystem.DialogueDatabase>();
            for (int i = 0; i < dbs.Length; i++)
            {
                var db = dbs[i];
                if (db == null) continue;
                n += DumpAssets(sb, "conv", db.conversations);
                n += DumpAssets(sb, "actor", db.actors);
                n += DumpAssets(sb, "item", db.items);
                n += DumpAssets(sb, "loc", db.locations);
                n += DumpAssets(sb, "var", db.variables);        n += DumpEntries(sb, db.conversations);

            }
        }
        catch (Exception e) { sb.Append("err;0;;;").Append(Csv(e.Message)).Append("\n"); }
        try { File.WriteAllText(Path.Combine(DataDir, "fields_dump.csv"), sb.ToString(), Encoding.UTF8); } catch (Exception e) { Log("dump write: " + e.Message); }
        Log("дамп полей: " + n + " значений, входов без ru " + missRu + ", без Menu Text ru " + missMru);
    }

    private static int DumpEntries(StringBuilder sb, Il2CppSystem.Collections.Generic.List<PixelCrushers.DialogueSystem.Conversation> list)
    {
        if (list == null) return 0;
        int n = 0;
        for (int c = 0; c < list.Count; c++)
        {
            var conv = list[c];
            if (conv == null) continue;
            string title = "";
            int cid = 0;
            try { title = conv.Title ?? ""; } catch { }
            try { cid = conv.id; } catch { }
            var ents = conv.dialogueEntries;
            if (ents == null) continue;
            for (int e = 0; e < ents.Count; e++)
            {
                var de = ents[e];
                if (de == null) continue;
                int eid = 0;
                try { eid = de.id; } catch { }
                var fields = de.fields;
                if (fields == null) continue;
                string dlg = "", mnu = "", ru = "", mru = "";
                for (int j = 0; j < fields.Count; j++)
                {
                    var f = fields[j];
                    if (f == null) continue;
                    string ft = f.title ?? "";
                    string v = f.value ?? "";
                    if (v.Length == 0) continue;
                    if (ft == "Dialogue Text") dlg = v;
                    else if (ft == "Menu Text") mnu = v;
                    else if (ft == "ru") ru = v;
                    else if (ft == "Menu Text ru") mru = v;
                    else continue;
                    sb.Append("entry;").Append(cid).Append('/').Append(eid).Append(';').Append(Csv(title)).Append(';').Append(Csv(ft)).Append(';').Append(Csv(v)).Append('\n');
                    n++;
                }
                if (dlg.Length > 0 && ru.Length == 0) missRu++;
                if (mnu.Length > 0 && mru.Length == 0) missMru++;
            }
        }
        return n;
    }
    private static int DumpAssets<T>(StringBuilder sb, string kind, Il2CppSystem.Collections.Generic.List<T> list) where T : PixelCrushers.DialogueSystem.Asset
    {
        if (list == null) return 0;
        int n = 0;
        for (int i = 0; i < list.Count; i++)
        {
            var a = list[i];
            if (a == null) continue;
            string nm = "";
            try { nm = a.Name ?? ""; } catch { }
            var fields = a.fields;
            if (fields == null) continue;
            for (int j = 0; j < fields.Count; j++)
            {
                var f = fields[j];
                if (f == null) continue;
                string v = f.value ?? "";
                if (v.Length == 0) continue;
                sb.Append(kind).Append(';').Append(a.id).Append(';').Append(Csv(nm)).Append(';').Append(Csv(f.title ?? "")).Append(';').Append(Csv(v)).Append('\n');
                n++;
            }
        }
        return n;
    }

    private static string Csv(string s)
    {
        if (s == null) return "";
        bool q = s.IndexOf(';') >= 0 || s.IndexOf('"') >= 0 || s.IndexOf('\n') >= 0 || s.IndexOf('\r') >= 0;
        string r = s.Replace("\"", "\"\"");
        return q ? "\"" + r + "\"" : r;
    }



    public static string DataDir { get { return Path.Combine(Paths.PluginPath, "LBBB_RU"); } }

    public static void Log(string s)
    {
        try { File.AppendAllText(Path.Combine(DataDir, "crew_log.txt"), DateTime.Now.ToString("HH:mm:ss ") + s + "\n"); } catch { }
    }

    private void CheckNativeMouse()
    {
        if (!nativeMode || entry == null || overlay != null) return;
        if (!entry.activeInHierarchy) return;
        Canvas chk = entry.GetComponentInParent<Canvas>();
        if (chk != null && !chk.isActiveAndEnabled) return;
        RectTransform g = entry.GetComponent<RectTransform>();
        if (g == null) return;
        Canvas c = entry.GetComponentInParent<Canvas>();
        Camera cam = null;
        if (c != null && c.renderMode != RenderMode.ScreenSpaceOverlay) cam = c.worldCamera;
        if (cam == null && c != null && c.renderMode != RenderMode.ScreenSpaceOverlay) cam = Camera.main;
        bool over = RectTransformUtility.RectangleContainsScreenPoint(g, UnityEngine.Input.mousePosition, cam);
        if (labelTmp != null)
        {
            Color target = over ? Color.white : template.color;
            if (labelTmp.color != target) labelTmp.color = target;
        }
        if (over && UnityEngine.Input.GetMouseButtonDown(0)) OnCrewClick();
    }

    // ---- заливка полей "ru" во ВСЕ базы диалогов + инвентаризация ----
    private static readonly HashSet<int> dbDone = new HashSet<int>();
    private static readonly HashSet<string> invSeen = new HashSet<string>();
    private static readonly HashSet<string> missSeen = new HashSet<string>();
    private static float nextDbScan;
    private static float nextLabelWrite;
    private static float nextFullScan;
    private static int fixedTotal, clearedTotal;
    private static System.Collections.Generic.Dictionary<string, string> idEn;

    private static System.Collections.Generic.Dictionary<string, string> IdEnDict
    {
        get
        {
            if (idEn != null) return idEn;
            idEn = new System.Collections.Generic.Dictionary<string, string>();
            try
            {
                string file = Path.Combine(DataDir, "ru.csv");
                if (File.Exists(file))
                {
                    string[] lines = File.ReadAllLines(file, Encoding.UTF8);
                    for (int i = 1; i < lines.Length; i++)
                    {
                        if (string.IsNullOrWhiteSpace(lines[i])) continue;
                        List<string> f = ParseCsvFields(lines[i]);
                        if (f.Count < 5) continue;
                        string en = f[3].Replace("\\n", "\n");
                        if (en.Trim().Length == 0) continue;
                        string key = f[0] + "_" + f[1] + "_" + f[2];
                        if (!idEn.ContainsKey(key)) idEn[key] = en;
                    }
                }
            }
            catch (Exception e) { Log("idEn: " + e.Message); }
            return idEn;
        }
    }

    private static string NormTxt(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = s.Replace("\\n", " ").Replace("\n", " ").Replace("\r", " ").Replace((char)160, ' ');
        s = System.Text.RegularExpressions.Regex.Replace(s, @"\[/?[a-zA-Z0-9=#.,]+\]", " ");
        s = System.Text.RegularExpressions.Regex.Replace(s, "<[^>]*>", " ");
        s = s.Replace("**", "").Replace("--", "").ToLowerInvariant();
        s = System.Text.RegularExpressions.Regex.Replace(s, @"[^0-9a-zа-яё ]+", " ");
        s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ").Trim();
        return s;
    }

    private static bool Similar(string a, string b)
    {
        string x = NormTxt(a);
        string y = NormTxt(b);
        if (x.Length == 0 || y.Length == 0) return false;
        if (x == y) return true;
        string[] wa = x.Split(' ');
        string[] wb = y.Split(' ');
        System.Collections.Generic.HashSet<string> sa = new System.Collections.Generic.HashSet<string>(wa);
        int inter = 0;
        System.Collections.Generic.HashSet<string> seen = new System.Collections.Generic.HashSet<string>();
        for (int i = 0; i < wb.Length; i++)
        {
            if (sa.Contains(wb[i]) && seen.Add(wb[i])) inter++;
        }
        int union = sa.Count + new System.Collections.Generic.HashSet<string>(wb).Count - inter;
        double jac = union > 0 ? (double)inter / union : 0.0;
        return jac >= 0.34;
    }

    private class DbJob
    {
        public PixelCrushers.DialogueSystem.DialogueDatabase db;
        public int conv, entry;
        public int filled, miss, fixedNow, clearedNow;
        public readonly StringBuilder inv = new StringBuilder();
        public readonly StringBuilder ms = new StringBuilder();
    }

    private static readonly List<DbJob> dbJobs = new List<DbJob>();
    private static readonly HashSet<int> dbQueued = new HashSet<int>();
    private static int jobChunk = 24;
    private static int pendNew, pendFilled, pendMiss, pendFixed, pendCleared;

    private static void LocalizeDatabases()
    {
        if (!lbbbFillPatched) PatchLbbbFill();
        if (!RusActive) return;
        if (Time.unscaledTime >= nextDbScan)
        {
            nextDbScan = Time.unscaledTime + 1f;
            if (Time.unscaledTime > nextFullScan)
            {
                nextFullScan = Time.unscaledTime + 30f;
                dbDone.Clear();
                dbQueued.Clear();
            }
            ScanNewDbs();
        }
        RunJobs();
    }

    private static void ScanNewDbs()
    {
        try
        {
            var all = Resources.FindObjectsOfTypeAll<PixelCrushers.DialogueSystem.DialogueDatabase>();
            if (all == null) return;
            for (int i = 0; i < all.Length; i++)
            {
                var db = all[i];
                if (db == null) continue;
                int id = db.GetInstanceID();
                if (dbDone.Contains(id) || dbQueued.Contains(id)) continue;
                dbQueued.Add(id);
                dbJobs.Add(new DbJob { db = db });
                pendNew++;
            }
        }
        catch (Exception e) { Log("db scan: " + e.Message); }
    }

    private static void RunJobs()
    {
        if (dbJobs.Count == 0)
        {
            FlushScanLog();
            return;
        }
        var sw = System.Diagnostics.Stopwatch.StartNew();
        int guard = 0;
        while (dbJobs.Count > 0 && guard < 8 && sw.ElapsedMilliseconds < 4)
        {
            guard++;
            var job = dbJobs[0];
            bool done = false;
            try { done = StepJob(job); }
            catch (Exception e) { Log("db fill: " + e.Message); done = true; }
            if (done)
            {
                FinishJob(job);
                dbJobs.RemoveAt(0);
            }
        }
        long spent = sw.ElapsedMilliseconds;
        if (spent > 6 && jobChunk > 8) jobChunk = Mathf.Max(8, jobChunk / 2);
        else if (spent < 2 && jobChunk < 240) jobChunk += 8;
        if (dbJobs.Count == 0) FlushScanLog();
    }

    private static bool StepJob(DbJob job)
    {
        var convs = job.db != null ? job.db.conversations : null;
        if (convs == null) return true;
        int processed = 0;
        while (job.conv < convs.Count)
        {
            var conv = convs[job.conv];
            if (conv == null) { job.conv++; job.entry = 0; continue; }
            int cid = conv.id;
            var entries = conv.dialogueEntries;
            if (entries == null) { job.conv++; job.entry = 0; continue; }
            while (job.entry < entries.Count)
            {
                var e = entries[job.entry];
                job.entry++;
                if (e == null) continue;
                var fields = e.fields;
                if (fields == null) continue;
                string dlg = PixelCrushers.DialogueSystem.Field.LookupValue(fields, "Dialogue Text");
                string mnu = PixelCrushers.DialogueSystem.Field.LookupValue(fields, "Menu Text");
                if (!string.IsNullOrEmpty(dlg)) StepField(fields, cid, e.id, "dlg", "ru", dlg, job.inv, job.ms, ref job.filled, ref job.miss, ref job.fixedNow, ref job.clearedNow);
                if (!string.IsNullOrEmpty(mnu)) StepField(fields, cid, e.id, "mnu", "Menu Text ru", mnu, job.inv, job.ms, ref job.filled, ref job.miss, ref job.fixedNow, ref job.clearedNow);
                processed++;
                if (processed >= jobChunk) return false;
            }
            job.conv++;
            job.entry = 0;
        }
        return true;
    }

    private static void FinishJob(DbJob job)
    {
        try { if (job.inv.Length > 0) AppendFile(Path.Combine(DataDir, "crew_entries.csv"), job.inv.ToString()); } catch { }
        try { if (job.ms.Length > 0) AppendFile(Path.Combine(DataDir, "crew_misses.csv"), job.ms.ToString()); } catch { }
        try { dbDone.Add(job.db.GetInstanceID()); } catch { }
        pendFilled += job.filled;
        pendMiss += job.miss;
        pendFixed += job.fixedNow;
        pendCleared += job.clearedNow;
    }

    private static void FlushScanLog()
    {
        if (pendFilled == 0 && pendFixed == 0 && pendCleared == 0 && pendNew == 0 && pendMiss == 0) return;
        int fresh = pendNew;
        if (fresh > 0) Log("базы диалогов: новых " + fresh + ", записано ru " + pendFilled + ", без перевода " + pendMiss + " (всего баз " + dbDone.Count + ")");
        if (pendFilled > 0 || pendFixed > 0 || pendCleared > 0)
        {
            fixedTotal += pendFixed;
            clearedTotal += pendCleared;
            AppendFile(Path.Combine(DataDir, "diag.log"), DateTime.Now.ToString("MM-dd HH:mm:ss") + " баз=" + fresh + " записано=" + pendFilled + " исправлено=" + pendFixed + " очищено=" + pendCleared + " без_перевода=" + pendMiss + " (итого исправлено " + fixedTotal + ", очищено " + clearedTotal + ")\n");
        }
        pendNew = 0; pendFilled = 0; pendMiss = 0; pendFixed = 0; pendCleared = 0;
    }

    private static bool lbbbFillPatched;
    private static int lbbbPatchTries;

    private static void PatchLbbbFill()
    {
        if (lbbbFillPatched || lbbbPatchTries > 120) return;
        lbbbPatchTries++;
        try
        {
            Type t = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try { t = asm.GetType("LbbbRU.LocalizationRU", false); } catch { }
                if (t != null) break;
            }
            if (t == null) return;
            var m = t.GetMethod("FillDatabase", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (m == null) return;
            var h = new HarmonyLib.Harmony("com.coreforgelabs.lbbb.crew.fill");
            h.Patch(m, prefix: new HarmonyLib.HarmonyMethod(typeof(CrewBehaviour).GetMethod("SkipLbbbFill", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)));
            lbbbFillPatched = true;
            Log("заливка бесед LbbbRU заменена на порционную (без зависаний на смене языка)");
        }
        catch (Exception e) { if (lbbbPatchTries < 3) Log("Harmony fill: " + e.Message); }
    }

    private static bool SkipLbbbFill(ref int __result)
    {
        __result = 0;
        return false;
    }

    private static void StepField(Il2CppSystem.Collections.Generic.List<PixelCrushers.DialogueSystem.Field> fields, int cid, int eid, string kind, string title, string text, StringBuilder inv, StringBuilder ms, ref int filled, ref int miss, ref int fixedNow, ref int clearedNow)
    {
        string have = null;
        try { have = PixelCrushers.DialogueSystem.Field.LookupValue(fields, title); } catch { }
        bool probeMark = have != null && have.IndexOf("ПРОВЕРКА_ЯЗЫКА", StringComparison.Ordinal) >= 0;
        if (probeMark) have = null;
        string exp = TranslateDbText(text);
        string rowEn;
        IdEnDict.TryGetValue(cid + "_" + eid + "_" + kind, out rowEn);
        bool foreign = rowEn != null && rowEn.Length > 0 && NormTxt(rowEn) != NormTxt(text) && !Similar(rowEn, text);
        string status;
        if (!string.IsNullOrEmpty(have))
        {
            if (!string.IsNullOrEmpty(exp) && exp != have && foreign)
            {
                try { PixelCrushers.DialogueSystem.Field.SetValue(fields, title, exp); fixedNow++; status = "fix"; }
                catch (Exception ex) { status = "err"; Log("fix ru: " + ex.Message); }
            }
            else if (string.IsNullOrEmpty(exp))
            {
                if (foreign)
                {
                    try { PixelCrushers.DialogueSystem.Field.SetValue(fields, title, text); clearedNow++; status = "clear"; }
                    catch (Exception ex) { status = "err"; Log("clear ru: " + ex.Message); }
                }
                else status = "ok";
            }
            else status = "ok";
        }
        else if (!string.IsNullOrEmpty(exp) && exp != text)
        {
            try { PixelCrushers.DialogueSystem.Field.SetValue(fields, title, exp); filled++; status = "filled"; }
            catch (Exception ex) { status = "err"; Log("set ru: " + ex.Message); }
        }
        else
        {
            if (probeMark && !string.IsNullOrEmpty(text))
            {
                try { PixelCrushers.DialogueSystem.Field.SetValue(fields, title, text); } catch { }
            }
            miss++; status = "miss";
        }
        string key = cid + "_" + eid + "_" + kind;
        if (invSeen.Add(key)) inv.Append(cid).Append(';').Append(eid).Append(';').Append(kind).Append(';').Append(status).Append(';').Append(Csv(text)).Append((char)10);
        if (status == "miss" && missSeen.Add(key)) ms.Append(cid).Append(';').Append(eid).Append(';').Append(kind).Append(';').Append(Csv(text)).Append((char)10);
    }

    private static string TranslateDbText(string s)
    {
        if (string.IsNullOrEmpty(s)) return null;
        string v;
        if (TryTr(s, out v) && !string.IsNullOrEmpty(v) && v != s) return v;
        string q = TryQuotes(s);
        if (q != null) return q;
        string c = TryComposed(s);
        if (c != null) return c;
        string bare = StripNoParse(s);
        if (bare != s && TryTr(bare, out v) && !string.IsNullOrEmpty(v) && v != bare) return v;
        string pref = TranslateWithUiPrefix(s);
        if (pref != null) return pref;
        return TryTemplate(s);
    }

    // "[f][em2]< look at the photo >" → берём готовый перевод "[ look at the photo ]", сохраняя префиксы
    private static string TranslateWithUiPrefix(string s)
    {
        try
        {
            System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(s, @"^((?:\s*\[(?:f|d|s|w|em\d)\])+)([\s\S]*)$");
            if (!m.Success) return null;
            string prefix = m.Groups[1].Value;
            string rest = m.Groups[2].Value.Trim();
            if (rest.Length == 0) return null;
            List<string> cands = new List<string>();
            cands.Add(rest);
            cands.Add("[ " + rest + " ]");
            cands.Add("< " + rest + " >");
            cands.Add("\"" + rest + "\"");
            System.Text.RegularExpressions.Match w = System.Text.RegularExpressions.Regex.Match(rest, @"^[<\[]\s*([\s\S]*?)\s*[>\]]$");
            if (w.Success)
            {
                string inner = w.Groups[1].Value.Trim();
                cands.Add(inner);
                cands.Add("[ " + inner + " ]");
                cands.Add("< " + inner + " >");
                cands.Add("\"" + inner + "\"");
            }
            for (int i = 0; i < cands.Count; i++)
            {
                string v;
                if (TryTr(cands[i], out v) && !string.IsNullOrEmpty(v) && v != cands[i]) return prefix + v;
            }
        }
        catch { }
        return null;
    }

    private static void AppendFile(string p, string s)
    {
        try { File.AppendAllText(p, s, Encoding.UTF8); } catch { }
    }


    private static int benchState;
    private static float benchTimer;

    private static void RunBench()
    {
        if (benchState == 0)
        {
            string f = Path.Combine(DataDir, "lang_bench.txt");
            if (!File.Exists(f)) return;
            try { File.Delete(f); } catch { }
            benchState = 1;
            benchTimer = Time.unscaledTime + 0.3f;
            Log("бенч языка: старт, сейчас " + (PixelCrushers.DialogueSystem.Localization.language ?? "?"));
            return;
        }
        if (Time.unscaledTime < benchTimer) return;
        if (benchState == 1)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try { PixelCrushers.DialogueSystem.Localization.language = "English"; } catch (Exception e) { Log("бенч: " + e.Message); }
            Log("бенч языка: переключение на English = " + sw.ElapsedMilliseconds + " мс");
            benchState = 2;
            benchTimer = Time.unscaledTime + 1.5f;
            return;
        }
        if (benchState == 2)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try { PixelCrushers.DialogueSystem.Localization.language = "ru"; } catch (Exception e) { Log("бенч: " + e.Message); }
            Log("бенч языка: переключение на ru = " + sw.ElapsedMilliseconds + " мс");
            benchState = 3;
            benchTimer = Time.unscaledTime + 4f;
            return;
        }
        if (benchState == 3)
        {
            Log("бенч языка: в очереди задач " + dbJobs.Count + ", готово баз " + dbDone.Count + ", кусок " + jobChunk);
            benchState = 0;
        }
    }


    private static readonly Dictionary<string, System.Diagnostics.Stopwatch> profSw = new Dictionary<string, System.Diagnostics.Stopwatch>();
    private static readonly Dictionary<string, long> profMax = new Dictionary<string, long>();
    private static readonly Dictionary<string, int> profCnt = new Dictionary<string, int>();
    private static float profNextLog;
    private static bool profPatched;

    private static void ProfStart(System.Reflection.MethodBase __originalMethod)
    {
        try
        {
            string n = __originalMethod.DeclaringType.Name + "." + __originalMethod.Name;
            profSw[n] = System.Diagnostics.Stopwatch.StartNew();
        }
        catch { }
    }

    private static void ProfEnd(System.Reflection.MethodBase __originalMethod)
    {
        try
        {
            string n = __originalMethod.DeclaringType.Name + "." + __originalMethod.Name;
            System.Diagnostics.Stopwatch sw;
            if (!profSw.TryGetValue(n, out sw)) return;
            profSw.Remove(n);
            long ms = sw.ElapsedMilliseconds;
            long mx; profMax.TryGetValue(n, out mx);
            if (ms > mx) profMax[n] = ms;
            int c; profCnt.TryGetValue(n, out c);
            profCnt[n] = c + 1;
        }
        catch { }
    }

    private static void ProfFlush()
    {
        if (Time.unscaledTime < profNextLog) return;
        profNextLog = Time.unscaledTime + 20f;
        if (profCnt.Count == 0) return;
        var sb = new StringBuilder();
        bool slow = false;
        foreach (var kv in profCnt)
        {
            long mx; profMax.TryGetValue(kv.Key, out mx);
            if (mx >= 40) slow = true;
            sb.Append(kv.Key).Append(" n=").Append(kv.Value).Append(" max=").Append(mx).Append("ms; ");
        }
        if (slow) Log("[проф] " + sb.ToString());
        profCnt.Clear();
        profMax.Clear();
    }

    private static bool logPatched;

    private static bool LogFilter(string s)
    {
        if (string.IsNullOrEmpty(s)) return true;
        try
        {
            if (File.Exists(Path.Combine(DataDir, "log_all.txt"))) return true;
            if (s.StartsWith("  Font '") || s.StartsWith("  dynamic TMP '")) return false;
            if (s.StartsWith("UnityEngine.Font assets:")) return false;
            if (s.StartsWith("проба: ")) return false;
            if (s == "родной язык: движок понимает поле ru") return false;
            if (s.StartsWith("родной язык: полей ru записано 0,")) return false;
            if (s.StartsWith("шрифт на старте:")) return false;
            if (s.StartsWith("NO RUSSIAN FONT AVAILABLE")) return false;
            if (s.StartsWith("created from '")) return false;
            if (s.StartsWith("atlas preheat:")) return false;
            if (s.StartsWith("ru font set:")) return false;
            if (s.StartsWith("таблицы: загружено своих строк")) return false;
            if (s.StartsWith("таблица \"")) return false;
            if (s.StartsWith("fontSwaps=")) return false;
            if (s.StartsWith("[perf] fps="))
            {
                int i1 = s.IndexOf("fps=") + 4;
                int i2 = s.IndexOf(" avg=");
                double fps = 0;
                if (i2 > i1) double.TryParse(s.Substring(i1, i2 - i1).Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out fps);
                int w1 = s.IndexOf("worst=") + 6;
                int w2 = s.IndexOf("ms", w1);
                int worst = 0;
                if (w2 > w1) int.TryParse(s.Substring(w1, w2 - w1), out worst);
                return fps < 45 || worst > 100;
            }
            if (s.StartsWith("[perf] burst:"))
            {
                int m1 = s.IndexOf("max=") + 4;
                int m2 = s.IndexOf("ms", m1);
                int mx = 0;
                if (m2 > m1) int.TryParse(s.Substring(m1, m2 - m1), out mx);
                return mx > 100;
            }
        }
        catch { }
        return true;
    }

    public static void PatchLog()
    {
        if (logPatched) return;
        try
        {
            Type t = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try { t = asm.GetType("LbbbRU.Core", false); } catch { }
                if (t != null) break;
            }
            if (t == null) return;
            var m = t.GetMethod("Log", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (m == null) return;
            var pre = typeof(CrewBehaviour).GetMethod("LogFilter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            new HarmonyLib.Harmony("com.coreforgelabs.lbbb.crew.log").Patch(m, prefix: new HarmonyLib.HarmonyMethod(pre));
            logPatched = true;
        }
        catch (Exception e) { Log("фильтр лога: " + e.Message); }
    }

    private static void PatchProfiler()
    {
        if (profPatched) return;
        profPatched = true;
        try
        {
            Type t = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try { t = asm.GetType("LbbbRU.Core", false); } catch { }
                if (t != null) break;
            }
            if (t == null) { profPatched = false; return; }
            var h = new HarmonyLib.Harmony("com.coreforgelabs.lbbb.crew.prof");
            var pre = typeof(CrewBehaviour).GetMethod("ProfStart", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var post = typeof(CrewBehaviour).GetMethod("ProfEnd", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            int n = 0;
            foreach (var name in new string[] { "FontProbe", "EnsureFont", "PreheatAtlas", "NativeLanguagePass", "AttachFallback", "SwapFonts", "ApplyTextMap" })
            {
                var m = t.GetMethod(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (m == null) continue;
                h.Patch(m, prefix: new HarmonyLib.HarmonyMethod(pre), postfix: new HarmonyLib.HarmonyMethod(post));
                n++;
            }
            Log("профайлер: повешен на " + n + " методов LbbbRU");
        }
        catch (Exception e) { Log("профайлер: " + e.Message); }
    }


    private static void ProbeTaskTable()
    {
        string f = Path.Combine(DataDir, "probe_table.txt");
        if (!File.Exists(f)) return;
        try { File.Delete(f); } catch { }
        try
        {
            var all = Resources.FindObjectsOfTypeAll<PixelCrushers.TextTable>();
            Log("проба таблиц: всего " + all.Length);
            foreach (var t in all)
            {
                if (t == null) continue;
                string n = null; try { n = t.name; } catch { }
                var fn = t.GetFieldNames();
                var langs = t.GetLanguageNames();
                Log("  таблица '" + n + "' полей=" + (fn == null ? "null" : fn.Count.ToString()) + " языков=" + (langs == null ? "null" : string.Join(",", langs)));
                if (n != "Tasks and Clues" || fn == null || fn.Count == 0) continue;
                string k = fn[0];
                string en = null; try { en = t.GetFieldTextForLanguage(k, "English"); } catch (Exception e) { Log("  en err " + e.GetType().Name); }
                Log("  ключ0='" + k + "' en='" + en + "'");
                try { t.SetFieldTextForLanguage(k, "ru", "ТЕСТ_ТАБЛИЦЫ"); Log("  set ok"); } catch (Exception e) { Log("  set err " + e.GetType().Name + ": " + e.Message); }
                string back = null; try { back = t.GetFieldTextForLanguage(k, "ru"); } catch { }
                Log("  обратно='" + back + "'");
                try
                {
                    StringBuilder fb = new StringBuilder();
                    for (int i = 0; i < fn.Count; i++)
                    {
                        string en2 = null; try { en2 = t.GetFieldTextForLanguage(fn[i], "English"); } catch { }
                        fb.Append(fn[i]).Append(';').Append(en2 ?? "").Append((char)10);
                    }
                    File.WriteAllText(Path.Combine(DataDir, "probe_fields.txt"), fb.ToString(), Encoding.UTF8);
                    Log("  поля выгружены: " + fn.Count);
                }
                catch (Exception e) { Log("  dump fields err " + e.Message); }
            }
        }
        catch (Exception e) { Log("проба таблиц err: " + e.Message); }
    }

    private void Update()
    {
        try
        {
            RunBench();
            PatchLog();
            ProbeTaskTable();
            PatchProfiler();
            ProfFlush();
            bool rus = RusActive;
            if (rus != lastRus)
            {
                lastRus = rus;
                itemLabel = null;
                dbDone.Clear();
                dbQueued.Clear();
                dbJobs.Clear();
                pendNew = pendFilled = pendMiss = pendFixed = pendCleared = 0;
                nextDbScan = 0f;
                nextFullScan = 0f;
                invSeen.Clear();
                missSeen.Clear();
                fixedTotal = 0;
                clearedTotal = 0;
                Log("язык сменился на " + (GameLang() ?? "null") + (rus ? " — перевод включён" : " — перевод выключен, вернул исходные строки"));
                if (!rus) RevertTexts();
            }
            CheckNativeMouse();
            TryPatchPlate();
            TryPatchVisible();
            TryLocalizeAssets();
            FillTaskTables();
            TryDumpFields();            CollectMissing();
            if (rus) PatchVisibleText();
            if (rus) LocalizeDatabases();
            FixStatLabels();

            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + 1f;
            KillOldPlate();
            EnsureMenuItem();
            EnsureSettingsLanguage();
            string trigOpen = Path.Combine(DataDir, "crew_open.txt");
            if (File.Exists(trigOpen)) { File.Delete(trigOpen); if (overlay == null) BuildPanel(); }
            string trigClick = Path.Combine(DataDir, "crew_click.txt");
            if (File.Exists(trigClick)) { File.Delete(trigClick); if (entry != null) { Button eb = entry.GetComponent<Button>(); if (eb != null) { Log("нажимаю пункт программно"); eb.onClick.Invoke(); } else Log("кнопка пункта не найдена"); } }
            string trigClose = Path.Combine(DataDir, "crew_close.txt");
            if (File.Exists(trigClose)) { File.Delete(trigClose); ClosePanel(); }
            if (overlay != null && UnityEngine.Input.GetKeyDown(KeyCode.Escape)) ClosePanel();
        }
        catch (Exception e) { Log("ERR " + e.Message); }
    }

    private static readonly System.Collections.Generic.Dictionary<string, string> missing = new System.Collections.Generic.Dictionary<string, string>();
    private static float nextMissingWrite;
    private static bool missingDirty;

    private static void CollectMissing()
    {
        try
        {
            TMP_Text[] all = Resources.FindObjectsOfTypeAll<TMP_Text>();
            if (all == null) return;
            int added = 0;
            for (int i = 0; i < all.Length; i++)
            {
                TMP_Text t = all[i];
                if (t == null) continue;
                GameObject go = t.gameObject;
                if (go == null || !go.activeInHierarchy) continue;
                string s = t.text;
                if (string.IsNullOrEmpty(s)) continue;
                s = StripNoParse(s.Trim());
                if (s.Length < 3 || s.Length > 400) continue;
                if (LatinRuns(s) == 0) continue;
                string _mv; if (TryTr(s, out _mv)) continue;
                if (missing.ContainsKey(s)) continue;
                if (missing.Count >= 4000) continue;
                missing[s] = PathOf(go.transform);
                added++;
            }
            if (added > 0) missingDirty = true;
            if (missingDirty && Time.unscaledTime >= nextMissingWrite)
            {
                nextMissingWrite = Time.unscaledTime + 10f;
                missingDirty = false;
                StringBuilder sb = new StringBuilder();
                foreach (System.Collections.Generic.KeyValuePair<string, string> kv in missing)
                    sb.Append(kv.Key.Replace("\n", "\\n")).Append('\t').Append(kv.Value).Append('\n');
                File.WriteAllText(Path.Combine(DataDir, "missing_text.txt"), sb.ToString(), Encoding.UTF8);
            }
        }
        catch (Exception e) { Log("missing: " + e.Message); }
    }

    private static int patches;

    private static bool HasCyrillic(string s)
    {
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c >= '\u0400' && c <= '\u04FF') return true;
        }
        return false;
    }

    private static float nextVisDbg;
    private static void PatchVisibleText()
    {
        if (!RusActive) return;
        try
        {
            TMP_Text[] all = Resources.FindObjectsOfTypeAll<TMP_Text>();
            if (all == null) return;
            int done = 0;
            for (int i = 0; i < all.Length && done < 200; i++)
            {
                TMP_Text t = all[i];
                if (t == null) continue;
                GameObject go = t.gameObject;
                if (go == null || !go.activeInHierarchy) continue;
                string s = t.text;
                if (string.IsNullOrEmpty(s) || s.Length > 800) continue;
                string bare = StripNoParse(s).Trim();
                string v;
                if (bare.IndexOf('\n') >= 0)
                {
                    string mv = TranslateLines(bare);
                    if (mv == null || mv == s) continue;
                    v = mv;
                }
                else
                {
                    if (HasCyrillic(bare)) continue;
                    if (LatinRuns(bare) == 0) continue;
                    if (!TryTr(bare, out v))
                    {
                        if (!TableDict.TryGetValue(bare, out v) || string.IsNullOrEmpty(v))
                        {
                            string c2 = TryComposed(bare) ?? TryQuotes(bare);
                            if (c2 == null) continue;
                            v = c2;
                        }
                    }
                }
                if (string.IsNullOrEmpty(v) || v == s) continue;
                int tid = t.GetInstanceID();
                if (!patchedOriginals.ContainsKey(tid)) { patchedOriginals[tid] = s; patchedRefs[tid] = t; }
                t.text = v;
                done++;
                patches++;
                if (patches <= 40) Log("правка текста [" + PathOf(go.transform) + "]: " + s.Substring(0, Math.Min(60, s.Length)));
            }
        }
        catch (Exception e) { Log("visible: " + e.Message); }
    }

    private static string TranslateLines(string text)
    {
        string[] parts = text.Split('\n');
        bool any = false;
        for (int i = 0; i < parts.Length; i++)
        {
            string line = parts[i].Trim();
            if (line.Length == 0) { parts[i] = ""; continue; }
            if (HasCyrillic(line) || LatinRuns(line) == 0) { parts[i] = line; continue; }
            string v;
            if (TryTr(line, out v) && !string.IsNullOrEmpty(v)) { parts[i] = v; any = true; continue; }
            if (TableDict.TryGetValue(line, out v) && !string.IsNullOrEmpty(v)) { parts[i] = v; any = true; continue; }
            string c2 = TryComposed(line) ?? TryQuotes(line);
            if (c2 != null) { parts[i] = c2; any = true; continue; }
            parts[i] = line;
        }
        return any ? string.Join("\n", parts) : null;
    }

    private static string StripNoParse(string s)
    {
        if (s.IndexOf("<noparse>", StringComparison.Ordinal) < 0 && s.IndexOf("</noparse>", StringComparison.Ordinal) < 0) return s;
        return s.Replace("<noparse>", "").Replace("</noparse>", "").Trim();
    }

    private static int LatinRuns(string s)    {
        int n = 0, run = 0;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            bool lat = (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
            if (lat) { run++; if (run == 3) { n++; run = 0; } }
            else run = 0;
        }
        return n;
    }

    private static string PathOf(Transform t)
    {
        StringBuilder sb = new StringBuilder();
        int depth = 0;
        while (t != null && depth < 6)
        {
            if (sb.Length > 0) sb.Insert(0, "/");
            sb.Insert(0, t.name);
            t = t.parent;
            depth++;
        }
        return sb.ToString();
    }

    private static float nextLabelFix;
    private static int labelFixes;

    private static void FixStatLabels()
    {
        if (Time.unscaledTime < nextLabelFix) return;
        nextLabelFix = Time.unscaledTime + 3f;
        try
        {
            TMP_Text[] all = Resources.FindObjectsOfTypeAll<TMP_Text>();
            if (all == null) return;
            for (int i = 0; i < all.Length; i++)
            {
                TMP_Text t = all[i];
                if (t == null) continue;
                GameObject go = t.gameObject;
                if (go == null) continue;
                string nm = go.name;
                if (nm != "SCIRadar" && nm != "EMP" && nm != "REC") continue;
                bool wrap = t.enableWordWrapping;
                float fs = t.fontSize;
                if (wrap) t.enableWordWrapping = false;
                if (fs > 26f) t.fontSize = 26f;
                t.overflowMode = TextOverflowModes.Overflow;
                if (wrap || labelFixes < 3)
                {
                    labelFixes++;
                    Log("метка радара " + nm + ": обрезка слов " + wrap + ", размер " + fs);
                }
            }
        }
        catch (Exception e) { Log("labels: " + e.Message); }
    }
    private void LateUpdate()
    {
        try
        {
            if (entry == null) return;
            if (nativeMode) PlaceNativeItem(); else PlaceMenuItem();
            if (Time.unscaledTime < nextLabelWrite) return;
            nextLabelWrite = Time.unscaledTime + 0.25f;
            foreach (TMP_Text t in entry.GetComponentsInChildren<TMP_Text>(true))
            {
                if (t == null) continue;
                if ((t.text ?? "").Trim() != ItemLabel)
                {
                    if (!labelLogged) { Log("игра перезаписывает текст пункта, чиню каждый кадр"); labelLogged = true; }
                    t.text = ItemLabel;
                }
            }
        }
        catch { }
    }

    private void KillOldPlate()
    {
        GameObject old = GameObject.Find("LBBB_RU_Credits");
        if (old != null) { UnityEngine.Object.Destroy(old); Log("старая плашка удалена"); }
    }

    private TMP_Text FindMenuTemplate()
    {
        TMP_Text[] texts = UnityEngine.Object.FindObjectsOfType<TMP_Text>(true);
        TMP_Text best = null;
        int bestCount = 0;
        foreach (TMP_Text t in texts)
        {
            if (t == null) continue;
            if (!t.gameObject.activeInHierarchy) continue;
            string v = (t.text ?? "").Trim();
            if (v != "Выйти из игры" && v != "Exit Game" && v != "Quit Game") continue;
            Transform btn = t.transform.parent;
            if (btn == null || btn.parent == null) continue;
            Transform panel = btn.parent;
            int count = 0;
            for (int i = 0; i < panel.childCount; i++)
            {
                Transform c = panel.GetChild(i);
                if (c.gameObject.activeInHierarchy && c.GetComponent<Button>() != null) count++;
            }
            if (count > bestCount) { bestCount = count; best = t; }
        }
        return best;
    }

    private bool BuildNativeItem()
    {
        TMP_Text tpl = FindMenuTemplate();
        if (tpl == null) return false;
        font = tpl.font;
        template = tpl;
        Transform btn = tpl.transform.parent;
        if (btn == null || btn.parent == null) return false;
        Transform panel = btn.parent;
        Transform existing = panel.Find("LBBB_CrewItem");
        if (existing != null)
        {
            entry = existing.gameObject;
            labelTmp = existing.GetComponentInChildren<TMP_Text>(true);
            nativeMode = true;
            return true;
        }
        RectTransform b = btn.GetComponent<RectTransform>();
        if (b == null) return false;

        GameObject go = Rect(layerRoot != null ? panel : panel, "LBBB_CrewItem");
        RectTransform g = go.GetComponent<RectTransform>();
        g.anchorMin = b.anchorMin;
        g.anchorMax = b.anchorMax;
        g.pivot = b.pivot;
        g.sizeDelta = b.sizeDelta;
        g.localScale = b.localScale;
        g.localRotation = b.localRotation;

        GameObject label = Rect(go.transform, "Label");
        TextMeshProUGUI tmp = label.AddComponent<TextMeshProUGUI>();
        tmp.font = tpl.font;
        if (tpl.fontSharedMaterial != null) tmp.fontSharedMaterial = tpl.fontSharedMaterial;
        tmp.text = ItemLabel;
        tmp.fontSize = tpl.fontSize;
        tmp.color = tpl.color;
        tmp.alignment = tpl.alignment;
        tmp.characterSpacing = tpl.characterSpacing;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = tpl.enableWordWrapping;
        tmp.enableAutoSizing = tpl.enableAutoSizing;
        tmp.fontSizeMin = tpl.fontSizeMin;
        tmp.fontSizeMax = tpl.fontSizeMax;
        tmp.margin = tpl.margin;
        tmp.overflowMode = tpl.overflowMode;
        tmp.lineSpacing = tpl.lineSpacing;
        tmp.paragraphSpacing = tpl.paragraphSpacing;
        RectTransform lr = label.GetComponent<RectTransform>();
        RectTransform nr = tpl.GetComponent<RectTransform>();
        if (nr != null)
        {
            lr.anchorMin = nr.anchorMin;
            lr.anchorMax = nr.anchorMax;
            lr.pivot = nr.pivot;
            lr.sizeDelta = nr.sizeDelta;
            lr.localScale = nr.localScale;
            if (nr.parent != null && nr.parent.GetComponent<RectTransform>() == b) lr.anchoredPosition = nr.anchoredPosition;
            else { lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one; lr.offsetMin = Vector2.zero; lr.offsetMax = Vector2.zero; }
        }
        else
        {
            lr.anchorMin = Vector2.zero;
            lr.anchorMax = Vector2.one;
            lr.offsetMin = Vector2.zero;
            lr.offsetMax = Vector2.zero;
        }

        Image hit = go.AddComponent<Image>();
        hit.color = new Color(1f, 1f, 1f, 0f);
        hit.raycastTarget = true;

        Image ray = go.GetComponent<Image>();
        if (ray != null) ray.raycastTarget = true;
        Button nb = go.AddComponent<Button>();
        nb.targetGraphic = hit;
        nb.transition = Selectable.Transition.None;
        System.Action h = OnCrewClick;
        nb.onClick.AddListener(h);

        go.transform.SetParent(panel, true);
        go.transform.SetSiblingIndex(btn.GetSiblingIndex());
        g.anchoredPosition = b.anchoredPosition;
        go.SetActive(true);
        entry = go;
        labelTmp = tmp;
        nativeMode = true;
        Log("пункт добавлен в нативное меню: размер=" + g.sizeDelta + " scale=" + g.localScale + " роднойScale=" + b.localScale + " шрифт=" + tmp.fontSize + " ширинаРодная=" + b.rect.size);
        return true;
    }

    private void EnsureMenuItem()
    {
        if (entry != null) return;
        if (File.Exists(Path.Combine(DataDir, "crew_off.txt")))
        {
            if (layer != null) UnityEngine.Object.Destroy(layer);
            return;
        }
        if (BuildNativeItem()) return;
        TMP_Text tpl = FindMenuTemplate();
        if (tpl == null) return;
        font = tpl.font;
        template = tpl;

        if (layer == null)
        {
            layer = new GameObject("LBBB_CrewMenuLayer", UiTypes());
            Canvas lc = layer.AddComponent<Canvas>();
            lc.renderMode = RenderMode.ScreenSpaceOverlay;
            lc.sortingOrder = 32760;
            layer.AddComponent<GraphicRaycaster>();
            layerRoot = layer.GetComponent<RectTransform>();
            layer.SetActive(true);
        }

        GameObject go = Rect(layerRoot, "LBBB_CrewEntry");
        Image hit = go.AddComponent<Image>();
        hit.color = new Color(1f, 1f, 1f, 0f);
        hit.raycastTarget = true;

        GameObject label = Rect(go.transform, "Label");
        TextMeshProUGUI tmp = label.AddComponent<TextMeshProUGUI>();
        tmp.font = tpl.font;
        if (tpl.fontSharedMaterial != null) tmp.fontSharedMaterial = tpl.fontSharedMaterial;
        tmp.text = ItemLabel;
        tmp.color = File.Exists(Path.Combine(DataDir, "crew_red.txt")) ? new Color(1f, 0f, 0f, 1f) : tpl.color;
        tmp.alignment = tpl.alignment;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = false;
        RectTransform lr = label.GetComponent<RectTransform>();
        lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
        lr.offsetMin = Vector2.zero; lr.offsetMax = Vector2.zero;

        Button b = go.AddComponent<Button>();
        b.targetGraphic = tmp;
        b.transition = Selectable.Transition.ColorTint;
        ColorBlock cb = b.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.4f, 1.4f, 1.4f, 1f);
        cb.pressedColor = new Color(1.7f, 1.7f, 1.7f, 1f);
        cb.selectedColor = Color.white;
        cb.disabledColor = Color.white;
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0.1f;
        b.colors = cb;
        System.Action h = OnCrewClick;
        b.onClick.AddListener(h);

        entry = go;
        labelTmp = tmp;
        Log("слой меню создан, шрифт=" + (tpl.font != null ? tpl.font.name : "нет"));
    }

    private void PlaceNativeItem()
    {
        if (entry == null || !nativeMode) return;
        if (template == null || !template.gameObject.activeInHierarchy) template = FindMenuTemplate();
        if (template == null)
        {
            if (entry.activeSelf) entry.SetActive(false);
            return;
        }
        if (!entry.activeSelf) entry.SetActive(true);
        Transform btn = template.transform.parent;
        if (btn == null || btn.parent == null) return;
        Transform panel = btn.parent;
        if (entry.transform.parent != panel)
        {
            entry.transform.SetParent(panel, false);
        }
        if (labelTmp != null)
        {
            Vector3 off = template.transform.position - btn.position;
            labelTmp.transform.position = entry.transform.position + off;
            labelTmp.transform.rotation = template.transform.rotation;
            labelTmp.transform.localScale = template.transform.localScale;
        }
        var items = new Il2CppSystem.Collections.Generic.List<Transform>();
        for (int i = 0; i < panel.childCount; i++)
        {
            Transform c = panel.GetChild(i);
            if (c == null || c == entry.transform) continue;
            if (!c.gameObject.activeInHierarchy) continue;
            if (c.GetComponentInChildren<TMP_Text>(true) == null) continue;
            items.Add(c);
        }
        if (panel.GetComponent<UnityEngine.UI.LayoutGroup>() != null)
        {
            return;
        }
        if (items.Count < 2) return;
        Transform last = items[items.Count - 1];
        Vector3 step = items[items.Count - 1].position - items[items.Count - 2].position;
        if (items.Count >= 3) step = items[items.Count - 2].position - items[items.Count - 3].position;
        if (step.magnitude < 0.0001f) return;
        entry.transform.position = last.position + step;
        entry.transform.rotation = last.rotation;
        entry.transform.localScale = last.localScale;
    }

    private void PlaceMenuItem()
    {
        if (entry == null || layerRoot == null) return;
        if (template == null || !template.gameObject.activeInHierarchy) template = FindMenuTemplate();
        if (template == null)
        {
            if (entry.activeSelf) entry.SetActive(false);
            return;
        }
        if (!entry.activeSelf) entry.SetActive(true);

        Canvas srcCanvas = template.GetComponentInParent<Canvas>();
        Camera srcCam = null;
        if (srcCanvas != null)
        {
            srcCam = srcCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : srcCanvas.worldCamera;
            if (srcCam == null) srcCam = Camera.main;
        }
        RectTransform s = template.GetComponent<RectTransform>();
        if (s == null) return;

        Vector3 spMin = RectTransformUtility.WorldToScreenPoint(srcCam, template.bounds.min);
        Vector3 spMax = RectTransformUtility.WorldToScreenPoint(srcCam, template.bounds.max);
        float textH = Mathf.Abs(spMax.y - spMin.y);
        float textLeft = Mathf.Min(spMin.x, spMax.x);
        float textRight = Mathf.Max(spMin.x, spMax.x);
        float textW = textRight - textLeft;
        if (textH < 4f || textW < 4f) return;

        float worldH = template.bounds.size.y;
        float pxPerUnit = worldH > 0.0001f ? textH / worldH : 1f;

        float spacing = textH * 1.7f;
        Transform src = template.transform.parent;
        Transform panel = src.parent;
        if (panel != null)
        {
            for (int i = src.GetSiblingIndex() - 1; i >= 0; i--)
            {
                TMP_Text prev = panel.GetChild(i).GetComponentInChildren<TMP_Text>(true);
                if (prev == null || !prev.gameObject.activeInHierarchy) continue;
                Vector3 pc = RectTransformUtility.WorldToScreenPoint(srcCam, prev.bounds.center);
                Vector3 cc = RectTransformUtility.WorldToScreenPoint(srcCam, template.bounds.center);
                spacing = Mathf.Abs(cc.y - pc.y);
                break;
            }
        }

        Canvas lcan = layer.GetComponent<Canvas>();
        float lscale = lcan != null && lcan.scaleFactor > 0f ? lcan.scaleFactor : 1f;
        float fontSize = template.fontSize * Mathf.Abs(s.lossyScale.y) * pxPerUnit;

        RectTransform gr = entry.GetComponent<RectTransform>();
        gr.anchorMin = new Vector2(0.5f, 0.5f);
        gr.anchorMax = new Vector2(0.5f, 0.5f);
        gr.pivot = new Vector2(0f, 0.5f);
        gr.sizeDelta = new Vector2((textW + textH * 2f) / lscale, textH / lscale);
        Vector3 center = RectTransformUtility.WorldToScreenPoint(srcCam, template.bounds.center);
        gr.anchoredPosition = new Vector2((textLeft - Screen.width * 0.5f) / lscale, (center.y - spacing - Screen.height * 0.5f) / lscale);

        if (labelTmp != null)
        {
            labelTmp.fontSize = fontSize / lscale;
            if (!File.Exists(Path.Combine(DataDir, "crew_red.txt"))) labelTmp.color = template.color;
            labelTmp.alignment = TextAlignmentOptions.Left;
            labelTmp.characterSpacing = template.characterSpacing;
        }
    }

    private void OnCrewClick()
    {
        if (Time.unscaledTime - lastClick < 0.4f) return;
        lastClick = Time.unscaledTime;
        Log("клик принят");
        if (overlay == null) BuildPanel(); else ClosePanel();
    }

    private List<string[]> ReadSupporters()
    {
        List<string[]> rows = new List<string[]>();
        try
        {
            string path = Path.Combine(DataDir, "boosty.csv");
            if (!File.Exists(path)) return rows;
            foreach (string raw in File.ReadAllLines(path, Encoding.UTF8))
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                string[] parts = raw.Split(';');
                if (parts.Length < 2) continue;
                string tier = parts[0].Trim();
                string name = parts[1].Trim();
                if (tier.Length == 0 || name.Length == 0) continue;
                if (tier.Equals("tier", StringComparison.OrdinalIgnoreCase)) continue;
                rows.Add(new string[] { tier, name });
            }
        }
        catch (Exception e) { Log("boosty.csv: " + e.Message); }
        return rows;
    }

    private void BuildPanel()
    {
        List<string[]> rows = ReadSupporters();
        GameObject canvasGo = new GameObject("LBBB_CrewPanel", UiTypes());
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32760;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        GameObject dim = Rect(canvasGo.transform, "Dim");
        Image dimImg = dim.AddComponent<Image>();
        dimImg.color = BackdropColor;
        Stretch(dim);
        Scanlines(canvasGo.transform);
        Button dimBtn = dim.AddComponent<Button>();
        dimBtn.transition = Selectable.Transition.None;
        System.Action close = ClosePanel;
        dimBtn.onClick.AddListener(close);

        GameObject root = Rect(canvasGo.transform, "Root");
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0.5f, 0.5f);
        rootRt.anchorMax = new Vector2(0.5f, 0.5f);
        rootRt.pivot = new Vector2(0.5f, 0.5f);
        rootRt.sizeDelta = new Vector2(1520f, 900f);
        rootRt.anchoredPosition = Vector2.zero;

        Transform rt2 = root.transform;
        Text(rt2, "Title", 0f, 380f, 1480f, 60f, ItemLabel, 46f, TierColor, TextAlignmentOptions.Left);
        Text(rt2, "Sub", 0f, 336f, 1480f, 26f, rows.Count + " человек — те, благодаря кому мод вообще вышел", 20f, MutedColor, TextAlignmentOptions.Left);
        HLine(rt2, 306f, 1480f, 1.5f);
        HLine(rt2, -352f, 1480f, 1.5f);

        List<string[]> left = new List<string[]>();
        List<string[]> right = new List<string[]>();
        foreach (string[] r in rows)
        {
            if (r[0] == "ЮНГИ") right.Add(r); else left.Add(r);
        }
        Column(rt2, -380f, left, TextAlignmentOptions.Left);
        Column(rt2, 380f, right, TextAlignmentOptions.Left);

        TextButton(rt2, -380f, -392f, 740f, "ПОДДЕРЖАТЬ НА BOOSTY", TextAlignmentOptions.Left, OpenBoosty);
        TextButton(rt2, 380f, -392f, 740f, "НАЗАД", TextAlignmentOptions.Right, ClosePanel);
        Text(rt2, "Footer", -380f, -430f, 740f, 24f, "Перевод и редактура: CoreForgeLabs · boosty.to/coreforgelabs", 16f, MutedColor, TextAlignmentOptions.Left);
        VLine(rt2, 0f, -30f, 596f, 1.5f);

        overlay = canvasGo;
        int visible = 0;
        foreach (TMP_Text t2 in canvasGo.GetComponentsInChildren<TMP_Text>(true)) if (t2 != null && t2.gameObject.activeInHierarchy) visible++;
        RectTransform crt = canvasGo.GetComponent<RectTransform>();
        RectTransform drt = dim.GetComponent<RectTransform>();
        Log("панель построена: сторонников=" + rows.Count + ", слева=" + left.Count + ", справа=" + right.Count + ", текстов=" + visible + ", canvas=" + (crt != null ? crt.rect.size.ToString() : "нет") + ", scale=" + scaler.scaleFactor + ", screen=" + Screen.width + "x" + Screen.height + ", dim=" + (drt != null ? drt.rect.size.ToString() : "нет"));
    }

    private void Column(Transform parent, float x, List<string[]> items, TextAlignmentOptions align)
    {
        float y = 266f;
        string cur = "";
        foreach (string[] r in items)
        {
            if (r[0] != cur)
            {
                if (cur.Length > 0) y -= 34f;
                TMP_Text th = Text(parent, "Tier", x, y, 720f, 32f, r[0].ToUpperInvariant(), 19f, TierColor, align);
                th.characterSpacing = 10f;
                th.fontStyle = FontStyles.Bold;
                y -= 44f;
                cur = r[0];
            }
            Text(parent, "Name", x, y, 720f, 32f, r[1], 22f, NameColor, align);
            y -= 44f;
        }
    }

    private void Scanlines(Transform parent)
    {
        try
        {
            Texture2D tex = new Texture2D(1, 2);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Point;
            tex.SetPixel(0, 0, new Color(1f, 1f, 1f, 0.05f));
            tex.SetPixel(0, 1, new Color(1f, 1f, 1f, 0f));
            tex.Apply();
            GameObject scan = Rect(parent, "Scan");
            RawImage ri = scan.AddComponent<RawImage>();
            ri.texture = tex;
            ri.raycastTarget = false;
            ri.uvRect = new Rect(0f, 0f, 1f, Mathf.Max(1f, Screen.height / 4f));
            Stretch(scan);
        }
        catch (Exception e) { Log("scanlines: " + e.Message); }
    }

    private static Il2CppReferenceArray<Il2CppSystem.Type> UiTypes()
    {
        Il2CppReferenceArray<Il2CppSystem.Type> arr = new Il2CppReferenceArray<Il2CppSystem.Type>(1);
        arr[0] = Il2CppType.Of<RectTransform>();
        return arr;
    }

    private GameObject Rect(Transform parent, string name)
    {
        GameObject go = new GameObject(name, UiTypes());
        go.transform.SetParent(parent, false);
        return go;
    }

    private void Stretch(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    private TMP_Text Text(Transform parent, string name, float x, float y, float w, float h, string value, float size, Color color, TextAlignmentOptions align)
    {
        GameObject go = Rect(parent, name);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = value;
        if (font != null) { tmp.font = font; tmp.fontSharedMaterial = font.material; }
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = false;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, y);
        return tmp;
    }

    private void VLine(Transform parent, float x, float yCenter, float height, float thickness)
    {
        GameObject go = Rect(parent, "VDivider");
        Image img = go.AddComponent<Image>();
        img.color = new Color(0.843f, 1f, 0.976f, 0.22f);
        img.raycastTarget = false;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(thickness, height);
        rt.anchoredPosition = new Vector2(x, yCenter);
    }

    private void HLine(Transform parent, float y, float width, float thickness)
    {
        GameObject go = Rect(parent, "Line");
        Image img = go.AddComponent<Image>();
        img.color = LineColor;
        img.raycastTarget = false;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, thickness);
        rt.anchoredPosition = new Vector2(0f, y);
    }

    private void TextButton(Transform parent, float x, float y, float w, string label, TextAlignmentOptions align, System.Action onClick)
    {
        GameObject go = Rect(parent, "Btn_" + label);
        Image hit = go.AddComponent<Image>();
        hit.color = new Color(1f, 1f, 1f, 0f);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, 44f);
        rt.anchoredPosition = new Vector2(x, y);
        TMP_Text tmp = Text(go.transform, "Label", 0f, 0f, w, 30f, label, 21f, NameColor, align);
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = hit;
        btn.transition = Selectable.Transition.None;
        if (onClick != null) btn.onClick.AddListener(onClick);
        Color baseColor = tmp.color;
        btn.targetGraphic = tmp;
        btn.transition = Selectable.Transition.ColorTint;
        ColorBlock cb = btn.colors;
        cb.normalColor = baseColor;
        cb.highlightedColor = Color.white;
        cb.pressedColor = new Color(1.6f, 1.6f, 1.6f, 1f);
        cb.selectedColor = baseColor;
        cb.disabledColor = baseColor;
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0f;
        btn.colors = cb;
    }

    private void OpenBoosty()
    {
        try { Application.OpenURL("https://boosty.to/coreforgelabs"); } catch (Exception e) { Log("OpenURL: " + e.Message); }
    }

    private void ClosePanel()
    {
        if (overlay != null) { UnityEngine.Object.Destroy(overlay); overlay = null; Log("панель закрыта"); }
    }
}
