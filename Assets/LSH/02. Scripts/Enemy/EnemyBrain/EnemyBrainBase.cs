using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyAction
{
    Building, //시대에 따른 건물 짓기를 다르게 할 것
    GetGold, //골드나 식량을 동시에 얻을꺼임.
}

public enum EnemyState
{
    Attack, //공격 <- 전투 전용 행동 타입
    Defend //방어 <- 일반적인 문명 행동 타입
}

public class EnemyBrainBase : MonoBehaviour
{
    [Header("적 행동 확률")]
    [SerializeField] protected List<ActionCase> actionCases = new List<ActionCase>();
}
