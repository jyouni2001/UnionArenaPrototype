using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Battle;
using Data;
using Enums;

namespace Manager
{
    // 전투 상태와 게임 서비스 요청을 조율
    public class BattleManager : BaseManager
    {
        #region 싱글톤 및 전투 상태

        public static BattleManager Instance { get; private set; } // 전투 매니저 싱글톤

        private const int MaxPlayerCount = 2; // 전투 참여 플레이어 수
        private const string TestDeckID = "a64e00e5f2414498848063059de16ae2"; // 테스트 덱 ID

        private readonly List<PlayerGameData> playerGameDataList = new List<PlayerGameData>(); // 플레이어별 전투 데이터

        private PlayerGameData localPlayerData; // 내 플레이어 데이터
        private PlayerGameData opponentPlayerData; // 상대 플레이어 데이터
        private PlayerGameData currentTurnPlayer; // 현재 턴 플레이어 데이터
        private PlayerGameData winnerPlayerData; // 승리한 플레이어 데이터
        private BattleEnums.PlayerType localPlayerType; // 로컬 플레이어 타입
        private BattleEnums.TurnPhase currentTurnPhase = BattleEnums.TurnPhase.None; // 현재 턴 페이즈
        private bool isWaitingForInitialHandChoice; // 최초 손패 선택 대기 여부
        private bool isRunningOpponentTurn; // 상대 AI 턴 진행 여부
        private bool isEndPhaseProcessed; // 엔드 페이즈 처리 여부
        private bool isGameOver; // 게임 종료 여부

        private BattleSetupService battleSetupService; // 덱과 최초 손패 준비 서비스
        private TurnFlowService turnFlowService; // 턴 진행 서비스
        private BattleCombatService battleCombatService; // 공격 및 블록 처리 서비스
        private BattleAIService battleAIService; // 상대 턴 행동 서비스
        private Coroutine opponentTurnCoroutine; // 상대 AI 턴 코루틴

        public IReadOnlyList<PlayerGameData> PlayerGameDataList => playerGameDataList; // 플레이어 데이터 목록
        public PlayerGameData LocalPlayerData => localPlayerData; // 내 플레이어 데이터
        public PlayerGameData OpponentPlayerData => opponentPlayerData; // 상대 플레이어 데이터
        public PlayerGameData CurrentTurnPlayer => currentTurnPlayer; // 현재 턴 플레이어 데이터
        public PlayerGameData WinnerPlayerData => winnerPlayerData; // 승리한 플레이어 데이터
        public BattleEnums.PlayerType LocalPlayerType => localPlayerType; // 로컬 플레이어 타입
        public BattleEnums.TurnPhase CurrentTurnPhase => currentTurnPhase; // 현재 턴 페이즈
        public bool CanTakeExtraDraw => !isGameOver && currentTurnPlayer == localPlayerData && currentTurnPhase == BattleEnums.TurnPhase.ExtraDraw && localPlayerData != null && !localPlayerData.hasTakenExtraDrawThisTurn && localPlayerData.activeAP >= 1 && localPlayerData.deck != null && localPlayerData.deck.Count > 0; // 엑스트라 드로우 가능 여부
        public bool IsWaitingForInitialHandChoice => isWaitingForInitialHandChoice; // 최초 손패 선택 대기 여부
        public bool IsWaitingForBlock => battleCombatService != null && battleCombatService.IsWaitingForBlock; // 블록 대기 여부
        public bool IsRunningOpponentTurn => isRunningOpponentTurn; // 상대 AI 턴 진행 여부
        public bool IsGameOver => isGameOver; // 게임 종료 여부

        public event Action<PlayerGameData> OnCombatFieldChanged; // 전투 카드 상태 변경 알림
        public event Action<CardRuntimeData> OnBlockChoiceRequested; // 블록 선택 UI 요청
        public event Action<PlayerGameData, PlayerGameData> OnGameEnded; // 승패 확정 알림
        public event Action<BattleEnums.TurnPhase> OnTurnPhaseChanged; // 턴 페이즈 변경 알림

        #endregion

        #region 생명 주기 및 서비스 연결

        // 싱글톤을 등록하고 초기화를 시작
        protected override void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            base.Awake();
        }

