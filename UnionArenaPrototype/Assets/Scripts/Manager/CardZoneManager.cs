
using System.Collections.Generic;
using UnityEngine;
using Battle;
using Enums;

namespace Manager
{
    // 카드의 영역 이동과 각 영역의 카드 리스트를 관리하는 매니저
    public class CardZoneManager : BaseManager
    {
        public static CardZoneManager Instance { get; private set; }

        // 매니저 초기화 및 싱글톤 등록
        protected override void Awake()
        {
            base.Awake();

            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        // 오브젝트 삭제 시 싱글톤 인스턴스 정리
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // 카드를 지정한 영역으로 이동
        public bool MoveCard(
            PlayerGameData playerGameData,
            CardRuntimeData card,
            BattleEnums.CardZone targetZone,
            BattleEnums.FieldPosition targetFieldPosition = BattleEnums.FieldPosition.None)
        {
            if (playerGameData == null || card == null)
            {
                return false;
            }

            // 이동할 영역의 리스트 가져오기
            List<CardRuntimeData> targetList = GetZoneList(playerGameData, targetZone);

            if (targetList == null)
            {
                return false;
            }

            // 필드 이동 시 세부 위치 확인
            if (targetZone == BattleEnums.CardZone.Field &&
                targetFieldPosition == BattleEnums.FieldPosition.None)
            {
                return false;
            }

            // 필드가 아닌 영역으로 이동 시 위치 초기화
            if (targetZone != BattleEnums.CardZone.Field)
            {
                targetFieldPosition = BattleEnums.FieldPosition.None;
            }

            // 같은 영역 내에서 위치 변경
            if (card.currentZone == targetZone)
            {
                if (!targetList.Contains(card))
                {
                    return false;
                }

                card.fieldPosition = targetFieldPosition;
                return true;
            }

            // 기존 영역의 리스트 가져오기
            List<CardRuntimeData> currentList = GetZoneList(playerGameData, card.currentZone);

            if (targetList.Contains(card))
            {
                return false;
            }

            // 기존 영역에서 카드 제거
            if (card.currentZone != BattleEnums.CardZone.None)
            {
                if (currentList == null || !currentList.Remove(card))
                {
                    return false;
                }
            }

            // 새로운 영역에 카드 추가
            targetList.Add(card);

            // 카드 위치 정보 갱신
            card.currentZone = targetZone;
            card.fieldPosition = targetFieldPosition;

            return true;
        }

        // 덱 맨 위 카드 1장을 손패로 이동
        public bool DrawOneCard(PlayerGameData playerData)
        {
            if (playerData == null ||
                playerData.deck == null ||
                playerData.hand == null)
            {
                return false;
            }

            // 덱에 카드가 없는 경우 드로우 중단
            if (playerData.deck.Count == 0)
            {
                return false;
            }

            // 덱의 첫 번째 카드를 맨 위 카드로 가져옴
            CardRuntimeData topCard = playerData.deck[0];

            // 카드가 없거나 덱 영역에 등록되지 않은 경우 중단
            if (topCard == null ||
                topCard.currentZone != BattleEnums.CardZone.Deck)
            {
                return false;
            }

            // 덱에서 손패 영역으로 카드 1장 이동
            return MoveCard(
                playerData,
                topCard,
                BattleEnums.CardZone.Hand);
        }

        // 지정한 수량만큼 카드를 드로우
        public bool DrawCards(PlayerGameData playerData, int count)
        {
            if (playerData == null ||
                playerData.deck == null ||
                playerData.hand == null)
            {
                return false;
            }

            // 드로우 수량 확인
            if (count <= 0)
            {
                Debug.LogError("DrawCards: 드로우 수량은 1 이상이어야 합니다.");
                return false;
            }

            // 드로우할 카드가 충분한지 먼저 확인
            if (playerData.deck.Count < count)
            {
                Debug.LogWarning("DrawCards: 덱에 드로우할 카드가 부족합니다.");
                return false;
            }

            // 지정한 수량만큼 1장 드로우 함수를 반복 호출
            for (int i = 0; i < count; i++)
            {
                if (!DrawOneCard(playerData))
                {
                    Debug.LogError($"DrawCards: {i + 1}번째 카드 드로우에 실패했습니다.");
                    return false;
                }
            }

            return true;
        }

        // 손패 7장을 현재 순서 그대로 덱 맨 아래로 이동
        public bool ReturnHandToDeckBottom(PlayerGameData playerData)
        {
            if (playerData == null ||
                playerData.hand == null ||
                playerData.deck == null)
            {
                return false;
            }

            // 손패가 정확히 7장인지 확인
            if (playerData.hand.Count != 7)
            {
                Debug.LogError("ReturnHandToDeckBottom: 손패가 7장이 아닙니다.");
                return false;
            }

            // 이동 전에 모든 카드 데이터 검증
            for (int i = 0; i < playerData.hand.Count; i++)
            {
                CardRuntimeData card = playerData.hand[i];

                if (card == null ||
                    card.currentZone != BattleEnums.CardZone.Hand ||
                    playerData.deck.Contains(card))
                {
                    Debug.LogError("ReturnHandToDeckBottom: 손패에 잘못된 카드가 있습니다.");
                    return false;
                }
            }

            // 손패의 첫 번째 카드를 반복해서 덱 맨 아래에 추가
            while (playerData.hand.Count > 0)
            {
                CardRuntimeData card = playerData.hand[0];

                if (!MoveCard(
                    playerData,
                    card,
                    BattleEnums.CardZone.Deck))
                {
                    Debug.LogError("ReturnHandToDeckBottom: 카드 이동에 실패했습니다.");
                    return false;
                }
            }

            return true;
        }

        // 덱 맨 위 카드 1장을 시큐리티로 이동
        public bool MoveTopCardToSecurity(PlayerGameData playerData)
        {
            if (playerData == null ||
                playerData.deck == null ||
                playerData.security == null)
            {
                return false;
            }

            // 덱에 카드가 없는 경우 이동 중단
            if (playerData.deck.Count == 0)
            {
                return false;
            }

            // 덱의 첫 번째 카드를 가져옴
            CardRuntimeData topCard = playerData.deck[0];

            if (topCard == null ||
                topCard.currentZone != BattleEnums.CardZone.Deck)
            {
                return false;
            }

            // 카드 1장을 시큐리티로 이동
            return MoveCard(
                playerData,
                topCard,
                BattleEnums.CardZone.Security);
        }

        // 덱 맨 위에서 지정한 수량만큼 시큐리티로 이동
        public bool MoveCardsToSecurity(PlayerGameData playerData, int count)
        {
            if (playerData == null ||
                playerData.deck == null ||
                playerData.security == null)
            {
                return false;
            }

            if (count <= 0)
            {
                Debug.LogError("MoveCardsToSecurity: 수량은 1 이상이어야 합니다.");
                return false;
            }

            // 이동할 카드가 충분한지 먼저 확인
            if (playerData.deck.Count < count)
            {
                Debug.LogWarning("MoveCardsToSecurity: 덱에 카드가 부족합니다.");
                return false;
            }

            // 지정한 수량만큼 1장 이동 함수를 반복 호출
            for (int i = 0; i < count; i++)
            {
                if (!MoveTopCardToSecurity(playerData))
                {
                    Debug.LogError($"MoveCardsToSecurity: {i + 1}번째 카드 이동에 실패했습니다.");
                    return false;
                }
            }

            return true;
        }

        // 덱에 있는 카드의 순서를 무작위로 변경
        public bool ShuffleDeck(PlayerGameData playerGameData)
        {
            if (playerGameData == null || playerGameData.deck == null)
            {
                return false;
            }

            // 카드가 2장 미만이면 셔플할 수 없음
            if (playerGameData.deck.Count < 2)
            {
                return false;
            }

            // 덱에 잘못된 카드 데이터가 있는지 확인
            for (int i = 0; i < playerGameData.deck.Count; i++)
            {
                if (playerGameData.deck[i] == null ||
                    playerGameData.deck[i].currentZone != BattleEnums.CardZone.Deck)
                {
                    return false;
                }
            }

            // Fisher-Yates 알고리즘으로 덱 순서 변경
            for (int i = playerGameData.deck.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);

                CardRuntimeData temp = playerGameData.deck[i];
                playerGameData.deck[i] = playerGameData.deck[randomIndex];
                playerGameData.deck[randomIndex] = temp;
            }

            return true;
        }

        // 지정한 영역의 카드 리스트 반환
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
    }
}
