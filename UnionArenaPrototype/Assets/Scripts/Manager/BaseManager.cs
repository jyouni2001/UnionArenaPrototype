using UnityEngine;

namespace Manager
{
    // 공통 초기화 상태와 생명 주기 진입점을 제공
    public class BaseManager : MonoBehaviour
    {
        #region 초기화 상태

        // 파생 매니저의 초기화 완료 여부
        public bool IsInitialized { get; private set; }

        #endregion

        #region 생명 주기

        // Unity 생명 주기에서 파생 매니저의 초기화를 시작
        protected virtual void Awake()
        {
            Initialize();
        }

        // 공통 초기화 완료 상태를 기록
        protected virtual void Initialize()
        {
            IsInitialized = true;
        }

        #endregion
    }
}
