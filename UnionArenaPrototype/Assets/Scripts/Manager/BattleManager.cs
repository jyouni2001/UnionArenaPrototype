using System.Collections.Generic;
using UnityEngine;
using Battle;
using Data;
using Enums;

namespace Manager
{
    // 전투 전체 진행 및 플레이어 데이터 관리
    public class BattleManager : BaseManager
    {
        public static BattleManager Instance { get; private set; }

        private const int MaxPlayerCount = 2;
        private const int RequiredDeckCardCount = 50;
        private const int InitialHandCardCount = 7;
        private const int InitialSecurityCardCount = 7;

        private const string TestDeckID = "a64e00e5f2414498848063059de16ae2";

        private List<PlayerGameData> playerGameDataList = new List<PlayerGameData>();
        private int nextCardInstanceId = 1;

        private PlayerGameData localPlayerData;
        private PlayerGameData opponentPlayerData;
        private PlayerGameData currentTurnPlayer;
        private BattleEnums.PlayerType localPlayerType;

        private bool isWaitingForInitialHandChoice = false;

        public IReadOnlyList<PlayerGameData> PlayerGameDataList => playerGameDataList;
        public PlayerGameData LocalPlayerData => localPlayerData;
        public PlayerGameData OpponentPlayerData => opponentPlayerData;
        public PlayerGameData CurrentTurnPlayer => currentTurnPlayer;
        public BattleEnums.PlayerType LocalPlayerType => localPlayerType;
        public bool IsWaitingForInitialHandChoice => isWaitingForInitialHandChoice;

        // 싱글톤 인스턴스 등록 및 기본 초기화
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

        // 오브젝트 삭제 시 싱글톤 인스턴스 정리
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // 전투 데이터 초기화
        protected override void Initialize()
        {
            playerGameDataList.Clear();
            nextCardInstanceId = 1;

            localPlayerData = null;
            opponentPlayerData = null;
            currentTurnPlayer = null;
            isWaitingForInitialHandChoice = false;

            for (int i = 0; i < MaxPlayerCount; i++)
            {
                PlayerGameData playerGameData = new PlayerGameData();
                playerGameData.playerIndex = i;
                playerGameDataList.Add(playerGameData);
            }

            SetLocalPlayerType(BattleEnums.PlayerType.PlayerA);

            base.Initialize();
        }

        // 씬 시작 시 테스트 덱을 로컬 플레이어에게 등록하고 최초 손패 준비
        private void Start()
        {
            if (localPlayerData == null)
            {
                Debug.LogError("BattleManager: 로컬 플레이어 데이터가 초기화되지 않았습니다.");
                return;
            }

            if (!InitializePlayerDeck(TestDeckID, localPlayerData.playerIndex))
            {
                return;
            }

            if (CardZoneManager.Instance == null ||
                !CardZoneManager.Instance.ShuffleDeck(localPlayerData))
            {
                Debug.LogError("BattleManager: 최초 덱 셔플에 실패했습니다.");
                return;
            }

            if (!DrawInitialHand())
            {
                Debug.LogError("BattleManager: 최초 손패 준비에 실패했습니다.");
                return;
            }

            Debug.Log("BattleManager: 최초 손패 선택 대기 상태입니다.");
        }

        // 플레이어 번호로 전투 데이터 반환
        public PlayerGameData GetPlayerGameData(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= playerGameDataList.Count)
            {
                return null;
            }

            return playerGameDataList[playerIndex];
        }

        // 현재 컴퓨터의 플레이어 타입을 기준으로 내 데이터와 상대 데이터를 연결
        public void SetLocalPlayerType(BattleEnums.PlayerType playerType)
        {
            if (playerType != BattleEnums.PlayerType.PlayerA &&
                playerType != BattleEnums.PlayerType.PlayerB)
            {
                Debug.LogError("BattleManager: 올바르지 않은 PlayerType입니다.");
                return;
            }

            int localPlayerIndex = (int)playerType - 1;
            int opponentPlayerIndex = localPlayerIndex == 0 ? 1 : 0;

            PlayerGameData newLocalPlayerData = GetPlayerGameData(localPlayerIndex);
            PlayerGameData newOpponentPlayerData = GetPlayerGameData(opponentPlayerIndex);

            if (newLocalPlayerData == null || newOpponentPlayerData == null)
            {
                Debug.LogError("BattleManager: 플레이어 데이터를 연결하지 못했습니다.");
                return;
            }

            localPlayerData = newLocalPlayerData;
            opponentPlayerData = newOpponentPlayerData;
            localPlayerType = playerType;

            Debug.Log(
                $"BattleManager: 내 플레이어 = {localPlayerType}, 상대 플레이어 = " +
                $"{(localPlayerIndex == 0 ? BattleEnums.PlayerType.PlayerB : BattleEnums.PlayerType.PlayerA)}");
        }

