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
            // 판정 결과 종류
            public BattleEnums.CombatResolutionType Type { get; private set; }

            // 공격한 카드
            public CardRuntimeData Attacker { get; private set; }

            // 블록한 카드, 블록이 없으면 null
            public CardRuntimeData Blocker { get; private set; }

            // 전투 결과 데이터를 생성
            public Resolution(
                BattleEnums.CombatResolutionType type,
                CardRuntimeData attacker,
                CardRuntimeData blocker)
            {
                Type = type;
                Attacker = attacker;
                Blocker = blocker;
            }
        }

        #endregion

        #region 전투 상태

        // 카드 영역 이동을 요청할 매니저
        private readonly CardZoneManager cardZoneManager;

        // 현재 공격을 선언한 플레이어
        private PlayerGameData attackingPlayer;

        // 현재 방어 중인 플레이어
        private PlayerGameData defendingPlayer;

        // 현재 공격 카드
        private CardRuntimeData attackingCard;

        // 공격 카드 상태 변경을 매니저에 알리는 이벤트
        public event Action<PlayerGameData> OnFieldStateChanged;

        // 블록 판정을 기다리는 공격이 있는지 반환
        public bool IsWaitingForBlock => attackingCard != null;

        // 현재 공격 카드 반환
        public CardRuntimeData AttackingCard => attackingCard;

        // 전투에 사용할 카드 영역 매니저를 전달받아 초기화
        public BattleCombatService(CardZoneManager cardZoneManager)
        {
            this.cardZoneManager = cardZoneManager;
        }

        #endregion

        #region 공격 선언

        // Active 상태의 내 FrontLine 카드로 공격을 선언하고 Rest 처리
        public bool TryDeclareAttack(
            PlayerGameData attackerPlayer,
            PlayerGameData defenderPlayer,
            CardRuntimeData attacker,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            if (IsWaitingForBlock)
            {
                errorMessage = "현재 공격 처리가 끝나지 않았습니다.";
                return false;
            }

            if (attackerPlayer == null ||
                defenderPlayer == null ||
                attacker == null ||
                attacker.cardData == null)
            {
                errorMessage = "공격에 필요한 데이터가 없습니다.";
                return false;
            }

            if (attackerPlayer.field == null ||
                !attackerPlayer.field.Contains(attacker) ||
                attacker.currentZone != BattleEnums.CardZone.Field ||
                attacker.fieldPosition != BattleEnums.FieldPosition.FrontLine ||
                attacker.cardState != CardEnums.CardState.Active ||
                attacker.ownerPlayerIndex != attackerPlayer.playerIndex)
            {
                errorMessage =
                    "Active 상태의 내 프론트라인 카드만 공격할 수 있습니다.";
                return false;
            }

            attackingPlayer = attackerPlayer;
            defendingPlayer = defenderPlayer;
            attackingCard = attacker;

            attacker.cardState = CardEnums.CardState.Rest;

            // 공격자의 Rest 상태가 바뀌었음을 매니저와 UI에 알림
            OnFieldStateChanged?.Invoke(attackerPlayer);

            Debug.Log(
                $"BattleCombatService: 공격 선언. " +
                $"카드 = {attacker.cardData.CardName}, " +
                $"BP = {attacker.cardData.BP}");

            return true;
        }

        #endregion

        #region 방어 AI 블록 선택

        // 공격자를 이길 수 있는 방어 카드 중 BP가 가장 낮은 카드 선택
        public CardRuntimeData FindBestBlocker()
        {
            if (!IsWaitingForBlock ||
                defendingPlayer == null ||
                defendingPlayer.field == null)
            {
                return null;
            }

            CardRuntimeData bestBlocker = null;

            for (int i = 0; i < defendingPlayer.field.Count; i++)
            {
                CardRuntimeData candidate =
                    defendingPlayer.field[i];

                if (candidate == null ||
                    candidate.cardData == null ||
                    candidate.currentZone != BattleEnums.CardZone.Field ||
                    candidate.fieldPosition !=
                        BattleEnums.FieldPosition.FrontLine ||
                    candidate.cardState != CardEnums.CardState.Active ||
                    candidate.ownerPlayerIndex !=
                        defendingPlayer.playerIndex)
                {
                    continue;
                }

                // 공격 BP보다 높은 카드 중에서만 블록 카드를 고름
                if (candidate.cardData.BP <= attackingCard.cardData.BP)
                {
                    continue;
                }

                // 공격을 막을 수 있는 카드 중 가장 낮은 BP를 선택
                if (bestBlocker == null ||
                    candidate.cardData.BP < bestBlocker.cardData.BP)
                {
                    bestBlocker = candidate;
                }
            }

            return bestBlocker;
        }

        #endregion

        #region 블록 결과 처리

        // 선택한 방어 카드와 공격 카드의 BP를 비교
        public bool TryResolveBlock(
            CardRuntimeData blocker,
            out Resolution resolution,
            out string errorMessage)
        {
            resolution = new Resolution(
                BattleEnums.CombatResolutionType.None,
                attackingCard,
                blocker);

            errorMessage = string.Empty;

            if (!IsWaitingForBlock)
            {
                errorMessage = "처리할 공격이 없습니다.";
                return false;
            }

            if (blocker == null ||
                blocker.cardData == null ||
                defendingPlayer == null ||
                defendingPlayer.field == null)
            {
                errorMessage = "블록 카드 또는 방어자 데이터가 없습니다.";
                return false;
            }

            if (!defendingPlayer.field.Contains(blocker) ||
                blocker.currentZone != BattleEnums.CardZone.Field ||
                blocker.fieldPosition != BattleEnums.FieldPosition.FrontLine ||
                blocker.cardState != CardEnums.CardState.Active ||
                blocker.ownerPlayerIndex != defendingPlayer.playerIndex)
            {
                errorMessage =
                    "Active 상태의 방어자 프론트라인 카드만 블록할 수 있습니다.";
                return false;
            }

            CardRuntimeData resolvedAttacker = attackingCard;

            // 공격 BP가 같거나 높으면 방어 카드만 트래시로 이동
            if (resolvedAttacker.cardData.BP >= blocker.cardData.BP)
            {
                if (cardZoneManager == null ||
                    !cardZoneManager.MoveCard(
                        defendingPlayer,
                        blocker,
                        BattleEnums.CardZone.Trash))
                {
                    errorMessage = "블록 카드를 트래시로 이동하지 못했습니다.";
                    return false;
                }

                resolution = new Resolution(
                    BattleEnums.CombatResolutionType.AttackerWins,
                    resolvedAttacker,
                    blocker);

                Debug.Log(
                    $"BattleCombatService: 공격자 승리. " +
                    $"공격 BP = {resolvedAttacker.cardData.BP}, " +
                    $"방어 BP = {blocker.cardData.BP}");
            }
            else
            {
                // 방어 BP가 더 높으면 양쪽 카드가 필드에 남고 공격 실패
                resolution = new Resolution(
                    BattleEnums.CombatResolutionType.AttackFails,
                    resolvedAttacker,
                    blocker);

                Debug.Log(
                    $"BattleCombatService: 공격 실패. " +
                    $"공격 BP = {resolvedAttacker.cardData.BP}, " +
                    $"방어 BP = {blocker.cardData.BP}");
            }

            ClearPendingAttack();
            return true;
        }

        // 블록하지 않고 공격을 통과시킴
        public bool TryPassBlock(
            out Resolution resolution,
            out PlayerGameData targetPlayer,
            out CardRuntimeData revealedSecurityCard,
            out string errorMessage)
        {
            resolution = new Resolution(
                BattleEnums.CombatResolutionType.None,
                attackingCard,
                null);

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
                errorMessage = "상대 시큐리티 데이터가 없습니다.";
                return false;
            }

            resolution = new Resolution(
                BattleEnums.CombatResolutionType.UnblockedAttack,
                attackingCard,
                null);

            // 시큐리티 맨 위 카드를 반환해 UI가 공개 후 트래시로 보낼 수 있게 함
            if (defendingPlayer.security.Count > 0)
            {
                revealedSecurityCard = defendingPlayer.security[0];

                Debug.Log(
                    $"BattleCombatService: 시큐리티 카드 공개. " +
                    $"카드 = {revealedSecurityCard.cardData.CardName}");
            }
            else
            {
                Debug.LogWarning(
                    "BattleCombatService: 공개할 시큐리티 카드가 없습니다.");
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