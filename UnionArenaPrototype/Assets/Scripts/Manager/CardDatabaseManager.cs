using System.Collections.Generic;
using Data;
using Loaders;
using UnityEngine;

namespace Manager
{
    // 카드 데이터 로딩을 조율하고 카드 ID별 조회 결과를 캐싱
    public class CardDatabaseManager : BaseManager
    {
        #region 싱글톤 및 카드 데이터

        public static CardDatabaseManager Instance { get; private set; }

        private CardDatabaseLoader cardDatabaseLoader;
        private readonly Dictionary<string, CardJsonData> cardDictionary =
            new Dictionary<string, CardJsonData>();

        // 외부 시스템이 카드 목록을 조회할 수 있도록 읽기 전용으로 제공
        public IReadOnlyDictionary<string, CardJsonData> CardDictionary =>
            cardDictionary;

        #endregion

        #region 생명 주기 및 데이터 로드

        // 중복 매니저를 정리한 뒤 카드 데이터 로드를 초기화
        protected override void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            base.Awake();
        }

        // Resources 로드를 서비스 객체에 맡기고 조회용 캐시를 구성
        protected override void Initialize()
        {
            cardDatabaseLoader = new CardDatabaseLoader();
            cardDictionary.Clear();

            if (!cardDatabaseLoader.LoadDatabase(
                    CardEnums.JJson.Cards_Converted.ToString()))
            {
                Debug.LogError("CardDatabaseManager: 카드 데이터 로드 실패");
                base.Initialize();
                return;
            }

            IReadOnlyList<CardJsonData> loadedCards =
                cardDatabaseLoader.CardList;

            for (int i = 0; i < loadedCards.Count; i++)
            {
                CardJsonData card = loadedCards[i];

                if (card == null ||
                    string.IsNullOrWhiteSpace(card.CardID) ||
                    string.IsNullOrWhiteSpace(card.CardName))
                {
                    Debug.LogWarning(
                        "CardDatabaseManager: ID 또는 이름이 없는 카드 데이터를 건너뜁니다.");
                    continue;
                }

                if (cardDictionary.ContainsKey(card.CardID))
                {
                    Debug.LogWarning(
                        "CardDatabaseManager: 중복된 카드 ID를 건너뜁니다. ID = " +
                        card.CardID);
                    continue;
                }

                cardDictionary.Add(card.CardID, card);
            }

            base.Initialize();
        }

        // 매니저가 제거될 때 싱글톤 참조 해제
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion

        #region 카드 조회

        // 카드 ID에 해당하는 원본 데이터를 캐시에서 반환
        public CardJsonData GetCardByID(string cardID)
        {
            if (string.IsNullOrWhiteSpace(cardID))
            {
                return null;
            }

            cardDictionary.TryGetValue(cardID, out CardJsonData cardData);
            return cardData;
        }

        #endregion
    }
}
