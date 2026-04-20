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
     * Holds the runtime battle state for a single unit.
     * Tracks health, attack, speed, team, and grid position.
     *
     * Inspector fields:
     * - Team: Player or Enemy
     * - Max Health: starting and maximum HP
     * - Attack Power: base attack value
     * - Speed: base move contribution for battle cards
     * - Grid Position: starting tile in the battle grid
     *
     * Usage:
     * - Attach to each unit GameObject used in battle.
     * - Registers itself with BattleBoardSystem while enabled.
     */
    public class BattleUnit : MonoBehaviour
    {
        private const bool EnableDamageDebug = false;

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
        private bool isDying;
        private BattleUnitAIProfile aiProfile;
        private BattleUnitHitFlash hitFlash;

        private void Awake()
        {
            CurrentHealth = maxHealth;
            aiProfile = GetComponent<BattleUnitAIProfile>();
            hitFlash = GetComponent<BattleUnitHitFlash>();
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
                /*Debug.Log($"[BattleUnit] 데미지 처리 건너뜀 unit={name}, amount={amount}, isAlive={IsAlive}");*/
                return;
            }

            int beforeHealth = CurrentHealth;
            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            /*Debug.Log($"[BattleUnit] 데미지 처리 unit={name}, amount={amount}," +
                $" hp={beforeHealth}->{CurrentHealth}, hasAIProfile={(aiProfile != null)}," +
                $" hasHitFlash={(hitFlash != null)}");*/
            aiProfile?.ShowDamage(amount);
            hitFlash?.Play();

            if (CurrentHealth == 0)
            {
                HandleDeath();
            }

            //if (EnableDamageDebug)
            //{
            //    if (CurrentHealth == 0)
            //    {
            //        Debug.Log(
            //            $"[BattleUnitDebug] ??壤? unit={name}, team={team}, grid={gridPosition}");
            //    }
            //}
        }

        private void HandleDeath()
        {
            if (isDying)
            {
                return;
            }

            isDying = true;
            if (BattleBoardSystem.Instance != null)
            {
                BattleBoardSystem.Instance.UnregisterUnit(this);
            }

            Destroy(gameObject);
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

