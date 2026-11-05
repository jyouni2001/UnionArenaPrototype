using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Battle;
using Manager;

namespace UI
{
    // 손패와 필드 카드의 표시 및 드래그를 관리
    public class HandView : MonoBehaviour
    {
        #region UI 연결

        [Header("손패 카드 UI")]
        [SerializeField] private List<CardView> cardViews = new List<CardView>(); // 카드 앞면을 표시하는 UI 목록

        [Header("상대 손패 뒷면 UI")]
        [SerializeField] private List<GameObject> cardBackViews = new List<GameObject>(); // 카드 뒷면을 표시하는 UI 목록

        [Header("드래그 UI")]
        [SerializeField] private RectTransform dragLayer; // 드래그 카드를 표시할 영역
        [SerializeField] private CardView draggingCard; // 마우스를 따라다니는 카드 UI
        [SerializeField] private Canvas rootCanvas; // 화면 좌표 변환에 사용할 Canvas

        #endregion

        #region 드래그 상태

        private CardView _originalCardView; // 드래그를 시작한 원본 카드 UI
        private CanvasGroup _originalCardCanvasGroup; // 원본 카드 표시 및 입력 제어
        private CardRuntimeData _draggingRuntimeCard; // 드래그 중인 카드 데이터
        private FieldSlotView _originalFieldSlot; // 드래그를 시작한 필드 슬롯
        private int _originalCardIndex = -1; // 드래그를 시작한 손패 순서
        private bool _isDragging = false; // 드래그 진행 여부
        private bool _isDraggingFieldCard = false; // 필드 카드 드래그 여부
        private bool _wasDroppedOnField = false; // 필드 슬롯에 드롭했는지 여부
        private float _originalCanvasAlpha; // 원본 카드의 드래그 전 투명도
        private bool _originalBlocksRaycasts; // 원본 카드의 드래그 전 입력 차단 상태
        private bool _originalInteractable; // 원본 카드의 드래그 전 상호작용 상태
        private bool _draggingRaycastTarget; // 드래그 카드의 드래그 전 레이캐스트 상태

        private static HandView _instance; // 필드 카드 드래그에 사용할 HandView
        private static HandView _activeDraggingHandView; // 현재 드래그 중인 HandView

        #endregion

        #region 초기화 및 해제

        // 드래그 UI가 연결된 HandView를 필드 카드 드래그에 등록
        private void Awake()
        {
            if (dragLayer != null && draggingCard != null && rootCanvas != null)
            {
                _instance = this;
            }
        }

        // 등록된 HandView가 제거되면 인스턴스 참조 해제
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        // 오브젝트가 비활성화되면 진행 중인 드래그 정리
        private void OnDisable()
        {
            RestoreOriginalCard();
            HideDraggingCard();
            ResetDragState();
        }

        #endregion

        #region 손패 카드 표시

        // 손패 데이터를 카드 앞면 UI에 반영
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
                Debug.LogError($"HandView: 카드 UI가 부족합니다. 손패 = {handCards.Count}, UI = {cardViews.Count}");
                return;
            }

