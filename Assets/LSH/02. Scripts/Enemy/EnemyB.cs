//using System.Collections.Generic;
//using UnityEngine;


//public class EnemyB : EnemyOrigin
//{
//    protected override void Start()
//    {
//        enemyID = 2;
//        enemyType = EnemyType.B;
//        base.Start();
//    }
//    protected override void StartEnemyTurn()
//    {
//        Debug.Log("<color=cyan>[적 B의 행동 실행]</color>");
//        base.StartEnemyTurn();
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
//}
