using UnityEngine;
using Data;

namespace Battle
{
    // 카드 한 장의 현재 상태를 관리하는 데이터 클래스
    [System.Serializable]
    public class CardRuntimeData
    {
        #region Card Identity // 카드 기본 정보
        
        public int instanceId; // 게임 내 카드 개별 식별 번호
        public CardJsonData cardData; // 원본 카드 데이터

        #endregion

        #region Card Location // 카드 위치 정보
        
        public BattleEnums.CardZone currentZone; // 덱, 핸드, 필드, 시큐리티 구분
        public BattleEnums.FieldPosition fieldPosition; // 프론트/에너지 라인 구분

        #endregion

        #region Card Ownership // 카드 소유자 정보
        
        public int ownerPlayerIndex; // 카드 소유 식별 번호

        #endregion
    }
}
