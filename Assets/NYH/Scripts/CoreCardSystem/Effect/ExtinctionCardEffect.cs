using NYH.CoreCardSystem;
using UnityEngine;

public class ExtinctionCardEffect : Effect
{
    [Header("효과 소멸 또는 능력치 활성화 여부")]
    [SerializeField]private bool ExtinctionCard;

    public override GameAction GetGameAction()
    {
        // TargetCard는 여기서 알 수 없으므로 null로 전달하고, CardSystem에서 처리하게 합니다.
        return new ExtinctionCardGA(ExtinctionCard);
    }
}
