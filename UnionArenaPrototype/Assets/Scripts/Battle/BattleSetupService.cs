using System;
using System.Collections.Generic;
using Data;
using Enums;
using Manager;
using UnityEngine;

namespace Battle
{
    // 전투 시작에 필요한 덱, 손패, 시큐리티 준비 규칙을 처리
    internal sealed class BattleSetupService
    {
        #region 초기 데이터 설정

        private const int RequiredDeckCardCount = 50; // 덱에 필요한 카드 수
        private const int InitialHandCardCount = 7; // 최초 손패 수
        private const int InitialSecurityCardCount = 7; // 최초 시큐리티 수

        private readonly Func<int, PlayerGameData> getPlayerData; // 플레이어 데이터 조회 함수
        private readonly DeckManager deckManager; // 덱 데이터 조회 매니저
        private readonly CardDatabaseManager cardDatabaseManager; // 카드 데이터 조회 매니저
        private readonly CardZoneManager cardZoneManager; // 카드 영역 이동 매니저

        private int nextCardInstanceId = 1; // 다음 카드 인스턴스 번호

        // 필요한 데이터 조회 함수와 매니저 참조를 전달받아 초기화
        public BattleSetupService(Func<int, PlayerGameData> getPlayerData, DeckManager deckManager,
            CardDatabaseManager cardDatabaseManager, CardZoneManager cardZoneManager)
        {
            this.getPlayerData = getPlayerData;
            this.deckManager = deckManager;
            this.cardDatabaseManager = cardDatabaseManager;
            this.cardZoneManager = cardZoneManager;
        }

        // 새 전투 시작 시 카드 인스턴스 번호를 초기화
        public void Reset()
        {
            nextCardInstanceId = 1;
        }

        #endregion

        #region 카드 인스턴스 및 덱 구성

        // 카드 원본 데이터를 전투 중 사용할 카드 인스턴스로 변환
        public CardRuntimeData CreateCardRuntimeData(CardJsonData cardData, int ownerPlayerIndex)
        {
            if (cardData == null || getPlayerData == null || getPlayerData(ownerPlayerIndex) == null)
            {
                return null;
            }

            CardRuntimeData runtimeCard = new CardRuntimeData
            {
                instanceId = nextCardInstanceId++,
                cardData = cardData,
                currentZone = BattleEnums.CardZone.None,
                fieldPosition = BattleEnums.FieldPosition.None,
                ownerPlayerIndex = ownerPlayerIndex
            };

            return runtimeCard;
        }

        // 저장된 덱을 검증하고 플레이어의 전투 덱으로 등록
        public bool InitializePlayerDeck(string deckID, int playerIndex)
        {
            if (deckManager == null || cardDatabaseManager == null || cardZoneManager == null)
            {
                Debug.LogError("BattleSetupService: 필요한 매니저가 없습니다.");
                return false;
            }

            PlayerGameData playerData = getPlayerData != null ? getPlayerData(playerIndex) : null; // 덱을 등록할 플레이어

            if (playerData == null)
            {
                Debug.LogError("BattleSetupService: 플레이어 번호가 올바르지 않습니다.");
                return false;
            }

            if (playerData.deck == null || playerData.deck.Count > 0)
            {
                Debug.LogError("BattleSetupService: 덱이 이미 등록되었거나 목록이 없습니다.");
                return false;
            }

            DeckData deckData = deckManager.GetDeck(deckID); // 저장된 덱 데이터

            if (deckData == null || deckData.Cards == null)
            {
                Debug.LogError("BattleSetupService: 덱 또는 덱 카드 목록을 찾을 수 없습니다.");
                return false;
            }

            int totalCardCount = 0; // 덱에 포함된 카드 총수
            List<CardJsonData> cardDataList = new List<CardJsonData>(); // 검증한 원본 카드 데이터

            // 카드별 수량과 카드 데이터 존재 여부를 먼저 확인
            foreach (DeckCardData deckCard in deckData.Cards)
            {
                if (deckCard == null || string.IsNullOrWhiteSpace(deckCard.CardID) || deckCard.Count < 1 || deckCard.Count > 4)
                {
                    Debug.LogError("BattleSetupService: 덱에 잘못된 카드 정보가 있습니다.");
                    return false;
                }

                CardJsonData cardData = cardDatabaseManager.GetCardByID(deckCard.CardID); // 원본 카드 데이터

                if (cardData == null)
                {
                    Debug.LogError("BattleSetupService: 카드 데이터를 찾을 수 없습니다. ID = " + deckCard.CardID);
                    return false;
                }

                totalCardCount += deckCard.Count;
                cardDataList.Add(cardData);
            }

            if (totalCardCount != RequiredDeckCardCount)
            {
                Debug.LogError($"BattleSetupService: 덱은 {RequiredDeckCardCount}장이어야 합니다. 현재 = {totalCardCount}장");
                return false;
            }

            // 검증된 카드 데이터를 전투용 인스턴스로 만들고 덱 영역에 등록
            for (int i = 0; i < deckData.Cards.Count; i++)
            {
                DeckCardData deckCard = deckData.Cards[i]; // 현재 덱 카드 정보
                CardJsonData cardData = cardDataList[i]; // 검증된 카드 원본 데이터

                for (int count = 0; count < deckCard.Count; count++)
                {
                    CardRuntimeData runtimeCard = CreateCardRuntimeData(cardData, playerIndex); // 전투 카드 인스턴스

                    if (runtimeCard == null || !cardZoneManager.MoveCard(playerData, runtimeCard, BattleEnums.CardZone.Deck))
                    {
                        Debug.LogError("BattleSetupService: 카드를 덱에 등록하지 못했습니다.");
                        return false;
                    }
                }
            }

            Debug.Log($"BattleSetupService: {deckData.DeckName} 덱 등록 완료 / PlayerIndex = {playerIndex} / 카드 수 = {playerData.deck.Count}");
            return true;
        }

