using System;
using UnityEngine;


/// 플레이어가 현재 서 있는 표면이 그림자인지 검사하고,
/// 그림자 모드 진입/해제, 게이지 소모/회복, 표면 앵커 유지 등을 담당하는 컨트롤러. 

/// 1. Awake에서 필요한 참조와 초기 상태를 준비
/// 2. Update에서 현재 표면의 점/법선을 구함
/// 3. 그 점이 그림자인지 검사
/// 4. 인디케이터, 게이지, 그림자 모드 상태를 갱신
/// 5. 필요할 때 앵커를 이용해 벽/천장/바닥 표면에 붙은 상태를 유지

public class ShadowInteractController : MonoBehaviour
{
    #region Inspector Fields

    [Header("Masks")]
    public LayerMask groundMask;//일반적인 바닥 레이어 마스크.
    public LayerMask occluderMask;//그림자를 만드는 장애물 레이어 마스크.
    public LayerMask surfaceMask;// 현재 플레이어가 붙어 있거나 서 있을 수 있는 표면 레이어 마스크.

    [Header("Gameplay Lights")]

    /// <summary>
    /// 실제 게임 플레이에서 그림자 판정에 사용하는 광원 목록.
    /// 분위기용 조명은 제외하고, 판정에 반영할 광원만 넣는 것이 중요하다.
    /// </summary>
    public Light[] gameplayLights;

    [Header("Indicator")]

    /// <summary>
    /// 현재 위치에서 그림자 모드 진입이 가능한지 보여주는 인디케이터 오브젝트.
    /// 보통 플레이어 발밑 표시 등에 사용한다.
    /// </summary>
    public GameObject shadowIndicator;

    [Header("Shadow Mode")]

    /// <summary>
    /// 그림자 모드 진입 시 아래로 살짝 내릴 시각 루트.
    /// 실제 물리 위치가 아니라 연출용 루트이다.
    /// </summary>
    public Transform visualRoot;

    /// <summary>
    /// 그림자 모드 진입 시 visualRoot를 Y축으로 얼마나 내릴지 결정한다.
    /// 음수일수록 아래로 내려간다.
    /// </summary>
    public float sinkVisualY = -0.35f;

    /// <summary>
    /// 그림자 모드일 때 이동 속도 배수.
    /// 1보다 작으면 느려진다.
    /// </summary>
    public float shadowSpeedMul = 0.6f;

    /// <summary>
    /// Directional Light의 그림자 판정 등에 사용할 최대 검사 거리.
    /// </summary>
    public float maxDirDistance = 80f;

    [Header("Shadow Gauge")]

    /// <summary>
    /// 게이지가 가득 찬 상태에서 모두 소모되기까지 걸리는 시간.
    /// </summary>
    public float drainFullSeconds = 5f;

    /// <summary>
    /// 게이지가 0에서 가득 찰 때까지 회복되는 시간.
    /// </summary>
    public float regenFullSeconds = 10f;

    /// <summary>
    /// 현재 그림자 게이지 값.
    /// 0~1 범위를 사용한다.
    /// </summary>
    [Range(0f, 1f)] public float gauge01 = 1f;

    [Header("Input")]

    /// <summary>
    /// 그림자 모드 진입/해제에 사용할 마우스 버튼 번호.
    /// 기본값 1은 보통 우클릭이다.
    /// </summary>
    public int mouseButton = 1;

    [Header("Anchor")]

    /// <summary>
    /// 표면에 스냅할 때 표면과 약간 띄워 줄 추가 오프셋.
    /// 표면에 너무 파묻히는 것을 방지한다.
    /// </summary>
    public float anchorSurfaceOffset = 0.08f;

    /// <summary>
    /// 앵커 표면을 다시 찾기 위해 레이캐스트를 쏠 때,
    /// 표면 법선 방향으로 얼마나 떨어진 위치에서 시작할지 정한다.
    /// </summary>
    public float anchorProbeOffset = 1f;

