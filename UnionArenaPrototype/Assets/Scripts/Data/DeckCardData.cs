using UnityEngine;

namespace Data
{
    // 덱에 포함된 카드 정보
    [System.Serializable]
    public class DeckCardData
    {
        [SerializeField] private string cardId; // 카드 원본 ID
        [SerializeField] private int count; // 해당 카드의 수량

        public string CardID => cardId; // 카드 ID 조회
        public int Count => count; // 카드 수량 조회

        public DeckCardData(string cardId, int count)
        {
            this.cardId = cardId; // 카드 ID 저장
            this.count = count; // 카드 수량 저장
        }

        // 카드 수량 1장 증가
        internal void AddOne()
        {
            count++; // 카드 수량 증가
        }

        // 카드 수량 1장 감소
        internal void RemoveOne()
        {
            count--; // 카드 수량 감소
        }
    }
}