using UnityEngine;

// ============================================================
// BuildingBehaviour — 모든 건물 Behaviour의 추상 부모
//
// BuildingData.visualPrefab에 부착해서 사용.
// TileMapManager가 PlaceBuilding 시 instance를 주입하고 OnPlaced 호출.
// 노드 이탈 시 SaveState, 재진입 시 LoadState가 호출된다.
// ============================================================
public abstract class BuildingBehaviour : MonoBehaviour
{
    [System.NonSerialized]
    public BuildingInstance instance;

    // 배치 완료 직후 1회 호출 (재진입 복원 시는 LoadState가 대신 역할)
    public virtual void OnPlaced() { }

    // 매 턴 종료 시 GameManager.EndTurn()에서 호출
    public virtual void OnTurnEnd() { }

    // 노드 이탈 시 호출 — 런타임 상태를 BuildingRuntimeState로 직렬화
    public virtual BuildingRuntimeState SaveState()
    {
        return new BuildingRuntimeState();
    }

    // 노드 재진입 시 호출 — 저장된 상태 복원
    public virtual void LoadState(BuildingRuntimeState state) { }
}
