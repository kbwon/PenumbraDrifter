using UnityEngine;

public class WindowMover : MonoBehaviour
{
    [Header("Nudge (per key press)")]
    public int stepPixels = 40;     // 한번 누를 때 이동량
    public bool clampToDisplay = true;

    [Header("Auto move")]
    public bool autoMove = false;
    public float autoSpeed = 1.0f;  // 왕복 속도
    public int autoAmplitude = 200; // 위아래 왕복 폭(픽셀)

    Vector2Int _basePos;

    void Start()
    {
        // Fullscreen이면 위치가 스냅/리사이즈될 수 있어 테스트는 Windowed 추천 :contentReference[oaicite:5]{index=5}
        Screen.fullScreenMode = FullScreenMode.Windowed;

        _basePos = Screen.mainWindowPosition; // 현재 창 좌상단 위치 :contentReference[oaicite:6]{index=6}
        Debug.Log($"[Start] display={Screen.mainWindowDisplayInfo.name}, pos={_basePos}"); // :contentReference[oaicite:7]{index=7}
    }

    void Update()
    {
        // 키보드로 위/아래 "한 칸씩" 이동
        if (Input.GetKeyDown(KeyCode.UpArrow)) Nudge(new Vector2Int(0, -stepPixels));
        if (Input.GetKeyDown(KeyCode.DownArrow)) Nudge(new Vector2Int(0, +stepPixels));

        // (옵션) 좌/우도 같이 테스트하고 싶으면
        if (Input.GetKeyDown(KeyCode.LeftArrow)) Nudge(new Vector2Int(-stepPixels, 0));
        if (Input.GetKeyDown(KeyCode.RightArrow)) Nudge(new Vector2Int(+stepPixels, 0));

        // Space로 자동 이동 토글
        if (Input.GetKeyDown(KeyCode.Space))
        {
            autoMove = !autoMove;
            _basePos = Screen.mainWindowPosition;
        }

        if (autoMove)
        {
            // 같은 모니터에서 y만 사인파로 움직이기
            float t = Time.unscaledTime * autoSpeed;
            int yOffset = Mathf.RoundToInt(Mathf.Sin(t) * autoAmplitude);

            var pos = new Vector2Int(_basePos.x, _basePos.y + yOffset);
            MoveTo(pos);
        }
    }

    void Nudge(Vector2Int delta)
    {
        var cur = Screen.mainWindowPosition; // :contentReference[oaicite:8]{index=8}
        MoveTo(cur + delta);
    }

    void MoveTo(Vector2Int targetPos)
    {
        var display = Screen.mainWindowDisplayInfo; // 현재 창이 올라가 있는 디스플레이 :contentReference[oaicite:9]{index=9}

        if (clampToDisplay)
        {
            // "디스플레이 좌상단 기준" 좌표이므로 0~(displaySize - windowSize)로 대략 클램프
            // (창 테두리/타이틀바 등 OS 장식 때문에 완벽히 딱 맞진 않을 수 있어요)
            int maxX = Mathf.Max(0, display.width - Screen.width);
            int maxY = Mathf.Max(0, display.height - Screen.height);

            targetPos.x = Mathf.Clamp(targetPos.x, 0, maxX);
            targetPos.y = Mathf.Clamp(targetPos.y, 0, maxY);
        }

        // Unity 버전에 따라 display가 in/ref로 보이기도 하는데,
        // '키워드 없이' 넘기면(in 시그니처에서) 가장 안전하게 컴파일됩니다.
        // MoveMainWindowTo는 비동기 이동입니다. :contentReference[oaicite:10]{index=10}
        Screen.MoveMainWindowTo(display, targetPos);
    }
}
