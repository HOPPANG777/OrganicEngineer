using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//==============================================================================
// 기능: GraphicsOptionsTabController
// 설명: 그래픽 옵션 탭의 드롭다운/토글/슬라이더 UI를 SettingsData.Graphics 와 연결한다.
//==============================================================================
// TODO: 그래픽 프리셋, 적용 전 확인 팝업, 해상도 변경 롤백 타이머
//
//==============================================================================

public class GraphicsOptionsTabController : MonoBehaviour, IOptionsTabController
{
    //--------------------------------------------------------------------------
    // [1] 패널
    //--------------------------------------------------------------------------

    [TabGroup("Panel")]
    [Tooltip("이 탭의 활성/비활성을 토글하는 루트 GameObject.")]
    [SerializeField] private GameObject _panelRoot;

    //--------------------------------------------------------------------------
    // [2] 컨트롤 (모두 선택적 — 인스펙터에서 비워두면 해당 컨트롤은 무시)
    //--------------------------------------------------------------------------

    [TabGroup("Controls")]
    [Tooltip("Quality Level 드롭다운(QualitySettings.names로 채워짐).")]
    [SerializeField] private TMP_Dropdown _qualityDropdown;

    [TabGroup("Controls")]
    [Tooltip("해상도 드롭다운(Screen.resolutions로 채워짐).")]
    [SerializeField] private TMP_Dropdown _resolutionDropdown;

    [TabGroup("Controls")]
    [Tooltip("화면 모드 드롭다운(WINDOWED/BORDERLESS/EXCLUSIVE).")]
    [SerializeField] private TMP_Dropdown _screenModeDropdown;

    [TabGroup("Controls")]
    [Tooltip("목표 프레임레이트 드롭다운(30/60/120/144/Unlimited).")]
    [SerializeField] private TMP_Dropdown _frameRateDropdown;

    [TabGroup("Controls")]
    [Tooltip("VSync 토글.")]
    [SerializeField] private Toggle _vsyncToggle;

    [TabGroup("Controls")]
    [Tooltip("안티에일리어싱 드롭다운.")]
    [SerializeField] private TMP_Dropdown _antiAliasingDropdown;

    [TabGroup("Controls")]
    [Tooltip("그림자 품질 드롭다운.")]
    [SerializeField] private TMP_Dropdown _shadowDropdown;

    [TabGroup("Controls")]
    [Tooltip("텍스처 품질 드롭다운.")]
    [SerializeField] private TMP_Dropdown _textureDropdown;

    [TabGroup("Controls")]
    [Tooltip("밝기 슬라이더.")]
    [SerializeField] private Slider _brightnessSlider;

    [TabGroup("Controls")]
    [Tooltip("밝기 값 라벨(선택).")]
    [SerializeField] private TextMeshProUGUI _brightnessValueLabel;

    //--------------------------------------------------------------------------
    // [3] 상태
    //--------------------------------------------------------------------------

    private SettingsController _settings; // 옵션 데이터를 읽고 적용할 SettingsController 참조

    private bool _hooked; // UI 이벤트를 이미 연결했는지 여부

    //--------------------------------------------------------------------------
    // [3-1] 프레임레이트 드롭다운 매핑
    //--------------------------------------------------------------------------

    // 드롭다운 인덱스에 대응되는 실제 목표 프레임레이트 값(-1 = 무제한)
    private readonly int[] _frameRateValues = { 30, 60, 120, 144, -1 };

    // 드롭다운에 표시되는 프레임레이트 라벨
    private readonly string[] _frameRateLabels = { "30 FPS", "60 FPS", "120 FPS", "144 FPS", "Unlimited" };

    //==============================================================================
    // 기능: GraphicsOptionsTabController::Bind
    // 설명: SettingsController 를 캐시하고 그래픽 옵션 UI 이벤트를 한 번만 연결한다.
    //==============================================================================
    // TODO: OnDestroy 이벤트 해제
    //
    //==============================================================================
    // controller: 옵션 화면에서 주입한 SettingsController
    public void Bind(SettingsController controller)
    {
        _settings = controller;
        if (_hooked) return;
        HookEvents();
        _hooked = true;
    }

