using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//==============================================================================
// 기능: AudioOptionsTabController
// 설명: 오디오 옵션 탭의 슬라이더/토글 UI를 SettingsData.Audio 와 연결한다.
//==============================================================================
// TODO: 채널별 프리셋, 볼륨 테스트 사운드 버튼 추가
//
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

    private SettingsController _settings; // 옵션 데이터를 읽고 적용할 SettingsController 참조
    private bool _hooked;                 // UI 이벤트를 이미 연결했는지 여부

    //==============================================================================
    // 기능: AudioOptionsTabController::Bind
    // 설명: SettingsController 를 캐시하고 오디오 UI 이벤트를 한 번만 연결한다.
    //==============================================================================
    // TODO: Unbind/OnDestroy 이벤트 해제
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
    // 기능: AudioOptionsTabController::Refresh
    // 설명: 현재 AudioSetting 값을 슬라이더/토글 UI에 다시 표시한다.
    //==============================================================================
    // TODO: 변경된 UI만 갱신하는 최적화
    //
    //==============================================================================
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

    //==============================================================================
    // 기능: AudioOptionsTabController::SetTabActive
    // 설명: 오디오 옵션 탭 루트 패널의 표시 여부를 전환한다.
    //==============================================================================
    // TODO: 탭 전환 애니메이션 연결
    //
    //==============================================================================
    // active: true면 탭을 표시하고 false면 숨김
    public void SetTabActive(bool active)
    {
        if (_panelRoot != null) _panelRoot.SetActive(active);
    }

    //==============================================================================
    // 기능: AudioOptionsTabController::HookEvents
    // 설명: 슬라이더/토글 값 변경 이벤트를 각 핸들러에 연결한다.
    //==============================================================================
    // TODO: 이벤트 중복 연결 방지 로직을 공통화
    //
    //==============================================================================
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

    // 마스터 볼륨 슬라이더 변경 처리
    private void OnMasterChanged(float v)  { if (IsReady()) { _settings.Data.AudioSetting.MasterVolume = v;  UpdateLabel(_masterLabel, v);  _settings.Apply(); } }
    // BGM 볼륨 슬라이더 변경 처리
    private void OnBgmChanged(float v)     { if (IsReady()) { _settings.Data.AudioSetting.BgmVolume = v;     UpdateLabel(_bgmLabel, v);     _settings.Apply(); } }
    // SFX 볼륨 슬라이더 변경 처리
    private void OnSfxChanged(float v)     { if (IsReady()) { _settings.Data.AudioSetting.SfxVolume = v;     UpdateLabel(_sfxLabel, v);     _settings.Apply(); } }
    // Voice 볼륨 슬라이더 변경 처리
    private void OnVoiceChanged(float v)   { if (IsReady()) { _settings.Data.AudioSetting.VoiceVolume = v;   UpdateLabel(_voiceLabel, v);   _settings.Apply(); } }
    // UI 볼륨 슬라이더 변경 처리
    private void OnUiChanged(float v)      { if (IsReady()) { _settings.Data.AudioSetting.UiVolume = v;      UpdateLabel(_uiLabel, v);      _settings.Apply(); } }
    // Ambient 볼륨 슬라이더 변경 처리
    private void OnAmbientChanged(float v) { if (IsReady()) { _settings.Data.AudioSetting.AmbientVolume = v; UpdateLabel(_ambientLabel, v); _settings.Apply(); } }

    //==============================================================================
    // 기능: AudioOptionsTabController::OnMuteChanged
    // 설명: 전체 음소거 토글 값을 AudioSetting 에 반영하고 즉시 적용한다.
    //==============================================================================
    // TODO:
    //
    //==============================================================================
    // isOn: 음소거 토글 상태
    private void OnMuteChanged(bool isOn)
    {
        if (!IsReady()) return;
        _settings.Data.AudioSetting.Mute = isOn;
        _settings.Apply();
    }

    //==============================================================================
    // 기능: AudioOptionsTabController::OnMuteOnFocusLostChanged
    // 설명: 창 포커스 상실 시 자동 음소거 옵션 값을 AudioSetting 에 저장한다.
    //==============================================================================
    // TODO: 토글 변경 직후 현재 포커스 상태와 동기화
    //
    //==============================================================================
    // isOn: 포커스 상실 음소거 토글 상태
    private void OnMuteOnFocusLostChanged(bool isOn)
    {
        if (!IsReady()) return;
        _settings.Data.AudioSetting.MuteOnFocusLost = isOn;
        // 적용은 실제로 OnApplicationFocus 시점에 일어나므로 Apply 불필요(현재 화면 체감 없음)
    }

    //==========================================================================
    // [7] 헬퍼
    //==========================================================================

    //==============================================================================
    // 기능: AudioOptionsTabController::SetSliderRange01
    // 설명: 볼륨 슬라이더의 입력 범위를 0~1로 강제한다.
    //==============================================================================
    // TODO:
    //
    //==============================================================================
    // s: 범위를 적용할 Slider
    private void SetSliderRange01(Slider s)
    {
        if (s == null) return;
        s.minValue = 0f;
        s.maxValue = 1f;
    }

    //==============================================================================
    // 기능: AudioOptionsTabController::UpdateLabel
    // 설명: 0~1 볼륨 값을 퍼센트 문자열로 변환해 라벨에 표시한다.
    //==============================================================================
    // TODO: 소수점 표시 옵션
    //
    //==============================================================================
    // label: 갱신할 TextMeshPro 라벨
    // value01: 0~1 범위의 볼륨 값
    private void UpdateLabel(TextMeshProUGUI label, float value01)
    {
        if (label == null) return;
        label.text = $"{Mathf.RoundToInt(Mathf.Clamp01(value01) * 100f)}%";
    }

    //==============================================================================
    // 기능: AudioOptionsTabController::IsReady
    // 설명: 오디오 옵션 UI 이벤트 처리에 필요한 Settings/Data/AudioSetting 이 준비되었는지 확인한다.
    //==============================================================================
    // TODO:
    //
    //==============================================================================
    private bool IsReady()
    {
        return _settings != null && _settings.Data != null && _settings.Data.AudioSetting != null;
    }
}
