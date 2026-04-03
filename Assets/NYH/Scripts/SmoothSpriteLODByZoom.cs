using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SmoothSpriteLODByZoom : MonoBehaviour
{
    [System.Serializable]
    public class LODStage
    {
        [Tooltip("이 줌 이하에서는 이 스테이지를 사용합니다.")]
        public float maxOrthographicSize = 6f;
        public Sprite sprite;
        [Tooltip("해당 구간에서 기본 크기 대비 얼마나 작아질지")]
        public float scaleMultiplier = 1f;
        [Range(0f, 1f)] public float alpha = 1f;
    }

    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private SpriteRenderer targetRenderer;

    [Header("LOD")]
    [SerializeField] private LODStage[] stages;
    [SerializeField] private bool hideWhenNearlyInvisible = true;
    [SerializeField] private float hiddenAlphaThreshold = 0.02f;

    private Vector3 baseScale;
    private Color baseColor;

    private void Reset()
    {
        targetRenderer = GetComponent<SpriteRenderer>();
        if (Camera.main != null)
            targetCamera = Camera.main;
    }

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<SpriteRenderer>();
        if (targetCamera == null)
            targetCamera = Camera.main;

        baseScale = transform.localScale;
        if (targetRenderer != null)
            baseColor = targetRenderer.color;
    }

    private void LateUpdate()
    {
        ApplySmoothLOD();
    }

    private void ApplySmoothLOD()
    {
        if (targetCamera == null || targetRenderer == null || stages == null || stages.Length == 0)
            return;

        float zoom = targetCamera.orthographicSize;
        int currentIndex = GetStageIndex(zoom);
        int nextIndex = Mathf.Min(currentIndex + 1, stages.Length - 1);

        LODStage currentStage = stages[currentIndex];
        LODStage nextStage = stages[nextIndex];

        float currentMin = currentIndex == 0 ? 0f : stages[currentIndex - 1].maxOrthographicSize;
        float currentMax = currentStage.maxOrthographicSize;
        float t = currentMax > currentMin
            ? Mathf.InverseLerp(currentMin, currentMax, zoom)
            : 0f;

        if (currentStage.sprite != null)
            targetRenderer.sprite = currentStage.sprite;

        float scale = Mathf.Lerp(currentStage.scaleMultiplier, nextStage.scaleMultiplier, t);
        transform.localScale = baseScale * scale;

        float alpha = Mathf.Lerp(currentStage.alpha, nextStage.alpha, t);
        Color color = baseColor;
        color.a = alpha;
        targetRenderer.color = color;

        if (hideWhenNearlyInvisible)
            targetRenderer.enabled = alpha > hiddenAlphaThreshold;
        else
            targetRenderer.enabled = true;
    }

    private int GetStageIndex(float zoom)
    {
        for (int i = 0; i < stages.Length; i++)
        {
            if (zoom <= stages[i].maxOrthographicSize)
                return i;
        }

        return stages.Length - 1;
    }
}
