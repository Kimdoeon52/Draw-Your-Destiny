using UnityEngine;

public class TestInput : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ResourceManager.Instance.AddGold(100);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ResourceManager.Instance.AddResearch(10);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ResourceManager.Instance.AddPopulation(1);
        }
    }
}
