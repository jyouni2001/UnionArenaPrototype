using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Data
{
    // 카드 데이터베이스 읽기
    public class CardDatabaseLoader
    {
        private List<CardJsonData> cardList = new List<CardJsonData>(); // json에서 읽어온 데이터 목록

        public List<CardJsonData> CardList => cardList;

        // resources 폴더 json 파일 불러오기
        public bool LoadDatabase(string fileName)
        {
            TextAsset jsonFile = Resources.Load<TextAsset>("CardData/" + fileName);

            if (jsonFile == null)
            {
                Debug.LogError("카드 json 파일을 찾을 수 없습니다.");
                return false;
            }

            // json 내용을 카드 데이터 목록으로 변환
            CardDatabaseJson database = JsonUtility.FromJson<CardDatabaseJson>(jsonFile.text);

            if (database == null || database.Cards == null)
            {
                Debug.LogError("카드 json데이터 형식이 올바르지 않습니다.");
                return false;
            }

            cardList = database.Cards;
            
            Debug.Log("카드 데이터 로드 완료 : " + cardList.Count + "장");
            return true;
        }

        // 카드 id로 특정 카드 찾기
        public CardJsonData GetCardByID(string cardID)
        {
            return cardList.Find(card => card.CardID == cardID);
        }

    }
}