using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Data;

namespace Manager
{
    // 덱 저장 데이터의 로드와 저장을 조율하고 덱 조회 요청을 전달
    public class DeckManager : BaseManager
    {
        #region 싱글톤 및 덱 데이터

        public static DeckManager Instance { get; private set; }

        private readonly List<DeckData> decks = new List<DeckData>();
        private string deckSavePath;
        private bool isLoaded;

        public IReadOnlyList<DeckData> Decks
        {
            get
            {
                EnsureLoaded();
                return decks;
            }
        }

        #endregion

        #region 생명 주기 및 로드 준비

        // 싱글톤을 등록하고 공통 초기화 상태를 기록
        protected override void Initialize()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

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

        // 사용자 데이터가 준비된 뒤 덱 파일을 한 번만 로드
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

            string userID = UserManager.Instance.CurrentUserData.LocalUserID;

            if (string.IsNullOrWhiteSpace(userID))
            {
                Debug.LogError("사용자 UID가 존재하지 않습니다.");
                return false;
            }

            string projectPath = Directory.GetParent(Application.dataPath).FullName;

            deckSavePath = Path.Combine(projectPath, "Decks", userID + ".json");

            return LoadDecks(userID);
        }

        #endregion

        #region 덱 저장 데이터 입출력

        // 사용자 ID에 해당하는 저장 파일을 읽어 덱 목록에 등록
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

        // 현재 덱 목록을 사용자 저장 파일에 기록
        public bool SaveDecks()
        {
            if (!EnsureLoaded())
            {
                return false;
            }

            string userID = UserManager.Instance.CurrentUserData.LocalUserID;
            DeckSaveData saveData = new DeckSaveData(userID);

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

        #endregion

        #region 덱 요청 전달

        // 덱 생성 요청을 데이터 객체에 전달하고 목록에 등록
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

            string deckId = System.Guid.NewGuid().ToString("N");
            DeckData deckData = new DeckData(deckId, deckName);

            decks.Add(deckData);

            return deckData;
        }

        // 지정한 덱 삭제 요청을 처리
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
                    decks.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        // 지정한 덱 이름 변경 요청을 데이터 객체에 전달
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

            DeckData deckData = GetDeck(deckId);

            if (deckData == null)
            {
                return false;
            }

            deckData.Rename(newName);

            return true;
        }

        // 덱 ID에 해당하는 덱 참조 반환
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

        // 카드 추가 요청을 대상 덱 데이터에 전달
        public bool AddCard(string deckId, string cardId)
        {
            if (!EnsureLoaded())
            {
                return false;
            }

            DeckData deckData = GetDeck(deckId);

            if (deckData == null)
            {
                return false;
            }

            return deckData.AddCard(cardId);
        }

        // 카드 제거 요청을 대상 덱 데이터에 전달
        public bool RemoveCard(string deckId, string cardId)
        {
            if (!EnsureLoaded())
            {
                return false;
            }

            DeckData deckData = GetDeck(deckId);

            if (deckData == null)
            {
                return false;
            }

            return deckData.RemoveCard(cardId);
        }

        #endregion
    }
}
