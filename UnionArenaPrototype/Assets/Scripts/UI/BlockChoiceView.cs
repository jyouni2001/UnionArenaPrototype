using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Battle;

namespace UI
{
    // 방어할 카드 선택과 공격 통과 버튼을 표시
    public class BlockChoiceView : MonoBehaviour
    {
        #region UI 연결

        [Header("블록 선택 패널")]
        [SerializeField] private GameObject panel; // 블록 선택 패널 오브젝트

        [Header("공격 카드 표시")]
        [SerializeField] private Text attackerText; // 공격 카드 이름과 BP 표시

        [Header("블록 카드 버튼")]
        [SerializeField] private Button[] blockerButtons = new Button[4]; // 실제 필드 슬롯 번호에 대응하는 버튼
        [SerializeField] private Text[] blockerTexts = new Text[4]; // 버튼에 표시할 카드 정보

        [Header("통과 버튼")]
        [SerializeField] private Button passButton; // 공격 통과 버튼

        private Action<CardRuntimeData> onBlockSelected; // 블록 카드 선택 이벤트
        private Action onPassSelected; // 공격 통과 이벤트

        #endregion

        #region 패널 표시

        // 공격 카드와 블록 가능한 카드를 실제 필드 슬롯 번호에 맞춰 표시
        public void Show(CardRuntimeData attacker, IReadOnlyList<CardRuntimeData> blockers, Action<CardRuntimeData> blockSelected, Action passSelected)
        {
            onBlockSelected = blockSelected;
            onPassSelected = passSelected;

            if (attackerText != null && attacker != null && attacker.cardData != null)
            {
                attackerText.text = $"공격: {attacker.cardData.CardName} / BP {attacker.cardData.BP}";
            }

            if (blockerButtons == null)
            {
                Debug.LogError("BlockChoiceView: 블록 버튼 배열이 연결되지 않았습니다.");
                return;
            }

            CardRuntimeData[] blockersBySlot = new CardRuntimeData[blockerButtons.Length]; // 실제 필드 슬롯 번호별 블록 카드

            // 실제 필드 슬롯 번호를 기준으로 블록 카드 배치
            if (blockers != null)
            {
                for (int i = 0; i < blockers.Count; i++)
                {
                    CardRuntimeData blocker = blockers[i];

                    if (blocker == null || blocker.cardData == null)
                    {
                        continue;
                    }

                    int slotIndex = blocker.fieldSlotIndex; // 카드가 실제로 위치한 필드 슬롯 번호

                    if (slotIndex < 0 || slotIndex >= blockersBySlot.Length)
                    {
                        Debug.LogWarning($"BlockChoiceView: 잘못된 블록 슬롯 번호입니다. SlotIndex = {slotIndex}");
                        continue;
                    }

                    if (blockersBySlot[slotIndex] != null)
                    {
                        Debug.LogWarning($"BlockChoiceView: 중복된 블록 슬롯입니다. SlotIndex = {slotIndex}");
                        continue;
                    }

                    blockersBySlot[slotIndex] = blocker;
                }
            }

            // 실제 카드가 존재하는 슬롯의 버튼만 활성화
            for (int i = 0; i < blockerButtons.Length; i++)
            {
                Button button = blockerButtons[i]; // 실제 필드 슬롯 번호와 같은 버튼
                CardRuntimeData blocker = blockersBySlot[i]; // 해당 슬롯에 존재하는 블록 카드

                bool hasBlocker = blocker != null;

                if (blockerTexts != null && i < blockerTexts.Length && blockerTexts[i] != null)
                {
                    blockerTexts[i].text = hasBlocker
                        ? $"{blocker.cardData.CardName} / BP {blocker.cardData.BP}"
                        : string.Empty;
                }

                if (button == null)
                {
                    continue;
                }

                button.onClick.RemoveAllListeners();
                button.gameObject.SetActive(hasBlocker);
                button.interactable = hasBlocker;

                if (!hasBlocker)
                {
                    continue;
                }

                CardRuntimeData selectedBlocker = blocker; // 버튼에 연결할 실제 카드 데이터

                button.onClick.AddListener(() => onBlockSelected?.Invoke(selectedBlocker));
            }

            if (passButton != null)
            {
                passButton.onClick.RemoveAllListeners();
                passButton.onClick.AddListener(() => onPassSelected?.Invoke());
                passButton.interactable = true;
            }

            if (panel != null)
            {
                panel.SetActive(true);
            }
            else
            {
                gameObject.SetActive(true);
            }
        }

        // 블록 선택 패널을 숨기고 임시 버튼 연결을 해제
        public void Hide()
        {
            if (blockerButtons != null)
            {
                for (int i = 0; i < blockerButtons.Length; i++)
                {
                    if (blockerButtons[i] != null)
                    {
                        blockerButtons[i].onClick.RemoveAllListeners();
                    }
                }
            }

            if (passButton != null)
            {
                passButton.onClick.RemoveAllListeners();
            }

            if (panel != null)
            {
                panel.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        #endregion
    }
}
