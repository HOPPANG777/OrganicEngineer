using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//==============================================================================
// GraphicsOptionsTabController: 그래픽 옵션 탭 UI
//==============================================================================
// - Quality / Resolution / ScreenMode / FrameRate / VSync / AA / Shadow / Texture / Brightness
// - 컨트롤 변경 시 SettingsData에 즉시 기록 + Settings.Apply() 호출 (미리보기)
// - 부모 OptionsScreenController가 Cancel 시 스냅샷에서 복원
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

    /// <summary>SettingsController 캐시.</summary>
    private SettingsController _settings;

    /// <summary>이벤트 후킹 1회 보장.</summary>
    private bool _hooked;

    //--------------------------------------------------------------------------
    // [3-1] 프레임레이트 드롭다운 매핑
    //--------------------------------------------------------------------------

    /// <summary>드롭다운 인덱스 → 실제 FrameRate 값(-1 = 무제한).</summary>
    private static readonly int[] FRAME_RATE_VALUES = { 30, 60, 120, 144, -1 };

    /// <summary>드롭다운 인덱스 → 라벨.</summary>
    private static readonly string[] FRAME_RATE_LABELS = { "30 FPS", "60 FPS", "120 FPS", "144 FPS", "Unlimited" };

    //==========================================================================
    // [4] IOptionsTabController 구현
    //==========================================================================

    /// <summary>Settings 주입 + 이벤트 후킹.</summary>
    public void Bind(SettingsController controller)
    {
        _settings = controller;
        if (_hooked) return;
        HookEvents();
        _hooked = true;
    }

    /// <summary>현재 Data를 UI 컨트롤로 동기화(드롭다운 옵션 채우기 포함).</summary>
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

    /// <summary>탭 활성/비활성.</summary>
    public void SetTabActive(bool active)
    {
        if (_panelRoot != null) _panelRoot.SetActive(active);
    }

    //==========================================================================
    // [5] 드롭다운 옵션 채우기
    //==========================================================================

    /// <summary>QualitySettings.names → Quality 드롭다운 옵션.</summary>
    private void PopulateQualityOptions()
    {
        if (_qualityDropdown == null) return;

        _qualityDropdown.ClearOptions();
        string[] names = QualitySettings.names;
        var options = new List<string>(names.Length);
        for (int i = 0; i < names.Length; i++) options.Add(names[i]);
        _qualityDropdown.AddOptions(options);
    }

    /// <summary>Settings.Graphics.GetResolutionLabels() → 해상도 드롭다운 옵션.</summary>
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

    /// <summary>FrameRate 드롭다운 옵션.</summary>
    private void PopulateFrameRateOptions()
    {
        if (_frameRateDropdown == null) return;

        _frameRateDropdown.ClearOptions();
        var options = new List<string>(FRAME_RATE_LABELS.Length);
        for (int i = 0; i < FRAME_RATE_LABELS.Length; i++) options.Add(FRAME_RATE_LABELS[i]);
        _frameRateDropdown.AddOptions(options);
    }

    /// <summary>주어진 라벨로 enum 드롭다운 채우기.</summary>
    private static void PopulateEnumOptions(TMP_Dropdown dropdown, string[] labels)
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

    /// <summary>현재 Graphics 데이터 → 해상도 드롭다운 인덱스(없으면 현재 화면 인덱스 또는 0).</summary>
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

    /// <summary>실제 FrameRate 값 → 드롭다운 인덱스(없으면 60FPS 인덱스).</summary>
    private static int GetFrameRateDropdownIndex(int frameRate)
    {
        for (int i = 0; i < FRAME_RATE_VALUES.Length; i++)
        {
            if (FRAME_RATE_VALUES[i] == frameRate) return i;
        }
        return 1; // 60FPS 기본
    }

    //==========================================================================
    // [6] 이벤트 후킹 / 핸들러
    //==========================================================================

    /// <summary>모든 컨트롤에 onValueChanged 리스너 등록.</summary>
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

    private void OnQualityChanged(int idx)
    {
        if (_settings?.Data?.GraphicsSetting == null) return;
        _settings.Data.GraphicsSetting.QualityLevel = idx;
        _settings.Apply();
    }

    private void OnResolutionChanged(int idx)
    {
        if (_settings == null) return;
        _settings.SetResolutionByIndex(idx);
        _settings.Apply();
    }

    private void OnScreenModeChanged(int idx)
    {
        if (_settings?.Data?.GraphicsSetting == null) return;
        _settings.Data.GraphicsSetting.ScreenModeValue = (SettingsData.ScreenMode)idx;
        _settings.Apply();
    }

    private void OnFrameRateChanged(int idx)
    {
        if (_settings?.Data?.GraphicsSetting == null) return;
        if (idx < 0 || idx >= FRAME_RATE_VALUES.Length) return;

        _settings.Data.GraphicsSetting.TargetFrameRate = FRAME_RATE_VALUES[idx];
        _settings.Apply();
    }

    private void OnVSyncChanged(bool isOn)
    {
        if (_settings?.Data?.GraphicsSetting == null) return;
        _settings.Data.GraphicsSetting.VSync = isOn;
        _settings.Apply();
    }

    private void OnAntiAliasingChanged(int idx)
    {
        if (_settings?.Data?.GraphicsSetting == null) return;
        _settings.Data.GraphicsSetting.AntiAliasing = (SettingsData.AntiAliasingMode)idx;
        _settings.Apply();
    }

    private void OnShadowChanged(int idx)
    {
        if (_settings?.Data?.GraphicsSetting == null) return;
        _settings.Data.GraphicsSetting.ShadowQuality = (SettingsData.ShadowLevel)idx;
        _settings.Apply();
    }

    private void OnTextureChanged(int idx)
    {
        if (_settings?.Data?.GraphicsSetting == null) return;
        _settings.Data.GraphicsSetting.TextureQuality = (SettingsData.TextureLevel)idx;
        _settings.Apply();
    }

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

    /// <summary>밝기 값 라벨 갱신(소수 둘째 자리).</summary>
    private void UpdateBrightnessLabel(float value)
    {
        if (_brightnessValueLabel == null) return;
        _brightnessValueLabel.text = value.ToString("0.00");
    }
}
