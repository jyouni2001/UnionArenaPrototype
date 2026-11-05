using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Battle;

namespace UI
{
    // 손패 제한 선택창에서 카드 한 장의 선택 표시를 관리
    public class HandLimitSelectionItemView : MonoBehaviour
    {
        #region UI 연결

        [Header("카드 선택 UI")]
        [SerializeField] private CardView cardView; // 카드 이미지 표시 UI
        [SerializeField] private Button selectButton; // 카드 선택 버튼
        [SerializeField] private GameObject selectedIndicator; // 선택 상태 표시 UI

        private UnityAction clickAction; // 현재 카드에 연결된 선택 이벤트
        private Action<int> onSelected; // 선택 인덱스를 전달할 콜백
        private int cardIndex = -1; // 표시 중인 카드 인덱스

        #endregion

        #region 카드 표시 및 선택

        // 손패 카드를 표시하고 선택 입력을 연결
        public void Bind(CardRuntimeData card, int index, Action<int> selectionCallback)
        {
            if (card == null || card.cardData == null || cardView == null || selectButton == null)
            {
                Debug.LogError("HandLimitSelectionItemView: 카드 데이터 또는 UI 연결을 확인하세요.");
                gameObject.SetActive(false);
                return;
            }

            Unbind();
            cardIndex = index;
            onSelected = selectionCallback;
            cardView.SetCard(card.cardData.CardID);
            cardView.SetDragEvents(null, null, null);
            SetSelected(false);
            clickAction = () => onSelected?.Invoke(cardIndex);
            selectButton.onClick.AddListener(clickAction);
            gameObject.SetActive(true);
        }

        // 카드 선택 표시를 갱신
        public void SetSelected(bool isSelected)
        {
            if (selectedIndicator != null)
            {
                selectedIndicator.SetActive(isSelected);
            }
        }

        // 카드 선택 이벤트와 표시 데이터를 해제
        public void Unbind()
        {
            if (selectButton != null && clickAction != null)
            {
                selectButton.onClick.RemoveListener(clickAction);
            }

            clickAction = null;
            onSelected = null;
            cardIndex = -1;
        }

        // 오브젝트가 제거되면 버튼 이벤트를 해제
        private void OnDestroy()
        {
            Unbind();
        }

        #endregion
    }
}
