namespace Enums
{
    // 전투에서 사용하는 카드 영역, 필드 위치, 플레이어, 결과와 턴 페이즈 구분
    public class BattleEnums
    {
        #region 카드 영역

        // 카드가 현재 속한 게임 영역
        public enum CardZone
        {
            Error = -1,   // 잘못된 영역 값
            None = 0,     // 영역 없음
            Deck = 1,     // 덱
            Hand = 2,     // 손패
            Field = 3,    // 필드
            Security = 4, // 시큐리티
            Trash = 5,    // 트래시
            Remove = 6    // 제외존
        }

        #endregion

        #region 필드 위치

        // 필드 카드가 놓이는 라인
        public enum FieldPosition
        {
            Error = -1,      // 잘못된 위치 값
            None = 0,        // 필드 위치 없음
            FrontLine = 1,   // 프론트라인
            EnergyLine = 2   // 에너지라인
        }

        #endregion

        #region 플레이어 구분

        // 게임 내 플레이어 구분
        public enum PlayerType
        {
            PlayerA = 1, // PlayerA
            PlayerB = 2  // PlayerB
        }

        #endregion

        #region 전투 결과

        // 공격 및 블록 처리 후 판정된 결과
        public enum CombatResolutionType
        {
            None,               // 판정 결과 없음
            UnblockedAttack,    // 블록 없이 공격 통과
            AttackerWins,       // 공격자가 이겨 방어 카드 퇴장
            AttackFails         // 방어자가 공격을 막음
        }

        #endregion

        #region 턴 페이즈

        // 한 플레이어의 턴이 진행되는 순서
        public enum TurnPhase
        {
            None,       // 진행 중인 페이즈 없음
            Start,      // AP 활성화, 필드 활성화와 드로우
            ExtraDraw,  // AP 1을 사용한 추가 드로우 선택
            Move,       // 에너지 존 카드를 프론트라인으로 이동
            Main,       // 카드 등장과 효과 처리
            Battle,     // 프론트라인 카드 공격
            End         // 턴 종료 처리와 상대에게 턴 전달
        }

        #endregion
    }
}