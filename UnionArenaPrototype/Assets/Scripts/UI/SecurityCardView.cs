using UnityEngine;
using Battle;

namespace UI
{
    // 미리 만들어 둔 시큐리티 카드 한 장의 뒷면·앞면 표시를 담당
    public class SecurityCardView : MonoBehaviour
    {
        #region UI 연결

        // 카드 뒷면 이미지 오브젝트
        [Header("카드 뒷면")]
        [SerializeField] private GameObject cardBackObject; // 뒷면 UI

        // 공개된 카드 앞면을 표시할 뷰
        [Header("카드 앞면")]
        [SerializeField] private CardView faceCardView; // 앞면 카드 UI

        #endregion

        #region 초기화 및 표시

        // 씬 시작 시 카드를 뒷면으로 표시
        private void Awake()
        {
            ShowBack();
        }

        // 시큐리티 카드를 뒷면으로 표시
        public void ShowBack()
        {
            if (cardBackObject != null)
            {
                cardBackObject.SetActive(true);
            }

            if (faceCardView != null)
            {
                faceCardView.gameObject.SetActive(false);
            }

            gameObject.SetActive(true);
        }

        // 공격으로 공개된 카드를 앞면으로 표시
        public void ShowFace(CardRuntimeData card)
        {
            if (card == null || card.cardData == null)
            {
                Debug.LogWarning(
                    "SecurityCardView: 공개할 카드 데이터가 없습니다.");
                return;
            }

            if (faceCardView == null)
            {
                Debug.LogError(
                    "SecurityCardView: Face Card View가 연결되지 않았습니다.");
                return;
            }

            gameObject.SetActive(true);

            if (cardBackObject != null)
            {
                cardBackObject.SetActive(false);
            }

            faceCardView.SetCard(card.cardData.CardID);
            faceCardView.gameObject.SetActive(true);
        }

        // 시큐리티에서 빠진 카드 슬롯을 숨김
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        #endregion
    }
}