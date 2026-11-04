using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Battle;
using Manager;

namespace UI
{
    // 손패 데이터를 카드 UI에 표시하고 드래그를 관리하는 클래스
    public class HandView : MonoBehaviour
    {
        [Header("손패 카드 UI")]
        [SerializeField] private List<CardView> cardViews = new List<CardView>(); // 카드 UI 목록

        [Header("드래그 UI")]
        [SerializeField] private RectTransform dragLayer; // 드래그 UI 표시 영역
        [SerializeField] private CardView draggingCard; // 마우스를 따라다니는 카드 UI
        [SerializeField] private Canvas rootCanvas; // 화면 좌표 변환에 사용할 Canvas

        private CardView _originalCardView; // 드래그를 시작한 원래 손패 카드
        private CanvasGroup _originalCardCanvasGroup; // 원본 카드의 표시 및 입력 제어
        private CardRuntimeData _draggingRuntimeCard; // 실제로 드래그 중인 카드 데이터
        private int _originalCardIndex = -1; // 드래그를 시작한 손패 순서
        private bool _isDragging = false; // 현재 드래그 진행 여부
        private bool _wasDroppedOnField = false; // 필드 슬롯에 드롭 성공했는지 여부

        private float _originalCanvasAlpha; // 드래그 전 원본 카드 투명도
        private bool _originalBlocksRaycasts; // 드래그 전 원본 카드 입력 차단 상태
        private bool _originalInteractable; // 드래그 전 원본 카드 상호작용 상태
        private bool _draggingRaycastTarget; // 드래그 전 복제 카드 입력 차단 상태

        private static HandView _activeDraggingHandView; // 현재 드래그 중인 손패 UI

        // 손패 데이터를 카드 UI에 반영
        public void SetCards(IReadOnlyList<CardRuntimeData> handCards)
        {
            if (handCards == null)
            {
                Debug.LogError("HandView: 손패 데이터가 없습니다.");
                return;
            }

            if (cardViews == null || cardViews.Count == 0)
            {
                Debug.LogError("HandView: 카드 UI 목록이 연결되지 않았습니다.");
                return;
            }

            if (handCards.Count > cardViews.Count)
            {
                Debug.LogError(
                    $"HandView: 카드 UI가 부족합니다. " +
                    $"손패 = {handCards.Count}, UI = {cardViews.Count}");

                return;
            }

            for (int i = 0; i < cardViews.Count; i++)
            {
                CardView cardView = cardViews[i]; // 현재 카드 UI

                if (cardView == null)
                {
                    Debug.LogError($"HandView: {i}번째 카드 UI가 연결되지 않았습니다.");
                    continue;
                }

                bool hasCard = i < handCards.Count; // 해당 순번에 카드가 있는지 확인

                cardView.gameObject.SetActive(hasCard); // 카드 데이터 유무에 따라 활성화

                cardView.SetDragEvents(
                    HandleBeginDrag,
                    HandleDrag,
                    HandleEndDrag); // 손패 카드 드래그 이벤트 연결

                if (!hasCard)
                {
                    continue;
                }

                CardRuntimeData card = handCards[i]; // 현재 순번의 카드 데이터

                if (card == null || card.cardData == null)
                {
                    Debug.LogError($"HandView: {i}번째 카드 데이터가 올바르지 않습니다.");
                    continue;
                }

                cardView.SetCard(card.cardData.CardID); // 카드 이미지 표시
            }
        }

