using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ShadowOutlineVisualizer : MonoBehaviour
{
    [Header("Refs")]
    public Transform target;                       // PlayerRoot
    public ShadowInteractController shadowCtrl;     // PlayerRoot의 ShadowInteractController

    [Header("Sampling")]
    public float cellSize = 0.5f;      // 0.4~0.6 권장 (작을수록 부드러움, 비용↑)
    public int radiusCells = 18;       // 표시 반경(셀 단위)
    public float updateInterval = 0.25f;

    [Header("Shrink / Margin")]
    public float margin = 0.35f;       // 경계 “조금 줄이기” (cc.radius*0.9 추천)

    [Header("Rendering")]
    public float yOffset = 0.03f;      // 바닥에서 살짝 띄우기
    public int smoothIterations = 1;   // 0~2 권장 (Chaikin 스무딩)

    [Header("When to show")]
    public bool showOnlyWhenNotInShadowMode = true;

    LineRenderer lr;
    float timer;

    int Size => radiusCells * 2 + 1; // 셀 그리드 크기

    // 정수 격자 코너(부동소수 오차 방지)
    struct EdgeKey
    {
        public Vector2Int a, b; // a < b 로 정규화
        public EdgeKey(Vector2Int p0, Vector2Int p1)
        {
            if (p0.x < p1.x || (p0.x == p1.x && p0.y <= p1.y)) { a = p0; b = p1; }
            else { a = p1; b = p0; }
        }
    }

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = true;
    }

    void LateUpdate()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = updateInterval;

        if (!target || !shadowCtrl)
        {
            ClearLine();
            return;
        }

        if (showOnlyWhenNotInShadowMode && shadowCtrl.IsInShadowMode)
        {
            ClearLine();
            return;
        }

        // 플레이어 발밑 지점
        if (!GroundUtil.GetGroundPoint(target, shadowCtrl.groundMask, out var gp, out var gn))
        {
            ClearLine();
            return;
        }

        // margin 자동 세팅(추천): CharacterController radius 기반
        var cc = target.GetComponent<CharacterController>();
        float effectiveMargin = (cc != null) ? (cc.radius * 0.9f) : margin;
        effectiveMargin = Mathf.Max(0.05f, effectiveMargin);

        // 중앙이 안전 그림자가 아니면 표시 X
        if (!shadowCtrl.IsShadowSafeAtWorldPos(gp, effectiveMargin))
        {
            ClearLine();
            return;
        }

        // 1) 샘플링: 안전 그림자 여부
        int size = Size;
        bool[,] ok = new bool[size, size];

        for (int z = -radiusCells; z <= radiusCells; z++)
        {
            for (int x = -radiusCells; x <= radiusCells; x++)
            {
                Vector3 wp = gp + new Vector3(x * cellSize, 0f, z * cellSize);
                ok[x + radiusCells, z + radiusCells] = shadowCtrl.IsShadowSafeAtWorldPos(wp, effectiveMargin);
            }
        }

        // 2) FloodFill: 중앙과 연결된 셀만 모으기
        List<Vector2Int> cells = FloodFillConnected(ok, size, radiusCells, radiusCells);

        if (cells.Count == 0)
        {
            ClearLine();
            return;
        }

        // 3) 경계 엣지 -> 루프 만들기(여러 루프 중 가장 큰 1개)
        List<Vector2Int> loop = BuildLargestLoopFromCells(cells, ok, size);

        if (loop == null || loop.Count < 4)
        {
            ClearLine();
            return;
        }

        // 4) 코너 좌표 -> 월드 좌표
        List<Vector3> pts = CornerLoopToWorld(loop, gp, cellSize, yOffset, size);

        // 5) 스무딩(선택)
        if (smoothIterations > 0)
            pts = ChaikinSmooth(pts, smoothIterations);

        ApplyLine(pts);
    }

    void ClearLine()
    {
        lr.positionCount = 0;
    }

    void ApplyLine(List<Vector3> pts)
    {
        if (pts == null || pts.Count < 4)
        {
            ClearLine();
            return;
        }

        lr.loop = true;
        lr.positionCount = pts.Count;
        lr.SetPositions(pts.ToArray());
    }

    static List<Vector2Int> FloodFillConnected(bool[,] ok, int size, int sx, int sz)
    {
        var cells = new List<Vector2Int>();
        var visited = new bool[size, size];
        var q = new Queue<Vector2Int>();

        if (!ok[sx, sz]) return cells;

        q.Enqueue(new Vector2Int(sx, sz));
        visited[sx, sz] = true;

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

        return cells;
    }

    static List<Vector2Int> BuildLargestLoopFromCells(List<Vector2Int> cells, bool[,] ok, int size)
    {
        // 경계 엣지 수집 (코너 격자: [0..size] 범위)
        var edges = new HashSet<EdgeKey>();

        foreach (var c in cells)
        {
            int x = c.x;
            int z = c.y;

            bool left = (x > 0) && ok[x - 1, z];
            bool right = (x < size - 1) && ok[x + 1, z];
            bool down = (z > 0) && ok[x, z - 1];
            bool up = (z < size - 1) && ok[x, z + 1];

            // cell (x,z)의 코너
            Vector2Int bl = new Vector2Int(x, z);
            Vector2Int tl = new Vector2Int(x, z + 1);
            Vector2Int tr = new Vector2Int(x + 1, z + 1);
            Vector2Int br = new Vector2Int(x + 1, z);

            if (!left) edges.Add(new EdgeKey(bl, tl));
            if (!right) edges.Add(new EdgeKey(br, tr));
            if (!down) edges.Add(new EdgeKey(bl, br));
            if (!up) edges.Add(new EdgeKey(tl, tr));
        }

        if (edges.Count == 0) return null;

        // 인접 리스트
        var adj = new Dictionary<Vector2Int, List<Vector2Int>>();
        void AddAdj(Vector2Int a, Vector2Int b)
        {
            if (!adj.TryGetValue(a, out var list)) { list = new List<Vector2Int>(2); adj[a] = list; }
            list.Add(b);
        }

        foreach (var e in edges)
        {
            AddAdj(e.a, e.b);
            AddAdj(e.b, e.a);
        }

        // “엣지 단위”로 루프를 구성해서 여러 루프 중 가장 긴 것 선택
        var used = new HashSet<EdgeKey>();
        List<Vector2Int> bestLoop = null;
        int bestLen = 0;

        foreach (var e in edges)
        {
            if (used.Contains(e)) continue;

            // 루프 시작
            var loop = new List<Vector2Int>();
            Vector2Int start = e.a;
            Vector2Int prev = e.a;
            Vector2Int cur = e.b;

            used.Add(e);
            loop.Add(start);
            loop.Add(cur);

            for (int guard = 0; guard < 100000; guard++)
            {
                if (cur == start) break;

                var nbs = adj[cur];
                if (nbs == null || nbs.Count == 0) break;

                // prev가 아닌 쪽으로 계속 진행
                Vector2Int next = nbs[0];
                if (nbs.Count > 1 && next == prev) next = nbs[1];

                var ek = new EdgeKey(cur, next);
                if (used.Contains(ek)) break;

                used.Add(ek);
                prev = cur;
                cur = next;
                loop.Add(cur);
            }

            // 닫힌 루프 형태로 끝났는지 확인(마지막이 start면 ok)
            if (loop.Count >= 4 && loop[loop.Count - 1] == start)
            {
                // 마지막 start 중복 제거(라인은 loop=true로 닫을 거라)
                loop.RemoveAt(loop.Count - 1);
                if (loop.Count > bestLen)
                {
                    bestLen = loop.Count;
                    bestLoop = loop;
                }
            }
        }

        return bestLoop;
    }

    static List<Vector3> CornerLoopToWorld(List<Vector2Int> loop, Vector3 gp, float cellSize, float yOffset, int size)
    {
        // size = 2r+1 (셀), 코너 좌표는 [0..size]
        int r = (size - 1) / 2;
        float half = cellSize * 0.5f;

        var pts = new List<Vector3>(loop.Count);
        foreach (var p in loop)
        {
            float wx = (p.x - r) * cellSize - half;
            float wz = (p.y - r) * cellSize - half;
            pts.Add(new Vector3(gp.x + wx, gp.y + yOffset, gp.z + wz));
        }
        return pts;
    }

    static List<Vector3> ChaikinSmooth(List<Vector3> pts, int iterations)
    {
        for (int it = 0; it < iterations; it++)
        {
            var result = new List<Vector3>(pts.Count * 2);
            for (int i = 0; i < pts.Count; i++)
            {
                Vector3 p0 = pts[i];
                Vector3 p1 = pts[(i + 1) % pts.Count];

                Vector3 q = Vector3.Lerp(p0, p1, 0.25f);
                Vector3 r = Vector3.Lerp(p0, p1, 0.75f);

                result.Add(q);
                result.Add(r);
            }
            pts = result;
        }
        return pts;
    }
}
