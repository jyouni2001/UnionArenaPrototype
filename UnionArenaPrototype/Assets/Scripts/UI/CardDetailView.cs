using System;
using Data;
using UnityEngine;
using Manager;
using UnityEngine.UI;


// Manager를 통해 카드 정보를 받아 화면에 표시하는 역할
namespace UI
{
    public class CardDetailView : MonoBehaviour
    {
        [SerializeField] private Text cardNameText; // 카드 이름 텍스트 저장 함수
        [SerializeField] private Text cardBpText; // 카드 bp 텍스트 저장 함수
        [SerializeField] private Text cardEffectText; // 카드 효과 텍스트 저장 함수
        
        // 카드 상세 정보 UI 설정하는 함수
        public void SetCardDetail(string cardID)
        {
            Debug.Log("카드 상세 정보 함수 실행됨: " + cardID);
            
            if (CardDatabaseManager.Instance == null) // databasemanager 존재 여부 확인
            {
                Debug.LogError("CardDetailView : CardDatabaseManager가 없습니다.");
                return;
            }

            CardJsonData card = CardDatabaseManager.Instance.GetCardByID(cardID); // Json에 ID를 사용해서 조회

            if (card == null) // 해당 카드가 없으면 로그
            {
                Debug.LogError("CardDetailView : 해당 카드를 찾을 수 없습니다." + cardID);
                return;
            }
            
            if (cardNameText == null || cardBpText == null || cardEffectText == null) // 이름 bp 효과 연결 여부 확인
            {
                Debug.LogError("CardDetailView: 카드 정보 텍스트 연결해야합니다.");
                return;
            }

            cardNameText.text = card.CardName; // 연결한 cardNameText에 실제 찾은 카드 이름 넣기

            if (card.CardType == CardEnums.CardType.Character || card.CardType == CardEnums.CardType.Raid)
            {   
                cardBpText.gameObject.SetActive(true);
                cardBpText.text = "BP : " + card.BP;
            }
            else
            {
                cardBpText.gameObject.SetActive(false);
            }
            
            if (string.IsNullOrWhiteSpace(card.EffectText)) // 효과가 없는 카드는 텍스트가 비어있음
            {
                cardEffectText.gameObject.SetActive(false);
            }
            else // 효과 존재시 text에 효과 집어 넣음
            {
                cardEffectText.gameObject.SetActive(true);
                cardEffectText.text = card.EffectText;
            }
        }
    }
}