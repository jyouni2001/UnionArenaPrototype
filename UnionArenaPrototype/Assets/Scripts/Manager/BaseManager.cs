using UnityEngine;

namespace Manager
{
    public class BaseManager : MonoBehaviour
    {
        public bool IsInitialized { get; private set; } // 초기화 확인
        
        // 초기화 진입점
        protected virtual void Awake()
        {
            Initialize();
        }
        
        // 초기화 처리
        protected virtual void Initialize()
        {
            IsInitialized = true;
        }
    }
}