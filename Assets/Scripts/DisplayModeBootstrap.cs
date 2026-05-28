using UnityEngine;

public class DisplayModeBootstrap : MonoBehaviour
{
    public bool forceOnStart = true;

    void Start()
    {
#if !UNITY_EDITOR
        if (!forceOnStart) return;

        Resolution r = Screen.currentResolution;
        Screen.SetResolution(
            r.width,
            r.height,
            FullScreenMode.FullScreenWindow
        );
#endif
    }
}