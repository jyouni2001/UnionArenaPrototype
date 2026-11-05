using UnityEngine;
using UnityEngine.EventSystems;
using Battle;
using Enums;
using Manager;

namespace UI
{
    // 필드 슬롯 카드 표시와 드래그 입력을 담당
    public class FieldSlotView : MonoBehaviour, IDropHandler
    {
        #region 슬롯 설정

        // 이 슬롯이 속한 필드 라인
        [Header("슬롯 위치")]
        [SerializeField]
        private BattleEnums.FieldPosition fieldPosition;

        // 이 슬롯의 라인 내 번호
        [Header("슬롯 번호")]
        [SerializeField]
        private int slotIndex = -1;

        // 이 슬롯에 카드를 표시하는 뷰
        [Header("슬롯에 표시할 카드 UI")]
        [SerializeField]
        private CardView fieldCardView;

        #endregion

        #region 슬롯 정보

        // 슬롯의 필드 라인 반환
        public BattleEnums.FieldPosition GetFieldPosition()
        {
            return fieldPosition;
        }

        // 슬롯 번호 반환
        public int GetSlotIndex()
        {
            return slotIndex;
        }

        // 슬롯에 카드 UI가 표시 중인지 반환
        public bool HasCard()
        {
            return fieldCardView != null &&
                   fieldCardView.gameObject.activeSelf;
        }

        // 이 슬롯의 로컬 플레이어 카드 데이터 반환
        public bool TryGetLocalFieldCard(
            out PlayerGameData playerData,
            out CardRuntimeData card)
        {
            playerData = null;
            card = null;

            if (BattleManager.Instance == null ||
                fieldPosition != BattleEnums.FieldPosition.FrontLine)
            {
                return false;
            }

            playerData = BattleManager.Instance.LocalPlayerData;

            if (playerData == null || playerData.field == null)
            {
                return false;
            }

            for (int i = 0; i < playerData.field.Count; i++)
            {
                CardRuntimeData fieldCard = playerData.field[i];

                if (fieldCard != null &&
                    fieldCard.fieldPosition == fieldPosition &&
                    fieldCard.fieldSlotIndex == slotIndex)
                {
                    card = fieldCard;
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region 카드 표시

        // 카드 이미지와 상태 회전을 슬롯에 표시
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
            SetCardRotation(card.cardState);

            HandView.RegisterFieldCardDragEvents(
                fieldCardView,
                this);

            fieldCardView.gameObject.SetActive(true);
        }

        // 카드 상태를 Z축 회전으로 표시
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

                default:
                    cardRect.localRotation =
                        Quaternion.Euler(0f, 0f, 0f);
                    break;
            }
        }

        // 카드 UI와 드래그 이벤트를 초기화
        public void ClearCard()
        {
            if (fieldCardView == null)
            {
                return;
            }

            fieldCardView.SetDragEvents(null, null, null);

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

        // 같은 라인 슬롯으로 드래그한 카드를 이동 또는 교환
        public void OnDrop(PointerEventData eventData)
        {
            if (eventData == null)
            {
                return;
            }

            HandView.TryDropOnFieldSlot(this);
        }

        #endregion
    }
}