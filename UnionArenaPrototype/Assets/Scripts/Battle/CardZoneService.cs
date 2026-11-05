using System;
using System.Collections.Generic;
using UnityEngine;
using Enums;

namespace Battle
{
    // 카드 목록 변경과 카드 영역 이동 규칙을 처리
    internal sealed class CardZoneService
    {
        #region 영역 변경 알림

        // 카드 영역이 바뀌었을 때 매니저에 알리는 이벤트
        public event Action<PlayerGameData, BattleEnums.CardZone> ZoneChanged;

        #endregion

        #region 카드 이동

        // 카드를 지정한 영역과 필드 슬롯으로 이동
        public bool MoveCard(
            PlayerGameData playerGameData,
            CardRuntimeData card,
            BattleEnums.CardZone targetZone,
            BattleEnums.FieldPosition targetFieldPosition =
                BattleEnums.FieldPosition.None,
            int targetFieldSlotIndex = -1)
        {
            if (playerGameData == null || card == null)
            {
                return false;
            }

            List<CardRuntimeData> targetList =
                GetZoneList(playerGameData, targetZone);

            if (targetList == null)
            {
                return false;
            }

            // 필드로 이동할 때 위치와 슬롯 번호 확인
            if (targetZone == BattleEnums.CardZone.Field)
            {
                if (targetFieldPosition == BattleEnums.FieldPosition.None ||
                    targetFieldPosition == BattleEnums.FieldPosition.Error)
                {
                    Debug.LogWarning(
                        "MoveCard: 필드 위치가 올바르지 않습니다.");

                    return false;
                }

                // 현재 필드 슬롯은 라인별 0~3번
                if (targetFieldSlotIndex < 0 ||
                    targetFieldSlotIndex >= 4)
                {
                    Debug.LogWarning(
                        "MoveCard: 필드 슬롯 번호가 올바르지 않습니다.");

                    return false;
                }

                // 같은 위치와 슬롯을 다른 카드가 사용 중인지 확인
                for (int i = 0; i < playerGameData.field.Count; i++)
                {
                    CardRuntimeData fieldCard =
                        playerGameData.field[i];

                    if (fieldCard == null || fieldCard == card)
                    {
                        continue;
                    }

                    if (fieldCard.fieldPosition == targetFieldPosition &&
                        fieldCard.fieldSlotIndex == targetFieldSlotIndex)
                    {
                        Debug.LogWarning(
                            "MoveCard: 이미 사용 중인 필드 슬롯입니다.");

                        return false;
                    }
                }
            }
            else
            {
                // 필드 밖으로 이동할 때 필드 위치 정보를 초기화
                targetFieldPosition = BattleEnums.FieldPosition.None;
                targetFieldSlotIndex = -1;
            }

            // 이미 같은 영역에 있는 카드인지 확인
            if (card.currentZone == targetZone)
            {
                if (!targetList.Contains(card))
                {
                    return false;
                }

                bool fieldPositionChanged =
                    card.fieldPosition != targetFieldPosition ||
                    card.fieldSlotIndex != targetFieldSlotIndex;

                card.fieldPosition = targetFieldPosition;
                card.fieldSlotIndex = targetFieldSlotIndex;

                // 필드 밖 영역에서는 Active/Rest 상태를 사용하지 않음
                if (targetZone != BattleEnums.CardZone.Field)
                {
                    card.cardState = CardEnums.CardState.None;
                }

                if (fieldPositionChanged)
                {
                    NotifyZoneChanged(
                        playerGameData,
                        targetZone);
                }

                // 같은 영역 안에서 위치만 바꾸면 기존 카드 상태 유지
                return true;
            }

            BattleEnums.CardZone previousZone = card.currentZone;

            List<CardRuntimeData> currentList =
                GetZoneList(playerGameData, previousZone);

            if (targetList.Contains(card))
            {
                return false;
            }

            // 기존 영역에서 카드 제거
            if (previousZone != BattleEnums.CardZone.None)
            {
                if (currentList == null || !currentList.Remove(card))
                {
                    Debug.LogWarning(
                        "MoveCard: 기존 영역에서 카드를 찾을 수 없습니다.");

                    return false;
                }
            }

            // 새 영역에 카드 추가
            targetList.Add(card);

            card.currentZone = targetZone;
            card.fieldPosition = targetFieldPosition;
            card.fieldSlotIndex = targetFieldSlotIndex;

            // 새로 필드에 들어온 카드는 Rest 상태로 시작
            if (targetZone == BattleEnums.CardZone.Field)
            {
                card.cardState = CardEnums.CardState.Rest;
            }
            else
            {
                // 필드 밖으로 이동하면 전투 상태 초기화
                card.cardState = CardEnums.CardState.None;
            }

            // 기존 영역과 새 영역의 변경을 각각 알림
            if (previousZone != BattleEnums.CardZone.None)
            {
                NotifyZoneChanged(
                    playerGameData,
                    previousZone);
            }

            NotifyZoneChanged(
                playerGameData,
                targetZone);

            return true;
        }

