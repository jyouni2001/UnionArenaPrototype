using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Battle;
using Manager;

namespace UI
{
    // 손패와 필드 카드의 드래그를 관리하는 클래스
    public class HandView : MonoBehaviour
    {
        [Header("손패 카드 UI")]
        [SerializeField] private List<CardView> cardViews = new List<CardView>(); // 손패 카드 UI 목록

        [Header("드래그 UI")]
        [SerializeField] private RectTransform dragLayer; // 드래그 UI 표시 영역
        [SerializeField] private CardView draggingCard; // 마우스를 따라다니는 카드 UI
        [SerializeField] private Canvas rootCanvas; // 화면 좌표 변환에 사용할 Canvas

        private CardView _originalCardView; // 드래그를 시작한 원본 카드
        private CanvasGroup _originalCardCanvasGroup; // 원본 카드 CanvasGroup
        private CardRuntimeData _draggingRuntimeCard; // 실제 드래그 중인 카드 데이터

        private FieldSlotView _originalFieldSlot; // 드래그를 시작한 필드 슬롯
        private int _originalCardIndex = -1; // 손패에서 드래그를 시작한 순서

        private bool _isDragging = false; // 드래그 진행 여부
        private bool _isDraggingFieldCard = false; // 필드 카드 드래그 여부
        private bool _wasDroppedOnField = false; // 필드 드롭 성공 여부

        private float _originalCanvasAlpha; // 원본 카드 기존 투명도
        private bool _originalBlocksRaycasts; // 원본 카드 기존 Raycast 상태
        private bool _originalInteractable; // 원본 카드 기존 상호작용 상태
        private bool _draggingRaycastTarget; // 드래그 카드 기존 Raycast 상태

        private static HandView _instance; // 현재 HandView 인스턴스
        private static HandView _activeDraggingHandView; // 현재 드래그 중인 HandView

