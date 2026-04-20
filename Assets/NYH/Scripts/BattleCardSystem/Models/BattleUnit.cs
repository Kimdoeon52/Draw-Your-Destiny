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
        [SerializeField] private Transform visualRoot;
        [SerializeField] private bool enableGridAlignmentDebug;

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
            ApplyGridWorldPosition();
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
            ApplyGridWorldPosition();
        }

        public void SetGridPosition(Vector2Int newPosition)
        {
            gridPosition = newPosition;
        }

        public void ApplyGridWorldPosition()
        {
            transform.position = GetWorldPositionForGrid(gridPosition, transform.position.z);

            if (enableGridAlignmentDebug)
            {
                LogGridAlignment("ApplyGridWorldPosition");
            }
        }

        public void SnapToGridCenter()
        {
            ApplyGridWorldPosition();
        }

        public Vector3 GetGridWorldPosition()
        {
            return GetWorldPositionForGrid(gridPosition, transform.position.z);
        }

        public static Vector2Int GetGridPositionForWorld(Vector3 worldPosition)
        {
            if (BattleGridCoordinateService.Instance.TryGetCell(worldPosition, out Vector2Int cell))
            {
                return cell;
            }

            return new Vector2Int(int.MinValue, int.MinValue);
        }

        public static Vector2Int NormalizeGridPosition(Vector2Int gridPosition)
        {
            return gridPosition;
        }

        public static Vector3 GetWorldPositionForGrid(Vector2Int position, float z)
        {
            if (BattleGridCoordinateService.Instance.TryGetWorldCenter(position, out Vector3 world))
            {
                world.z = z;
                return world;
            }

            return new Vector3(position.x, position.y, z);
        }

        public void LogGridAlignment(string caller = "BattleUnit")
        {
            Vector3 rootPosition = transform.position;
            Vector3 expectedPosition = GetGridWorldPosition();
            Vector3 visualLocalPosition = visualRoot != null ? visualRoot.localPosition : Vector3.zero;

            Debug.Log(
                $"[BattleUnitGrid] caller={caller}, unit={name}, team={team}, grid={gridPosition}, " +
                $"rootWorld={rootPosition}, expectedWorld={expectedPosition}, " +
                $"visualLocal={visualLocalPosition}, hasVisualRoot={(visualRoot != null)}");
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
            aiProfile?.ShowDamage(amount);
            hitFlash?.Play();

            if (CurrentHealth == 0)
            {
                HandleDeath();
            }
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
