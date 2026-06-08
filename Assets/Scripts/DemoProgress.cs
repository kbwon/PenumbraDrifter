using UnityEngine;

public enum DemoContinuePoint
{
    None = 0,
    Stage1 = 1,
    SpecialStage = 2,
    BossStage = 3
}

public static class DemoProgress
{
    const string ContinuePointKey = "PenumbraDemo_ContinuePoint";

    public static DemoContinuePoint ContinuePoint
    {
        get
        {
            int value = PlayerPrefs.GetInt(ContinuePointKey, 0);
            return (DemoContinuePoint)Mathf.Clamp(value, 0, 3);
        }
    }

    public static bool HasContinue => ContinuePoint != DemoContinuePoint.None;

    public static void ResetProgress()
    {
        PlayerPrefs.SetInt(ContinuePointKey, 0);
        PlayerPrefs.Save();
    }

    public static void SaveContinuePoint(DemoContinuePoint point)
    {
        DemoContinuePoint current = ContinuePoint;

        if ((int)point < (int)current)
            return;

        PlayerPrefs.SetInt(ContinuePointKey, (int)point);
        PlayerPrefs.Save();
    }

    public static string GetContinueSceneName()
    {
        switch (ContinuePoint)
        {
            case DemoContinuePoint.Stage1:
                return "Stage1";

            case DemoContinuePoint.SpecialStage:
                return "SpecialStage";

            case DemoContinuePoint.BossStage:
                return "BossStage";

            default:
                return "";
        }
    }
}