
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    // 카드 목록을 열었을 때 최대 15개의 카드 UI를 생성하고 재사용
    public class ZoneCardListView : MonoBehaviour
    {
        [Header("카드 UI 프리팹")]
        [SerializeField] private GameObject cardItemPrefab; // 카드 UI 프리팹
        [SerializeField] private RectTransform content; // 카드 UI가 생성될 Content

        [Header("카드 배치 설정")]
        [SerializeField] private int maxCardCount = 15; // 생성할 최대 카드 UI 개수
        [SerializeField] private int columnCount = 5; // 한 줄에 배치할 카드 개수

        [Header("카드 크기 5:7 비율")]
        [SerializeField] private float cardWidth = 260.0f; // 카드 너비
        [SerializeField] private float cardHeight = 364.0f; // 카드 높이

        [Header("카드 간격")]
        [SerializeField] private float spacingX = 20.0f; // 가로 카드 간격
        [SerializeField] private float spacingY = 20.0f; // 세로 카드 간격

        private readonly List<GameObject> cardItems = new List<GameObject>(); // 생성된 카드 UI 목록

        private void Awake()
        {
            CreateCardItems();
        }

        // 게임 시작 시 최대 개수만큼 카드 UI 생성
        private void CreateCardItems()
        {
            if (cardItemPrefab == null || content == null) // 프리팹 또는 Content가 연결되지 않은 경우
            {
                Debug.LogError("ZoneCardListView: 카드 UI 프리팹 또는 Content가 연결되지 않았습니다.");
                return;
            }

            if (maxCardCount <= 0 || columnCount <= 0) // 잘못된 카드 개수 및 열 설정 방지
            {
                Debug.LogError("ZoneCardListView: 카드 개수와 열 개수는 1 이상이어야 합니다.");
                return;
            }

            for (int i = 0; i < maxCardCount; i++)
            {
                GameObject cardItem = Instantiate(cardItemPrefab, content); // Content 아래에 카드 UI 생성
                RectTransform rectTransform = cardItem.GetComponent<RectTransform>(); // 카드 UI의 위치와 크기 설정

                rectTransform.anchorMin = new Vector2(0.0f, 1.0f); // 기준점 왼쪽 위
                rectTransform.anchorMax = new Vector2(0.0f, 1.0f); // 기준점 왼쪽 위
                rectTransform.pivot = new Vector2(0.5f, 1.0f); // 위치 계산 기준을 카드 상단 중앙으로 설정
                rectTransform.sizeDelta = new Vector2(cardWidth, cardHeight); // 카드 크기 설정

                cardItems.Add(cardItem); // 생성한 카드 UI를 관리 목록에 저장
                cardItem.SetActive(false); // 카드 데이터가 연결되기 전까지 숨김
            }

            ArrangeCardItems();
        }

        // 카드 UI를 설정된 열 개수에 맞춰 자동 정렬
        private void ArrangeCardItems()
        {
            float totalRowWidth = columnCount * cardWidth + (columnCount - 1) * spacingX; // 카드 너비와 카드 사이 간격을 합산
            float startX = (content.rect.width - totalRowWidth) * 0.5f + cardWidth * 0.5f; // Content 가운데 정렬 및 Pivot 보정

            for (int i = 0; i < cardItems.Count; i++)
            {
                int row = i / columnCount; // 정수 나눗셈으로 카드의 행 번호 계산
                int column = i % columnCount; // 나머지 연산으로 카드의 열 번호 계산

                float posX = startX + column * (cardWidth + spacingX); // 열 번호에 따른 가로 위치 계산
                float posY = -row * (cardHeight + spacingY); // 행 번호에 따른 세로 위치 계산

                RectTransform rectTransform = cardItems[i].GetComponent<RectTransform>(); // 카드 UI의 RectTransform 가져오기
                rectTransform.anchoredPosition = new Vector2(posX, posY); // 계산한 위치 적용
            }

            int rowCount = Mathf.CeilToInt((float)maxCardCount / columnCount); // 최대 카드 개수에 필요한 행 개수 계산
            float contentHeight = rowCount * cardHeight + (rowCount - 1) * spacingY; // 전체 행 높이와 행 사이 간격 계산

            content.sizeDelta = new Vector2(content.sizeDelta.x, contentHeight); // 기존 가로 크기를 유지하고 Content 높이 변경
        }

        // 전달받은 카드 ID에 맞춰 카드 이미지 표시
        // 카드 목록이 변경될 때 외부 관리자에서 호출
        public void SetCardIDs(List<string> cardIDs)
        {
            if (cardIDs == null) // 전달받은 카드 목록이 없는 경우
            {
                HideAllCardItems();
                return;
            }

            for (int i = 0; i < cardItems.Count; i++)
            {
                if (i >= cardIDs.Count) // 표시할 카드 데이터가 없는 UI는 숨김
                {
                    cardItems[i].SetActive(false);
                    continue;
                }

                CardView cardView = cardItems[i].GetComponent<CardView>(); // 카드 이미지 표시를 담당하는 CardView 가져오기

                if (cardView == null) // CardView가 연결되지 않은 경우
                {
                    Debug.LogError("ZoneCardListView: CardView가 연결되지 않았습니다.");
                    cardItems[i].SetActive(false);
                    continue;
                }

                cardItems[i].SetActive(true); // 카드 UI 활성화
                cardView.SetCard(cardIDs[i]); // 카드 ID에 해당하는 이미지 표시
            }
        }

        // 카드 목록이 없거나 초기화할 때 모든 카드 UI 숨기기
        private void HideAllCardItems()
        {
            for (int i = 0; i < cardItems.Count; i++)
            {
                cardItems[i].SetActive(false); // 모든 카드 UI 비활성화
            }
        }
    }
}
