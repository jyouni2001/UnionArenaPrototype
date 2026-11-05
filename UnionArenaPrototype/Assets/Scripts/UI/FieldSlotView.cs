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

        // 슬롯이 속한 필드 위치 반환
        public Enums.BattleEnums.FieldPosition GetFieldPosition()
        {
            return fieldPosition;
        }

        // 슬롯 번호 반환
        public int GetSlotIndex()
        {
            return slotIndex;
        }

        // 슬롯에 카드가 표시 중인지 확인
        public bool HasCard()
        {
            return fieldCardView != null &&
                   fieldCardView.gameObject.activeSelf;
        }

        #endregion

        #region 카드 표시

        // 카드 데이터에 맞춰 이미지, 상태 회전, 드래그 이벤트를 설정
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

            // 카드 이미지 표시
            fieldCardView.SetCard(card.cardData.CardID);

            // 카드 상태에 맞춰 필드 카드의 회전 설정
            SetCardRotation(card.cardState);

            // HandView의 드래그 처리를 필드 카드 UI에 연결
            HandView.RegisterFieldCardDragEvents(
                fieldCardView,
                this);

            fieldCardView.gameObject.SetActive(true);
        }

        // 카드 상태를 필드 카드의 Z축 회전으로 표시
        private void SetCardRotation(CardEnums.CardState cardState)
        {
            RectTransform cardRect =
                fieldCardView.transform as RectTransform;

            if (cardRect == null)
            {
                Debug.LogWarning(
                    $"{gameObject.name}: 필드 카드에 RectTransform이 없습니다.");

                return;
            }

            switch (cardState)
            {
                case CardEnums.CardState.Active:
                    cardRect.localRotation =
                        Quaternion.Euler(0f, 0f, 0f);
                    break;

                case CardEnums.CardState.Rest:
                    cardRect.localRotation =
                        Quaternion.Euler(0f, 0f, 90f);
                    break;

                // None 또는 Error 상태는 기본 방향으로 표시
                default:
                    cardRect.localRotation =
                        Quaternion.Euler(0f, 0f, 0f);
                    break;
            }
        }

        // 필드 슬롯의 카드 표시와 드래그 이벤트를 초기화
        public void ClearCard()
        {
            if (fieldCardView == null)
            {
                return;
            }

            // 이전 슬롯의 이벤트 연결이 남지 않도록 해제
            fieldCardView.SetDragEvents(null, null, null);

            // 다음 카드에 이전 회전 상태가 남지 않도록 초기화
            RectTransform cardRect =
                fieldCardView.transform as RectTransform;

            if (cardRect != null)
            {
                cardRect.localRotation =
                    Quaternion.Euler(0f, 0f, 0f);
            }

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

            // 슬롯이 차 있어도 전달해야 같은 라인의 카드 교환이 가능
            HandView.TryDropOnFieldSlot(this);
        }

        #endregion
    }
}