        // 필드 카드를 같은 라인 안에서 이동하거나 서로 교환
        public bool MoveOrSwapFieldCard(
            PlayerGameData playerData,
            CardRuntimeData movingCard,
            BattleEnums.FieldPosition targetFieldPosition,
            int targetFieldSlotIndex)
        {
            if (playerData == null ||
                playerData.field == null ||
                movingCard == null)
            {
                return false;
            }

            if (movingCard.currentZone != BattleEnums.CardZone.Field ||
                !playerData.field.Contains(movingCard))
            {
                Debug.LogWarning(
                    "MoveOrSwapFieldCard: 이동할 카드가 필드에 없습니다.");

                return false;
            }

            if (movingCard.fieldPosition == BattleEnums.FieldPosition.None ||
                movingCard.fieldPosition == BattleEnums.FieldPosition.Error)
            {
                Debug.LogWarning(
                    "MoveOrSwapFieldCard: 기존 필드 위치가 올바르지 않습니다.");

                return false;
            }

            if (targetFieldPosition == BattleEnums.FieldPosition.None ||
                targetFieldPosition == BattleEnums.FieldPosition.Error)
            {
                Debug.LogWarning(
                    "MoveOrSwapFieldCard: 이동할 필드 위치가 올바르지 않습니다.");

                return false;
            }

            if (targetFieldSlotIndex < 0 ||
                targetFieldSlotIndex >= 4)
            {
                Debug.LogWarning(
                    "MoveOrSwapFieldCard: 이동할 슬롯 번호가 올바르지 않습니다.");

                return false;
            }

            // FrontLine과 EnergyLine 사이의 이동 차단
            if (movingCard.fieldPosition != targetFieldPosition)
            {
                Debug.LogWarning(
                    "MoveOrSwapFieldCard: 서로 다른 필드 라인으로 이동할 수 없습니다.");

                return false;
            }

            BattleEnums.FieldPosition originalFieldPosition =
                movingCard.fieldPosition;

            int originalSlotIndex =
                movingCard.fieldSlotIndex;

            if (originalSlotIndex == targetFieldSlotIndex)
            {
                return true;
            }

            CardRuntimeData targetCard = null;

            // 도착 슬롯을 사용 중인 카드 검색
            for (int i = 0; i < playerData.field.Count; i++)
            {
                CardRuntimeData fieldCard =
                    playerData.field[i];

                if (fieldCard == null || fieldCard == movingCard)
                {
                    continue;
                }

                if (fieldCard.fieldPosition == targetFieldPosition &&
                    fieldCard.fieldSlotIndex == targetFieldSlotIndex)
                {
                    targetCard = fieldCard;
                    break;
                }
            }

            if (targetCard != null)
            {
                if (targetCard.currentZone != BattleEnums.CardZone.Field ||
                    !playerData.field.Contains(targetCard))
                {
                    Debug.LogWarning(
                        "MoveOrSwapFieldCard: 교환 대상 카드 데이터가 올바르지 않습니다.");

                    return false;
                }

                // 두 카드의 슬롯 위치만 교환하고 카드 상태는 유지
                movingCard.fieldPosition = targetFieldPosition;
                movingCard.fieldSlotIndex = targetFieldSlotIndex;

                targetCard.fieldPosition = originalFieldPosition;
                targetCard.fieldSlotIndex = originalSlotIndex;
            }
            else
            {
                // 도착 슬롯이 비어 있으면 카드 위치만 변경
                movingCard.fieldPosition = targetFieldPosition;
                movingCard.fieldSlotIndex = targetFieldSlotIndex;
            }

            NotifyZoneChanged(
                playerData,
                BattleEnums.CardZone.Field);

            return true;
        }

