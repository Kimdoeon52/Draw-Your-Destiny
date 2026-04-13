using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 미니맵 UI를 클릭하면 메인 카메라를 해당 월드 좌표로 이동시키는 컴포넌트.
/// IPointerClickHandler를 구현하여 UI EventSystem과 연동한다.
/// </summary>
public class MinimapClick : MonoBehaviour, IPointerClickHandler
{
    #region ── Inspector ──

    [Header("미니맵 레퍼런스")]
    [Tooltip("미니맵 UI의 RectTransform")]
    [SerializeField] private RectTransform minimapRect;

    [Tooltip("실제 월드맵의 크기 기준 Transform (현재 미사용, 확장용)")]
    [SerializeField] private Transform worldMapBounds;

    [Tooltip("이동시킬 메인 카메라")]
    [SerializeField] private Camera mainCamera;

    #endregion

    #region ── 월드맵 좌표 범위 (하드코딩 — 맵 변경 시 수정 필요) ──

    // 맵 좌하단 월드 좌표
    private const float MAP_START_X = -138f;
    private const float MAP_START_Y = -8f;

    // 맵 전체 크기 (width = 16 - (-138), height = 136 - (-8))
    private const float MAP_WIDTH  = 154f;
    private const float MAP_HEIGHT = 144f;

    #endregion

    /// <summary>
    /// 미니맵 UI 클릭 시 호출. 클릭 위치 비율을 월드 좌표로 변환하여 카메라를 이동시킨다.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                minimapRect, eventData.position, eventData.pressEventCamera, out localPoint))
            return;

        // ① 미니맵 내 클릭 비율 계산 (0~1)
        float xRate = (localPoint.x + minimapRect.rect.width  * 0.5f) / minimapRect.rect.width;
        float yRate = (localPoint.y + minimapRect.rect.height * 0.5f) / minimapRect.rect.height;

        // ② 비율 → 월드 좌표 변환
        float worldX = MAP_START_X + (xRate * MAP_WIDTH);
        float worldY = MAP_START_Y + (yRate * MAP_HEIGHT);

        // ③ 카메라 이동 (Z 값 유지)
        mainCamera.transform.position = new Vector3(worldX, worldY, mainCamera.transform.position.z);
    }
}