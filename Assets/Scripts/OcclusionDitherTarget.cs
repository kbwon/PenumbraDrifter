using UnityEngine;
using UnityEngine.Rendering;

public class OcclusionDitherTarget : MonoBehaviour
{
    [Header("Refs")]
    public Renderer[] renderers;

    [Tooltip("SG_DitherLitOccluder로 만든 Dither 전용 템플릿 머티리얼을 넣습니다.")]
    public Material ditherTemplateMaterial;

    [Header("Dither")]
    [Tooltip("가려졌을 때 Dither 머티리얼의 _DitherFade 값입니다.")]
    [Range(0f, 1f)]
    public float occludedDitherFade = 1f;

    [Tooltip("가려질 때 Dither 값이 부드럽게 변하게 할지 여부입니다. 우선 false 추천.")]
    public bool useFadeIn = false;

    [Tooltip("useFadeIn이 켜져 있을 때 시작 Dither 값입니다.")]
    [Range(0f, 1f)]
    public float fadeInStartValue = 1f;

    public float fadeSpeed = 8f;

    [Header("Restore")]
    [Tooltip("가림이 풀리면 바로 원본 머티리얼로 복구합니다.")]
    public bool restoreOriginalImmediately = true;

    [Header("Shadow Proxy")]
    [Tooltip("켜면 Dither 상태에서 보이는 Renderer는 그림자를 끄고, 원본 머티리얼을 가진 그림자 전용 복제 Renderer가 그림자를 대신 만듭니다.")]
    public bool useShadowProxy = true;

    [Tooltip("Shadow Proxy 오브젝트 이름 접미사입니다.")]
    public string shadowProxyNameSuffix = "_ShadowOnlyProxy";

    Material[][] originalSharedMaterials;
    Material[][] ditherRuntimeMaterials;
    ShadowCastingMode[] originalShadowCastingModes;

    GameObject shadowProxyRoot;
    Renderer[] shadowProxyRenderers;

    bool initialized;
    bool usingDitherMaterial;

    float currentFade;
    float targetFade;

    void Awake()
    {
        Initialize();

        // 평상시에는 원본 머티리얼 그대로 시작한다.
        SetShadowProxyActive(false);
    }

    void Update()
    {
        if (!initialized) return;
        if (!usingDitherMaterial) return;
        if (!useFadeIn) return;

        currentFade = Mathf.MoveTowards(
            currentFade,
            targetFade,
            fadeSpeed * Time.deltaTime
        );

        ApplyDitherFade(currentFade);
    }

    void OnDestroy()
    {
        RestoreOriginalMaterials();
        DestroyDitherRuntimeMaterials();

        if (shadowProxyRoot != null)
        {
            if (Application.isPlaying)
                Destroy(shadowProxyRoot);
            else
                DestroyImmediate(shadowProxyRoot);
        }
    }

    public void SetOccluded(bool occluded)
    {
        if (!initialized)
            Initialize();

        if (!initialized)
            return;

        if (occluded)
            EnterDitherMode();
        else
            ExitDitherMode();
    }

    void Initialize()
    {
        if (initialized)
            return;

        if (ditherTemplateMaterial == null)
        {
            Debug.LogWarning($"[{nameof(OcclusionDitherTarget)}] {name}: Dither Template Material이 비어 있습니다.");
            return;
        }

        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>(true);

        if (renderers == null || renderers.Length == 0)
        {
            Debug.LogWarning($"[{nameof(OcclusionDitherTarget)}] {name}: Renderer를 찾지 못했습니다.");
            return;
        }

        originalSharedMaterials = new Material[renderers.Length][];
        ditherRuntimeMaterials = new Material[renderers.Length][];
        originalShadowCastingModes = new ShadowCastingMode[renderers.Length];

        for (int r = 0; r < renderers.Length; r++)
        {
            Renderer rend = renderers[r];
            if (!rend) continue;

            originalShadowCastingModes[r] = rend.shadowCastingMode;

            Material[] sourceMats = rend.sharedMaterials;
            originalSharedMaterials[r] = sourceMats;

            Material[] ditherMats = new Material[sourceMats.Length];

            for (int i = 0; i < sourceMats.Length; i++)
            {
                Material src = sourceMats[i];

                if (src == null)
                {
                    ditherMats[i] = null;
                    continue;
                }

                Material inst = new Material(ditherTemplateMaterial);
                inst.name = src.name + "_DitherRuntime";

                CopyCommonMaterialProperties(src, inst);

                if (inst.HasProperty("_DitherFade"))
                    inst.SetFloat("_DitherFade", occludedDitherFade);

                ditherMats[i] = inst;
            }

            ditherRuntimeMaterials[r] = ditherMats;
        }

        if (useShadowProxy)
            CreateShadowProxies();

        initialized = true;
    }

