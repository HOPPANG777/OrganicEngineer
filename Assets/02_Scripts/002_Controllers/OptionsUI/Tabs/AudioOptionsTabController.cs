using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//==============================================================================
// AudioOptionsTabController: 오디오 옵션 탭 UI
//==============================================================================
// - 6채널 슬라이더(Master/BGM/SFX/Voice/UI/Ambient) + Mute / MuteOnFocusLost 토글
// - 슬라이더 값은 0~1 범위, 라벨은 % 표기 ("80%")
//==============================================================================

public class AudioOptionsTabController : MonoBehaviour, IOptionsTabController
{
    //--------------------------------------------------------------------------
    // [1] 패널
    //--------------------------------------------------------------------------

    [TabGroup("Panel")]
    [Tooltip("이 탭의 활성/비활성을 토글하는 루트 GameObject.")]
    [SerializeField] private GameObject _panelRoot;

    //--------------------------------------------------------------------------
    // [2] 볼륨 슬라이더 + 라벨 (선택적)
    //--------------------------------------------------------------------------

    [TabGroup("Volumes")]
    [Tooltip("마스터 볼륨 슬라이더.")]
    [SerializeField] private Slider _masterSlider;

    [TabGroup("Volumes")]
    [Tooltip("마스터 볼륨 값 라벨(선택).")]
    [SerializeField] private TextMeshProUGUI _masterLabel;

    [TabGroup("Volumes")]
    [Tooltip("BGM 볼륨 슬라이더.")]
    [SerializeField] private Slider _bgmSlider;

    [TabGroup("Volumes")]
    [Tooltip("BGM 볼륨 값 라벨(선택).")]
    [SerializeField] private TextMeshProUGUI _bgmLabel;

    [TabGroup("Volumes")]
    [Tooltip("SFX 볼륨 슬라이더.")]
    [SerializeField] private Slider _sfxSlider;

    [TabGroup("Volumes")]
    [Tooltip("SFX 볼륨 값 라벨(선택).")]
    [SerializeField] private TextMeshProUGUI _sfxLabel;

    [TabGroup("Volumes")]
    [Tooltip("Voice 볼륨 슬라이더.")]
    [SerializeField] private Slider _voiceSlider;

    [TabGroup("Volumes")]
    [Tooltip("Voice 볼륨 값 라벨(선택).")]
    [SerializeField] private TextMeshProUGUI _voiceLabel;

    [TabGroup("Volumes")]
    [Tooltip("UI 볼륨 슬라이더.")]
    [SerializeField] private Slider _uiSlider;

    [TabGroup("Volumes")]
    [Tooltip("UI 볼륨 값 라벨(선택).")]
    [SerializeField] private TextMeshProUGUI _uiLabel;

    [TabGroup("Volumes")]
    [Tooltip("Ambient 볼륨 슬라이더.")]
    [SerializeField] private Slider _ambientSlider;

    [TabGroup("Volumes")]
    [Tooltip("Ambient 볼륨 값 라벨(선택).")]
    [SerializeField] private TextMeshProUGUI _ambientLabel;

    //--------------------------------------------------------------------------
    // [3] 토글
    //--------------------------------------------------------------------------

    [TabGroup("Toggles")]
    [Tooltip("전체 음소거 토글.")]
    [SerializeField] private Toggle _muteToggle;

    [TabGroup("Toggles")]
    [Tooltip("창 비활성 시 자동 음소거 토글.")]
    [SerializeField] private Toggle _muteOnFocusLostToggle;

    //--------------------------------------------------------------------------
    // [4] 상태
    //--------------------------------------------------------------------------

    private SettingsController _settings;
    private bool _hooked;

    //==========================================================================
    // [5] IOptionsTabController 구현
    //==========================================================================

    public void Bind(SettingsController controller)
    {
        _settings = controller;
        if (_hooked) return;
        HookEvents();
        _hooked = true;
    }

