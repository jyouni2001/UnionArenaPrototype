using System;
using Enums;
using Manager;
using UnityEngine;

namespace Battle
{
    // 공격 선언과 블록·시큐리티 결과 판정을 처리
    public sealed class BattleCombatService
    {
        #region 전투 결과 데이터

        // 공격 또는 블록 처리 결과
        public struct Resolution
        {
            public BattleEnums.CombatResolutionType Type { get; private set; } // 판정 결과 종류
            public CardRuntimeData Attacker { get; private set; } // 공격한 카드
            public CardRuntimeData Blocker { get; private set; } // 블록한 카드

            // 전투 결과 데이터를 생성
            public Resolution(BattleEnums.CombatResolutionType type, CardRuntimeData attacker, CardRuntimeData blocker)
            {
                Type = type;
                Attacker = attacker;
                Blocker = blocker;
            }
        }

        #endregion

        #region 전투 상태

        private readonly CardZoneManager cardZoneManager; // 카드 영역 이동 매니저
        private PlayerGameData attackingPlayer; // 공격 선언한 플레이어
        private PlayerGameData defendingPlayer; // 방어 중인 플레이어
        private CardRuntimeData attackingCard; // 현재 공격 카드

        public event Action<PlayerGameData> OnFieldStateChanged; // 필드 카드 상태 변경 알림
        public bool IsWaitingForBlock => attackingCard != null; // 블록 대기 여부
        public CardRuntimeData AttackingCard => attackingCard; // 현재 공격 카드

        // 전투에 사용할 카드 영역 매니저를 전달받아 초기화
        public BattleCombatService(CardZoneManager cardZoneManager)
        {
            this.cardZoneManager = cardZoneManager;
        }

        #endregion

        #region 공격 선언

        // Active 상태의 FrontLine 카드로 공격을 선언하고 Rest 처리
        public bool TryDeclareAttack(PlayerGameData attackerPlayer, PlayerGameData defenderPlayer, CardRuntimeData attacker, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (IsWaitingForBlock)
            {
                errorMessage = "현재 공격 처리가 끝나지 않았습니다.";
                return false;
            }

            if (attackerPlayer == null || defenderPlayer == null || attacker == null || attacker.cardData == null)
            {
                errorMessage = "공격에 필요한 데이터가 없습니다.";
                return false;
            }

            if (attackerPlayer.field == null || !attackerPlayer.field.Contains(attacker) ||
                attacker.currentZone != BattleEnums.CardZone.Field ||
                attacker.fieldPosition != BattleEnums.FieldPosition.FrontLine ||
                attacker.cardState != CardEnums.CardState.Active ||
                attacker.ownerPlayerIndex != attackerPlayer.playerIndex)
            {
                errorMessage = "Active 상태의 FrontLine 카드만 공격할 수 있습니다.";
                return false;
            }

            attackingPlayer = attackerPlayer;
            defendingPlayer = defenderPlayer;
            attackingCard = attacker;
            attacker.cardState = CardEnums.CardState.Rest;

            OnFieldStateChanged?.Invoke(attackerPlayer);
            Debug.Log($"BattleCombatService: 공격 선언. 카드 = {attacker.cardData.CardName}, BP = {attacker.cardData.BP}");
            return true;
        }

        #endregion

        #region 방어 카드 조회

        // 방어자의 Active FrontLine 카드 중 공격을 이길 수 있는 최저 BP 카드를 반환
        public CardRuntimeData FindBestBlocker()
        {
            if (!IsWaitingForBlock || defendingPlayer == null || defendingPlayer.field == null)
            {
                return null;
            }

            CardRuntimeData bestBlocker = null;

            for (int i = 0; i < defendingPlayer.field.Count; i++)
            {
                CardRuntimeData candidate = defendingPlayer.field[i];

                if (!CanBlock(candidate) || candidate.cardData.BP <= attackingCard.cardData.BP)
                {
                    continue;
                }

                if (bestBlocker == null || candidate.cardData.BP < bestBlocker.cardData.BP)
                {
                    bestBlocker = candidate;
                }
            }

            return bestBlocker;
        }

        // 카드가 현재 공격을 막을 수 있는 Active FrontLine 카드인지 확인
        public bool IsEligibleBlocker(CardRuntimeData candidate)
        {
            return CanBlock(candidate);
        }

