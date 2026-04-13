using UnityEngine;

public class EnemyARockWarrior : EnemyUnitBase
{
    [Header("최대체력")]
    [SerializeField] private int maxHealth;
    [Header("현재체력")]
    [SerializeField] private int currentHealth;

    protected override void OnEnable()
    {//무난한 일반 병사
        base.OnEnable();
        maxHealth = 150;
        attackPower = 20;
        moveSpeed = 3f;
        SetupHealth();
        enemyUnitTypeBase = EnemyUnitTypeBase.RockWarrior;
    }
    protected override void Update()
    {
        base.Update();
        if (currentHealth <= 0)
        {
            Dead();
        }
        if (Input.GetKeyDown(KeyCode.S)) //임시 공격받았음
        {
            TakeDamage(10);
        }
    }
    public void Attack(int targetID)
    {

    }

    public void SetupHealth()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log("공격 받음!");
    }
    protected override void Dead()
    {
        base.Dead();
    }
}
