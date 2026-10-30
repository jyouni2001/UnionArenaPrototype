
using System.Collections.Generic;
using UnityEngine;

namespace Data
{
    // 하나의 덱 정보
    [System.Serializable]
    public class DeckData
    {
        [SerializeField] private string deckId; // 덱 고유 ID
        [SerializeField] private string deckName; // 덱 이름
        [SerializeField] private List<DeckCardData> cards = new List<DeckCardData>(); // 덱에 포함된 카드 목록

        public string DeckID => deckId; // 덱 ID 조회
        public string DeckName => deckName; // 덱 이름 조회
        public IReadOnlyList<DeckCardData> Cards => cards; // 카드 목록 조회

        public DeckData(string deckId, string deckName)
        {
            this.deckId = deckId; // 덱 ID 저장
            this.deckName = deckName; // 덱 이름 저장
        }

        // 덱 이름 변경
        internal void Rename(string newName)
        {
            deckName = newName; // 새로운 덱 이름 저장
        }

        // 특정 카드의 수량 조회
        internal int GetCardCount(string cardId)
        {
            foreach (DeckCardData card in cards)
            {
                if (card.CardID == cardId)
                {
                    return card.Count;
                }
            }

            return 0;
        }

        // 덱에 포함된 전체 카드 수량 조회
        internal int GetTotalCardCount()
        {
            int totalCount = 0; // 전체 카드 수량

            foreach (DeckCardData card in cards)
            {
                totalCount += card.Count; // 카드 수량 합산
            }

            return totalCount;
        }

        // 덱에 카드 추가
        internal bool AddCard(string cardId)
        {
            if (string.IsNullOrWhiteSpace(cardId))
            {
                return false;
            }

            if (GetTotalCardCount() >= 50)
            {
                return false;
            }

            foreach (DeckCardData card in cards)
            {
                if (card.CardID == cardId)
                {
                    if (card.Count >= 4)
                    {
                        return false;
                    }

                    card.AddOne();
                    return true;
                }
            }

            cards.Add(new DeckCardData(cardId, 1)); // 새로운 카드 한 장 추가
            return true;
        }

        // 덱에서 카드 제거
        internal bool RemoveCard(string cardId)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].CardID == cardId)
                {
                    cards[i].RemoveOne(); // 카드 수량 1장 감소

                    if (cards[i].Count <= 0)
                    {
                        cards.RemoveAt(i); // 수량이 0이면 카드 목록에서 제거
                    }

                    return true;
                }
            }

            return false;
        }
    }
}
