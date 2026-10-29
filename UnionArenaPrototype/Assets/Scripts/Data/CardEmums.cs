using UnityEngine;

public class CardEnums
{
    // Json
    public enum JJson
    {
        Error = -1,
        None = 0,
        Cards = 1,
        Cards_Converted = 2
    }
    
    // 카드 종류
    public enum CardType
    {
        Error = -1,
        None = 0,
        ActionPoint = 1,
        Character = 2,
        Option = 3,
        Field = 4,
        Raid = 5
    }

    // 카드 색
    public enum CardColor
    {
        Error = -1,
        None = 0,
        Red = 1,
        Yellow = 2,
        Green = 3,
        Blue = 4,
        Purple = 5
    }
    
    // 카드 상태
    public enum CardState
    {
        Error = -1,
        None = 0,
        Active = 1,
        Rest = 2
    }
    
    // 카드 트리거
    public enum CardTrigger
    {
        Error = -1,
        None = 0,
        
        Final = 1,
        Raid = 2,
        Special = 3,
        
        // 컬러트리거
        Red = 4,
        Yellow = 5,
        Green = 6,
        Blue = 7,
        Purple = 8,
        
        Get = 9,
        Active = 10,
        Draw = 11
        
    }
    
    // 카드 작품 및 소속
    public enum CardFeatureType
    {
        Error = -1,
        None = 0,
        Series = 1, // 작품
        Group = 2, // 소속
        Character = 3 // 캐릭터
    }
    
    // 카드 키워드
    public enum CardKeywordType
    {
        Error = -1,
        None = 0,
        Step = 1,             // 스텝
        Sniping = 2,          // 저격
        DoubleAttack = 3,     // 2회 어택
        DoubleBlock = 4,      // 2회 블록
        Impact = 5,           // 임팩트 X
        ImpactPlusOne = 6,    // 임팩트 +1
        Damage = 7,           // 데미지 X
        DamagePlusOne = 8,    // 데미지 +1
        ImpactNull = 9        // 임팩트 무효
    }
}
