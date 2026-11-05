using System;
using UnityEngine;
using UnityEngine.UI;
using Battle;

namespace UI
{
    // 전달받은 에너지 요약을 색상 이미지와 숫자로 표시
    public class EnergyAreaView : MonoBehaviour
    {
        [Serializable]
        private class EnergyColorSlot
        {
            [SerializeField] private CardEnums.CardColor color;
            [SerializeField] private Image backgroundImage;
            [SerializeField] private Text countText;

            [NonSerialized] public int lastCount = int.MinValue;

            public CardEnums.CardColor Color => color;
            public Image BackgroundImage => backgroundImage;
            public Text CountText => countText;
        }

        [Header("색상별 에너지 이미지와 숫자")]
        [SerializeField] private EnergyColorSlot[] energySlots =
            new EnergyColorSlot[5];

        private void Awake()
        {
            HideAllSlots();
        }

        // UIManager가 계산 결과를 전달
        public void SetEnergySummary(EnergySummary summary)
        {
            if (energySlots == null)
            {
                return;
            }

            for (int i = 0; i < energySlots.Length; i++)
            {
                EnergyColorSlot slot = energySlots[i];

                if (slot == null || slot.BackgroundImage == null)
                {
                    continue;
                }

                int count = summary.GetCount(slot.Color);

                if (count == slot.lastCount)
                {
                    continue;
                }

                slot.lastCount = count;

                bool hasEnergy = count > 0;
                slot.BackgroundImage.gameObject.SetActive(hasEnergy);

                if (!hasEnergy)
                {
                    continue;
                }

                slot.BackgroundImage.color = GetEnergyColor(slot.Color);
                slot.BackgroundImage.raycastTarget = false;

                if (slot.CountText != null)
                {
                    slot.CountText.text = count.ToString();
                    slot.CountText.color = GetTextColor(slot.Color);
                    slot.CountText.raycastTarget = false;
                }
            }
        }

        private void HideAllSlots()
        {
            if (energySlots == null)
            {
                return;
            }

            for (int i = 0; i < energySlots.Length; i++)
            {
                EnergyColorSlot slot = energySlots[i];

                if (slot == null)
                {
                    continue;
                }

                slot.lastCount = int.MinValue;

                if (slot.BackgroundImage != null)
                {
                    slot.BackgroundImage.gameObject.SetActive(false);
                    slot.BackgroundImage.raycastTarget = false;
                }

                if (slot.CountText != null)
                {
                    slot.CountText.raycastTarget = false;
                }
            }
        }

        private Color GetEnergyColor(CardEnums.CardColor color)
        {
            switch (color)
            {
                case CardEnums.CardColor.Red:
                    return new Color32(220, 55, 65, 255);
                case CardEnums.CardColor.Yellow:
                    return new Color32(255, 205, 35, 255);
                case CardEnums.CardColor.Green:
                    return new Color32(55, 155, 85, 255);
                case CardEnums.CardColor.Blue:
                    return new Color32(55, 125, 220, 255);
                case CardEnums.CardColor.Purple:
                    return new Color32(145, 75, 190, 255);
                default:
                    return Color.gray;
            }
        }

        private Color GetTextColor(CardEnums.CardColor color)
        {
            return color == CardEnums.CardColor.Yellow
                ? Color.black
                : Color.white;
        }
    }
}