    /// <summary>
    /// 앵커 표면 재탐색 시 사용할 최대 레이 거리.
    /// </summary>
    public float anchorProbeDistance = 2.5f;

    [Header("Colliders")]

    /// <summary>
    /// 일반 상태에서 사용하는 캡슐 콜라이더.
    /// </summary>
    public CapsuleCollider normalCollider;

    /// <summary>
    /// 그림자 모드 상태에서 사용하는 캡슐 콜라이더.
    /// 그림자 모드에서 더 납작하거나 다른 충돌 형태를 쓰고 싶을 때 사용한다.
    /// </summary>
    public CapsuleCollider shadowCollider;

    #endregion

    #region Runtime State

    /// <summary>
    /// 현재 그림자 모드인지 여부.
    /// </summary>
    bool inShadowMode;

    /// <summary>
    /// 인디케이터가 현재 켜져 있는지 캐시해 중복 SetActive 호출을 줄인다.
    /// </summary>
    bool indicatorVisible;

    /// <summary>
    /// 현재 특정 표면에 앵커가 걸려 있는지 여부.
    /// 벽/천장/특수 표면을 따라다닐 때 사용한다.
    /// </summary>
    bool hasSurfaceAnchorInternal;

    /// <summary>
    /// 그림자 모드 진입 전 visualRoot의 원래 로컬 위치를 저장한다.
    /// 그림자 모드 해제 시 원위치 복원에 사용한다.
    /// </summary>
    Vector3 visualOriginalLocalPos;

    /// <summary>
    /// 현재 앵커가 걸린 표면의 법선 방향.
    /// </summary>
    Vector3 anchorNormal;

    /// <summary>
    /// 현재 앵커가 걸린 표면의 콜라이더.
    /// </summary>
    Collider anchorCollider;

    /// <summary>
    /// 마지막으로 UI 등에 알린 게이지 값.
    /// 값이 실제로 바뀌었을 때만 이벤트를 보내기 위한 캐시이다.
    /// </summary>
    float lastGaugeValue = -1f;

    #endregion

    #region Events / Properties

    /// <summary>
    /// 그림자 모드 진입/해제 시 외부에 알리는 이벤트.
    /// true면 진입, false면 해제.
    /// </summary>
    public event Action<bool> OnShadowModeChanged;

    /// <summary>
    /// 게이지 값이 변했을 때 외부에 알리는 이벤트.
    /// UI 갱신 등에 사용 가능하다.
    /// </summary>
    public event Action<float> OnGaugeChanged;

    /// <summary>
    /// 현재 그림자 모드 여부를 외부에 제공한다.
    /// </summary>
    public bool IsInShadowMode => inShadowMode;

    /// <summary>
    /// 현재 상태에 따른 이동 속도 배수를 제공한다.
    /// 일반 상태는 1, 그림자 모드는 shadowSpeedMul.
    /// </summary>
    public float SpeedMultiplier => inShadowMode ? shadowSpeedMul : 1f;

    /// <summary>
    /// 현재 게이지 값을 외부에 제공한다.
    /// </summary>
    public float Gauge01 => gauge01;

    /// <summary>
    /// 현재 표면 앵커가 유효한지 외부에 제공한다.
    /// </summary>
    public bool HasSurfaceAnchor => hasSurfaceAnchorInternal;

    /// <summary>
    /// 현재 앵커 표면의 법선을 외부에 제공한다.
    /// </summary>
    public Vector3 AnchorNormal => anchorNormal;

