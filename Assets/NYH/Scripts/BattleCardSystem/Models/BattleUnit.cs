namespace NYH.BattleCardSystem
{
    using UnityEngine;

    public enum BattleTeam
    {
        Player,
        Enemy,
    }

    /*
     * BattleUnit
     *
     * 역할:
     * - 전투에 참여하는 유닛 1개의 체력, 공격력, 속도, 팀, 그리드 좌표를 보관합니다.
     * - BattleBoardSystem에 자신을 등록하고, 피격/이동 시 현재 상태를 갱신합니다.
     *
     * 인스펙터에서 넣는 것:
     * - Team: 플레이어/적 구분
     * - Max Health: 최대 체력
     * - Attack Power: 기본 공격력
     * - Speed: 이동 카드 사용 시 추가 이동량 계산용 속도
     * - Grid Position: 전투 시작 위치
     *
     * 사용하는 법:
     * - 전투 씬의 유닛 오브젝트마다 붙입니다.
     * - BattleBoardSystem이 있어야 정상 등록됩니다.
     */
    public class BattleUnit : MonoBehaviour
    {
        [SerializeField] private string unitId;
        [SerializeField] private BattleTeam team;
        [SerializeField] private int maxHealth = 10;
        [SerializeField] private int attackPower = 1;
        [SerializeField] private int speed = 1;
        [SerializeField] private Vector2Int gridPosition;

        public string UnitId => unitId;
        public BattleTeam Team => team;
        public int MaxHealth => maxHealth;
        public int CurrentHealth { get; private set; }
        public int AttackPower => attackPower;
        public int CurrentAttackPower => Mathf.Max(0, attackPower + attackPowerModifier);
        public int Speed => speed;
        public int CurrentSpeed => Mathf.Max(0, speed + speedModifier);
        public Vector2Int GridPosition => gridPosition;
        public bool IsAlive => CurrentHealth > 0;
        public bool IsStunned => isStunned;
        public bool IsDisarmed => isDisarmed;

        private int attackPowerModifier;
        private int speedModifier;
        private bool isStunned;
        private bool isDisarmed;

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        private void OnEnable()
        {
            if (BattleBoardSystem.Instance != null)
            {
                BattleBoardSystem.Instance.RegisterUnit(this, gridPosition);
            }
        }

        private void OnDisable()
        {
            if (BattleBoardSystem.Instance != null)
            {
                BattleBoardSystem.Instance.UnregisterUnit(this);
            }
        }

        public void Initialize(Vector2Int startPosition, int health)
        {
            gridPosition = startPosition;
            CurrentHealth = Mathf.Clamp(health, 0, maxHealth);
        }

        public void SetGridPosition(Vector2Int newPosition)
        {
            gridPosition = newPosition;
        }

        public void ModifyAttackPower(int amount)
        {
            attackPowerModifier += amount;
        }

        public void ModifySpeed(int amount)
        {
            speedModifier += amount;
        }

        public void SetStunned(bool value)
        {
            isStunned = value;
        }

        public void SetDisarmed(bool value)
        {
            isDisarmed = value;
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || !IsAlive)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        }

        public void TakePercentDamage(float ratio)
        {
            if (!IsAlive)
            {
                return;
            }

            int damage = Mathf.Max(1, Mathf.FloorToInt(MaxHealth * Mathf.Max(0f, ratio)));
            TakeDamage(damage);
        }
    }
}
