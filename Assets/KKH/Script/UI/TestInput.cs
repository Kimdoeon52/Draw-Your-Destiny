using UnityEngine;

public class TestInput : MonoBehaviour
{
    void Update()
    {
        if (ResourceManager.Instance == null || GameManager.Instance == null) return; // Null 체크 추가

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
        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("E Key Pressed: Calling GameManager.EndTurn()");
            GameManager.Instance.EndTurn(); // 턴 종료를 호출하여 checkResearch 실행
        }

        if (Input.GetKeyDown(KeyCode.R)) // 연구 포인트 테스트를 위한 임시 추가
        {
            ResourceManager.Instance.AddResearch(100); // 시대를 바로 전환할 만큼의 연구 포인트 추가
            Debug.Log("R Key Pressed: Added 100 Research Points.");
        }

        if (Input.GetKeyDown(KeyCode.F)) // 식량 추가 테스트
        {
            ResourceManager.Instance.AddFood(50); // 식량 50 추가
            Debug.Log("F Key Pressed: Added 50 Food.");
        }
        if (Input.GetKeyDown(KeyCode.M)) // 광석 추가 테스트
        {
            ResourceManager.Instance.AddIron(50); // 광석 50 추가
            Debug.Log("M Key Pressed: Added 50 Iron.");
        }
    }
}

