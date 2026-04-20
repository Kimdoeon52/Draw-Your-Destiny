using UnityEngine;

using Base;

public class Wizzard : HumanBase, IFightable
{
    [Header("최대체력")]
    [SerializeField] private int maxHealth;
    [Header("현재체력")]
    [SerializeField] int currentHealth;
    //[Header("공격력")]
    //[SerializeField] int attackPower;
    protected override void OnEnable()
    {//마법사 특 몸이 뒤지게 약함. 하지만 공격력은 매우 강함.
        base.OnEnable();
        maxHealth = 50;
        //attackPower = 200;
        SetupHealth();
        unitTypeBase = UnitTypeBase.Wizzard;
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