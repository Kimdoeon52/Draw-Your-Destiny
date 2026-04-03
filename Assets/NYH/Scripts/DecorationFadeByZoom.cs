using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DecorationFadeByZoom : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private float fadeStartZoom = 8f;
    [SerializeField] private float fadeEndZoom = 12f;
    [SerializeField] private bool disableWhenInvisible = true;

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

        if (targetRenderer != null)
            baseColor = targetRenderer.color;
    }

    private void LateUpdate()
    {
        if (targetCamera == null || targetRenderer == null)
            return;

        float zoom = targetCamera.orthographicSize;
        float alpha = 1f;

        if (fadeEndZoom > fadeStartZoom)
            alpha = 1f - Mathf.InverseLerp(fadeStartZoom, fadeEndZoom, zoom);

        Color color = baseColor;
        color.a = alpha;
        targetRenderer.color = color;

        if (disableWhenInvisible)
            targetRenderer.enabled = alpha > 0.01f;
    }
}
