using System.Collections.Generic;
using NYH.CoreCardSystem;
using UnityEngine;

/// <summary>
/// 여러 효과 중 확률에 따라 하나만 골라서 발생시키는 컨테이너 효과입니다.
/// </summary>
[CreateAssetMenu(fileName = "RandomChanceEffect", menuName = "NYH/Effect/RandomChance")]
public class RandomChanceEffect : Effect
{
    [System.Serializable]
    public class EffectOption
    {
        public Effect effect; // 실행할 효과 (골드 증가, 인구 증가 등)
        public float weight; // 확률 가중치 (예: 70, 30 또는 0.7, 0.3)
    }

    [Header("발동 후보 효과 목록")]
    [SerializeField] private List<EffectOption> options = new List<EffectOption>();

    public override GameAction GetGameAction(int effectIndex = 0, Card sourceCard = null)
    {
        if (options == null || options.Count == 0) return null;

        // 1. 전체 가중치의 합을 구함
        float totalWeight = 0;
        foreach (var option in options)
        {
            totalWeight += option.weight;
        }

        // 2. 0부터 전체 합 사이의 랜덤 값 생성
        float roll = Random.Range(0, totalWeight);
        float currentWeight = 0;

        // 3. 랜덤 값이 어느 구간에 속하는지 확인하여 효과 선택
        foreach (var option in options)
        {
            currentWeight += option.weight;
            if (roll <= currentWeight)
            {
                if (option.effect != null)
                {
                    Debug.Log($"[RandomChance] 당첨된 효과: {option.effect}");
                    return option.effect.GetGameAction();
                }
                break;
            }
        }

        return null;
    }
}