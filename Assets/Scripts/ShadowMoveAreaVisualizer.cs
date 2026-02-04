using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ShadowMoveAreaVisualizer : MonoBehaviour
{
    public Transform target; // PlayerRoot
    public ShadowInteractController shadowCtrl;

    [Header("Sampling")]
    public float cellSize = 0.5f;     // 작을수록 정교하지만 무거움
    public int radiusCells = 18;      // 표시 반경 (cell 기준)
    public float updateInterval = 0.25f;

    [Header("Margin (shrink area)")]
    public float margin = 0.35f;      // CC.radius 기반으로 런타임에 덮어씌울 수 있음

    [Header("Rendering")]
    public float yOffset = 0.02f;     // 바닥에서 살짝 띄우기

    MeshFilter mf;
    Mesh mesh;
    float t;

    int size; // grid width = 2*radiusCells+1

    void Awake()
    {
        mf = GetComponent<MeshFilter>();
        mesh = new Mesh { name = "ShadowAreaMesh" };
        mf.sharedMesh = mesh;
        size = radiusCells * 2 + 1;
    }

    void LateUpdate()
    {
        t -= Time.deltaTime;
        if (t > 0f) return;
        t = updateInterval;

        if (!target || !shadowCtrl) { mesh.Clear(); return; }

        // "이동 가능한 그림자 위에 섰을 때"만 표시 (그림자 모드 아닐 때)
        if (shadowCtrl.IsInShadowMode) { mesh.Clear(); return; }

        // 플레이어 발밑이 그림자여야 표시
        if (!GroundUtil.GetGroundPoint(target, shadowCtrl.groundMask, out var gp, out var gn))
        {
            mesh.Clear();
            return;
        }

        // 시작 셀(중앙)이 안전 그림자가 아니면 표시 X
        if (!shadowCtrl.IsShadowSafeAtWorldPos(gp, margin))
        {
            mesh.Clear();
            return;
        }

        // grid index: (ix, iz) in [-r..r]
        // flood fill용
        bool[,] ok = new bool[size, size];
        bool[,] visited = new bool[size, size];

        // 먼저 샘플링(안전 그림자 여부)
        for (int z = -radiusCells; z <= radiusCells; z++)
        {
            for (int x = -radiusCells; x <= radiusCells; x++)
            {
                Vector3 wp = gp + new Vector3(x * cellSize, 0, z * cellSize);
                ok[x + radiusCells, z + radiusCells] = shadowCtrl.IsShadowSafeAtWorldPos(wp, margin);
            }
        }

        // BFS: 중앙에서 연결된 셀만 모으기
        Queue<Vector2Int> q = new Queue<Vector2Int>();
        int cx = radiusCells, cz = radiusCells;
        q.Enqueue(new Vector2Int(cx, cz));
        visited[cx, cz] = true;

        List<Vector2Int> cells = new List<Vector2Int>();

        int[] dx = { 1, -1, 0, 0 };
        int[] dz = { 0, 0, 1, -1 };

        while (q.Count > 0)
        {
            var p = q.Dequeue();
            if (!ok[p.x, p.y]) continue;

            cells.Add(p);

            for (int i = 0; i < 4; i++)
            {
                int nx = p.x + dx[i];
                int nz = p.y + dz[i];
                if (nx < 0 || nz < 0 || nx >= size || nz >= size) continue;
                if (visited[nx, nz]) continue;
                visited[nx, nz] = true;
                if (ok[nx, nz]) q.Enqueue(new Vector2Int(nx, nz));
            }
        }

        BuildMeshFromCells(mesh, gp, gn, cells);
    }

    void BuildMeshFromCells(Mesh m, Vector3 center, Vector3 normal, List<Vector2Int> cells)
    {
        m.Clear();
        if (cells.Count == 0) return;

        int quadCount = cells.Count;
        var verts = new Vector3[quadCount * 4];
        var uvs = new Vector2[quadCount * 4];
        var tris = new int[quadCount * 6];

        // 바닥 평면 기준(Plane이면 normal=up)
        // 여기서는 단순히 XZ에 깔아도 충분
        int vi = 0;
        int ti = 0;

        float half = cellSize * 0.5f;
        Vector3 up = normal;

        for (int i = 0; i < cells.Count; i++)
        {
            int gx = cells[i].x - radiusCells;
            int gz = cells[i].y - radiusCells;

            Vector3 c = center + new Vector3(gx * cellSize, 0, gz * cellSize);
            c += up * yOffset;

            // 사각형 4점
            verts[vi + 0] = c + new Vector3(-half, 0, -half);
            verts[vi + 1] = c + new Vector3(-half, 0, half);
            verts[vi + 2] = c + new Vector3(half, 0, half);
            verts[vi + 3] = c + new Vector3(half, 0, -half);

            uvs[vi + 0] = new Vector2(0, 0);
            uvs[vi + 1] = new Vector2(0, 1);
            uvs[vi + 2] = new Vector2(1, 1);
            uvs[vi + 3] = new Vector2(1, 0);

            tris[ti + 0] = vi + 0;
            tris[ti + 1] = vi + 1;
            tris[ti + 2] = vi + 2;
            tris[ti + 3] = vi + 0;
            tris[ti + 4] = vi + 2;
            tris[ti + 5] = vi + 3;

            vi += 4;
            ti += 6;
        }

        m.vertices = verts;
        m.uv = uvs;
        m.triangles = tris;
        m.RecalculateNormals();
        m.RecalculateBounds();
    }
}
