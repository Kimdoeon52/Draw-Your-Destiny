using UnityEngine;

public class RockWarrior : HumanBase
{
    private int maxHealth;

    protected override void OnEnable()
    {
        base.OnEnable();
        health = 150;
        maxHealth = health;
        attackPower = 20;
    }
}
