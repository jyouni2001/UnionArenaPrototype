using System;
using System.Collections.Generic;
using Battle;
using Enums;
using UnityEngine;

namespace Manager
{
    // 카드 영역 서비스의 생명 주기와 변경 이벤트 전달을 조율
    public class CardZoneManager : BaseManager
    {
        #region 싱글톤 및 서비스 참조

        // 다른 클래스에서 카드 영역 요청을 보낼 때 사용하는 매니저 인스턴스
        public static CardZoneManager Instance { get; private set; }

        // 카드 목록 변경과 이동 처리를 담당하는 서비스
        private CardZoneService cardZoneService;

        // 여러 카드 변경 이벤트를 모으기 위한 일괄 처리 중첩 횟수
        private int updateBatchDepth;

        // 일괄 작업이 끝난 뒤 전달할 영역 변경 목록
        private readonly List<ZoneChangeData> pendingZoneUpdates =
            new List<ZoneChangeData>();

        // 영역 변경 이벤트에 전달할 데이터
        private struct ZoneChangeData
        {
            public PlayerGameData playerData;
            public BattleEnums.CardZone zone;

            public ZoneChangeData(
                PlayerGameData playerData,
                BattleEnums.CardZone zone)
            {
                this.playerData = playerData;
                this.zone = zone;
            }
        }

        // UI 등에서 카드 영역 변경을 구독할 때 사용하는 이벤트
        public event Action<PlayerGameData, BattleEnums.CardZone> OnZoneChanged;

        // 매니저와 서비스를 연결하고 기본 초기화를 진행
        protected override void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            cardZoneService = new CardZoneService();
            cardZoneService.ZoneChanged += HandleServiceZoneChanged;

            base.Awake();
        }

        // 매니저가 제거될 때 서비스 이벤트 연결을 해제
        private void OnDestroy()
        {
            if (cardZoneService != null)
            {
                cardZoneService.ZoneChanged -= HandleServiceZoneChanged;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion

        #region 영역 변경 이벤트 조율

        // 서비스에서 전달한 영역 변경을 구독자에게 전달
        private void HandleServiceZoneChanged(
            PlayerGameData playerData,
            BattleEnums.CardZone zone)
        {
            if (playerData == null)
            {
                return;
            }

            // 일괄 처리 중이면 같은 플레이어와 영역의 이벤트를 한 번만 저장
            if (updateBatchDepth > 0)
            {
                for (int i = 0; i < pendingZoneUpdates.Count; i++)
                {
                    ZoneChangeData pendingData = pendingZoneUpdates[i];

                    if (pendingData.playerData == playerData &&
                        pendingData.zone == zone)
                    {
                        return;
                    }
                }

                pendingZoneUpdates.Add(
                    new ZoneChangeData(playerData, zone));

                return;
            }

            OnZoneChanged?.Invoke(playerData, zone);
        }

        // 여러 카드 이동이 끝난 뒤 모아 둔 이벤트를 한 번씩 전달
        private bool RunBatched(Func<bool> operation)
        {
            updateBatchDepth++;

            try
            {
                return operation();
            }
            finally
            {
                updateBatchDepth--;

                if (updateBatchDepth == 0)
                {
                    List<ZoneChangeData> changes =
                        new List<ZoneChangeData>(pendingZoneUpdates);

                    pendingZoneUpdates.Clear();

                    for (int i = 0; i < changes.Count; i++)
                    {
                        OnZoneChanged?.Invoke(
                            changes[i].playerData,
                            changes[i].zone);
                    }
                }
            }
        }

        // 현재 연결된 카드 영역 서비스
        private CardZoneService Service => cardZoneService;

        #endregion

        #region 카드 이동 요청

        // 카드 영역 이동 요청을 서비스에 전달
        public bool MoveCard(
            PlayerGameData playerGameData,
            CardRuntimeData card,
            BattleEnums.CardZone targetZone,
            BattleEnums.FieldPosition targetFieldPosition =
                BattleEnums.FieldPosition.None,
            int targetFieldSlotIndex = -1)
        {
            return Service != null &&
                   Service.MoveCard(
                       playerGameData,
                       card,
                       targetZone,
                       targetFieldPosition,
                       targetFieldSlotIndex);
        }

        // 필드 카드의 같은 라인 이동 또는 교환 요청을 서비스에 전달
        public bool MoveOrSwapFieldCard(
            PlayerGameData playerData,
            CardRuntimeData movingCard,
            BattleEnums.FieldPosition targetFieldPosition,
            int targetFieldSlotIndex)
        {
            return Service != null &&
                   Service.MoveOrSwapFieldCard(
                       playerData,
                       movingCard,
                       targetFieldPosition,
                       targetFieldSlotIndex);
        }

        // 손패 카드 순서 변경 요청을 서비스에 전달
        public bool ReorderHand(
            PlayerGameData playerData,
            int fromIndex,
            int toIndex)
        {
            return Service != null &&
                   Service.ReorderHand(
                       playerData,
                       fromIndex,
                       toIndex);
        }

        #endregion

        #region 드로우·시큐리티·덱 요청

        // 덱 맨 위 카드 한 장을 손패로 옮기도록 요청
        public bool DrawOneCard(PlayerGameData playerData)
        {
            return Service != null &&
                   Service.DrawOneCard(playerData);
        }

        // 지정한 수량만큼 카드를 뽑고 영역 변경 이벤트를 모아서 전달
        public bool DrawCards(PlayerGameData playerData, int count)
        {
            return Service != null &&
                   RunBatched(() => Service.DrawCards(playerData, count));
        }

        // 손패를 덱 맨 아래로 옮기고 영역 변경 이벤트를 모아서 전달
        public bool ReturnHandToDeckBottom(PlayerGameData playerData)
        {
            return Service != null &&
                   RunBatched(
                       () => Service.ReturnHandToDeckBottom(playerData));
        }

        // 덱 맨 위 카드 한 장을 시큐리티로 옮기도록 요청
        public bool MoveTopCardToSecurity(PlayerGameData playerData)
        {
            return Service != null &&
                   Service.MoveTopCardToSecurity(playerData);
        }

        // 지정한 수량만큼 시큐리티로 옮기고 영역 변경 이벤트를 모아서 전달
        public bool MoveCardsToSecurity(
            PlayerGameData playerData,
            int count)
        {
            return Service != null &&
                   RunBatched(
                       () => Service.MoveCardsToSecurity(playerData, count));
        }

        // 덱 순서를 섞도록 요청
        public bool ShuffleDeck(PlayerGameData playerGameData)
        {
            return Service != null &&
                   Service.ShuffleDeck(playerGameData);
        }

        #endregion
    }
}