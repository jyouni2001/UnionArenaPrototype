using System.Collections.Generic;
using Enums;
using Battle;
using Manager;
using UnityEngine;

namespace Battle
{
    // AP, 필드 카드 상태, 기본 드로우의 턴 규칙을 처리
    internal sealed class TurnFlowService
    {
        #region 의존성 및 턴 규칙

        private const int MaximumAPCardCount = 3; // AP 카드 최대 장수
        private const int MaximumHandCardCount = 8; // 엔드 페이즈 손패 제한

        private readonly CardZoneManager cardZoneManager;

        // 카드 영역 변경 요청을 수행할 매니저 참조를 전달받음
        public TurnFlowService(CardZoneManager cardZoneManager)
        {
            this.cardZoneManager = cardZoneManager;
        }

        #endregion

        #region 턴 시작 처리

        // 지정한 플레이어의 AP, 필드 카드 상태, 기본 드로우 처리
        public bool BeginTurn(
            PlayerGameData playerData,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            if (playerData == null)
            {
                errorMessage = "턴을 시작할 플레이어 데이터가 없습니다.";
                return false;
            }

            if (playerData.field == null || cardZoneManager == null)
            {
                errorMessage = "필드 데이터 또는 CardZoneManager가 없습니다.";
                return false;
            }

            int nextTurnCount = playerData.turnCount + 1;
            int targetAPCardCount =
                GetTargetAPCardCount(playerData, nextTurnCount);

            // 새 턴의 AP를 준비하고 사용한 AP를 모두 활성화
            if (!playerData.RefreshAP(targetAPCardCount))
            {
                errorMessage = "AP 준비에 실패했습니다.";
                return false;
            }

            playerData.turnCount = nextTurnCount;

            // 스타트 단계에서 필드 카드를 Active 상태로 갱신
            if (!cardZoneManager.ActivateFieldCards(playerData))
            {
                errorMessage = "스타트 단계 필드 카드 활성화에 실패했습니다.";
                return false;
            }

            bool isPlayerOneFirstTurn =
                playerData.playerIndex == 0 &&
                playerData.turnCount == 1;

            // 선공 첫 턴을 제외하고 기본 드로우 한 장 처리
            if (!isPlayerOneFirstTurn &&
                playerData.deck != null &&
                playerData.deck.Count > 0)
            {
                if (!cardZoneManager.DrawOneCard(playerData))
                {
                    Debug.LogWarning(
                        "TurnFlowService: 턴 시작 드로우에 실패했습니다.");
                }
            }

            return true;
        }

        // 플레이어 번호와 턴 번호에 따른 AP 카드 수 반환
        private int GetTargetAPCardCount(
            PlayerGameData playerData,
            int turnNumber)
        {
            if (playerData.playerIndex == 0)
            {
                return Mathf.Min(turnNumber, MaximumAPCardCount);
            }

            if (turnNumber == 1)
            {
                return 2;
            }

            return Mathf.Min(turnNumber, MaximumAPCardCount);
        }

        #endregion

        #region 턴 종료 처리

        // 턴을 마치는 플레이어의 필드 카드를 Active 상태로 갱신
        public bool EndTurn(
            PlayerGameData playerData,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            if (playerData == null || cardZoneManager == null)
            {
                errorMessage = "플레이어 데이터 또는 CardZoneManager가 없습니다.";
                return false;
            }

            if (!cardZoneManager.ActivateFieldCards(playerData))
            {
                errorMessage = "엔드 단계 필드 카드 활성화에 실패했습니다.";
                return false;
            }

            return true;
        }
        #region 손패 제한 처리

        // 선택한 손패 카드를 검증한 뒤 제외존으로 이동
        public bool MoveSelectedHandCardsToRemove(PlayerGameData playerData, IReadOnlyList<CardRuntimeData> selectedCards, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (playerData == null || playerData.hand == null || playerData.remove == null || cardZoneManager == null || selectedCards == null)
            {
                errorMessage = "손패 제외에 필요한 데이터가 없습니다.";
                return false;
            }

            HashSet<CardRuntimeData> uniqueCards = new HashSet<CardRuntimeData>();

            for (int i = 0; i < selectedCards.Count; i++)
            {
                CardRuntimeData card = selectedCards[i];

                if (card == null || card.currentZone != BattleEnums.CardZone.Hand || !playerData.hand.Contains(card) || !uniqueCards.Add(card))
                {
                    errorMessage = "선택한 카드가 손패에 없거나 중복 선택되었습니다.";
                    return false;
                }
            }

            List<CardRuntimeData> movedCards = new List<CardRuntimeData>();

            for (int i = 0; i < selectedCards.Count; i++)
            {
                CardRuntimeData card = selectedCards[i];

                if (!cardZoneManager.MoveCard(playerData, card, BattleEnums.CardZone.Remove))
                {
                    for (int rollbackIndex = movedCards.Count - 1; rollbackIndex >= 0; rollbackIndex--)
                    {
                        cardZoneManager.MoveCard(playerData, movedCards[rollbackIndex], BattleEnums.CardZone.Hand);
                    }

                    errorMessage = "선택한 카드를 제외존으로 이동하지 못했습니다.";
                    return false;
                }

                movedCards.Add(card);
            }

            return true;
        }

        // AI 손패가 8장을 넘으면 마지막 카드부터 제외존으로 이동
        public bool MoveExcessHandCardsToRemove(PlayerGameData playerData, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (playerData == null || playerData.hand == null || playerData.remove == null || cardZoneManager == null)
            {
                errorMessage = "AI 손패 정리에 필요한 데이터가 없습니다.";
                return false;
            }

            while (playerData.hand.Count > MaximumHandCardCount)
            {
                CardRuntimeData card = playerData.hand[playerData.hand.Count - 1];

                if (card == null || !cardZoneManager.MoveCard(playerData, card, BattleEnums.CardZone.Remove))
                {
                    errorMessage = "AI 손패 초과 카드를 제외존으로 이동하지 못했습니다.";
                    return false;
                }

                string cardName = card.cardData != null ? card.cardData.CardName : "이름 없는 카드";
                Debug.Log($"TurnFlowService: AI 손패 초과 카드를 제외존으로 이동했습니다. 카드 = {cardName}");
            }

            return true;
        }

        #endregion

        #endregion
    }
}
