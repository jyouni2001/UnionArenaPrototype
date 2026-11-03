using UnityEngine;
using UnityEngine.UI;
using Data;
using Loaders;

namespace UI
{
    public class CardView : MonoBehaviour
    {
        [Header("카드 이미지")]
        [SerializeField] private Image cardImage; // 카드 이미지

        private CardImageLoader _cardImageLoader; // 카드 이미지 불러오는 객체

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

        // 카드 id에 해당되는 이미지 표시
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

            Sprite sprite = _cardImageLoader.LoadCardImage(cardID);

            if (sprite == null)
            {
                Debug.LogError("CardView: 카드 이미지를 불러오지 못했습니다. " + cardID);
                return;
            }

            cardImage.sprite = sprite;
            cardImage.preserveAspect = true; // 이미지 비율 유지
        }
    }
}