    //==============================================================================
    // 기능: GraphicsOptionsTabController::Refresh
    // 설명: 현재 GraphicsSetting 값을 각 그래픽 옵션 UI 컨트롤에 다시 표시한다.
    //==============================================================================
    // TODO: 드롭다운 옵션 재생성을 최초 1회로 제한
    //
    //==============================================================================
    public void Refresh()
    {
        if (_settings == null || _settings.Data == null || _settings.Data.GraphicsSetting == null) return;

        SettingsData.Graphics g = _settings.Data.GraphicsSetting;

        PopulateQualityOptions();
        PopulateResolutionOptions();
        PopulateEnumOptions(_screenModeDropdown,    new string[] { "창모드", "테두리 없음", "전체화면" });
        PopulateEnumOptions(_antiAliasingDropdown,  new string[] { "Off", "FXAA", "SMAA", "MSAA 2x", "MSAA 4x", "MSAA 8x" });
        PopulateEnumOptions(_shadowDropdown,        new string[] { "Off", "Low", "Medium", "High" });
        PopulateEnumOptions(_textureDropdown,       new string[] { "Low", "Medium", "High", "Ultra" });
        PopulateFrameRateOptions();

        if (_qualityDropdown != null)      _qualityDropdown.SetValueWithoutNotify(g.QualityLevel);
        if (_resolutionDropdown != null)   _resolutionDropdown.SetValueWithoutNotify(GetResolutionDropdownIndex(g));
        if (_screenModeDropdown != null)   _screenModeDropdown.SetValueWithoutNotify((int)g.ScreenModeValue);
        if (_frameRateDropdown != null)    _frameRateDropdown.SetValueWithoutNotify(GetFrameRateDropdownIndex(g.TargetFrameRate));
        if (_vsyncToggle != null)          _vsyncToggle.SetIsOnWithoutNotify(g.VSync);
        if (_antiAliasingDropdown != null) _antiAliasingDropdown.SetValueWithoutNotify((int)g.AntiAliasing);
        if (_shadowDropdown != null)       _shadowDropdown.SetValueWithoutNotify((int)g.ShadowQuality);
        if (_textureDropdown != null)      _textureDropdown.SetValueWithoutNotify((int)g.TextureQuality);

        if (_brightnessSlider != null)
        {
            if (_settings.Config != null)
            {
                _brightnessSlider.minValue = _settings.Config.BrightnessMin;
                _brightnessSlider.maxValue = _settings.Config.BrightnessMax;
            }
            _brightnessSlider.SetValueWithoutNotify(g.Brightness);
        }

        UpdateBrightnessLabel(g.Brightness);
    }

    //==============================================================================
    // 기능: GraphicsOptionsTabController::SetTabActive
    // 설명: 그래픽 옵션 탭 루트 패널의 표시 여부를 전환한다.
    //==============================================================================
    // TODO: 탭 전환 애니메이션 연결
    //
    //==============================================================================
    // active: true면 탭을 표시하고 false면 숨김
    public void SetTabActive(bool active)
    {
        if (_panelRoot != null) _panelRoot.SetActive(active);
    }

    //==========================================================================
    // [5] 드롭다운 옵션 채우기
    //==========================================================================

    //==============================================================================
    // 기능: GraphicsOptionsTabController::PopulateQualityOptions
    // 설명: QualitySettings.names 값을 Quality 드롭다운 옵션으로 채운다.
    //==============================================================================
    // TODO: 품질 이름 지역화
    //
    //==============================================================================
    private void PopulateQualityOptions()
    {
        if (_qualityDropdown == null) return;

        _qualityDropdown.ClearOptions();
        string[] names = QualitySettings.names;
        var options = new List<string>(names.Length);
        for (int i = 0; i < names.Length; i++) options.Add(names[i]);
        _qualityDropdown.AddOptions(options);
    }

    //==============================================================================
    // 기능: GraphicsOptionsTabController::PopulateResolutionOptions
    // 설명: GraphicsSettingsController 의 해상도 라벨을 해상도 드롭다운 옵션으로 채운다.
    //==============================================================================
    // TODO: 중복 해상도 필터 표시
    //
    //==============================================================================
    private void PopulateResolutionOptions()
    {
        if (_resolutionDropdown == null) return;
        if (_settings == null || _settings.Graphics == null) return;

        _resolutionDropdown.ClearOptions();
        string[] labels = _settings.Graphics.GetResolutionLabels();
        var options = new List<string>(labels.Length);
        for (int i = 0; i < labels.Length; i++) options.Add(labels[i]);
        _resolutionDropdown.AddOptions(options);
    }

