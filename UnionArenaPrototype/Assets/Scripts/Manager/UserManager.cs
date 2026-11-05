
using System;
using System.IO;
using UnityEngine;
using Data;

namespace Manager
{
    // 로컬 사용자 데이터의 저장 경로와 로드·저장 시점을 조율
    public class UserManager : BaseManager
    {
        #region 싱글톤 및 사용자 데이터

        public static UserManager Instance { get; private set; }

        public UserData CurrentUserData => currentUserData;

        private UserData currentUserData;
        private string userDataPath;

        #endregion

        #region 생명 주기 및 사용자 데이터 준비

        // 싱글톤을 등록하고 사용자 데이터를 준비
        protected override void Initialize()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            userDataPath = Path.Combine(
                Application.persistentDataPath,
                "UserData.json");

            LoadUserData();

            base.Initialize();
        }

        // 매니저가 제거될 때 싱글톤 참조 해제
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // 저장 파일에서 사용자 정보를 읽고 없으면 새로 생성
        private void LoadUserData()
        {
            if (File.Exists(userDataPath))
            {
                string json = File.ReadAllText(userDataPath);
                currentUserData = JsonUtility.FromJson<UserData>(json);

                if (currentUserData != null && !string.IsNullOrWhiteSpace(currentUserData.LocalUserID))
                {
                    return;
                }
            }

            CreateUserData();
        }

        #endregion

        #region 사용자 데이터 변경 및 저장

        // 로컬 사용자 ID와 기본 표시 이름을 생성해 저장
        private void CreateUserData()
        {
            string localUserID = Guid.NewGuid().ToString("N");

            currentUserData = new UserData(localUserID, "Player");

            SaveUserData();
        }

        // 현재 사용자 데이터를 JSON 파일에 기록
        private void SaveUserData()
        {
            string json = JsonUtility.ToJson(currentUserData, true);
            File.WriteAllText(userDataPath, json);
        }

        // 표시 이름 변경 요청을 데이터 객체에 전달하고 저장
        public void ChangeDisplayName(string newDisplayName)
        {
            if (currentUserData == null || string.IsNullOrWhiteSpace(newDisplayName))
            {
                return;
            }

            currentUserData.ChangeDisplayName(newDisplayName);
            SaveUserData();
        }

        #endregion
    }
}
