namespace NYH.CoreCardSystem
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// 게임의 모든 동작(GameAction)을 중앙에서 관리하고 순서대로 실행하는 엔진입니다.
    /// 대기열(Queue) 시스템을 사용하여, 실행 중에 들어온 명령을 차례대로 수행합니다.
    /// </summary>
    public class ActionSystem : Singleton<ActionSystem>
    {
        public bool IsPerforming { get; private set; } = false;
        private Queue<GameAction> actionQueue = new();

        // [추가] 현재 처리 중인 연쇄 반응 리스트를 추적하기 위한 변수
        private List<GameAction> currentReactions = null;

        private static Dictionary<Type, List<Action<GameAction>>> preSubs = new();
        private static Dictionary<Type, List<Action<GameAction>>> postSubs = new();
        private static Dictionary<Type, Func<GameAction, IEnumerator>> performers = new();

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this)
            {
                return;
            }

            DontDestroyOnLoad(gameObject);
            Debug.Log($"[ActionSystem] Awake 완료: scene={gameObject.scene.name}, object={name}");
        }

        public void Perform(GameAction action, System.Action OnPerformFinished = null)
        {
            if (action == null)
            {
                Debug.LogWarning("[ActionSystem] null 액션은 실행할 수 없습니다.");
                return;
            }

            actionQueue.Enqueue(action);
            if (!IsPerforming)
            {
                StartCoroutine(ProcessQueue(OnPerformFinished));
            }
        }

        private IEnumerator ProcessQueue(System.Action OnAllFinished)
        {
            IsPerforming = true;
            while (actionQueue.Count > 0)
            {
                yield return Flow(actionQueue.Dequeue());
            }
            IsPerforming = false;
            OnAllFinished?.Invoke();
        }

        private IEnumerator Flow(GameAction action)
        {
            // 1단계: PRE
            currentReactions = action.PreReactions;
            PerformSubscribers(action, preSubs);
            yield return PerformReactions(action.PreReactions);

            // 2단계: PERFORM
            currentReactions = action.PerformReactions;
            yield return     PerformPerformer(action);
            yield return PerformReactions(action.PerformReactions);

            // 3단계: POST
            currentReactions = action.PostReactions;
            PerformSubscribers(action, postSubs);
            yield return PerformReactions(action.PostReactions);
            
            currentReactions = null;
        }

        private IEnumerator PerformReactions(List<GameAction> reactions)
        {
            // 연쇄 반응 실행 중에는 currentReactions가 바뀔 수 있으므로 복사본 사용
            List<GameAction> copy = new List<GameAction>(reactions);
            // 원본 리스트는 비워줌 
            reactions.Clear(); 

            foreach (var reaction in copy)
            {
                yield return Flow(reaction);
            }
        }

        /// <summary>
        /// 현재 실행 중인 단계에 새로운 연쇄 반응을 추가합니다.
        /// </summary>
        public void AddReaction(GameAction gameAction)
        {
            if (currentReactions != null)
            {
                currentReactions.Add(gameAction);
            }
            else
            {
                // 실행 중인 단계가 없다면 다음 메인 액션으로 대기열에 추가
                Perform(gameAction);
            }
        }

        private IEnumerator PerformPerformer(GameAction action)
        {
            Type type = action.GetType();
            if (performers.ContainsKey(type))
            {
                yield return performers[type](action);
            }
        }

        private void PerformSubscribers(GameAction action, Dictionary<Type, List<Action<GameAction>>> subs)
        {
            Type type = action.GetType();
            if (subs.ContainsKey(type))
            {
                foreach (var sub in subs[type])
                {
                    sub(action);
                }
            }
        }

        public static void AttachPerformer<T>(Func<T, IEnumerator> performer) where T : GameAction
        {
            Type type = typeof(T);
            Func<GameAction, IEnumerator> wrapperedPerformer = a => performer((T)a);
            if (performers.ContainsKey(type)) performers[type] = wrapperedPerformer;
            else performers.Add(type, wrapperedPerformer);
        }

        /// <summary>
        /// 등록된 실행자를 제거합니다. 
        /// </summary>
        public static void DetachPerformer<T>() where T : GameAction
        {
            Type type = typeof(T);
            if (performers.ContainsKey(type)) performers.Remove(type);
        }

        public static void SubscribeReaction<T>(Action<T> reaction, ReactionTiming timing) where T : GameAction
        {
            Dictionary<Type, List<Action<GameAction>>> subs = timing == ReactionTiming.PRE ? preSubs : postSubs;
            Action<GameAction> wrapperedReaction = a => reaction((T)a);
            if (!subs.ContainsKey(typeof(T))) subs.Add(typeof(T), new());
            subs[typeof(T)].Add(wrapperedReaction);
        }
    }
}
