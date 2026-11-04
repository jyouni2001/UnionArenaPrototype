using UnityEngine;
using Battle;
using Manager;

namespace UI
{
    // 플레이어 AP 상태를 AP 카드 이미지에 표시
    public class APAreaView : MonoBehaviour
    {
        [Header("표시할 플레이어 영역")]
        [SerializeField] private bool isLocalPlayerArea = true;

        [Header("AP 카드 이미지 3장 (왼쪽부터 순서대로)")]
        [SerializeField] private GameObject[] apCardObjects =
            new GameObject[3];

        private PlayerGameData _lastPlayerData;
        private int _lastAPCardCount = int.MinValue;
        private int _lastActiveAP = int.MinValue;

        private void OnEnable()
        {
            // 활성화될 때 다음 Update에서 화면을 다시 갱신
            _lastPlayerData = null;
            _lastAPCardCount = int.MinValue;
            _lastActiveAP = int.MinValue;
        }

        private void Update()
        {
            if (BattleManager.Instance == null)
            {
                ShowNoAP();
                return;
            }

            PlayerGameData playerData = isLocalPlayerArea
                ? BattleManager.Instance.LocalPlayerData
                : BattleManager.Instance.OpponentPlayerData;

            if (playerData == null)
            {
                ShowNoAP();
                return;
            }

            int cardObjectCount =
                apCardObjects != null ? apCardObjects.Length : 0;

            int totalAP = Mathf.Clamp(
                playerData.apCardCount,
                0,
                cardObjectCount);

            int activeAP = Mathf.Clamp(
                playerData.activeAP,
                0,
                totalAP);

            // AP 데이터가 바뀌지 않았으면 UI 갱신 생략
            if (playerData == _lastPlayerData &&
                totalAP == _lastAPCardCount &&
                activeAP == _lastActiveAP)
            {
                return;
            }

            _lastPlayerData = playerData;
            _lastAPCardCount = totalAP;
            _lastActiveAP = activeAP;

            RefreshAPCards(totalAP, activeAP);
        }

        // AP 개수와 상태에 맞춰 카드 표시 및 회전
        private void RefreshAPCards(int totalAP, int activeAP)
        {
            if (apCardObjects == null)
            {
                return;
            }

            for (int i = 0; i < apCardObjects.Length; i++)
            {
                GameObject apCard = apCardObjects[i];

                if (apCard == null)
                {
                    continue;
                }

                bool shouldBeVisible = i < totalAP;
                apCard.SetActive(shouldBeVisible);

                if (!shouldBeVisible)
                {
                    continue;
                }

                RectTransform cardRect =
                    apCard.transform as RectTransform;

                if (cardRect == null)
                {
                    Debug.LogWarning(
                        $"{gameObject.name}: AP 카드에 RectTransform이 없습니다.");
                    continue;
                }

                CardEnums.CardState cardState =
                    i < activeAP
                        ? CardEnums.CardState.Active
                        : CardEnums.CardState.Rest;

                switch (cardState)
                {
                    case CardEnums.CardState.Active:
                        cardRect.localRotation =
                            Quaternion.Euler(0f, 0f, 0f);
                        break;

                    case CardEnums.CardState.Rest:
                        cardRect.localRotation =
                            Quaternion.Euler(0f, 0f, 90f);
                        break;
                }
            }
        }

        // 플레이어 데이터가 없으면 AP 카드를 모두 숨김
        private void ShowNoAP()
        {
            if (apCardObjects == null)
            {
                return;
            }

            if (_lastPlayerData == null &&
                _lastAPCardCount == 0 &&
                _lastActiveAP == 0)
            {
                return;
            }

            _lastPlayerData = null;
            _lastAPCardCount = 0;
            _lastActiveAP = 0;

            for (int i = 0; i < apCardObjects.Length; i++)
            {
                if (apCardObjects[i] != null)
                {
                    apCardObjects[i].SetActive(false);
                }
            }
        }
    }
}