using Unity.VisualScripting;
using UnityEngine;

public class TestScripts : MonoBehaviour
{
    [SerializeField] HumanPool humanPool;
    
    // Update is called once per frame
    void Start()
    {
        humanPool = FindAnyObjectByType<HumanPool>();
    }
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.A))
        {
            humanPool.GetHuman();
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            for (int i = 0; i < 5; i++)
            {
                GameObject human = humanPool.GetHuman();

                if (human != null)
                {
                    HumanUnit humanScript = human.GetComponent<HumanUnit>();
                    humanScript.UseAdultUnitCard();
                    Debug.Log("성인 유닛 카드 사용!");
                }
            }
        }
    }
}
