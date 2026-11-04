using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class CardCsvToJsonConverter : EditorWindow
    {
        [Serializable]
        public class CardList
        {
            public List<CardData> cards = new List<CardData>();
        }

        [Serializable]
        public class CardData
        {
            public string cardID;
            public string cardName;

            public int cardColor;
            public int cardType;
            public int bp;

            public int requiredEnColor;
            public int requiredEnCost;
            public int requiredApCost;

            public int cardTrigger;
            public string effectText;
            public string[] cardFeatures;
            public KeywordData[] cardKeywords;

            public int generatedEnColor;
            public int generatedEnCount;
            public bool generatedEnCanIncrease;
        }

        [Serializable]
        public class KeywordData
        {
            public int cardKeywordType;
            public int value;
        }

        [Serializable]
        public class StringArrayWrapper
        {
            public string[] items;
        }

        [Serializable]
        public class KeywordArrayWrapper
        {
            public KeywordData[] items;
        }

        [MenuItem("Tools/Card CSV To JSON")]
        public static void OpenWindow()
        {
            GetWindow<CardCsvToJsonConverter>("카드 CSV 변환");
        }

        private void OnGUI()
        {
            GUILayout.Label("카드 CSV → JSON 변환", EditorStyles.boldLabel);

            if (GUILayout.Button("CSV 선택 및 JSON 생성"))
            {
                ConvertCsv();
            }
        }

        private void ConvertCsv()
        {
            string csvPath = EditorUtility.OpenFilePanel(
                "UnionArenaCards.csv 선택",
                "",
                "csv"
            );

            if (string.IsNullOrEmpty(csvPath))
            {
                return;
            }

            try
            {
                string[] lines = File.ReadAllLines(csvPath, Encoding.UTF8);
                CardList cardList = new CardList();

                // 첫 줄은 헤더이므로 제외
                for (int i = 1; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i]))
                    {
                        continue;
                    }

                    List<string> columns = ParseCsvLine(lines[i]);

                    if (columns.Count != 15)
                    {
                        Debug.LogError(
                            $"CSV {i + 1}행의 열 개수가 15개가 아닙니다: {columns.Count}"
                        );
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(columns[0]) ||
                        string.IsNullOrWhiteSpace(columns[1]))
                    {
                        Debug.LogError(
                            $"CSV {i + 1}행의 카드 ID 또는 이름이 비어 있습니다."
                        );
                        return;
                    }

                    CardData card = new CardData
                    {
                        // 1~9열
                        cardID = columns[0].Trim(),
                        cardName = columns[1].Trim(),
                        cardColor = ParseIntCell(columns[2]),
                        cardType = ParseIntCell(columns[3]),
                        bp = ParseIntCell(columns[4]),
                        requiredEnColor = ParseIntCell(columns[5]),
                        requiredEnCost = ParseIntCell(columns[6]),
                        requiredApCost = ParseIntCell(columns[7]),
                        cardTrigger = ParseIntCell(columns[8]),

                        // 10~12열
                        effectText = columns[9].Trim(),
                        cardFeatures = ParseStringArray(columns[10]),
                        cardKeywords = ParseKeywordArray(columns[11]),

                        // 13~15열
                        generatedEnColor = ParseIntCell(columns[12]),
                        generatedEnCount = ParseIntCell(columns[13]),
                        generatedEnCanIncrease = ParseBoolCell(columns[14])
                    };

                    cardList.cards.Add(card);
                }

                string json = JsonUtility.ToJson(cardList, true);

                string outputPath = EditorUtility.SaveFilePanel(
                    "변환된 JSON 저장",
                    Application.dataPath + "/Resources/CardData",
                    "Cards_Converted.json",
                    "json"
                );

                if (string.IsNullOrEmpty(outputPath))
                {
                    return;
                }

                File.WriteAllText(
                    outputPath,
                    json,
                    new UTF8Encoding(false)
                );

                AssetDatabase.Refresh();

                Debug.Log(
                    $"CSV 변환 완료: {cardList.cards.Count}장\n{outputPath}"
                );

                EditorUtility.DisplayDialog(
                    "변환 완료",
                    $"카드 {cardList.cards.Count}장을 JSON으로 변환했습니다.",
                    "확인"
                );
            }
            catch (Exception e)
            {
                Debug.LogError("CSV 변환 실패: " + e.Message);

                EditorUtility.DisplayDialog(
                    "변환 실패",
                    e.Message,
                    "확인"
                );
            }
        }

        private int ParseIntCell(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return 0;
            }

            return int.Parse(value.Trim(), CultureInfo.InvariantCulture);
        }

        private bool ParseBoolCell(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return bool.Parse(value.Trim());
        }

        private string[] ParseStringArray(string jsonArray)
        {
            if (string.IsNullOrWhiteSpace(jsonArray))
            {
                return new string[0];
            }

            StringArrayWrapper wrapper =
                JsonUtility.FromJson<StringArrayWrapper>(
                    "{\"items\":" + jsonArray + "}"
                );

            return wrapper != null && wrapper.items != null
                ? wrapper.items
                : new string[0];
        }

        private KeywordData[] ParseKeywordArray(string jsonArray)
        {
            if (string.IsNullOrWhiteSpace(jsonArray))
            {
                return new KeywordData[0];
            }

            KeywordArrayWrapper wrapper =
                JsonUtility.FromJson<KeywordArrayWrapper>(
                    "{\"items\":" + jsonArray + "}"
                );

            return wrapper != null && wrapper.items != null
                ? wrapper.items
                : new KeywordData[0];
        }

        private List<string> ParseCsvLine(string line)
        {
            List<string> result = new List<string>();
            StringBuilder value = new StringBuilder();
            bool insideQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char current = line[i];

                if (current == '"')
                {
                    if (insideQuotes &&
                        i + 1 < line.Length &&
                        line[i + 1] == '"')
                    {
                        // CSV에서 ""는 실제 큰따옴표 하나를 뜻함
                        value.Append('"');
                        i++;
                    }
                    else
                    {
                        insideQuotes = !insideQuotes;
                    }
                }
                else if (current == ',' && !insideQuotes)
                {
                    result.Add(value.ToString());
                    value.Clear();
                }
                else
                {
                    value.Append(current);
                }
            }

            result.Add(value.ToString());
            return result;
        }
    }
}