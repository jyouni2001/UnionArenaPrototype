using System.Collections;
using Battle;
using Enums;
using Manager;
using UnityEngine;

namespace Battle
{
    // 상대 플레이어의 카드 소환과 공격 순서를 처리
    internal sealed class BattleAIService
    {
        #region AI 처리 설정

        private const float ActionDelay = 0.4f; // AI 행동 사이 대기 시간
        private const float SecurityRevealWait = 1.1f; // 시큐리티 공개 UI가 끝날 때까지 기다리는 시간

        private readonly MonoBehaviour coroutineOwner; // 코루틴 실행 주체
        private readonly BattleManager battleManager; // 전투 요청을 전달할 매니저
        private readonly CardZoneManager cardZoneManager; // 카드 영역 이동 매니저

        // 필요한 매니저 참조를 전달받아 초기화
        public BattleAIService(MonoBehaviour coroutineOwner, BattleManager battleManager, CardZoneManager cardZoneManager)
        {
            this.coroutineOwner = coroutineOwner;
            this.battleManager = battleManager;
            this.cardZoneManager = cardZoneManager;
        }

        #endregion

        #region 상대 턴 처리

        // 상대 턴 동안 낼 수 있는 카드를 소환하고 액티브 카드를 공격시킴
        public IEnumerator RunTurn(PlayerGameData opponent, PlayerGameData localPlayer)
        {
            if (opponent == null || localPlayer == null || cardZoneManager == null || battleManager == null)
            {
                Debug.LogError("BattleAIService: AI 턴 처리에 필요한 데이터가 없습니다.");
                yield break;
            }

            // 시작 페이즈에서 엑스트라 드로우와 이동 페이즈를 거쳐 메인 페이즈로 이동
            if (!battleManager.AdvanceOpponentTurnPhase())
            {
                Debug.LogWarning("BattleAIService: 시작 페이즈를 진행하지 못했습니다.");
                yield break;
            }

            yield return new WaitForSeconds(ActionDelay);

            if (!battleManager.AdvanceOpponentTurnPhase() || !battleManager.AdvanceOpponentTurnPhase())
            {
                Debug.LogWarning("BattleAIService: 메인 페이즈로 이동하지 못했습니다.");
                yield break;
            }

            yield return new WaitForSeconds(ActionDelay);

            while (TryPlayLowestCostCard(opponent))
            {
                yield return new WaitForSeconds(ActionDelay);
            }

            if (!battleManager.AdvanceOpponentTurnPhase())
            {
                Debug.LogWarning("BattleAIService: 배틀 페이즈로 이동하지 못했습니다.");
                yield break;
            }

            yield return new WaitForSeconds(ActionDelay);

            if (opponent.field != null)
            {
                CardRuntimeData[] attackers = opponent.field.ToArray();

                for (int i = 0; i < attackers.Length; i++)
                {
                    CardRuntimeData attacker = attackers[i];

                    if (attacker == null || attacker.fieldPosition != BattleEnums.FieldPosition.FrontLine || attacker.cardState != CardEnums.CardState.Active)
                    {
                        continue;
                    }

                    if (!battleManager.TryDeclareAttack(opponent, attacker,
                            out BattleCombatService.Resolution resolution,
                            out PlayerGameData securityOwner,
                            out CardRuntimeData revealedCard,
                            out string errorMessage))
                    {
                        Debug.LogWarning($"BattleAIService: 공격 실패. {errorMessage}");
                        continue;
                    }

                    if (battleManager.IsWaitingForBlock)
                    {
                        while (battleManager.IsWaitingForBlock)
                        {
                            yield return null;
                        }
                    }
                    else if (revealedCard != null && securityOwner != null)
                    {
                        if (UIManager.Instance != null && UIManager.Instance.TryRevealSecurityCard(securityOwner, revealedCard))
                        {
                            yield return new WaitForSecondsRealtime(SecurityRevealWait);
                        }
                        else
                        {
                            Debug.LogWarning("BattleAIService: 시큐리티 UI를 찾지 못해 공개 연출 없이 트래시로 이동합니다.");
                            battleManager.MoveRevealedSecurityToTrash(securityOwner, revealedCard);
                        }
                    }

                    yield return new WaitForSecondsRealtime(SecurityRevealWait);
                    yield return new WaitForSeconds(ActionDelay);
                }
            }

            // 배틀 후 엔드 페이즈를 처리하고 내 시작 페이즈로 턴 전달
            if (!battleManager.AdvanceOpponentTurnPhase() || !battleManager.AdvanceOpponentTurnPhase())
            {
                Debug.LogWarning("BattleAIService: AI 턴을 정상적으로 종료하지 못했습니다.");
            }
        }

        #endregion

        #region 카드 소환

        // 낼 수 있는 캐릭터 중 AP 비용이 가장 낮은 카드를 소환
        private bool TryPlayLowestCostCard(PlayerGameData player)
        {
            if (player == null || player.hand == null || player.field == null || player.activeAP <= 0)
            {
                return false;
            }

            CardRuntimeData selectedCard = null;

            for (int i = 0; i < player.hand.Count; i++)
            {
                CardRuntimeData candidate = player.hand[i];

                if (candidate == null || candidate.cardData == null || candidate.cardData.CardType != CardEnums.CardType.Character)
                {
                    continue;
                }

                int apCost = candidate.cardData.RequiredApCost;

                if (apCost < 0 || apCost > player.activeAP)
                {
                    continue;
                }

                if (!EnergyRuleService.HasEnoughEnergy(player, candidate.cardData, out string energyError))
                {
                    continue;
                }

                if (FindEmptySlot(player, BattleEnums.FieldPosition.FrontLine) < 0)
                {
                    continue;
                }

                if (selectedCard == null || apCost < selectedCard.cardData.RequiredApCost)
                {
                    selectedCard = candidate;
                }
            }

            if (selectedCard == null)
            {
                return false;
            }

            int slotIndex = FindEmptySlot(player, BattleEnums.FieldPosition.FrontLine);

            if (slotIndex < 0 || !cardZoneManager.MoveCard(player, selectedCard, BattleEnums.CardZone.Field, BattleEnums.FieldPosition.FrontLine, slotIndex))
            {
                return false;
            }

            if (!player.TrySpendAP(selectedCard.cardData.RequiredApCost))
            {
                cardZoneManager.MoveCard(player, selectedCard, BattleEnums.CardZone.Hand);
                Debug.LogWarning("BattleAIService: AP 지불에 실패해 소환을 취소했습니다.");
                return false;
            }

            Debug.Log($"BattleAIService: 카드 소환. 카드 = {selectedCard.cardData.CardName}, 슬롯 = {slotIndex}, 남은 AP = {player.activeAP}");
            return true;
        }

        // 지정한 라인에서 첫 번째 빈 슬롯 번호를 반환
        private int FindEmptySlot(PlayerGameData player, BattleEnums.FieldPosition position)
        {
            if (player == null || player.field == null)
            {
                return -1;
            }

            for (int slotIndex = 0; slotIndex < 4; slotIndex++)
            {
                bool isOccupied = false;

                for (int i = 0; i < player.field.Count; i++)
                {
                    CardRuntimeData fieldCard = player.field[i];

                    if (fieldCard != null && fieldCard.fieldPosition == position && fieldCard.fieldSlotIndex == slotIndex)
                    {
                        isOccupied = true;
                        break;
                    }
                }

                if (!isOccupied)
                {
                    return slotIndex;
                }
            }

            return -1;
        }

        #endregion
    }
}
