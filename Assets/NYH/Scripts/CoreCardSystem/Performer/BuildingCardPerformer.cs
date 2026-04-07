namespace NYH.CoreCardSystem
{
    using System.Collections;
    using UnityEngine;

    /*
     * BuildingCardPerformer
     *
     * - 설치 카드 및 건물 배치 처리
     *
     * 역할:
     * - 설치 카드의 배치 가능 여부 검사와 실제 건물 설치를 전담합니다.
     * - 카드 시스템과 KDU 타일맵/건물 시스템 사이의 연결 지점입니다.
     *
     * 여기에 넣는 것:
     * - InstallBuildingEffect 확인
     * - TileMapManager.CanPlace 검사
     * - PlayBuildingGA 실행
     * - 카드 사용 후 건물 설치를 예약하는 로직 등
     *
     * 여기에 넣지 않는 것:
     * - 일반 카드 드로우/버리기/소멸 처리
     * - 골드/식량/연구 같은 자원 변경
     *
     * 사용하는 법:
     * - CardView 또는 CardSystem에서 설치 확정 좌표가 정해지면
     *   TryQueuePlacementCard(card, tilePos, isTargetingMode)를 호출합니다.
     * - 실제 PlayBuildingGA는 CardSystem.Perform()에서 이 클래스의 Perform()으로 위임합니다.
     */
    
    /// <summary>
    /// 설치 카드 검증과 실제 건물 배치만 담당하는 스크립트 부분 
    /// </summary>
    public class BuildingCardPerformer
    {
        public bool TryQueuePlacementCard(Card sourceCard, Vector3Int targetPos, bool isTargetingMode)
        {
            if (sourceCard == null)
            {
                Debug.LogWarning("[_CardSystem] 설치할 카드가 없습니다.");
                return false;
            }

            InstallBuildingEffect installEffect = null;
            if (sourceCard.Effects != null)
            {
                foreach (var effect in sourceCard.Effects)
                {
                    if (effect is InstallBuildingEffect foundEffect)
                    {
                        installEffect = foundEffect;
                        break;
                    }
                }
            }

            if (installEffect == null || installEffect.buildingData == null)
            {
                Debug.LogWarning($"[_CardSystem] {sourceCard.Title} 카드에 설치할 건물 데이터가 없습니다.");
                return false;
            }

            if (!isTargetingMode)
            {
                Debug.LogWarning($"[_CardSystem] {sourceCard.Title} 건물 카드는 가운데 배치 모드에서만 사용할 수 있습니다.");
                return false;
            }

            if (TileMapManager.Instance == null)
            {
                Debug.LogError("[_CardSystem] TileMapManager가 없어 건물을 설치할 수 없습니다.");
                return false;
            }

            if (!TileMapManager.Instance.CanPlace(targetPos, installEffect.buildingData))
            {
                Debug.LogWarning($"[_CardSystem] {installEffect.buildingData.buildingName} 건물을 {targetPos}에 설치할 수 없습니다.");
                return false;
            }

            ActionSystem.Instance.Perform(
                new PlayCardGA(sourceCard),
                () => ActionSystem.Instance.Perform(new PlayBuildingGA(sourceCard, installEffect.buildingData, targetPos, isTargetingMode)));
            return true;
        }

        public IEnumerator Perform(PlayBuildingGA playBuildingGA)
        {
            if (playBuildingGA == null || playBuildingGA.Data == null)
            {
                Debug.LogWarning("[_CardSystem] 설치할 건물 데이터가 없어 배치를 중단합니다.");
                yield break;
            }

            if (!playBuildingGA.IsTargetingMode)
            {
                Debug.LogWarning($"[_CardSystem] {playBuildingGA.SourceCard?.Title} 건물 카드는 가운데 배치 모드가 아니어서 설치를 취소합니다.");
                yield break;
            }

            if (TileMapManager.Instance == null)
            {
                Debug.LogError("[_CardSystem] TileMapManager가 없어 건물을 설치할 수 없습니다.");
                yield break;
            }

            if (!TileMapManager.Instance.CanPlace(playBuildingGA.TargetPos, playBuildingGA.Data))
            {
                Debug.LogWarning($"[_CardSystem] {playBuildingGA.Data.buildingName} 건물을 {playBuildingGA.TargetPos}에 설치할 수 없습니다.");
                yield break;
            }

            TileMapManager.Instance.PlaceBuilding(playBuildingGA.TargetPos, playBuildingGA.Data);
            yield return null;
        }
    }
}
