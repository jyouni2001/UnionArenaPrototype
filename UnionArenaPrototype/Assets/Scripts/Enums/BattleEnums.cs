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

        #region FieldPosition

        public enum FieldPosition
        {
            Error = -1, // 에러
            None = 0, // 없음
            FrontLine = 1, // 프론트라인
            EnergyLine = 2 // 에너지라인
        }

        #endregion

        #region PlayerType

        // 게임 내 플레이어를 구분하는 enum
        public enum PlayerType
        {
            PlayerA = 1,    // 방장
            PlayerB = 2    // 참가자
        }

        #endregion
        
    }
}