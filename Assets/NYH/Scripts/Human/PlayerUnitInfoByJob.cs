using UnityEngine;


[CreateAssetMenu(fileName = "UnitInfo", menuName = "Unit/UnitInfoByJob")]
public class PlayerUnitInfoByJob : ScriptableObject
{
    public Job job;
    public int age = 1;
    public int maxHealth = 1;
    public int attackPower = 1;
}
