using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Battle;

namespace UI
{
    // 엔드 페이즈 손패 제한 카드를 가로 스크롤로 선택하는 UI 관리
    public class HandLimitSelectionView : MonoBehaviour
    {
        #region UI 연결

        [Header("가로 스크롤 UI")]
        [SerializeField] private ScrollRect handScrollRect; // 손패 선택 가로 스크롤
        [SerializeField] private RectTransform cardContent; // 재사용 카드 슬롯이 놓이는 Content
        [SerializeField] private HandLimitSelectionItemView cardItemPrefab; // 손패 선택 카드 프리팹

        [Header("선택 안내 UI")]
        [SerializeField] private Text instructionText; // 선택 안내 문구
        [SerializeField] private Text selectedCountText; // 현재 선택 장수 표시
        [SerializeField] private Button confirmButton; // 선택 확정 버튼

        [Header("카드 배치 설정")]
        [SerializeField] private int visibleItemCount = 5; // 화면에 표시할 카드 슬롯 수
        [SerializeField] private int bufferItemCount = 2; // 좌우 재사용 대기 슬롯 수
        [SerializeField] private Vector2 cardSize = new Vector2(180f, 252f); // 카드 크기
        [SerializeField] private float cardSpacing = 20f; // 카드 사이 간격
        [SerializeField] private float sidePadding = 20f; // Content 좌우 여백

        private readonly List<HandLimitSelectionItemView> cardItems = new List<HandLimitSelectionItemView>(); // 미리 생성해 재사용하는 카드 UI
        private readonly List<int> boundCardIndices = new List<int>(); // 각 카드 UI에 연결된 손패 인덱스
        private readonly List<CardRuntimeData> handCards = new List<CardRuntimeData>(); // 선택창에 표시할 손패
        private readonly HashSet<int> selectedCardIndices = new HashSet<int>(); // 선택된 손패 인덱스
        private int requiredRemoveCount; // 제외해야 할 카드 장수
        private Func<IReadOnlyList<CardRuntimeData>, bool> onConfirmed; // 선택 결과 전달 콜백

        #endregion

        #region 생명 주기 및 패널 상태

        // UI 버튼과 스크롤 이벤트를 연결
        private void OnEnable()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(HandleConfirmButtonClicked);
            }

