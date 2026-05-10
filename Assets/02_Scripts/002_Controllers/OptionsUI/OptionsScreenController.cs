using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

//==============================================================================
// OptionsScreenController: 옵션 화면 진입점
//==============================================================================
// - Apply / Cancel / Reset 흐름 관리
// - 탭 전환(Graphics/Audio/Camera/Gameplay)
// - Cancel 복원: 열 때 JsonUtility로 스냅샷 → 취소 시 FromJsonOverwrite로 되돌림
// - SettingsController는 GameRoot.Instance.Settings로 자동 획득
//==============================================================================

public class OptionsScreenController : MonoBehaviour
{
    //--------------------------------------------------------------------------
    // [1] Inspector 연결
    //--------------------------------------------------------------------------

    [TabGroup("Refs")]
    [Tooltip("옵션 화면 전체 패널 루트(Show/Hide 대상).")]
    [SerializeField] private GameObject _panelRoot;

    [TabGroup("Refs")]
    [Tooltip("Apply 버튼.")]
    [SerializeField] private Button _applyButton;

    [TabGroup("Refs")]
    [Tooltip("Cancel 버튼(원래 값으로 되돌리고 닫힘).")]
    [SerializeField] private Button _cancelButton;

    [TabGroup("Refs")]
    [Tooltip("Reset 버튼(현재 탭들을 기본값으로 되돌림).")]
    [SerializeField] private Button _resetButton;

    [TabGroup("Tabs")]
    [Tooltip("Graphics 탭 컨트롤러.")]
    [SerializeField] private GraphicsOptionsTabController _graphicsTab;

    [TabGroup("Tabs")]
    [Tooltip("Audio 탭 컨트롤러.")]
    [SerializeField] private AudioOptionsTabController _audioTab;

    [TabGroup("Tabs")]
    [Tooltip("Camera 탭 컨트롤러.")]
    [SerializeField] private CameraOptionsTabController _cameraTab;

    [TabGroup("Tabs")]
    [Tooltip("Gameplay 탭 컨트롤러.")]
    [SerializeField] private GameplayOptionsTabController _gameplayTab;

    [TabGroup("Tab Buttons")]
    [Tooltip("Graphics 탭 전환 버튼.")]
    [SerializeField] private Button _graphicsTabButton;

    [TabGroup("Tab Buttons")]
    [Tooltip("Audio 탭 전환 버튼.")]
    [SerializeField] private Button _audioTabButton;

    [TabGroup("Tab Buttons")]
    [Tooltip("Camera 탭 전환 버튼.")]
    [SerializeField] private Button _cameraTabButton;

    [TabGroup("Tab Buttons")]
    [Tooltip("Gameplay 탭 전환 버튼.")]
    [SerializeField] private Button _gameplayTabButton;

    [TabGroup("Behavior")]
    [Tooltip("Awake 시점에 패널을 자동으로 비활성화할지 여부.")]
    [SerializeField] private bool _hideOnAwake = true;

    [TabGroup("Behavior")]
    [Tooltip("토글 키(예: ESC)를 자동 감지할지 여부.")]
    [SerializeField] private bool _listenForToggleKey = true;

    [TabGroup("Behavior")]
    [Tooltip("자동 감지에 사용할 키. 기본 Escape.")]
    [SerializeField] private KeyCode _toggleKey = KeyCode.Escape;

    //--------------------------------------------------------------------------
    // [2] 상태
    //--------------------------------------------------------------------------

    /// <summary>SettingsController 캐시.</summary>
    private SettingsController _settings;

    /// <summary>탭 컨트롤러 통합 배열.</summary>
    private IOptionsTabController[] _tabs;

    /// <summary>Open 시점의 데이터 스냅샷(Cancel 복원용).</summary>
    private string _originalJson;

    /// <summary>이미 이벤트가 후킹되었는지.</summary>
    private bool _hooked;

    //==========================================================================
    // [3] Unity 생명주기
    //==========================================================================

    /// <summary>탭 바인딩 + 버튼 이벤트 후킹 + 초기 숨김.</summary>
    private void Awake()
    {
        _tabs = new IOptionsTabController[]
        {
            _graphicsTab,
            _audioTab,
            _cameraTab,
            _gameplayTab,
        };

        if (_hideOnAwake && _panelRoot != null) _panelRoot.SetActive(false);
    }

    /// <summary>GameRoot 준비 후 바인딩(가장 안전).</summary>
    private void Start()
    {
        EnsureBound();
    }

    //==========================================================================
    // [4] 바인딩 (한 번만)
    //==========================================================================

