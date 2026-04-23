namespace NYH.BattleCardSystem
{
    using Cysharp.Threading.Tasks;
    using TMPro;
    using UnityEngine;

    /*
     * BattleUnitAIProfile
     *
     * 역할:
     * - 개별 적 유닛의 AI 전략, 이동량, 공격 사거리를 인스펙터에서 덮어씁니다.
     * - 간단한 데미지 텍스트 표시도 함께 담당합니다.
     */
    public class BattleUnitAIProfile : MonoBehaviour
    {
        [Header("AI")]
        [SerializeField] private AIBehaviorStrategySO strategy;
        [SerializeField] private int moveBudget = 3;
        [SerializeField] private int attackRange = 1;

        [Header("Presentation")]
        [SerializeField] private TMP_Text damageText;
        [SerializeField] private float damageTextDuration = 0.8f;

        public AIBehaviorStrategySO Strategy => strategy;
        public int MoveBudget => Mathf.Max(0, moveBudget);
        public int AttackRange => Mathf.Max(1, attackRange);

        private void Awake()
        {
            if (damageText != null)
            {
                damageText.gameObject.SetActive(false);
            }
        }

        // 유닛 위 데미지 텍스트를 잠시 표시합니다.
        public void ShowDamage(int damageAmount)
        {
            Debug.Log($"[BattleUnitAIProfile] 데미지 텍스트 요청 unit={name}, amount={damageAmount}, hasDamageText={(damageText != null)}");
            if (damageText == null || damageAmount <= 0)
            {
                return;
            }

            ShowDamageAsync(damageAmount).Forget();
        }

        private async UniTaskVoid ShowDamageAsync(int damageAmount)
        {
            damageText.text = $"-{damageAmount}";
            damageText.gameObject.SetActive(true);
            await UniTask.Delay(Mathf.RoundToInt(damageTextDuration * 1000f));
            if (damageText != null)
            {
                damageText.gameObject.SetActive(false);
            }
        }
    }
}
