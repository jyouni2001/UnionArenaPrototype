using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Battle;
using Enums;

namespace Manager
{
    // 게임 화면의 UI 연결 및 갱신 관리
    public class UIManager : BaseManager
    {
        #region 싱글톤 및 UI 참조

        public static UIManager Instance { get; private set; }

        [Header("멀리건 UI")]
        [SerializeField] private GameObject mulliganPanel; // 멀리건 패널
        [SerializeField] private UI.HandView mulliganHandView; // 멀리건 손패 UI

        [Header("일반 손패 UI")]
        [SerializeField] private UI.HandView playerHandView; // 내 일반 손패 UI

        [Header("플레이어 프론트라인 UI")]
        [SerializeField] private UI.FieldSlotView[] playerFrontLineSlots =
            new UI.FieldSlotView[4]; // 프론트라인 슬롯 UI 배열

        [Header("플레이어 에너지라인 UI")]
        [SerializeField] private UI.FieldSlotView[] playerEnergyLineSlots =
            new UI.FieldSlotView[4]; // 에너지라인 슬롯 UI 배열
        
        [Header("플레이어 에너지 표시 UI")]
        [SerializeField] private UI.EnergyAreaView playerEnergyAreaView;

        [Header("멀리건 버튼")]
        [SerializeField] private Button keepButton; // 손패 유지 버튼
        [SerializeField] private Button mulliganButton; // 멀리건 버튼

        [Header("턴 버튼")]
        [SerializeField] private Button endTurnButton; // 턴 종료 버튼

        private bool isProcessing = false; // 버튼 중복 입력 방지
        private Coroutine _refreshCoroutine; // UI 갱신 코루틴

        #endregion

        #region 생명 주기 및 이벤트 연결

        // 싱글톤 등록 및 기본 초기화
        protected override void Awake()
        {
            // 이미 다른 UI 매니저가 존재하면 현재 오브젝트 제거
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this; // 현재 UI 매니저를 싱글톤으로 등록

            base.Awake(); // 부모 매니저의 초기화 실행
        }

        // UI 매니저 초기화
        protected override void Initialize()
        {
            isProcessing = false; // 버튼 처리 상태 초기화

            base.Initialize(); // 부모 매니저의 초기화 실행
        }

        // 버튼 이벤트 및 카드 영역 변경 이벤트 등록
        private void Start()
        {
            // 유지 버튼이 연결되어 있으면 클릭 이벤트 등록
            if (keepButton != null)
            {
                keepButton.onClick.AddListener(KeepInitialHand);
            }

            // 멀리건 버튼이 연결되어 있으면 클릭 이벤트 등록
            if (mulliganButton != null)
            {
                mulliganButton.onClick.AddListener(MulliganInitialHand);
            }

            // 턴 종료 버튼이 연결되어 있으면 클릭 이벤트 등록
            if (endTurnButton != null)
            {
                endTurnButton.onClick.AddListener(HandleEndTurnButtonClicked);
            }

            // 카드 영역 매니저가 존재하면 공통 영역 변경 이벤트 구독
            if (CardZoneManager.Instance != null)
            {
                CardZoneManager.Instance.OnZoneChanged += HandleZoneChanged;
            }
            else
            {
                Debug.LogError("UIManager: CardZoneManager를 찾을 수 없습니다.");
            }

            StartCoroutine(WaitForInitialHand());
        }

