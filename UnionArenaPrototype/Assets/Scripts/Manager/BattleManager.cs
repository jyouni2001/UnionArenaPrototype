using System;
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

        // 전투 매니저 싱글톤
        public static BattleManager Instance { get; private set; }

        // 전투에 참가하는 플레이어 수
        private const int MaxPlayerCount = 2;

        // 프로토타입에서 사용할 테스트 덱 ID
        private const string TestDeckID =
            "a64e00e5f2414498848063059de16ae2";

        // 플레이어별 게임 데이터를 보관하는 목록
        private readonly List<PlayerGameData> playerGameDataList =
            new List<PlayerGameData>();

        // 내 플레이어 데이터
        private PlayerGameData localPlayerData;

        // 상대 플레이어 데이터
        private PlayerGameData opponentPlayerData;

        // 현재 턴을 진행하는 플레이어 데이터
        private PlayerGameData currentTurnPlayer;

        // 로컬 플레이어 타입
        private BattleEnums.PlayerType localPlayerType;

        // 최초 손패 선택 대기 여부
        private bool isWaitingForInitialHandChoice;

        // 덱과 최초 손패 준비 서비스
        private BattleSetupService battleSetupService;

        // 턴 진행 서비스
        private TurnFlowService turnFlowService;

        // 공격 및 블록 처리 서비스
        private BattleCombatService battleCombatService;

        // 플레이어 데이터 목록을 읽기 전용으로 제공
        public IReadOnlyList<PlayerGameData> PlayerGameDataList =>
            playerGameDataList;

        // 내 플레이어 데이터 조회
        public PlayerGameData LocalPlayerData => localPlayerData;

        // 상대 플레이어 데이터 조회
        public PlayerGameData OpponentPlayerData => opponentPlayerData;

        // 현재 턴 플레이어 데이터 조회
        public PlayerGameData CurrentTurnPlayer => currentTurnPlayer;

        // 로컬 플레이어 타입 조회
        public BattleEnums.PlayerType LocalPlayerType => localPlayerType;

        // 최초 손패 선택 대기 상태 조회
        public bool IsWaitingForInitialHandChoice =>
            isWaitingForInitialHandChoice;

        // 블록 선택이 끝나지 않은 공격이 있는지 조회
        public bool IsWaitingForBlock =>
            battleCombatService != null &&
            battleCombatService.IsWaitingForBlock;

        // 전투 카드 상태 변경 알림
        public event Action<PlayerGameData> OnCombatFieldChanged;

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

        // 제거될 때 서비스 이벤트와 싱글톤 참조 해제
        private void OnDestroy()
        {
            if (battleCombatService != null)
            {
                battleCombatService.OnFieldStateChanged -=
                    HandleCombatFieldStateChanged;
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
            isWaitingForInitialHandChoice = false;

            battleSetupService = null;
            turnFlowService = null;
            battleCombatService = null;

            for (int i = 0; i < MaxPlayerCount; i++)
            {
                PlayerGameData playerData = new PlayerGameData
                {
                    playerIndex = i
                };

                playerGameDataList.Add(playerData);
            }

            SetLocalPlayerType(BattleEnums.PlayerType.PlayerA);
            base.Initialize();
        }

        // 필요한 매니저와 서비스를 연결
        private bool EnsureServicesReady()
        {
            if (battleSetupService != null &&
                turnFlowService != null &&
                battleCombatService != null)
            {
                return true;
            }

            if (DeckManager.Instance == null ||
                CardDatabaseManager.Instance == null ||
                CardZoneManager.Instance == null)
            {
                Debug.LogError(
                    "BattleManager: 전투 준비에 필요한 매니저가 없습니다.");
                return false;
            }

            if (battleSetupService == null)
            {
                battleSetupService = new BattleSetupService(
                    GetPlayerGameData,
                    DeckManager.Instance,
                    CardDatabaseManager.Instance,
                    CardZoneManager.Instance);
            }

            if (turnFlowService == null)
            {
                turnFlowService = new TurnFlowService(
                    CardZoneManager.Instance);
            }

            if (battleCombatService == null)
            {
                battleCombatService = new BattleCombatService(
                    CardZoneManager.Instance);

                battleCombatService.OnFieldStateChanged +=
                    HandleCombatFieldStateChanged;
            }

            return true;
        }

        // 전투 서비스의 카드 상태 변경 알림을 외부에 전달
        private void HandleCombatFieldStateChanged(
            PlayerGameData playerData)
        {
            OnCombatFieldChanged?.Invoke(playerData);
        }

        // 양쪽 테스트 덱과 최초 손패·시큐리티를 준비
        private void Start()
        {
            if (localPlayerData == null ||
                opponentPlayerData == null ||
                !EnsureServicesReady())
            {
                Debug.LogError(
                    "BattleManager: 전투 준비를 시작할 수 없습니다.");
                return;
            }

            if (!PreparePlayerDeckAndSecurity(localPlayerData))
            {
                return;
            }

            if (!PreparePlayerDeckAndSecurity(opponentPlayerData))
            {
                return;
            }

            if (!DrawInitialHand())
            {
                Debug.LogError(
                    "BattleManager: 최초 손패 준비에 실패했습니다.");
                return;
            }

            Debug.Log(
                "BattleManager: 최초 손패 선택 대기 상태입니다.");
        }

        // 해당 플레이어의 덱을 만들고 섞은 뒤 시큐리티 7장 준비
        private bool PreparePlayerDeckAndSecurity(
            PlayerGameData playerData)
        {
            if (playerData == null)
            {
                return false;
            }

            if (!InitializePlayerDeck(
                    TestDeckID,
                    playerData.playerIndex))
            {
                return false;
            }

            if (!CardZoneManager.Instance.ShuffleDeck(playerData))
            {
                Debug.LogError(
                    $"BattleManager: 덱 셔플에 실패했습니다. PlayerIndex = {playerData.playerIndex}");
                return false;
            }

            if (!battleSetupService.PrepareSecurity(playerData))
            {
                Debug.LogError(
                    $"BattleManager: 시큐리티 준비에 실패했습니다. PlayerIndex = {playerData.playerIndex}");
                return false;
            }

            return true;
        }

        #endregion

        #region 플레이어 참조 조율

        // 플레이어 번호에 해당하는 데이터를 반환
        public PlayerGameData GetPlayerGameData(int playerIndex)
        {
            if (playerIndex < 0 ||
                playerIndex >= playerGameDataList.Count)
            {
                return null;
            }

            return playerGameDataList[playerIndex];
        }

        // 로컬 플레이어와 상대 플레이어 참조를 연결
        public void SetLocalPlayerType(
            BattleEnums.PlayerType playerType)
        {
            if (playerType != BattleEnums.PlayerType.PlayerA &&
                playerType != BattleEnums.PlayerType.PlayerB)
            {
                Debug.LogError(
                    "BattleManager: 올바르지 않은 PlayerType입니다.");
                return;
            }

            int localPlayerIndex = (int)playerType - 1;
            int opponentPlayerIndex =
                localPlayerIndex == 0 ? 1 : 0;

            PlayerGameData newLocalPlayerData =
                GetPlayerGameData(localPlayerIndex);

            PlayerGameData newOpponentPlayerData =
                GetPlayerGameData(opponentPlayerIndex);

            if (newLocalPlayerData == null ||
                newOpponentPlayerData == null)
            {
                Debug.LogError(
                    "BattleManager: 플레이어 참조 연결에 실패했습니다.");
                return;
            }

            localPlayerData = newLocalPlayerData;
            opponentPlayerData = newOpponentPlayerData;
            localPlayerType = playerType;

            Debug.Log(
                $"BattleManager: 내 플레이어 = {localPlayerType}, 상대 플레이어 = " +
                $"{(localPlayerIndex == 0 ? BattleEnums.PlayerType.PlayerB : BattleEnums.PlayerType.PlayerA)}");
        }

        #endregion

        #region 카드 및 최초 손패 준비

        // 카드 원본 데이터로 전투용 카드 인스턴스를 생성
        public CardRuntimeData CreateCardRuntimeData(
            CardJsonData cardData,
            int ownerPlayerIndex)
        {
            if (!EnsureServicesReady())
            {
                return null;
            }

            return battleSetupService.CreateCardRuntimeData(
                cardData,
                ownerPlayerIndex);
        }

        // 지정한 플레이어의 전투 덱을 등록
        public bool InitializePlayerDeck(
            string deckID,
            int playerIndex)
        {
            if (!EnsureServicesReady())
            {
                return false;
            }

            return battleSetupService.InitializePlayerDeck(
                deckID,
                playerIndex);
        }

        // 내 덱에서 최초 손패 7장을 준비
        public bool DrawInitialHand()
        {
            if (localPlayerData == null ||
                isWaitingForInitialHandChoice ||
                !EnsureServicesReady())
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

        // 최초 손패를 유지하고 첫 턴을 시작
        public bool KeepInitialHand()
        {
            if (!isWaitingForInitialHandChoice ||
                localPlayerData == null ||
                !EnsureServicesReady())
            {
                Debug.LogWarning(
                    "BattleManager: 최초 손패 선택 상태가 아닙니다.");
                return false;
            }

            if (!StartLocalFirstTurn())
            {
                return false;
            }

            isWaitingForInitialHandChoice = false;
            Debug.Log(
                "BattleManager: 최초 손패 유지 및 시큐리티 준비 완료.");
            return true;
        }

        // 최초 손패를 멀리건 처리하고 첫 턴을 시작
        public bool MulliganInitialHand()
        {
            if (!isWaitingForInitialHandChoice ||
                localPlayerData == null ||
                !EnsureServicesReady())
            {
                Debug.LogWarning(
                    "BattleManager: 최초 손패 선택 상태가 아닙니다.");
                return false;
            }

            if (!battleSetupService.Mulligan(localPlayerData))
            {
                return false;
            }

            if (!StartLocalFirstTurn())
            {
                return false;
            }

            isWaitingForInitialHandChoice = false;
            Debug.Log(
                "BattleManager: 멀리건 및 시큐리티 준비 완료.");
            return true;
        }

        #endregion

        #region 공격 및 블록 요청

        // 공격을 선언하고 자동 블록 판정 또는 시큐리티 공개를 진행
        public bool TryDeclareAttack(
            PlayerGameData attackerPlayer,
            CardRuntimeData attacker,
            out BattleCombatService.Resolution resolution,
            out PlayerGameData securityOwner,
            out CardRuntimeData revealedSecurityCard,
            out string errorMessage)
        {
            resolution = new BattleCombatService.Resolution(
                BattleEnums.CombatResolutionType.None,
                attacker,
                null);

            securityOwner = null;
            revealedSecurityCard = null;
            errorMessage = string.Empty;

            if (!EnsureServicesReady())
            {
                errorMessage = "전투 서비스가 준비되지 않았습니다.";
                return false;
            }

            if (attackerPlayer == null ||
                attackerPlayer != currentTurnPlayer)
            {
                errorMessage = "현재 턴 플레이어만 공격할 수 있습니다.";
                return false;
            }

            PlayerGameData defenderPlayer =
                attackerPlayer == localPlayerData
                    ? opponentPlayerData
                    : localPlayerData;

            if (!battleCombatService.TryDeclareAttack(
                    attackerPlayer,
                    defenderPlayer,
                    attacker,
                    out errorMessage))
            {
                return false;
            }

            // 상대가 공격을 막을 수 있으면 그중 가장 적은 BP 카드로 블록
            CardRuntimeData blocker =
                battleCombatService.FindBestBlocker();

            if (blocker != null)
            {
                return battleCombatService.TryResolveBlock(
                    blocker,
                    out resolution,
                    out errorMessage);
            }

            // 이길 수 있는 블로커가 없으면 공격을 통과시키고 시큐리티 공개
            securityOwner = defenderPlayer;

            return battleCombatService.TryPassBlock(
                out resolution,
                out securityOwner,
                out revealedSecurityCard,
                out errorMessage);
        }

        // 공개한 시큐리티 카드를 해당 플레이어의 트래시로 이동
        public bool MoveRevealedSecurityToTrash(
            PlayerGameData securityOwner,
            CardRuntimeData securityCard)
        {
            if (securityOwner == null ||
                securityCard == null ||
                securityOwner.security == null ||
                !securityOwner.security.Contains(securityCard) ||
                CardZoneManager.Instance == null)
            {
                return false;
            }

            return CardZoneManager.Instance.MoveCard(
                securityOwner,
                securityCard,
                BattleEnums.CardZone.Trash);
        }

        #endregion

        #region 턴 요청 조율

        // 로컬 플레이어의 첫 턴을 시작
        private bool StartLocalFirstTurn()
        {
            if (localPlayerData == null)
            {
                Debug.LogError(
                    "BattleManager: 첫 턴 플레이어 데이터가 없습니다.");
                return false;
            }

            if (localPlayerData.turnCount > 0)
            {
                Debug.LogWarning(
                    "BattleManager: 첫 턴이 이미 시작되었습니다.");
                return false;
            }

            return BeginTurn(localPlayerData);
        }

        // 턴 서비스를 호출하고 현재 턴 플레이어를 갱신
        private bool BeginTurn(PlayerGameData playerData)
        {
            if (playerData == null || !EnsureServicesReady())
            {
                return false;
            }

            if (!turnFlowService.BeginTurn(
                    playerData,
                    out string errorMessage))
            {
                Debug.LogError(
                    "BattleManager: 턴 시작 실패. " + errorMessage);
                return false;
            }

            currentTurnPlayer = playerData;

            Debug.Log(
                $"BattleManager: 턴 시작. 플레이어 인덱스 = {playerData.playerIndex}, " +
                $"턴 = {playerData.turnCount}, 활성 AP = {playerData.activeAP}");
            return true;
        }

        // 현재 턴을 종료하고 다음 플레이어 턴으로 전환
        public bool EndCurrentTurn()
        {
            if (currentTurnPlayer == null ||
                isWaitingForInitialHandChoice)
            {
                Debug.LogWarning(
                    "BattleManager: 종료할 턴이 없습니다.");
                return false;
            }

            if (IsWaitingForBlock)
            {
                Debug.LogWarning(
                    "BattleManager: 진행 중인 공격 처리를 먼저 끝내야 합니다.");
                return false;
            }

            if (!EnsureServicesReady())
            {
                return false;
            }

            if (!turnFlowService.EndTurn(
                    currentTurnPlayer,
                    out string errorMessage))
            {
                Debug.LogError(
                    "BattleManager: 턴 종료 처리 실패. " + errorMessage);
                return false;
            }

            PlayerGameData nextPlayer =
                currentTurnPlayer == localPlayerData
                    ? opponentPlayerData
                    : localPlayerData;

            return BeginTurn(nextPlayer);
        }

        #endregion
    }
}