        // 방어 카드의 현재 상태와 소유자를 검사
        private bool CanBlock(CardRuntimeData candidate)
        {
            return IsWaitingForBlock &&
                   candidate != null &&
                   candidate.cardData != null &&
                   defendingPlayer != null &&
                   defendingPlayer.field != null &&
                   defendingPlayer.field.Contains(candidate) &&
                   candidate.currentZone == BattleEnums.CardZone.Field &&
                   candidate.fieldPosition == BattleEnums.FieldPosition.FrontLine &&
                   candidate.cardState == CardEnums.CardState.Active &&
                   candidate.ownerPlayerIndex == defendingPlayer.playerIndex;
        }

        #endregion

        #region 블록 결과 처리

        // 선택한 방어 카드와 공격 카드의 BP를 비교하고 방어 카드를 Rest 처리
        public bool TryResolveBlock(CardRuntimeData blocker, out Resolution resolution, out string errorMessage)
        {
            resolution = new Resolution(BattleEnums.CombatResolutionType.None, attackingCard, blocker);
            errorMessage = string.Empty;

            if (!IsWaitingForBlock)
            {
                errorMessage = "처리할 공격이 없습니다.";
                return false;
            }

            if (!CanBlock(blocker))
            {
                errorMessage = "Active 상태의 방어자 FrontLine 카드만 블록할 수 있습니다.";
                return false;
            }

            CardRuntimeData resolvedAttacker = attackingCard;
            blocker.cardState = CardEnums.CardState.Rest;
            OnFieldStateChanged?.Invoke(defendingPlayer);

            if (resolvedAttacker.cardData.BP >= blocker.cardData.BP)
            {
                if (cardZoneManager == null || !cardZoneManager.MoveCard(defendingPlayer, blocker, BattleEnums.CardZone.Trash))
                {
                    blocker.cardState = CardEnums.CardState.Active;
                    OnFieldStateChanged?.Invoke(defendingPlayer);
                    errorMessage = "블록 카드를 트래시로 이동하지 못했습니다.";
                    return false;
                }

                resolution = new Resolution(BattleEnums.CombatResolutionType.AttackerWins, resolvedAttacker, blocker);
                Debug.Log($"BattleCombatService: 공격자 승리. 공격 BP = {resolvedAttacker.cardData.BP}, 방어 BP = {blocker.cardData.BP}");
            }
            else
            {
                resolution = new Resolution(BattleEnums.CombatResolutionType.AttackFails, resolvedAttacker, blocker);
                Debug.Log($"BattleCombatService: 공격 실패. 공격 BP = {resolvedAttacker.cardData.BP}, 방어 BP = {blocker.cardData.BP}");
            }

            ClearPendingAttack();
            return true;
        }

        // 블록하지 않고 공격을 통과시켜 시큐리티 카드를 반환
        public bool TryPassBlock(out Resolution resolution, out PlayerGameData targetPlayer, out CardRuntimeData revealedSecurityCard, out string errorMessage)
        {
            resolution = new Resolution(BattleEnums.CombatResolutionType.None, attackingCard, null);
            targetPlayer = defendingPlayer;
            revealedSecurityCard = null;
            errorMessage = string.Empty;

            if (!IsWaitingForBlock)
            {
                errorMessage = "처리할 공격이 없습니다.";
                return false;
            }

            if (defendingPlayer.security == null)
            {
                errorMessage = "방어자의 시큐리티 데이터가 없습니다.";
                return false;
            }

            resolution = new Resolution(BattleEnums.CombatResolutionType.UnblockedAttack, attackingCard, null);

            if (defendingPlayer.security.Count > 0)
            {
                revealedSecurityCard = defendingPlayer.security[0];
                Debug.Log($"BattleCombatService: 시큐리티 카드 공개 대기. 카드 = {revealedSecurityCard.cardData.CardName}");
            }
            else
            {
                Debug.LogWarning("BattleCombatService: 공개할 시큐리티 카드가 없습니다.");
            }

            ClearPendingAttack();
            return true;
        }

        #endregion

        #region 전투 상태 초기화

        // 진행 중인 공격 참조를 정리
        private void ClearPendingAttack()
        {
            attackingPlayer = null;
            defendingPlayer = null;
            attackingCard = null;
        }

        #endregion
    }
}