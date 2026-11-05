using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Battle;
using Enums;

namespace Manager
{
    // 게임 화면의 UI 참조, 이벤트 연결, 화면 갱신을 조율
    public class UIManager : BaseManager
    {
        #region 싱글톤 및 UI 참조

        public static UIManager Instance { get; private set; } // UI 매니저 싱글톤

        [Header("멀리건 UI")]
        [SerializeField] private GameObject mulliganPanel; // 멀리건 패널
        [SerializeField] private UI.HandView mulliganHandView; // 멀리건 손패 UI

        [Header("일반 손패 UI")]
        [SerializeField] private UI.HandView playerHandView; // 내 손패 UI

        [Header("플레이어 프론트라인 UI")]
        [SerializeField] private UI.FieldSlotView[] playerFrontLineSlots = new UI.FieldSlotView[4]; // 내 프론트라인 슬롯

        [Header("플레이어 에너지라인 UI")]
        [SerializeField] private UI.FieldSlotView[] playerEnergyLineSlots = new UI.FieldSlotView[4]; // 내 에너지라인 슬롯

        [Header("상대 프론트라인 UI")]
        [SerializeField] private UI.FieldSlotView[] opponentFrontLineSlots = new UI.FieldSlotView[4]; // 상대 프론트라인 슬롯

        [Header("상대 에너지라인 UI")]
        [SerializeField] private UI.FieldSlotView[] opponentEnergyLineSlots = new UI.FieldSlotView[4]; // 상대 에너지라인 슬롯

        [Header("플레이어 에너지 표시 UI")]
        [SerializeField] private UI.EnergyAreaView playerEnergyAreaView; // 내 에너지 표시 UI

        [Header("내 시큐리티 UI")]
        [SerializeField] private UI.SecurityAreaView playerSecurityAreaView; // 내 시큐리티 카드 UI

        [Header("상대 시큐리티 UI")]
        [SerializeField] private UI.SecurityAreaView opponentSecurityAreaView; // 상대 시큐리티 카드 UI

        [Header("블록 선택 UI")]
        [SerializeField] private UI.BlockChoiceView blockChoiceView; // 블록 카드 선택 패널

        [Header("게임 종료 UI")]
        [SerializeField] private GameObject gameOverPanel; // 게임 종료 패널
        [SerializeField] private Text gameOverText; // 승패 결과 텍스트

        [Header("멀리건 버튼")]
        [SerializeField] private Button keepButton; // 손패 유지 버튼
        [SerializeField] private Button mulliganButton; // 멀리건 버튼

        [Header("턴 버튼")]
        [SerializeField] private Button endTurnButton; // 턴 종료 버튼

        private bool isProcessing; // 버튼 입력 중복 처리 방지
        private Coroutine playerRefreshCoroutine; // 내 UI 갱신 코루틴
        private Coroutine opponentRefreshCoroutine; // 상대 필드 갱신 코루틴

        #endregion

        #region 생명 주기 및 이벤트 연결

        // 싱글톤을 등록하고 공통 초기화를 시작
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

        // 버튼 입력 처리 상태를 초기화
        protected override void Initialize()
        {
            isProcessing = false;
            base.Initialize();
        }

        // 버튼과 게임 이벤트를 연결
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

            if (endTurnButton != null)
            {
                endTurnButton.onClick.AddListener(HandleEndTurnButtonClicked);
            }

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }

            if (CardZoneManager.Instance != null)
            {
                CardZoneManager.Instance.OnZoneChanged += HandleZoneChanged;
            }
            else
            {
                Debug.LogError("UIManager: CardZoneManager를 찾을 수 없습니다.");
            }

            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.OnBlockChoiceRequested += HandleBlockChoiceRequested;
                BattleManager.Instance.OnCombatFieldChanged += HandleCombatFieldChanged;
                BattleManager.Instance.OnGameEnded += HandleGameEnded;
            }

            StartCoroutine(WaitForInitialHand());
        }

        // 버튼과 게임 이벤트 구독을 해제
        private void OnDestroy()
        {
            if (playerRefreshCoroutine != null)
            {
                StopCoroutine(playerRefreshCoroutine);
                playerRefreshCoroutine = null;
            }

            if (opponentRefreshCoroutine != null)
            {
                StopCoroutine(opponentRefreshCoroutine);
                opponentRefreshCoroutine = null;
            }

            if (keepButton != null)
            {
                keepButton.onClick.RemoveListener(KeepInitialHand);
            }

            if (mulliganButton != null)
            {
                mulliganButton.onClick.RemoveListener(MulliganInitialHand);
            }

            if (endTurnButton != null)
            {
                endTurnButton.onClick.RemoveListener(HandleEndTurnButtonClicked);
            }

            if (CardZoneManager.Instance != null)
            {
                CardZoneManager.Instance.OnZoneChanged -= HandleZoneChanged;
            }

            if (BattleManager.Instance != null)
            {
                BattleManager.Instance.OnBlockChoiceRequested -= HandleBlockChoiceRequested;
                BattleManager.Instance.OnCombatFieldChanged -= HandleCombatFieldChanged;
                BattleManager.Instance.OnGameEnded -= HandleGameEnded;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion

        #region 버튼 및 게임 이벤트 처리

        // 턴 종료 요청을 BattleManager에 전달
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
            RefreshOpponentField();
            RefreshSecurityViews();
        }

        // 카드 영역 변경에 따라 관련 UI 갱신을 예약
        private void HandleZoneChanged(PlayerGameData playerData, BattleEnums.CardZone changedZone)
        {
            if (BattleManager.Instance == null || playerData == null)
            {
                return;
            }

            bool isLocalPlayer = playerData == BattleManager.Instance.LocalPlayerData;
            bool isOpponentPlayer = playerData == BattleManager.Instance.OpponentPlayerData;

            if ((changedZone == BattleEnums.CardZone.Security || changedZone == BattleEnums.CardZone.Trash) &&
                (isLocalPlayer || isOpponentPlayer))
            {
                RefreshSecurityViews();
            }

            if (isLocalPlayer && (changedZone == BattleEnums.CardZone.Hand || changedZone == BattleEnums.CardZone.Field))
            {
                SchedulePlayerUIRefresh();
            }

            if (isOpponentPlayer && changedZone == BattleEnums.CardZone.Field)
            {
                ScheduleOpponentFieldRefresh();
            }
        }

        // 전투 중 카드 상태가 바뀌면 해당 플레이어 필드를 갱신
        private void HandleCombatFieldChanged(PlayerGameData playerData)
        {
            if (BattleManager.Instance == null || playerData == null)
            {
                return;
            }

            if (playerData == BattleManager.Instance.LocalPlayerData)
            {
                RefreshPlayerField();
            }
            else if (playerData == BattleManager.Instance.OpponentPlayerData)
            {
                RefreshOpponentField();
            }
        }

        // 게임이 끝나면 승패 패널을 표시
        private void HandleGameEnded(PlayerGameData winner, PlayerGameData loser)
        {
            if (BattleManager.Instance == null)
            {
                return;
            }

            bool localPlayerWon = winner == BattleManager.Instance.LocalPlayerData;

            if (gameOverText != null)
            {
                gameOverText.text = localPlayerWon ? "승리!" : "패배";
            }

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }

            if (endTurnButton != null)
            {
                endTurnButton.interactable = false;
            }

            if (blockChoiceView != null)
            {
                blockChoiceView.Hide();
            }

            Debug.Log(localPlayerWon ? "UIManager: 게임 승리 화면을 표시합니다." : "UIManager: 게임 패배 화면을 표시합니다.");
        }

        // 상대 공격에 대응할 블록 선택 UI를 표시
        private void HandleBlockChoiceRequested(CardRuntimeData attacker)
        {
            if (BattleManager.Instance == null)
            {
                return;
            }

            List<CardRuntimeData> blockers = BattleManager.Instance.GetActiveLocalBlockers();

            if (blockChoiceView == null || blockers.Count == 0)
            {
                HandlePassBlockChoice();
                return;
            }

            blockChoiceView.Show(attacker, blockers, HandleBlockerSelected, HandlePassBlockChoice);
        }

        // 선택한 카드로 공격을 블록하고 결과를 화면에 반영
        private void HandleBlockerSelected(CardRuntimeData blocker)
        {
            if (BattleManager.Instance == null)
            {
                return;
            }

            if (!BattleManager.Instance.TryResolveLocalBlock(blocker, out BattleCombatService.Resolution resolution, out string errorMessage))
            {
                Debug.LogWarning("UIManager: 블록 처리 실패. " + errorMessage);
                return;
            }

            if (blockChoiceView != null)
            {
                blockChoiceView.Hide();
            }

            RefreshPlayerField();
            RefreshOpponentField();
            Debug.Log($"UIManager: 블록 처리 완료. 결과 = {resolution.Type}");
        }

        // 블록을 포기하고 시큐리티 공격을 처리
        private void HandlePassBlockChoice()
        {
            if (BattleManager.Instance == null)
            {
                return;
            }

            if (!BattleManager.Instance.TryPassLocalBlock(out BattleCombatService.Resolution resolution,
                    out PlayerGameData securityOwner, out CardRuntimeData revealedSecurityCard, out string errorMessage))
            {
                Debug.LogWarning("UIManager: 공격 통과 처리 실패. " + errorMessage);
                return;
            }

            if (blockChoiceView != null)
            {
                blockChoiceView.Hide();
            }

            if (revealedSecurityCard != null && !TryRevealSecurityCard(securityOwner, revealedSecurityCard))
            {
                Debug.LogWarning("UIManager: 시큐리티 공개 UI가 없어 바로 트래시로 이동합니다.");
                BattleManager.Instance.MoveRevealedSecurityToTrash(securityOwner, revealedSecurityCard);
            }

            RefreshSecurityViews();
        }

        // 다음 프레임에 내 손패와 필드 UI 갱신을 예약
        private void SchedulePlayerUIRefresh()
        {
            if (playerRefreshCoroutine == null)
            {
                playerRefreshCoroutine = StartCoroutine(RefreshPlayerUIAfterFrame());
            }
        }

        // 카드 이동 처리가 끝난 뒤 내 UI를 갱신
        private IEnumerator RefreshPlayerUIAfterFrame()
        {
            yield return null;
            playerRefreshCoroutine = null;

            if (BattleManager.Instance == null || BattleManager.Instance.LocalPlayerData == null)
            {
                yield break;
            }

            RefreshPlayerHand();
            RefreshPlayerField();
        }

        // 다음 프레임에 상대 필드 UI 갱신을 예약
        private void ScheduleOpponentFieldRefresh()
        {
            if (opponentRefreshCoroutine == null)
            {
                opponentRefreshCoroutine = StartCoroutine(RefreshOpponentFieldAfterFrame());
            }
        }

        // 카드 이동 처리가 끝난 뒤 상대 필드 UI를 갱신
        private IEnumerator RefreshOpponentFieldAfterFrame()
        {
            yield return null;
            opponentRefreshCoroutine = null;
            RefreshOpponentField();
        }

        // 최초 손패 선택 상태가 준비될 때까지 대기
        private IEnumerator WaitForInitialHand()
        {
            float waitTime = 0f;

            while (waitTime < 10f)
            {
                if (BattleManager.Instance != null && BattleManager.Instance.IsWaitingForInitialHandChoice)
                {
                    RefreshMulliganHand();
                    RefreshSecurityViews();
                    ShowMulliganPanel();
                    yield break;
                }

                yield return null;
                waitTime += Time.unscaledDeltaTime;
            }

            Debug.LogError("UIManager: 최초 손패 준비를 확인하지 못했습니다.");
        }

        #endregion

        #region 손패 및 필드 UI 갱신

        // 현재 손패 데이터를 멀리건 UI에 표시
        public void RefreshMulliganHand()
        {
            if (BattleManager.Instance == null || BattleManager.Instance.LocalPlayerData == null || mulliganHandView == null)
            {
                Debug.LogError("UIManager: 멀리건 손패 UI 연결을 확인하세요.");
                return;
            }

            mulliganHandView.SetCards(BattleManager.Instance.LocalPlayerData.hand);
        }

        // 현재 손패 데이터를 일반 손패 UI에 표시
        public void RefreshPlayerHand()
        {
            if (BattleManager.Instance == null || BattleManager.Instance.LocalPlayerData == null || playerHandView == null)
            {
                Debug.LogError("UIManager: 일반 손패 UI 연결을 확인하세요.");
                return;
            }

            playerHandView.SetCards(BattleManager.Instance.LocalPlayerData.hand);
        }

        // 내 필드 데이터를 프론트라인과 에너지라인 UI에 표시
        public void RefreshPlayerField()
        {
            if (BattleManager.Instance == null || BattleManager.Instance.LocalPlayerData == null)
            {
                return;
            }

            RefreshFieldSlots(BattleManager.Instance.LocalPlayerData, playerFrontLineSlots, playerEnergyLineSlots);
            RefreshPlayerEnergy();
        }

        // 상대 필드 데이터를 프론트라인과 에너지라인 UI에 표시
        public void RefreshOpponentField()
        {
            if (BattleManager.Instance == null || BattleManager.Instance.OpponentPlayerData == null)
            {
                return;
            }

            RefreshFieldSlots(BattleManager.Instance.OpponentPlayerData, opponentFrontLineSlots, opponentEnergyLineSlots);
        }

        // 플레이어 필드 데이터를 슬롯 UI에 표시
        private void RefreshFieldSlots(PlayerGameData playerData, UI.FieldSlotView[] frontLineSlots, UI.FieldSlotView[] energyLineSlots)
        {
            ClearFieldSlots(frontLineSlots);
            ClearFieldSlots(energyLineSlots);

            if (playerData.field == null)
            {
                return;
            }

            for (int i = 0; i < playerData.field.Count; i++)
            {
                CardRuntimeData card = playerData.field[i];

                if (card == null)
                {
                    continue;
                }

                if (card.fieldPosition == BattleEnums.FieldPosition.FrontLine)
                {
                    ShowFieldCard(frontLineSlots, card.fieldSlotIndex, card);
                }
                else if (card.fieldPosition == BattleEnums.FieldPosition.EnergyLine)
                {
                    ShowFieldCard(energyLineSlots, card.fieldSlotIndex, card);
                }
            }
        }

        // 카드 소유자에 맞는 시큐리티 뷰에 공개 요청을 전달
        public bool TryRevealSecurityCard(PlayerGameData securityOwner, CardRuntimeData securityCard)
        {
            if (BattleManager.Instance == null || securityOwner == null || securityCard == null)
            {
                return false;
            }

            if (securityOwner == BattleManager.Instance.LocalPlayerData && playerSecurityAreaView != null)
            {
                return playerSecurityAreaView.TryRevealSecurityCard(securityOwner, securityCard);
            }

            if (securityOwner == BattleManager.Instance.OpponentPlayerData && opponentSecurityAreaView != null)
            {
                return opponentSecurityAreaView.TryRevealSecurityCard(securityOwner, securityCard);
            }

            return false;
        }

        // 양쪽 플레이어의 시큐리티 수에 맞춰 카드 UI 갱신
        public void RefreshSecurityViews()
        {
            if (BattleManager.Instance == null)
            {
                return;
            }

            PlayerGameData localPlayer = BattleManager.Instance.LocalPlayerData;
            PlayerGameData opponentPlayer = BattleManager.Instance.OpponentPlayerData;

            if (playerSecurityAreaView != null && localPlayer != null)
            {
                playerSecurityAreaView.SetSecurityCards(localPlayer.security);
            }

            if (opponentSecurityAreaView != null && opponentPlayer != null)
            {
                opponentSecurityAreaView.SetSecurityCards(opponentPlayer.security);
            }
        }

        // 에너지 계산 결과를 에너지 표시 UI에 전달
        private void RefreshPlayerEnergy()
        {
            if (playerEnergyAreaView == null)
            {
                return;
            }

            PlayerGameData playerData = BattleManager.Instance != null ? BattleManager.Instance.LocalPlayerData : null;
            EnergySummary summary = EnergyRuleService.GetEnergySummary(playerData);
            playerEnergyAreaView.SetEnergySummary(summary);
        }

        // 필드 슬롯 카드 UI를 모두 비활성화
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

        // 지정 슬롯에 필드 카드를 표시
        private void ShowFieldCard(UI.FieldSlotView[] slots, int slotIndex, CardRuntimeData card)
        {
            if (slots == null)
            {
                return;
            }

            if (slotIndex < 0 || slotIndex >= slots.Length)
            {
                Debug.LogWarning($"UIManager: 잘못된 필드 슬롯 번호입니다. SlotIndex = {slotIndex}");
                return;
            }

            if (slots[slotIndex] == null)
            {
                Debug.LogWarning($"UIManager: 필드 슬롯 UI가 연결되지 않았습니다. SlotIndex = {slotIndex}");
                return;
            }

            slots[slotIndex].ShowCard(card);
        }

        #endregion

        #region 최초 손패 선택 UI

        // 멀리건 패널을 표시
        public void ShowMulliganPanel()
        {
            if (mulliganPanel != null)
            {
                mulliganPanel.SetActive(true);
            }
        }

        // 멀리건 패널을 숨김
        public void HideMulliganPanel()
        {
            if (mulliganPanel != null)
            {
                mulliganPanel.SetActive(false);
            }
        }

        // 최초 손패 유지 요청을 처리
        public void KeepInitialHand()
        {
            if (isProcessing || BattleManager.Instance == null)
            {
                return;
            }

            isProcessing = true;
            SetMulliganButtons(false);
            HideMulliganPanel();

            if (!BattleManager.Instance.KeepInitialHand())
            {
                ShowMulliganPanel();
                SetMulliganButtons(true);
                isProcessing = false;
                return;
            }

            RefreshPlayerHand();
            RefreshPlayerField();
            RefreshOpponentField();
            RefreshSecurityViews();
            isProcessing = false;
        }

        // 최초 손패 멀리건 요청을 처리
        public void MulliganInitialHand()
        {
            if (isProcessing || BattleManager.Instance == null)
            {
                return;
            }

            isProcessing = true;
            SetMulliganButtons(false);
            HideMulliganPanel();

            if (!BattleManager.Instance.MulliganInitialHand())
            {
                ShowMulliganPanel();
                SetMulliganButtons(true);
                isProcessing = false;
                return;
            }

            RefreshPlayerHand();
            RefreshPlayerField();
            RefreshOpponentField();
            RefreshSecurityViews();
            HideMulliganPanel();
            SetMulliganButtons(false);
            isProcessing = false;
        }

        // 멀리건 버튼 입력 가능 여부를 설정
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