        // 카드 인스턴스 생성
        public CardRuntimeData CreateCardRuntimeData(CardJsonData cardData, int ownerPlayerIndex)
        {
            if (cardData == null)
            {
                return null;
            }

            if (GetPlayerGameData(ownerPlayerIndex) == null)
            {
                return null;
            }

            CardRuntimeData cardRuntimeData = new CardRuntimeData();
            cardRuntimeData.instanceId = nextCardInstanceId++;
            cardRuntimeData.cardData = cardData;
            cardRuntimeData.currentZone = BattleEnums.CardZone.None;
            cardRuntimeData.fieldPosition = BattleEnums.FieldPosition.None;
            cardRuntimeData.ownerPlayerIndex = ownerPlayerIndex;

            return cardRuntimeData;
        }

        // 저장된 덱을 전투용 카드 인스턴스로 변환하여 플레이어 덱에 등록
        public bool InitializePlayerDeck(string deckID, int playerIndex)
        {
            if (DeckManager.Instance == null ||
                CardDatabaseManager.Instance == null ||
                CardZoneManager.Instance == null)
            {
                Debug.LogError("BattleManager: 필요한 매니저가 존재하지 않습니다.");
                return false;
            }

            PlayerGameData playerData = GetPlayerGameData(playerIndex);

            if (playerData == null)
            {
                Debug.LogError("BattleManager: 올바르지 않은 플레이어 번호입니다.");
                return false;
            }

            if (playerData.deck == null || playerData.deck.Count > 0)
            {
                Debug.LogError("BattleManager: 이미 카드가 등록되었거나 덱 리스트가 없습니다.");
                return false;
            }

            DeckData deckData = DeckManager.Instance.GetDeck(deckID);

            if (deckData == null)
            {
                Debug.LogError("BattleManager: 지정한 덱을 찾을 수 없습니다. ID = " + deckID);
                return false;
            }

            if (deckData.Cards == null)
            {
                Debug.LogError("BattleManager: 덱 카드 목록이 없습니다.");
                return false;
            }

            int totalCardCount = 0;
            List<CardJsonData> cardDataList = new List<CardJsonData>();

            foreach (DeckCardData deckCard in deckData.Cards)
            {
                if (deckCard == null ||
                    string.IsNullOrWhiteSpace(deckCard.CardID) ||
                    deckCard.Count < 1 ||
                    deckCard.Count > 4)
                {
                    Debug.LogError("BattleManager: 덱에 잘못된 카드 정보가 있습니다.");
                    return false;
                }

                CardJsonData cardData =
                    CardDatabaseManager.Instance.GetCardByID(deckCard.CardID);

                if (cardData == null)
                {
                    Debug.LogError(
                        "BattleManager: 카드 데이터베이스에서 카드를 찾을 수 없습니다. ID = " +
                        deckCard.CardID);
                    return false;
                }

                totalCardCount += deckCard.Count;
                cardDataList.Add(cardData);
            }

            if (totalCardCount != RequiredDeckCardCount)
            {
                Debug.LogError(
                    $"BattleManager: 덱 카드 수가 올바르지 않습니다. " +
                    $"현재 = {totalCardCount}장, 필요 = {RequiredDeckCardCount}장");
                return false;
            }

            for (int i = 0; i < deckData.Cards.Count; i++)
            {
                DeckCardData deckCard = deckData.Cards[i];
                CardJsonData cardData = cardDataList[i];

                for (int count = 0; count < deckCard.Count; count++)
                {
                    CardRuntimeData runtimeData =
                        CreateCardRuntimeData(cardData, playerIndex);

                    if (runtimeData == null)
                    {
                        Debug.LogError("BattleManager: 카드 인스턴스 생성에 실패했습니다.");
                        return false;
                    }

                    if (!CardZoneManager.Instance.MoveCard(
                        playerData,
                        runtimeData,
                        BattleEnums.CardZone.Deck))
                    {
                        Debug.LogError("BattleManager: 카드를 덱 영역에 등록하지 못했습니다.");
                        return false;
                    }
                }
            }

            Debug.Log(
                $"BattleManager: {deckData.DeckName} 덱 등록 완료 / " +
                $"PlayerIndex = {playerIndex} / 카드 수 = {playerData.deck.Count}");

            return true;
        }

        // 최초 손패 7장을 준비한다.
        public bool DrawInitialHand()
        {
            if (localPlayerData == null ||
                CardZoneManager.Instance == null)
            {
                Debug.LogError("DrawInitialHand: 플레이어 또는 CardZoneManager가 없습니다.");
                return false;
            }

            if (isWaitingForInitialHandChoice)
            {
                Debug.LogWarning("DrawInitialHand: 이미 최초 손패 선택을 기다리고 있습니다.");
                return false;
            }

            if (localPlayerData.hand == null ||
                localPlayerData.hand.Count != 0)
            {
                Debug.LogError("DrawInitialHand: 손패가 비어 있지 않습니다.");
                return false;
            }

            if (localPlayerData.deck == null ||
                localPlayerData.deck.Count <
                InitialHandCardCount + InitialSecurityCardCount)
            {
                Debug.LogError("DrawInitialHand: 덱에 필요한 카드 수가 부족합니다.");
                return false;
            }

            for (int i = 0; i < InitialHandCardCount; i++)
            {
                if (!CardZoneManager.Instance.DrawOneCard(localPlayerData))
                {
                    Debug.LogError($"DrawInitialHand: {i + 1}번째 카드 드로우에 실패했습니다.");
                    return false;
                }
            }

            isWaitingForInitialHandChoice = true;

            Debug.Log("BattleManager: 최초 손패 7장 준비 완료.");
            return true;
        }

