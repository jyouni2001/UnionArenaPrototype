using System.Collections.Generic;
using UnityEngine;
using Battle;
using Data;
using Enums;

namespace Manager
{
    // 전투 서비스와 매니저를 연결하고 플레이어별 진행 상태를 조율
    public class BattleManager : BaseManager
    {
        #region 싱글톤 및 전투 상태

        public static BattleManager Instance { get; private set; }

        private const int MaxPlayerCount = 2;
        private const string TestDeckID =
            "a64e00e5f2414498848063059de16ae2";

        // 전투에 참가하는 플레이어와 현재 턴 플레이어
        private readonly List<PlayerGameData> playerGameDataList =
            new List<PlayerGameData>();

        private PlayerGameData localPlayerData;
        private PlayerGameData opponentPlayerData;
        private PlayerGameData currentTurnPlayer;
        private BattleEnums.PlayerType localPlayerType;

        // 최초 손패 선택이 진행 중인지 나타냄
        private bool isWaitingForInitialHandChoice;

        // 전투 준비와 턴 규칙을 처리하는 서비스
        private BattleSetupService battleSetupService;
        private TurnFlowService turnFlowService;

        public IReadOnlyList<PlayerGameData> PlayerGameDataList =>
            playerGameDataList;

        public PlayerGameData LocalPlayerData => localPlayerData;
        public PlayerGameData OpponentPlayerData => opponentPlayerData;
        public PlayerGameData CurrentTurnPlayer => currentTurnPlayer;
        public BattleEnums.PlayerType LocalPlayerType => localPlayerType;
        public bool IsWaitingForInitialHandChoice =>
            isWaitingForInitialHandChoice;

        #endregion

        #region 생명 주기 및 초기화

        // 싱글톤 등록 후 플레이어 전투 데이터를 초기화
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

        // 매니저가 제거될 때 싱글톤 참조 해제
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // 플레이어 상태를 만들고 로컬/상대 참조를 연결
        protected override void Initialize()
        {
            playerGameDataList.Clear();
            localPlayerData = null;
            opponentPlayerData = null;
            currentTurnPlayer = null;
            isWaitingForInitialHandChoice = false;
            battleSetupService = null;
            turnFlowService = null;

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

        // 필요한 매니저 참조를 서비스에 연결
        private bool EnsureServicesReady()
        {
            if (battleSetupService != null && turnFlowService != null)
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

            battleSetupService = new BattleSetupService(
                GetPlayerGameData,
                DeckManager.Instance,
                CardDatabaseManager.Instance,
                CardZoneManager.Instance);

            turnFlowService = new TurnFlowService(
                CardZoneManager.Instance);

            return true;
        }

        // 테스트 덱을 등록하고 최초 손패 선택 상태를 준비
        private void Start()
        {
            if (localPlayerData == null || !EnsureServicesReady())
            {
                Debug.LogError(
                    "BattleManager: 전투 준비를 시작할 수 없습니다.");

                return;
            }

            if (!InitializePlayerDeck(
                TestDeckID,
                localPlayerData.playerIndex))
            {
                return;
            }

            if (!CardZoneManager.Instance.ShuffleDeck(localPlayerData))
            {
                Debug.LogError(
                    "BattleManager: 최초 덱 셔플에 실패했습니다.");

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

        #endregion

        #region 플레이어 참조 조율

        // 플레이어 번호에 맞는 전투 데이터 반환
        public PlayerGameData GetPlayerGameData(int playerIndex)
        {
            if (playerIndex < 0 ||
                playerIndex >= playerGameDataList.Count)
            {
                return null;
            }

            return playerGameDataList[playerIndex];
        }

        // 로컬 플레이어와 상대 플레이어 참조 연결
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
            int opponentPlayerIndex = localPlayerIndex == 0 ? 1 : 0;

            PlayerGameData newLocalPlayerData =
                GetPlayerGameData(localPlayerIndex);

            PlayerGameData newOpponentPlayerData =
                GetPlayerGameData(opponentPlayerIndex);

            if (newLocalPlayerData == null ||
                newOpponentPlayerData == null)
            {
                Debug.LogError(
                    "BattleManager: 플레이어 데이터 참조를 연결하지 못했습니다.");

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

        #region 전투 준비 요청

        // 전투 준비 서비스에 카드 인스턴스 생성을 요청
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

        // 전투 준비 서비스에 플레이어 덱 등록을 요청
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

        // 최초 손패를 준비하고 UI에 선택 대기 상태를 알림
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

        // 유지 선택을 서비스에 전달하고 첫 턴을 시작
        public bool KeepInitialHand()
        {
            if (!isWaitingForInitialHandChoice ||
                localPlayerData == null ||
                !EnsureServicesReady())
            {
                Debug.LogWarning(
                    "KeepInitialHand: 최초 손패 선택 상태가 아닙니다.");

                return false;
            }

            if (!battleSetupService.PrepareSecurity(localPlayerData))
            {
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

        // 멀리건 처리를 서비스에 전달하고 첫 턴을 시작
        public bool MulliganInitialHand()
        {
            if (!isWaitingForInitialHandChoice ||
                localPlayerData == null ||
                !EnsureServicesReady())
            {
                Debug.LogWarning(
                    "MulliganInitialHand: 최초 손패 선택 상태가 아닙니다.");

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

        // 턴 서비스에 시작 처리를 요청하고 현재 턴 플레이어를 갱신
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

        // 턴 종료 요청을 처리하고 다음 플레이어 턴으로 전환
        public bool EndCurrentTurn()
        {
            if (currentTurnPlayer == null ||
                isWaitingForInitialHandChoice)
            {
                Debug.LogWarning(
                    "BattleManager: 종료할 턴이 없습니다.");

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