        #endregion

        #region 최초 손패와 시큐리티 준비

        // 덱 위에서 최초 손패 7장을 손패로 이동
        public bool DrawInitialHand(PlayerGameData playerData)
        {
            if (playerData == null || cardZoneManager == null)
            {
                Debug.LogError("BattleSetupService: 플레이어 또는 CardZoneManager가 없습니다.");
                return false;
            }

            if (playerData.hand == null || playerData.hand.Count != 0)
            {
                Debug.LogError("BattleSetupService: 최초 손패를 준비할 수 없는 상태입니다.");
                return false;
            }

            if (playerData.deck == null || playerData.deck.Count < InitialHandCardCount + InitialSecurityCardCount)
            {
                Debug.LogError("BattleSetupService: 덱에 준비할 카드 수가 부족합니다.");
                return false;
            }

            for (int i = 0; i < InitialHandCardCount; i++)
            {
                if (!cardZoneManager.DrawOneCard(playerData))
                {
                    Debug.LogError($"BattleSetupService: 최초 손패 {i + 1}번째 드로우 실패");
                    return false;
                }
            }

            return true;
        }

        // 선택이 끝난 플레이어의 덱 위 카드 7장을 시큐리티로 이동
        public bool PrepareSecurity(PlayerGameData playerData)
        {
            if (playerData == null || cardZoneManager == null || playerData.deck == null ||
                playerData.security == null || playerData.security.Count > 0 ||
                playerData.deck.Count < InitialSecurityCardCount)
            {
                Debug.LogError("BattleSetupService: 시큐리티를 준비할 수 없는 상태입니다.");
                return false;
            }

            return cardZoneManager.MoveCardsToSecurity(playerData, InitialSecurityCardCount);
        }

        // 손패를 덱 아래로 되돌리고 새 손패를 뽑은 뒤 덱을 섞음
        public bool Mulligan(PlayerGameData playerData)
        {
            if (playerData == null || cardZoneManager == null)
            {
                return false;
            }

            if (playerData.security == null || playerData.security.Count > 0)
            {
                Debug.LogError("BattleSetupService: 시큐리티가 준비된 뒤에는 멀리건할 수 없습니다.");
                return false;
            }

            if (!cardZoneManager.ReturnHandToDeckBottom(playerData))
            {
                return false;
            }

            if (!cardZoneManager.DrawCards(playerData, InitialHandCardCount))
            {
                return false;
            }

            if (!cardZoneManager.ShuffleDeck(playerData))
            {
                Debug.LogError("BattleSetupService: 멀리건 후 덱 셔플에 실패했습니다.");
                return false;
            }

            return true;
        }

        #endregion
    }
}