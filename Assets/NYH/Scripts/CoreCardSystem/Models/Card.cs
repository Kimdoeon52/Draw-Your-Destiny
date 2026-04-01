namespace NYH.CoreCardSystem
{
    using System.Collections.Generic;
    using System.Text;
    using UnityEngine;
    using UnityEngine.UI;

    public enum CardType
    {
        None,   
        Fight,  //전투
        Money,  //경제
        Science,//과학
        Normal, //중립
        Common  //일반
    }

    /// <summary>
    /// 게임 내에서 실제로 생성되어 손패나 덱에 존재하는 '카드 객체'입니다.
    /// 에셋 데이터인 CardData를 참조하며, 게임 중 변할 수 있는 상태(예: 마나 비용)를 직접 관리합니다.
    /// </summary>
    public class Card
    {
        // 이름, 설명, 이미지, 효과 등은 게임 중에 변할 일이 없으므로 
        // 데이터 원본(CardData)에서 실시간으로 가져옵니다 (=> 표현식 사용).
        public string Title => data.cardName;
        public string Description => BuildDescription();
        public Sprite Image => data.Image;
        public List<Effect> Effects => data.Effects;
        public int CardID => data.cardID;
        public CardType _CardType => data.cardType;

        /// <summary>
        /// 카드의 마나 비용입니다. 
        /// CardData에서 직접 가져오지 않고 별도의 프로퍼티(Mana)로 둔 이유는,
        /// 게임 중에 특정 효과로 인해 '이 카드의 비용만' 줄어들거나 늘어날 수 있기 때문입니다.
        /// </summary>
        public int Cost { get; set; }

        // 이 카드가 참조하고 있는 데이터 원본(에셋)
        public CardData data { get; private set; }

        /// <summary>
        /// 생성자: CardData 에셋을 기반으로 새로운 카드 인스턴스를 만듭니다.
        /// </summary>
        /// <param name="cardData">에디터에서 만든 카드 데이터 에셋</param>
        public Card(CardData cardData)
        {
            data = cardData;

            // 처음 생성될 때만 데이터 원본의 코스트 값을 복사해옵니다.
            // 이후에는 이 Card 객체의 Cost 변수만 수정하여 게임 로직을 처리합니다.
            Cost = cardData.Cost;
        }
        private string BuildDescription()
        {
            string description = data.Description;
            if (string.IsNullOrEmpty(description) || data.Effects == null) return description;

            StringBuilder builder = new StringBuilder(description);
            foreach (var effect in data.Effects)
            {
                if (effect == null) continue;

                var tokens = effect.GetDescriptionTokens();
                if (tokens == null) continue;

                foreach (var pair in tokens)
                {
                    builder.Replace($"{{{pair.Key}}}", pair.Value);
                }
            }

            return builder.ToString();
        }
    }
}

