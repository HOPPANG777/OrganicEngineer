using System;
using UnityEngine;

//==============================================================================
// Singleton<T>: MonoBehaviour 싱글톤 베이스
//==============================================================================
// - 씬에서 기존 인스턴스 우선 탐색
// - (옵션) 없으면 자동 생성
// - (옵션) DontDestroyOnLoad 유지
// - 종료 중 재생성 방지
//==============================================================================

/// <summary>
/// 동일한 컴포넌트를 하나의 GameObject에 여러 개 붙이는 것을 방지합니다.
/// 싱글톤은 반드시 하나만 존재해야 하므로 중요한 안전장치입니다.
/// </summary>
[DisallowMultipleComponent]
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    //--------------------------------------------------------------------------
    // [1] 내부 상태(static)
    //--------------------------------------------------------------------------

    /// <summary>싱글톤 인스턴스(static).</summary>
    private static T _instance;

    /// <summary>종료(quit) 중 여부.</summary>
    private static bool _isQuitting;

    /// <summary>씬 탐색을 1회만 수행하기 위한 플래그.</summary>
    private static bool _searchedInScene;

    /// <summary>자동 생성 허용 여부(타입별).</summary>
    private static bool _autoCreateIfNotFound = true;

    /// <summary>자동 생성 시 GameObject 이름(타입별).</summary>
    private static string _autoCreatedObjectName = $"[Singleton] {typeof(T).Name}";

    /// <summary>DontDestroyOnLoad 적용 여부(타입별).</summary>
    private static bool _persistAcrossScenes = true;

    /// <summary>중복 생성 방지용 lock.</summary>
    private static readonly object _lock = new object();

    //--------------------------------------------------------------------------
    // [2] 타입별 설정 API (파생 클래스에서 static ctor로 1회 설정)
    //--------------------------------------------------------------------------

    /// <summary>타입별 싱글톤 정책 설정(보통 static ctor에서 1회 호출).</summary>
    protected static void ConfigureSingleton(
        bool allowAutoCreate,
        string autoCreatedObjectName = null,
        bool persistAcrossScenes = true)
    {
        _autoCreateIfNotFound = allowAutoCreate;
        _persistAcrossScenes = persistAcrossScenes;

        if (!string.IsNullOrWhiteSpace(autoCreatedObjectName))
            _autoCreatedObjectName = autoCreatedObjectName;
    }

    //--------------------------------------------------------------------------
    // [3] 외부 API
    //--------------------------------------------------------------------------

    /// <summary>인스턴스 접근자(정책에 따라 씬 탐색/자동 생성).</summary>
    public static T Instance
    {
        get
        {
            // 종료 중에는 새로 만들지 않습니다.
            if (_isQuitting)
            {
                Log($"Instance 요청 무시(종료 중): {typeof(T).Name}");
                return null;
            }
            // 빠른 경로: 이미 있으면 즉시 반환
            if (_instance != null)
                return _instance;
            lock (_lock)
            {
                // 락 이후 다시 확인
                if (_instance != null)
                    return _instance;
                // 1) 씬에서 기존 인스턴스를 딱 한 번 찾아봅니다.
                if (!_searchedInScene)
                {
                    _searchedInScene = true;
                    // Unity 2023+에서는 FindFirstObjectByType이 권장됩니다.
                    // 구버전 호환이 필요하면 FindObjectOfType을 사용할 수 있습니다.
#if UNITY_2023_1_OR_NEWER
                    _instance = UnityEngine.Object.FindFirstObjectByType<T>();
#else
                    _instance = UnityEngine.Object.FindObjectOfType<T>();
#endif
                    if (_instance != null)
                    {
                        Log($"씬에서 기존 인스턴스 발견: {GetHierarchyPath(_instance.gameObject)}");
                        // 타입 설정상 DontDestroyOnLoad가 필요한 경우 보장합니다.
                        // (씬 배치 인스턴스를 사용했는데 DontDestroyOnLoad가 안 걸려있는 경우 대비)
                        if (_persistAcrossScenes)
                            DontDestroyOnLoad(_instance.gameObject);
                        return _instance;
                    }
                }

                // 2) 씬에도 없으면 자동 생성 여부 확인
                // - allowAutoCreate=false인 타입은 여기서 null을 반환하여 “프리팹/씬 배치 강제”를 실현합니다.
                if (!_autoCreateIfNotFound)
                {
                    LogWarning($"Instance 요청 실패(자동 생성 금지): {typeof(T).Name} - 씬/프리팹에 배치되어 있어야 합니다.");
                    return null;
                }

                return CreateAutoInstance();
            }
        }
    }

    /// <summary>
    /// 인스턴스가 이미 존재하는지 여부만 확인합니다(생성/탐색 없음).
    /// - 로직 분기(있으면 사용, 없으면 다른 처리)에 유용합니다.
    /// </summary>
    public static bool HasInstance => _instance != null && !_isQuitting;

    /// <summary>
    /// "가능하면 가져오고, 없으면 false"를 반환하는 안전한 접근 메서드입니다.
    /// - 자동 생성이 싫은 코드 경로에서 특히 유용합니다.
    /// </summary>
    public static bool TryGetInstance(out T value)
    {
        if (_isQuitting)
        {
            value = null;
            return false;
        }
        value = _instance;
        if (value != null) return true;
        // 씬 탐색 1회는 허용
        if (!_searchedInScene)
        {
            _searchedInScene = true;
#if UNITY_2023_1_OR_NEWER
            value = UnityEngine.Object.FindFirstObjectByType<T>();
#else
            value = UnityEngine.Object.FindObjectOfType<T>();
#endif
            _instance = value;
        }
        return value != null;
    }
    //--------------------------------------------------------------------------
    // [4] Unity 생명주기
    //--------------------------------------------------------------------------
    /// <summary>
    /// Awake에서 싱글톤 등록/중복 제거를 수행합니다.
    /// - _instance가 비어있으면: 자신을 등록하고 필요 시 DontDestroyOnLoad
    /// - 이미 다른 인스턴스가 있으면: 자신은 중복이므로 파괴
    /// </summary>
    protected virtual void Awake()
    {
        if (_isQuitting)
        {
            // 종료 중에 생성된 오브젝트면 즉시 정리
            Destroy(gameObject);
            return;
        }
        if (_instance == null)
        {
            _instance = this as T;
            _searchedInScene = true; // 이미 찾았으니 씬 탐색은 불필요
            if (_persistAcrossScenes)
                DontDestroyOnLoad(gameObject);
            Log($"싱글톤 등록 완료: {GetHierarchyPath(gameObject)}");
        }
        else if (_instance != this)
        {
            // 중복 인스턴스 처리: 개발 단계에서 원인 파악이 중요하므로 로그를 남깁니다.
            LogWarning($"중복 싱글톤 감지 -> 파괴: {GetHierarchyPath(gameObject)} / 기존: {GetHierarchyPath(_instance.gameObject)}");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 애플리케이션 종료 시 호출됩니다.
    /// 여기서 플래그를 세팅해두면, 종료 과정에서 Instance가 호출돼도 재생성되지 않습니다.
    /// </summary>
    protected virtual void OnApplicationQuit()
    {
        _isQuitting = true;
    }

    /// <summary>
    /// 오브젝트가 파괴될 때 호출됩니다.
    /// - 본인이 _instance인 경우에만 static 참조를 정리합니다.
    /// - null 체크 후 로그를 남겨 NRE를 방지합니다.
    /// </summary>
    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            Log($"싱글톤 해제(파괴): {GetHierarchyPath(gameObject)}");
            _instance = null;
            // _searchedInScene는 유지: 파괴 후 다시 필요해질 수 있으니 재탐색 허용 여부는 정책에 따라 다릅니다.
            // 여기서는 다음 Instance 호출에서 다시 생성/탐색을 허용하는 쪽으로 둡니다.
        }
    }

    //--------------------------------------------------------------------------
    // [5] 내부 유틸
    //--------------------------------------------------------------------------
    /// <summary>
    /// 씬에 없을 때 자동으로 인스턴스를 생성합니다.
    /// - GameObject 생성
    /// - T 컴포넌트 추가
    /// - 이름 지정
    /// - 필요 시 DontDestroyOnLoad는 Awake에서 처리
    /// </summary>
    private static T CreateAutoInstance()
    {
        var go = new GameObject(_autoCreatedObjectName);
        var created = go.AddComponent<T>();
        // 생성 직후 Awake에서 _instance 등록 + DontDestroyOnLoad 처리됩니다.
        Log($"자동 생성 완료: {GetHierarchyPath(go)}");
        return created;
    }

    /// <summary>
    /// Hierarchy에서 오브젝트의 경로를 만들어 디버깅 가독성을 높입니다.
    /// </summary>
    private static string GetHierarchyPath(GameObject go)
    {
        if (go == null) return "(null)";
        var t = go.transform;
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = $"{t.name}/{path}";
        }
        return path;
    }

    /// <summary>
    /// 로그 출력. 에디터 또는 개발 빌드에서만.
    /// 릴리즈 빌드에서는 불필요한 로그로 성능/보안 이슈가 생길 수 있어 제한합니다.
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private static void Log(string message)
    {
        Debug.Log($"[{typeof(T).Name}] {message}");
    }

    /// <summary>
    /// 경고 로그. 에디터 또는 개발 빌드에서만.
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private static void LogWarning(string message)
    {
        Debug.LogWarning($"[{typeof(T).Name}] {message}");
    }
}