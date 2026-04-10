using UnityEngine;
using Base;

public class Farmer : HumanBase
{
    protected override void OnEnable()
    {
        base.OnEnable();
        canMoveJobBase = CanMoveJobBase.CannotMoveJob;
    }
}
