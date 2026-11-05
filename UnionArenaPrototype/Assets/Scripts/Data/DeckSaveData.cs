using System.Collections.Generic;
using UnityEngine;

namespace Data
{
    // 공유 덱 저장 데이터
    [System.Serializable]
    public class DeckSaveData
    {
        [SerializeField] private int dataVersion = 1; // 사용자 ID 없이 덱을 공유하는 저장 형식 버전
        [SerializeField] private List<DeckData> decks = new List<DeckData>(); // 저장할 덱 목록

        public int DataVersion => dataVersion; // 저장 형식 버전 조회
        public IReadOnlyList<DeckData> Decks => decks; // 저장된 덱 목록 조회

        public DeckSaveData()
        {
        }

        // 덱을 저장 목록에 추가
        internal bool AddDeck(DeckData deckData)
        {
            if (deckData == null)
            {
                return false;
            }

            if (decks == null)
            {
                decks = new List<DeckData>(); // 목록이 비어 있으면 추가할 수 있도록 초기화
            }

            decks.Add(deckData);
            return true;
        }

        // ID가 일치하는 덱을 저장 목록에서 제거
        internal bool RemoveDeck(string deckID)
        {
            if (decks == null)
            {
                return false;
            }

            for (int i = 0; i < decks.Count; i++)
            {
                if (decks[i] != null && decks[i].DeckID == deckID)
                {
                    decks.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }
    }
}