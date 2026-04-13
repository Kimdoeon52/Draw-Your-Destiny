using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 전체 상태를 관리하는 싱글톤 매니저.
/// 현재 시대, 영주성, 유닛 등 플레이어 소유 엔티티를 총괄한다.
/// </summary>
public class PlayerManager : PersistentSingleton<PlayerManager>
{
    #region ── Inspector: 플레이어 상태 ──

    [Header("플레이어 상태")]
    [SerializeField] private Era currentEra = Era.Stone;

    /// <summary>현재 시대를 반환한다.</summary>
    public Era CurrentEra => currentEra;

    #endregion

    #region ── Inspector: 플레이어 엔티티 ──

    [Header("플레이어 엔티티")]
    [SerializeField] private PlayerLordCastle playerCastle;

    /// <summary>플레이어의 영주성을 반환한다.</summary>
    public PlayerLordCastle PlayerCastle => playerCastle;

    #endregion

    #region ── 미구현 (주석 처리된 기능) ──

    // ── 유닛 리스트 (구현 시 주석 해제) ──
    // private List<HumanUnit> playerUnits = new List<HumanUnit>();
    // public IReadOnlyList<HumanUnit> PlayerUnits => playerUnits;

    // ── 이벤트 (UI·다른 시스템에서 구독) ──
    // public event Action<Era> OnEraChanged;
    // public event Action<HumanUnit> OnUnitSpawned;
    // public event Action<HumanUnit> OnUnitDied;

    #endregion

    // =====================================================================
    #region ── Unity 생명주기 ──
    // =====================================================================

    protected override void Awake()
    {
        base.Awake();
    }

    #endregion

    // =====================================================================
    #region ── 공개 API ──
    // =====================================================================

    /// <summary>
    /// 초기 영주성을 설정한다.
    /// </summary>
    /// <param name="castle">할당할 PlayerLordCastle 인스턴스.</param>
    public void Initialize(PlayerLordCastle castle)
    {
        playerCastle = castle;
    }

    /// <summary>
    /// 유닛을 생성한다.
    /// </summary>
    /// <param name="unitInfo">직업별 유닛 정보.</param>
    /// <param name="spawnPosition">생성 월드 좌표.</param>
    /// <remarks>
    /// TODO: HumanPool을 사용하여 유닛을 생성하고 RegisterUnit()을 호출한다.
    /// 기존 GameManager.GenerateHumans 로직을 이쪽으로 이관 예정.
    /// </remarks>
    public void SpawnUnit(PlayerUnitInfoByJob unitInfo, Vector3 spawnPosition)
    {
        // TODO: HumanPool을 사용하여 유닛 생성 및 RegisterUnit 호출
    }

    #endregion

    // =====================================================================
    #region ── 미구현 메서드 (주석 처리) ──
    // =====================================================================

    // /// <summary>시대를 업그레이드한다.</summary>
    // public void SetEra(Era newEra)
    // {
    //     if (currentEra == newEra) return;
    //     currentEra = newEra;
    //     Debug.Log($"[PlayerManager] Era upgraded to: {newEra}");
    //     OnEraChanged?.Invoke(currentEra);
    // }

    // /// <summary>유닛을 플레이어 소유 리스트에 등록한다.</summary>
    // public void RegisterUnit(HumanUnit unit)
    // {
    //     if (!playerUnits.Contains(unit))
    //     {
    //         playerUnits.Add(unit);
    //         OnUnitSpawned?.Invoke(unit);
    //     }
    // }

    // /// <summary>유닛을 플레이어 소유 리스트에서 제거한다.</summary>
    // public void UnregisterUnit(HumanUnit unit)
    // {
    //     if (playerUnits.Remove(unit))
    //     {
    //         OnUnitDied?.Invoke(unit);
    //     }
    // }

    #endregion
}
