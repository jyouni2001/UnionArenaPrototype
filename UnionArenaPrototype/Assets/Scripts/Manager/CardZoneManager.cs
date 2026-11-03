using System;
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

        // 손패가 변경되었을 때 해당 플레이어 데이터를 전달하는 이벤트
        public event Action<PlayerGameData> OnHandChanged;

        // 현재 진행 중인 손패 일괄 변경 작업의 중첩 횟수
        private int handUpdateBatchDepth = 0;

        // 일괄 변경 작업이 끝난 뒤 이벤트를 발생시킬 플레이어 목록
        private HashSet<PlayerGameData> pendingHandUpdates = new HashSet<PlayerGameData>();

        // 매니저 초기화 및 싱글톤 등록
        protected override void Awake()
        {
            base.Awake();

            // 이미 다른 카드 영역 매니저가 존재하면 현재 오브젝트 제거
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this; // 현재 카드 영역 매니저를 싱글톤으로 등록
        }

        // 오브젝트 삭제 시 싱글톤 인스턴스 정리
        private void OnDestroy()
        {
            // 현재 인스턴스가 싱글톤으로 등록된 객체일 때만 초기화
            if (Instance == this)
            {
                Instance = null; // 싱글톤 참조 해제
            }
        }

        // 카드를 지정한 영역으로 이동
        public bool MoveCard(
            PlayerGameData playerGameData,
            CardRuntimeData card,
            BattleEnums.CardZone targetZone,
            BattleEnums.FieldPosition targetFieldPosition = BattleEnums.FieldPosition.None)
        {
            // 플레이어 또는 카드 데이터가 없으면 이동 중단
            if (playerGameData == null || card == null)
            {
                return false;
            }

            List<CardRuntimeData> targetList = GetZoneList(playerGameData, targetZone); // 이동할 영역의 카드 목록

            // 이동할 영역의 목록이 존재하지 않으면 이동 중단
            if (targetList == null)
            {
                return false;
            }

            // 필드로 이동할 때 세부 위치가 지정되지 않았다면 이동 중단
            if (targetZone == BattleEnums.CardZone.Field &&
                targetFieldPosition == BattleEnums.FieldPosition.None)
            {
                return false;
            }

            // 필드가 아닌 영역으로 이동할 때는 세부 위치 초기화
            if (targetZone != BattleEnums.CardZone.Field)
            {
                targetFieldPosition = BattleEnums.FieldPosition.None; // 필드 위치가 필요 없는 영역으로 이동
            }

            // 카드가 이미 같은 영역에 있다면 필드 위치만 변경
            if (card.currentZone == targetZone)
            {
                // 해당 영역의 목록에 카드가 실제로 등록되어 있는지 확인
                if (!targetList.Contains(card))
                {
                    return false;
                }

                card.fieldPosition = targetFieldPosition; // 카드의 필드 위치 갱신
                return true;
            }

            BattleEnums.CardZone previousZone = card.currentZone; // 카드가 이동하기 전 위치 저장
            List<CardRuntimeData> currentList = GetZoneList(playerGameData, previousZone); // 카드가 기존에 있던 영역의 목록

            // 이동할 영역에 이미 카드가 등록되어 있다면 중복 이동 방지
            if (targetList.Contains(card))
            {
                return false;
            }

            // 기존 영역에서 카드 제거
            if (previousZone != BattleEnums.CardZone.None)
            {
                // 기존 영역 목록이 없거나 카드 제거에 실패하면 이동 중단
                if (currentList == null || !currentList.Remove(card))
                {
                    return false;
                }
            }

            targetList.Add(card); // 이동할 영역의 목록에 카드 추가

            card.currentZone = targetZone; // 카드의 현재 영역 갱신
            card.fieldPosition = targetFieldPosition; // 카드의 필드 위치 갱신

            // 기존 위치 또는 이동할 위치가 손패라면 손패 변경 이벤트 발생
            if (previousZone == BattleEnums.CardZone.Hand ||
                targetZone == BattleEnums.CardZone.Hand)
            {
                NotifyHandChanged(playerGameData); // 손패 변경 이벤트 전달 또는 일괄 처리 목록에 추가
            }

            return true;
        }

        // 손패 변경 이벤트를 발생시키거나 일괄 처리 목록에 추가
        private void NotifyHandChanged(PlayerGameData playerData)
        {
            // 플레이어 데이터가 없으면 이벤트 처리 중단
            if (playerData == null)
            {
                return;
            }

            // 일괄 처리 중이면 즉시 이벤트를 발생시키지 않음
            if (handUpdateBatchDepth > 0)
            {
                pendingHandUpdates.Add(playerData); // 작업 종료 후 이벤트를 발생시킬 플레이어 저장
                return;
            }

            OnHandChanged?.Invoke(playerData); // 이벤트를 구독한 객체에 손패 변경 알림
        }

        // 손패 일괄 변경 작업 시작
        private void BeginHandUpdateBatch()
        {
            handUpdateBatchDepth++; // 일괄 변경 작업 중첩 횟수 증가
        }

        // 손패 일괄 변경 작업 종료 및 대기 중인 이벤트 전달
        private void EndHandUpdateBatch()
        {
            handUpdateBatchDepth--; // 일괄 변경 작업 중첩 횟수 감소

            // 아직 다른 일괄 작업이 진행 중이면 이벤트 전달 보류
            if (handUpdateBatchDepth > 0)
            {
                return;
            }

            handUpdateBatchDepth = 0; // 중첩 횟수가 음수가 되는 상황 방지

            List<PlayerGameData> updatedPlayers =
                new List<PlayerGameData>(pendingHandUpdates); // 이벤트 전달 대상 목록 복사

            pendingHandUpdates.Clear(); // 이벤트 전달 전에 대기 목록 초기화

            // 변경된 플레이어마다 손패 변경 이벤트 한 번씩 발생
            for (int i = 0; i < updatedPlayers.Count; i++)
            {
                OnHandChanged?.Invoke(updatedPlayers[i]); // 해당 플레이어의 손패 변경 알림
            }
        }

        // 덱 맨 위 카드 1장을 손패로 이동
        public bool DrawOneCard(PlayerGameData playerData)
        {
            // 플레이어 또는 카드 목록이 없으면 드로우 중단
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

            CardRuntimeData topCard = playerData.deck[0]; // 덱의 첫 번째 카드를 맨 위 카드로 가져옴

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
            // 플레이어 또는 카드 목록이 없으면 드로우 중단
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

            BeginHandUpdateBatch(); // 여러 장의 드로우를 하나의 손패 변경 작업으로 시작

            try
            {
                // 지정한 수량만큼 1장 드로우 함수를 반복 호출
                for (int i = 0; i < count; i++)
                {
                    // 드로우 중 하나라도 실패하면 작업 중단
                    if (!DrawOneCard(playerData))
                    {
                        Debug.LogError($"DrawCards: {i + 1}번째 카드 드로우에 실패했습니다.");
                        return false;
                    }
                }

                return true;
            }
            finally
            {
                EndHandUpdateBatch(); // 성공 또는 실패와 관계없이 일괄 변경 작업 종료
            }
        }

        // 손패 7장을 현재 순서 그대로 덱 맨 아래로 이동
        public bool ReturnHandToDeckBottom(PlayerGameData playerData)
        {
            // 플레이어 또는 카드 목록이 없으면 이동 중단
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
                CardRuntimeData card = playerData.hand[i]; // 현재 검사할 손패 카드

                // 카드가 없거나 손패 영역에 없거나 덱에 중복 등록된 경우 중단
                if (card == null || card.currentZone != BattleEnums.CardZone.Hand || playerData.deck.Contains(card))
                {
                    Debug.LogError("ReturnHandToDeckBottom: 손패에 잘못된 카드가 있습니다.");
                    return false;
                }
            }

            BeginHandUpdateBatch(); // 손패 전체 이동을 하나의 변경 작업으로 시작

            try
            {
                // 손패의 첫 번째 카드를 반복해서 덱 맨 아래에 추가
                while (playerData.hand.Count > 0)
                {
                    CardRuntimeData card = playerData.hand[0]; // 현재 손패의 첫 번째 카드

                    // 카드 이동에 실패하면 작업 중단
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
            finally
            {
                EndHandUpdateBatch(); // 성공 또는 실패와 관계없이 일괄 변경 작업 종료
            }
        }

        // 덱 맨 위 카드 1장을 시큐리티로 이동
        public bool MoveTopCardToSecurity(PlayerGameData playerData)
        {
            // 플레이어 또는 카드 목록이 없으면 이동 중단
            if (playerData == null || playerData.deck == null || playerData.security == null)
            {
                return false;
            }

            // 덱에 카드가 없는 경우 이동 중단
            if (playerData.deck.Count == 0)
            {
                return false;
            }

            CardRuntimeData topCard = playerData.deck[0]; // 덱의 첫 번째 카드를 가져옴

            // 카드가 없거나 덱 영역에 등록되지 않은 경우 중단
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
            // 플레이어 또는 카드 목록이 없으면 이동 중단
            if (playerData == null ||
                playerData.deck == null ||
                playerData.security == null)
            {
                return false;
            }

            // 이동 수량 확인
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
                // 카드 이동 중 하나라도 실패하면 작업 중단
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
            // 플레이어 또는 덱 목록이 없으면 셔플 중단
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
                // 카드가 없거나 덱 영역에 등록되지 않은 경우 중단
                if (playerGameData.deck[i] == null ||
                    playerGameData.deck[i].currentZone != BattleEnums.CardZone.Deck)
                {
                    return false;
                }
            }

            // Fisher-Yates 알고리즘으로 덱 순서 변경
            for (int i = playerGameData.deck.Count - 1; i > 0; i--)
            {
                int randomIndex = UnityEngine.Random.Range(0, i + 1); // 현재 위치와 교환할 무작위 인덱스

                CardRuntimeData temp = playerGameData.deck[i]; // 현재 카드 임시 저장
                playerGameData.deck[i] = playerGameData.deck[randomIndex]; // 무작위 위치의 카드를 현재 위치로 이동
                playerGameData.deck[randomIndex] = temp; // 임시 저장한 카드를 무작위 위치로 이동
            }

            return true;
        }

        // 지정한 영역의 카드 리스트 반환
        private List<CardRuntimeData> GetZoneList(
            PlayerGameData playerData,
            BattleEnums.CardZone zone)
        {
            // 플레이어 데이터가 없으면 목록 반환 불가
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