        // 최초 손패를 유지하고 시큐리티 7장을 준비한다.
        public bool KeepInitialHand()
        {
            if (!isWaitingForInitialHandChoice)
            {
                Debug.LogWarning("KeepInitialHand: 최초 손패 선택 대기 상태가 아닙니다.");
                return false;
            }

            if (CardZoneManager.Instance == null)
            {
                return false;
            }

            if (!CardZoneManager.Instance.MoveCardsToSecurity(
                localPlayerData,
                InitialSecurityCardCount))
            {
                return false;
            }

            isWaitingForInitialHandChoice = false;
            StartLocalFirstTurn();

            Debug.Log("BattleManager: 최초 손패 유지 및 시큐리티 준비 완료.");
            return true;
        }

        // 최초 손패를 덱 아래로 되돌리고 새 손패를 준비한다.
        public bool MulliganInitialHand()
        {
            if (!isWaitingForInitialHandChoice)
            {
                Debug.LogWarning("MulliganInitialHand: 최초 손패 선택 대기 상태가 아닙니다.");
                return false;
            }

            if (CardZoneManager.Instance == null)
            {
                return false;
            }

            if (!CardZoneManager.Instance.ReturnHandToDeckBottom(localPlayerData))
            {
                return false;
            }

            if (!CardZoneManager.Instance.DrawCards(
                localPlayerData,
                InitialHandCardCount))
            {
                return false;
            }

            if (!CardZoneManager.Instance.ShuffleDeck(localPlayerData))
            {
                Debug.LogError("MulliganInitialHand: 남은 덱 셔플에 실패했습니다.");
                return false;
            }

            if (!CardZoneManager.Instance.MoveCardsToSecurity(
                localPlayerData,
                InitialSecurityCardCount))
            {
                return false;
            }

            isWaitingForInitialHandChoice = false;
            StartLocalFirstTurn();

            Debug.Log("BattleManager: 멀리건 및 시큐리티 준비 완료.");
            return true;
        }

        // 로컬 플레이어의 첫 턴 시작
        private void StartLocalFirstTurn()
        {
            if (localPlayerData == null)
            {
                Debug.LogError("BattleManager: 첫 턴을 시작할 플레이어 데이터가 없습니다.");
                return;
            }

            if (localPlayerData.turnCount > 0)
            {
                Debug.LogWarning("BattleManager: 첫 턴이 이미 시작되었습니다.");
                return;
            }

            BeginTurn(localPlayerData);
        }

        // 지정 플레이어의 턴 시작 처리
        private bool BeginTurn(PlayerGameData playerData)
        {
            if (playerData == null)
            {
                Debug.LogError("BattleManager: 턴을 시작할 플레이어 데이터가 없습니다.");
                return false;
            }

            int nextTurnCount = playerData.turnCount + 1;
            int targetAPCardCount =
                GetTargetAPCardCount(playerData, nextTurnCount);

            if (!playerData.RefreshAP(targetAPCardCount))
            {
                Debug.LogError("BattleManager: AP 준비에 실패했습니다.");
                return false;
            }

            playerData.turnCount = nextTurnCount;
            currentTurnPlayer = playerData;

            bool isPlayerOneFirstTurn =
                playerData.playerIndex == 0 &&
                playerData.turnCount == 1;

            // 선공 첫 턴을 제외하고 턴 시작 시 카드 1장 드로우
            if (!isPlayerOneFirstTurn &&
                playerData.deck != null &&
                playerData.deck.Count > 0 &&
                CardZoneManager.Instance != null)
            {
                if (!CardZoneManager.Instance.DrawOneCard(playerData))
                {
                    Debug.LogWarning("BattleManager: 턴 시작 드로우에 실패했습니다.");
                }
            }

            Debug.Log(
                $"BattleManager: 턴 시작. 플레이어 인덱스 = {playerData.playerIndex}, " +
                $"턴 = {playerData.turnCount}, 활성 AP = {playerData.activeAP}");

            return true;
        }

        // 플레이어 턴과 턴 번호에 필요한 AP 카드 수 반환
        private int GetTargetAPCardCount(
            PlayerGameData playerData,
            int turnNumber)
        {
            if (playerData.playerIndex == 0)
            {
                return Mathf.Min(turnNumber, 3);
            }

            if (turnNumber == 1)
            {
                return 2;
            }

            return Mathf.Min(turnNumber, 3);
        }

        // 현재 턴을 마치고 상대 턴 시작
        public bool EndCurrentTurn()
        {
            if (currentTurnPlayer == null ||
                isWaitingForInitialHandChoice)
            {
                Debug.LogWarning("BattleManager: 종료할 턴이 없습니다.");
                return false;
            }

            PlayerGameData nextPlayer =
                currentTurnPlayer == localPlayerData
                    ? opponentPlayerData
                    : localPlayerData;

            return BeginTurn(nextPlayer);
        }
    }
}