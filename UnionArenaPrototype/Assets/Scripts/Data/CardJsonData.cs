using System;
using System.Collections.Generic;
using UnityEngine;

namespace Data
{
    [Serializable]
    public class CardJsonData
    {
        [SerializeField] private string cardID; // 카드 고유 ID
        [SerializeField] private string cardName; // 카드 이름

        [SerializeField] private CardEnums.CardColor cardColor; // 카드 색
        [SerializeField] private CardEnums.CardType cardType; // 카드 종류

        [SerializeField] private int bp; // Battle Point

        [SerializeField] private CardEnums.CardColor requiredEnColor; // 필요 에너지 색
        [SerializeField] private int requiredEnCost; // 필요 에너지 수
        [SerializeField] private int requiredApCost; // 필요 Action Point 비용

        [SerializeField] private CardEnums.CardColor generatedEnColor; // 생성 에너지 색
        [SerializeField] private int generatedEnCount; // 생성 에너지 수
        [SerializeField] private bool generatedEnCanIncrease; // 효과로 생성량이 증가할 수 있는 카드인지

        [SerializeField] private CardEnums.CardTrigger cardTrigger; // 카드 트리거
        [SerializeField] private string effectText; // 카드 효과

        [SerializeField] private List<string> cardFeatures = new List<string>(); // 카드 특징 목록
        [SerializeField] private List<CardKeywordJsonData> cardKeywords =
            new List<CardKeywordJsonData>(); // 카드 키워드 목록

        public string CardID => cardID;
        public string CardName => cardName;

        public CardEnums.CardColor CardColor => cardColor;
        public CardEnums.CardType CardType => cardType;

        public int BP => bp;

        public CardEnums.CardColor RequiredEnColor => requiredEnColor;
        public int RequiredEnCost => requiredEnCost;
        public int RequiredApCost => requiredApCost;

        public CardEnums.CardColor GeneratedEnColor => generatedEnColor;
        public int GeneratedEnCount => generatedEnCount;
        public bool GeneratedEnCanIncrease => generatedEnCanIncrease;

        public CardEnums.CardTrigger CardTrigger => cardTrigger;
        public string EffectText => effectText;

        public List<string> CardFeatures => cardFeatures;
        public List<CardKeywordJsonData> CardKeywords => cardKeywords;
    }

    [Serializable]
    public class CardFeatureJsonData
    {
        [SerializeField] private CardEnums.CardFeatureType cardFeatureType; // 카드 특징 종류
        [SerializeField] private string featureName; // 카드 특징 이름

        public CardEnums.CardFeatureType CardFeatureType => cardFeatureType;
        public string FeatureName => featureName;
    }

    [Serializable]
    public class CardKeywordJsonData
    {
        [SerializeField] private CardEnums.CardKeywordType cardKeywordType; // 키워드 종류
        [SerializeField] private int value; // 임팩트/데미지 수치

        public CardEnums.CardKeywordType CardKeywordType => cardKeywordType;
        public int Value => value;
    }
}