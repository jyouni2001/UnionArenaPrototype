
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
        [SerializeField] private RectTransform cardContent; // 카드 UI가 배치되는 Content
        [SerializeField] private HandLimitSelectionItemView cardItemPrefab; // 손패 선택 카드 프리팹

        [Header("선택 안내 UI")]
        [SerializeField] private Text instructionText; // 선택 안내 문구
        [SerializeField] private Text selectedCountText; // 현재 선택 장수 표시
        [SerializeField] private Button confirmButton; // 선택 확정 버튼

        private readonly List<HandLimitSelectionItemView> cardItems =
            new List<HandLimitSelectionItemView>(); // 생성된 카드 UI 목록

        private readonly List<CardRuntimeData> handCards =
            new List<CardRuntimeData>(); // 선택창에 표시할 손패

        private readonly HashSet<int> selectedCardIndices =
            new HashSet<int>(); // 선택된 손패 인덱스

        private int requiredRemoveCount; // 제외해야 할 카드 장수
        private Func<IReadOnlyList<CardRuntimeData>, bool> onConfirmed; // 선택 결과 전달 콜백

        #endregion

        #region 생명 주기 및 패널 상태

        // UI 버튼 이벤트 연결
        private void OnEnable()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(HandleConfirmButtonClicked);
            }
        }

        // UI 버튼 이벤트 해제
        private void OnDisable()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(HandleConfirmButtonClicked);
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
                }
            }

            gameObject.SetActive(false);
        }

        #endregion

        #region 손패 표시 및 카드 생성

        // 손패를 표시하고 제외할 카드 장수를 선택하도록 준비
        public bool Show(
            IReadOnlyList<CardRuntimeData> cards,
            int removeCount,
            Func<IReadOnlyList<CardRuntimeData>, bool> confirmCallback)
        {
            if (cards == null ||
                handScrollRect == null ||
                cardContent == null ||
                cardItemPrefab == null ||
                confirmButton == null ||
                removeCount <= 0 ||
                removeCount > cards.Count)
            {
                Debug.LogError(
                    "HandLimitSelectionView: ScrollRect, Content, 카드 프리팹 또는 선택 장수를 확인하세요.");

                return false;
            }

            handCards.Clear();
            selectedCardIndices.Clear();

            onConfirmed = confirmCallback;
            requiredRemoveCount = removeCount;

            // 손패 데이터 유효성 검사 및 복사
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] == null || cards[i].cardData == null)
                {
                    Debug.LogError(
                        $"HandLimitSelectionView: {i}번째 손패 카드 데이터가 올바르지 않습니다.");

                    return false;
                }

                handCards.Add(cards[i]);
            }

            // 손패 장수만큼 카드 UI 준비
            EnsureCardItems();

            // 선택 안내 문구 갱신
            if (instructionText != null)
            {
                instructionText.text =
                    $"리무브로 보낼 카드를 {requiredRemoveCount}장 선택하세요.";
            }

            // 선택창 활성화
            gameObject.SetActive(true);

            // 카드 UI에 손패 데이터 연결
            for (int i = 0; i < cardItems.Count; i++)
            {
                HandLimitSelectionItemView cardItem = cardItems[i];

                if (cardItem == null)
                {
                    continue;
                }

                if (i >= handCards.Count)
                {
                    cardItem.Unbind();
                    cardItem.gameObject.SetActive(false);
                    continue;
                }

                cardItem.Bind(
                    handCards[i],
                    i,
                    HandleCardSelected);

                cardItem.SetSelected(false);
            }

            // Unity Layout System의 배치 계산 갱신
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(cardContent);
            Canvas.ForceUpdateCanvases();

            // 스크롤을 가장 왼쪽으로 초기화
            handScrollRect.horizontalNormalizedPosition = 0f;

            // 선택 UI 갱신
            RefreshSelectionUI();

            return true;
        }

        // Content 아래에 카드 UI를 필요한 장수만큼 생성
        private void EnsureCardItems()
        {
            // Content에 이미 존재하는 카드 UI 확인
            if (cardItems.Count == 0)
            {
                HandLimitSelectionItemView[] existingItems =
                    cardContent.GetComponentsInChildren<HandLimitSelectionItemView>(true);

                cardItems.AddRange(existingItems);
            }

            // 손패 장수보다 카드 UI가 부족하면 추가 생성
            while (cardItems.Count < handCards.Count)
            {
                HandLimitSelectionItemView cardItem =
                    Instantiate(cardItemPrefab, cardContent);

                cardItem.gameObject.SetActive(false);
                cardItems.Add(cardItem);
            }
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

            RefreshCardSelection();
            RefreshSelectionUI();
        }

        // 카드 선택 표시 갱신
        private void RefreshCardSelection()
        {
            for (int i = 0; i < cardItems.Count; i++)
            {
                if (cardItems[i] == null || i >= handCards.Count)
                {
                    continue;
                }

                cardItems[i].SetSelected(
                    selectedCardIndices.Contains(i));
            }
        }

        // 현재 선택 장수와 확정 버튼 상태 갱신
        private void RefreshSelectionUI()
        {
            if (selectedCountText != null)
            {
                selectedCountText.text =
                    $"선택: {selectedCardIndices.Count} / {requiredRemoveCount}";
            }

            if (confirmButton != null)
            {
                confirmButton.interactable =
                    selectedCardIndices.Count == requiredRemoveCount;
            }
        }

        // 선택한 카드들을 UIManager에 전달
        private void HandleConfirmButtonClicked()
        {
            if (selectedCardIndices.Count != requiredRemoveCount ||
                onConfirmed == null)
            {
                return;
            }

            List<CardRuntimeData> selectedCards =
                new List<CardRuntimeData>();

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
