using Base;
using UnityEngine;

public class Healer : HumanBase ,IFightable
{
    [Header("최대체력")]
    [SerializeField] private int maxHealth;
    [Header("현재체력")]
    [SerializeField] int currentHealth;
    //[Header("공격력")]
    //[SerializeField] int attackPower;
    protected override void OnEnable()
    {//힐러특 체력과 공격력이 약함. 하지만 힐링함.
        base.OnEnable();
        maxHealth = 100;
        //attackPower = 10;
        SetupHealth();
        unitTypeBase = UnitTypeBase.Healer;
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
