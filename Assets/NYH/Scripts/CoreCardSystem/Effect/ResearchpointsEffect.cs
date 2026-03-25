using NYH.CoreCardSystem;
using UnityEngine;

public class ResearchpointsEffect : Effect
{
    [Header("증가 시킬 연구 포인트")]
    [SerializeField] private int ResarchPointGA;

    public override GameAction GetGameAction()
    {
        return new ResearchpointsGA(ResarchPointGA);
    }
}