namespace NYH.CoreCardSystem
{
    using UnityEngine;
    using DG.Tweening;

    public class CardViewCreator : Singleton<CardViewCreator>
    {
        [SerializeField] private CardView cardViewPrefab;

        public CardView CreateCardView(Card card, Vector3 position, Quaternion rotation)
        {
            // 부모를 확실히 지정하여 생성
            CardView cardView = Instantiate(cardViewPrefab, transform);
            
            // ✅ 즉시 보이도록 크기를 1로 설정 (애니메이션은 선택사항)
            cardView.transform.localScale = Vector3.one; 
            
            // 위치와 회전 초기화
            cardView.transform.position = position;
            cardView.transform.rotation = rotation;

            cardView.Setup(card);
            return cardView;
        }
    }
}
