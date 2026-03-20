using System.Collections.Generic;
using UnityEngine;

public class HumanPool : MonoBehaviour
{
    public GameObject humanPrefab;
    public int poolSize = 20;

    private Queue<GameObject> pool = new Queue<GameObject>();

    void Start()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject human = Instantiate(humanPrefab);
            human.SetActive(false);

            pool.Enqueue(human);
        }
    }

    public GameObject GetHuman() //이거 쓰셈 소환할때.(카드 만드는 사람은 이걸 읽도록)
    {
        if (pool.Count == 0)
        {
            Debug.Log("전부 소환됐음요");
            return null;
        }

        GameObject human = pool.Dequeue();
        human.SetActive(true);
        HumanUnit humanUnit = human.GetComponent<HumanUnit>();
        if (humanUnit != null)
        {
            humanUnit.UnitAppear();
        }

        return human;
    }

    public void ReturnHuman(GameObject human)
    {
        human.SetActive(false);
        pool.Enqueue(human);
    }
}