        // 버튼 이벤트, 카드 영역 변경 이벤트 및 싱글톤 해제
        private void OnDestroy()
        {
            // 실행 중인 UI 갱신 코루틴 중지
            if (_refreshCoroutine != null)
            {
                StopCoroutine(_refreshCoroutine);
                _refreshCoroutine = null;
            }

            // 유지 버튼 이벤트 해제
            if (keepButton != null)
            {
                keepButton.onClick.RemoveListener(KeepInitialHand);
            }

            // 멀리건 버튼 이벤트 해제
            if (mulliganButton != null)
            {
                mulliganButton.onClick.RemoveListener(MulliganInitialHand);
            }

            // 턴 종료 버튼 이벤트 해제
            if (endTurnButton != null)
            {
                endTurnButton.onClick.RemoveListener(HandleEndTurnButtonClicked);
            }

            // 카드 영역 변경 이벤트 구독 해제
            if (CardZoneManager.Instance != null)
            {
                CardZoneManager.Instance.OnZoneChanged -= HandleZoneChanged;
            }

            // 현재 인스턴스가 싱글톤으로 등록된 객체일 때만 초기화
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion

        #region 버튼 및 카드 영역 이벤트 조율

        // 턴 종료 버튼 처리
        private void HandleEndTurnButtonClicked()
        {
            if (BattleManager.Instance == null)
            {
                Debug.LogError("UIManager: BattleManager를 찾을 수 없습니다.");
                return;
            }

            if (!BattleManager.Instance.EndCurrentTurn())
            {
                Debug.LogWarning("UIManager: 턴을 넘기지 못했습니다.");
                return;
            }

            RefreshPlayerHand();
            RefreshPlayerField();
        }

        // 카드 영역 변경 이벤트 수신
        private void HandleZoneChanged(
            PlayerGameData playerData,
            BattleEnums.CardZone changedZone)
        {
            // 전투 매니저 또는 내 플레이어 데이터가 준비되지 않았다면 갱신 중단
            if (BattleManager.Instance == null ||
                BattleManager.Instance.LocalPlayerData == null)
            {
                return;
            }

            // 상대방 데이터가 변경된 경우 내 UI는 갱신하지 않음
            if (playerData != BattleManager.Instance.LocalPlayerData)
            {
                return;
            }

            // 드래그 종료 이후 손패와 필드 UI를 함께 갱신하도록 예약
            SchedulePlayerUIRefresh();
        }

        // 플레이어 UI 갱신 예약
        private void SchedulePlayerUIRefresh()
        {
            // 이미 갱신 작업이 예약되어 있으면 중복 실행하지 않음
            if (_refreshCoroutine != null)
            {
                return;
            }

            _refreshCoroutine = StartCoroutine(RefreshPlayerUIAfterFrame());
        }

        // 다음 프레임에 플레이어 UI 갱신
        private IEnumerator RefreshPlayerUIAfterFrame()
        {
            // 현재 프레임의 드래그 이벤트 처리가 끝날 때까지 대기
            yield return null;

            _refreshCoroutine = null;

            // 전투 매니저 또는 플레이어 데이터가 없으면 갱신 중단
            if (BattleManager.Instance == null ||
                BattleManager.Instance.LocalPlayerData == null)
            {
                yield break;
            }

            RefreshPlayerHand();
            RefreshPlayerField();
        }

        // 최초 손패 준비 완료 대기
        private IEnumerator WaitForInitialHand()
        {
            float waitTime = 0f;

            // 최대 10초 동안 최초 손패 선택 상태 확인
            while (waitTime < 10f)
            {
                if (BattleManager.Instance != null &&
                    BattleManager.Instance.IsWaitingForInitialHandChoice)
                {
                    RefreshMulliganHand();
                    ShowMulliganPanel();
                    yield break;
                }

                yield return null;
                waitTime += Time.unscaledDeltaTime;
            }

            Debug.LogError("UIManager: 최초 손패 준비를 확인하지 못했습니다.");
        }

        #endregion

        #region UI 표시 갱신

        // 멀리건 손패 표시
        public void RefreshMulliganHand()
        {
            if (BattleManager.Instance == null ||
                BattleManager.Instance.LocalPlayerData == null ||
                mulliganHandView == null)
            {
                Debug.LogError("UIManager: 멀리건 손패 연결을 확인하세요.");
                return;
            }

            mulliganHandView.SetCards(
                BattleManager.Instance.LocalPlayerData.hand);
        }

        // 일반 손패 표시
        public void RefreshPlayerHand()
        {
            if (BattleManager.Instance == null ||
                BattleManager.Instance.LocalPlayerData == null ||
                playerHandView == null)
            {
                Debug.LogError("UIManager: 일반 손패 UI 연결을 확인하세요.");
                return;
            }

            playerHandView.SetCards(
                BattleManager.Instance.LocalPlayerData.hand);
        }

        // 플레이어 필드 UI 갱신
        public void RefreshPlayerField()
        {
            if (BattleManager.Instance == null ||
                BattleManager.Instance.LocalPlayerData == null)
            {
                return;
            }

            PlayerGameData playerData =
                BattleManager.Instance.LocalPlayerData;

            ClearFieldSlots(playerFrontLineSlots);
            ClearFieldSlots(playerEnergyLineSlots);

            if (playerData.field != null)
            {
                for (int i = 0; i < playerData.field.Count; i++)
                {
                    CardRuntimeData card = playerData.field[i];

                    if (card == null)
                    {
                        continue;
                    }

                    int slotIndex = card.fieldSlotIndex;

                    if (card.fieldPosition == BattleEnums.FieldPosition.FrontLine)
                    {
                        ShowFieldCard(playerFrontLineSlots, slotIndex, card);
                    }
                    else if (card.fieldPosition == BattleEnums.FieldPosition.EnergyLine)
                    {
                        ShowFieldCard(playerEnergyLineSlots, slotIndex, card);
                    }
                }
            }

            RefreshPlayerEnergy();
        }
        
        // 에너지 규칙 서비스의 결과를 에너지 View에 전달
        private void RefreshPlayerEnergy()
        {
            if (playerEnergyAreaView == null)
            {
                return;
            }

            PlayerGameData playerData = null;

            if (BattleManager.Instance != null)
            {
                playerData = BattleManager.Instance.LocalPlayerData;
            }

            EnergySummary summary =
                EnergyRuleService.GetEnergySummary(playerData);

            playerEnergyAreaView.SetEnergySummary(summary);
        }

        // 필드 슬롯 배열의 카드 UI 초기화
        private void ClearFieldSlots(UI.FieldSlotView[] slots)
        {
            if (slots == null)
            {
                return;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                {
                    slots[i].ClearCard();
                }
            }
        }

        // 지정한 필드 슬롯에 카드 표시
        private void ShowFieldCard(
            UI.FieldSlotView[] slots,
            int slotIndex,
            CardRuntimeData card)
        {
            if (slots == null)
            {
                return;
            }

            if (slotIndex < 0 || slotIndex >= slots.Length)
            {
                Debug.LogWarning(
                    $"UIManager: 잘못된 필드 슬롯 번호입니다. SlotIndex: {slotIndex}");
                return;
            }

            if (slots[slotIndex] == null)
            {
                Debug.LogWarning(
                    $"UIManager: 필드 슬롯 UI가 연결되지 않았습니다. SlotIndex: {slotIndex}");
                return;
            }

            slots[slotIndex].ShowCard(card);
        }

        #endregion

        #region 최초 손패 선택 UI

        // 멀리건 패널 표시
        public void ShowMulliganPanel()
        {
            if (mulliganPanel != null)
            {
                mulliganPanel.SetActive(true);
            }
        }

        // 멀리건 패널 숨김
        public void HideMulliganPanel()
        {
            if (mulliganPanel != null)
            {
                mulliganPanel.SetActive(false);
            }
        }

        // 최초 손패 유지
        public void KeepInitialHand()
        {
            if (isProcessing || BattleManager.Instance == null)
            {
                return;
            }

            isProcessing = true;
            SetMulliganButtons(false);
            HideMulliganPanel();

            bool success = BattleManager.Instance.KeepInitialHand();

            if (!success)
            {
                ShowMulliganPanel();
                SetMulliganButtons(true);
                isProcessing = false;
                return;
            }

            RefreshPlayerHand();
            RefreshPlayerField();
        }

        // 최초 손패 멀리건
        public void MulliganInitialHand()
        {
            if (isProcessing || BattleManager.Instance == null)
            {
                return;
            }

            isProcessing = true;
            SetMulliganButtons(false);
            HideMulliganPanel();

            bool success = BattleManager.Instance.MulliganInitialHand();

            if (!success)
            {
                ShowMulliganPanel();
                SetMulliganButtons(true);
                isProcessing = false;
                return;
            }

            RefreshPlayerHand();
            HideMulliganPanel();

            SetMulliganButtons(false);
            isProcessing = false;
        }

        // 멀리건 버튼 활성화 상태 변경
        private void SetMulliganButtons(bool interactable)
        {
            if (keepButton != null)
            {
                keepButton.interactable = interactable;
            }

            if (mulliganButton != null)
            {
                mulliganButton.interactable = interactable;
            }
        }

        #endregion
    }
}