    //==============================================================================
    // 기능: GraphicsOptionsTabController::PopulateFrameRateOptions
    // 설명: 고정 프레임레이트 라벨 배열을 프레임레이트 드롭다운 옵션으로 채운다.
    //==============================================================================
    // TODO: 모니터 주사율 기반 자동 옵션 추가
    //
    //==============================================================================
    private void PopulateFrameRateOptions()
    {
        if (_frameRateDropdown == null) return;

        _frameRateDropdown.ClearOptions();
        var options = new List<string>(_frameRateLabels.Length);
        for (int i = 0; i < _frameRateLabels.Length; i++) options.Add(_frameRateLabels[i]);
        _frameRateDropdown.AddOptions(options);
    }

    //==============================================================================
    // 기능: GraphicsOptionsTabController::PopulateEnumOptions
    // 설명: 문자열 라벨 배열을 enum 형태 드롭다운 옵션으로 채운다.
    //==============================================================================
    // TODO: 공통 UI 헬퍼로 이동
    //
    //==============================================================================
    // dropdown: 옵션을 채울 드롭다운
    // labels: 표시할 라벨 배열
    private void PopulateEnumOptions(TMP_Dropdown dropdown, string[] labels)
    {
        if (dropdown == null) return;

        dropdown.ClearOptions();
        var options = new List<string>(labels.Length);
        for (int i = 0; i < labels.Length; i++) options.Add(labels[i]);
        dropdown.AddOptions(options);
    }

    //--------------------------------------------------------------------------
    // [5-1] 인덱스 매핑 헬퍼
    //--------------------------------------------------------------------------

    //==============================================================================
    // 기능: GraphicsOptionsTabController::GetResolutionDropdownIndex
    // 설명: Graphics 데이터의 ResolutionIndex 또는 현재 화면 해상도 인덱스를 드롭다운 인덱스로 반환한다.
    //==============================================================================
    // TODO: 해상도 목록이 비어 있을 때 UI 비활성화
    //
    //==============================================================================
    // g: 현재 그래픽 설정 데이터
    private int GetResolutionDropdownIndex(SettingsData.Graphics g)
    {
        if (g.ResolutionIndex >= 0) return g.ResolutionIndex;
        if (_settings != null && _settings.Graphics != null)
        {
            int cur = _settings.Graphics.GetCurrentResolutionIndex();
            return cur >= 0 ? cur : 0;
        }
        return 0;
    }

    //==============================================================================
    // 기능: GraphicsOptionsTabController::GetFrameRateDropdownIndex
    // 설명: 실제 프레임레이트 값을 드롭다운 인덱스로 변환하고 없으면 60FPS 인덱스를 반환한다.
    //==============================================================================
    // TODO: Dictionary 캐싱
    //
    //==============================================================================
    // frameRate: 찾을 프레임레이트 값
    private int GetFrameRateDropdownIndex(int frameRate)
    {
        for (int i = 0; i < _frameRateValues.Length; i++)
        {
            if (_frameRateValues[i] == frameRate) return i;
        }
        return 1; // 60FPS 기본
    }

    //==========================================================================
    // [6] 이벤트 후킹 / 핸들러
    //==========================================================================

    //==============================================================================
    // 기능: GraphicsOptionsTabController::HookEvents
    // 설명: 그래픽 옵션 컨트롤의 값 변경 이벤트를 각 핸들러에 연결한다.
    //==============================================================================
    // TODO: 이벤트 중복 연결 방지 로직 공통화
    //
    //==============================================================================
    private void HookEvents()
    {
        if (_qualityDropdown != null)      _qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
        if (_resolutionDropdown != null)   _resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        if (_screenModeDropdown != null)   _screenModeDropdown.onValueChanged.AddListener(OnScreenModeChanged);
        if (_frameRateDropdown != null)    _frameRateDropdown.onValueChanged.AddListener(OnFrameRateChanged);
        if (_vsyncToggle != null)          _vsyncToggle.onValueChanged.AddListener(OnVSyncChanged);
        if (_antiAliasingDropdown != null) _antiAliasingDropdown.onValueChanged.AddListener(OnAntiAliasingChanged);
        if (_shadowDropdown != null)       _shadowDropdown.onValueChanged.AddListener(OnShadowChanged);
        if (_textureDropdown != null)      _textureDropdown.onValueChanged.AddListener(OnTextureChanged);
        if (_brightnessSlider != null)     _brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
    }

