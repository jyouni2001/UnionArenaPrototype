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

            // 시작 페이즈에서 엑스트라 드로우 페이즈로 자동 진입한 상태인지 확인
            if (battleManager.CurrentTurnPlayer != opponent ||
                battleManager.CurrentTurnPhase != BattleEnums.TurnPhase.ExtraDraw)
            {
                Debug.LogWarning("BattleAIService: 엑스트라 드로우 페이즈가 아닙니다.");
                yield break;
            }

            // AI는 현재 엑스트라 드로우를 선택하지 않고 건너뜀
            if (!battleManager.AdvanceOpponentTurnPhase())
            {
                Debug.LogWarning("BattleAIService: 이동 페이즈로 진행하지 못했습니다.");
                yield break;
            }

            yield return new WaitForSeconds(ActionDelay);

            // 에너지라인에 카드가 있으면 이동 페이즈를 거쳐 메인으로 진행
            if (battleManager.CurrentTurnPlayer == opponent &&
                battleManager.CurrentTurnPhase == BattleEnums.TurnPhase.Move)
            {
                if (!battleManager.AdvanceOpponentTurnPhase())
                {
                    Debug.LogWarning("BattleAIService: 메인 페이즈로 이동하지 못했습니다.");
                    yield break;
                }
            }

            // 자동 페이즈 진행으로 턴이 변경되었거나 메인에 도달하지 못한 경우 종료
            if (battleManager.CurrentTurnPlayer != opponent ||
                battleManager.CurrentTurnPhase != BattleEnums.TurnPhase.Main)
            {
                yield break;
            }

            yield return new WaitForSeconds(ActionDelay);

            // 메인 페이즈에서 낼 수 있는 카드를 반복해서 소환
            while (TryPlayLowestCostCard(opponent))
            {
                yield return new WaitForSeconds(ActionDelay);

                if (battleManager.IsGameOver ||
                    battleManager.CurrentTurnPlayer != opponent ||
                    battleManager.CurrentTurnPhase != BattleEnums.TurnPhase.Main)
                {
                    yield break;
                }
            }

            // 프론트라인이 비어 있으면 BattleManager가 배틀을 건너뛰고 턴을 넘김
            if (!battleManager.AdvanceOpponentTurnPhase())
            {
                Debug.LogWarning("BattleAIService: 배틀 페이즈로 이동하지 못했습니다.");
                yield break;
            }

            // 배틀을 건너뛰어 다음 플레이어 턴으로 넘어간 경우 AI 행동 종료
            if (battleManager.CurrentTurnPlayer != opponent ||
                battleManager.CurrentTurnPhase != BattleEnums.TurnPhase.Battle)
            {
                yield break;
            }

            yield return new WaitForSeconds(ActionDelay);

            // 상대 프론트라인의 Active 카드를 공격자로 처리
            if (opponent.field != null)
            {
                CardRuntimeData[] attackers = opponent.field.ToArray();

                for (int i = 0; i < attackers.Length; i++)
                {
                    if (battleManager.IsGameOver ||
                        battleManager.CurrentTurnPlayer != opponent ||
                        battleManager.CurrentTurnPhase != BattleEnums.TurnPhase.Battle)
                    {
                        yield break;
                    }

                    CardRuntimeData attacker = attackers[i];

                    if (attacker == null ||
                        attacker.fieldPosition != BattleEnums.FieldPosition.FrontLine ||
                        attacker.cardState != CardEnums.CardState.Active)
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

                    // 내 블록 선택이 끝날 때까지 대기
                    if (battleManager.IsWaitingForBlock)
                    {
                        while (battleManager.IsWaitingForBlock)
                        {
                            if (battleManager.IsGameOver)
                            {
                                yield break;
                            }

                            yield return null;
                        }
                    }
                    else if (revealedCard != null && securityOwner != null)
                    {
                        if (UIManager.Instance != null &&
                            UIManager.Instance.TryRevealSecurityCard(securityOwner, revealedCard))
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

            // 배틀 페이즈 종료 요청: 엔드 처리 후 BattleManager가 자동으로 다음 턴을 시작
            if (!battleManager.IsGameOver &&
                battleManager.CurrentTurnPlayer == opponent &&
                battleManager.CurrentTurnPhase == BattleEnums.TurnPhase.Battle)
            {
                if (!battleManager.AdvanceOpponentTurnPhase())
                {
                    Debug.LogWarning("BattleAIService: AI 엔드 페이즈 처리에 실패했습니다.");
                }
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

                if (candidate == null ||
                    candidate.cardData == null ||
                    candidate.cardData.CardType != CardEnums.CardType.Character)
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

            if (slotIndex < 0 ||
                !cardZoneManager.MoveCard(player, selectedCard, BattleEnums.CardZone.Field,
                    BattleEnums.FieldPosition.FrontLine, slotIndex))
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

                    if (fieldCard != null &&
                        fieldCard.fieldPosition == position &&
                        fieldCard.fieldSlotIndex == slotIndex)
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
