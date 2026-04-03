using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteLODByZoom : MonoBehaviour
{
    [System.Serializable]
    public class LODLevel
    {
        [Tooltip("이 orthographicSize 이하에서 사용할 스프라이트")]
        public float maxOrthographicSize = 6f;
        public Sprite sprite;
        [Tooltip("true면 이 줌 구간에서는 오브젝트를 숨깁니다.")]
        public bool hide;
        [Tooltip("줌 전환 경계에서 깜빡임을 줄이기 위한 완충값")]
        public float hysteresis = 0.25f;
    }

    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private SpriteRenderer targetRenderer;

    [Header("LOD")]
    [SerializeField] private LODLevel[] levels;

    private int currentLevelIndex = -1;

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
    }

    private void Start()
    {
        ApplyLOD(force: true);
    }

    private void LateUpdate()
    {
        ApplyLOD(force: false);
    }

    private void ApplyLOD(bool force)
    {
        if (targetCamera == null || targetRenderer == null || levels == null || levels.Length == 0)
            return;

        int nextIndex = GetLODIndex(targetCamera.orthographicSize);
        if (!force && nextIndex == currentLevelIndex)
            return;

        currentLevelIndex = nextIndex;
        LODLevel level = levels[currentLevelIndex];

        targetRenderer.enabled = !level.hide;
        if (!level.hide && level.sprite != null)
            targetRenderer.sprite = level.sprite;
    }

    private int GetLODIndex(float orthographicSize)
    {
        if (currentLevelIndex >= 0 && currentLevelIndex < levels.Length)
        {
            float boundary = levels[currentLevelIndex].maxOrthographicSize;
            float hysteresis = Mathf.Max(0f, levels[currentLevelIndex].hysteresis);
            float minStay = boundary - hysteresis;
            float maxStay = boundary + hysteresis;

            bool isLast = currentLevelIndex == levels.Length - 1;
            if ((orthographicSize >= minStay && orthographicSize <= maxStay) || (isLast && orthographicSize >= minStay))
                return currentLevelIndex;
        }

        for (int i = 0; i < levels.Length; i++)
        {
            if (orthographicSize <= levels[i].maxOrthographicSize)
                return i;
        }

        return levels.Length - 1;
    }
}
