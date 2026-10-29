using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// CSV를 JSON으로 변환
namespace Editor
{
    public class CardCsvToJsonConverter : EditorWindow 
    {
        [Serializable] 
        public class CardList // 카드 목록을 담는 클래스
        {
            public List<CardData> cards = new List<CardData>(); // 카드 데이터 목록
        }

        [Serializable]
        public class CardData // 카드 한 장의 데이터
        {
            public string cardID; // 카드 고유 ID
            public string cardName; // 카드 이름
            public int cardColor; // 카드 색상 번호
            public int cardType; // 카드 종류 번호
            public int bp; // 카드 BP
            public int requiredEnColor; // 필요한 에너지 색상 번호
            public int requiredEnCost; // 필요한 에너지 비용
            public int requiredApCost; // 필요한 AP 비용
            public int cardTrigger; // 카드 발동 조건 번호
            public string effectText; // 카드 효과 설명
            public string[] cardFeatures; // 카드 특징 목록
            public KeywordData[] cardKeywords; // 카드 키워드 목록
        }

        [Serializable] 
        public class KeywordData // 카드 키워드 데이터
        {
            public int cardKeywordType; // 키워드 종류 번호
            public int value; // 키워드 수치
        }

        [Serializable]
        public class StringArrayWrapper // 문자열 배열을 감싸는 클래스
        {
            public string[] items; // 문자열 목록
        }

        [Serializable] 
        public class KeywordArrayWrapper // 키워드 배열을 감싸는 클래스
        {
            public KeywordData[] items; // 키워드 목록
        }

        [MenuItem("Tools/Card CSV To JSON")] // Unity 상단 메뉴에 변환 도구 등록
        public static void OpenWindow() // 변환 창 열기
        {
            GetWindow<CardCsvToJsonConverter>("카드 CSV 변환"); // 변환 창 표시
        }

        private void OnGUI() // 변환 창 화면 구성
        {
            GUILayout.Label("카드 CSV → JSON 변환", EditorStyles.boldLabel); // 창 제목 표시

            if (GUILayout.Button("CSV 선택 및 JSON 생성")) // 변환 버튼 표시
            {
                ConvertCsv(); // CSV 변환 실행
            }
        }

        private void ConvertCsv() // CSV 파일을 JSON으로 변환
        {
            string csvPath = EditorUtility.OpenFilePanel("UnionArenaCards.csv 선택", "", "csv"); // CSV 파일 선택 창 표시

            if (string.IsNullOrEmpty(csvPath)) // 파일을 선택하지 않은 경우
            {
                return; // 변환 종료
            }

            try // 변환 중 오류 처리 시작
            {
                string[] lines = File.ReadAllLines(csvPath, Encoding.UTF8); // CSV 전체 행 읽기
                CardList cardList = new CardList(); // 카드 목록 생성

                for (int i = 1; i < lines.Length; i++) // 첫 번째 행인 제목을 제외하고 반복
                {
                    if (string.IsNullOrWhiteSpace(lines[i])) // 빈 행인지 확인
                    {
                        continue; // 빈 행 건너뛰기
                    }

                    List<string> columns = ParseCsvLine(lines[i]); // CSV 한 행을 열 단위로 분리

                    if (columns.Count != 12) // 열이 12개인지 확인
                    {
                        Debug.LogError($"CSV {i + 1}행의 열 개수가 12개가 아닙니다: {columns.Count}"); // 오류 출력
                        return; // 변환 종료
                    }

                    if (string.IsNullOrWhiteSpace(columns[0]) ||
                        string.IsNullOrWhiteSpace(columns[1])) // 카드 ID와 이름 확인
                    {
                        Debug.LogError($"CSV {i + 1}행의 카드 ID 또는 이름이 비어 있습니다."); // 오류 출력
                        return; // 변환 종료
                    }

                    CardData card = new CardData
                    {
                        cardID = columns[0].Trim(), // 카드 ID 입력
                        cardName = columns[1].Trim(), // 카드 이름 입력
                        cardColor = int.Parse(columns[2]), // 카드 색상 번호 변환
                        cardType = int.Parse(columns[3]), // 카드 종류 번호 변환
                        bp = int.Parse(columns[4]), // BP 변환
                        requiredEnColor = int.Parse(columns[5]), // 필요 에너지 색상 변환
                        requiredEnCost = int.Parse(columns[6]), // 필요 에너지 비용 변환
                        requiredApCost = int.Parse(columns[7]), // 필요 AP 비용 변환
                        cardTrigger = int.Parse(columns[8]), // 발동 조건 번호 변환
                        effectText = columns[9], // 효과 설명 입력

                        cardFeatures = JsonUtility.FromJson<StringArrayWrapper>("{\"items\":" + columns[10] + "}").items, // 특징 JSON 배열 변환

                        cardKeywords = JsonUtility.FromJson<KeywordArrayWrapper>("{\"items\":" + columns[11] + "}").items // 키워드 JSON 배열 변환
                    };

                    cardList.cards.Add(card); // 변환한 카드 등록
                }

                string json = JsonUtility.ToJson(cardList, true);

                string outputPath = EditorUtility.SaveFilePanel("변환된 JSON 저장",Application.dataPath + "/Resources/CardData",
                    "Cards_Converted.json","json"); // JSON 저장 위치 선택

                if (string.IsNullOrEmpty(outputPath)) // 저장 위치를 선택하지 않은 경우
                {
                    return; // 저장 종료
                }

                File.WriteAllText(
                    outputPath, json, new UTF8Encoding(false));

                AssetDatabase.Refresh(); // Unity 에셋 목록 갱신

                Debug.Log($"CSV 변환 완료: {cardList.cards.Count}장\n{outputPath}"); // 변환 결과 출력

                EditorUtility.DisplayDialog("변환 완료",$"카드 {cardList.cards.Count}장을 JSON으로 변환했습니다.","확인"); // 완료 메시지 표시
            }
            catch (Exception e) // 변환 중 발생한 오류 처리
            {
                Debug.LogError("CSV 변환 실패: " + e.Message); // 오류 내용 출력

                EditorUtility.DisplayDialog("변환 실패",e.Message,"확인"); // 오류 메시지 표시
            }
        }

        private List<string> ParseCsvLine(string line) // CSV 한 행을 열 단위로 분리
        {
            List<string> result = new List<string>(); // 분리된 열 목록
            StringBuilder value = new StringBuilder(); // 현재 열의 문자열
            bool insideQuotes = false; // 큰따옴표 안에 있는지 확인

            for (int i = 0; i < line.Length; i++) // 문자열을 한 글자씩 확인
            {
                char c = line[i]; // 현재 문자 가져오기

                if (c == '"') // 큰따옴표를 만난 경우
                {
                    if (insideQuotes &&
                        i + 1 < line.Length &&
                        line[i + 1] == '"') // 큰따옴표가 두 번 연속된 경우
                    {
                        value.Append('"'); // 실제 큰따옴표 한 개 추가
                        i++; // 다음 큰따옴표 건너뛰기
                    }
                    else
                    {
                        insideQuotes = !insideQuotes; // 따옴표 내부 상태 전환
                    }
                }
                else if (c == ',' && !insideQuotes) // 따옴표 바깥의 쉼표인 경우
                {
                    result.Add(value.ToString()); // 현재 열 등록
                    value.Clear(); // 다음 열을 위해 문자열 초기화
                }
                else
                {
                    value.Append(c); // 현재 문자 추가
                }
            }

            result.Add(value.ToString()); // 마지막 열 등록

            return result; // 분리된 열 목록 반환
        }
    }
}
