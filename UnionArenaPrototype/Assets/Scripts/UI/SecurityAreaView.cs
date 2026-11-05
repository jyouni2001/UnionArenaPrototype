using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Battle;
using Enums;
using Manager;

namespace UI
{
    // 미리 만든 시큐리티 카드 UI와 공격 드롭을 관리
    public class SecurityAreaView : MonoBehaviour, IDropHandler
    {
        #region 시큐리티 UI 설정

        [Header("시큐리티 카드 UI - 위쪽 카드부터 연결")]
        [SerializeField] private List<SecurityCardView> securityCardViews = new List<SecurityCardView>(7); // 시큐리티 카드 슬롯

        [Header("공격 드롭 설정")]
        [SerializeField] private bool acceptsAttackDrop; // 상대 영역만 체크

        [Header("공개 표시 시간")]
        [SerializeField] private float revealDuration = 1f; // 카드 앞면 공개 시간

        private bool isResolvingReveal; // 공개 처리 중복 방지

        #endregion

        #region 시큐리티 표시 갱신

        // 현재 시큐리티 수만큼 뒷면 UI를 켜고 나머지 UI를 끔
        public void SetSecurityCards(IReadOnlyList<CardRuntimeData> securityCards)
        {
            if (securityCardViews == null)
            {
                return;
            }

            int visibleCardCount = securityCards != null ? securityCards.Count : 0;

            for (int i = 0; i < securityCardViews.Count; i++)
            {
                SecurityCardView cardView = securityCardViews[i];

                if (cardView == null)
                {
                    Debug.LogWarning($"{gameObject.name}: 시큐리티 카드 UI {i}번이 연결되지 않았습니다.");
                    continue;
                }

                if (i < visibleCardCount)
                {
                    cardView.ShowBack();
                }
                else
                {
                    cardView.Hide();
                }
            }
        }

        #endregion

        #region 공격 드롭 처리

        // 공격 카드를 상대 시큐리티 영역에 놓았을 때 공격을 선언
        public void OnDrop(PointerEventData eventData)
        {
            if (!acceptsAttackDrop || isResolvingReveal || eventData == null || eventData.pointerDrag == null || BattleManager.Instance == null)
            {
                return;
            }

            FieldSlotView sourceSlot = eventData.pointerDrag.GetComponentInParent<FieldSlotView>();

            if (sourceSlot == null || !sourceSlot.TryGetLocalFieldCard(out PlayerGameData attackerPlayer, out CardRuntimeData attacker))
            {
                return;
            }

            if (!BattleManager.Instance.TryDeclareAttack(attackerPlayer, attacker, out BattleCombatService.Resolution resolution, out PlayerGameData securityOwner, out CardRuntimeData revealedSecurityCard, out string errorMessage))
            {
                Debug.LogWarning("SecurityAreaView: " + errorMessage);
                return;
            }

            sourceSlot.ShowCard(attacker);

            if (resolution.Type == BattleEnums.CombatResolutionType.AttackerWins)
            {
                Debug.Log("SecurityAreaView: 블록 카드가 트래시로 이동했습니다.");
                return;
            }

            if (resolution.Type == BattleEnums.CombatResolutionType.AttackFails)
            {
                Debug.Log("SecurityAreaView: 블록되어 시큐리티 피해가 없습니다.");
                return;
            }

            if (resolution.Type != BattleEnums.CombatResolutionType.UnblockedAttack)
            {
                return;
            }

            if (revealedSecurityCard == null)
            {
                Debug.LogWarning("SecurityAreaView: 공개할 시큐리티 카드가 없습니다.");
                return;
            }

            StartCoroutine(RevealThenMoveToTrash(securityOwner, revealedSecurityCard));
        }

        #endregion

        #region 시큐리티 공개 및 트래시 이동

        // 공개된 시큐리티 카드를 앞면으로 표시하고 처리를 시작
        public bool TryRevealSecurityCard(PlayerGameData securityOwner, CardRuntimeData securityCard)
        {
            if (isResolvingReveal || securityOwner == null || securityCard == null)
            {
                return false;
            }

            StartCoroutine(RevealThenMoveToTrash(securityOwner, securityCard));
            return true;
        }

        // 시큐리티 카드 한 장을 공개하고 1초 뒤 트리거 없이 트래시로 이동
        private IEnumerator RevealThenMoveToTrash(PlayerGameData securityOwner, CardRuntimeData securityCard)
        {
            isResolvingReveal = true;

            if (securityOwner.security == null)
            {
                Debug.LogError("SecurityAreaView: 시큐리티 데이터가 없습니다.");
                isResolvingReveal = false;
                yield break;
            }

            int cardIndex = securityOwner.security.IndexOf(securityCard);

            if (cardIndex < 0 || cardIndex >= securityCardViews.Count || securityCardViews[cardIndex] == null)
            {
                Debug.LogError("SecurityAreaView: 공개할 시큐리티 슬롯을 찾지 못했습니다.");
                isResolvingReveal = false;
                yield break;
            }

            SecurityCardView cardView = securityCardViews[cardIndex];
            cardView.ShowFace(securityCard);

            Debug.Log($"SecurityAreaView: 시큐리티 공개. 카드 = {securityCard.cardData.CardName}. {revealDuration:0.##}초 뒤 트래시로 이동합니다. 트리거는 처리하지 않습니다.");

            yield return new WaitForSecondsRealtime(Mathf.Max(0f, revealDuration));

            if (BattleManager.Instance == null || !BattleManager.Instance.MoveRevealedSecurityToTrash(securityOwner, securityCard))
            {
                Debug.LogError("SecurityAreaView: 공개한 시큐리티 카드를 트래시로 이동하지 못했습니다.");
                cardView.ShowBack();
                isResolvingReveal = false;
                yield break;
            }

            SetSecurityCards(securityOwner.security);
            isResolvingReveal = false;
        }

        #endregion
    }
}