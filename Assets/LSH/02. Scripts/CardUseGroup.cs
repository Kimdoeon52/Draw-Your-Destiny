using UnityEngine;

public class CardUseGroup : MonoBehaviour
{
    [SerializeField] HumanPool humanPool;

    private void Start()
    {
        humanPool = FindAnyObjectByType<HumanPool>();
    }

    //-----------------유닛 소환하는 함수들---------------------
    //호출할때 N명 입력해서 소환하는거임.

    public void SpawnSolider(int n) //병사 소환
    {
        for (int i = 0; i < n; i++)
        {
            GameObject human = humanPool.GetHuman(0);
            if(human != null)
            {
                HumanUnit humanScript = human.GetComponent<HumanUnit>();
                humanScript.job = Job.Soldier;
                humanScript.UnitAppear();
            }
        }
    }

    public void SpawnFarmer(int n) //농부 소환
    {
        for (int i = 0; i < n; i++)
        {
            GameObject human = humanPool.GetHuman(0);
            if (human != null)
            {
                HumanUnit humanScript = human.GetComponent<HumanUnit>();
                humanScript.job = Job.Farmer;
                humanScript.UnitAppear();
            }
        }
    }
    public void SpawnMiner(int n) //광부 소환
    {
        for (int i = 0; i < n; i++)
        {
            GameObject human = humanPool.GetHuman(0);
            if (human != null)
            {
                HumanUnit humanScript = human.GetComponent<HumanUnit>();
                humanScript.job = Job.Miner;
                humanScript.UnitAppear();
            }
        }
    }

}