        // 코루틴과 이벤트 연결을 해제하고 싱글톤 참조를 정리
        private void OnDestroy()
        {
            if (opponentTurnCoroutine != null)
            {
                StopCoroutine(opponentTurnCoroutine);
                opponentTurnCoroutine = null;
            }

            if (battleCombatService != null)
            {
                battleCombatService.OnFieldStateChanged -= HandleCombatFieldStateChanged;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        // 플레이어 데이터를 만들고 기본 참조를 연결
        protected override void Initialize()
        {
            playerGameDataList.Clear();
            localPlayerData = null;
            opponentPlayerData = null;
            currentTurnPlayer = null;
            winnerPlayerData = null;
            currentTurnPhase = BattleEnums.TurnPhase.None;
            isWaitingForInitialHandChoice = false;
            isRunningOpponentTurn = false;
            isEndPhaseProcessed = false;
            isGameOver = false;
            battleSetupService = null;
            turnFlowService = null;
            battleCombatService = null;
            battleAIService = null;
            opponentTurnCoroutine = null;

            for (int i = 0; i < MaxPlayerCount; i++)
            {
                PlayerGameData playerData = new PlayerGameData { playerIndex = i }; // 플레이어별 전투 데이터
                playerGameDataList.Add(playerData);
            }

            SetLocalPlayerType(BattleEnums.PlayerType.PlayerA);
            base.Initialize();
        }

        // 필요한 매니저와 서비스를 연결
        private bool EnsureServicesReady()
        {
            if (battleSetupService != null && turnFlowService != null && battleCombatService != null && battleAIService != null)
            {
                return true;
            }

            if (DeckManager.Instance == null || CardDatabaseManager.Instance == null || CardZoneManager.Instance == null)
            {
                Debug.LogError("BattleManager: 전투 준비에 필요한 매니저가 없습니다.");
                return false;
            }

            if (battleSetupService == null)
            {
                battleSetupService = new BattleSetupService(GetPlayerGameData, DeckManager.Instance, CardDatabaseManager.Instance, CardZoneManager.Instance);
            }

            if (turnFlowService == null)
            {
                turnFlowService = new TurnFlowService(CardZoneManager.Instance);
            }

            if (battleCombatService == null)
            {
                battleCombatService = new BattleCombatService(CardZoneManager.Instance);
                battleCombatService.OnFieldStateChanged += HandleCombatFieldStateChanged;
            }

            if (battleAIService == null)
            {
                battleAIService = new BattleAIService(this, this, CardZoneManager.Instance);
            }

            return true;
        }

        // 전투 서비스의 카드 상태 변경을 외부에 전달
        private void HandleCombatFieldStateChanged(PlayerGameData playerData)
        {
            OnCombatFieldChanged?.Invoke(playerData);
        }

        // 양쪽 덱과 최초 손패를 준비하고 멀리건 선택 상태로 진입
        private void Start()
        {
            if (localPlayerData == null || opponentPlayerData == null || !EnsureServicesReady())
            {
                Debug.LogError("BattleManager: 전투 준비를 시작할 수 없습니다.");
                return;
            }

            if (!PreparePlayerDeck(localPlayerData) || !PreparePlayerDeck(opponentPlayerData))
            {
                return;
            }

            // 프로토타입에서는 상대가 최초 손패를 자동으로 유지
            if (!battleSetupService.DrawInitialHand(opponentPlayerData))
            {
                Debug.LogError("BattleManager: 상대 최초 손패 준비에 실패했습니다.");
                return;
            }

            if (!DrawInitialHand())
            {
                Debug.LogError("BattleManager: 내 최초 손패 준비에 실패했습니다.");
                return;
            }

            Debug.Log("BattleManager: 최초 손패 선택 대기 상태입니다. 아직 시큐리티는 준비되지 않았습니다.");
        }

        // 지정 플레이어의 테스트 덱을 등록하고 덱을 섞음
        private bool PreparePlayerDeck(PlayerGameData playerData)
        {
            if (playerData == null || !InitializePlayerDeck(TestDeckID, playerData.playerIndex))
            {
                return false;
            }

            if (!CardZoneManager.Instance.ShuffleDeck(playerData))
            {
                Debug.LogError($"BattleManager: 덱 셔플에 실패했습니다. PlayerIndex = {playerData.playerIndex}");
                return false;
            }

            return true;
        }

        // 양쪽 최초 손패 선택이 끝난 뒤 시큐리티 7장씩 준비
        private bool PrepareSecurityForBothPlayers()
        {
            if (localPlayerData == null || opponentPlayerData == null || battleSetupService == null)
            {
                Debug.LogError("BattleManager: 시큐리티 준비에 필요한 데이터가 없습니다.");
                return false;
            }

            if (localPlayerData.security == null || opponentPlayerData.security == null ||
                localPlayerData.security.Count > 0 || opponentPlayerData.security.Count > 0)
            {
                Debug.LogError("BattleManager: 시큐리티가 이미 준비되었거나 목록이 없습니다.");
                return false;
            }

            if (localPlayerData.deck == null || opponentPlayerData.deck == null ||
                localPlayerData.deck.Count < 7 || opponentPlayerData.deck.Count < 7)
            {
                Debug.LogError("BattleManager: 시큐리티에 놓을 카드가 부족합니다.");
                return false;
            }

            if (!battleSetupService.PrepareSecurity(localPlayerData) || !battleSetupService.PrepareSecurity(opponentPlayerData))
            {
                Debug.LogError("BattleManager: 양쪽 시큐리티 준비에 실패했습니다.");
                return false;
            }

            Debug.Log("BattleManager: 양쪽 시큐리티 7장 준비 완료.");
            return true;
        }

        #endregion

        #region 플레이어 참조 조율

        // 플레이어 번호에 해당하는 데이터를 반환
        public PlayerGameData GetPlayerGameData(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= playerGameDataList.Count)
            {
                return null;
            }

            return playerGameDataList[playerIndex];
        }

        // 로컬 플레이어와 상대 플레이어 참조를 연결
        public void SetLocalPlayerType(BattleEnums.PlayerType playerType)
        {
            if (playerType != BattleEnums.PlayerType.PlayerA && playerType != BattleEnums.PlayerType.PlayerB)
            {
                Debug.LogError("BattleManager: 올바르지 않은 PlayerType입니다.");
                return;
            }

            int localPlayerIndex = (int)playerType - 1; // 로컬 플레이어 목록 번호
            int opponentPlayerIndex = localPlayerIndex == 0 ? 1 : 0; // 상대 플레이어 목록 번호
            PlayerGameData newLocalPlayerData = GetPlayerGameData(localPlayerIndex); // 로컬 플레이어 데이터
            PlayerGameData newOpponentPlayerData = GetPlayerGameData(opponentPlayerIndex); // 상대 플레이어 데이터

            if (newLocalPlayerData == null || newOpponentPlayerData == null)
            {
                Debug.LogError("BattleManager: 플레이어 참조 연결에 실패했습니다.");
                return;
            }

            localPlayerData = newLocalPlayerData;
            opponentPlayerData = newOpponentPlayerData;
            localPlayerType = playerType;

            Debug.Log($"BattleManager: 내 플레이어 = {localPlayerType}, 상대 플레이어 = {(localPlayerIndex == 0 ? BattleEnums.PlayerType.PlayerB : BattleEnums.PlayerType.PlayerA)}");
        }

        #endregion

        #region 카드 및 최초 손패 준비

        // 카드 원본 데이터로 전투용 카드 인스턴스를 생성
        public CardRuntimeData CreateCardRuntimeData(CardJsonData cardData, int ownerPlayerIndex)
        {
            if (!EnsureServicesReady())
            {
                return null;
            }

            return battleSetupService.CreateCardRuntimeData(cardData, ownerPlayerIndex);
        }

        // 지정한 플레이어의 전투 덱을 등록
        public bool InitializePlayerDeck(string deckID, int playerIndex)
        {
            return EnsureServicesReady() && battleSetupService.InitializePlayerDeck(deckID, playerIndex);
        }

        // 내 덱에서 최초 손패 7장을 준비
        public bool DrawInitialHand()
        {
            if (localPlayerData == null || isWaitingForInitialHandChoice || !EnsureServicesReady())
            {
                return false;
            }

            if (!battleSetupService.DrawInitialHand(localPlayerData))
            {
                return false;
            }

            isWaitingForInitialHandChoice = true;
            Debug.Log("BattleManager: 최초 손패 7장 준비 완료.");
            return true;
        }

        // 최초 손패를 유지하고 양쪽 시큐리티를 준비한 뒤 첫 턴 시작
        public bool KeepInitialHand()
        {
            if (!isWaitingForInitialHandChoice || localPlayerData == null || !EnsureServicesReady())
            {
                Debug.LogWarning("BattleManager: 최초 손패 선택 상태가 아닙니다.");
                return false;
            }

            if (!PrepareSecurityForBothPlayers() || !StartLocalFirstTurn())
            {
                return false;
            }

            isWaitingForInitialHandChoice = false;
            Debug.Log("BattleManager: 최초 손패 유지 및 시큐리티 준비 완료.");
            return true;
        }

        // 최초 손패를 멀리건 처리하고 양쪽 시큐리티를 준비한 뒤 첫 턴 시작
        public bool MulliganInitialHand()
        {
            if (!isWaitingForInitialHandChoice || localPlayerData == null || !EnsureServicesReady())
            {
                Debug.LogWarning("BattleManager: 최초 손패 선택 상태가 아닙니다.");
                return false;
            }

            if (!battleSetupService.Mulligan(localPlayerData) || !PrepareSecurityForBothPlayers() || !StartLocalFirstTurn())
            {
                return false;
            }

            isWaitingForInitialHandChoice = false;
            Debug.Log("BattleManager: 멀리건 및 양쪽 시큐리티 준비 완료.");
            return true;
        }

        #endregion

        #region 공격 및 블록 요청

        // 공격을 선언하고 방어자에 따라 블록 선택 또는 AI 방어를 처리
        public bool TryDeclareAttack(PlayerGameData attackerPlayer, CardRuntimeData attacker,
            out BattleCombatService.Resolution resolution, out PlayerGameData securityOwner,
            out CardRuntimeData revealedSecurityCard, out string errorMessage)
        {
            resolution = new BattleCombatService.Resolution(BattleEnums.CombatResolutionType.None, attacker, null);
            securityOwner = null;
            revealedSecurityCard = null;
            errorMessage = string.Empty;

            if (isGameOver)
            {
                errorMessage = "이미 게임이 종료되었습니다.";
                return false;
            }

            if (!EnsureServicesReady())
            {
                errorMessage = "전투 서비스가 준비되지 않았습니다.";
                return false;
            }

            if (attackerPlayer == null || attackerPlayer != currentTurnPlayer)
            {
                errorMessage = "현재 턴 플레이어만 공격할 수 있습니다.";
                return false;
            }

            PlayerGameData defenderPlayer = attackerPlayer == localPlayerData ? opponentPlayerData : localPlayerData;

            if (!battleCombatService.TryDeclareAttack(attackerPlayer, defenderPlayer, attacker, out errorMessage))
            {
                return false;
            }

            // 상대 AI가 공격하면 내 블록 선택을 기다림
            if (attackerPlayer == opponentPlayerData)
            {
                List<CardRuntimeData> blockers = GetActiveLocalBlockers();

                if (blockers.Count > 0 && OnBlockChoiceRequested != null)
                {
                    OnBlockChoiceRequested.Invoke(attacker);
                    return true;
                }

                // 블록 UI 또는 블록 가능한 카드가 없으면 공격을 통과시킴
                return ResolvePassedAttack(attackerPlayer, out resolution, out securityOwner, out revealedSecurityCard, out errorMessage);
            }

            // 내 공격은 상대 AI가 공격을 이길 수 있는 카드로 자동 블록
            CardRuntimeData blocker = battleCombatService.FindBestBlocker();

            if (blocker != null)
            {
                return battleCombatService.TryResolveBlock(blocker, out resolution, out errorMessage);
            }

            return ResolvePassedAttack(attackerPlayer, out resolution, out securityOwner, out revealedSecurityCard, out errorMessage);
        }

        // 블록하지 않은 공격을 통과시키고 시큐리티가 없으면 게임을 종료
        private bool ResolvePassedAttack(PlayerGameData attackerPlayer, out BattleCombatService.Resolution resolution,
            out PlayerGameData securityOwner, out CardRuntimeData revealedSecurityCard, out string errorMessage)
        {
            bool success = battleCombatService.TryPassBlock(out resolution, out securityOwner, out revealedSecurityCard, out errorMessage);

            if (success && securityOwner != null && (securityOwner.security == null || securityOwner.security.Count == 0))
            {
                EndGame(attackerPlayer, securityOwner);
            }

            return success;
        }

        // 내 블록 UI에 표시할 Active FrontLine 카드를 반환
        public List<CardRuntimeData> GetActiveLocalBlockers()
        {
            List<CardRuntimeData> blockers = new List<CardRuntimeData>();

            if (!IsWaitingForBlock || localPlayerData == null || localPlayerData.field == null || battleCombatService == null)
            {
                return blockers;
            }

            for (int i = 0; i < localPlayerData.field.Count; i++)
            {
                CardRuntimeData card = localPlayerData.field[i];

                if (battleCombatService.IsEligibleBlocker(card))
                {
                    blockers.Add(card);
                }
            }

            return blockers;
        }

        // 선택한 내 필드 카드로 상대 공격을 블록
        public bool TryResolveLocalBlock(CardRuntimeData blocker, out BattleCombatService.Resolution resolution, out string errorMessage)
        {
            resolution = new BattleCombatService.Resolution(BattleEnums.CombatResolutionType.None, null, blocker);
            errorMessage = string.Empty;

            if (isGameOver || !IsWaitingForBlock || currentTurnPlayer != opponentPlayerData || battleCombatService == null)
            {
                errorMessage = "현재 내 블록 선택을 기다리는 공격이 없습니다.";
                return false;
            }

            return battleCombatService.TryResolveBlock(blocker, out resolution, out errorMessage);
        }

        // 블록하지 않고 공격을 통과시키고 시큐리티 공개 데이터를 반환
        public bool TryPassLocalBlock(out BattleCombatService.Resolution resolution,
            out PlayerGameData securityOwner, out CardRuntimeData revealedSecurityCard, out string errorMessage)
        {
            resolution = new BattleCombatService.Resolution(BattleEnums.CombatResolutionType.None, null, null);
            securityOwner = null;
            revealedSecurityCard = null;
            errorMessage = string.Empty;

            if (isGameOver || !IsWaitingForBlock || currentTurnPlayer != opponentPlayerData || battleCombatService == null)
            {
                errorMessage = "현재 통과할 공격이 없습니다.";
                return false;
            }

            return ResolvePassedAttack(opponentPlayerData, out resolution, out securityOwner, out revealedSecurityCard, out errorMessage);
        }

        // 공개한 시큐리티 카드를 트래시로 이동하고 마지막 카드였으면 게임을 종료
        public bool MoveRevealedSecurityToTrash(PlayerGameData securityOwner, CardRuntimeData securityCard)
        {
            if (isGameOver || securityOwner == null || securityCard == null || securityOwner.security == null ||
                !securityOwner.security.Contains(securityCard) || CardZoneManager.Instance == null)
            {
                return false;
            }

            if (!CardZoneManager.Instance.MoveCard(securityOwner, securityCard, BattleEnums.CardZone.Trash))
            {
                return false;
            }

            if (securityOwner.security.Count == 0)
            {
                PlayerGameData winner = GetOtherPlayer(securityOwner);
                EndGame(winner, securityOwner);
            }

            return true;
        }

        // 지정 플레이어의 상대 플레이어 데이터를 반환
        private PlayerGameData GetOtherPlayer(PlayerGameData playerData)
        {
            return playerData == localPlayerData ? opponentPlayerData : localPlayerData;
        }

        // 승리자와 패배자를 기록하고 게임 종료 이벤트를 알림
        private void EndGame(PlayerGameData winner, PlayerGameData loser)
        {
            if (isGameOver || winner == null || loser == null)
            {
                return;
            }

            isGameOver = true;
            winnerPlayerData = winner;

            if (opponentTurnCoroutine != null)
            {
                StopCoroutine(opponentTurnCoroutine);
                opponentTurnCoroutine = null;
            }

            isRunningOpponentTurn = false;

            Debug.Log($"BattleManager: 게임 종료. 승자 PlayerIndex = {winner.playerIndex}, 패자 PlayerIndex = {loser.playerIndex}");
            OnGameEnded?.Invoke(winner, loser);
        }

        #endregion

        #region 턴 요청 조율

        // 로컬 플레이어의 첫 턴을 시작
        private bool StartLocalFirstTurn()
        {
            if (localPlayerData == null || localPlayerData.turnCount > 0)
            {
                Debug.LogWarning("BattleManager: 첫 턴을 시작할 수 없거나 이미 시작되었습니다.");
                return false;
            }

            return BeginTurn(localPlayerData);
        }

        // 턴 서비스를 호출하고 현재 턴 플레이어와 시작 페이즈를 갱신
        private bool BeginTurn(PlayerGameData playerData)
        {
            if (isGameOver || playerData == null || !EnsureServicesReady())
            {
                return false;
            }

            if (!turnFlowService.BeginTurn(playerData, out string errorMessage))
            {
                Debug.LogError("BattleManager: 턴 시작 실패. " + errorMessage);
                return false;
            }

            currentTurnPlayer = playerData;
            isEndPhaseProcessed = false;
            SetTurnPhase(BattleEnums.TurnPhase.Start);

            Debug.Log($"BattleManager: 턴 시작. 플레이어 인덱스 = {playerData.playerIndex}, 턴 = {playerData.turnCount}, 활성 AP = {playerData.activeAP}");
            return true;
        }

        // 내 턴 엑스트라 드로우를 처리하고 이동 페이즈로 이동
        public bool TryExtraDraw(out string errorMessage)
        {
            errorMessage = string.Empty;

            if (!CanTakeExtraDraw)
            {
                errorMessage = "엑스트라 드로우를 할 수 없습니다. AP, 덱, 페이즈 상태를 확인하세요.";
                return false;
            }

            if (!localPlayerData.TrySpendAP(1))
            {
                errorMessage = "엑스트라 드로우에 필요한 AP가 부족합니다.";
                return false;
            }

            if (CardZoneManager.Instance == null || !CardZoneManager.Instance.DrawOneCard(localPlayerData))
            {
                localPlayerData.activeAP += 1;
                errorMessage = "카드를 뽑지 못해 AP를 돌려받았습니다.";
                return false;
            }

            localPlayerData.hasTakenExtraDrawThisTurn = true;
            SetTurnPhase(BattleEnums.TurnPhase.Move);
            Debug.Log($"BattleManager: 엑스트라 드로우 완료. 남은 AP = {localPlayerData.activeAP}");
            return true;
        }

        // 이동 페이즈에 내 에너지라인 카드를 프론트라인 빈 슬롯으로 이동
        public bool TryMoveEnergyCardToFrontLine(PlayerGameData playerData, CardRuntimeData card, int targetSlotIndex, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (isGameOver || isWaitingForInitialHandChoice || IsWaitingForBlock)
            {
                errorMessage = "현재 상태에서는 필드 카드를 이동할 수 없습니다.";
                return false;
            }

            if (playerData == null || playerData != localPlayerData || currentTurnPlayer != localPlayerData)
            {
                errorMessage = "내 턴에 내 필드 카드만 이동할 수 있습니다.";
                return false;
            }

            if (currentTurnPhase != BattleEnums.TurnPhase.Move)
            {
                errorMessage = "이동 페이즈에만 에너지라인 카드를 옮길 수 있습니다.";
                return false;
            }

            if (card == null || card.cardData == null || card.currentZone != BattleEnums.CardZone.Field || card.fieldPosition != BattleEnums.FieldPosition.EnergyLine || playerData.field == null || !playerData.field.Contains(card) || card.ownerPlayerIndex != playerData.playerIndex)
            {
                errorMessage = "내 에너지라인에 있는 카드만 이동할 수 있습니다.";
                return false;
            }

            if (targetSlotIndex < 0 || targetSlotIndex >= 4)
            {
                errorMessage = "프론트라인 슬롯 번호가 올바르지 않습니다.";
                return false;
            }

            if (CardZoneManager.Instance == null)
            {
                errorMessage = "CardZoneManager를 찾을 수 없습니다.";
                return false;
            }

            if (!CardZoneManager.Instance.MoveCard(playerData, card, BattleEnums.CardZone.Field, BattleEnums.FieldPosition.FrontLine, targetSlotIndex))
            {
                errorMessage = "프론트라인 슬롯이 이미 사용 중이거나 이동에 실패했습니다.";
                return false;
            }

            Debug.Log($"BattleManager: 에너지라인 카드 이동 완료. 카드 = {card.cardData.CardName}, 프론트라인 슬롯 = {targetSlotIndex}");
            return true;
        }

        // 엑스트라 드로우를 건너뛰고 이동 페이즈로 이동
        public bool PassExtraDraw()
        {
            if (isGameOver || currentTurnPlayer != localPlayerData || currentTurnPhase != BattleEnums.TurnPhase.ExtraDraw || localPlayerData == null)
            {
                return false;
            }

            localPlayerData.hasTakenExtraDrawThisTurn = true;
            SetTurnPhase(BattleEnums.TurnPhase.Move);
            Debug.Log("BattleManager: 엑스트라 드로우를 건너뛰고 이동 페이즈로 이동합니다.");
            return true;
        }

        // 내 턴의 다음 페이즈로 이동
        public bool AdvanceTurnPhase()
        {
            if (isGameOver || currentTurnPlayer != localPlayerData || isWaitingForInitialHandChoice || IsWaitingForBlock)
            {
                return false;
            }

            switch (currentTurnPhase)
            {
                case BattleEnums.TurnPhase.Start:
                    SetTurnPhase(BattleEnums.TurnPhase.ExtraDraw);
                    return true;

                case BattleEnums.TurnPhase.ExtraDraw:
                    SetTurnPhase(BattleEnums.TurnPhase.Move);
                    return true;

                case BattleEnums.TurnPhase.Move:
                    SetTurnPhase(BattleEnums.TurnPhase.Main);
                    return true;

                case BattleEnums.TurnPhase.Main:
                    SetTurnPhase(BattleEnums.TurnPhase.Battle);
                    return true;

                case BattleEnums.TurnPhase.Battle:
                    return ProcessEndPhase();

                case BattleEnums.TurnPhase.End:
                    return StartNextPlayerTurn();

                default:
                    return false;
            }
        }

        // 페이즈 값을 갱신하고 UI에 변경을 알림
        private void SetTurnPhase(BattleEnums.TurnPhase phase)
        {
            currentTurnPhase = phase;
            OnTurnPhaseChanged?.Invoke(currentTurnPhase);
        }

        // 엔드 페이즈 필드 활성화를 처리하고 End 페이즈로 전환
        private bool ProcessEndPhase()
        {
            if (isEndPhaseProcessed)
            {
                SetTurnPhase(BattleEnums.TurnPhase.End);
                return true;
            }

            if (!EnsureServicesReady())
            {
                Debug.LogError("BattleManager: 엔드 페이즈 서비스가 준비되지 않았습니다.");
                return false;
            }

            if (!turnFlowService.EndTurn(currentTurnPlayer, out string errorMessage))
            {
                Debug.LogError("BattleManager: 엔드 페이즈 처리에 실패했습니다. " + errorMessage);
                return false;
            }

            isEndPhaseProcessed = true;
            SetTurnPhase(BattleEnums.TurnPhase.End);
            return true;
        }

        // 현재 플레이어의 턴을 마치고 다음 플레이어 시작 페이즈로 이동
        private bool StartNextPlayerTurn()
        {
            if (currentTurnPhase != BattleEnums.TurnPhase.End || !isEndPhaseProcessed)
            {
                return false;
            }

            PlayerGameData nextPlayer = currentTurnPlayer == localPlayerData ? opponentPlayerData : localPlayerData;

            if (!BeginTurn(nextPlayer))
            {
                return false;
            }

            if (nextPlayer == opponentPlayerData)
            {
                opponentTurnCoroutine = StartCoroutine(RunOpponentTurn());
            }

            return true;
        }

        // 상대 AI가 현재 턴을 종료하고 다음 플레이어 턴으로 넘김
        public bool EndCurrentTurn()
        {
            if (isGameOver || currentTurnPlayer != opponentPlayerData || !isRunningOpponentTurn || IsWaitingForBlock)
            {
                Debug.LogWarning("BattleManager: 종료할 수 있는 상대 턴이 없습니다.");
                return false;
            }

            if (!ProcessEndPhase())
            {
                return false;
            }

            return StartNextPlayerTurn();
        }

        // 상대 AI 행동을 순서대로 실행
        private IEnumerator RunOpponentTurn()
        {
            if (battleAIService == null)
            {
                Debug.LogError("BattleManager: AI 서비스가 준비되지 않았습니다.");
                yield break;
            }

            isRunningOpponentTurn = true;
            yield return battleAIService.RunTurn(opponentPlayerData, localPlayerData);
            isRunningOpponentTurn = false;
            opponentTurnCoroutine = null;
        }

        #endregion
    }
}



