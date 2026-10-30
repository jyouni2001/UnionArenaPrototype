using UnityEngine;
using Data;
using Loaders;

namespace Tester
{
    // 카드 데이터베이스 로더 정상작동 되는지 확인
    public class CardDatabaseLoaderTester : MonoBehaviour
    {
        private void Start()
        {
            CardDatabaseLoader loader = new CardDatabaseLoader(); // 카드 데이터 로더 생성
            bool success = loader.LoadDatabase(CardEnums.JJson.Cards.ToString()); // JSON 파일 불러오기 문자열 불러옴
            
            
            
            if (!success)
            {
                Debug.LogError("카드 데이터 테스트 실패");
                return;
            }
            
            Debug.Log("불러온 카드 수: " + loader.CardList.Count); // 불러온 카드 수 확인

            // 첫 번째 카드 정보 확인
            if (loader.CardList.Count > 0)
            {
                for (int i = 0; i < loader.CardList.Count; i++)
                {
                    CardJsonData card = loader.CardList[i];

                    Debug.Log("카드 ID: " + card.CardID);
                    Debug.Log("카드 이름: " + card.CardName);
                    Debug.Log("카드 BP: " + card.BP);
                    Debug.Log("카드 효과: " + card.EffectText);
                }
            }
        }
    }
}