    public void Refresh()
    {
        if (_settings?.Data?.AudioSetting == null) return;

        SettingsData.Audio a = _settings.Data.AudioSetting;

        SetSliderRange01(_masterSlider);
        SetSliderRange01(_bgmSlider);
        SetSliderRange01(_sfxSlider);
        SetSliderRange01(_voiceSlider);
        SetSliderRange01(_uiSlider);
        SetSliderRange01(_ambientSlider);

        if (_masterSlider != null)  _masterSlider.SetValueWithoutNotify(a.MasterVolume);
        if (_bgmSlider != null)     _bgmSlider.SetValueWithoutNotify(a.BgmVolume);
        if (_sfxSlider != null)     _sfxSlider.SetValueWithoutNotify(a.SfxVolume);
        if (_voiceSlider != null)   _voiceSlider.SetValueWithoutNotify(a.VoiceVolume);
        if (_uiSlider != null)      _uiSlider.SetValueWithoutNotify(a.UiVolume);
        if (_ambientSlider != null) _ambientSlider.SetValueWithoutNotify(a.AmbientVolume);

        UpdateLabel(_masterLabel, a.MasterVolume);
        UpdateLabel(_bgmLabel, a.BgmVolume);
        UpdateLabel(_sfxLabel, a.SfxVolume);
        UpdateLabel(_voiceLabel, a.VoiceVolume);
        UpdateLabel(_uiLabel, a.UiVolume);
        UpdateLabel(_ambientLabel, a.AmbientVolume);

        if (_muteToggle != null)             _muteToggle.SetIsOnWithoutNotify(a.Mute);
        if (_muteOnFocusLostToggle != null)  _muteOnFocusLostToggle.SetIsOnWithoutNotify(a.MuteOnFocusLost);
    }

    public void SetTabActive(bool active)
    {
        if (_panelRoot != null) _panelRoot.SetActive(active);
    }

    //==========================================================================
    // [6] 이벤트 후킹 / 핸들러
    //==========================================================================

    private void HookEvents()
    {
        if (_masterSlider != null)  _masterSlider.onValueChanged.AddListener(OnMasterChanged);
        if (_bgmSlider != null)     _bgmSlider.onValueChanged.AddListener(OnBgmChanged);
        if (_sfxSlider != null)     _sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        if (_voiceSlider != null)   _voiceSlider.onValueChanged.AddListener(OnVoiceChanged);
        if (_uiSlider != null)      _uiSlider.onValueChanged.AddListener(OnUiChanged);
        if (_ambientSlider != null) _ambientSlider.onValueChanged.AddListener(OnAmbientChanged);

        if (_muteToggle != null)             _muteToggle.onValueChanged.AddListener(OnMuteChanged);
        if (_muteOnFocusLostToggle != null)  _muteOnFocusLostToggle.onValueChanged.AddListener(OnMuteOnFocusLostChanged);
    }

    private void OnMasterChanged(float v)  { if (IsReady()) { _settings.Data.AudioSetting.MasterVolume = v;  UpdateLabel(_masterLabel, v);  _settings.Apply(); } }
    private void OnBgmChanged(float v)     { if (IsReady()) { _settings.Data.AudioSetting.BgmVolume = v;     UpdateLabel(_bgmLabel, v);     _settings.Apply(); } }
    private void OnSfxChanged(float v)     { if (IsReady()) { _settings.Data.AudioSetting.SfxVolume = v;     UpdateLabel(_sfxLabel, v);     _settings.Apply(); } }
    private void OnVoiceChanged(float v)   { if (IsReady()) { _settings.Data.AudioSetting.VoiceVolume = v;   UpdateLabel(_voiceLabel, v);   _settings.Apply(); } }
    private void OnUiChanged(float v)      { if (IsReady()) { _settings.Data.AudioSetting.UiVolume = v;      UpdateLabel(_uiLabel, v);      _settings.Apply(); } }
    private void OnAmbientChanged(float v) { if (IsReady()) { _settings.Data.AudioSetting.AmbientVolume = v; UpdateLabel(_ambientLabel, v); _settings.Apply(); } }

    private void OnMuteChanged(bool isOn)
    {
        if (!IsReady()) return;
        _settings.Data.AudioSetting.Mute = isOn;
        _settings.Apply();
    }

    private void OnMuteOnFocusLostChanged(bool isOn)
    {
        if (!IsReady()) return;
        _settings.Data.AudioSetting.MuteOnFocusLost = isOn;
        // 적용은 실제로 OnApplicationFocus 시점에 일어나므로 Apply 불필요(현재 화면 체감 없음)
    }

    //==========================================================================
    // [7] 헬퍼
    //==========================================================================

    /// <summary>볼륨 슬라이더의 범위를 0~1로 강제.</summary>
    private static void SetSliderRange01(Slider s)
    {
        if (s == null) return;
        s.minValue = 0f;
        s.maxValue = 1f;
    }

    /// <summary>0~1 값을 % 라벨로 갱신.</summary>
    private static void UpdateLabel(TextMeshProUGUI label, float value01)
    {
        if (label == null) return;
        label.text = $"{Mathf.RoundToInt(Mathf.Clamp01(value01) * 100f)}%";
    }

    /// <summary>핸들러 진입 가드(데이터 유효성).</summary>
    private bool IsReady()
    {
        return _settings != null && _settings.Data != null && _settings.Data.AudioSetting != null;
    }
}
