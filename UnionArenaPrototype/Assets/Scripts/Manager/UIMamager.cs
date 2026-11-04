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

        [Header("멀리건 버튼")]
        [SerializeField] private Button keepButton; // 손패 유지 버튼
        [SerializeField] private Button mulliganButton; // 멀리건 버튼

        private bool isProcessing = false; // 버튼 중복 입력 방지
        private Coroutine _refreshCoroutine; // UI 갱신 코루틴

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
                keepButton.onClick.AddListener(KeepInitialHand); // 손패 유지 함수 연결
            }

            // 멀리건 버튼이 연결되어 있으면 클릭 이벤트 등록
            if (mulliganButton != null)
            {
                mulliganButton.onClick.AddListener(MulliganInitialHand); // 멀리건 함수 연결
            }

            // 카드 영역 매니저가 존재하면 공통 영역 변경 이벤트 구독
            if (CardZoneManager.Instance != null)
            {
                CardZoneManager.Instance.OnZoneChanged += HandleZoneChanged; // 영역 변경 이벤트 수신 함수 등록
            }
            else
            {
                Debug.LogError("UIManager: CardZoneManager를 찾을 수 없습니다.");
            }

            StartCoroutine(WaitForInitialHand()); // 최초 손패 준비 상태 확인 시작
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

            // 유지 버튼이 연결되어 있으면 클릭 이벤트 해제
            if (keepButton != null)
            {
                keepButton.onClick.RemoveListener(KeepInitialHand); // 유지 버튼 함수 연결 해제
            }

            // 멀리건 버튼이 연결되어 있으면 클릭 이벤트 해제
            if (mulliganButton != null)
            {
                mulliganButton.onClick.RemoveListener(MulliganInitialHand); // 멀리건 버튼 함수 연결 해제
            }

            // 카드 영역 매니저가 존재하면 공통 영역 변경 이벤트 구독 해제
            if (CardZoneManager.Instance != null)
            {
                CardZoneManager.Instance.OnZoneChanged -= HandleZoneChanged; // 카드 영역 변경 이벤트 수신 함수 해제
            }

            // 현재 인스턴스가 싱글톤으로 등록된 객체일 때만 초기화
            if (Instance == this)
            {
                Instance = null; // 싱글톤 참조 해제
            }
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

            _refreshCoroutine = null; // 예약 상태 초기화

            // 전투 매니저 또는 플레이어 데이터가 없으면 갱신 중단
            if (BattleManager.Instance == null ||
                BattleManager.Instance.LocalPlayerData == null)
            {
                yield break;
            }

            RefreshPlayerHand(); // 손패 UI 갱신
            RefreshPlayerField(); // 필드 UI 갱신
        }

        // 최초 손패 준비 완료 대기
        private IEnumerator WaitForInitialHand()
        {
            float waitTime = 0f; // 최초 손패 준비를 기다린 누적 시간

            // 최대 10초 동안 최초 손패 선택 상태 확인
            while (waitTime < 10f)
            {
                // 전투 매니저가 존재하고 최초 손패 선택 대기 상태인지 확인
                if (BattleManager.Instance != null &&
                    BattleManager.Instance.IsWaitingForInitialHandChoice)
                {
                    RefreshMulliganHand(); // 멀리건 손패 UI 갱신
                    ShowMulliganPanel(); // 멀리건 패널 표시
                    yield break;
                }

                yield return null; // 다음 프레임까지 대기
                waitTime += Time.unscaledDeltaTime; // 실제 시간 기준 대기 시간 누적
            }

            Debug.LogError("UIManager: 최초 손패 준비를 확인하지 못했습니다.");
        }

        // 멀리건 손패 표시
        public void RefreshMulliganHand()
        {
            // 전투 매니저, 플레이어 데이터 또는 멀리건 UI가 없으면 갱신 중단
            if (BattleManager.Instance == null ||
                BattleManager.Instance.LocalPlayerData == null ||
                mulliganHandView == null)
            {
                Debug.LogError("UIManager: 멀리건 손패 연결을 확인하세요.");
                return;
            }

            // 현재 손패 데이터를 멀리건 UI에 전달
            mulliganHandView.SetCards(
                BattleManager.Instance.LocalPlayerData.hand);
        }

        // 일반 손패 표시
        public void RefreshPlayerHand()
        {
            // 전투 매니저, 플레이어 데이터 또는 일반 손패 UI가 없으면 갱신 중단
            if (BattleManager.Instance == null ||
                BattleManager.Instance.LocalPlayerData == null ||
                playerHandView == null)
            {
                Debug.LogError("UIManager: 일반 손패 UI 연결을 확인하세요.");
                return;
            }

            // 현재 손패 데이터를 일반 손패 UI에 전달
            playerHandView.SetCards(
                BattleManager.Instance.LocalPlayerData.hand);
        }

        // 플레이어 필드 UI 갱신
        public void RefreshPlayerField()
        {
            // 전투 매니저 또는 플레이어 데이터가 없으면 갱신 중단
            if (BattleManager.Instance == null ||
                BattleManager.Instance.LocalPlayerData == null)
            {
                return;
            }

            PlayerGameData playerData =
                BattleManager.Instance.LocalPlayerData; // 현재 플레이어 데이터

            // 기존 프론트라인 카드 UI 초기화
            ClearFieldSlots(playerFrontLineSlots);

            // 기존 에너지라인 카드 UI 초기화
            ClearFieldSlots(playerEnergyLineSlots);

            // 필드 카드 목록이 없으면 갱신 중단
            if (playerData.field == null)
            {
                return;
            }

            // 필드에 등록된 카드 순회
            for (int i = 0; i < playerData.field.Count; i++)
            {
                CardRuntimeData card = playerData.field[i]; // 현재 검사할 카드 데이터

                // 카드 데이터가 없으면 건너뛰기
                if (card == null)
                {
                    continue;
                }

                int slotIndex = card.fieldSlotIndex; // 카드가 배치된 슬롯 번호

                // 프론트라인 카드 표시
                if (card.fieldPosition == BattleEnums.FieldPosition.FrontLine)
                {
                    ShowFieldCard(playerFrontLineSlots, slotIndex, card);
                }
                // 에너지라인 카드 표시
                else if (card.fieldPosition == BattleEnums.FieldPosition.EnergyLine)
                {
                    ShowFieldCard(playerEnergyLineSlots, slotIndex, card);
                }
            }
        }

        // 필드 슬롯 배열의 카드 UI 초기화
        private void ClearFieldSlots(UI.FieldSlotView[] slots)
        {
            // 슬롯 배열이 없으면 종료
            if (slots == null)
            {
                return;
            }

            // 모든 슬롯 순회
            for (int i = 0; i < slots.Length; i++)
            {
                // 슬롯 연결이 없으면 건너뛰기
                if (slots[i] == null)
                {
                    continue;
                }

                slots[i].ClearCard(); // 카드 UI 숨김
            }
        }

        // 지정한 필드 슬롯에 카드 표시
        private void ShowFieldCard(
            UI.FieldSlotView[] slots,
            int slotIndex,
            CardRuntimeData card)
        {
            // 슬롯 배열이 없으면 종료
            if (slots == null)
            {
                return;
            }

            // 슬롯 번호가 배열 범위를 벗어나면 표시 중단
            if (slotIndex < 0 || slotIndex >= slots.Length)
            {
                Debug.LogWarning(
                    $"UIManager: 잘못된 필드 슬롯 번호입니다. SlotIndex: {slotIndex}");

                return;
            }

            // 해당 슬롯 UI가 연결되지 않았다면 표시 중단
            if (slots[slotIndex] == null)
            {
                Debug.LogWarning(
                    $"UIManager: 필드 슬롯 UI가 연결되지 않았습니다. SlotIndex: {slotIndex}");

                return;
            }

            slots[slotIndex].ShowCard(card); // 해당 슬롯에 카드 표시
        }

        // 멀리건 패널 표시
        public void ShowMulliganPanel()
        {
            // 멀리건 패널이 연결되어 있으면 활성화
            if (mulliganPanel != null)
            {
                mulliganPanel.SetActive(true); // 멀리건 패널 화면에 표시
            }
        }

        // 멀리건 패널 숨김
        public void HideMulliganPanel()
        {
            // 멀리건 패널이 연결되어 있으면 비활성화
            if (mulliganPanel != null)
            {
                mulliganPanel.SetActive(false); // 멀리건 패널 화면에서 숨김
            }
        }

        // 최초 손패 유지
        public void KeepInitialHand()
        {
            // 이미 버튼 처리가 진행 중이거나 전투 매니저가 없으면 실행 중단
            if (isProcessing || BattleManager.Instance == null)
            {
                return;
            }

            isProcessing = true; // 버튼 중복 입력 방지 상태 설정
            SetMulliganButtons(false); // 멀리건 버튼 비활성화
            HideMulliganPanel(); // 멀리건 패널 숨김

            bool success =
                BattleManager.Instance.KeepInitialHand(); // 최초 손패 유지 처리 결과

            // 손패 유지 처리에 실패한 경우 버튼과 패널 복구
            if (!success)
            {
                ShowMulliganPanel(); // 멀리건 패널 다시 표시
                SetMulliganButtons(true); // 멀리건 버튼 다시 활성화
                isProcessing = false; // 버튼 처리 상태 초기화
                return;
            }

            RefreshPlayerHand(); // 유지한 손패를 일반 손패 UI에 표시
            RefreshPlayerField(); // 현재 필드 UI 갱신
        }

        // 최초 손패 멀리건
        public void MulliganInitialHand()
        {
            // 이미 버튼 처리가 진행 중이거나 전투 매니저가 없으면 실행 중단
            if (isProcessing || BattleManager.Instance == null)
            {
                return;
            }

            isProcessing = true; // 버튼 중복 입력 방지 상태 설정
            SetMulliganButtons(false); // 멀리건 버튼 비활성화
            HideMulliganPanel(); // 멀리건 패널 숨김

            bool success =
                BattleManager.Instance.MulliganInitialHand(); // 멀리건 처리 결과

            // 멀리건 처리에 실패한 경우 버튼과 패널 복구
            if (!success)
            {
                ShowMulliganPanel(); // 멀리건 패널 다시 표시
                SetMulliganButtons(true); // 멀리건 버튼 다시 활성화
                isProcessing = false; // 버튼 처리 상태 초기화
                return;
            }

            RefreshPlayerHand(); // 새 손패를 일반 손패 UI에 표시
            HideMulliganPanel(); // 멀리건 패널 숨김

            SetMulliganButtons(false); // 멀리건 버튼 비활성화
            isProcessing = false; // 버튼 처리 상태 초기화
        }

        // 멀리건 버튼 활성화 상태 변경
        private void SetMulliganButtons(bool interactable)
        {
            // 유지 버튼이 연결되어 있으면 활성화 상태 변경
            if (keepButton != null)
            {
                keepButton.interactable = interactable; // 유지 버튼 클릭 가능 여부 설정
            }

            // 멀리건 버튼이 연결되어 있으면 활성화 상태 변경
            if (mulliganButton != null)
            {
                mulliganButton.interactable = interactable; // 멀리건 버튼 클릭 가능 여부 설정
            }
        }
    }
}
