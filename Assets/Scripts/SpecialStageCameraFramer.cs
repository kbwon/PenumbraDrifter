using UnityEngine;

public class SpecialStageCameraFramer : MonoBehaviour
{
    [Header("Refs")]
    public FollowCamera followCamera;
    public Transform player;
    public Transform elevatorRoot;

    [Header("Focus")]
    public Vector3 localBaseFocus = Vector3.zero;

    [Tooltip("창문이 더 보이게 카메라 중심을 살짝 밀어주는 값입니다. 엘리베이터 로컬 좌표 기준입니다.")]
    public Vector3 localViewBias = new Vector3(0.4f, 0f, 0.8f);

    [Header("Clamp")]
    public Vector2 clampX = new Vector2(-1.2f, 1.2f);
    public Vector2 clampZ = new Vector2(-0.8f, 1.0f);

    [Header("Smooth")]
    public float focusSmooth = 8f;

    bool active;
    Vector3 currentFocus;

    void Awake()
    {
        ResolveRefs();
    }

    void ResolveRefs()
    {
        if (followCamera == null && GameManager.Instance != null)
            followCamera = GameManager.Instance.followCamera;

        if (player == null && GameManager.Instance != null && GameManager.Instance.PlayerTransform != null)
            player = GameManager.Instance.PlayerTransform;
    }

    public void Begin()
    {
        ResolveRefs();

        if (followCamera == null || player == null || elevatorRoot == null)
            return;

        active = true;

        currentFocus = BuildTargetFocus();

        followCamera.SetCinematicMode(true);
        followCamera.SetCinematicInstantPosition(false);
        followCamera.SetFocusPoint(currentFocus);
        followCamera.SnapNow();
    }

    public void End()
    {
        active = false;

        if (followCamera != null)
        {
            followCamera.SetCinematicInstantPosition(true);
            followCamera.ClearFocusOverride();
        }
    }

    void LateUpdate()
    {
        if (!active) return;
        if (followCamera == null || player == null || elevatorRoot == null) return;

        Vector3 targetFocus = BuildTargetFocus();
        float k = 1f - Mathf.Exp(-focusSmooth * Time.deltaTime);

        currentFocus = Vector3.Lerp(currentFocus, targetFocus, k);
        followCamera.SetFocusPoint(currentFocus);
    }

    Vector3 BuildTargetFocus()
    {
        Vector3 playerLocal = elevatorRoot.InverseTransformPoint(player.position);

        float x = Mathf.Clamp(playerLocal.x, clampX.x, clampX.y);
        float z = Mathf.Clamp(playerLocal.z, clampZ.x, clampZ.y);

        Vector3 focusLocal = localBaseFocus + localViewBias;
        focusLocal.x += x;
        focusLocal.z += z;

        return elevatorRoot.TransformPoint(focusLocal);
    }
}