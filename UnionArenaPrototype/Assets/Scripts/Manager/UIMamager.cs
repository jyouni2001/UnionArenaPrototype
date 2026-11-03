
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Battle;

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

        [Header("멀리건 버튼")]
        [SerializeField] private Button keepButton; // 손패 유지 버튼
        [SerializeField] private Button mulliganButton; // 멀리건 버튼

        private bool isProcessing = false; // 버튼 중복 입력 방지

        // 싱글톤 등록 및 기본 초기화
        protected override void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            base.Awake();
        }

        // UI 매니저 초기화
        protected override void Initialize()
        {
            isProcessing = false;

            base.Initialize();
        }

        // 버튼 이벤트 등록 및 최초 손패 준비 대기
        private void Start()
        {
            if (keepButton != null)
            {
                keepButton.onClick.AddListener(KeepInitialHand);
            }

            if (mulliganButton != null)
            {
                mulliganButton.onClick.AddListener(MulliganInitialHand);
            }

            StartCoroutine(WaitForInitialHand());
        }

        // 버튼 이벤트 및 싱글톤 해제
        private void OnDestroy()
        {
            if (keepButton != null)
            {
                keepButton.onClick.RemoveListener(KeepInitialHand);
            }

            if (mulliganButton != null)
            {
                mulliganButton.onClick.RemoveListener(MulliganInitialHand);
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        // 최초 손패 준비 완료 대기
        private IEnumerator WaitForInitialHand()
        {
            float waitTime = 0f; // 대기 시간

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
    }
}
