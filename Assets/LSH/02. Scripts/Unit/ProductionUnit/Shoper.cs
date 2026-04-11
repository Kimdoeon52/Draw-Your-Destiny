using Base;
using UnityEngine;

public class Shoper : HumanBase
{
    protected override void OnEnable()
    {
        base.OnEnable();
        canMoveJobBase = CanMoveJobBase.CannotMoveJob;
        unitTypeBase = UnitTypeBase.Farmer;
    }
}
