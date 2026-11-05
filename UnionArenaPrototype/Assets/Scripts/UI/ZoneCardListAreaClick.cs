using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Battle;
using Manager;

namespace UI
{
    // 플레이어의 Trash 및 Remove 영역 우클릭 처리
    public class ZoneCardListAreaClick : MonoBehaviour, IPointerClickHandler
    {
        [Header("카드 목록 패널")]
        [SerializeField] private GameObject _zoneCardListPanel; // 카드 목록 패널
        [SerializeField] private ZoneCardListView _zoneCardListView; // 카드 목록 UI

        [Header("영역 설정")]
        [SerializeField] private bool _isOpponentZone; // 상대 플레이어 영역 여부
        [SerializeField] private bool _isRemoveZone; // Remove 영역 여부

        // 마우스 우클릭 입력을 확인하고 해당 영역의 카드 목록 표시
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Right)
            {
                return;
            }

            if (BattleManager.Instance == null)
            {
                Debug.LogError("ZoneCardListAreaClick: BattleManager를 찾을 수 없습니다.");
                return;
            }

            if (_zoneCardListPanel == null || _zoneCardListView == null)
            {
                Debug.LogError("ZoneCardListAreaClick: 카드 목록 패널 또는 목록 UI가 연결되지 않았습니다.");
                return;
            }

            PlayerGameData playerData = _isOpponentZone
                ? BattleManager.Instance.OpponentPlayerData
                : BattleManager.Instance.LocalPlayerData; // 목록을 확인할 플레이어 데이터

            if (playerData == null)
            {
                Debug.LogError("ZoneCardListAreaClick: 플레이어 데이터를 찾을 수 없습니다.");
                return;
            }

            List<CardRuntimeData> cards = _isRemoveZone
                ? playerData.remove
                : playerData.trash; // 우클릭한 영역의 카드 목록

            if (cards == null)
            {
                Debug.LogError("ZoneCardListAreaClick: 카드 목록 데이터가 null입니다.");
                return;
            }

            _zoneCardListView.SetCards(cards); // 기존 카드 목록 UI에 데이터 전달
            _zoneCardListPanel.SetActive(true); // 카드 목록 패널 활성화

            string playerName = _isOpponentZone ? "상대" : "내";
            string zoneName = _isRemoveZone ? "Remove" : "Trash";

            Debug.Log($"ZoneCardListAreaClick: {playerName} 플레이어의 {zoneName} 목록을 열었습니다. 카드 수 = {cards.Count}");
        }

        // 카드 목록 패널 닫기
        public void ClosePanel()
        {
            if (_zoneCardListPanel == null)
            {
                Debug.LogError("ZoneCardListAreaClick: 닫을 패널이 연결되지 않았습니다.");
                return;
            }

            _zoneCardListPanel.SetActive(false); // 카드 목록 패널 비활성화
        }
    }
}
