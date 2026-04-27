namespace NYH.BattleCardSystem
{
    using System.Collections.Generic;

    internal static class BattleCardKeywordTextFormatter
    {
        private static readonly Dictionary<BattleCardKeyword, (string Label, string Color)> KeywordStyles = new()
        {
            { BattleCardKeyword.Ranged, ("\uC6D0\uAC70\uB9AC", "#8BCF7A") },
            { BattleCardKeyword.Melle, ("\uADFC\uAC70\uB9AC", "#E8796E") },
            { BattleCardKeyword.Stun, ("\uAE30\uC808", "#F2D35E") },
            { BattleCardKeyword.Slow, ("\uB454\uD654", "#6FB7B2") },
            { BattleCardKeyword.Disarm, ("\uBB34\uC7A5\uD574\uC81C", "#B66F6A") },
            { BattleCardKeyword.Push, ("\uBC00\uAE30", "#6BB7D6") },
            { BattleCardKeyword.Pull, ("\uB2F9\uAE30\uAE30", "#B58AD9") },
            { BattleCardKeyword.MoveSpeedUp, ("\uC774\uB3D9\uC18D\uB3C4 \uC99D\uAC00", "#7FCFA6") },
            { BattleCardKeyword.AttackPowerUp, ("\uACF5\uACA9\uB825 \uC99D\uAC00", "#F28B82") },
            { BattleCardKeyword.AreaAttack, ("\uBC94\uC704 \uACF5\uACA9", "#F2A65A") },
            { BattleCardKeyword.NonPiercing, ("\uBE44\uAD00\uD1B5", "#8FA3B8") },
        };

        // 카드 설명 본문 안에 포함된 키워드 단어를 TMP 색상 태그로 감쌉니다.
        public static string ApplyKeywordColors(string description, IReadOnlyList<BattleCardKeyword> keywords)
        {
            if (string.IsNullOrEmpty(description) || keywords == null || keywords.Count == 0)
            {
                return description ?? string.Empty;
            }

            string result = description;
            for (int i = 0; i < keywords.Count; i++)
            {
                BattleCardKeyword keyword = keywords[i];
                if (keyword == BattleCardKeyword.None || !KeywordStyles.TryGetValue(keyword, out var style))
                {
                    continue;
                }

                result = result.Replace(style.Label, Colorize(style.Label, style.Color));
            }

            return result;
        }

        // 카드 설명 맨 위에 표시할 색상 키워드 라인을 만듭니다.
        public static string FormatKeywordList(IReadOnlyList<BattleCardKeyword> keywords)
        {
            if (keywords == null || keywords.Count == 0)
            {
                return string.Empty;
            }

            List<string> labels = new();
            HashSet<BattleCardKeyword> usedKeywords = new();
            for (int i = 0; i < keywords.Count; i++)
            {
                BattleCardKeyword keyword = keywords[i];
                if (keyword == BattleCardKeyword.None
                    || !usedKeywords.Add(keyword)
                    || !KeywordStyles.TryGetValue(keyword, out var style))
                {
                    continue;
                }

                labels.Add(Colorize(style.Label, style.Color));
            }

            return labels.Count == 0 ? string.Empty : string.Join(" ", labels);
        }

        // TextMeshPro rich text에서 사용할 색상 태그 문자열을 만듭니다.
        private static string Colorize(string text, string color)
        {
            return $"<color={color}>{text}</color>";
        }
    }
}
