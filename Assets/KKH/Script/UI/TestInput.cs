using UnityEngine;

/// <summary>
/// 디버그/테스트용 키 입력 핸들러.
/// 숫자 키와 알파벳 키로 자원 추가·턴 종료 등을 빠르게 테스트한다.
/// 릴리스 빌드에서는 비활성화하거나 제거해야 한다.
/// </summary>
/// <remarks>
/// ■ 조작법
///   1 — 골드 +100
///   2 — 연구 +10
///   3 — 인구 +1
///   R — 연구 +100 (시대 전환 테스트)
///   F — 식량 +50
///   E — 턴 종료 (GameManager.EndTurn)
/// </remarks>
public class TestInput : MonoBehaviour
{
    private void Update()
    {
        if (ResourceManager.Instance == null || GameManager.Instance == null) return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
            ResourceManager.Instance.AddGold(100);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            ResourceManager.Instance.AddResearch(10);

        if (Input.GetKeyDown(KeyCode.Alpha3))
            ResourceManager.Instance.AddPopulation(1);

        if (Input.GetKeyDown(KeyCode.E))
        {
            Debug.Log("[TestInput] E — 턴 종료 호출");
            GameManager.Instance.EndTurn();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            ResourceManager.Instance.AddResearch(100);
            Debug.Log("[TestInput] R — 연구 포인트 +100");
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            ResourceManager.Instance.AddFood(50);
            Debug.Log("[TestInput] F — 식량 +50");
        }
    }
}
