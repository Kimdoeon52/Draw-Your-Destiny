using UnityEngine;
using UnityEngine.Tilemaps;

public class TileCoordFinder : MonoBehaviour
{
    public Tilemap targetTilemap;

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        worldPos.z = 0f;
        Vector3Int cellPos = targetTilemap.WorldToCell(worldPos);

        Debug.Log($"[TileCoordFinder] 클릭 타일 좌표: {cellPos}");
    }
}
