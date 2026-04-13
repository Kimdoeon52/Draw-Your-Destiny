// 영주성 — 런타임 로직 없음.
// 배치 완료 즉시 WorldMapManager.OnMansionRebuilt()를 호출해 배치 UI 잠금을 해제한다.
// 카드 시스템은 Mansion BuildingData를 PlaceBuilding에 전달하기만 하면 자동으로 처리됨.
public class MansionBehaviour : BuildingBehaviour
{
    public override void OnPlaced()
    {
        WorldMapManager.Instance?.OnMansionRebuilt();
    }
}
