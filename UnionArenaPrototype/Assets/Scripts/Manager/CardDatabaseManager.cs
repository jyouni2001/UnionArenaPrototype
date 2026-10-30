using System.Collections.Generic;
using Data;
using UnityEngine;
using Loaders;


// 카드 데이터를 Dictionary에 등록하고 ID로 빠르게 조회하는 역할
namespace Manager
{
    public class CardDatabaseManager : BaseManager
    {
        public static CardDatabaseManager Instance { get; private set; } // 데이터베이스 매니저 싱글톤

        private CardDatabaseLoader _cardDatabaseLoader; // json 데이터 로더
        
        private Dictionary<string, CardJsonData> cardDictionary = new Dictionary<string, CardJsonData>(); // 카드 id로 카드 데이터 저장
        
        
        public IReadOnlyDictionary<string, CardJsonData> CardDictionary => cardDictionary; // 외부에서 값변경 불가 조회만 가능

        protected override void Awake()
        {
            base.Awake(); // 부모 awake 한수 실행

            if (Instance != null && Instance != this) // 만약 다른 cardmanager가 존재하고 매니저가 현재 오브젝트와 다른경우
            {
                Destroy(gameObject); // 오브젝트 삭제
                return;
            }

            Instance = this; // 현재 오브젝트를 대표 인스턴스로 등록
            
            _cardDatabaseLoader = new CardDatabaseLoader(); // json파일 읽어 오는 로더 생성
            
            if (!_cardDatabaseLoader.LoadDatabase(CardEnums.JJson.Cards_Converted.ToString()))
            {
                Debug.LogError("CardDatabaseManager: 카드 데이터 로드 실패");
                return;
            }

            foreach (CardJsonData card in _cardDatabaseLoader.CardList) // 카드 목록 처음부터 끝까지 확인
            {
                if (card == null || string.IsNullOrWhiteSpace(card.CardID) || string.IsNullOrWhiteSpace(card.CardName)) // 카드 id와 이름이 비어있거나 카드 데이터가 없으면
                {
                    Debug.LogWarning("ID또는 이름이 없거나 잘못된 카드 데이터입니다.");
                    continue;
                }
                
                if (cardDictionary.ContainsKey(card.CardID)) // 같은 카드 id가 등록이 되어있으면
                {
                    Debug.LogWarning("중복된 카드 ID: " + card.CardID);
                    continue;
                }
                
                cardDictionary.Add(card.CardID, card); // 카드id로 카드 데이터 전체를 값으로 저장
            }
            
            
            foreach (var card in cardDictionary.Values)
            {
                //Debug.Log("등록된 카드: " + card.CardID + " / " + card.CardName);
            }
        }
        
        // 카드 ID로 데이터 조회 하는 함수
        public CardJsonData GetCardByID(string cardID)
        {
            if (string.IsNullOrWhiteSpace(cardID)) // 입력 데이터가 비어있으면
            {
                return null; // null 반환
            }

            cardDictionary.TryGetValue(cardID, out CardJsonData card); // id를 찾아 card 변수에 저장
            return card;
        }
    }
}