    /// <summary>Settings 참조 + 탭 Bind + 버튼 이벤트 후킹.</summary>
    private void EnsureBound()
    {
        if (_hooked) return;

        if (GameRoot.TryGetInstance(out GameRoot root) && root != null)
        {
            _settings = root.Settings;
        }

        if (_settings == null)
        {
            Debug.LogError("[OptionsScreen] GameRoot.Settings를 찾지 못했습니다. BootScene부터 진입했는지 확인하세요.");
            return;
        }

        // 탭 바인딩 책임은 OptionsTab 으로 분리
        OptionsTab.BindAll(_tabs, _settings);

        if (_applyButton != null)        _applyButton.onClick.AddListener(OnApplyClicked);
        if (_cancelButton != null)       _cancelButton.onClick.AddListener(OnCancelClicked);
        if (_resetButton != null)        _resetButton.onClick.AddListener(OnResetClicked);

        if (_graphicsTabButton != null)  _graphicsTabButton.onClick.AddListener(() => SwitchTab(0));
        if (_audioTabButton != null)     _audioTabButton.onClick.AddListener(() => SwitchTab(1));
        if (_cameraTabButton != null)    _cameraTabButton.onClick.AddListener(() => SwitchTab(2));
        if (_gameplayTabButton != null)  _gameplayTabButton.onClick.AddListener(() => SwitchTab(3));

        _hooked = true;
    }

    //==========================================================================
    // [5] 외부 진입점 (Open / Close)
    //==========================================================================

    /*
     * @brief 옵션 화면을 연다. 현재 데이터를 스냅샷한 뒤 첫 탭부터 표시
     */
    public void Open()
    {
        EnsureBound();

        if (_settings == null || _settings.Data == null)
        {
            Debug.LogError("[OptionsScreen] Settings.Data가 비어있어 열 수 없습니다.");
            return;
        }

        // 현재 데이터 스냅샷 (Cancel용)
        _originalJson = JsonUtility.ToJson(_settings.Data);

        // 모든 탭의 UI를 현재 데이터로 동기화
        // 모든 탭 UI 새로고침 책임은 OptionsTab 으로 분리
        OptionsTab.RefreshAll(_tabs);

        SwitchTab(0);

        if (_panelRoot != null) _panelRoot.SetActive(true);
    }

    /// <summary>외부에서 강제로 닫기(저장/취소 없이).</summary>
    public void Close()
    {
        if (_panelRoot != null) _panelRoot.SetActive(false);
    }

    /// <summary>현재 옵션 화면이 열려있는지.</summary>
    public bool IsOpen => _panelRoot != null && _panelRoot.activeSelf;

    /// <summary>Open / CancelAndClose 토글. ESC 키 또는 외부 버튼에서 호출.</summary>
    public void Toggle()
    {
        if (IsOpen) CancelAndClose();
        else        Open();
    }

    /// <summary>저장 후 닫기(Apply 버튼과 동일).</summary>
    public void ConfirmAndClose() => OnApplyClicked();

    /// <summary>되돌리고 닫기(Cancel 버튼과 동일).</summary>
    public void CancelAndClose() => OnCancelClicked();

    //==========================================================================
    // [6] 버튼 핸들러
    //==========================================================================

    /// <summary>Apply: 현재 _data를 디스크에 저장하고 화면을 닫는다.</summary>
    private void OnApplyClicked()
    {
        if (_settings == null) { Close(); return; }

        // 미리보기 단계에서 이미 _data와 시스템에 반영되어 있으므로, 저장만 수행
        _settings.SaveAsync(this.GetCancellationTokenOnDestroy()).Forget();

        Close();
    }

    /// <summary>Cancel: 스냅샷으로 데이터 복원 후 시스템 재적용 + 닫기.</summary>
    private void OnCancelClicked()
    {
        if (_settings != null && _settings.Data != null && !string.IsNullOrEmpty(_originalJson))
        {
            // 인스턴스 교체 없이 기존 _data의 필드를 원래 값으로 덮어쓰기
            JsonUtility.FromJsonOverwrite(_originalJson, _settings.Data);
            _settings.Apply();
        }

        Close();
    }

    /// <summary>Reset: 기본값으로 되돌리고(즉시 적용) 모든 탭 UI 새로고침.</summary>
    private void OnResetClicked()
    {
        if (_settings == null) return;

        _settings.ResetToDefault();

        // Reset 후 모든 탭 UI 새로고침
        OptionsTab.RefreshAll(_tabs);
    }

    //==========================================================================
    // [6-1] ESC 토글 (선택)
    //==========================================================================

    /// <summary>토글 키 감시. _listenForToggleKey가 true일 때만 동작.</summary>
    private void Update()
    {
        if (!_listenForToggleKey) return;
        if (!IsToggleKeyPressedThisFrame()) return;

        // Settings가 아직 준비 안 됐으면 무시(BootScene 직후 등)
        if (_settings == null || _settings.Data == null) return;

        Toggle();
    }

    /*
     * @brief 활성 입력 시스템에 맞춰 _toggleKey가 이번 프레임에 눌렸는지 검사
     * @return 눌렸으면 true
     * @note Legacy Input Manager / New Input System 모두 지원
     */
    private bool IsToggleKeyPressedThisFrame()
    {
        // 입력 시스템별 키 감지는 OptionsHotkey 로 분리
        return OptionsHotkey.IsPressedThisFrame(_toggleKey);
    }

    //==========================================================================
    // [7] 탭 전환
    //==========================================================================

    /// <summary>탭 인덱스에 해당하는 패널만 활성화.</summary>
    private void SwitchTab(int index)
    {
        // 탭 전환 책임은 OptionsTab 으로 분리
        OptionsTab.Switch(_tabs, index);
    }
}
