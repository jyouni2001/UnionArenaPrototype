using Data;
using Enums;

namespace Battle
{
    // 에너지 지불 가능 여부를 판정하는 게임 규칙 클래스
    public static class EnergyRuleService
    {
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

            // 필요 에너지 비용이 0이면 에너지 조건 없음
            if (requiredCost <= 0)
            {
                return true;
            }

            int totalEnergy = 0;
            int matchingColorEnergy = 0;
            CardEnums.CardColor requiredColor = cardData.RequiredEnColor;

            for (int i = 0; i < playerData.field.Count; i++)
            {
                CardRuntimeData fieldCard = playerData.field[i];

                if (fieldCard == null ||
                    fieldCard.cardData == null ||
                    fieldCard.fieldPosition != BattleEnums.FieldPosition.EnergyLine)
                {
                    continue;
                }

                int generatedCount = fieldCard.cardData.GeneratedEnCount;

                if (generatedCount <= 0)
                {
                    continue;
                }

                // 에너지 라인의 카드는 휴식 상태여도 에너지를 생성
                totalEnergy += generatedCount;

                if (fieldCard.cardData.GeneratedEnColor == requiredColor)
                {
                    matchingColorEnergy += generatedCount;
                }
            }

            if (totalEnergy < requiredCost)
            {
                errorMessage =
                    $"에너지가 부족합니다. 필요 = {requiredCost}, 보유 = {totalEnergy}";
                return false;
            }

            if (requiredColor != CardEnums.CardColor.None &&
                matchingColorEnergy <= 0)
            {
                errorMessage =
                    $"필요한 색상의 에너지가 없습니다. 필요 색상 = {requiredColor}";
                return false;
            }

            return true;
        }
    }
}