using System.Collections.Generic;
using UnityEngine;
using Battle;

namespace UI
{
    // 손패 데이터를 카드 UI에 표시하는 클래스
    public class HandView : MonoBehaviour
    {
        [Header("손패 카드 UI")]
        [SerializeField] private List<CardView> cardViews = new List<CardView>(); // 카드 UI 목록

        // 손패 데이터를 카드 UI에 반영
        public void SetCards(IReadOnlyList<CardRuntimeData> handCards)
        {
            if (handCards == null)
            {
                Debug.LogError("HandView: 손패 데이터가 없습니다.");
                return;
            }

            if (cardViews == null || cardViews.Count == 0)
            {
                Debug.LogError("HandView: 카드 UI 목록이 연결되지 않았습니다.");
                return;
            }

            if (handCards.Count > cardViews.Count)
            {
                Debug.LogError(
                    $"HandView: 카드 UI가 부족합니다. " +
                    $"손패 = {handCards.Count}, UI = {cardViews.Count}");
                return;
            }

            for (int i = 0; i < cardViews.Count; i++)
            {
                CardView cardView = cardViews[i];

                if (cardView == null)
                {
                    Debug.LogError($"HandView: {i}번째 카드 UI가 연결되지 않았습니다.");
                    continue;
                }

                bool hasCard = i < handCards.Count; // 해당 순번에 카드가 있는지 확인
                cardView.gameObject.SetActive(hasCard); // 카드 데이터 유무에 따라 활성화

                if (!hasCard)
                {
                    continue;
                }

                CardRuntimeData card = handCards[i]; // 현재 순번의 카드 데이터

                if (card == null || card.cardData == null)
                {
                    Debug.LogError($"HandView: {i}번째 카드 데이터가 올바르지 않습니다.");
                    continue;
                }

                cardView.SetCard(card.cardData.CardID); // 카드 이미지 표시
            }
        }

        // 모든 카드 UI 비활성화
        public void ClearCards()
        {
            if (cardViews == null)
            {
                return;
            }

            foreach (CardView cardView in cardViews)
            {
                if (cardView != null)
                {
                    cardView.gameObject.SetActive(false);
                }
            }
        }
    }
}