        // 드래그 시작
        private void HandleBeginDrag(CardView cardView, PointerEventData eventData)
        {
            if (_isDragging || cardView == null)
            {
                return;
            }

            if (dragLayer == null || draggingCard == null || rootCanvas == null)
            {
                Debug.LogError("HandView: DragLayer, DraggingCard, Canvas 연결을 확인하세요.");
                return;
            }

            string cardID = cardView.GetCardID(); // 드래그할 카드 ID 가져오기

            if (string.IsNullOrEmpty(cardID))
            {
                Debug.LogError("HandView: 드래그할 카드의 ID가 없습니다.");
                return;
            }

            _originalCardView = cardView; // 드래그를 시작한 카드 저장
            _originalCardIndex = cardViews.IndexOf(cardView); // 원래 손패 UI 순서 저장

            if (_originalCardIndex < 0)
            {
                _originalCardView = null;
                return;
            }

            // 실제 플레이어 손패 데이터 확인
            if (BattleManager.Instance == null ||
                BattleManager.Instance.LocalPlayerData == null)
            {
                Debug.LogError("HandView: 플레이어 데이터를 찾을 수 없습니다.");

                ResetDragState();
                return;
            }

            PlayerGameData playerData = BattleManager.Instance.LocalPlayerData; // 현재 플레이어 데이터

            if (playerData.hand == null ||
                _originalCardIndex >= playerData.hand.Count)
            {
                Debug.LogError("HandView: 드래그할 카드의 손패 데이터가 없습니다.");

                ResetDragState();
                return;
            }

            _draggingRuntimeCard = playerData.hand[_originalCardIndex]; // 실제 드래그 카드 저장

            if (_draggingRuntimeCard == null ||
                _draggingRuntimeCard.cardData == null ||
                _draggingRuntimeCard.currentZone != Enums.BattleEnums.CardZone.Hand)
            {
                Debug.LogError("HandView: 드래그할 카드 데이터가 올바르지 않습니다.");

                ResetDragState();
                return;
            }

            // UI 카드와 실제 데이터 카드가 일치하는지 확인
            if (_draggingRuntimeCard.cardData.CardID != cardID)
            {
                Debug.LogError("HandView: UI 카드와 실제 손패 카드 데이터가 일치하지 않습니다.");

                ResetDragState();
                return;
            }

            _originalCardCanvasGroup =
                _originalCardView.GetComponent<CanvasGroup>(); // 원본 카드 CanvasGroup 확인

            if (_originalCardCanvasGroup == null)
            {
                _originalCardCanvasGroup =
                    _originalCardView.gameObject.AddComponent<CanvasGroup>(); // 없으면 추가
            }

            _originalCanvasAlpha = _originalCardCanvasGroup.alpha; // 기존 투명도 저장
            _originalBlocksRaycasts = _originalCardCanvasGroup.blocksRaycasts; // 기존 입력 차단 상태 저장
            _originalInteractable = _originalCardCanvasGroup.interactable; // 기존 상호작용 상태 저장

            draggingCard.SetCard(cardID); // 드래그 카드 이미지 설정
            draggingCard.gameObject.SetActive(true); // 드래그 카드 표시

            Image draggingImage = draggingCard.GetCardImage(); // 드래그 카드 이미지 가져오기

            if (draggingImage != null)
            {
                _draggingRaycastTarget = draggingImage.raycastTarget; // 기존 Raycast 상태 저장
                draggingImage.raycastTarget = false; // 드래그 카드가 입력을 가로채지 않도록 설정
            }

            _originalCardCanvasGroup.alpha = 0f; // 원본 카드만 투명하게 표시
            _originalCardCanvasGroup.blocksRaycasts = false; // 원본 카드 입력 차단 해제
            _originalCardCanvasGroup.interactable = false; // 원본 카드 상호작용 비활성화

            _isDragging = true; // 드래그 상태 시작
            _wasDroppedOnField = false; // 필드 드롭 상태 초기화

            _activeDraggingHandView = this; // 현재 드래그 중인 손패 UI 등록

            UpdateDraggingCardPosition(eventData); // 드래그 시작 위치 반영
        }

        // 드래그 중 마우스 위치 갱신
        private void HandleDrag(CardView cardView, PointerEventData eventData)
        {
            if (!_isDragging)
            {
                return;
            }

            UpdateDraggingCardPosition(eventData); // 드래그 카드가 마우스를 따라가도록 이동
        }

