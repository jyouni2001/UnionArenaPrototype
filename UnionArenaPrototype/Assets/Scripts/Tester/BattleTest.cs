
using UnityEngine;
using Manager;
using Battle;
using Data;
using Enums;

namespace Tester
{
    // 전투 데이터 및 덱 시스템 테스트
    public class BattleTest : MonoBehaviour
    {
        private void Start()
        {
            TestPlayerGameData(); // 플레이어 데이터 테스트
            TestDeckCreation(); // 덱 생성 및 카드 추가 테스트
            TestDeckManagement(); // 덱 이름 변경 및 삭제 테스트
        }

        // 플레이어 데이터 생성 확인
        private void TestPlayerGameData()
        {
            if (BattleManager.Instance == null)
            {
                Debug.LogError("BattleManager 인스턴스가 없습니다.");
                return;
            }

            PlayerGameData player0 = BattleManager.Instance.GetPlayerGameData(0); // 첫 번째 플레이어
            PlayerGameData player1 = BattleManager.Instance.GetPlayerGameData(1); // 두 번째 플레이어

            if (player0 == null || player1 == null)
            {
                Debug.LogError("플레이어 데이터 생성에 실패했습니다.");
                return;
            }

            Debug.Log("플레이어 데이터 생성 성공");
            Debug.Log("플레이어 0 인덱스: " + player0.playerIndex);
            Debug.Log("플레이어 1 인덱스: " + player1.playerIndex);

            TestCardCreation(player0); // 카드 생성 테스트
        }

        // 카드 생성 및 배치 확인
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

            CardJsonData cardData = CardDatabaseManager.Instance.GetCardByID(
                "UA11BT_GNT-1-001"
            ); // 테스트 카드 조회

            if (cardData == null)
            {
                Debug.LogError("테스트 카드를 찾을 수 없습니다.");
                return;
            }

            CardRuntimeData cardRuntimeData =
                BattleManager.Instance.CreateCardRuntimeData(
                    cardData,
                    playerData.playerIndex
                ); // 런타임 카드 생성

            if (cardRuntimeData == null)
            {
                Debug.LogError("런타임 카드 생성에 실패했습니다.");
                return;
            }

            Debug.Log("런타임 카드 생성 성공");
            Debug.Log("카드 ID: " + cardRuntimeData.cardData.CardID);
            Debug.Log("카드 이름: " + cardRuntimeData.cardData.CardName);
            Debug.Log("카드 인스턴스 ID: " + cardRuntimeData.instanceId);
            Debug.Log("카드 소유자: " + cardRuntimeData.ownerPlayerIndex);

            bool moveResult = CardZoneManager.Instance.MoveCard(
                playerData,
                cardRuntimeData,
                BattleEnums.CardZone.Deck,
                BattleEnums.FieldPosition.None
            ); // 카드를 덱 영역으로 이동

            if (moveResult)
            {
                Debug.Log("카드 영역 이동 성공");
            }
            else
            {
                Debug.LogError("카드 영역 이동 실패");
            }
        }

        // 덱 생성 및 카드 추가 확인
        private void TestDeckCreation()
        {
            if (DeckManager.Instance == null)
            {
                Debug.LogError("DeckManager 인스턴스가 없습니다.");
                return;
            }

            DeckData deckData = DeckManager.Instance.CreateDeck("테스트 덱"); // 테스트 덱 생성

            if (deckData == null)
            {
                Debug.LogError("덱 생성에 실패했습니다.");
                return;
            }

            Debug.Log("덱 생성 성공");
            Debug.Log("덱 이름: " + deckData.DeckName);
            Debug.Log("덱 ID: " + deckData.DeckID);
            Debug.Log("현재 보유 덱 개수: " + DeckManager.Instance.Decks.Count);

            int successCount = 0; // 카드 추가 성공 횟수

            for (int i = 0; i < 5; i++)
            {
                bool addResult = DeckManager.Instance.AddCard(
                    deckData.DeckID,
                    "UA11BT_GNT-1-001"
                ); // 동일 카드 추가 시도

                if (addResult)
                {
                    successCount++; // 성공 횟수 증가
                    Debug.Log((i + 1) + "번째 카드 추가 성공");
                }
                else
                {
                    Debug.Log((i + 1) + "번째 카드 추가 거부");
                }
            }

            int totalCardCount = 0; // 전체 카드 수량

            foreach (DeckCardData cardData in deckData.Cards)
            {
                totalCardCount += cardData.Count; // 카드 수량 합산

                Debug.Log(
                    "덱 카드 ID: " + cardData.CardID +
                    " / 수량: " + cardData.Count
                );
            }

            Debug.Log("카드 추가 성공 횟수: " + successCount);
            Debug.Log("현재 테스트 덱 카드 수: " + totalCardCount);

            if (successCount == 4 && totalCardCount == 4)
            {
                Debug.Log("동일 카드 최대 4장 제한 테스트 성공");
            }
            else
            {
                Debug.LogError("동일 카드 최대 4장 제한 테스트 실패");
            }
            
            // 덱 저장 테스트
            bool saveResult = DeckManager.Instance.SaveDecks(); // 덱 JSON 저장

            if (saveResult)
            {
                Debug.Log("덱 JSON 저장 테스트 성공");
            }
            else
            {
                Debug.LogError("덱 JSON 저장 테스트 실패");
            }
        }

        // 덱 이름 변경 및 삭제 테스트
        private void TestDeckManagement()
        {
            if (DeckManager.Instance == null)
            {
                Debug.LogError("DeckManager 인스턴스가 없습니다.");
                return;
            }

            DeckData deckData = DeckManager.Instance.CreateDeck("변경 전 덱"); // 테스트 덱 생성

            if (deckData == null)
            {
                Debug.LogError("테스트 덱 생성 실패");
                return;
            }

            string deckId = deckData.DeckID; // 테스트 덱 ID 저장

            Debug.Log("덱 이름 변경 및 삭제 테스트 시작");
            Debug.Log("변경 전 덱 이름: " + deckData.DeckName);

            bool renameResult = DeckManager.Instance.RenameDeck(
                deckId,
                "변경 후 덱"
            ); // 덱 이름 변경

            if (renameResult && deckData.DeckName == "변경 후 덱")
            {
                Debug.Log("덱 이름 변경 테스트 성공");
                Debug.Log("변경된 덱 이름: " + deckData.DeckName);
            }
            else
            {
                Debug.LogError("덱 이름 변경 테스트 실패");
            }

            bool removeResult = DeckManager.Instance.RemoveDeck(deckId); // 덱 삭제

            if (removeResult && DeckManager.Instance.GetDeck(deckId) == null)
            {
                Debug.Log("덱 삭제 테스트 성공");
            }
            else
            {
                Debug.LogError("덱 삭제 테스트 실패");
            }
        }
    }
}
