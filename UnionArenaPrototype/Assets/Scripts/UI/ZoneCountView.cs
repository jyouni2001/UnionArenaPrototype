using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    // 한 플레이어의 덱·트래쉬·리무브 카드 수를 표시
    public class ZoneCountView : MonoBehaviour
    {
        #region UI 연결

        [Header("영역별 카드 수 Text")]
        [SerializeField] private Text deckCountText; // 덱 카드 수
        [SerializeField] private Text trashCountText; // 트래쉬 카드 수
        [SerializeField] private Text removeCountText; // 리무브 카드 수

        private int lastDeckCount = -1; // 마지막으로 표시한 덱 카드 수
        private int lastTrashCount = -1; // 마지막으로 표시한 트래쉬 카드 수
        private int lastRemoveCount = -1; // 마지막으로 표시한 리무브 카드 수

        #endregion

        #region 카드 수 표시

        // 카드 수가 바뀐 영역의 Text만 갱신
        public void SetCounts(int deckCount, int trashCount, int removeCount)
        {
            if (lastDeckCount != deckCount)
            {
                lastDeckCount = deckCount;

                if (deckCountText != null)
                {
                    deckCountText.text = deckCount.ToString();
                }
            }

            if (lastTrashCount != trashCount)
            {
                lastTrashCount = trashCount;

                if (trashCountText != null)
                {
                    trashCountText.text = trashCount.ToString();
                }
            }

            if (lastRemoveCount != removeCount)
            {
                lastRemoveCount = removeCount;

                if (removeCountText != null)
                {
                    removeCountText.text = removeCount.ToString();
                }
            }
        }

        #endregion
    }
}