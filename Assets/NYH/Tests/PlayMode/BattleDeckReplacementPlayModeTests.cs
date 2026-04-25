namespace NYH.Tests.PlayMode
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using NUnit.Framework;
    using NYH.BattleCardSystem;
    using NYH.CoreCardSystem;
    using UnityEngine;
    using UnityEngine.TestTools;
    using UnityEngine.UI;

    public class BattleDeckReplacementPlayModeTests
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

        [UnityTest]
        public IEnumerator ReplacementUi_ConfirmReturnsSelectedCandidateAndCloses()
        {
            BattleDeckReplacementUI ui = BattleDeckReplacementUI.GetOrCreate();
            BattleCardData rewardCard = CreateCard(100, "Reward");
            BattleCardData candidateA = CreateCard(1, "Candidate A");
            BattleCardData candidateB = CreateCard(2, "Candidate B");

            BattleCardData confirmedCard = null;
            bool cancelCalled = false;

            ui.Show(
                rewardCard,
                new List<BattleCardData> { candidateA, candidateB },
                selected => confirmedCard = selected,
                () => cancelCalled = true);

            yield return null;

            FindButton(ui.transform, "CandidateButton_1").onClick.Invoke();
            FindButton(ui.transform, "ConfirmButton").onClick.Invoke();

            yield return null;

            Assert.AreSame(candidateB, confirmedCard);
            Assert.False(cancelCalled);
            Assert.False(ui.IsOpen);
        }

        [UnityTest]
        public IEnumerator ReplacementSelector_CancelCompletesCoroutineWithNullSelection()
        {
            BattleDeckReplacementSelector selector = new();
            CoroutineRunner runner = new GameObject("CoroutineRunner").AddComponent<CoroutineRunner>();
            BattleCardData rewardCard = CreateCard(100, "Reward");
            BattleCardData candidateA = CreateCard(1, "Candidate A");

            bool callbackInvoked = false;
            bool routineFinished = false;
            BattleCardData selectedCard = rewardCard;

            runner.StartCoroutine(RunSelector(selector, rewardCard, candidateA, data =>
            {
                selectedCard = data;
                callbackInvoked = true;
            }, () => routineFinished = true));

            yield return null;

            BattleDeckReplacementUI ui = BattleDeckReplacementUI.GetOrCreate();
            FindButton(ui.transform, "CancelButton").onClick.Invoke();

            yield return null;

            Assert.True(callbackInvoked);
            Assert.True(routineFinished);
            Assert.IsNull(selectedCard);
        }

        [UnityTest]
        public IEnumerator BattleCardSystem_CreatesCollectionAndPersistsReplacement_WhenRewardExceedsLimit()
        {
            List<BattleCardData> cards = CreateSequentialCards(31);
            SetupCatalog(cards);

            GameObject systemObject = new("BattleCardSystem");
            BattleCardSystem battleCardSystem = systemObject.AddComponent<BattleCardSystem>();
            SetPrivateField(battleCardSystem, "baseBattleDeck", cards.GetRange(0, 30));

            battleCardSystem.SetupFromInspector();
            yield return null;

            Assert.NotNull(BattleDeckCollection.Instance);
            Assert.AreEqual(30, BattleDeckCollection.Instance.CurrentBattleDeck.Count);

            Assert.AreEqual(BattleDeckAddResult.NeedsReplacement, battleCardSystem.AddEarnedBattleCard(cards[30]));

            BattleCardData replaceTarget = BattleDeckCollection.Instance.GetReplaceableCards()[0];
            Assert.AreEqual(BattleDeckAddResult.Replaced, BattleDeckCollection.Instance.ReplaceCard(replaceTarget, cards[30]));

            Object.DestroyImmediate(BattleDeckCollection.Instance.gameObject);
            BattleDeckCollection reloadedCollection = BattleDeckCollection.GetOrCreate();
            reloadedCollection.ConfigureBaseDeck(cards.GetRange(0, 30));

            List<int> currentIds = GetIds(reloadedCollection.CurrentBattleDeck);
            CollectionAssert.DoesNotContain(currentIds, replaceTarget.CardID);
            CollectionAssert.Contains(currentIds, cards[30].CardID);
        }

        private static IEnumerator RunSelector(
            BattleDeckReplacementSelector selector,
            BattleCardData rewardCard,
            BattleCardData candidate,
            System.Action<BattleCardData> onSelected,
            System.Action onFinished)
        {
            yield return selector.SelectReplacement(
                rewardCard,
                new List<BattleCardData> { candidate },
                onSelected);
            onFinished?.Invoke();
        }

        private static List<BattleCardData> CreateSequentialCards(int count)
        {
            List<BattleCardData> cards = new();
            for (int i = 1; i <= count; i++)
            {
                cards.Add(CreateCard(i, $"Battle Card {i}"));
            }

            return cards;
        }

        private static BattleCardData CreateCard(int id, string name)
        {
            BattleCardData card = ScriptableObject.CreateInstance<BattleCardData>();
            SetAutoProperty(card, "CardID", id);
            SetAutoProperty(card, "CardName", name);
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

        private static void SetupCatalog(IReadOnlyList<BattleCardData> cards)
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

        private static Button FindButton(Transform root, string name)
        {
            Transform target = FindChildRecursive(root, name);
            Assert.NotNull(target, $"Button not found: {name}");
            Button button = target.GetComponent<Button>();
            Assert.NotNull(button, $"Button component missing: {name}");
            return button;
        }

        private static Transform FindChildRecursive(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == name)
            {
                return root;
            }

            foreach (Transform child in root)
            {
                Transform result = FindChildRecursive(child, name);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static void CleanupSceneObjects()
        {
            DestroyIfExists(BattleDeckCollection.Instance);
            DestroyIfExists(BattleCardCatalog.Instance);
            DestroyIfExists(BattleDeckReplacementUI.Instance);
            DestroyIfExists(BattleCardSystem.Instance);
            DestroyIfExists(Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>());

            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (canvas != null && canvas.name == "BattleDeckReplacementCanvas")
                {
                    Object.DestroyImmediate(canvas.gameObject);
                }
            }

            foreach (CoroutineRunner runner in Object.FindObjectsByType<CoroutineRunner>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(runner.gameObject);
            }
        }

        private static void DestroyIfExists(Object target)
        {
            if (target != null)
            {
                Object.DestroyImmediate(target is Component component ? component.gameObject : target);
            }
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

        private sealed class CoroutineRunner : MonoBehaviour
        {
        }
    }
}
