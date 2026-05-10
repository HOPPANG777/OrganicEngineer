using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

// 기능: OptionsScreenController
// 설명: 옵션 화면의 열기/닫기, Apply/Cancel/Reset, 탭 전환, 토글 키 입력을 관리한다.
//==============================================================================
// TODO: OptionsOpen/OptionsApply 컴포넌트 완전 분리, 버튼 이벤트 해제 처리
//
//==============================================================================

public partial class OptionsScreenController : MonoBehaviour
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

    // 옵션 탭 바인딩/새로고침/전환 기능
    private OptionsTab _optionsTab = new OptionsTab();

    // 옵션 화면 토글 단축키 감지 기능
    private OptionsHotkey _optionsHotkey = new OptionsHotkey();

    private SettingsController _settings; // GameRoot 에서 가져온 SettingsController 캐시

    private IOptionsTabController[] _tabs; // 탭 컨트롤러 통합 배열

    private string _originalJson; // Open 시점의 데이터 스냅샷(Cancel 복원용)

    private bool _hooked; // 버튼 이벤트가 이미 연결되었는지 여부

    //==========================================================================
    // [3] Unity 생명주기
    //==========================================================================

    //==============================================================================
    // 기능: OptionsScreenController::Awake
    // 설명: 탭 배열을 구성하고 옵션 패널 초기 숨김 상태를 적용한다.
    //==============================================================================
    // TODO: 탭 배열 구성 로직 별도 컴포넌트화
    //
    //==============================================================================
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

    //==============================================================================
    // 기능: OptionsScreenController::Start
    // 설명: GameRoot 준비 후 SettingsController 와 탭/버튼 이벤트를 연결한다.
    //==============================================================================
    // TODO: GameRoot 초기화 완료 이벤트 기반으로 전환
    //
    //==============================================================================
    private void Start()
    {
        EnsureBound();
    }

    //==========================================================================
    // [4] 바인딩 (한 번만)
    //==========================================================================

    //==============================================================================
    // 기능: OptionsScreenController::EnsureBound
    // 설명: Settings 참조를 확보하고 탭 Bind 및 버튼 이벤트 후킹을 한 번만 수행한다.
    //==============================================================================
    // TODO: 이벤트 중복 연결 검증 강화
    //
    //==============================================================================
    private void EnsureBound()
    {
        if (_hooked) return;

        GameRoot root = FindFirstObjectByType<GameRoot>();
        if (root != null)
        {
            _settings = root.Settings;
        }

        if (_settings == null)
        {
            Debug.LogError("[OptionsScreen] GameRoot.Settings를 찾지 못했습니다. BootScene부터 진입했는지 확인하세요.");
            return;
        }

        // 탭 바인딩 책임은 OptionsTab 으로 분리
        _optionsTab.BindAll(_tabs, _settings);

        if (_applyButton != null)        _applyButton.onClick.AddListener(OnApplyClicked);
        if (_cancelButton != null)       _cancelButton.onClick.AddListener(OnCancelClicked);
        if (_resetButton != null)        _resetButton.onClick.AddListener(OnResetClicked);

        if (_graphicsTabButton != null)  _graphicsTabButton.onClick.AddListener(() => SwitchTab(0));
        if (_audioTabButton != null)     _audioTabButton.onClick.AddListener(() => SwitchTab(1));
        if (_cameraTabButton != null)    _cameraTabButton.onClick.AddListener(() => SwitchTab(2));
        if (_gameplayTabButton != null)  _gameplayTabButton.onClick.AddListener(() => SwitchTab(3));

        _hooked = true;
    }

}
