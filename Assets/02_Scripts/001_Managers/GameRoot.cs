using UnityEngine;

//==============================================================================
// GameRoot: 프로젝트 전역 루트(단일 싱글턴)
//==============================================================================
// - 전역 접근은 `GameRoot` 하나로만 통일
// - GameRoot는 자동 생성 금지(프리팹/씬 배치 강제)
// - 전역 초기화는 BootScene의 BootLoader가 명시적으로 호출
//==============================================================================

public class GameRoot : Singleton<GameRoot>
{
    //--------------------------------------------------------------------------
    // [0] 싱글톤 정책(타입별 설정)
    //--------------------------------------------------------------------------

    /// <summary>GameRoot는 자동 생성 금지(프리팹/씬 배치 강제).</summary>
    static GameRoot()
    {
        ConfigureSingleton(
            allowAutoCreate: false,
            autoCreatedObjectName: "[GameRoot]",
            persistAcrossScenes: true);
    }

    //--------------------------------------------------------------------------
    // [1] Inspector 연결 (프리팹 구성 기반)
    //--------------------------------------------------------------------------

    /// <summary>컨트롤러/팩토리를 프리팹으로 붙여두는 루트.</summary>
    [SerializeField]
    private Transform _controllersRoot;

    //--------------------------------------------------------------------------
    // [2] 초기화 상태
    //--------------------------------------------------------------------------

    /// <summary>전역 초기화가 끝났는지 여부(1회 보장).</summary>
    [SerializeField]
    private bool _isInitialized;

    /// <summary>초기화 완료 여부(읽기 전용).</summary>
    public bool IsInitialized => _isInitialized;

    //--------------------------------------------------------------------------
    // [3] BootLoader가 호출하는 초기화 진입점
    //--------------------------------------------------------------------------

    /// <summary>BootLoader가 호출하는 전역 초기화(안전하게 1회만 동작).</summary>
    public void InitializeIfNeeded()
    {
        if (_isInitialized)
            return;

        _isInitialized = true;

        // 환경 설정
        Application.targetFrameRate = 60;

        if (_controllersRoot == null)
        {
            Debug.LogWarning("[GameRoot] ControllersRoot가 없습니다.");
        }
    }

    //--------------------------------------------------------------------------
    // [4] Unity 생명주기
    //--------------------------------------------------------------------------

    /// <summary>Start에 초기화 로직을 두지 않습니다.</summary>
    private void Start()
    {
        // 의도적으로 비워둡니다.
    }
}

