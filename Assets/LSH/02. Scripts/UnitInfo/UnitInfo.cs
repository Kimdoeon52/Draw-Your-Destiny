using UnityEngine;
public enum Gender
{
    Male,
    Female
}
public enum AgeGroup
{
    Baby,
    Adult,
    Old
}
public enum Job
{
    Farmer,
    Soldier,
    Miner
}
[CreateAssetMenu(fileName = "UnitInfo", menuName = "Unit/UnitInfo")]
public class UnitInfo : ScriptableObject
{
    public int adultStartAge = 3;
    public int babyStartAge = 0;
    public int maxHealth = 1;
    public int attackPower = 1;
    public float startNaturalDeathChance = 0f;
    public float naturalDeathIncreasePerTurn = 0.01f;
}