    /// <summary>
    /// 현재 상태에 맞는 캡슐 콜라이더를 반환한다.
    /// 그림자 모드면 shadowCollider를 우선 사용하고,
    /// 일반 모드면 normalCollider를 우선 사용한다.
    /// 둘 다 없으면 자기 자신에게 붙은 CapsuleCollider를 fallback으로 사용한다.
    /// </summary>
    public CapsuleCollider ActiveCollider
    {
        get
        {
            if (inShadowMode)
            {
                if (shadowCollider != null) return shadowCollider;
                if (normalCollider != null) return normalCollider;
            }
            else
            {
                if (normalCollider != null) return normalCollider;
                if (shadowCollider != null) return shadowCollider;
            }

            return GetComponent<CapsuleCollider>();
        }
    }

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// 시작 시 필요한 참조를 캐시하고 초기 상태를 맞춘다.
    /// </summary>
    void Awake()
    {
        // normal / shadow 콜라이더 자동 탐색
        CacheColliders();

        // GameManager에 자신을 등록
        if (GameManager.Instance != null)
            GameManager.Instance.RegisterShadow(this);

        // 시작 시 인디케이터는 끈 상태로 시작
        if (shadowIndicator != null)
            shadowIndicator.SetActive(false);

        // 그림자 모드 연출 복원을 위해 원래 visual 위치 저장
        if (visualRoot != null)
            visualOriginalLocalPos = visualRoot.localPosition;

        // surfaceMask를 따로 안 넣었다면 groundMask를 기본값으로 사용
        if (surfaceMask.value == 0)
            surfaceMask = groundMask;

        // 시작은 일반 모드
        ApplyColliderMode(false);

        // 게이지 초기값을 UI 등에 강제로 한번 알림
        NotifyGaugeChanged(true);
    }

    /// <summary>
    /// 매 프레임 현재 표면과 그림자 상태를 검사하고,
    /// 인디케이터, 게이지, 그림자 모드 전환을 처리한다.
    /// </summary>
    void Update()
    {
        // 1. 현재 플레이어가 붙어 있는 표면의 점과 법선을 구한다.
        if (!TryGetCurrentSurfacePoint(out Vector3 point, out Vector3 normal))
        {
            // 표면 자체를 찾지 못하면 그림자 판정 불가
            SetIndicator(false);

            // 그림자 모드 중이었다면 강제로 해제
            if (inShadowMode)
                ExitShadowMode();

            return;
        }

        // 2. 현재 표면의 점이 그림자인지 검사
        bool onShadow = IsShadowAtPoint(point, normal);

        // 3. 일반 상태일 때만 "진입 가능" 인디케이터를 보여줌
        if (!inShadowMode)
            SetIndicator(onShadow);
        else
            SetIndicator(false);

        // 4. 그림자 위에 있는지 여부에 따라 게이지 갱신
        UpdateGauge(onShadow);

        // 5. 입력이 들어왔고 현재 그림자 위라면 그림자 모드 진입/해제
        if (Input.GetMouseButtonDown(mouseButton) && onShadow)
        {
            if (!inShadowMode)
            {
                // 게이지가 남아 있어야만 진입 가능
                if (gauge01 > 0f)
                    EnterShadowMode();
            }
            else
            {
                ExitShadowMode();
            }
        }
    }

    #endregion

    #region Core State Control

    /// <summary>
    /// 자식 오브젝트 포함 전체에서 normal / shadow 콜라이더를 자동 탐색한다.
    /// Inspector에 직접 넣지 않았을 때 대비용이다.
    /// </summary>
    void CacheColliders()
    {
        if (normalCollider != null && shadowCollider != null)
            return;

        CapsuleCollider[] cols = GetComponentsInChildren<CapsuleCollider>(true);
        for (int i = 0; i < cols.Length; i++)
        {
            CapsuleCollider col = cols[i];
            if (col == null) continue;

            // 자기 자신에 붙은 콜라이더는 우선 normalCollider로 취급
            if (col.gameObject == gameObject)
            {
                if (normalCollider == null)
                    normalCollider = col;
                continue;
            }

            // 아직 normalCollider가 없다면 첫 번째 자식 콜라이더를 normal로 사용
            if (normalCollider == null)
            {
                normalCollider = col;
                continue;
            }

            // 그 외 다른 콜라이더 하나를 shadowCollider로 사용
            if (shadowCollider == null && col != normalCollider)
                shadowCollider = col;
        }
    }