            for (int i = 0; i < cardViews.Count; i++)
            {
                CardView cardView = cardViews[i];

                if (cardView == null)
                {
                    Debug.LogError($"HandView: {i}번째 카드 UI가 연결되지 않았습니다.");
                    continue;
                }

                bool hasCard = i < handCards.Count;
                cardView.gameObject.SetActive(hasCard);
                cardView.SetDragEvents(HandleBeginDrag, HandleDrag, HandleEndDrag);

                if (!hasCard)
                {
                    continue;
                }

                CardRuntimeData card = handCards[i];

                if (card == null || card.cardData == null)
                {
                    Debug.LogError($"HandView: {i}번째 카드 데이터가 올바르지 않습니다.");
                    continue;
                }

                cardView.SetCard(card.cardData.CardID);
            }
        }

        // 상대 손패 카드 수만큼 카드 뒷면 UI를 표시
        public void SetCardBackCount(int handCount)
        {
            if (cardBackViews == null)
            {
                Debug.LogError("HandView: 카드 뒷면 UI 목록이 없습니다.");
                return;
            }

            int visibleCount = Mathf.Clamp(handCount, 0, cardBackViews.Count);

            if (handCount > cardBackViews.Count)
            {
                Debug.LogWarning($"HandView: 카드 뒷면 UI가 부족합니다. 손패 = {handCount}, UI = {cardBackViews.Count}");
            }

            for (int i = 0; i < cardBackViews.Count; i++)
            {
                if (cardBackViews[i] != null)
                {
                    cardBackViews[i].SetActive(i < visibleCount);
                }
            }
        }

        // 모든 손패 앞면 UI를 비활성화
        public void ClearCards()
        {
            if (cardViews == null)
            {
                return;
            }

            for (int i = 0; i < cardViews.Count; i++)
            {
                if (cardViews[i] != null)
                {
                    cardViews[i].gameObject.SetActive(false);
                }
            }
        }

        #endregion

        #region 드래그 이벤트 연결

        // 필드 카드 UI에 드래그 이벤트를 연결
        public static void RegisterFieldCardDragEvents(CardView cardView, FieldSlotView fieldSlot)
        {
            if (cardView == null || fieldSlot == null)
            {
                return;
            }

            if (_instance == null)
            {
                Debug.LogError("HandView: 드래그 UI가 연결된 HandView를 찾을 수 없습니다.");
                return;
            }

            HandView handView = _instance;

            cardView.SetDragEvents(
                (view, eventData) => handView.HandleFieldBeginDrag(view, eventData, fieldSlot),
                handView.HandleDrag,
                handView.HandleEndDrag);
        }

        #endregion

        #region 손패 카드 드래그 시작

        // 손패 카드 드래그를 시작
        private void HandleBeginDrag(CardView cardView, PointerEventData eventData)
        {
            if (_isDragging || cardView == null)
            {
                return;
            }

            int cardIndex = cardViews.IndexOf(cardView);

            if (cardIndex < 0)
            {
                return;
            }

            if (BattleManager.Instance == null || BattleManager.Instance.LocalPlayerData == null)
            {
                Debug.LogError("HandView: 플레이어 데이터를 찾을 수 없습니다.");
                return;
            }

            PlayerGameData playerData = BattleManager.Instance.LocalPlayerData;

            if (playerData.hand == null || cardIndex >= playerData.hand.Count)
            {
                Debug.LogError("HandView: 드래그할 손패 데이터가 없습니다.");
                return;
            }

            CardRuntimeData card = playerData.hand[cardIndex];

            if (card == null || card.cardData == null || card.currentZone != Enums.BattleEnums.CardZone.Hand)
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

        #endregion

        #region 필드 카드 드래그 시작

        // 필드 카드 드래그를 시작
        private void HandleFieldBeginDrag(CardView cardView, PointerEventData eventData, FieldSlotView fieldSlot)
        {
            if (_isDragging || cardView == null || fieldSlot == null)
            {
                return;
            }

            if (BattleManager.Instance == null || BattleManager.Instance.LocalPlayerData == null)
            {
                Debug.LogError("HandView: 플레이어 데이터를 찾을 수 없습니다.");
                return;
            }

            PlayerGameData playerData = BattleManager.Instance.LocalPlayerData;

            if (playerData.field == null)
            {
                Debug.LogError("HandView: 필드 데이터가 없습니다.");
                return;
            }

            CardRuntimeData fieldCard = null;

            // 슬롯 위치와 일치하는 필드 카드 데이터를 찾음
            foreach (CardRuntimeData card in playerData.field)
            {
                if (card == null || card.cardData == null)
                {
                    continue;
                }

                if (card.fieldPosition == fieldSlot.GetFieldPosition() && card.fieldSlotIndex == fieldSlot.GetSlotIndex())
                {
                    fieldCard = card;
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

        #endregion

        #region 공통 드래그 처리

        // 손패와 필드 카드의 공통 드래그 시작 처리를 수행
        private void StartDraggingCard(CardView cardView, PointerEventData eventData, CardRuntimeData runtimeCard, bool isFieldCard, int cardIndex, FieldSlotView fieldSlot)
        {
            if (dragLayer == null || draggingCard == null || rootCanvas == null)
            {
                Debug.LogError("HandView: DragLayer, DraggingCard, Canvas 연결을 확인하세요.");
                return;
            }

            if (runtimeCard == null || runtimeCard.cardData == null)
            {
                Debug.LogError("HandView: 드래그할 카드 데이터가 올바르지 않습니다.");
                return;
            }

            string cardID = runtimeCard.cardData.CardID;

            _originalCardView = cardView;
            _originalCardIndex = cardIndex;
            _originalFieldSlot = fieldSlot;
            _draggingRuntimeCard = runtimeCard;
            _isDraggingFieldCard = isFieldCard;

            _originalCardCanvasGroup = cardView.GetComponent<CanvasGroup>();

            if (_originalCardCanvasGroup == null)
            {
                _originalCardCanvasGroup = cardView.gameObject.AddComponent<CanvasGroup>();
            }

            _originalCanvasAlpha = _originalCardCanvasGroup.alpha;
            _originalBlocksRaycasts = _originalCardCanvasGroup.blocksRaycasts;
            _originalInteractable = _originalCardCanvasGroup.interactable;

            draggingCard.SetCard(cardID);
            draggingCard.gameObject.SetActive(true);

            Image draggingImage = draggingCard.GetCardImage();

            if (draggingImage != null)
            {
                _draggingRaycastTarget = draggingImage.raycastTarget;
                draggingImage.raycastTarget = false;
            }

            _originalCardCanvasGroup.alpha = 0f;
            _originalCardCanvasGroup.blocksRaycasts = false;
            _originalCardCanvasGroup.interactable = false;

            _isDragging = true;
            _wasDroppedOnField = false;
            _activeDraggingHandView = this;

            UpdateDraggingCardPosition(eventData);
        }

        // 드래그 중 마우스 위치를 갱신
        private void HandleDrag(CardView cardView, PointerEventData eventData)
        {
            if (_isDragging)
            {
                UpdateDraggingCardPosition(eventData);
            }
        }

        #endregion

        #region 필드 슬롯 드롭

        // 드래그한 카드를 필드 슬롯에 놓도록 요청
        public static bool TryDropOnFieldSlot(FieldSlotView fieldSlot)
        {
            HandView handView = _activeDraggingHandView;

            if (handView == null || !handView._isDragging || fieldSlot == null)
            {
                return false;
            }

            if (BattleManager.Instance == null || BattleManager.Instance.LocalPlayerData == null)
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
                // 필드 카드는 같은 라인에서 이동하거나 교환
                moveSuccess = CardZoneManager.Instance.MoveOrSwapFieldCard(
                    playerData, card, fieldSlot.GetFieldPosition(), fieldSlot.GetSlotIndex());
            }
            else
            {
                // 손패 카드는 내 턴에만 필드로 배치
                if (BattleManager.Instance.CurrentTurnPlayer != playerData)
                {
                    Debug.LogWarning("HandView: 현재 내 턴이 아닙니다.");
                    return false;
                }

                // 카드의 에너지 조건을 확인
                if (!EnergyRuleService.HasEnoughEnergy(playerData, card.cardData, out string energyError))
                {
                    Debug.LogWarning($"HandView: {energyError}");
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
                    Debug.LogWarning($"HandView: AP가 부족합니다. 필요 = {apCost}, 보유 = {playerData.activeAP}");
                    return false;
                }

                // 조건을 통과한 카드를 지정 필드 슬롯으로 이동
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

                // 필드 배치가 성공한 뒤 AP 지불
                if (apCost > 0 && !playerData.TrySpendAP(apCost))
                {
                    CardZoneManager.Instance.MoveCard(playerData, card, Enums.BattleEnums.CardZone.Hand);
                    Debug.LogWarning("HandView: AP 지불에 실패해 카드를 손패로 되돌렸습니다.");
                    return false;
                }
            }

            if (!moveSuccess)
            {
                Debug.LogWarning("HandView: 필드 슬롯으로 카드 이동에 실패했습니다.");
                return false;
            }

            handView._wasDroppedOnField = true;
            Debug.Log($"HandView: 카드 {card.cardData.CardID} 이동 성공. 위치 = {fieldSlot.GetFieldPosition()}, 슬롯 = {fieldSlot.GetSlotIndex()}, 남은 AP = {playerData.activeAP}");
            return true;
        }

        #endregion

        #region 드래그 종료 및 손패 재정렬

        // 드래그 종료 후 카드와 드래그 상태를 복구
        private void HandleEndDrag(CardView cardView, PointerEventData eventData)
        {
            if (!_isDragging)
            {
                return;
            }

            bool wasFieldCard = _isDraggingFieldCard;
            bool wasDroppedOnField = _wasDroppedOnField;
            int fromIndex = _originalCardIndex;
            int toIndex = -1;

            // 필드에 놓지 않은 손패는 손패 순서 변경 위치를 계산
            if (!wasFieldCard && !wasDroppedOnField)
            {
                toIndex = GetDropIndex(eventData);
            }

            RestoreOriginalCard();
            HideDraggingCard();
            ResetDragState();

            // 필드 카드 드래그는 손패 순서를 변경하지 않음
            if (wasDroppedOnField || wasFieldCard)
            {
                return;
            }

            if (BattleManager.Instance != null && BattleManager.Instance.LocalPlayerData != null && CardZoneManager.Instance != null)
            {
                CardZoneManager.Instance.ReorderHand(BattleManager.Instance.LocalPlayerData, fromIndex, toIndex);
            }
        }

        // 마우스 위치에 해당하는 손패 삽입 순서를 계산
        private int GetDropIndex(PointerEventData eventData)
        {
            if (eventData == null || rootCanvas == null)
            {
                return -1;
            }

            int insertIndex = 0;
            Camera uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;

            for (int i = 0; i < cardViews.Count; i++)
            {
                CardView cardView = cardViews[i];

                if (cardView == null || cardView == _originalCardView || !cardView.gameObject.activeInHierarchy)
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

        #endregion

        #region 드래그 UI 복구

        // 드래그가 끝난 원본 카드의 표시와 입력 상태를 복구
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

        // 드래그용 카드 UI를 숨김
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

        // 마우스 화면 좌표를 DragLayer 로컬 좌표로 변환
        private void UpdateDraggingCardPosition(PointerEventData eventData)
        {
            if (dragLayer == null || draggingCard == null || rootCanvas == null || eventData == null)
            {
                return;
            }

            Camera uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;

            if (rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay && uiCamera == null)
            {
                Debug.LogError("HandView: Canvas의 Render Camera가 연결되지 않았습니다.");
                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(dragLayer, eventData.position, uiCamera, out Vector2 localPoint))
            {
                RectTransform draggingRect = draggingCard.transform as RectTransform;

                if (draggingRect == null)
                {
                    Debug.LogError("HandView: DraggingCard의 RectTransform을 찾을 수 없습니다.");
                    return;
                }

                draggingRect.anchoredPosition = localPoint;
            }
        }

        // 드래그 상태와 임시 참조를 초기화
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

        #endregion
    }
}