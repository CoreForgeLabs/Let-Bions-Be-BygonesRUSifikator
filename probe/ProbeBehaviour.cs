using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LbbbProbe;

public class ProbeBehaviour : MonoBehaviour
{
    private float next = 0f;

    private void Update()
    {
        try
        {
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + 1f;
            string dir = Path.Combine(Paths.PluginPath, "LBBB_RU");
            string trigger = Path.Combine(dir, "probe.txt");
            if (!File.Exists(trigger)) return;
            File.Delete(trigger);
            Dump(dir);
        }
        catch (Exception e)
        {
            File.AppendAllText(Path.Combine(Paths.PluginPath, "LBBB_RU", "probe_err.txt"), e.ToString() + "\n");
        }
    }

    private void Dump(string dir)
    {
        List<string> sb = new List<string>();
        Scene sc = SceneManager.GetActiveScene();
        sb.Add("scene: " + sc.name + " camera=" + (Camera.main != null ? Camera.main.name : "none") + " screen=" + Screen.width + "x" + Screen.height);
        Canvas[] canvases = UnityEngine.Object.FindObjectsOfType<Canvas>(true);
        sb.Add("canvases: " + canvases.Length);
        foreach (Canvas c in canvases)
        {
            if (c == null) continue;
            sb.Add("=== canvas '" + c.name + "' mode=" + c.renderMode + " order=" + c.sortingOrder + " active=" + c.gameObject.activeInHierarchy);
            CanvasScaler scaler = c.GetComponent<CanvasScaler>();
            if (scaler != null) sb.Add("    scaler ref=" + scaler.referenceResolution.x + "x" + scaler.referenceResolution.y + " match=" + scaler.matchWidthOrHeight);
            Camera cam = c.renderMode == RenderMode.ScreenSpaceCamera ? c.worldCamera : (c.renderMode == RenderMode.WorldSpace ? Camera.main : null);
            Walk(c.transform, 1, sb, cam);
        }
        string path = Path.Combine(dir, "hierarchy_" + DateTime.Now.ToString("HHmmss") + ".txt");
        File.WriteAllText(path, string.Join("\n", sb));
        File.AppendAllText(Path.Combine(Paths.PluginPath, "LBBB_RU", "probe_done.txt"), path + "\n");
    }

    private void Walk(Transform t, int depth, List<string> sb, Camera cam)
    {
        if (t == null || depth > 9) return;
        string pad = new string(' ', depth * 2);
        RectTransform rt = t.TryCast<RectTransform>();
        string rect = "";
        if (rt != null)
        {
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Vector2 bl = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
            Vector2 tr = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
            rect = " world=" + Mathf.RoundToInt(rt.position.x) + "," + Mathf.RoundToInt(rt.position.y) + " pos=" + Mathf.RoundToInt(rt.anchoredPosition.x) + "," + Mathf.RoundToInt(rt.anchoredPosition.y) + " size=" + Mathf.RoundToInt(rt.sizeDelta.x) + "x" + Mathf.RoundToInt(rt.sizeDelta.y)
                + " screen=" + Mathf.RoundToInt(bl.x) + "," + Mathf.RoundToInt(bl.y) + ".." + Mathf.RoundToInt(tr.x) + "," + Mathf.RoundToInt(tr.y);
        }
        string text = "";
        TMP_Text tmp = t.gameObject.GetComponent<TMP_Text>();
        if (tmp != null)
        {
            text = " TEXT=\"" + (tmp.text ?? "").Replace("\n", " / ") + "\" size=" + Mathf.RoundToInt(tmp.fontSize) + " align=" + tmp.alignment
                + " rgba=" + Mathf.RoundToInt(tmp.color.r * 255) + "," + Mathf.RoundToInt(tmp.color.g * 255) + "," + Mathf.RoundToInt(tmp.color.b * 255) + "," + Mathf.RoundToInt(tmp.color.a * 255)
                + " font=" + (tmp.font != null ? tmp.font.name : "null");
        }
        string comps = "";
        Button btn = t.gameObject.GetComponent<Button>();
        if (btn != null) comps += " [Button]";
        Image img = t.gameObject.GetComponent<Image>();
        if (img != null) comps += " [Image:" + (img.sprite != null ? img.sprite.name : "null") + " rgba=" + Mathf.RoundToInt(img.color.r * 255) + "," + Mathf.RoundToInt(img.color.g * 255) + "," + Mathf.RoundToInt(img.color.b * 255) + "," + Mathf.RoundToInt(img.color.a * 255) + "]";
        if (t.gameObject.GetComponent<CanvasGroup>() != null) comps += " [Group]";
        sb.Add(pad + t.name + rect + comps + text + (t.gameObject.activeSelf ? "" : " (OFF)"));
        for (int i = 0; i < t.childCount; i++) Walk(t.GetChild(i), depth + 1, sb, cam);
    }
}