            if (handScrollRect != null)
            {
                handScrollRect.onValueChanged.AddListener(HandleScrollChanged);
            }
        }

        // UI 버튼과 스크롤 이벤트를 해제
        private void OnDisable()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(HandleConfirmButtonClicked);
            }

            if (handScrollRect != null)
            {
                handScrollRect.onValueChanged.RemoveListener(HandleScrollChanged);
            }
        }

        // 카드 선택창을 닫고 선택 상태를 초기화
        public void Hide()
        {
            onConfirmed = null;
            selectedCardIndices.Clear();

            for (int i = 0; i < cardItems.Count; i++)
            {
                if (cardItems[i] != null)
                {
                    cardItems[i].Unbind();
                    cardItems[i].gameObject.SetActive(false);
                    boundCardIndices[i] = -1;
                }
            }

            gameObject.SetActive(false);
        }

        #endregion

        #region 손패 표시 및 카드 슬롯 재사용

        // 손패를 표시하고 제외할 카드 장수를 선택하도록 준비
        public bool Show(IReadOnlyList<CardRuntimeData> cards, int removeCount, Func<IReadOnlyList<CardRuntimeData>, bool> confirmCallback)
        {
            if (cards == null || handScrollRect == null || cardContent == null || cardItemPrefab == null || confirmButton == null || removeCount <= 0 || removeCount > cards.Count)
            {
                Debug.LogError("HandLimitSelectionView: ScrollRect, Content, 카드 프리팹 또는 선택 장수를 확인하세요.");
                return false;
            }

            handCards.Clear();
            selectedCardIndices.Clear();
            onConfirmed = confirmCallback;
            requiredRemoveCount = removeCount;

            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] == null || cards[i].cardData == null)
                {
                    Debug.LogError($"HandLimitSelectionView: {i}번째 손패 카드 데이터가 올바르지 않습니다.");
                    return false;
                }

                handCards.Add(cards[i]);
            }

            EnsureCardItems();
            SetContentSize();

            for (int i = 0; i < boundCardIndices.Count; i++)
            {
                boundCardIndices[i] = -1;
            }

            if (instructionText != null)
            {
                instructionText.text = $"리무브로 보낼 카드를 {requiredRemoveCount}장 선택하세요.";
            }

            gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
            handScrollRect.horizontalNormalizedPosition = 0f;
            RefreshVisibleCards();
            RefreshSelectionUI();
            return true;
        }

        // Content 아래에 미리 둔 카드 UI를 재사용하고 부족하면 필요한 만큼 생성
        private void EnsureCardItems()
        {
            if (cardItems.Count == 0)
            {
                cardItems.AddRange(cardContent.GetComponentsInChildren<HandLimitSelectionItemView>(true));

                for (int i = 0; i < cardItems.Count; i++)
                {
                    cardItems[i].gameObject.SetActive(false);
                    boundCardIndices.Add(-1);
                }
            }

            int poolSize = Mathf.Max(visibleItemCount + bufferItemCount * 2, cardItems.Count);

            while (cardItems.Count < poolSize)
            {
                HandLimitSelectionItemView cardItem = Instantiate(cardItemPrefab, cardContent);
                cardItem.gameObject.SetActive(false);
                cardItems.Add(cardItem);
                boundCardIndices.Add(-1);
            }

            for (int i = 0; i < cardItems.Count; i++)
            {
                RectTransform itemRect = cardItems[i].transform as RectTransform;

                if (itemRect == null)
                {
                    Debug.LogError("HandLimitSelectionView: 카드 프리팹에 RectTransform이 없습니다.");
                    continue;
                }

                itemRect.anchorMin = new Vector2(0f, 0.5f);
                itemRect.anchorMax = new Vector2(0f, 0.5f);
                itemRect.pivot = new Vector2(0f, 0.5f);
                itemRect.sizeDelta = cardSize;
            }
        }

        // 손패 전체 길이에 맞춰 Content 가로 크기를 설정
        private void SetContentSize()
        {
            float contentWidth = sidePadding * 2f + handCards.Count * cardSize.x + Mathf.Max(0, handCards.Count - 1) * cardSpacing;
            float viewportWidth = handScrollRect.viewport != null ? handScrollRect.viewport.rect.width : 0f;
            cardContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(contentWidth, viewportWidth));
            cardContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, cardSize.y);
        }

        // 현재 스크롤 위치에 보이는 카드들에 손패 데이터를 연결
        private void RefreshVisibleCards()
        {
            if (handCards.Count == 0 || cardContent == null)
            {
                return;
            }

            float scrollX = Mathf.Max(0f, -cardContent.anchoredPosition.x);
            int firstVisibleIndex = Mathf.FloorToInt(scrollX / (cardSize.x + cardSpacing));
            int maximumStartIndex = Mathf.Max(0, handCards.Count - cardItems.Count);
            int firstPoolIndex = Mathf.Clamp(firstVisibleIndex - bufferItemCount, 0, maximumStartIndex);

            for (int i = 0; i < cardItems.Count; i++)
            {
                int cardIndex = firstPoolIndex + i;
                HandLimitSelectionItemView cardItem = cardItems[i];

                if (cardIndex >= handCards.Count)
                {
                    cardItem.Unbind();
                    cardItem.gameObject.SetActive(false);
                    boundCardIndices[i] = -1;
                    continue;
                }

                RectTransform itemRect = cardItem.transform as RectTransform;
                itemRect.anchoredPosition = new Vector2(sidePadding + cardIndex * (cardSize.x + cardSpacing), 0f);

                if (boundCardIndices[i] != cardIndex)
                {
                    cardItem.Bind(handCards[cardIndex], cardIndex, HandleCardSelected);
                    boundCardIndices[i] = cardIndex;
                }

                cardItem.SetSelected(selectedCardIndices.Contains(cardIndex));
            }
        }

        // 스크롤 위치가 바뀌면 왼쪽으로 벗어난 카드 UI를 오른쪽 카드에 재사용
        private void HandleScrollChanged(Vector2 normalizedPosition)
        {
            RefreshVisibleCards();
        }

        #endregion

        #region 카드 선택 및 확정

        // 손패 카드의 선택 상태를 토글
        private void HandleCardSelected(int cardIndex)
        {
            if (cardIndex < 0 || cardIndex >= handCards.Count)
            {
                return;
            }

            if (selectedCardIndices.Contains(cardIndex))
            {
                selectedCardIndices.Remove(cardIndex);
            }
            else if (selectedCardIndices.Count < requiredRemoveCount)
            {
                selectedCardIndices.Add(cardIndex);
            }

            RefreshVisibleCards();
            RefreshSelectionUI();
        }

        // 현재 선택 장수와 확정 버튼 상태를 갱신
        private void RefreshSelectionUI()
        {
            if (selectedCountText != null)
            {
                selectedCountText.text = $"선택: {selectedCardIndices.Count} / {requiredRemoveCount}";
            }

            if (confirmButton != null)
            {
                confirmButton.interactable = selectedCardIndices.Count == requiredRemoveCount;
            }
        }

        // 선택한 카드들을 UIManager에 전달
        private void HandleConfirmButtonClicked()
        {
            if (selectedCardIndices.Count != requiredRemoveCount || onConfirmed == null)
            {
                return;
            }

            List<CardRuntimeData> selectedCards = new List<CardRuntimeData>();

            foreach (int index in selectedCardIndices)
            {
                selectedCards.Add(handCards[index]);
            }

            if (onConfirmed.Invoke(selectedCards))
            {
                Hide();
            }
        }

        #endregion
    }
}