        // 손패 카드의 순서 변경
        public bool ReorderHand(
            PlayerGameData playerData,
            int fromIndex,
            int toIndex)
        {
            if (playerData == null || playerData.hand == null)
            {
                Debug.LogError(
                    "ReorderHand: 플레이어 또는 손패 데이터가 없습니다.");

                return false;
            }

            if (fromIndex < 0 || fromIndex >= playerData.hand.Count)
            {
                Debug.LogError(
                    "ReorderHand: 기존 카드 위치가 올바르지 않습니다.");

                return false;
            }

            if (toIndex < 0 || toIndex >= playerData.hand.Count)
            {
                Debug.LogError(
                    "ReorderHand: 이동할 카드 위치가 올바르지 않습니다.");

                return false;
            }

            if (fromIndex == toIndex)
            {
                return true;
            }

            CardRuntimeData movingCard =
                playerData.hand[fromIndex];

            if (movingCard == null ||
                movingCard.currentZone != BattleEnums.CardZone.Hand)
            {
                Debug.LogError(
                    "ReorderHand: 이동할 카드가 손패에 등록되어 있지 않습니다.");

                return false;
            }

            playerData.hand.RemoveAt(fromIndex);
            playerData.hand.Insert(toIndex, movingCard);

            NotifyZoneChanged(
                playerData,
                BattleEnums.CardZone.Hand);

            return true;
        }

        #endregion

        #region 턴 시작 카드 상태 갱신

        // 플레이어 필드의 Rest 카드를 Active로 변경
        public bool ActivateFieldCards(PlayerGameData playerData)
        {
            if (playerData == null || playerData.field == null)
            {
                Debug.LogError(
                    "ActivateFieldCards: 플레이어 또는 필드 데이터가 없습니다.");

                return false;
            }

            bool stateChanged = false;

            for (int i = 0; i < playerData.field.Count; i++)
            {
                CardRuntimeData card =
                    playerData.field[i];

                if (card == null ||
                    card.currentZone != BattleEnums.CardZone.Field ||
                    card.cardState == CardEnums.CardState.Active)
                {
                    continue;
                }

                card.cardState = CardEnums.CardState.Active;
                stateChanged = true;
            }

            // 카드 상태가 실제로 바뀌었을 때 필드 UI 갱신 알림
            if (stateChanged)
            {
                NotifyZoneChanged(
                    playerData,
                    BattleEnums.CardZone.Field);
            }

            return true;
        }

        #endregion

        #region 영역 변경 알림

        // 카드 영역 변경이 발생했음을 매니저에 알림
        private void NotifyZoneChanged(
            PlayerGameData playerData,
            BattleEnums.CardZone changedZone)
        {
            if (playerData != null)
            {
                ZoneChanged?.Invoke(
                    playerData,
                    changedZone);
            }
        }

        #endregion

        #region 드로우 및 손패

        // 덱 맨 위 카드 한 장을 손패로 이동
        public bool DrawOneCard(PlayerGameData playerData)
        {
            if (playerData == null ||
                playerData.deck == null ||
                playerData.hand == null)
            {
                return false;
            }

            if (playerData.deck.Count == 0)
            {
                return false;
            }

            CardRuntimeData topCard =
                playerData.deck[0];

            if (topCard == null ||
                topCard.currentZone != BattleEnums.CardZone.Deck)
            {
                return false;
            }

            return MoveCard(
                playerData,
                topCard,
                BattleEnums.CardZone.Hand);
        }

        // 지정한 수량만큼 카드 드로우
        public bool DrawCards(
            PlayerGameData playerData,
            int count)
        {
            if (playerData == null ||
                playerData.deck == null ||
                playerData.hand == null ||
                count <= 0 ||
                playerData.deck.Count < count)
            {
                return false;
            }

            for (int i = 0; i < count; i++)
            {
                if (!DrawOneCard(playerData))
                {
                    return false;
                }
            }

            return true;
        }

