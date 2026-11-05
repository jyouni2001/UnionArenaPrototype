using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using Battle;
using Enums;
using Manager;

namespace UI
{
    // 상대 시큐리티의 공격 드롭과 공개 카드 표시를 담당
    public class SecurityAreaView : MonoBehaviour, IDropHandler
    {
        #region UI 설정

        // 공개된 시큐리티 카드를 잠시 표시할 카드 UI
        [Header("공개 시큐리티 카드 UI")]
        [SerializeField]
        private CardView revealedSecurityCardView;

        // 공개 카드를 화면에 표시할 시간
        [Header("공개 표시 시간")]
        [SerializeField]
        private float revealDuration = 0.8f;

        #endregion

        #region 드래그 공격 처리

        // 내 프론트라인 카드를 놓으면 공격을 선언
        public void OnDrop(PointerEventData eventData)
        {
            if (eventData == null ||
                eventData.pointerDrag == null)
            {
                return;
            }

            if (BattleManager.Instance == null)
            {
                Debug.LogError(
                    "SecurityAreaView: BattleManager를 찾을 수 없습니다.");
                return;
            }

            // 드래그한 카드의 상위 오브젝트에서 원래 필드 슬롯을 찾음
            FieldSlotView sourceSlot =
                eventData.pointerDrag.GetComponentInParent<FieldSlotView>();

            if (sourceSlot == null ||
                !sourceSlot.TryGetLocalFieldCard(
                    out PlayerGameData attackerPlayer,
                    out CardRuntimeData attacker))
            {
                return;
            }

            if (!BattleManager.Instance.TryDeclareAttack(
                    attackerPlayer,
                    attacker,
                    out BattleCombatService.Resolution resolution,
                    out PlayerGameData securityOwner,
                    out CardRuntimeData revealedSecurityCard,
                    out string errorMessage))
            {
                Debug.LogWarning(
                    "SecurityAreaView: " + errorMessage);
                return;
            }

            // 공격 선언으로 바뀐 Rest 상태를 슬롯에 표시
            sourceSlot.ShowCard(attacker);

            if (resolution.Type ==
                BattleEnums.CombatResolutionType.AttackerWins)
            {
                Debug.Log(
                    "SecurityAreaView: 블록 카드가 트래시로 이동했습니다.");
                return;
            }

            if (resolution.Type ==
                BattleEnums.CombatResolutionType.AttackFails)
            {
                Debug.Log(
                    "SecurityAreaView: 블록되어 시큐리티 피해가 없습니다.");
                return;
            }

            if (resolution.Type ==
                BattleEnums.CombatResolutionType.UnblockedAttack)
            {
                if (revealedSecurityCard == null)
                {
                    Debug.LogWarning(
                        "SecurityAreaView: 상대 시큐리티가 비어 있습니다.");
                    return;
                }

                StartCoroutine(
                    RevealThenMoveToTrash(
                        securityOwner,
                        revealedSecurityCard));
            }
        }

        #endregion

        #region 시큐리티 공개 처리

        // 시큐리티 카드를 공개한 뒤 트리거 없이 트래시로 이동
        private IEnumerator RevealThenMoveToTrash(
            PlayerGameData securityOwner,
            CardRuntimeData securityCard)
        {
            if (securityOwner == null ||
                securityCard == null)
            {
                yield break;
            }

            if (revealedSecurityCardView != null)
            {
                revealedSecurityCardView.SetCard(
                    securityCard.cardData.CardID);

                revealedSecurityCardView.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning(
                    "SecurityAreaView: 공개 카드 UI가 연결되지 않아 콘솔에만 공개합니다.");
            }

            Debug.Log(
                $"SecurityAreaView: 시큐리티 공개. " +
                $"카드 = {securityCard.cardData.CardName}. 트리거는 이번 단계에서 처리하지 않습니다.");

            yield return new WaitForSecondsRealtime(
                Mathf.Max(0f, revealDuration));

            if (!BattleManager.Instance.MoveRevealedSecurityToTrash(
                    securityOwner,
                    securityCard))
            {
                Debug.LogError(
                    "SecurityAreaView: 공개된 시큐리티 카드를 트래시로 이동하지 못했습니다.");
            }

            if (revealedSecurityCardView != null)
            {
                revealedSecurityCardView.gameObject.SetActive(false);
            }
        }

        #endregion
    }
}