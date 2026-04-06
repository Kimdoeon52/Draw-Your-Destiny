using UnityEngine;
using UnityEngine.EventSystems;

public class MinimapClick : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private RectTransform minimapRect; // 미니맵 UI의 Rect
    [SerializeField] private Transform worldMapBounds;  // 실제 월드맵의 크기 기준
    [SerializeField] private Camera mainCamera;         // 조절할 메인 카메라

    public void OnPointerClick(PointerEventData eventData)
    {
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(minimapRect, eventData.position, eventData.pressEventCamera, out localPoint))
        {
            float width = minimapRect.rect.width;
            float height = minimapRect.rect.height;

            // 1. 미니맵 내 클릭 비율 계산 (0~1)
            float xRate = (localPoint.x + minimapRect.rect.width * 0.5f) / minimapRect.rect.width;
            float yRate = (localPoint.y + minimapRect.rect.height * 0.5f) / minimapRect.rect.height;
            // 2. 만약 맵의 중앙이 (0,0)이라면 -100, 100
            // 만약 맵의 좌하단이 (0,0)이라면 0, 200으로 설정
            float startX = -138f;
            float startY = -8f;
            float mapWidth = 154f;  // (16 - (-138))
            float mapHeight = 144f; // (136 - (-8))

            float worldX = startX + (xRate * mapWidth);
            float worldY = startY + (yRate * mapHeight);
            // 3. 카메라 이동 (Z값 유지)
            mainCamera.transform.position = new Vector3(worldX, worldY, mainCamera.transform.position.z);
            Debug.Log($"미니맵 클릭됨ㅇㅇ 좌표: {eventData.position}");
        }

    }
}