        // 필드 슬롯에 카드를 드롭할 때 호출
        public static bool TryDropOnFieldSlot(FieldSlotView fieldSlot)
        {
            // 현재 드래그 중인 손패 UI 확인
            HandView handView = _activeDraggingHandView;

            if (handView == null ||
                !handView._isDragging ||
                fieldSlot == null)
            {
                return false;
            }

            // 실제 플레이어 데이터 확인
            if (BattleManager.Instance == null ||
                BattleManager.Instance.LocalPlayerData == null)
            {
                Debug.LogError("HandView: 필드 드롭에 필요한 플레이어 데이터가 없습니다.");
                return false;
            }

            // 카드 영역 매니저 확인
            if (CardZoneManager.Instance == null)
            {
                Debug.LogError("HandView: CardZoneManager를 찾을 수 없습니다.");
                return false;
            }

            PlayerGameData playerData =
                BattleManager.Instance.LocalPlayerData; // 현재 플레이어 데이터

            CardRuntimeData card =
                handView._draggingRuntimeCard; // 드래그 중인 실제 카드

            if (card == null ||
                card.currentZone != Enums.BattleEnums.CardZone.Hand)
            {
                Debug.LogWarning("HandView: 드래그 중인 카드가 손패에 없습니다.");
                return false;
            }

            // 필드 슬롯의 위치와 번호를 가져와 카드 이동
            bool moveSuccess = CardZoneManager.Instance.MoveCard(
                playerData,
                card,
                Enums.BattleEnums.CardZone.Field,
                fieldSlot.GetFieldPosition(),
                fieldSlot.GetSlotIndex());

            if (!moveSuccess)
            {
                Debug.LogWarning("HandView: 필드 슬롯으로 카드 이동에 실패했습니다.");
                return false;
            }

            handView._wasDroppedOnField = true; // 필드 드롭 성공 기록

            Debug.Log(
                $"HandView: 카드 {card.cardData.CardID} 필드 배치 성공. " +
                $"위치 = {fieldSlot.GetFieldPosition()}, " +
                $"슬롯 = {fieldSlot.GetSlotIndex()}");

            return true;
        }

        // 드래그 종료
        private void HandleEndDrag(CardView cardView, PointerEventData eventData)
        {
            if (!_isDragging)
            {
                return;
            }

            int fromIndex = _originalCardIndex; // 이동 전 카드 순서

            // 필드 드롭에 성공하지 않은 경우에만 손패 삽입 위치 계산
            int toIndex = -1;

            if (!_wasDroppedOnField)
            {
                toIndex = GetDropIndex(eventData);
            }

            RestoreOriginalCard(); // 원본 카드 상태 복구
            HideDraggingCard(); // 드래그 카드 숨김

            bool wasDroppedOnField = _wasDroppedOnField; // 필드 드롭 결과 저장

            ResetDragState(); // 드래그 상태 초기화

            // 필드 배치에 성공했다면 손패 재정렬을 실행하지 않음
            if (wasDroppedOnField)
            {
                return;
            }

            // 이동할 위치가 유효하면 실제 손패 데이터 순서 변경
            if (BattleManager.Instance != null &&
                BattleManager.Instance.LocalPlayerData != null &&
                CardZoneManager.Instance != null)
            {
                CardZoneManager.Instance.ReorderHand(
                    BattleManager.Instance.LocalPlayerData,
                    fromIndex,
                    toIndex); // 실제 손패 목록 재정렬
            }
        }

