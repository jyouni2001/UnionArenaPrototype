using System;
using System.Collections.Generic;
using System.IO;
using Data;
using UnityEngine;

namespace Manager
{
    // 공유 덱 데이터의 로드와 저장을 관리
    public class DeckManager : BaseManager
    {
        #region 싱글톤 및 덱 데이터

        private const string SharedDeckFileName = "Decks.json";
        private const int CurrentDataVersion = 2;

        public static DeckManager Instance { get; private set; }

        private readonly List<DeckData> decks = new List<DeckData>();
        private string deckDirectoryPath;
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

        // 싱글톤 등록
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

        // 싱글톤 참조 해제
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // 프로젝트 루트의 Decks 폴더에서 덱을 한 번 로드
        private bool EnsureLoaded()
        {
            if (isLoaded)
            {
                return true;
            }

            DirectoryInfo projectDirectory = Directory.GetParent(Application.dataPath);

            if (projectDirectory == null)
            {
                Debug.LogError("DeckManager: 프로젝트 경로를 확인할 수 없습니다.");
                return false;
            }

            deckDirectoryPath = Path.Combine(projectDirectory.FullName, "Decks");
            deckSavePath = Path.Combine(deckDirectoryPath, SharedDeckFileName);

            try
            {
                Directory.CreateDirectory(deckDirectoryPath);
            }
            catch (Exception exception)
            {
                Debug.LogError("DeckManager: 덱 폴더를 준비하지 못했습니다. " + exception.Message);
                return false;
            }

            if (!LoadDecks())
            {
                return false;
            }

            isLoaded = true;
            return true;
        }

        #endregion

        #region 덱 데이터 입출력

