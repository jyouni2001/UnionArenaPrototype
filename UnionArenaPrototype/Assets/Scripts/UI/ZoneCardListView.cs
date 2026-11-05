using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Battle;

namespace UI
{
    // Trash 및 Remove 카드 목록 UI 관리
    // 최대 15개의 카드 UI를 생성하고 스크롤 위치에 따라 재사용
    // 실제 CardRuntimeData 목록을 전달받아 카드 이미지를 표시
    // 카드 수에 따라 Content 높이와 스크롤 범위를 자동 조정
    // 패널 우클릭 및 바깥쪽 좌클릭으로 패널 닫기
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
        private readonly List<CardRuntimeData> currentCards = new List<CardRuntimeData>(); // 현재 표시할 카드 데이터

        private ScrollRect scrollRect; // 카드 목록 스크롤 관리 객체
        private RectTransform panelRect; // 카드 목록 패널의 RectTransform
        private Canvas parentCanvas; // 패널이 속한 Canvas
        private int currentFirstRow = -1; // 현재 화면에서 재사용 중인 첫 번째 행

        private int VisibleRowCount => Mathf.CeilToInt((float)maxCardCount / columnCount); // 화면에 유지할 UI 행 개수
        private float RowHeight => cardHeight + spacingY; // 카드 한 행의 전체 높이

        private void Awake()
        {
            panelRect = GetComponent<RectTransform>(); // 현재 오브젝트의 패널 영역 가져오기
            parentCanvas = GetComponentInParent<Canvas>(); // 상위 Canvas 가져오기

            CreateCardItems();
        }

        private void Update()
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            Mouse mouse = Mouse.current; // 현재 마우스 입력 장치 가져오기

            if (mouse == null)
            {
                return;
            }

