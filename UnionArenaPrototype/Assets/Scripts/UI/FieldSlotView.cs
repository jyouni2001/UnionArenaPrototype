using UnityEngine;
using UnityEngine.EventSystems;
using Battle;

namespace UI
{
    // 필드 슬롯의 카드 표시와 드래그·드롭 입력을 관리
    public class FieldSlotView : MonoBehaviour, IDropHandler
    {
        #region 슬롯 설정

        [Header("슬롯 위치")]
        [SerializeField]
        private Enums.BattleEnums.FieldPosition fieldPosition;

        [Header("슬롯 번호")]
        [SerializeField]
        private int slotIndex = -1;

        [Header("슬롯에 표시할 카드 UI")]
        [SerializeField]
        private CardView fieldCardView;

        #endregion

        #region 슬롯 정보

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

        #endregion

        #region 카드 표시

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

            fieldCardView.SetCard(card.cardData.CardID);

            // HandView의 드래그 처리를 이 필드 카드 UI에 연결
            HandView.RegisterFieldCardDragEvents(
                fieldCardView,
                this);

            fieldCardView.gameObject.SetActive(true);
        }

        // 필드 슬롯의 카드 표시와 드래그 이벤트 연결 제거
        public void ClearCard()
        {
            if (fieldCardView == null)
            {
                return;
            }

            fieldCardView.SetDragEvents(null, null, null);
            fieldCardView.gameObject.SetActive(false);
        }

        #endregion

        #region 드롭 입력

        // 드래그한 카드를 이 슬롯에 놓았을 때 호출
        public void OnDrop(PointerEventData eventData)
        {
            if (eventData == null)
            {
                return;
            }

            // 슬롯이 차 있어도 요청을 전달해야 같은 라인 카드 교환이 가능
            HandView.TryDropOnFieldSlot(this);
        }

        #endregion
    }
}