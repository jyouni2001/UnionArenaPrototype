using Data;
using Enums;

namespace Battle
{
    // UI에 전달할 에너지 계산 결과
    public struct EnergySummary
    {
        public int TotalEnergy { get; private set; }

        private int _red;
        private int _yellow;
        private int _green;
        private int _blue;
        private int _purple;

        public EnergySummary(
            int totalEnergy,
            int red,
            int yellow,
            int green,
            int blue,
            int purple)
        {
            TotalEnergy = totalEnergy;
            _red = red;
            _yellow = yellow;
            _green = green;
            _blue = blue;
            _purple = purple;
        }

        public int GetCount(CardEnums.CardColor color)
        {
            switch (color)
            {
                case CardEnums.CardColor.Red:
                    return _red;
                case CardEnums.CardColor.Yellow:
                    return _yellow;
                case CardEnums.CardColor.Green:
                    return _green;
                case CardEnums.CardColor.Blue:
                    return _blue;
                case CardEnums.CardColor.Purple:
                    return _purple;
                default:
                    return 0;
            }
        }
    }

    // 에너지 계산과 지불 가능 여부를 판정하는 게임 규칙 클래스
    public static class EnergyRuleService
    {
        // 에너지 라인 카드들의 총량과 색상별 생성량을 한 번에 계산
        public static EnergySummary GetEnergySummary(PlayerGameData playerData)
        {
            int total = 0;
            int red = 0;
            int yellow = 0;
            int green = 0;
            int blue = 0;
            int purple = 0;

            if (playerData == null || playerData.field == null)
            {
                return new EnergySummary(total, red, yellow, green, blue, purple);
            }

            for (int i = 0; i < playerData.field.Count; i++)
            {
                CardRuntimeData fieldCard = playerData.field[i];

                if (!IsEnergyLineCard(fieldCard))
                {
                    continue;
                }

                int generatedCount = fieldCard.cardData.GeneratedEnCount;

                if (generatedCount <= 0)
                {
                    continue;
                }

                total += generatedCount;

                switch (fieldCard.cardData.GeneratedEnColor)
                {
                    case CardEnums.CardColor.Red:
                        red += generatedCount;
                        break;
                    case CardEnums.CardColor.Yellow:
                        yellow += generatedCount;
                        break;
                    case CardEnums.CardColor.Green:
                        green += generatedCount;
                        break;
                    case CardEnums.CardColor.Blue:
                        blue += generatedCount;
                        break;
                    case CardEnums.CardColor.Purple:
                        purple += generatedCount;
                        break;
                }
            }

            return new EnergySummary(total, red, yellow, green, blue, purple);
        }

        // 기존 호출부 호환용 총 에너지 조회
        public static int GetTotalEnergy(PlayerGameData playerData)
        {
            return GetEnergySummary(playerData).TotalEnergy;
        }

        // 기존 호출부 호환용 색상 에너지 조회
        public static int GetEnergyCountByColor(
            PlayerGameData playerData,
            CardEnums.CardColor color)
        {
            return GetEnergySummary(playerData).GetCount(color);
        }

        // 카드 사용에 필요한 에너지가 있는지 판정
        public static bool HasEnoughEnergy(
            PlayerGameData playerData,
            CardJsonData cardData,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            if (playerData == null)
            {
                errorMessage = "플레이어 데이터가 없습니다.";
                return false;
            }

            if (cardData == null)
            {
                errorMessage = "카드 데이터가 없습니다.";
                return false;
            }

            if (playerData.field == null)
            {
                errorMessage = "필드 데이터가 없습니다.";
                return false;
            }

            int requiredCost = cardData.RequiredEnCost;

            if (requiredCost <= 0)
            {
                return true;
            }

            EnergySummary summary = GetEnergySummary(playerData);

            if (summary.TotalEnergy < requiredCost)
            {
                errorMessage =
                    $"에너지가 부족합니다. 필요 = {requiredCost}, 보유 = {summary.TotalEnergy}";
                return false;
            }

            if (cardData.RequiredEnColor != CardEnums.CardColor.None &&
                summary.GetCount(cardData.RequiredEnColor) <= 0)
            {
                errorMessage =
                    $"필요한 색상의 에너지가 없습니다. 필요 색상 = {cardData.RequiredEnColor}";
                return false;
            }

            return true;
        }

        private static bool IsEnergyLineCard(CardRuntimeData card)
        {
            return card != null &&
                   card.cardData != null &&
                   card.fieldPosition == BattleEnums.FieldPosition.EnergyLine;
        }
    }
}