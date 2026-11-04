using System.Collections.Generic;

namespace Battle
{
    // 플레이어의 게임 진행 및 카드 영역 데이터 관리
    [System.Serializable]
    public class PlayerGameData
    {
        public int playerIndex; // 플레이어 번호

        public List<CardRuntimeData> deck = new List<CardRuntimeData>(); // 덱
        public List<CardRuntimeData> hand = new List<CardRuntimeData>(); // 손패
        public List<CardRuntimeData> security = new List<CardRuntimeData>(); // 시큐리티
        public List<CardRuntimeData> field = new List<CardRuntimeData>(); // 필드
        public List<CardRuntimeData> trash = new List<CardRuntimeData>(); // 트래쉬
        public List<CardRuntimeData> remove = new List<CardRuntimeData>(); // 리무브

        public int turnCount = 0; // 이 플레이어가 시작한 턴 수
        public int apCardCount = 0; // AP 영역의 총 AP 수
        public int activeAP = 0; // 사용할 수 있는 AP
        public int restingAP = 0; // 사용 후 휴식 상태인 AP
        public bool hasTakenExtraDrawThisTurn = false; // 이번 턴 추가 드로우 여부

        // AP를 지불하고 휴식 상태로 전환
        public bool TrySpendAP(int amount)
        {
            if (amount <= 0 || activeAP < amount)
            {
                return false;
            }

            activeAP -= amount;
            restingAP += amount;
            return true;
        }

        // 새 턴에 맞는 AP 장수를 준비하고 모두 활성화
        public bool RefreshAP(int targetAPCardCount)
        {
            if (targetAPCardCount < 0 || targetAPCardCount > 3)
            {
                return false;
            }

            // AP 영역의 카드 수는 게임 중 감소하지 않음
            if (targetAPCardCount > apCardCount)
            {
                apCardCount = targetAPCardCount;
            }

            activeAP = apCardCount;
            restingAP = 0;
            hasTakenExtraDrawThisTurn = false;

            return true;
        }
    }
}