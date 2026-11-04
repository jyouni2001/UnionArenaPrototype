using UnityEngine;
using UnityEngine.EventSystems;
using Battle;

namespace UI
{
    // 필드 슬롯의 카드 표시와 드롭 입력을 관리하는 클래스
    public class FieldSlotView : MonoBehaviour, IDropHandler
    {
        [Header("슬롯 위치")]
        [SerializeField] private Enums.BattleEnums.FieldPosition fieldPosition; // FrontLine 또는 EnergyLine

        [Header("슬롯 번호")]
        [SerializeField] private int slotIndex = -1; // 슬롯 번호

        [Header("슬롯에 표시할 카드 UI")]
        [SerializeField] private CardView fieldCardView; // 슬롯에 표시할 카드 UI

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

        // 필드 슬롯에 카드 표시
        public void ShowCard(CardRuntimeData card)
        {
            if (card == null || card.cardData == null)
            {
                ClearCard();
                return;
            }

            if (fieldCardView == null)
            {
                Debug.LogError(
                    $"{gameObject.name}: Field Card View가 연결되지 않았습니다.");

                return;
            }

            fieldCardView.SetCard(card.cardData.CardID); // 카드 이미지 설정
            fieldCardView.gameObject.SetActive(true); // 카드 UI 활성화
        }

        // 필드 슬롯의 카드 표시 제거
        public void ClearCard()
        {
            if (fieldCardView == null)
            {
                return;
            }

            fieldCardView.gameObject.SetActive(false); // 카드 UI 비활성화
        }

        // 다른 UI에서 드래그한 카드가 슬롯 위에 놓였을 때 호출
        public void OnDrop(PointerEventData eventData)
        {
            // 드래그 이벤트 데이터가 없으면 처리 중단
            if (eventData == null)
            {
                return;
            }

            // 이미 카드가 표시된 슬롯이면 배치 요청 중단
            if (HasCard())
            {
                return;
            }

            // 손패의 드래그 카드가 현재 슬롯에 배치되도록 요청
            HandView.TryDropOnFieldSlot(this);
        }
    }
}