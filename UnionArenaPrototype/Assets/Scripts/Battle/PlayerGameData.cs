using System.Collections.Generic;

namespace Battle
{
    // 플레이어 게임 중 데이터 영역 별 관리
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
    }
}