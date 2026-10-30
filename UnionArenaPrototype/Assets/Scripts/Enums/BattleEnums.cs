using UnityEngine;

namespace Enums
{
    public class BattleEnums 
    {
        #region Card  Zone
        public enum CardZone
        {
            Error = -1, // 에러
            None = 0, // 없음
            Deck = 1, // 덱
            Hand = 2, // 핸드
            Field = 3, // 필드
            Security = 4, // 시큐리티
            Trash = 5, // 트래쉬
            Remove = 6 // 리무브
        }
        
        #endregion

        #region

        public enum FieldPosition
        {
            Error = -1, // 에러
            None = 0, // 없음
            FrontLine = 1, // 프론트라인
            EnergyLine = 2 // 에너지라인
        }

        #endregion
    }
}