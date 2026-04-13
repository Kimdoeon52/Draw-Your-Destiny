using System.Collections.Generic;
using UnityEngine;

public class EnemyResources : MonoBehaviour
{
    [Header("적의 골드와 식량")]
    [SerializeField] public int enemygold;
    [SerializeField] public int enemyfood;

    private List<EnemyBrainBase> brains = new List<EnemyBrainBase>(); //각 적 리스트

    public void Register(EnemyBrainBase brain)//적의 브레인 등록
    {
        brains.Add(brain); //브레인 리스트에 추가
        brain.SetFaction(this); //해당 적의 수뇌부의 set faction을 이 클래스의 인스턴스로 설정
    }

    public bool TrySpendGold(int amount) //골드 소비 시도 할때 사용 예를 들면 건물 짓기나 기타 등등,,,?
    {
        if (enemygold < amount) return false; //골드가 부족하면 false 반환
        enemygold -= amount;//골드가 충분하면 골드 차감 후 true 반환
        return true;
    }

    public void AddGold(int amount) //골드 추가할 때 사용
    {
        enemygold += amount;
    }

    public void AddFood(int amount) //식량 추가할 때 사용. 식량은 전투용이긴한데... 굳이 필요한가...?
    {
        enemyfood += amount;
    }
}
