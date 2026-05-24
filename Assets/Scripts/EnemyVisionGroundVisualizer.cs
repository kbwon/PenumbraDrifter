using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(EnemyVision))]
public class EnemyVisionGroundVisualizer : MonoBehaviour
{
    [Header("Refs")]
    public EnemyVision vision;

    [Header("Materials")]
    public Material alertMaterial;   // 옅은 노란색 반투명
    public Material attackMaterial;  // 옅은 빨간색 반투명

    [Header("Ground")]
    public LayerMask groundMask;
    public float groundYOffset = 0.035f;
    public float groundRayHeight = 3f;
    public float groundRayDistance = 8f;

    [Header("Shape")]
    [Range(8, 96)] public int segments = 36;
    public bool hideWhenNoConfig = true;

    MeshFilter alertMeshFilter;
    MeshFilter attackMeshFilter;

    Mesh alertMesh;
    Mesh attackMesh;

    void Awake()
    {
        if (vision == null)
            vision = GetComponent<EnemyVision>();

        alertMeshFilter = CreateVisionMeshObject("AlertVisionCone", alertMaterial);
        attackMeshFilter = CreateVisionMeshObject("AttackVisionCone", attackMaterial);

        alertMesh = alertMeshFilter.sharedMesh;
        attackMesh = attackMeshFilter.sharedMesh;
    }

    void LateUpdate()
    {
        if (vision == null || vision.config == null)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);

        float viewRange = vision.config.viewDistance;
        float attackRange = Mathf.Min(vision.config.attackViewDistance, viewRange);

        BuildConeMesh(alertMeshFilter.transform, alertMesh, viewRange);
        BuildConeMesh(attackMeshFilter.transform, attackMesh, attackRange);
    }

    MeshFilter CreateVisionMeshObject(string objectName, Material material)
    {
        GameObject go = new GameObject(objectName);
        go.transform.SetParent(transform, false);

        MeshFilter mf = go.AddComponent<MeshFilter>();
        MeshRenderer mr = go.AddComponent<MeshRenderer>();

        Mesh mesh = new Mesh();
        mesh.name = objectName + "_Mesh";
        mesh.MarkDynamic();

        mf.sharedMesh = mesh;

        if (material != null)
            mr.sharedMaterial = material;

        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;

        return mf;
    }

    void BuildConeMesh(Transform meshTransform, Mesh mesh, float range)
    {
        if (range <= 0f)
        {
            mesh.Clear();
            return;
        }

        Transform eye = vision.eye != null ? vision.eye : vision.transform;

        Vector3 origin = eye.position;

        Vector3 forward = eye.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude <= 0.0001f)
        {
            mesh.Clear();
            return;
        }

        forward.Normalize();

        int vertexCount = segments + 2;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[segments * 3];

        Vector3 centerGround = ProjectToGround(origin);
        vertices[0] = meshTransform.InverseTransformPoint(centerGround);

        float halfAngle = vision.config.viewAngle * 0.5f;

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = Mathf.Lerp(-halfAngle, halfAngle, t);

            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * forward;
            float visibleDistance = range;

            if (Physics.Raycast(
                    origin,
                    dir,
                    out RaycastHit hit,
                    range,
                    vision.obstacleMask,
                    QueryTriggerInteraction.Ignore))
            {
                visibleDistance = hit.distance;
            }

            Vector3 point = origin + dir * visibleDistance;
            Vector3 groundPoint = ProjectToGround(point);

            vertices[i + 1] = meshTransform.InverseTransformPoint(groundPoint);
        }

        for (int i = 0; i < segments; i++)
        {
            int tri = i * 3;

            triangles[tri] = 0;
            triangles[tri + 1] = i + 1;
            triangles[tri + 2] = i + 2;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }

    Vector3 ProjectToGround(Vector3 worldPoint)
    {
        Vector3 rayOrigin = worldPoint + Vector3.up * groundRayHeight;

        if (groundMask.value != 0 &&
            Physics.Raycast(
                rayOrigin,
                Vector3.down,
                out RaycastHit hit,
                groundRayDistance,
                groundMask,
                QueryTriggerInteraction.Ignore))
        {
            return hit.point + Vector3.up * groundYOffset;
        }

        worldPoint.y = transform.position.y + groundYOffset;
        return worldPoint;
    }

    void SetVisible(bool visible)
    {
        if (!hideWhenNoConfig)
            visible = true;

        if (alertMeshFilter != null)
            alertMeshFilter.gameObject.SetActive(visible);

        if (attackMeshFilter != null)
            attackMeshFilter.gameObject.SetActive(visible);
    }
}