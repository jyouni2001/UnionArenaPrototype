using UnityEngine;
using Data;
using Loaders;

namespace Tester
{
    public class CardImageLoaderTester : MonoBehaviour
    {
        void Start()
        {
            CardImageLoader cardImageLoader = new CardImageLoader(); // 카드 이미지 로더 생성
        
            string targetCardID = "UA11BT_GNT-1-001"; // 파일과 일치하는 ID 입력
        
            Sprite cardSprite = cardImageLoader.LoadCardImage(targetCardID); // 카드 id로 이미지 불러오기
            
            if (cardSprite == null)
            {
                Debug.LogError("카드 이미지 테스트 실패: " + targetCardID);
                return;
            }

            Debug.Log("카드 이미지 테스트 성공!");
            Debug.Log("카드 ID: " + targetCardID);
            Debug.Log("이미지 이름: " + cardSprite.name);
            Debug.Log("이미지 크기: " + cardSprite.texture.width + " x " +cardSprite.texture.height);
        }
        
    }
}