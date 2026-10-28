using UnityEngine;

namespace Data
{
    public class CardImageLoader
    {
        // ID로 실제 이미지 불러오기
        public Sprite LoadCardImage(string cardID)
        {
            if (string.IsNullOrWhiteSpace(cardID)) // ID가 비어 있으면 이미지를 불러오지 않는다.
            {
                Debug.LogWarning("CardImageLoader: 카드 ID가 비어 있습니다.");
                return null;
            }

            string seriesFolder = cardID.Split('_')[0]; // 시리즈 폴더명이 UA11BT여서 _앞부분을 잘라서 사용

            string imagePath = "CardImages/" + seriesFolder + "/" + cardID; // 이미지 경로 만들기

             Sprite cardSprite = Resources.Load<Sprite>(imagePath); // PNG 이미지를 Sprite로 불러온다.
            
            if (cardSprite == null) // sprite가 없으면 에러
            {
                Debug.LogError("CardImageLoader: 카드를 불러오지 못했습니다. 경로: " + imagePath);
            }
            
            return cardSprite;
        }
    }
}