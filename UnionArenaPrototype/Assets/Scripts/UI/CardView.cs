using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Data;
using Loaders;

namespace UI
{
    public class CardView : MonoBehaviour,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        [Header("카드 이미지")]
        [SerializeField] private Image cardImage; // 카드 이미지

        private CardImageLoader _cardImageLoader; // 카드 이미지 불러오는 객체
        private string _currentCardID; // 현재 표시 중인 카드 ID

        private Action<CardView, PointerEventData> _onBeginDrag; // 드래그 시작 이벤트
        private Action<CardView, PointerEventData> _onDrag; // 드래그 중 이벤트
        private Action<CardView, PointerEventData> _onEndDrag; // 드래그 종료 이벤트

        private void Awake()
        {
            InitializeImageLoader();
        }

        // 이미지 로더 초기화
        private void InitializeImageLoader()
        {
            if (_cardImageLoader != null)
            {
                return;
            }

            _cardImageLoader = new CardImageLoader(); // 카드 이미지 로더 생성
        }

        // 카드 ID에 해당되는 이미지 표시
        public void SetCard(string cardID)
        {
            if (cardImage == null)
            {
                Debug.LogError("CardView: CardImage가 연결되지 않았습니다.");
                return;
            }

            if (string.IsNullOrEmpty(cardID))
            {
                Debug.LogError("CardView: 카드 ID가 비어 있습니다.");
                return;
            }

            if (_cardImageLoader == null)
            {
                InitializeImageLoader(); // 초기화되지 않은 경우 로더 생성
            }

            Sprite sprite = _cardImageLoader.LoadCardImage(cardID); // 카드 이미지 불러오기

            if (sprite == null)
            {
                Debug.LogError("CardView: 카드 이미지를 불러오지 못했습니다. " + cardID);
                return;
            }

            cardImage.sprite = sprite; // 카드 이미지 적용
            cardImage.preserveAspect = true; // 이미지 비율 유지

            _currentCardID = cardID; // 현재 표시 중인 카드 ID 저장
        }

        // 현재 표시 중인 카드 ID 반환
        public string GetCardID()
        {
            return _currentCardID;
        }

        // 손패 UI에서 드래그 이벤트 연결
        public void SetDragEvents(
            Action<CardView, PointerEventData> onBeginDrag,
            Action<CardView, PointerEventData> onDrag,
            Action<CardView, PointerEventData> onEndDrag)
        {
            _onBeginDrag = onBeginDrag; // 드래그 시작 이벤트 연결
            _onDrag = onDrag; // 드래그 중 이벤트 연결
            _onEndDrag = onEndDrag; // 드래그 종료 이벤트 연결
        }

        // 마우스로 카드를 잡았을 때 호출
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData == null)
            {
                return;
            }

            _onBeginDrag?.Invoke(this, eventData); // 손패 UI에 드래그 시작 전달
        }

        // 마우스를 움직이는 동안 호출
        public void OnDrag(PointerEventData eventData)
        {
            if (eventData == null)
            {
                return;
            }

            _onDrag?.Invoke(this, eventData); // 손패 UI에 드래그 중 이벤트 전달
        }

        // 마우스 버튼을 놓았을 때 호출
        public void OnEndDrag(PointerEventData eventData)
        {
            if (eventData == null)
            {
                return;
            }

            _onEndDrag?.Invoke(this, eventData); // 손패 UI에 드래그 종료 전달
        }

        // 카드 이미지 반환
        public Image GetCardImage()
        {
            return cardImage; // 현재 카드 이미지 반환
        }
    }
}
