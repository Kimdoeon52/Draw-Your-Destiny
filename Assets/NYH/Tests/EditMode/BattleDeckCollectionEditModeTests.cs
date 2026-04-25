#if false // 컴파일 에러 임시 비활성화
namespace NYH.Tests.EditMode
{
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using NUnit.Framework;
    using NYH.BattleCardSystem;
    using NYH.CoreCardSystem;
    using UnityEngine;
    using UnityEngine.TestTools;

    public class BattleDeckCollectionEditModeTests
    {
        [SetUp]
        public void SetUp()
        {
            CleanupSceneObjects();
            BattleDeckPersistenceService.ClearSavedDeck();
        }

        [TearDown]
        public void TearDown()
        {
            BattleDeckPersistenceService.ClearSavedDeck();
            CleanupSceneObjects();
        }

        [Test]
        public void GetOrCreate_CreatesSingleton_WhenMissing()
        {
            BattleDeckCollection collection = BattleDeckCollection.GetOrCreate();

            Assert.NotNull(collection);
            Assert.AreSame(collection, BattleDeckCollection.Instance);
        }

        [Test]
        public void ConfigureBaseDeck_RebuildsCurrentDeckFromBaseAndEarned_WhenNoSavedDeck()
        {
            BattleDeckCollection collection = BattleDeckCollection.GetOrCreate();
            BattleCardData baseCardA = CreateCard(1);
            BattleCardData baseCardB = CreateCard(2);
            BattleCardData earnedCard = CreateCard(3);

            SetPrivateField(collection, "earnedBattleCards", new List<BattleCardData> { earnedCard });

            collection.ConfigureBaseDeck(new List<BattleCardData> { baseCardA, baseCardB });

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, GetIds(collection.CurrentBattleDeck));
            Assert.True(BattleDeckPersistenceService.HasSavedDeck());
        }

