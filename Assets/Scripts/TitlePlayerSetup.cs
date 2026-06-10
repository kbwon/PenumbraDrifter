using UnityEngine;

[DefaultExecutionOrder(1000)]
public class TitlePlayerSetup : MonoBehaviour
{
    public PlayerController player;
    public ShadowInteractController shadow;

    [Header("Title Options")]
    public bool lockBillboard = true;
    public bool infiniteGauge = true;
    public bool forceShadowIdleOnStart = true;

    [Header("Animator State")]
    public string shadowIdleStateName = "Base Layer.ShadowIdle";
    public int animatorLayer = 0;

    [Header("Flip Fix")]
    public bool overrideArtFacesRightInTitle = true;
    public bool titleArtFacesRight = false;

    void Awake()
    {
        ResolveRefs();

        if (shadow != null)
            shadow.infiniteShadowGauge = infiniteGauge;

        if (player != null)
        {
            if (lockBillboard)
                player.SetBillboardLocked(true);

            if (overrideArtFacesRightInTitle)
                player.artFacesRight = titleArtFacesRight;

            player.SyncShadowStateWithoutTransition();
            player.SetInputLocked(false, false);
        }

        if (forceShadowIdleOnStart)
            ForceShadowIdleAnimator();
    }

    void Start()
    {
        ResolveRefs();

        if (shadow != null)
            shadow.infiniteShadowGauge = infiniteGauge;

        if (player != null)
        {
            if (lockBillboard)
                player.SetBillboardLocked(true);

            if (overrideArtFacesRightInTitle)
                player.artFacesRight = titleArtFacesRight;

            player.SyncShadowStateWithoutTransition();
            player.SetInputLocked(false, false);
        }

        if (forceShadowIdleOnStart)
            ForceShadowIdleAnimator();
    }

    void ResolveRefs()
    {
        if (player == null)
            player = GetComponent<PlayerController>();

        if (shadow == null)
            shadow = GetComponent<ShadowInteractController>();
    }

    void ForceShadowIdleAnimator()
    {
        if (player == null || player.anim == null)
            return;

        player.anim.SetBool("isWalk", false);
        player.anim.SetBool("Idle", false);
        player.anim.SetBool("isCrouching", false);
        player.anim.SetBool("isCrouchMoving", false);
        player.anim.SetBool("isPushing", false);
        player.anim.SetBool("isPushMoving", false);
        player.anim.SetBool("isShadowWalk", false);
        player.anim.SetBool("ShadowIdle", true);

        if (!string.IsNullOrEmpty(shadowIdleStateName))
            player.anim.Play(shadowIdleStateName, animatorLayer, 0f);

        player.anim.Update(0f);
    }
}