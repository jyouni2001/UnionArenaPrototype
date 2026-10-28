using UnityEngine;

namespace UI
{
    public class CardViewTester : MonoBehaviour
    {
        [SerializeField] private CardView cardView; // 카드 이미지 오브젝트 연결
        
        private void Start()
        {
            // 1. CardView 컴포넌트가 연결되었는지 확인한다.
            if (cardView == null)
            {
                Debug.LogError(
                    "CardViewTester: CardView가 연결되지 않았습니다."
                );

                return; // 연결되지 않았으므로 테스트 종료
            }

            string testCardID = "UA11BT_GNT-1-001"; // 테스트할 고유 ID 지정

            cardView.SetCard(testCardID); // SetCard 함수 호출

            Debug.Log("CardView 테스트 실행: " + testCardID);
        }
    }
}