        // 공유 덱 파일을 우선 읽고, 없으면 기존 ID별 파일을 가져옴
        private bool LoadDecks()
        {
            if (File.Exists(deckSavePath))
            {
                if (!TryReadDeckFile(deckSavePath, out DeckSaveData saveData))
                {
                    return false;
                }

                AddUniqueDecks(saveData.Decks);
                Debug.Log($"DeckManager: 공유 덱 파일을 불러왔습니다. 덱 수 = {decks.Count}");
                return true;
            }

            int importedFileCount = 0;

            try
            {
                string[] legacyFilePaths = Directory.GetFiles(deckDirectoryPath, "*.json");
                Array.Sort(legacyFilePaths, StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < legacyFilePaths.Length; i++)
                {
                    if (string.Equals(legacyFilePaths[i], deckSavePath, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (!TryReadDeckFile(legacyFilePaths[i], out DeckSaveData legacyData))
                    {
                        Debug.LogWarning("DeckManager: 기존 덱 파일을 읽지 못해 건너뜁니다. " + Path.GetFileName(legacyFilePaths[i]));
                        continue;
                    }

                    int previousCount = decks.Count;
                    AddUniqueDecks(legacyData.Decks);

                    if (decks.Count > previousCount)
                    {
                        importedFileCount++;
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogError("DeckManager: 기존 덱 파일 검색에 실패했습니다. " + exception.Message);
                return false;
            }

            if (importedFileCount > 0)
            {
                Debug.Log($"DeckManager: 기존 덱 파일 {importedFileCount}개에서 덱 {decks.Count}개를 가져왔습니다. 기존 파일은 보존됩니다.");

                if (!SaveDecks())
                {
                    Debug.LogWarning("DeckManager: 통합 덱 파일 저장에 실패했습니다. 기존 파일은 보존되어 있습니다.");
                }
            }
            else
            {
                Debug.Log("DeckManager: 기존 덱 파일이 없어 빈 덱 목록으로 시작합니다.");
            }

            return true;
        }

        // JSON 파일을 읽고 데이터 버전을 확인
        private bool TryReadDeckFile(string filePath, out DeckSaveData saveData)
        {
            saveData = null;

            try
            {
                string json = File.ReadAllText(filePath);
                saveData = JsonUtility.FromJson<DeckSaveData>(json);

                if (saveData == null || saveData.Decks == null)
                {
                    Debug.LogWarning("DeckManager: 덱 저장 데이터 형식이 올바르지 않습니다. " + Path.GetFileName(filePath));
                    return false;
                }

                if (saveData.DataVersion < 1 || saveData.DataVersion > CurrentDataVersion)
                {
                    Debug.LogWarning($"DeckManager: 지원하지 않는 데이터 버전입니다. 파일 = {Path.GetFileName(filePath)}, 버전 = {saveData.DataVersion}");
                    return false;
                }

                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"DeckManager: 덱 파일을 읽지 못했습니다. 파일 = {Path.GetFileName(filePath)}, 오류 = {exception.Message}");
                return false;
            }
        }

        // 같은 덱 ID가 중복되지 않도록 목록에 추가
        private void AddUniqueDecks(IReadOnlyList<DeckData> sourceDecks)
        {
            if (sourceDecks == null)
            {
                return;
            }

            HashSet<string> deckIDs = new HashSet<string>();

            for (int i = 0; i < decks.Count; i++)
            {
                if (decks[i] != null && !string.IsNullOrWhiteSpace(decks[i].DeckID))
                {
                    deckIDs.Add(decks[i].DeckID);
                }
            }

            for (int i = 0; i < sourceDecks.Count; i++)
            {
                DeckData deckData = sourceDecks[i];

                if (deckData == null || string.IsNullOrWhiteSpace(deckData.DeckID))
                {
                    continue;
                }

                if (deckIDs.Add(deckData.DeckID))
                {
                    decks.Add(deckData);
                }
            }
        }

        // 현재 덱 목록을 프로젝트 Decks 폴더의 Decks.json에 저장
        public bool SaveDecks()
        {
            if (!EnsureLoaded())
            {
                return false;
            }

            DeckSaveData saveData = new DeckSaveData();

            for (int i = 0; i < decks.Count; i++)
            {
                if (decks[i] != null && !saveData.AddDeck(decks[i]))
                {
                    Debug.LogError("DeckManager: 덱 저장 데이터 구성에 실패했습니다.");
                    return false;
                }
            }

            try
            {
                string json = JsonUtility.ToJson(saveData, true);
                File.WriteAllText(deckSavePath, json);
                Debug.Log($"DeckManager: 덱 저장 완료. 덱 수 = {decks.Count}, 경로 = {deckSavePath}");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError("DeckManager: 덱 저장에 실패했습니다. " + exception.Message);
                return false;
            }
        }

        #endregion

        #region 덱 요청

        // 새 덱 생성
        public DeckData CreateDeck(string deckName)
        {
            if (!EnsureLoaded() || string.IsNullOrWhiteSpace(deckName))
            {
                return null;
            }

            DeckData deckData = new DeckData(Guid.NewGuid().ToString("N"), deckName);
            decks.Add(deckData);
            return deckData;
        }

        // 지정한 덱 삭제
        public bool RemoveDeck(string deckId)
        {
            if (!EnsureLoaded())
            {
                return false;
            }

            for (int i = 0; i < decks.Count; i++)
            {
                if (decks[i] != null && decks[i].DeckID == deckId)
                {
                    decks.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        // 덱 이름 변경
        public bool RenameDeck(string deckId, string newName)
        {
            if (!EnsureLoaded() || string.IsNullOrWhiteSpace(newName))
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

        // 덱 ID로 덱 조회
        public DeckData GetDeck(string deckId)
        {
            if (!EnsureLoaded())
            {
                return null;
            }

            for (int i = 0; i < decks.Count; i++)
            {
                if (decks[i] != null && decks[i].DeckID == deckId)
                {
                    return decks[i];
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

            DeckData deckData = GetDeck(deckId);
            return deckData != null && deckData.AddCard(cardId);
        }

        // 덱에서 카드 제거
        public bool RemoveCard(string deckId, string cardId)
        {
            if (!EnsureLoaded())
            {
                return false;
            }

            DeckData deckData = GetDeck(deckId);
            return deckData != null && deckData.RemoveCard(cardId);
        }

        #endregion
    }
}