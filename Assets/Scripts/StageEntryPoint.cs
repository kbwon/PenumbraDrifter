using UnityEngine;

public class StageEntryPoint : MonoBehaviour
{
    public string entryId = "Stage01_Start";

    [Header("Points")]
    public Transform spawnPoint;
    public Transform walkTarget;

    [Header("Camera On Enter")]
    public bool setCameraYawOnEnter = true;
    public float cameraYawOnEnter = 45f;

    [Header("Stage Intro")]
    public bool playStageIntroAfterEnter = true;
    public StageIntroDirector stageIntroDirector;

    [Header("Fallback")]
    public Vector3 fallbackWalkDirection = Vector3.forward;

    public Vector3 SpawnPosition
    {
        get
        {
            if (spawnPoint != null) return spawnPoint.position;
            return transform.position;
        }
    }

    public Vector3 GetWalkDirection()
    {
        if (walkTarget != null)
        {
            Vector3 dir = walkTarget.position - SpawnPosition;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.0001f)
                return dir.normalized;
        }

        Vector3 fallback = fallbackWalkDirection;
        fallback.y = 0f;

        if (fallback.sqrMagnitude <= 0.0001f)
            fallback = Vector3.forward;

        return fallback.normalized;
    }

    public float GetWalkDistance()
    {
        if (walkTarget == null) return 4f;

        Vector3 a = SpawnPosition;
        Vector3 b = walkTarget.position;
        a.y = 0f;
        b.y = 0f;

        return Vector3.Distance(a, b);
    }
}