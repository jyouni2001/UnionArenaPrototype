
using UnityEngine;
using Manager;
using Battle;
using Data;

// 전투 데이터 및 실제 카드 인스턴스 테스트
public class BattleTest : MonoBehaviour
{
    // 테스트 시작
    private void Start()
    {
        TestPlayerGameData();
    }

    // 플레이어 데이터 생성 확인
    private void TestPlayerGameData()
    {
        if (BattleManager.Instance == null)
        {
            Debug.LogError("BattleManager 인스턴스가 없습니다.");
            return;
        }

        PlayerGameData player0 = BattleManager.Instance.GetPlayerGameData(0); // 0번 플레이어 데이터 가져오기
        PlayerGameData player1 = BattleManager.Instance.GetPlayerGameData(1); // 1번 플레이어 데이터 가져오기

        if (player0 == null || player1 == null)
        {
            Debug.LogError("플레이어 데이터 생성에 실패했습니다.");
            return;
        }

        Debug.Log("플레이어 데이터 생성 성공"); // 생성 성공 확인
        Debug.Log("플레이어 0번 인덱스: " + player0.playerIndex); // 0번 플레이어 확인
        Debug.Log("플레이어 1번 인덱스: " + player1.playerIndex); // 1번 플레이어 확인

        TestCardCreation(player0); // 실제 카드 생성 테스트
    }

    // 실제 카드 인스턴스 생성 및 영역 이동 확인
    private void TestCardCreation(PlayerGameData playerData)
    {
        if (CardDatabaseManager.Instance == null)
        {
            Debug.LogError("CardDatabaseManager 인스턴스가 없습니다.");
            return;
        }

        if (CardZoneManager.Instance == null)
        {
            Debug.LogError("CardZoneManager 인스턴스가 없습니다.");
            return;
        }

        CardJsonData cardData = CardDatabaseManager.Instance.GetCardByID("UA11BT_GNT-1-001"); // 카드 원본 데이터 조회

        if (cardData == null)
        {
            Debug.LogError("해당 카드 ID를 찾을 수 없습니다.");
            return;
        }

        CardRuntimeData cardRuntimeData = BattleManager.Instance.CreateCardRuntimeData(
            cardData,
            playerData.playerIndex
        ); // 실제 카드 인스턴스 생성

        if (cardRuntimeData == null)
        {
            Debug.LogError("카드 인스턴스 생성에 실패했습니다.");
            return;
        }

        Debug.Log("카드 인스턴스 생성 성공"); // 생성 성공 확인
        Debug.Log("카드 ID: " + cardRuntimeData.cardData.CardID); // 원본 카드 ID 확인
        Debug.Log("카드 이름: " + cardRuntimeData.cardData.CardName); // 원본 카드 이름 확인
        Debug.Log("카드 인스턴스 번호: " + cardRuntimeData.instanceId); // 개별 카드 번호 확인
        Debug.Log("카드 소유 플레이어: " + cardRuntimeData.ownerPlayerIndex); // 소유 플레이어 확인

        bool moveResult = CardZoneManager.Instance.MoveCard(
            playerData,
            cardRuntimeData,
            BattleEnums.CardZone.Deck
        ); // 생성한 카드를 덱으로 이동

        if (!moveResult)
        {
            Debug.LogError("실제 카드를 덱으로 이동하는 데 실패했습니다.");
            return;
        }

        Debug.Log("실제 카드 덱 등록 성공"); // 덱 등록 확인
        Debug.Log("현재 덱 카드 수: " + playerData.deck.Count); // 덱 카드 수 확인
    }
}
