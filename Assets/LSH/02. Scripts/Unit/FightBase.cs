using UnityEngine;

public class FightBase : MonoBehaviour
{
    [Header("최대체력")]
    [SerializeField] int maxHealth; // 최대 체력 설정
    [Header("현재체력")]
    [SerializeField] int currentHealth; // 현재 체력
    [Header("공격력")]
    [SerializeField] int attackPower; // 공격력 설정
    protected virtual void SetupHealth() // 체력 설정 함수
    {
        currentHealth = maxHealth; // 초기 체력은 최대 체력으로 설정
    }

    protected virtual void TakeDamage(int damage) // 데미지 처리 함수
    {
        currentHealth -= damage; // 데미지만큼 체력 감소
        if (currentHealth <= 0) // 체력이 0 이하가 되면 사망 처리
        {
            HumanPool.Instance.ReturnHuman(gameObject); // 객체 풀로 반환하여 재사용
        }
    }
}
