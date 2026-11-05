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
        [SerializeField] private Button[] blockerButtons = new Button[4]; // 블록 카드 선택 버튼
        [SerializeField] private Text[] blockerTexts = new Text[4]; // 버튼에 표시할 카드 정보

        [Header("통과 버튼")]
        [SerializeField] private Button passButton; // 공격 통과 버튼

        private Action<CardRuntimeData> onBlockSelected; // 블록 카드 선택 이벤트
        private Action onPassSelected; // 공격 통과 이벤트

        #endregion

        #region 패널 표시

        // 공격 카드와 블록 가능한 카드를 패널에 표시
        public void Show(CardRuntimeData attacker, IReadOnlyList<CardRuntimeData> blockers, Action<CardRuntimeData> blockSelected, Action passSelected)
        {
            onBlockSelected = blockSelected;
            onPassSelected = passSelected;

            if (attackerText != null && attacker != null && attacker.cardData != null)
            {
                attackerText.text = $"공격: {attacker.cardData.CardName} / BP {attacker.cardData.BP}";
            }

            int blockerCount = blockers != null ? blockers.Count : 0;

            for (int i = 0; i < blockerButtons.Length; i++)
            {
                Button button = blockerButtons[i];
                bool hasBlocker = i < blockerCount && blockers[i] != null && blockers[i].cardData != null;

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

                CardRuntimeData blocker = blockers[i];

                if (blockerTexts != null && i < blockerTexts.Length && blockerTexts[i] != null)
                {
                    blockerTexts[i].text = $"{blocker.cardData.CardName} / BP {blocker.cardData.BP}";
                }

                button.onClick.AddListener(() => onBlockSelected?.Invoke(blocker));
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
            for (int i = 0; i < blockerButtons.Length; i++)
            {
                if (blockerButtons[i] != null)
                {
                    blockerButtons[i].onClick.RemoveAllListeners();
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