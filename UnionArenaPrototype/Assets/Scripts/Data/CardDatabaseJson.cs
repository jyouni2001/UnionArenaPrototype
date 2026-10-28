using System;
using System.Collections.Generic;
using UnityEngine;

namespace Data
{
    // 카드 데이터베이스
    [Serializable]
    public class CardDatabaseJson
    {
        [SerializeField] private List<CardJsonData> cards = new List<CardJsonData>();
        
        public List<CardJsonData> Cards => cards;
    }
}