            if (mouse.rightButton.wasPressedThisFrame)
            {
                if (IsPointerInsidePanel())
                {
                    ClosePanel();
                }

                return;
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (!IsPointerInsidePanel())
                {
                    ClosePanel();
                }
            }
        }

        private void Start()
        {
            Canvas.ForceUpdateCanvases(); // UI 크기 계산이 완료되도록 갱신

            if (content != null)
            {
                ArrangeCardItems(); // 카드 UI 기본 위치 설정
            }
        }

        private void OnEnable()
        {
            if (scrollRect == null && content != null)
            {
                scrollRect = content.GetComponentInParent<ScrollRect>(); // Content의 상위 ScrollRect 찾기
            }

            if (scrollRect != null)
            {
                scrollRect.onValueChanged.AddListener(OnScrollValueChanged); // 스크롤 위치 변경 이벤트 등록
            }
        }

        private void OnDisable()
        {
            if (scrollRect != null)
            {
                scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged); // 스크롤 이벤트 해제
            }
        }

        // 게임 시작 시 최대 개수만큼 카드 UI 생성
        private void CreateCardItems()
        {
            if (cardItemPrefab == null || content == null)
            {
                Debug.LogError("ZoneCardListView: 카드 UI 프리팹 또는 Content가 연결되지 않았습니다.");
                return;
            }

            if (maxCardCount <= 0 || columnCount <= 0 || maxCardCount % columnCount != 0)
            {
                Debug.LogError("ZoneCardListView: 최대 카드 UI 개수는 열 개수로 나누어 떨어져야 합니다.");
                return;
            }

            scrollRect = content.GetComponentInParent<ScrollRect>(); // Content의 상위 ScrollRect 찾기

            if (scrollRect == null)
            {
                Debug.LogError("ZoneCardListView: Content 상위에 ScrollRect가 없습니다.");
                return;
            }

            for (int i = 0; i < maxCardCount; i++)
            {
                GameObject cardItem = Instantiate(cardItemPrefab, content); // Content 아래에 카드 UI 생성
                RectTransform rectTransform = cardItem.GetComponent<RectTransform>(); // 카드 UI의 위치와 크기 설정

                if (rectTransform == null)
                {
                    Debug.LogError("ZoneCardListView: 카드 프리팹에 RectTransform이 없습니다.");
                    Destroy(cardItem);
                    continue;
                }

                rectTransform.anchorMin = new Vector2(0.0f, 1.0f); // 기준점 왼쪽 위
                rectTransform.anchorMax = new Vector2(0.0f, 1.0f); // 기준점 왼쪽 위
                rectTransform.pivot = new Vector2(0.5f, 1.0f); // 위치 계산 기준을 카드 상단 중앙으로 설정
                rectTransform.sizeDelta = new Vector2(cardWidth, cardHeight); // 카드 크기 설정

                cardItems.Add(cardItem); // 생성한 카드 UI를 관리 목록에 저장
                cardItem.SetActive(false); // 카드 데이터가 연결되기 전까지 숨김
            }

            ArrangeCardItems();
        }

        // 카드 UI를 5열 기준으로 자동 정렬
        private void ArrangeCardItems()
        {
            if (content == null || cardItems.Count == 0)
            {
                return;
            }

            float totalRowWidth = columnCount * cardWidth + (columnCount - 1) * spacingX; // 한 줄 전체 너비
            float startX = (content.rect.width - totalRowWidth) * 0.5f + cardWidth * 0.5f; // Content 가운데 정렬

            for (int i = 0; i < cardItems.Count; i++)
            {
                int row = i / columnCount; // 카드가 배치될 행 번호
                int column = i % columnCount; // 카드가 배치될 열 번호

                float posX = startX + column * (cardWidth + spacingX); // 가로 위치 계산
                float posY = -row * RowHeight; // 행 번호에 따라 아래쪽으로 배치

                RectTransform rectTransform = cardItems[i].GetComponent<RectTransform>();
                rectTransform.anchoredPosition = new Vector2(posX, posY); // 카드 위치 적용
            }
        }

        // 실제 카드 데이터를 전달받아 목록 갱신
        public void SetCards(List<CardRuntimeData> cards)
        {
            currentCards.Clear(); // 이전 카드 데이터 초기화

            if (cards != null)
            {
                currentCards.AddRange(cards); // 전달받은 카드 목록 복사
            }

            UpdateContentHeight(); // 실제 카드 수에 맞춰 Content 높이 변경

            if (scrollRect != null)
            {
                scrollRect.StopMovement(); // 이전 스크롤 관성 정지
                Canvas.ForceUpdateCanvases(); // Content 높이 변경 반영
                scrollRect.verticalNormalizedPosition = 1.0f; // 목록을 맨 위로 이동
            }

            currentFirstRow = -1; // 첫 번째 행을 다시 계산하도록 초기화
            RefreshVisibleCards(); // 현재 화면에 필요한 카드 UI 갱신
        }

        // 실제 카드 수에 따라 Content 높이 계산
        private void UpdateContentHeight()
        {
            if (content == null)
            {
                return;
            }

            int rowCount = Mathf.CeilToInt((float)currentCards.Count / columnCount); // 실제 카드 수에 필요한 행 개수 계산
            float contentHeight = rowCount > 0 ? rowCount * cardHeight + (rowCount - 1) * spacingY : 0.0f; // 전체 높이 계산

            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight); // Content 높이 변경
        }

        // 스크롤 위치가 변경되었을 때 호출
        private void OnScrollValueChanged(Vector2 normalizedPosition)
        {
            RefreshVisibleCards(); // 스크롤 위치에 맞춰 카드 UI 재사용
        }

        // 현재 스크롤 위치에 필요한 카드 UI 갱신
        private void RefreshVisibleCards()
        {
            if (content == null || scrollRect == null || cardItems.Count == 0)
            {
                return;
            }

            int totalRowCount = Mathf.CeilToInt((float)currentCards.Count / columnCount); // 실제 카드 행 개수 계산

            if (totalRowCount <= 0)
            {
                HideAllCardItems();
                currentFirstRow = 0;
                return;
            }

            float viewportHeight = scrollRect.viewport.rect.height; // 실제 화면에 표시되는 영역의 높이
            float maxScrollY = Mathf.Max(0.0f, content.rect.height - viewportHeight); // 실제 스크롤 가능한 전체 거리
            float scrollY = Mathf.Clamp(content.anchoredPosition.y, 0.0f, maxScrollY); // 현재 스크롤 위치 계산

            int firstRow = Mathf.FloorToInt(scrollY / RowHeight); // 현재 스크롤 위치의 첫 번째 행 계산
            int maxFirstRow = Mathf.Max(0, totalRowCount - VisibleRowCount); // 마지막 위치에서 표시할 첫 번째 행 계산

            firstRow = Mathf.Clamp(firstRow, 0, maxFirstRow); // 첫 번째 행이 목록 범위를 벗어나지 않도록 제한

            if (firstRow == currentFirstRow)
            {
                return;
            }

            currentFirstRow = firstRow; // 현재 첫 번째 행 저장

            for (int i = 0; i < cardItems.Count; i++)
            {
                int rowOffset = i / columnCount; // UI가 배치될 화면 내 행 번호
                int column = i % columnCount; // UI가 배치될 열 번호
                int dataRow = firstRow + rowOffset; // 실제 카드 데이터의 행 번호
                int cardIndex = dataRow * columnCount + column; // 실제 카드 목록의 인덱스

                if (cardIndex >= currentCards.Count || rowOffset >= VisibleRowCount)
                {
                    cardItems[i].SetActive(false); // 카드 데이터가 없는 UI 숨김
                    continue;
                }

                CardRuntimeData cardData = currentCards[cardIndex]; // 표시할 실제 카드 데이터 가져오기

                if (cardData == null || cardData.cardData == null)
                {
                    cardItems[i].SetActive(false);
                    continue;
                }

                CardView cardView = cardItems[i].GetComponent<CardView>(); // 카드 이미지 표시 담당 객체 가져오기

                if (cardView == null)
                {
                    Debug.LogError("ZoneCardListView: CardView가 연결되지 않았습니다.");
                    cardItems[i].SetActive(false);
                    continue;
                }

                RectTransform rectTransform = cardItems[i].GetComponent<RectTransform>(); // 카드 UI 위치 가져오기

                float posX = ArrangeCardX(column); // 열에 따른 가로 위치
                float posY = -rowOffset * RowHeight; // 화면 내 행에 따른 세로 위치

                rectTransform.anchoredPosition = new Vector2(posX, posY); // 카드 위치 갱신

                cardItems[i].SetActive(true); // 카드 UI 활성화
                cardView.SetCard(cardData.cardData.CardID); // 실제 카드 이미지 표시
            }
        }

        // Content 가운데 기준으로 카드 X 좌표 계산
        private float ArrangeCardX(int column)
        {
            float totalRowWidth = columnCount * cardWidth + (columnCount - 1) * spacingX;
            float startX = (content.rect.width - totalRowWidth) * 0.5f + cardWidth * 0.5f;

            return startX + column * (cardWidth + spacingX);
        }

        // 카드 목록이 없을 때 모든 카드 UI 숨김
        private void HideAllCardItems()
        {
            for (int i = 0; i < cardItems.Count; i++)
            {
                cardItems[i].SetActive(false); // 모든 카드 UI 비활성화
            }
        }

        // 마우스 포인터가 카드 목록 패널 내부에 있는지 확인
        private bool IsPointerInsidePanel()
        {
            if (panelRect == null)
            {
                return false;
            }

            Camera uiCamera = null;

            if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                uiCamera = parentCanvas.worldCamera; // 카메라 기반 Canvas일 때 UI 카메라 사용
            }

            return RectTransformUtility.RectangleContainsScreenPoint(
                panelRect,
                Mouse.current.position.ReadValue(),
                uiCamera
            );
        }

        // 카드 목록 패널 닫기
        private void ClosePanel()
        {
            gameObject.SetActive(false); // 현재 패널 비활성화
        }
    }
}