    void EnterDitherMode()
    {
        if (usingDitherMaterial)
        {
            targetFade = occludedDitherFade;

            if (!useFadeIn)
                ApplyDitherFade(occludedDitherFade);

            return;
        }

        usingDitherMaterial = true;

        for (int r = 0; r < renderers.Length; r++)
        {
            Renderer rend = renderers[r];
            if (!rend) continue;

            if (ditherRuntimeMaterials[r] == null)
                continue;

            // 화면에 보이는 Renderer는 Dither 머티리얼로 전환한다.
            rend.sharedMaterials = ditherRuntimeMaterials[r];

            // 중요:
            // 이 Renderer가 계속 그림자를 만들면 Dither 그림자가 생긴다.
            // Shadow Proxy를 쓰는 경우, 보이는 Renderer의 그림자 캐스팅은 끈다.
            if (useShadowProxy)
                rend.shadowCastingMode = ShadowCastingMode.Off;
        }

        // 원본 머티리얼을 가진 그림자 전용 Renderer를 켠다.
        if (useShadowProxy)
            SetShadowProxyActive(true);

        if (useFadeIn)
        {
            currentFade = fadeInStartValue;
            targetFade = occludedDitherFade;
            ApplyDitherFade(currentFade);
        }
        else
        {
            currentFade = occludedDitherFade;
            targetFade = occludedDitherFade;
            ApplyDitherFade(occludedDitherFade);
        }
    }

    void ExitDitherMode()
    {
        if (!usingDitherMaterial)
            return;

        if (restoreOriginalImmediately)
        {
            RestoreOriginalMaterials();
            SetShadowProxyActive(false);

            usingDitherMaterial = false;
            currentFade = 0f;
            targetFade = 0f;
            return;
        }

        RestoreOriginalMaterials();
        SetShadowProxyActive(false);

        usingDitherMaterial = false;
        currentFade = 0f;
        targetFade = 0f;
    }

    void RestoreOriginalMaterials()
    {
        if (renderers == null || originalSharedMaterials == null)
            return;

        for (int r = 0; r < renderers.Length; r++)
        {
            Renderer rend = renderers[r];
            if (!rend) continue;
            if (originalSharedMaterials[r] == null) continue;

            rend.sharedMaterials = originalSharedMaterials[r];
            rend.shadowCastingMode = originalShadowCastingModes[r];
        }
    }

    void ApplyDitherFade(float fade)
    {
        if (ditherRuntimeMaterials == null)
            return;

        for (int r = 0; r < ditherRuntimeMaterials.Length; r++)
        {
            Material[] mats = ditherRuntimeMaterials[r];
            if (mats == null) continue;

            for (int i = 0; i < mats.Length; i++)
            {
                Material mat = mats[i];
                if (!mat) continue;

                if (mat.HasProperty("_DitherFade"))
                    mat.SetFloat("_DitherFade", fade);
            }
        }
    }

    void CreateShadowProxies()
    {
        if (shadowProxyRoot != null)
            return;

        shadowProxyRoot = new GameObject(name + shadowProxyNameSuffix);
        shadowProxyRoot.transform.SetParent(transform, false);
        shadowProxyRoot.transform.localPosition = Vector3.zero;
        shadowProxyRoot.transform.localRotation = Quaternion.identity;
        shadowProxyRoot.transform.localScale = Vector3.one;

        shadowProxyRenderers = new Renderer[renderers.Length];

        for (int r = 0; r < renderers.Length; r++)
        {
            Renderer sourceRenderer = renderers[r];
            if (!sourceRenderer) continue;

            // 현재 건물 에셋은 대부분 MeshRenderer일 가능성이 높다.
            MeshRenderer sourceMeshRenderer = sourceRenderer as MeshRenderer;
            if (!sourceMeshRenderer) continue;

            MeshFilter sourceMeshFilter = sourceRenderer.GetComponent<MeshFilter>();
            if (!sourceMeshFilter || !sourceMeshFilter.sharedMesh) continue;

            GameObject proxy = new GameObject(sourceRenderer.name + "_ShadowOnly");
            proxy.transform.SetParent(sourceRenderer.transform, false);
            proxy.transform.localPosition = Vector3.zero;
            proxy.transform.localRotation = Quaternion.identity;
            proxy.transform.localScale = Vector3.one;

            MeshFilter proxyMeshFilter = proxy.AddComponent<MeshFilter>();
            proxyMeshFilter.sharedMesh = sourceMeshFilter.sharedMesh;

            MeshRenderer proxyRenderer = proxy.AddComponent<MeshRenderer>();
            proxyRenderer.sharedMaterials = originalSharedMaterials[r];

            // 핵심:
            // 화면에는 보이지 않고 그림자만 만든다.
            proxyRenderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            proxyRenderer.receiveShadows = false;

            // 필요 시 라이트 프로브/반사 프로브 설정도 원본과 맞춘다.
            proxyRenderer.lightProbeUsage = sourceMeshRenderer.lightProbeUsage;
            proxyRenderer.reflectionProbeUsage = sourceMeshRenderer.reflectionProbeUsage;
            proxyRenderer.probeAnchor = sourceMeshRenderer.probeAnchor;

            shadowProxyRenderers[r] = proxyRenderer;
        }

        SetShadowProxyActive(false);
    }

