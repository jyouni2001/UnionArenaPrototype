
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
        public static BattleManager Instance { get; private set; } // 전투 매니저 싱글톤

        private const int MaxPlayerCount = 2; // 최대 플레이어 수

        private List<PlayerGameData> playerGameDataList = new List<PlayerGameData>(); // 플레이어별 전투 데이터

        private int nextCardInstanceId = 1; // 다음 카드 인스턴스 번호

        private PlayerGameData localPlayerData; // 현재 컴퓨터 사용자의 데이터
        private PlayerGameData opponentPlayerData; // 상대 플레이어의 데이터

        private BattleEnums.PlayerType localPlayerType; // 현재 컴퓨터 사용자의 플레이어 타입

        public IReadOnlyList<PlayerGameData> PlayerGameDataList => playerGameDataList; // 외부에서 플레이어 데이터 조회

        public PlayerGameData LocalPlayerData => localPlayerData; // 내 플레이어 데이터 조회
        public PlayerGameData OpponentPlayerData => opponentPlayerData; // 상대 플레이어 데이터 조회
        public BattleEnums.PlayerType LocalPlayerType => localPlayerType; // 내 플레이어 타입 조회

        // 싱글톤 인스턴스 등록 및 기본 초기화
        protected override void Awake()
        {
            if (Instance != null && Instance != this) // 이미 다른 매니저가 존재하는 경우
            {
                Destroy(gameObject); // 현재 오브젝트 삭제
                return;
            }

            Instance = this; // 현재 오브젝트를 대표 인스턴스로 등록
            base.Awake(); // 기본 초기화 실행
        }

        // 오브젝트 삭제 시 싱글톤 인스턴스 정리
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null; // 현재 인스턴스가 대표 인스턴스라면 초기화
            }
        }

        // 전투 데이터 초기화
        protected override void Initialize()
        {
            playerGameDataList.Clear(); // 플레이어 데이터 리스트 초기화
            nextCardInstanceId = 1; // 카드 인스턴스 번호 초기화

            localPlayerData = null; // 내 플레이어 데이터 초기화
            opponentPlayerData = null; // 상대 플레이어 데이터 초기화

            // 최대 플레이어 수만큼 플레이어 게임 데이터 생성 후 리스트에 추가
            for (int i = 0; i < MaxPlayerCount; i++)
            {
                PlayerGameData playerGameData = new PlayerGameData(); // 새로운 플레이어 데이터 생성
                playerGameData.playerIndex = i; // 플레이어 번호 지정
                playerGameDataList.Add(playerGameData); // 플레이어 데이터 리스트에 추가
            }

            SetLocalPlayerType(BattleEnums.PlayerType.PlayerA); // 현재 테스트 컴퓨터를 방장으로 설정

            base.Initialize(); // 기본 초기화 완료 처리
        }

        // 플레이어 번호로 전투 데이터 반환
        public PlayerGameData GetPlayerGameData(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= playerGameDataList.Count)
            {
                return null;
            }

            return playerGameDataList[playerIndex]; // 해당 플레이어 데이터 반환
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

            int localPlayerIndex = (int)playerType - 1; // enum 값 1, 2를 리스트 인덱스 0, 1로 변환
            int opponentPlayerIndex = localPlayerIndex == 0 ? 1 : 0; // 반대 플레이어 인덱스 계산

            PlayerGameData newLocalPlayerData = GetPlayerGameData(localPlayerIndex);
            PlayerGameData newOpponentPlayerData = GetPlayerGameData(opponentPlayerIndex);

            if (newLocalPlayerData == null || newOpponentPlayerData == null)
            {
                Debug.LogError("BattleManager: 플레이어 데이터를 연결하지 못했습니다.");
                return;
            }

            localPlayerData = newLocalPlayerData; // 내 플레이어 데이터 연결
            opponentPlayerData = newOpponentPlayerData; // 상대 플레이어 데이터 연결
            localPlayerType = playerType; // 내 플레이어 타입 저장

            Debug.Log($"BattleManager: 내 플레이어 = {localPlayerType}, 상대 플레이어 = {(localPlayerIndex == 0 ? BattleEnums.PlayerType.PlayerB : BattleEnums.PlayerType.PlayerA)}");
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

            CardRuntimeData cardRuntimeData = new CardRuntimeData(); // 새로운 카드 인스턴스 생성
            cardRuntimeData.instanceId = nextCardInstanceId++; // 카드 고유 번호 부여
            cardRuntimeData.cardData = cardData; // 카드 원본 데이터 연결
            cardRuntimeData.currentZone = BattleEnums.CardZone.None; // 카드의 현재 영역 초기화
            cardRuntimeData.fieldPosition = BattleEnums.FieldPosition.None; // 필드 세부 위치 초기화
            cardRuntimeData.ownerPlayerIndex = ownerPlayerIndex; // 카드 소유 플레이어 지정

            return cardRuntimeData; // 생성한 카드 인스턴스 반환
        }
    }
}
