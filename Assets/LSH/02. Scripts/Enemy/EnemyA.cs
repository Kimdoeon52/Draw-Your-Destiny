//using UnityEngine;

//public class EnemyA : EnemyOrigin
//{
//    protected override void Start()
//    {
//        enemyID = 1;
//        enemyType = EnemyType.A;
//        base.Start();
//    }

//    protected override void StartEnemyTurn()
//    {
//        Debug.Log("<color=yellow>[적 A의 행동 실행]</color>");
//        base.StartEnemyTurn();
//    }

//    protected override void DoBuilding()
//    {
//        base.DoBuilding();
//    }

//    protected override void BuildSmallTown()
//    {
//        if (outPostData == null)
//        {
//            Debug.LogWarning("outpostData가 연결되지 않았습니다.");
//            return;
//        }

//        if (gold < outpostCost)
//        {
//            Debug.LogWarning($"소규모 영지 건설 골드 부족. 현재 골드: {gold}");
//            return;
//        }

//        TileMapManager tileMapManager = TileMapManager.Instance;
//        if (tileMapManager == null)
//        {
//            Debug.LogWarning("TileMapManager가 없습니다.");
//            return;
//        }

//        if (!TryFindNearestGoldMine(out Vector3Int nearestGoldMine))
//        {
//            Debug.LogWarning("가장 가까운 금광을 찾지 못했습니다.");
//            return;
//        }

//        if (!TryFindOutpostPositionNearGoldMine(nearestGoldMine, out Vector3Int outpostPos))
//        {
//            Debug.LogWarning("금광 주변에 소규모 영지를 지을 위치를 찾지 못했습니다.");
//            return;
//        }

//        bool success = tileMapManager.PlaceBuildingForAI(outpostPos, outPostData, enemyID);

//        if (!success)
//        {
//            Debug.LogWarning("소규모 영지 배치에 실패했습니다.");
//            return;
//        }

//        gold -= outpostCost;

//        Debug.Log($"<color=cyan>[소규모 영지 건설]</color> {enemyType}가 금광 {nearestGoldMine} 근처 {outpostPos}에 영지를 설치했습니다. 현재 골드: {gold}");
//    }
//    // 적 A는 더 많은 인간을 뽑는다고 가정
//    //protected override void DoGetEnemyHuman()
//    //{
//    //    Debug.Log("A는 더 많이 뽑는다!");

//    //    GameObject enemy1 = enemyUnitPool.GetEnemyUnit(enemyType);
//    //    GameObject enemy2 = enemyUnitPool.GetEnemyUnit(enemyType);

//    //    gold -= 20;
//    //}

//    // 적 A는 건물 안 짓는다고 가정
//    //protected override void DoBuilding()
//    //{
//    //    Debug.Log("A는 건물 안 지음");
//    //}

//    // 적 A는 인간 소환이 더 싸다고 가정
//    //protected override bool CheckAction(EnemyActionType action)
//    //{
//    //    if (action == EnemyActionType.GetHuman)
//    //        return gold >= 5; // A는 더 싸게 소환

//    //    return base.CheckAction(action);
//    //}
//}