    /// <summary>
    /// 그림자 모드 진입 시 호출된다.
    /// 콜라이더를 그림자 모드용으로 바꾸고, visualRoot를 아래로 내린다.
    /// </summary>
    void EnterShadowMode()
    {
        inShadowMode = true;
        ApplyColliderMode(true);

        if (visualRoot != null)
        {
            Vector3 pos = visualOriginalLocalPos;
            pos.y += sinkVisualY;
            visualRoot.localPosition = pos;
        }

        SetIndicator(false);
        OnShadowModeChanged?.Invoke(true);
    }

    /// <summary>
    /// 그림자 모드 해제 시 호출된다.
    /// 콜라이더와 visualRoot를 원래 상태로 되돌리고,
    /// 표면 앵커도 함께 초기화한다.
    /// </summary>
    void ExitShadowMode()
    {
        inShadowMode = false;
        ApplyColliderMode(false);

        if (visualRoot != null)
            visualRoot.localPosition = visualOriginalLocalPos;

        ClearSurfaceAnchor();
        OnShadowModeChanged?.Invoke(false);
    }

    /// <summary>
    /// 현재 상태에 맞게 normal / shadow 콜라이더 활성화 상태를 전환한다.
    /// </summary>
    void ApplyColliderMode(bool shadowMode)
    {
        if (normalCollider != null && normalCollider != shadowCollider)
            normalCollider.enabled = !shadowMode;

        if (shadowCollider != null)
            shadowCollider.enabled = shadowMode;

        // shadowCollider가 없으면 normalCollider를 항상 유지
        if (normalCollider != null && shadowCollider == null)
            normalCollider.enabled = true;
    }

    /// <summary>
    /// 외부에서 강제로 그림자 모드를 해제하고 싶을 때 사용하는 함수.
    /// </summary>
    public void ForceExitShadowMode()
    {
        if (inShadowMode)
            ExitShadowMode();
    }

    #endregion

    #region Gauge / Indicator

    /// <summary>
    /// 그림자 위에 있는지 여부와 현재 모드에 따라 게이지를 갱신한다.
    /// 
    /// - 그림자 모드 중: 게이지 소모
    /// - 일반 모드: 게이지 회복
    /// - 그림자 모드인데 그림자에서 벗어나면 자동 해제
    /// - 게이지가 0이 되면 자동 해제
    /// </summary>
    void UpdateGauge(bool onShadow)
    {
        if (inShadowMode)
        {
            gauge01 -= Time.deltaTime / Mathf.Max(0.01f, drainFullSeconds);
            gauge01 = Mathf.Clamp01(gauge01);

            if (gauge01 <= 0f)
            {
                gauge01 = 0f;
                ExitShadowMode();
            }
            else if (!onShadow)
            {
                ExitShadowMode();
            }
        }
        else
        {
            gauge01 += Time.deltaTime / Mathf.Max(0.01f, regenFullSeconds);
            gauge01 = Mathf.Clamp01(gauge01);
        }

        NotifyGaugeChanged(false);
    }

    /// <summary>
    /// 게이지 값이 실제로 바뀌었을 때만 이벤트를 보낸다.
    /// force가 true면 값 비교 없이 무조건 알린다.
    /// </summary>
    void NotifyGaugeChanged(bool force)
    {
        if (force || Mathf.Abs(lastGaugeValue - gauge01) > 0.0001f)
        {
            lastGaugeValue = gauge01;
            OnGaugeChanged?.Invoke(gauge01);
        }
    }

    /// <summary>
    /// 그림자 진입 가능 인디케이터의 표시 상태를 바꾼다.
    /// 이미 같은 상태면 아무것도 하지 않는다.
    /// </summary>
    void SetIndicator(bool visible)
    {
        if (shadowIndicator == null) return;
        if (indicatorVisible == visible) return;

        indicatorVisible = visible;
        shadowIndicator.SetActive(visible);
    }