        // 마우스 위치에 해당하는 손패 삽입 순서 계산
        private int GetDropIndex(PointerEventData eventData)
        {
            int insertIndex = 0; // 이동할 카드가 들어갈 위치

            Camera uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : rootCanvas.worldCamera; // Canvas 렌더 모드에 맞는 카메라

            // 드래그 중인 원본 카드를 제외하고 다른 카드 위치 확인
            for (int i = 0; i < cardViews.Count; i++)
            {
                CardView cardView = cardViews[i]; // 현재 비교할 카드 UI

                if (cardView == null ||
                    cardView == _originalCardView ||
                    !cardView.gameObject.activeInHierarchy)
                {
                    continue;
                }

                RectTransform cardRect = cardView.transform as RectTransform; // 카드 위치 정보

                if (cardRect == null)
                {
                    continue;
                }

                float cardCenterX = RectTransformUtility.WorldToScreenPoint(
                    uiCamera,
                    cardRect.TransformPoint(cardRect.rect.center)).x; // 카드 중앙의 화면 X 좌표

                if (eventData.position.x < cardCenterX)
                {
                    return insertIndex; // 현재 카드 앞에 삽입
                }

                insertIndex++; // 다음 카드 위치 확인
            }

            return insertIndex; // 모든 카드 뒤에 삽입
        }

        // 원본 카드 상태 복구
        private void RestoreOriginalCard()
        {
            if (_originalCardCanvasGroup == null)
            {
                return;
            }

            _originalCardCanvasGroup.alpha = _originalCanvasAlpha; // 기존 투명도 복구
            _originalCardCanvasGroup.blocksRaycasts = _originalBlocksRaycasts; // 기존 입력 차단 상태 복구
            _originalCardCanvasGroup.interactable = _originalInteractable; // 기존 상호작용 상태 복구
        }

        // 드래그 카드 숨김 및 입력 설정 복구
        private void HideDraggingCard()
        {
            if (draggingCard == null)
            {
                return;
            }

            Image draggingImage = draggingCard.GetCardImage(); // 드래그 카드 이미지 가져오기

            if (draggingImage != null)
            {
                draggingImage.raycastTarget = _draggingRaycastTarget; // 기존 Raycast 상태 복구
            }

            draggingCard.gameObject.SetActive(false); // 드래그 카드 숨김
        }

        // 마우스 위치를 DragLayer의 로컬 좌표로 변환
        private void UpdateDraggingCardPosition(PointerEventData eventData)
        {
            if (dragLayer == null || draggingCard == null || rootCanvas == null)
            {
                return;
            }

            Camera uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : rootCanvas.worldCamera; // Canvas 렌더 모드에 맞는 UI 카메라 선택

            if (rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay && uiCamera == null)
            {
                Debug.LogError("HandView: Canvas의 Render Camera가 연결되지 않았습니다.");
                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragLayer,
                eventData.position,
                uiCamera,
                out Vector2 localPoint))
            {
                RectTransform draggingRect =
                    draggingCard.transform as RectTransform; // 드래그 카드 RectTransform

                if (draggingRect == null)
                {
                    Debug.LogError("HandView: DraggingCard의 RectTransform을 찾을 수 없습니다.");
                    return;
                }

                draggingRect.anchoredPosition = localPoint; // 마우스 위치에 드래그 카드 배치
            }
        }

        // 드래그 상태 초기화
        private void ResetDragState()
        {
            _isDragging = false; // 드래그 상태 초기화
            _wasDroppedOnField = false; // 필드 드롭 상태 초기화

            _originalCardView = null; // 원본 카드 참조 해제
            _originalCardCanvasGroup = null; // CanvasGroup 참조 해제
            _draggingRuntimeCard = null; // 실제 카드 데이터 참조 해제
            _originalCardIndex = -1; // 기존 순서 초기화

            if (_activeDraggingHandView == this)
            {
                _activeDraggingHandView = null; // 현재 드래그 UI 등록 해제
            }
        }

        // 모든 카드 UI 비활성화
        public void ClearCards()
        {
            if (cardViews == null)
            {
                return;
            }

            foreach (CardView cardView in cardViews)
            {
                if (cardView != null)
                {
                    cardView.gameObject.SetActive(false);
                }
            }
        }

        // 오브젝트가 비활성화될 때 드래그 상태 정리
        private void OnDisable()
        {
            RestoreOriginalCard(); // 원본 카드 상태 복구
            HideDraggingCard(); // 드래그 카드 숨김

            ResetDragState(); // 드래그 상태 초기화
        }
    }
}