using UnityEngine;
using Battle;

namespace UI
{
    // 필드의 개별 슬롯 UI를 관리하는 클래스
    public class FieldSlotView : MonoBehaviour
    {
        [Header("슬롯 위치")]
        [SerializeField] private Enums.BattleEnums.FieldPosition fieldPosition;

        [Header("슬롯 번호")]
        [SerializeField] private int slotIndex = -1;

        [Header("슬롯에 표시할 카드 UI")]
        [SerializeField] private CardView fieldCardView;

        // 슬롯의 필드 위치 반환
        public Enums.BattleEnums.FieldPosition GetFieldPosition()
        {
            return fieldPosition;
        }

        // 슬롯 번호 반환
        public int GetSlotIndex()
        {
            return slotIndex;
        }

        // 슬롯에 카드가 표시되어 있는지 확인
        public bool HasCard()
        {
            return fieldCardView != null &&
                   fieldCardView.gameObject.activeSelf;
        }

        // 카드 데이터를 받아 슬롯에 표시
        public void ShowCard(CardRuntimeData card)
        {
            // 카드 데이터 또는 카드 정보가 없으면 표시 중단
            if (card == null || card.cardData == null)
            {
                ClearCard();
                return;
            }

            // 카드 UI가 연결되지 않았다면 표시 중단
            if (fieldCardView == null)
            {
                Debug.LogError(
                    $"{gameObject.name}: Field Card View가 연결되지 않았습니다.");

                return;
            }

            // 카드 이미지 설정
            fieldCardView.SetCard(card.cardData.CardID);

            // 카드 UI 활성화
            fieldCardView.gameObject.SetActive(true);
        }

        // 슬롯에 표시된 카드 숨기기
        public void ClearCard()
        {
            // 카드 UI가 없다면 종료
            if (fieldCardView == null)
            {
                return;
            }

            // 카드 UI 비활성화
            fieldCardView.gameObject.SetActive(false);
        }
    }
}