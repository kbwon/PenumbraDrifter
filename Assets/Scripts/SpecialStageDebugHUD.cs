using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class SpecialStageDebugHUD : MonoBehaviour
{
    public static SpecialStageDebugHUD Instance { get; private set; }

    [Header("Display")]
    public bool showHud = true;
    public bool alsoUnityConsole = true;
    public KeyCode toggleKey = KeyCode.F9;
    public int maxLines = 12;

    [Header("Style")]
    public int fontSize = 18;
    public Vector2 offset = new Vector2(12f, 12f);
    public Vector2 size = new Vector2(900f, 360f);

    string currentStep = "Not started";
    readonly Queue<string> logs = new Queue<string>();

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            showHud = !showHud;
    }

    public static void Step(string message, Object context = null)
    {
        if (Instance != null)
            Instance.SetStepInternal(message);

        Log("STEP", message, context);
    }

    public static void Log(string source, string message, Object context = null)
    {
        string line = $"[{Time.time:000.00}] [{source}] {message}";

        if (Instance != null)
            Instance.AddLine(line);

        if (Instance == null || Instance.alsoUnityConsole)
            Debug.Log($"[SpecialStage] {line}", context);
    }

    public static void Warn(string source, string message, Object context = null)
    {
        string line = $"[{Time.time:000.00}] [WARN/{source}] {message}";

        if (Instance != null)
            Instance.AddLine(line);

        Debug.LogWarning($"[SpecialStage] {line}", context);
    }

    public static void Error(string source, string message, Object context = null)
    {
        string line = $"[{Time.time:000.00}] [ERROR/{source}] {message}";

        if (Instance != null)
            Instance.AddLine(line);

        Debug.LogError($"[SpecialStage] {line}", context);
    }

    void SetStepInternal(string message)
    {
        currentStep = message;
    }

    void AddLine(string line)
    {
        logs.Enqueue(line);

        while (logs.Count > maxLines)
            logs.Dequeue();
    }

    void OnGUI()
    {
        if (!showHud)
            return;

        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = fontSize;
        style.normal.textColor = Color.white;
        style.wordWrap = true;

        string text = $"CURRENT STEP: {currentStep}\n\n";

        foreach (string line in logs)
            text += line + "\n";

        GUI.Box(new Rect(offset.x, offset.y, size.x, size.y), text, style);
    }
}