        private void Awake()
        {
            _instance = this; // HandView 인스턴스 등록
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null; // 인스턴스 해제
            }
        }

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
                    $"HandView: 카드 UI가 부족합니다. 손패 = {handCards.Count}, UI = {cardViews.Count}");

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

                bool hasCard = i < handCards.Count; // 해당 순번의 카드 존재 여부

                cardView.gameObject.SetActive(hasCard); // 카드 데이터에 따라 활성화

                cardView.SetDragEvents(HandleBeginDrag, HandleDrag, HandleEndDrag); // 손패 드래그 이벤트 연결

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

        // 필드 카드 UI에 드래그 이벤트 연결
        public static void RegisterFieldCardDragEvents(
            CardView cardView,
            FieldSlotView fieldSlot)
        {
            if (cardView == null || fieldSlot == null)
            {
                return;
            }

            if (_instance == null)
            {
                Debug.LogError("HandView: 필드 카드 드래그를 등록할 HandView가 없습니다.");
                return;
            }

            HandView handView = _instance; // 현재 HandView

            cardView.SetDragEvents(
                (view, eventData) => handView.HandleFieldBeginDrag(view, eventData, fieldSlot),
                handView.HandleDrag,
                handView.HandleEndDrag); // 필드 카드 드래그 이벤트 연결
        }

        // 손패 카드 드래그 시작
        private void HandleBeginDrag(CardView cardView, PointerEventData eventData)
        {
            if (_isDragging || cardView == null)
            {
                return;
            }

            int cardIndex = cardViews.IndexOf(cardView); // 손패 UI 순서 확인

            if (cardIndex < 0)
            {
                return;
            }

            if (BattleManager.Instance == null ||
                BattleManager.Instance.LocalPlayerData == null)
            {
                Debug.LogError("HandView: 플레이어 데이터를 찾을 수 없습니다.");
                return;
            }

            PlayerGameData playerData = BattleManager.Instance.LocalPlayerData; // 현재 플레이어 데이터

            if (playerData.hand == null ||
                cardIndex >= playerData.hand.Count)
            {
                Debug.LogError("HandView: 드래그할 손패 데이터가 없습니다.");
                return;
            }

            CardRuntimeData card = playerData.hand[cardIndex]; // 실제 손패 카드

            if (card == null ||
                card.cardData == null ||
                card.currentZone != Enums.BattleEnums.CardZone.Hand)
            {
                Debug.LogError("HandView: 손패 카드 데이터가 올바르지 않습니다.");
                return;
            }

            if (card.cardData.CardID != cardView.GetCardID())
            {
                Debug.LogError("HandView: UI 카드와 실제 손패 데이터가 일치하지 않습니다.");
                return;
            }

            StartDraggingCard(cardView, eventData, card, false, cardIndex, null);
        }

        // 필드 카드 드래그 시작
        private void HandleFieldBeginDrag(
            CardView cardView,
            PointerEventData eventData,
            FieldSlotView fieldSlot)
        {
            if (_isDragging ||
                cardView == null ||
                fieldSlot == null)
            {
                return;
            }

            if (BattleManager.Instance == null ||
                BattleManager.Instance.LocalPlayerData == null)
            {
                Debug.LogError("HandView: 플레이어 데이터를 찾을 수 없습니다.");
                return;
            }

            PlayerGameData playerData = BattleManager.Instance.LocalPlayerData; // 현재 플레이어 데이터

            if (playerData.field == null)
            {
                Debug.LogError("HandView: 필드 데이터가 없습니다.");
                return;
            }

            CardRuntimeData fieldCard = null; // 드래그할 필드 카드

            foreach (CardRuntimeData card in playerData.field)
            {
                if (card == null || card.cardData == null)
                {
                    continue;
                }

                if (card.fieldPosition == fieldSlot.GetFieldPosition() &&
                    card.fieldSlotIndex == fieldSlot.GetSlotIndex())
                {
                    fieldCard = card; // 해당 슬롯의 카드 찾기
                    break;
                }
            }

            if (fieldCard == null)
            {
                Debug.LogWarning("HandView: 해당 필드 슬롯에 카드 데이터가 없습니다.");
                return;
            }

            if (fieldCard.currentZone != Enums.BattleEnums.CardZone.Field)
            {
                Debug.LogWarning("HandView: 드래그할 카드가 필드에 있지 않습니다.");
                return;
            }

            if (fieldCard.cardData.CardID != cardView.GetCardID())
            {
                Debug.LogError("HandView: 필드 UI 카드와 실제 카드 데이터가 일치하지 않습니다.");
                return;
            }

            StartDraggingCard(cardView, eventData, fieldCard, true, -1, fieldSlot);
        }

        // 손패와 필드 카드의 공통 드래그 시작 처리
        private void StartDraggingCard(
            CardView cardView,
            PointerEventData eventData,
            CardRuntimeData runtimeCard,
            bool isFieldCard,
            int cardIndex,
            FieldSlotView fieldSlot)
        {
            if (dragLayer == null || draggingCard == null || rootCanvas == null)
            {
                Debug.LogError("HandView: DragLayer, DraggingCard, Canvas 연결을 확인하세요.");
                return;
            }

            if (runtimeCard == null ||
                runtimeCard.cardData == null)
            {
                Debug.LogError("HandView: 드래그할 카드 데이터가 올바르지 않습니다.");
                return;
            }

            string cardID = runtimeCard.cardData.CardID; // 드래그할 카드 ID

            _originalCardView = cardView; // 원본 카드 저장
            _originalCardIndex = cardIndex; // 손패 순서 저장
            _originalFieldSlot = fieldSlot; // 원본 필드 슬롯 저장
            _draggingRuntimeCard = runtimeCard; // 실제 카드 데이터 저장
            _isDraggingFieldCard = isFieldCard; // 필드 카드 여부 저장

            _originalCardCanvasGroup = cardView.GetComponent<CanvasGroup>(); // 원본 CanvasGroup 확인

            if (_originalCardCanvasGroup == null)
            {
                _originalCardCanvasGroup = cardView.gameObject.AddComponent<CanvasGroup>(); // 없으면 추가
            }

            _originalCanvasAlpha = _originalCardCanvasGroup.alpha; // 기존 투명도 저장
            _originalBlocksRaycasts = _originalCardCanvasGroup.blocksRaycasts; // 기존 Raycast 상태 저장
            _originalInteractable = _originalCardCanvasGroup.interactable; // 기존 상호작용 상태 저장

            draggingCard.SetCard(cardID); // 드래그 카드 이미지 설정
            draggingCard.gameObject.SetActive(true); // 드래그 카드 표시

            Image draggingImage = draggingCard.GetCardImage(); // 드래그 카드 이미지

            if (draggingImage != null)
            {
                _draggingRaycastTarget = draggingImage.raycastTarget; // 기존 Raycast 상태 저장
                draggingImage.raycastTarget = false; // 드래그 카드 입력 차단
            }

            _originalCardCanvasGroup.alpha = 0f; // 원본 카드 숨김
            _originalCardCanvasGroup.blocksRaycasts = false; // 원본 카드 Raycast 차단
            _originalCardCanvasGroup.interactable = false; // 원본 카드 상호작용 비활성화

            _isDragging = true; // 드래그 시작
            _wasDroppedOnField = false; // 필드 드롭 상태 초기화

            _activeDraggingHandView = this; // 현재 드래그 UI 등록

            UpdateDraggingCardPosition(eventData); // 드래그 시작 위치 반영
        }

        // 드래그 중 마우스 위치 갱신
        private void HandleDrag(CardView cardView, PointerEventData eventData)
        {
            if (!_isDragging)
            {
                return;
            }

            UpdateDraggingCardPosition(eventData); // 드래그 중 카드 위치 갱신
        }

        // 필드 슬롯에 카드 드롭
        public static bool TryDropOnFieldSlot(FieldSlotView fieldSlot)
        {
            HandView handView = _activeDraggingHandView; // 현재 드래그 중인 HandView

            if (handView == null ||
                !handView._isDragging ||
                fieldSlot == null)
            {
                return false;
            }

            if (BattleManager.Instance == null ||
                BattleManager.Instance.LocalPlayerData == null)
            {
                Debug.LogError("HandView: 필드 드롭에 필요한 플레이어 데이터가 없습니다.");
                return false;
            }

            if (CardZoneManager.Instance == null)
            {
                Debug.LogError("HandView: CardZoneManager를 찾을 수 없습니다.");
                return false;
            }

            PlayerGameData playerData = BattleManager.Instance.LocalPlayerData;
            CardRuntimeData card = handView._draggingRuntimeCard;

            if (card == null || card.cardData == null)
            {
                Debug.LogWarning("HandView: 드래그 중인 카드 데이터가 없습니다.");
                return false;
            }

            bool moveSuccess;

            if (handView._isDraggingFieldCard)
            {
                // 필드 카드끼리 이동하거나 같은 라인에서 스왑
                moveSuccess = CardZoneManager.Instance.MoveOrSwapFieldCard(
                    playerData,
                    card,
                    fieldSlot.GetFieldPosition(),
                    fieldSlot.GetSlotIndex());
            }
            else
            {
                // 손패 카드는 현재 턴 플레이어만 배치 가능
                if (BattleManager.Instance.CurrentTurnPlayer != playerData)
                {
                    Debug.LogWarning("HandView: 현재 내 턴이 아닙니다.");
                    return false;
                }

                int apCost = card.cardData.RequiredApCost;

                if (apCost < 0)
                {
                    Debug.LogWarning("HandView: 카드의 AP 비용이 올바르지 않습니다.");
                    return false;
                }

                if (playerData.activeAP < apCost)
                {
                    Debug.LogWarning(
                        $"HandView: AP가 부족합니다. 필요 = {apCost}, 보유 = {playerData.activeAP}");
                    return false;
                }

                moveSuccess = CardZoneManager.Instance.MoveCard(
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

                // 배치 성공 후 AP 비용 지불
                if (apCost > 0 && !playerData.TrySpendAP(apCost))
                {
                    // AP 지불에 실패하면 카드를 손패로 복귀
                    CardZoneManager.Instance.MoveCard(
                        playerData,
                        card,
                        Enums.BattleEnums.CardZone.Hand);

                    Debug.LogWarning(
                        "HandView: AP 지불에 실패해 카드를 손패로 되돌렸습니다.");
                    return false;
                }
            }

            if (!moveSuccess)
            {
                Debug.LogWarning("HandView: 필드 슬롯으로 카드 이동에 실패했습니다.");
                return false;
            }

            handView._wasDroppedOnField = true; // 필드 드롭 성공 기록

            Debug.Log(
                $"HandView: 카드 {card.cardData.CardID} 이동 성공. " +
                $"위치 = {fieldSlot.GetFieldPosition()}, " +
                $"슬롯 = {fieldSlot.GetSlotIndex()}, " +
                $"남은 AP = {playerData.activeAP}");

            return true;
        }

        // 드래그 종료
        private void HandleEndDrag(CardView cardView, PointerEventData eventData)
        {
            if (!_isDragging)
            {
                return;
            }

            bool wasFieldCard = _isDraggingFieldCard; // 필드 카드 드래그 여부
            bool wasDroppedOnField = _wasDroppedOnField; // 필드 드롭 성공 여부

            int fromIndex = _originalCardIndex; // 기존 손패 순서
            int toIndex = -1; // 변경할 손패 순서

            if (!wasFieldCard && !wasDroppedOnField)
            {
                toIndex = GetDropIndex(eventData); // 손패 삽입 위치 계산
            }

            RestoreOriginalCard(); // 원본 카드 상태 복구
            HideDraggingCard(); // 드래그 카드 숨김
            ResetDragState(); // 드래그 상태 초기화

            if (wasDroppedOnField || wasFieldCard)
            {
                return;
            }

            if (BattleManager.Instance != null &&
                BattleManager.Instance.LocalPlayerData != null &&
                CardZoneManager.Instance != null)
            {
                CardZoneManager.Instance.ReorderHand(
                    BattleManager.Instance.LocalPlayerData,
                    fromIndex,
                    toIndex); // 손패 재정렬
            }
        }

        // 마우스 위치에 해당하는 손패 삽입 순서 계산
        private int GetDropIndex(PointerEventData eventData)
        {
            if (eventData == null || rootCanvas == null)
            {
                return -1;
            }

            int insertIndex = 0; // 삽입 위치

            Camera uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : rootCanvas.worldCamera; // UI 카메라

            for (int i = 0; i < cardViews.Count; i++)
            {
                CardView cardView = cardViews[i];

                if (cardView == null ||
                    cardView == _originalCardView ||
                    !cardView.gameObject.activeInHierarchy)
                {
                    continue;
                }

                RectTransform cardRect = cardView.transform as RectTransform;

                if (cardRect == null)
                {
                    continue;
                }

                float cardCenterX = RectTransformUtility.WorldToScreenPoint(
                    uiCamera,
                    cardRect.TransformPoint(cardRect.rect.center)).x;

                if (eventData.position.x < cardCenterX)
                {
                    return insertIndex;
                }

                insertIndex++;
            }

            return insertIndex;
        }

        // 원본 카드 상태 복구
        private void RestoreOriginalCard()
        {
            if (_originalCardCanvasGroup == null)
            {
                return;
            }

            _originalCardCanvasGroup.alpha = _originalCanvasAlpha;
            _originalCardCanvasGroup.blocksRaycasts = _originalBlocksRaycasts;
            _originalCardCanvasGroup.interactable = _originalInteractable;
        }

        // 드래그 카드 숨김
        private void HideDraggingCard()
        {
            if (draggingCard == null)
            {
                return;
            }

            Image draggingImage = draggingCard.GetCardImage();

            if (draggingImage != null)
            {
                draggingImage.raycastTarget = _draggingRaycastTarget;
            }

            draggingCard.gameObject.SetActive(false);
        }

        // 마우스 위치를 DragLayer 로컬 좌표로 변환
        private void UpdateDraggingCardPosition(PointerEventData eventData)
        {
            if (dragLayer == null ||
                draggingCard == null ||
                rootCanvas == null ||
                eventData == null)
            {
                return;
            }

            Camera uiCamera =
                rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                    ? null
                    : rootCanvas.worldCamera;

            if (rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay &&
                uiCamera == null)
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
                    draggingCard.transform as RectTransform;

                if (draggingRect == null)
                {
                    Debug.LogError("HandView: DraggingCard의 RectTransform을 찾을 수 없습니다.");
                    return;
                }

                draggingRect.anchoredPosition = localPoint;
            }
        }

        // 드래그 상태 초기화
        private void ResetDragState()
        {
            _isDragging = false;
            _isDraggingFieldCard = false;
            _wasDroppedOnField = false;

            _originalCardView = null;
            _originalCardCanvasGroup = null;
            _draggingRuntimeCard = null;
            _originalFieldSlot = null;
            _originalCardIndex = -1;

            if (_activeDraggingHandView == this)
            {
                _activeDraggingHandView = null;
            }
        }

        // 모든 손패 카드 UI 비활성화
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

        // 오브젝트 비활성화 시 드래그 상태 정리
        private void OnDisable()
        {
            RestoreOriginalCard();
            HideDraggingCard();
            ResetDragState();
        }
    }
}