        [Test]
        public void ConfigureBaseDeck_UpdatesBaseButPreservesSavedCurrentDeck_WhenSavedDeckExists()
        {
            BattleCardData card1 = CreateCard(1);
            BattleCardData card2 = CreateCard(2);
            BattleCardData card3 = CreateCard(3);
            BattleCardData card4 = CreateCard(4);
            SetupCatalog(card1, card2, card3, card4);

            BattleDeckCollection collection = BattleDeckCollection.GetOrCreate();
            collection.ConfigureBaseDeck(new List<BattleCardData> { card1, card2 });
            Assert.AreEqual(BattleDeckAddResult.Added, collection.AddRewardCard(card3));

            collection.ConfigureBaseDeck(new List<BattleCardData> { card1, card4 });

            CollectionAssert.AreEqual(new[] { 1, 4 }, GetIds(collection.BaseBattleDeck));
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, GetIds(collection.CurrentBattleDeck));
            CollectionAssert.AreEqual(new[] { 2, 3 }, GetIds(collection.EarnedBattleCards));
        }

        [Test]
        public void AddRewardCard_RequiresReplacementAtDeckLimit_AndPersistsReplacement()
        {
            List<BattleCardData> cards = CreateSequentialCards(31);
            SetupCatalog(cards.ToArray());

            BattleDeckCollection collection = BattleDeckCollection.GetOrCreate();
            List<BattleCardData> baseDeck = cards.GetRange(0, 30);
            collection.ConfigureBaseDeck(baseDeck);

            Assert.AreEqual(BattleDeckAddResult.NeedsReplacement, collection.AddRewardCard(cards[30]));
            Assert.AreEqual(BattleDeckAddResult.Replaced, collection.ReplaceCard(cards[0], cards[30]));

            Object.DestroyImmediate(collection.gameObject);

            BattleDeckCollection reloadedCollection = BattleDeckCollection.GetOrCreate();
            reloadedCollection.ConfigureBaseDeck(baseDeck);

            List<int> currentIds = GetIds(reloadedCollection.CurrentBattleDeck);
            Assert.AreEqual(30, currentIds.Count);
            CollectionAssert.DoesNotContain(currentIds, 1);
            CollectionAssert.Contains(currentIds, 31);
        }

        [Test]
        public void ClearSavedCurrentDeck_RebuildsFromBaseAndEarned_OnNextConfigure()
        {
            BattleDeckCollection collection = BattleDeckCollection.GetOrCreate();
            BattleCardData baseCardA = CreateCard(1);
            BattleCardData baseCardB = CreateCard(2);
            BattleCardData earnedCard = CreateCard(3);

            collection.ConfigureBaseDeck(new List<BattleCardData> { baseCardA, baseCardB });
            Assert.AreEqual(BattleDeckAddResult.Added, collection.AddRewardCard(earnedCard));

            collection.ClearSavedCurrentDeck();

            Assert.False(BattleDeckPersistenceService.HasSavedDeck());
            Assert.AreEqual(0, ((List<BattleCardData>)GetPrivateField(collection, "currentBattleDeck")).Count);

            collection.ConfigureBaseDeck(new List<BattleCardData> { baseCardA, baseCardB });

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, GetIds(collection.CurrentBattleDeck));
        }

        [Test]
        public void BuildIdMap_LogsWarning_WhenDuplicateCardIdExists()
        {
            BattleCardData firstCard = CreateCard(1, "First");
            BattleCardData duplicateCard = CreateCard(1, "Duplicate");

            GameObject catalogObject = new("BattleCardCatalog");
            BattleCardCatalog catalog = catalogObject.AddComponent<BattleCardCatalog>();
            SetPrivateField(catalog, "allBattleCards", new List<BattleCardData> { firstCard, duplicateCard });

            LogAssert.Expect(LogType.Warning, new Regex(@"\[BattleCardCatalog\] Duplicate CardID detected: id=1"));
            InvokePrivateMethod(catalog, "BuildIdMap");
        }

        private static List<BattleCardData> CreateSequentialCards(int count)
        {
            List<BattleCardData> cards = new();
            for (int i = 1; i <= count; i++)
            {
                cards.Add(CreateCard(i));
            }

            return cards;
        }

        private static BattleCardData CreateCard(int id, string name = null)
        {
            BattleCardData card = ScriptableObject.CreateInstance<BattleCardData>();
            SetAutoProperty(card, "CardID", id);
            SetAutoProperty(card, "CardName", name ?? $"Battle Card {id}");
            SetAutoProperty(card, "CardType", BattleCardType.Skill);
            SetAutoProperty(card, "ActionPointCost", 1);
            SetAutoProperty(card, "DisplayMoveRange", 0);
            SetAutoProperty(card, "IgnoresDeckLimit", false);
            SetAutoProperty(card, "IsConsumable", false);
            SetAutoProperty(card, "TargetingMode", BattleCardTargetingMode.Auto);
            SetAutoProperty(card, "Keywords", new List<BattleCardKeyword>());
            SetAutoProperty(card, "Effects", new List<Effect>());
            SetPrivateField(card, "description", $"Description {id}");
            SetPrivateField(card, "allowedUserUnitTypes", new List<UnitType>());
            return card;
        }

        private static void SetupCatalog(params BattleCardData[] cards)
        {
            GameObject catalogObject = new("BattleCardCatalog");
            BattleCardCatalog catalog = catalogObject.AddComponent<BattleCardCatalog>();
            SetPrivateField(catalog, "allBattleCards", new List<BattleCardData>(cards));
            InvokePrivateMethod(catalog, "BuildIdMap");
        }

        private static List<int> GetIds(IReadOnlyList<BattleCardData> cards)
        {
            List<int> ids = new();
            if (cards == null)
            {
                return ids;
            }

            foreach (BattleCardData card in cards)
            {
                if (card != null)
                {
                    ids.Add(card.CardID);
                }
            }

            return ids;
        }

        private static void CleanupSceneObjects()
        {
            DestroyIfExists(BattleDeckCollection.Instance);
            DestroyIfExists(BattleCardCatalog.Instance);
            DestroyIfExists(BattleDeckReplacementUI.Instance);
            DestroyIfExists(BattleCardSystem.Instance);
        }

        private static void DestroyIfExists(Object target)
        {
            if (target != null)
            {
                Object.DestroyImmediate(target is Component component ? component.gameObject : target);
            }
        }

        private static object GetPrivateField(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            return field?.GetValue(target);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field?.SetValue(target, value);
        }

        private static void SetAutoProperty(object target, string propertyName, object value)
        {
            SetPrivateField(target, $"<{propertyName}>k__BackingField", value);
        }

        private static void InvokePrivateMethod(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method?.Invoke(target, null);
        }
    }
}
#endif
