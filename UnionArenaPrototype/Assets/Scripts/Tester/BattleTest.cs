using UnityEngine;
using Manager;
using Battle;
using Data;
using Enums;
using System.Collections.Generic;

namespace Tester
{
    // 전투 데이터 및 덱 시스템 테스트
    public class BattleTest : MonoBehaviour
    {
        private void Start()
        {
            TestPlayerGameData(); // 플레이어 데이터 테스트
            TestDeckCreation(); // 무작위 덱 생성 및 카드 추가 테스트
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

        // 33종의 카드 중 무작위로 50장 덱 생성 및 저장
        private void TestDeckCreation()
        {
            if (DeckManager.Instance == null)
            {
                Debug.LogError("DeckManager 인스턴스가 없습니다.");
                return;
            }

            if (CardDatabaseManager.Instance == null)
            {
                Debug.LogError("CardDatabaseManager 인스턴스가 없습니다.");
                return;
            }

            string[] cardIDs =
            {
                "UA11BT_GNT-1-001",
                "UA11BT_GNT-1-002",
                "UA11BT_GNT-1-003",
                "UA11BT_GNT-1-004",
                "UA11BT_GNT-1-005",
                "UA11BT_GNT-1-006",
                "UA11BT_GNT-1-007",
                "UA11BT_GNT-1-008",
                "UA11BT_GNT-1-009",
                "UA11BT_GNT-1-010",
                "UA11BT_GNT-1-011",
                "UA11BT_GNT-1-012",
                "UA11BT_GNT-1-013",
                "UA11BT_GNT-1-014",
                "UA11BT_GNT-1-015",
                "UA11BT_GNT-1-016",
                "UA11BT_GNT-1-017",
                "UA11BT_GNT-1-018",
                "UA11BT_GNT-1-019",
                "UA11BT_GNT-1-020",
                "UA11BT_GNT-1-021",
                "UA11BT_GNT-1-022",
                "UA11BT_GNT-1-023",
                "UA11BT_GNT-1-024",
                "UA11BT_GNT-1-025",
                "UA11BT_GNT-1-026",
                "UA11BT_GNT-1-027",
                "UA11BT_GNT-1-028",
                "UA11BT_GNT-1-029",
                "UA11BT_GNT-1-030",
                "UA11BT_GNT-1-031",
                "UA11BT_GNT-1-032",
                "UA11BT_GNT-1-033"
            };

            List<string> validCardIDs = new List<string>(); // 실제 등록된 카드 ID

            // 데이터베이스에 등록된 카드만 후보로 사용
            foreach (string cardID in cardIDs)
            {
                if (CardDatabaseManager.Instance.GetCardByID(cardID) != null)
                {
                    validCardIDs.Add(cardID);
                }
                else
                {
                    Debug.LogError("카드 데이터가 없습니다: " + cardID);
                }
            }

            if (validCardIDs.Count == 0)
            {
                Debug.LogError("사용할 수 있는 카드가 없습니다.");
                return;
            }

            DeckData deckData = DeckManager.Instance.CreateDeck("무작위 50장 덱"); // 새 덱 생성

            if (deckData == null)
            {
                Debug.LogError("덱 생성에 실패했습니다.");
                return;
            }

            int totalCardCount = 0; // 현재 덱 카드 수
            const int targetCardCount = 50; // 목표 덱 카드 수
            const int maxCardCount = 4; // 동일 카드 최대 수량

            // 50장이 될 때까지 무작위 카드 선택
            while (totalCardCount < targetCardCount)
            {
                int randomIndex = Random.Range(0, validCardIDs.Count); // 무작위 카드 인덱스
                string selectedCardID = validCardIDs[randomIndex];

                int currentCount = 0; // 선택한 카드의 현재 수량

                foreach (DeckCardData cardData in deckData.Cards)
                {
                    if (cardData.CardID == selectedCardID)
                    {
                        currentCount = cardData.Count;
                        break;
                    }
                }

                if (currentCount >= maxCardCount)
                {
                    continue; // 최대 수량에 도달한 카드는 건너뜀
                }

                if (DeckManager.Instance.AddCard(deckData.DeckID, selectedCardID))
                {
                    totalCardCount++; // 카드 추가 성공 시 전체 수량 증가
                }
            }

            // 덱 JSON 저장
            if (!DeckManager.Instance.SaveDecks())
            {
                Debug.LogError("무작위 덱 저장에 실패했습니다.");
                DeckManager.Instance.RemoveDeck(deckData.DeckID);
                DeckManager.Instance.SaveDecks(); // 실패한 덱 제거 상태 저장
                return;
            }

            Debug.Log("무작위 덱 생성 및 저장 성공");
            Debug.Log("덱 이름: " + deckData.DeckName);
            Debug.Log("덱 ID: " + deckData.DeckID);
            Debug.Log("전체 카드 수: " + totalCardCount);

            // 생성된 덱의 카드별 수량 확인
            foreach (DeckCardData cardData in deckData.Cards)
            {
                Debug.Log(
                    "카드 ID: " + cardData.CardID +
                    " / 수량: " + cardData.Count
                );
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
