
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Data;

namespace Manager
{
    // 사용자의 덱 정보를 관리하는 매니저
    public class DeckManager : BaseManager
    {
        public static DeckManager Instance { get; private set; } // 매니저 싱글톤

        public IReadOnlyList<DeckData> Decks
        {
            get
            {
                EnsureLoaded(); // 덱 데이터 불러오기 확인
                return decks;
            }
        }

        private List<DeckData> decks = new List<DeckData>(); // 현재 덱 목록
        private string deckSavePath; // 덱 저장 파일 경로
        private bool isLoaded; // 덱 데이터 불러오기 여부

        // 매니저 초기화
        protected override void Initialize()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject); // 중복 매니저 제거
                return;
            }

            Instance = this; // 싱글톤 등록
            DontDestroyOnLoad(gameObject); // 씬 이동 시 유지

            base.Initialize();
        }

        // 덱 데이터 초기화 여부 확인
        private bool EnsureLoaded()
        {
            if (isLoaded)
            {
                return true;
            }

            if (UserManager.Instance == null ||
                UserManager.Instance.CurrentUserData == null)
            {
                Debug.LogError("사용자 데이터가 초기화되지 않았습니다.");
                return false;
            }

            string userID = UserManager.Instance.CurrentUserData.LocalUserID; // 현재 사용자 UID

            if (string.IsNullOrWhiteSpace(userID))
            {
                Debug.LogError("사용자 UID가 존재하지 않습니다.");
                return false;
            }

            deckSavePath = Path.Combine(
                Application.persistentDataPath,
                "Decks",
                userID + ".json"
            ); // 사용자별 덱 저장 경로

            return LoadDecks(userID); // 저장된 덱 불러오기
        }

        // 덱 데이터 불러오기
        private bool LoadDecks(string userID)
        {
            if (!File.Exists(deckSavePath))
            {
                isLoaded = true; // 저장 파일이 없으면 빈 덱 목록으로 시작
                Debug.Log("저장된 덱 파일이 없습니다. 새로운 덱 목록을 사용합니다.");
                return true;
            }

            try
            {
                string json = File.ReadAllText(deckSavePath); // JSON 파일 읽기
                DeckSaveData saveData = JsonUtility.FromJson<DeckSaveData>(json); // JSON 데이터 변환

                if (saveData == null)
                {
                    Debug.LogError("덱 저장 파일을 읽을 수 없습니다.");
                    return false;
                }

                if (saveData.UserID != userID)
                {
                    Debug.LogError("덱 저장 파일의 사용자 UID가 일치하지 않습니다.");
                    return false;
                }

                if (saveData.DataVersion != 1)
                {
                    Debug.LogError("지원하지 않는 덱 저장 데이터 버전입니다.");
                    return false;
                }

                decks.Clear(); // 기존 덱 목록 초기화

                foreach (DeckData deckData in saveData.Decks)
                {
                    if (deckData != null)
                    {
                        decks.Add(deckData); // 불러온 덱 추가
                    }
                }

                isLoaded = true; // 덱 데이터 불러오기 완료

                Debug.Log("덱 데이터 불러오기 성공: " + decks.Count + "개");
                return true;
            }
            catch (System.Exception exception)
            {
                Debug.LogError("덱 데이터 불러오기 실패: " + exception.Message);
                return false;
            }
        }

        // 덱 데이터 저장
        public bool SaveDecks()
        {
            if (!EnsureLoaded())
            {
                return false;
            }

            string userID = UserManager.Instance.CurrentUserData.LocalUserID; // 현재 사용자 UID
            DeckSaveData saveData = new DeckSaveData(userID); // 저장 데이터 생성

            foreach (DeckData deckData in decks)
            {
                if (!saveData.AddDeck(deckData))
                {
                    Debug.LogError("덱 저장 데이터 구성에 실패했습니다.");
                    return false;
                }
            }

            try
            {
                string directoryPath = Path.GetDirectoryName(deckSavePath); // 저장 폴더 경로
                Directory.CreateDirectory(directoryPath); // 저장 폴더 생성

                string json = JsonUtility.ToJson(saveData, true); // JSON 문자열 변환
                File.WriteAllText(deckSavePath, json); // JSON 파일 저장

                Debug.Log("덱 데이터 저장 성공: " + decks.Count + "개");
                return true;
            }
            catch (System.Exception exception)
            {
                Debug.LogError("덱 데이터 저장 실패: " + exception.Message);
                return false;
            }
        }

        // 새로운 덱 생성
        public DeckData CreateDeck(string deckName)
        {
            if (!EnsureLoaded())
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(deckName))
            {
                return null;
            }

            string deckId = System.Guid.NewGuid().ToString("N"); // 덱 고유 ID 생성
            DeckData deckData = new DeckData(deckId, deckName); // 덱 데이터 생성

            decks.Add(deckData); // 덱 목록에 추가

            return deckData;
        }

        // 덱 삭제
        public bool RemoveDeck(string deckId)
        {
            if (!EnsureLoaded())
            {
                return false;
            }

            for (int i = 0; i < decks.Count; i++)
            {
                if (decks[i].DeckID == deckId)
                {
                    decks.RemoveAt(i); // 해당 덱 제거
                    return true;
                }
            }

            return false;
        }

        // 덱 이름 변경
        public bool RenameDeck(string deckId, string newName)
        {
            if (!EnsureLoaded())
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(newName))
            {
                return false;
            }

            DeckData deckData = GetDeck(deckId); // 대상 덱 조회

            if (deckData == null)
            {
                return false;
            }

            deckData.Rename(newName); // 덱 이름 변경

            return true;
        }

        // 덱 ID로 덱 조회
        public DeckData GetDeck(string deckId)
        {
            if (!EnsureLoaded())
            {
                return null;
            }

            foreach (DeckData deckData in decks)
            {
                if (deckData.DeckID == deckId)
                {
                    return deckData;
                }
            }

            return null;
        }

        // 덱에 카드 추가
        public bool AddCard(string deckId, string cardId)
        {
            if (!EnsureLoaded())
            {
                return false;
            }

            DeckData deckData = GetDeck(deckId); // 대상 덱 조회

            if (deckData == null)
            {
                return false;
            }

            return deckData.AddCard(cardId); // 카드 추가 요청
        }

        // 덱에서 카드 제거
        public bool RemoveCard(string deckId, string cardId)
        {
            if (!EnsureLoaded())
            {
                return false;
            }

            DeckData deckData = GetDeck(deckId); // 대상 덱 조회

            if (deckData == null)
            {
                return false;
            }

            return deckData.RemoveCard(cardId); // 카드 제거 요청
        }
    }
}
