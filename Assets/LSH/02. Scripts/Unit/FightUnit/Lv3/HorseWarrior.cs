using UnityEngine;
using Base;
public class HorseWarrior : HumanBase, IFightable
{
    [Header("최대체력")]
    [SerializeField] private int maxHealth;
    [Header("현재체력")]
    [SerializeField] int currentHealth;
    [Header("공격력")]
    [SerializeField] int attackPower;
    protected override void OnEnable()
    {//체력과 공격력이 높고 이동속도도 빠름.
        base.OnEnable();
        maxHealth = 250;
        attackPower = 40;
        moveSpeed = 5f;
        SetupHealth();
        unitTypeBase = UnitTypeBase.HorseWarrior;
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
