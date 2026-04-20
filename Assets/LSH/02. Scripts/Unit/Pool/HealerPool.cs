using Base;
using PoolBase;
using System.Collections.Generic;
using UnityEngine;

public class HealerPool : HumanPool
{
    [SerializeField] private GameObject bronzePrefab;
    [SerializeField] private GameObject ironPrefab;

    protected Queue<GameObject> bronzepool = new Queue<GameObject>();
    protected Queue<GameObject> ironpool = new Queue<GameObject>();
    protected override void Start()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject human = Instantiate(bronzePrefab, poolParent);
            human.SetActive(false);
            bronzepool.Enqueue(human);
        }
        for (int i = 0; i < poolSize; i++)
        {
            GameObject human = Instantiate(ironPrefab, poolParent);
            human.SetActive(false);
            ironpool.Enqueue(human);
        }
    }

    public override GameObject GetHuman(int ownerCivID)
    {
        switch (GameManager.Instance.playerEra)
        {
            case Era.Bronze:
                {
                    if (bronzepool.Count == 0)
                    {
                        Debug.Log("전부 소환됐음요");
                        return null;
                    }
                    GameObject human = bronzepool.Dequeue();
                    ActivateHuman(human, ownerCivID);
                    return human;
                }
            case Era.Iron:
                {
                    if (ironpool.Count == 0)
                    {
                        Debug.Log("전부 소환됐음요");
                        return null;
                    }
                    GameObject human = ironpool.Dequeue();
                    ActivateHuman(human, ownerCivID);
                    return human;
                }
        }
        return null;
    }
}