        // 손패 7장을 현재 순서 그대로 덱 맨 아래로 이동
        public bool ReturnHandToDeckBottom(
            PlayerGameData playerData)
        {
            if (playerData == null ||
                playerData.hand == null ||
                playerData.deck == null ||
                playerData.hand.Count != 7)
            {
                return false;
            }

            // 이동 전에 손패 카드 데이터가 올바른지 확인
            List<CardRuntimeData> handCards =
                new List<CardRuntimeData>(playerData.hand);

            for (int i = 0; i < handCards.Count; i++)
            {
                CardRuntimeData card = handCards[i];

                if (card == null ||
                    card.currentZone != BattleEnums.CardZone.Hand ||
                    playerData.deck.Contains(card))
                {
                    return false;
                }
            }

            // 손패 순서를 유지하며 카드를 덱으로 이동
            for (int i = 0; i < handCards.Count; i++)
            {
                if (!MoveCard(
                    playerData,
                    handCards[i],
                    BattleEnums.CardZone.Deck))
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region 시큐리티

        // 덱 맨 위 카드 한 장을 시큐리티로 이동
        public bool MoveTopCardToSecurity(
            PlayerGameData playerData)
        {
            if (playerData == null ||
                playerData.deck == null ||
                playerData.security == null)
            {
                return false;
            }

            if (playerData.deck.Count == 0)
            {
                return false;
            }

            CardRuntimeData topCard =
                playerData.deck[0];

            if (topCard == null ||
                topCard.currentZone != BattleEnums.CardZone.Deck)
            {
                return false;
            }

            return MoveCard(
                playerData,
                topCard,
                BattleEnums.CardZone.Security);
        }

        // 덱 맨 위에서 지정한 수량만큼 시큐리티로 이동
        public bool MoveCardsToSecurity(
            PlayerGameData playerData,
            int count)
        {
            if (playerData == null ||
                playerData.deck == null ||
                playerData.security == null ||
                count <= 0 ||
                playerData.deck.Count < count)
            {
                return false;
            }

            for (int i = 0; i < count; i++)
            {
                if (!MoveTopCardToSecurity(playerData))
                {
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region 덱

        // 덱에 있는 카드의 순서를 무작위로 변경
        public bool ShuffleDeck(
            PlayerGameData playerGameData)
        {
            if (playerGameData == null ||
                playerGameData.deck == null)
            {
                return false;
            }

            if (playerGameData.deck.Count < 2)
            {
                return false;
            }

            for (int i = 0; i < playerGameData.deck.Count; i++)
            {
                if (playerGameData.deck[i] == null ||
                    playerGameData.deck[i].currentZone !=
                        BattleEnums.CardZone.Deck)
                {
                    return false;
                }
            }

            // Fisher-Yates 방식으로 덱 순서를 섞음
            for (int i = playerGameData.deck.Count - 1; i > 0; i--)
            {
                int randomIndex =
                    UnityEngine.Random.Range(0, i + 1);

                CardRuntimeData temp =
                    playerGameData.deck[i];

                playerGameData.deck[i] =
                    playerGameData.deck[randomIndex];

                playerGameData.deck[randomIndex] = temp;
            }

            return true;
        }

        #endregion

        #region 영역별 카드 목록 조회

        // 지정한 카드 영역에 해당하는 목록을 반환
        private List<CardRuntimeData> GetZoneList(
            PlayerGameData playerData,
            BattleEnums.CardZone zone)
        {
            if (playerData == null)
            {
                return null;
            }

            switch (zone)
            {
                case BattleEnums.CardZone.Deck:
                    return playerData.deck;

                case BattleEnums.CardZone.Hand:
                    return playerData.hand;

                case BattleEnums.CardZone.Security:
                    return playerData.security;

                case BattleEnums.CardZone.Field:
                    return playerData.field;

                case BattleEnums.CardZone.Trash:
                    return playerData.trash;

                case BattleEnums.CardZone.Remove:
                    return playerData.remove;

                default:
                    return null;
            }
        }

        #endregion
    }
}