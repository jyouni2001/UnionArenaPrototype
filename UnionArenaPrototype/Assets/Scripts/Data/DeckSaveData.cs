using System.Collections.Generic;
using UnityEngine;

namespace Data
{
    // 한 사용자의 전체 덱 저장 정보
    [System.Serializable]
    public class DeckSaveData
    {
        [SerializeField] private int dataVersion = 1; // 저장 데이터 구조 버전
        [SerializeField] private string userID; // 덱 소유자의 로컬 UID
        [SerializeField] private List<DeckData> decks = new List<DeckData>(); // 사용자 덱 목록

        public int DataVersion => dataVersion; // 저장 데이터 버전 조회
        public string UserID => userID; // 사용자 UID 조회
        public IReadOnlyList<DeckData> Decks => decks; // 덱 목록 조회

        public DeckSaveData()
        {
        }

        public DeckSaveData(string userID)
        {
            this.userID = userID; // 사용자 UID 저장
        }

        // 덱 추가
        internal bool AddDeck(DeckData deckData)
        {
            if (deckData == null)
            {
                return false;
            }

            decks.Add(deckData); // 덱 목록에 추가
            return true;
        }

        // 덱 제거
        internal bool RemoveDeck(string deckID)
        {
            for (int i = 0; i < decks.Count; i++)
            {
                if (decks[i].DeckID == deckID)
                {
                    decks.RemoveAt(i); // 해당 덱 제거
                    return true;
                }
            }

            return false;
        }
    }
}