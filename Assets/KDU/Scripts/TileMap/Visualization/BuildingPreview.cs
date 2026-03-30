using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BuildingPreview : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private BuildingData currentBuilding;
    private bool isActive = false;
    private bool hasLoggedUpdate = false;

    [Header("Preview Colors")]
    public Color validColor   = new Color(0, 1, 0, 0.5f);
    public Color invalidColor = new Color(1, 0, 0, 0.5f);

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

        spriteRenderer.sortingOrder = 10;
        HidePreview();
    }

    public void ShowPreview(BuildingData building)
    {
        if (building == null) return;

        currentBuilding = building;
        spriteRenderer.sprite = building.sprite;
        isActive = true;
        spriteRenderer.enabled = true;
        hasLoggedUpdate = false;

        Debug.Log($"[BuildingPreview] ShowPreview building={building.buildingName}, sprite={(building.sprite != null)}, enabled={spriteRenderer.enabled}");

        transform.localScale = new Vector3(building.width, building.height, 1);
    }

    public void HidePreview()
    {
        isActive = false;
        spriteRenderer.enabled = false;
        currentBuilding = null;
        hasLoggedUpdate = false;
    }

    public void UpdatePreview(Vector3 position, bool isValid)
    {
        if (!isActive) return;

        transform.position = position;
        spriteRenderer.color = isValid ? validColor : invalidColor;

        if (!hasLoggedUpdate)
        {
            Debug.Log($"[BuildingPreview] UpdatePreview pos={position}, valid={isValid}, sprite={(spriteRenderer.sprite != null)}, sortingOrder={spriteRenderer.sortingOrder}");
            hasLoggedUpdate = true;
        }
    }
}