    void SetShadowProxyActive(bool active)
    {
        if (shadowProxyRoot == null)
            return;

        shadowProxyRoot.SetActive(active);
    }

    void CopyCommonMaterialProperties(Material src, Material dst)
    {
        CopyBaseMap(src, dst);
        CopyBaseColor(src, dst);
        CopyNormal(src, dst);
        CopyMetallicSmoothness(src, dst);
        CopyEmission(src, dst);
    }

    void CopyBaseMap(Material src, Material dst)
    {
        if (!dst.HasProperty("_BaseMap"))
            return;

        if (src.HasProperty("_BaseMap"))
        {
            dst.SetTexture("_BaseMap", src.GetTexture("_BaseMap"));
            dst.SetTextureScale("_BaseMap", src.GetTextureScale("_BaseMap"));
            dst.SetTextureOffset("_BaseMap", src.GetTextureOffset("_BaseMap"));
            return;
        }

        if (src.HasProperty("_MainTex"))
        {
            dst.SetTexture("_BaseMap", src.GetTexture("_MainTex"));
            dst.SetTextureScale("_BaseMap", src.GetTextureScale("_MainTex"));
            dst.SetTextureOffset("_BaseMap", src.GetTextureOffset("_MainTex"));
        }
    }

    void CopyBaseColor(Material src, Material dst)
    {
        if (!dst.HasProperty("_BaseColor"))
            return;

        if (src.HasProperty("_BaseColor"))
        {
            dst.SetColor("_BaseColor", src.GetColor("_BaseColor"));
            return;
        }

        if (src.HasProperty("_Color"))
        {
            dst.SetColor("_BaseColor", src.GetColor("_Color"));
            return;
        }

        dst.SetColor("_BaseColor", Color.white);
    }

    void CopyNormal(Material src, Material dst)
    {
        if (src.HasProperty("_BumpMap") && dst.HasProperty("_BumpMap"))
            dst.SetTexture("_BumpMap", src.GetTexture("_BumpMap"));

        if (src.HasProperty("_BumpScale") && dst.HasProperty("_BumpScale"))
            dst.SetFloat("_BumpScale", src.GetFloat("_BumpScale"));
    }

    void CopyMetallicSmoothness(Material src, Material dst)
    {
        if (src.HasProperty("_Metallic") && dst.HasProperty("_Metallic"))
            dst.SetFloat("_Metallic", src.GetFloat("_Metallic"));

        if (src.HasProperty("_Smoothness") && dst.HasProperty("_Smoothness"))
            dst.SetFloat("_Smoothness", src.GetFloat("_Smoothness"));
    }

    void CopyEmission(Material src, Material dst)
    {
        if (src.HasProperty("_EmissionMap") && dst.HasProperty("_EmissionMap"))
            dst.SetTexture("_EmissionMap", src.GetTexture("_EmissionMap"));

        if (src.HasProperty("_EmissionColor") && dst.HasProperty("_EmissionColor"))
            dst.SetColor("_EmissionColor", src.GetColor("_EmissionColor"));

        if (src.IsKeywordEnabled("_EMISSION"))
            dst.EnableKeyword("_EMISSION");
    }

    void DestroyDitherRuntimeMaterials()
    {
        if (ditherRuntimeMaterials == null)
            return;

        for (int r = 0; r < ditherRuntimeMaterials.Length; r++)
        {
            Material[] mats = ditherRuntimeMaterials[r];
            if (mats == null) continue;

            for (int i = 0; i < mats.Length; i++)
            {
                Material mat = mats[i];
                if (!mat) continue;

                if (Application.isPlaying)
                    Destroy(mat);
                else
                    DestroyImmediate(mat);
            }
        }
    }
}