    // Quality 드롭다운 변경 값을 SettingsData.Graphics 에 반영
    private void OnQualityChanged(int idx)
    {
        if (_settings?.Data?.GraphicsSetting == null) return;
        _settings.Data.GraphicsSetting.QualityLevel = idx;
        _settings.Apply();
    }

    // 해상도 드롭다운 변경 값을 SettingsData.Graphics 에 반영
    private void OnResolutionChanged(int idx)
    {
        if (_settings == null) return;
        _settings.SetResolutionByIndex(idx);
        _settings.Apply();
    }

    // 화면 모드 드롭다운 변경 값을 SettingsData.Graphics 에 반영
    private void OnScreenModeChanged(int idx)
    {
        if (_settings?.Data?.GraphicsSetting == null) return;
        _settings.Data.GraphicsSetting.ScreenModeValue = (SettingsData.ScreenMode)idx;
        _settings.Apply();
    }

    // 프레임레이트 드롭다운 변경 값을 SettingsData.Graphics 에 반영
    private void OnFrameRateChanged(int idx)
    {
        if (_settings?.Data?.GraphicsSetting == null) return;
        if (idx < 0 || idx >= _frameRateValues.Length) return;

        _settings.Data.GraphicsSetting.TargetFrameRate = _frameRateValues[idx];
        _settings.Apply();
    }

    // VSync 토글 변경 값을 SettingsData.Graphics 에 반영
    private void OnVSyncChanged(bool isOn)
    {
        if (_settings?.Data?.GraphicsSetting == null) return;
        _settings.Data.GraphicsSetting.VSync = isOn;
        _settings.Apply();
    }

    // 안티에일리어싱 드롭다운 변경 값을 SettingsData.Graphics 에 반영
    private void OnAntiAliasingChanged(int idx)
    {
        if (_settings?.Data?.GraphicsSetting == null) return;
        _settings.Data.GraphicsSetting.AntiAliasing = (SettingsData.AntiAliasingMode)idx;
        _settings.Apply();
    }

    // 그림자 품질 드롭다운 변경 값을 SettingsData.Graphics 에 반영
    private void OnShadowChanged(int idx)
    {
        if (_settings?.Data?.GraphicsSetting == null) return;
        _settings.Data.GraphicsSetting.ShadowQuality = (SettingsData.ShadowLevel)idx;
        _settings.Apply();
    }

    // 텍스처 품질 드롭다운 변경 값을 SettingsData.Graphics 에 반영
    private void OnTextureChanged(int idx)
    {
        if (_settings?.Data?.GraphicsSetting == null) return;
        _settings.Data.GraphicsSetting.TextureQuality = (SettingsData.TextureLevel)idx;
        _settings.Apply();
    }

    // 밝기 슬라이더 변경 값을 SettingsData.Graphics 에 반영
    private void OnBrightnessChanged(float value)
    {
        if (_settings?.Data?.GraphicsSetting == null) return;
        _settings.Data.GraphicsSetting.Brightness = value;
        UpdateBrightnessLabel(value);
        _settings.Apply();
    }

    //--------------------------------------------------------------------------
    // [6-1] 라벨 갱신
    //--------------------------------------------------------------------------

    //==============================================================================
    // 기능: GraphicsOptionsTabController::UpdateBrightnessLabel
    // 설명: 밝기 값을 소수점 둘째 자리 문자열로 변환해 라벨에 표시한다.
    //==============================================================================
    // TODO: 퍼센트 표시 옵션
    //
    //==============================================================================
    // value: 표시할 밝기 값
    private void UpdateBrightnessLabel(float value)
    {
        if (_brightnessValueLabel == null) return;
        _brightnessValueLabel.text = value.ToString("0.00");
    }
}