    #endregion

    #region Surface / Shadow Query

    /// <summary>
    /// 현재 플레이어가 서 있거나 붙어 있는 표면의 점과 법선을 구한다.
    /// 
    /// 우선순위:
    /// 1. 앵커가 있으면 앵커 표면을 다시 찾는다.
    /// 2. 앵커가 없거나 실패하면 위에서 아래로 레이를 쏘아 surfaceMask 표면을 찾는다.
    /// </summary>
    public bool TryGetCurrentSurfacePoint(out Vector3 point, out Vector3 normal)
    {
        // 앵커가 있으면 먼저 그 표면을 다시 찾아본다.
        if (hasSurfaceAnchorInternal && anchorCollider != null)
        {
            Vector3 origin = transform.position + anchorNormal * anchorProbeOffset;
            Vector3 dir = -anchorNormal;

            if (Physics.Raycast(origin, dir, out RaycastHit hit, anchorProbeDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider == anchorCollider)
                {
                    point = hit.point;
                    normal = hit.normal;
                    return true;
                }
            }

            // 기존 앵커 표면을 더 이상 찾지 못하면 앵커 해제
            ClearSurfaceAnchor();
        }

        // 일반적인 바닥/표면 탐색
        Vector3 originDown = transform.position + Vector3.up * 2f;
        if (Physics.Raycast(originDown, Vector3.down, out RaycastHit hitDown, 10f, surfaceMask, QueryTriggerInteraction.Ignore))
        {
            point = hitDown.point;
            normal = hitDown.normal;
            return true;
        }

        point = default;
        normal = Vector3.up;
        return false;
    }

