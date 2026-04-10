using UnityEngine;

public class Farmer : HumanBase
{
    protected override void OnEnable()
    {
        base.OnEnable();
        canMoveJobBase = CanMoveJobBase.CannotMoveJob;
    }
}
