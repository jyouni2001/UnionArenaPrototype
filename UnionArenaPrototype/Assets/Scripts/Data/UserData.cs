using UnityEngine;

namespace Data
{
    // 로컬 사용자 정보
    [System.Serializable]
    public class UserData
    {
        [SerializeField] private string localUserID; // 로컬 사용자 고유 UID
        [SerializeField] private string displayName = "Player"; // 사용자 표시 이름

        public string LocalUserID => localUserID; // 로컬 UID 조회
        public string DisplayName => displayName; // 표시 이름 조회

        public UserData()
        {
        }

        public UserData(string localUserID, string displayName)
        {
            this.localUserID = localUserID; // 로컬 UID 저장
            this.displayName = displayName; // 표시 이름 저장
        }

        // 표시 이름 변경
        internal void ChangeDisplayName(string newDisplayName)
        {
            if (string.IsNullOrWhiteSpace(newDisplayName))
            {
                return;
            }

            displayName = newDisplayName; // 새로운 표시 이름 저장
        }
    }
}