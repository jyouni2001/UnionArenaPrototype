using System.Collections.Generic;
using UnityEngine;
using Battle;
using Enums;

namespace Manager
{
    // 카드 영역 이동 및 관리
    public class CardZoneManager : BaseManager
    {
        public static CardZoneManager Instance { get; private set; } // 카드 영역 매니저 싱글톤

        protected override void Awake()
        {
            base.Awake();

            if (Instance != null && Instance != this) // 이미 다른 매니저가 존재하는 경우
            {
                Destroy(gameObject); // 현재 오브젝트 삭제
                return;
            }

            Instance = this; // 현재 오브젝트를 대표 인스턴스로 등록
        }

        // 카드를 지정한 영역으로 이동
        public bool MoveCard(
            PlayerGameData playerData,
            CardRuntimeData card,
            BattleEnums.CardZone targetZone,
            BattleEnums.FieldPosition targetFieldPosition = BattleEnums.FieldPosition.None)
        {
            if (playerData == null || card == null)
            {
                return false;
            }

            // 이동할 영역의 리스트 가져오기
            List<CardRuntimeData> targetList = GetZoneList(playerData, targetZone);

            if (targetList == null)
            {
                return false;
            }
            
            // 필드 이동 시 세부 위치 확인
            if (targetZone == BattleEnums.CardZone.Field && targetFieldPosition == BattleEnums.FieldPosition.None)
            {
                return false;
            }

            // 필드가 아닌 영역으로 이동 시 위치 초기화
            if (targetZone != BattleEnums.CardZone.Field)
            {
                targetFieldPosition = BattleEnums.FieldPosition.None;
            }

            // 같은 영역 내에서 위치 변경
            if (card.currentZone == targetZone)
            {
                if (!targetList.Contains(card))
                {
                    return false;
                }

                card.fieldPosition = targetFieldPosition;
                return true;
            }

            // 기존 영역의 리스트 가져오기
            List<CardRuntimeData> currentList = GetZoneList(playerData, card.currentZone);

            if (targetList.Contains(card))
            {
                return false;
            }

            // 기존 영역에서 카드 제거
            if (card.currentZone != BattleEnums.CardZone.None)
            {
                if (currentList == null || !currentList.Remove(card))
                {
                    return false;
                }
            }

            // 새로운 영역에 카드 추가
            targetList.Add(card);

            // 카드 위치 정보 갱신
            card.currentZone = targetZone;
            card.fieldPosition = targetFieldPosition;

            return true;
        }

        // 지정한 영역의 카드 리스트 반환
        private List<CardRuntimeData> GetZoneList(
            PlayerGameData playerData,
            BattleEnums.CardZone zone)
        {
            switch (zone)
            {
                case BattleEnums.CardZone.Deck:
                    return playerData.deck;
                case BattleEnums.CardZone.Hand:
                    return playerData.hand;
                case BattleEnums.CardZone.Security:
                    return playerData.security;
                case BattleEnums.CardZone.Field:
                    return playerData.field;
                case BattleEnums.CardZone.Trash:
                    return playerData.trash;
                case BattleEnums.CardZone.Remove:
                    return playerData.remove;
                default:
                    return null;
            }
        }
    }
}
