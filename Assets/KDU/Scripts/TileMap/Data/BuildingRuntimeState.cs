using System;
using System.Collections.Generic;

// ============================================================
// BuildingRuntimeState — 건물 런타임 상태 스냅샷
//
// 노드 이탈 시 BuildingBehaviour.SaveState()가 이 클래스로 직렬화.
// 재진입 시 BuildingBehaviour.LoadState()로 복원.
// ============================================================
[Serializable]
public class BuildingRuntimeState
{
    public int tick;
    public int activeCount;
    public int waiting;

    // 향후 건물 타입별 추가 필드가 필요하면 extraKeys/extraValues로 확장
    public List<string> extraKeys = new List<string>();
    public List<int> extraValues = new List<int>();
}
