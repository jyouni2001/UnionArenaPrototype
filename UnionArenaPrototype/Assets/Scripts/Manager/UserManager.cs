
using System;
using System.IO;
using UnityEngine;
using Data;

namespace Manager
{
    // 로컬 사용자 정보 관리
    public class UserManager : BaseManager
    {
        public static UserManager Instance { get; private set; } // 싱글톤 인스턴스

        public UserData CurrentUserData => currentUserData; // 현재 사용자 정보 조회

        private UserData currentUserData; // 현재 사용자 데이터
        private string userDataPath; // 사용자 JSON 파일 경로

        // 초기화 처리
        protected override void Initialize()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this; // 싱글톤 등록
            DontDestroyOnLoad(gameObject); // Scene 변경 시 유지

            userDataPath = Path.Combine(Application.persistentDataPath, "UserData.json"); // 저장 경로 설정

            LoadUserData(); // 사용자 정보 불러오기

            base.Initialize(); // 초기화 완료 처리
        }

        // 사용자 정보 불러오기
        private void LoadUserData()
        {
            if (File.Exists(userDataPath))
            {
                string json = File.ReadAllText(userDataPath); // JSON 파일 읽기
                currentUserData = JsonUtility.FromJson<UserData>(json); // 사용자 데이터 변환

                if (currentUserData != null && !string.IsNullOrWhiteSpace(currentUserData.LocalUserID))
                {
                    return;
                }
            }

            CreateUserData(); // 사용자 정보가 없으면 새로 생성
        }

        // 새로운 사용자 정보 생성
        private void CreateUserData()
        {
            string localUserID = Guid.NewGuid().ToString("N"); // 새로운 UUID 생성

            currentUserData = new UserData(localUserID, "Player"); // 사용자 데이터 생성

            SaveUserData(); // 사용자 정보 저장
        }

        // 사용자 정보 저장
        private void SaveUserData()
        {
            string json = JsonUtility.ToJson(currentUserData, true); // JSON 변환
            File.WriteAllText(userDataPath, json); // 파일 저장
        }

        // 사용자 표시 이름 변경
        public void ChangeDisplayName(string newDisplayName)
        {
            if (currentUserData == null || string.IsNullOrWhiteSpace(newDisplayName))
            {
                return;
            }

            currentUserData.ChangeDisplayName(newDisplayName); // 표시 이름 변경
            SaveUserData(); // 변경 정보 저장
        }
    }
}
