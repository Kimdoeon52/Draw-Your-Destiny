using UnityEngine;
using Base;
public class SuperUnit : HumanBase, IFightable
{
    [Header("최대체력")]
    [SerializeField] private int maxHealth;
    [Header("현재체력")]
    [SerializeField] int currentHealth;
    //[Header("공격력")]
    //[SerializeField] int attackPower;
    protected override void OnEnable()
    {//그저 미친체력깡패 대신 이동속도가 매우 느리다
        base.OnEnable();
        maxHealth = 500;
        //attackPower = 50;
        moveSpeed = 1f;
        SetupHealth();
        unitTypeBase = UnitTypeBase.SuperUnit;
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
