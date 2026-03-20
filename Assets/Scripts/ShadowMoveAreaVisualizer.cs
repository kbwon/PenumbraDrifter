using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ShadowMoveAreaVisualizer : MonoBehaviour
{
    public Transform target;
    public ShadowInteractController shadowCtrl;
    public PlayerController player;

    [Header("Sampling")]
    public float cellSize = 0.5f;
    public int radiusCells = 18;
    public float updateInterval = 0.25f;

    [Header("Margin")]
    public float margin = 0.35f;
    public bool usePlayerRadius = true;

    [Header("Rendering")]
    public float yOffset = 0.02f;

    MeshFilter mf;
    Mesh mesh;
    float timer;
    int size;

    void Awake()
    {
        mf = GetComponent<MeshFilter>();
        mesh = new Mesh { name = "ShadowAreaMesh" };
        mf.sharedMesh = mesh;
        size = radiusCells * 2 + 1;

        if (GameManager.Instance != null)
        {
            if (target == null && GameManager.Instance.PlayerTransform != null)
                target = GameManager.Instance.PlayerTransform;

            if (shadowCtrl == null)
                shadowCtrl = GameManager.Instance.shadow;

            if (player == null)
                player = GameManager.Instance.player;
        }
    }

    void LateUpdate()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = updateInterval;

        if (!target || !shadowCtrl)
        {
            mesh.Clear();
            return;
        }

        if (!player && target)
            player = target.GetComponent<PlayerController>();

        if (shadowCtrl.IsInShadowMode)
        {
            mesh.Clear();
            return;
        }

        if (!shadowCtrl.TryGetCurrentSurfacePoint(out Vector3 surfacePoint, out Vector3 surfaceNormal))
        {
            mesh.Clear();
            return;
        }

        float currentMargin = usePlayerRadius && player != null
            ? shadowCtrl.GetActiveRadiusWorld() * 0.9f
            : margin;

        if (!shadowCtrl.IsShadowSafeAtPoint(surfacePoint, surfaceNormal, currentMargin))
        {
            mesh.Clear();
            return;
        }

        bool[,] ok = new bool[size, size];
        bool[,] visited = new bool[size, size];

        for (int z = -radiusCells; z <= radiusCells; z++)
        {
            for (int x = -radiusCells; x <= radiusCells; x++)
            {
                Vector3 worldPos = surfacePoint + new Vector3(x * cellSize, 0f, z * cellSize);
                ok[x + radiusCells, z + radiusCells] = shadowCtrl.IsShadowSafeAtWorldPos(worldPos, currentMargin);
            }
        }

        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        int cx = radiusCells;
        int cz = radiusCells;
        queue.Enqueue(new Vector2Int(cx, cz));
        visited[cx, cz] = true;

        List<Vector2Int> cells = new List<Vector2Int>();
        int[] dx = { 1, -1, 0, 0 };
        int[] dz = { 0, 0, 1, -1 };

        // 현재 위치와 연결된 셀만 표시한다.
        while (queue.Count > 0)
        {
            Vector2Int p = queue.Dequeue();
            if (!ok[p.x, p.y]) continue;

            cells.Add(p);

            for (int i = 0; i < 4; i++)
            {
                int nx = p.x + dx[i];
                int nz = p.y + dz[i];

                if (nx < 0 || nz < 0 || nx >= size || nz >= size) continue;
                if (visited[nx, nz]) continue;

                visited[nx, nz] = true;
                if (ok[nx, nz])
                    queue.Enqueue(new Vector2Int(nx, nz));
            }
        }

        BuildMeshFromCells(mesh, surfacePoint, surfaceNormal, cells);
    }

    void BuildMeshFromCells(Mesh targetMesh, Vector3 center, Vector3 normal, List<Vector2Int> cells)
    {
        targetMesh.Clear();
        if (cells.Count == 0) return;

        int quadCount = cells.Count;
        Vector3[] verts = new Vector3[quadCount * 4];
        Vector2[] uvs = new Vector2[quadCount * 4];
        int[] tris = new int[quadCount * 6];

        int vi = 0;
        int ti = 0;
        float half = cellSize * 0.5f;
        Vector3 up = normal;

        for (int i = 0; i < cells.Count; i++)
        {
            int gx = cells[i].x - radiusCells;
            int gz = cells[i].y - radiusCells;

            Vector3 c = center + new Vector3(gx * cellSize, 0f, gz * cellSize);
            c += up * yOffset;

            verts[vi + 0] = c + new Vector3(-half, 0f, -half);
            verts[vi + 1] = c + new Vector3(-half, 0f, half);
            verts[vi + 2] = c + new Vector3(half, 0f, half);
            verts[vi + 3] = c + new Vector3(half, 0f, -half);

            uvs[vi + 0] = new Vector2(0f, 0f);
            uvs[vi + 1] = new Vector2(0f, 1f);
            uvs[vi + 2] = new Vector2(1f, 1f);
            uvs[vi + 3] = new Vector2(1f, 0f);

            tris[ti + 0] = vi + 0;
            tris[ti + 1] = vi + 1;
            tris[ti + 2] = vi + 2;
            tris[ti + 3] = vi + 0;
            tris[ti + 4] = vi + 2;
            tris[ti + 5] = vi + 3;

            vi += 4;
            ti += 6;
        }

        targetMesh.vertices = verts;
        targetMesh.uv = uvs;
        targetMesh.triangles = tris;
        targetMesh.RecalculateNormals();
        targetMesh.RecalculateBounds();
    }
}