    // 월드 좌표 하나를 기준으로 그 위치가 그림자인지 판정
    public bool IsShadowAtWorldPos(Vector3 worldPos)
    {
        Vector3 origin = worldPos + Vector3.up * 2f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 10f, surfaceMask, QueryTriggerInteraction.Ignore))
            return IsShadowAtPoint(hit.point, hit.normal);

        return false;
    }

    // 특정 표면의 점과 법선을 기준으로 그림자인지 판정
    public bool IsShadowAtPoint(Vector3 point, Vector3 normal)
    {
        if (gameplayLights != null && gameplayLights.Length > 0)
        {
            return ShadowQuery.IsPointInShadow(
                point,
                normal,
                gameplayLights,
                occluderMask,
                maxDirDistance: maxDirDistance
            );
        }

        return false;
    }

    /// <summary>
    /// 월드 좌표를 기준으로 "안전한 그림자"인지 검사한다.
    /// 단순히 한 점만 보는 것이 아니라, 내부적으로 중심 + 주변 지점들도 함께 검사한다.
    /// </summary>
    public bool IsShadowSafeAtWorldPos(Vector3 worldPos, float margin)
    {
        // 앵커 표면이 있으면 그 표면 기준으로 검사
        if (hasSurfaceAnchorInternal && anchorCollider != null)
        {
            Vector3 origin = worldPos + anchorNormal * anchorProbeOffset;
            Vector3 dir = -anchorNormal;

            if (Physics.Raycast(origin, dir, out RaycastHit hit, anchorProbeDistance, ~0, QueryTriggerInteraction.Ignore)
                && hit.collider == anchorCollider)
            {
                return IsShadowSafeAtPoint(hit.point, hit.normal, margin);
            }
        }

        // 일반적인 표면 탐색 후 검사
        Vector3 originDown = worldPos + Vector3.up * 2f;
        if (!Physics.Raycast(originDown, Vector3.down, out RaycastHit hitDown, 10f, surfaceMask, QueryTriggerInteraction.Ignore))
            return false;

        return IsShadowSafeAtPoint(hitDown.point, hitDown.normal, margin);
    }

    /// <summary>
    /// 중심점 하나만 보는 것이 아니라, 주변 네 방향까지 함께 검사해
    /// 플레이어가 실제로 설 수 있을 만큼 충분히 넓은 그림자인지 확인한다.
    /// </summary>
    public bool IsShadowSafeAtPoint(Vector3 point, Vector3 normal, float margin)
    {
        normal = normal.normalized;
        BuildTangents(normal, out Vector3 t1, out Vector3 t2);

        Vector3[] offsets =
        {
            Vector3.zero,
            t1 * margin,
            -t1 * margin,
            t2 * margin,
            -t2 * margin,
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            Vector3 test = point + offsets[i];
            if (!IsShadowAtPoint(test, normal))
                return false;
        }

        return true;
    }

    /// <summary>
    /// 주어진 표면 법선으로부터 표면 위 두 개의 접선 방향을 만든다.
    /// 안전 그림자 검사 시 주변 점을 배치하는 데 사용한다.
    /// </summary>
    static void BuildTangents(Vector3 normal, out Vector3 t1, out Vector3 t2)
    {
        Vector3 axis = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > 0.95f ? Vector3.right : Vector3.up;
        t1 = Vector3.Cross(normal, axis).normalized;
        t2 = Vector3.Cross(normal, t1).normalized;
    }

    #endregion

    #region Surface Anchor / Snap

    /// <summary>
    /// 현재 표면에 앵커를 설정한다.
    /// 벽/천장/특수 표면을 따라다니는 동안 같은 표면을 계속 참조하는 데 사용한다.
    /// </summary>
    public void SetSurfaceAnchor(Vector3 normal, Collider col)
    {
        hasSurfaceAnchorInternal = true;
        anchorNormal = normal.normalized;
        anchorCollider = col;
    }

    /// <summary>
    /// 현재 표면 앵커를 해제한다.
    /// </summary>
    public void ClearSurfaceAnchor()
    {
        hasSurfaceAnchorInternal = false;
        anchorCollider = null;
    }

    /// <summary>
    /// 앵커 표면을 다시 찾아 actor를 해당 표면에 맞게 스냅한다.
    /// Transform 기반 이동을 사용하는 쪽에서 사용 가능하다.
    /// </summary>
    public void SnapToAnchoredSurface(Transform actor, float snapDistance = 2f)
    {
        if (actor == null || !hasSurfaceAnchorInternal || anchorCollider == null) return;

        Vector3 origin = actor.position + anchorNormal * anchorProbeOffset;
        Vector3 dir = -anchorNormal;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, snapDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider != anchorCollider) return;
            actor.position = GetRootPositionForSurfaceHit(hit.point, hit.normal, anchorSurfaceOffset);
        }
    }

    /// <summary>
    /// 앵커 표면을 다시 찾아 Rigidbody를 해당 표면에 맞게 스냅한다.
    /// Rigidbody 기반 이동을 사용하는 쪽에서 사용 가능하다.
    /// </summary>
    public void SnapToAnchoredSurface(Rigidbody body, float snapDistance = 2f)
    {
        if (body == null || !hasSurfaceAnchorInternal || anchorCollider == null) return;

        Vector3 origin = body.position + anchorNormal * anchorProbeOffset;
        Vector3 dir = -anchorNormal;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, snapDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider != anchorCollider) return;
            body.position = GetRootPositionForSurfaceHit(hit.point, hit.normal, anchorSurfaceOffset);
        }
    }

    /// <summary>
    /// 표면의 hit 정보로부터 플레이어 루트가 가야 할 실제 위치를 계산한다.
    /// 
    /// - 바닥: 콜라이더 하단이 바닥에 닿도록 Y 계산
    /// - 천장: 콜라이더 상단이 천장에 닿도록 Y 계산
    /// - 벽: 표면 법선 방향으로 반지름만큼 밀어냄
    /// </summary>
    public Vector3 GetRootPositionForSurfaceHit(Vector3 point, Vector3 normal, float extraOffset)
    {
        CapsuleCollider col = ActiveCollider;
        if (col == null) return transform.position;

        Vector3 newPos = transform.position;
        float radius = GetColliderRadiusWorld(col);
        float halfH = GetColliderHeightWorld(col) * 0.5f;
        float centerY = GetColliderCenterRootLocal(col).y;

        // 바닥
        if (normal.y > 0.7f)
        {
            newPos.x = point.x;
            newPos.z = point.z;
            newPos.y = point.y + (halfH - centerY) + extraOffset;
        }
        // 천장
        else if (normal.y < -0.7f)
        {
            newPos.x = point.x;
            newPos.z = point.z;
            newPos.y = point.y - (centerY + halfH) - extraOffset;
        }
        // 벽
        else
        {
            newPos.x = point.x;
            newPos.z = point.z;
            newPos.y = point.y + (halfH - centerY);
            newPos += normal.normalized * (radius + extraOffset);
        }

        return newPos;
    }

    #endregion

    #region Active Collider Utility

    /// <summary>
    /// 현재 활성 콜라이더의 월드 반지름을 반환한다.
    /// </summary>
    public float GetActiveRadiusWorld()
    {
        CapsuleCollider col = ActiveCollider;
        return col != null ? GetColliderRadiusWorld(col) : 0.35f;
    }

    /// <summary>
    /// 현재 활성 콜라이더의 월드 높이를 반환한다.
    /// </summary>
    public float GetActiveHeightWorld()
    {
        CapsuleCollider col = ActiveCollider;
        return col != null ? GetColliderHeightWorld(col) : 2f;
    }

    /// <summary>
    /// 현재 활성 콜라이더 중심의 월드 좌표를 반환한다.
    /// </summary>
    public Vector3 GetActiveCenterWorld()
    {
        CapsuleCollider col = ActiveCollider;
        if (col == null) return transform.position;
        return col.transform.TransformPoint(col.center);
    }

    /// <summary>
    /// 현재 활성 콜라이더 중심을 플레이어 루트 기준 로컬 좌표로 반환한다.
    /// </summary>
    public Vector3 GetActiveCenterRootLocal()
    {
        CapsuleCollider col = ActiveCollider;
        if (col == null) return Vector3.zero;
        return GetColliderCenterRootLocal(col);
    }

    /// <summary>
    /// 안전 그림자 검사 등에 사용할 여유 거리 값을 반환한다.
    /// 보통 현재 콜라이더 반지름의 일정 비율을 사용한다.
    /// </summary>
    public float GetActiveMargin(float factor = 0.9f)
    {
        return GetActiveRadiusWorld() * factor;
    }

    /// <summary>
    /// 특정 캡슐 콜라이더 중심을 플레이어 루트 기준 로컬 좌표로 변환한다.
    /// </summary>
    Vector3 GetColliderCenterRootLocal(CapsuleCollider col)
    {
        Vector3 worldCenter = col.transform.TransformPoint(col.center);
        return transform.InverseTransformPoint(worldCenter);
    }

    /// <summary>
    /// 특정 캡슐 콜라이더의 월드 높이를 계산한다.
    /// Y 스케일을 반영한다.
    /// </summary>
    float GetColliderHeightWorld(CapsuleCollider col)
    {
        Vector3 scale = col.transform.lossyScale;
        return col.height * Mathf.Abs(scale.y);
    }

    /// <summary>
    /// 특정 캡슐 콜라이더의 월드 반지름을 계산한다.
    /// X/Z 중 더 큰 스케일을 반영한다.
    /// </summary>
    float GetColliderRadiusWorld(CapsuleCollider col)
    {
        Vector3 scale = col.transform.lossyScale;
        float radiusScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        return col.radius * radiusScale;
    }

    #endregion
}