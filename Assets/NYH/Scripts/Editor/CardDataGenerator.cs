using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using NYH.CoreCardSystem;

public class CardDataGenerator : EditorWindow
{
    [MenuItem("Tools/Generate All CardData")]
    public static void Generate()
    {
        string savePath = "Assets/NYH/CardData";

        if (!AssetDatabase.IsValidFolder(savePath))
        {
            AssetDatabase.CreateFolder("Assets/NYH", "CardData");
        }

        foreach (var entry in CardDataList)
        {
            string assetPath = $"{savePath}/{entry.fileName}.asset";

            CardData existing = AssetDatabase.LoadAssetAtPath<CardData>(assetPath);
            CardData asset = existing != null ? existing : ScriptableObject.CreateInstance<CardData>();

            asset.name = entry.koreanName;

            SerializedObject so = new SerializedObject(asset);
            so.FindProperty("<cardID>k__BackingField").intValue       = entry.cardID;
            so.FindProperty("<Cost>k__BackingField").intValue         = entry.cost;
            so.FindProperty("<cardType>k__BackingField").enumValueIndex = (int)entry.cardType;
            so.FindProperty("description").stringValue               = entry.description;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (existing == null)
            {
                AssetDatabase.CreateAsset(asset, assetPath);
            }
            else
            {
                EditorUtility.SetDirty(asset);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[CardDataGenerator] {CardDataList.Count}장 CardData 생성/갱신 완료");
    }

    // ── 카드 데이터 정의 ──────────────────────────────────────────────

    private struct CardEntry
    {
        public int      cardID;
        public string   fileName;    // 영어 (에셋 파일명)
        public string   koreanName;  // 한국어 (SO name / 카드 타이틀)
        public CardType cardType;
        public int      cost;
        public string   description;
    }

    private static readonly List<CardEntry> CardDataList = new List<CardEntry>
    {
        // ── 기본 지급 (1001~) ──────────────────────────────────────────
        new CardEntry { cardID=1001, fileName="Farmer_Dispatch",         koreanName="농부 소집",        cardType=CardType.Common, cost=1,
            description="농부 5명이 파견됩니다.(성별 랜덤)" },
        new CardEntry { cardID=1002, fileName="Merchant_Dispatch",       koreanName="상인 소집",        cardType=CardType.Common, cost=1,
            description="상인 5명이 파견됩니다.(성별 랜덤)" },
        new CardEntry { cardID=1003, fileName="Soldier_Dispatch",        koreanName="군인 소집",        cardType=CardType.Common, cost=1,
            description="군인 5명이 파견됩니다.(남성)(병사 사용 가능 인수이 남아있을때만 사용가능)" },
        new CardEntry { cardID=1004, fileName="Miner_Dispatch",          koreanName="채광꾼 소집",      cardType=CardType.Common, cost=1,
            description="채광꾼 5명이 파견됩니다.(성별 랜덤)" },
        new CardEntry { cardID=1005, fileName="Scout",                   koreanName="정찰",             cardType=CardType.Common, cost=2,
            description="원하는 곳에 탐험 대원을 파견합니다.(동쪽 내부의 1/3~1/4)" },
        new CardEntry { cardID=1006, fileName="Small_Temporary_Building",koreanName="소규모 임시 건설", cardType=CardType.Common, cost=10,
            description="30칸 이내의 groundmap 중 비어있는 8x8 사이즈 동을 지정해 소규모 임시를 건설합니다." },
        new CardEntry { cardID=1007, fileName="Expedition",              koreanName="원정",             cardType=CardType.Common, cost=1,
            description="원정 카드를 내려놓은 곳으로 10명의 병사를 보냅니다. 중간에 적을 만나면 그 적과는 원정합니다. 죽지 않았다면 목적지까지 계속 갑니다. 카드를 사용해도 이하인로 계속 놓입니다." },
        new CardEntry { cardID=1008, fileName="Tribal_Study_Room",       koreanName="부족 학습실",      cardType=CardType.Common, cost=1,
            description="매해 연구 포인트를 10씩 획득합니다.(1개씩만 건설 가능)(설치 되어 있는 건물 및 덱의 카드 모두 적을 새로 업그레이드)" },

        // ── 기본 지급 + 석기 등장 (1101~) ────────────────────────────
        new CardEntry { cardID=1101, fileName="Tent_Construction",       koreanName="임막 건설",        cardType=CardType.Common, cost=3,
            description="최대 인구 수를 5명 늘려주는 건물을 건설합니다.(설치 되어 있는 건물 및 덱의 카드 모두 한 벽돌집으로 자동 업그레이드)" },
        new CardEntry { cardID=1102, fileName="Tribal_Training_Ground",  koreanName="부족 훈련지 건설", cardType=CardType.Common, cost=3,
            description="병사 10명 수용할 수 있는 건물을 건설합니다.(설치 되어 있는 건물 및 덱의 카드 모두 훈련소로 자동 업그레이드)" },
        new CardEntry { cardID=1103, fileName="Barter_Market",           koreanName="물물교환소",       cardType=CardType.Common, cost=2,
            description="상인 5명 수용 가능한 물물 교환소를 건설합니다.(매해 상인 1명당 0.2씩 골드 획득)(설치 되어 있는 건물 및 덱의 카드 모두 상인 거리로 자동 업그레이드)" },
        new CardEntry { cardID=1104, fileName="Farm_Construction",       koreanName="농장 건설",        cardType=CardType.Common, cost=3,
            description="농부 5명 수용 가능한 농장을 건설합니다.(매 해 농부 1명당 식량 0.5획득)" },

        // ── 석기 단독 (2001~) ─────────────────────────────────────────
        new CardEntry { cardID=2001, fileName="Warriors_Morale",         koreanName="전사들의 전의",    cardType=CardType.Fight, cost=2,
            description="해당 임지에 있는 병력들이 무장합니다. 공격력이 1만큼, 체력이 5만큼 증가합니다.(중첩 불가)(덱의 있는 카드는 자동 업그레이드)" },

        // ── 석기+청동기+철기 등장 (2201~) ───────────────────────────
        new CardEntry { cardID=2201, fileName="Military_Conscription",   koreanName="군역 인수",        cardType=CardType.Fight, cost=3,
            description="사용 시 현재 남성 인구 지수이 병사로 고정. 매 해 자동 반영" },
        new CardEntry { cardID=2202, fileName="Elite_Squad",             koreanName="정예 부대",        cardType=CardType.Fight, cost=3,
            description="병력을 50명 이하로 유지 시 병사 공격력 50% 증가" },
        new CardEntry { cardID=2203, fileName="Emergency_Treatment",     koreanName="응급 치료",        cardType=CardType.Fight, cost=2,
            description="해당 임지에 있는 병력들의 체력을 모두 회복시킵니다." },
        new CardEntry { cardID=2204, fileName="Wall_Crumbling",          koreanName="성벽 쓰러뜨리다",  cardType=CardType.Fight, cost=1,
            description="해당 임지에 있는 병력 1명당 해당 임지의 성벽이 1%씩 쓰러집니다." },
        new CardEntry { cardID=2205, fileName="Emergency_Support",       koreanName="긴급 지지",        cardType=CardType.Fight, cost=2,
            description="병력을 10명 보충합니다.(바로 성인으로 적용)" },
        new CardEntry { cardID=2206, fileName="Focused_Decision",        koreanName="집중 결지",        cardType=CardType.Science, cost=1,
            description="연구 포인트를 20만큼 소모해 덱 상단 5장을 공개하고 그 중 과학 트리 카드 1장을 즉시 이하인로 가져오며 해당 카드의 코스트를 0으로 만듭니다.(나머지는 덱으로 복귀)" },
        new CardEntry { cardID=2207, fileName="Theory_Establishment",    koreanName="이론 정립",        cardType=CardType.Science, cost=1,
            description="연구 포인트를 10만큼 획득합니다. 현재 연구 포인트가 50 이상이면 연구포인트를 추가로 10만큼 획득합니다." },
        new CardEntry { cardID=2208, fileName="Tech_Duplication",        koreanName="기술 복제",        cardType=CardType.Science, cost=15,
            description="뽑을 카드 더미에서 카드 1장을 골라 그 카드를 복사합니다." },
        new CardEntry { cardID=2209, fileName="Observatory",             koreanName="관측소",           cardType=CardType.Science, cost=2,
            description="연구 포인트를 15만큼 획득 및 카드 1장을 드로우합니다." },
        new CardEntry { cardID=2210, fileName="Tech_Record",             koreanName="기술 기록",        cardType=CardType.Science, cost=1,
            description="연구 포인트를 10만큼 획득합니다.(만약 이번 해 이미 연구 포인트를 획득했다면 카드 1장 추가 드로우)" },
        new CardEntry { cardID=2211, fileName="Grain_Supply",            koreanName="과잉 공급",        cardType=CardType.Money, cost=0,
            description="금을 3개 획득합니다." },
        new CardEntry { cardID=2212, fileName="Large_Investment",        koreanName="대규모 투자",      cardType=CardType.Money, cost=2,
            description="이번해 동안 건물 코스트가 n% 감소합니다.(이하인에 있는 경제 트리 카드 1장당 모든 건물의 건설 코스트가 이번 해 10%씩 감소. 최대 80% 감소)" },
        new CardEntry { cardID=2213, fileName="Capital_Utilization",     koreanName="자본 이용",        cardType=CardType.Money, cost=1,
            description="금을 n 획득합니다.(이하인에 있는 경제 트리 카드 1장당 금 5씩 획득)" },
        new CardEntry { cardID=2214, fileName="Grain_Trading",           koreanName="곡물 환전",        cardType=CardType.Money, cost=1,
            description="카드 사용 시 해당 해 보유 한 금의 10%만큼 식량을 추가로 획득" },
        new CardEntry { cardID=2215, fileName="Economic_Momentum",       koreanName="경제 대도약",      cardType=CardType.Money, cost=3,
            description="이번 해에 사용하는 모든 경제 카드의 효과가 2배. 단 다음 해에는 전투 및 과학 카드를 사용할 수 없음" },
        new CardEntry { cardID=2216, fileName="Hand_Exchange",           koreanName="패 교환",          cardType=CardType.Normal, cost=1,
            description="이하인 2장을 랜덤으로 버리고 2장을 드로우합니다." },
        new CardEntry { cardID=2217, fileName="Problem_Solving",         koreanName="문제 해결",        cardType=CardType.Normal, cost=1,
            description="덱에 있는 카드 1장을 선택해 소멸시킵니다." },

        // ── 청동기 단독 (3001~) ───────────────────────────────────────
        new CardEntry { cardID=3001, fileName="Brick_House_Construction",koreanName="한 벽돌집 건설",   cardType=CardType.Common, cost=3,
            description="최대 인구 수를 7명 늘려주는 건물을 건설합니다.(설치 되어 있는 건물 및 덱의 카드 모두 이가집으로 자동 업그레이드)" },
        new CardEntry { cardID=3002, fileName="Training_Center",         koreanName="훈련소 건설",      cardType=CardType.Common, cost=3,
            description="병사 15명 수용할 수 있는 건물을 건설합니다.(설치 되어 있는 건물 및 덱의 카드 모두 병사 건물로 자동 업그레이드)" },
        new CardEntry { cardID=3003, fileName="Merchant_Street",         koreanName="상인 거리",        cardType=CardType.Common, cost=2,
            description="상인 5명 수용 가능한 상인 거리를 건설합니다.(매해 상인 1명당 0.4씩 골드 획득)(설치 되어 있는 건물 및 덱의 카드 모두 무역소로 자동 업그레이드)" },
        new CardEntry { cardID=3004, fileName="Bronze_Armament",         koreanName="청동계 전의",      cardType=CardType.Fight, cost=4,
            description="해당 임지에 있는 병력들이 무장합니다. 공격력이 2만큼, 체력이 10만큼 증가합니다.(중첩 불가)(덱에 있는 카드는 자동 업그레이드)" },

        // ── 청동기+철기 등장 (3101~) ─────────────────────────────────
        new CardEntry { cardID=3101, fileName="Slaughter",               koreanName="장살",             cardType=CardType.Fight, cost=1,
            description="50명을 초과한 만큼 병력을 모두 처치. 처치한 병력 10명당 카드 1장 드로우" },
        new CardEntry { cardID=3102, fileName="Golden_Ratio",            koreanName="황금 비율",        cardType=CardType.Fight, cost=2,
            description="해당 카드를 사용할 때 병력이 정확히 50명이라면 이번 게임 병사 공격력이 영구적으로 10% 증가" },
        new CardEntry { cardID=3103, fileName="Research_Acceleration",   koreanName="연구 가속",        cardType=CardType.Science, cost=0,
            description="연구 포인트를 30만큼 소모해 다음에 사용하는 스킬 카드 1장의 효과를 한 번 더 발동합니다." },
        new CardEntry { cardID=3104, fileName="Investment",              koreanName="투자",             cardType=CardType.Money, cost=1,
            description="카드를 1장 추가로 드로우합니다.(사용 시 코스트가 1 증가한 후 이하인로 다시 돌아와)(해 종료 시 코스트 원상복귀)" },
        new CardEntry { cardID=3105, fileName="Market_Life",             koreanName="시장의 생명",      cardType=CardType.Money, cost=2,
            description="해당 해 식량이 20개 이상이면 카드를 2장 추가로 드로우합니다.(카드를 추가로 드로우 후 해의 해당 해의 식량 소모가 10%만큼 증가)" },
        new CardEntry { cardID=3106, fileName="Market",                  koreanName="시장",             cardType=CardType.Money, cost=0,
            description="식량을 5개 획득합니다." },
        new CardEntry { cardID=3107, fileName="Hegemony_Declaration",    koreanName="패권의 선언",      cardType=CardType.Normal, cost=3,
            description="우호도 80 이상인 나라가 있다면 카드를 추가로 획득. 적을 전투 시 해당 우호도 0으로 초기화 및 효과 -1" },
        new CardEntry { cardID=3108, fileName="Tradition_Inheritance",   koreanName="전통의 계칙",      cardType=CardType.Normal, cost=2,
            description="지금까지 사용한 영구 카드 수만큼 매 해 n장씩 추가로 드로우합니다.(최대 3장)" },
        new CardEntry { cardID=3109, fileName="Trade_Route",             koreanName="국역로",           cardType=CardType.Normal, cost=3,
            description="미니맵에 국역로를 지정. 상인 효과 미정" },
        new CardEntry { cardID=3110, fileName="Gift_Tribute",            koreanName="선물 공인",        cardType=CardType.Normal, cost=5,
            description="카드를 내려놓은 다른 문명의 우호도를 20만큼 올립니다." },
        new CardEntry { cardID=3111, fileName="Diplomatic_Protocol",     koreanName="외교 관례",        cardType=CardType.Normal, cost=2,
            description="선물 공인 카드의 우호도 증가되었을 영구히 증가시킵니다." },
        new CardEntry { cardID=3112, fileName="Continuous_Activity",     koreanName="연속 활동",        cardType=CardType.Normal, cost=2,
            description="카드를 5장 사용할 때마다 카드 1장을 드로우합니다." },
        new CardEntry { cardID=3113, fileName="Mine",                    koreanName="성광",             cardType=CardType.Normal, cost=10,
            description="성광하는 전 달 한명 별 철 을 1개 획득합니다.(소규모 임지 2칸 이내에 철광이 있을때 사용 가능)(철기 이후부터 사용 가능)" },

        // ── 철기 단독 (4001~) ─────────────────────────────────────────
        new CardEntry { cardID=4001, fileName="Two_Story_House",         koreanName="이가집 건설",      cardType=CardType.Common, cost=3,
            description="최대 인구 수를 10명 늘려주는 건물을 건설합니다." },
        new CardEntry { cardID=4002, fileName="Military_Building",       koreanName="병사 건물 건설",   cardType=CardType.Common, cost=3,
            description="병사 20명 수용할 수 있는 건물을 건설합니다." },
        new CardEntry { cardID=4003, fileName="Trade_Post",              koreanName="무역소",           cardType=CardType.Common, cost=2,
            description="상인 5명 수용 가능한 무역소를 건설합니다.(매해 상인 1명당 0.6씩 골드 획득)" },
        new CardEntry { cardID=4004, fileName="Iron_Armament",           koreanName="철계 전의",        cardType=CardType.Fight, cost=6,
            description="해당 임지에 있는 병력들이 무장합니다. 공격력이 3만큼, 체력이 15만큼 증가합니다.(중첩 불가)(덱에 있는 카드는 자동 업그레이드)" },
        new CardEntry { cardID=4005, fileName="Weapons_Lab",             koreanName="병기 연구소",      cardType=CardType.Science, cost=3,
            description="설치 후 매 해 현재 연구 포인트 50당 전체 병사 공격력 및 체력 +1% 누적 적용(단리 적용)(연구 포인트 100 달성 시 사용가능)" },
        new CardEntry { cardID=4006, fileName="Enlightenment",           koreanName="계시",             cardType=CardType.Science, cost=2,
            description="연구 포인트를 50만큼 소모해 덱에서 카드를 3장 드로우합니다. 현재 이하인의 모든 카드 코스트가 이번 해 0이 됩니다.(이번 해 더 이상 카드 드로우 불가)" },

        // ── 플레이 부가 (9001~) ───────────────────────────────────────
        new CardEntry { cardID=9001, fileName="Library",                 koreanName="도서관",           cardType=CardType.Common, cost=2,
            description="매해 연구 포인트를 20씩 획득합니다.(1개씩만 건설 가능)(설치 되어 있는 건물 및 덱의 카드 서당으로 모두 자동 업그레이드)" },
        new CardEntry { cardID=9002, fileName="Village_School",          koreanName="서당",             cardType=CardType.Common, cost=3,
            description="매해 연구 포인트를 30씩 획득합니다.(1개씩만 건설 가능)" },
    };
}
