using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//==============================================================================
// 기능: CameraOptionsTabController
// 설명: 카메라/입력 옵션 탭의 감도 슬라이더와 반전/스무딩 토글을 SettingsData.Camera 와 연결한다.
//==============================================================================
// TODO: 키보드/패드 감도 분리, 리바인딩 UI 연결
//
//==============================================================================

public class CameraOptionsTabController : MonoBehaviour, IOptionsTabController
{
    //--------------------------------------------------------------------------
    // [1] 패널
    //--------------------------------------------------------------------------

    [TabGroup("Panel")]
    [Tooltip("이 탭의 활성/비활성을 토글하는 루트 GameObject.")]
    [SerializeField] private GameObject _panelRoot;

    //--------------------------------------------------------------------------
    // [2] 슬라이더 + 라벨
    //--------------------------------------------------------------------------

    [TabGroup("Sliders")]
    [Tooltip("X축 감도 슬라이더.")]
    [SerializeField] private Slider _sensitivityXSlider;

    [TabGroup("Sliders")]
    [Tooltip("X축 감도 값 라벨(선택).")]
    [SerializeField] private TextMeshProUGUI _sensitivityXLabel;

    [TabGroup("Sliders")]
    [Tooltip("Y축 감도 슬라이더.")]
    [SerializeField] private Slider _sensitivityYSlider;

    [TabGroup("Sliders")]
    [Tooltip("Y축 감도 값 라벨(선택).")]
    [SerializeField] private TextMeshProUGUI _sensitivityYLabel;

    [TabGroup("Sliders")]
    [Tooltip("조준 시 감도 배수 슬라이더.")]
    [SerializeField] private Slider _aimMultiplierSlider;

    [TabGroup("Sliders")]
    [Tooltip("조준 배수 값 라벨(선택).")]
    [SerializeField] private TextMeshProUGUI _aimMultiplierLabel;

    //--------------------------------------------------------------------------
    // [3] 토글
    //--------------------------------------------------------------------------

    [TabGroup("Toggles")]
    [Tooltip("X축 반전 토글.")]
    [SerializeField] private Toggle _invertXToggle;

    [TabGroup("Toggles")]
    [Tooltip("Y축 반전 토글.")]
    [SerializeField] private Toggle _invertYToggle;

    [TabGroup("Toggles")]
    [Tooltip("마우스 스무딩 토글.")]
    [SerializeField] private Toggle _mouseSmoothingToggle;

    //--------------------------------------------------------------------------
    // [4] 상태
    //--------------------------------------------------------------------------

    private SettingsController _settings; // 옵션 데이터를 읽고 적용할 SettingsController 참조
    private bool _hooked;                 // UI 이벤트를 이미 연결했는지 여부

    //==============================================================================
    // 기능: CameraOptionsTabController::Bind
    // 설명: SettingsController 를 캐시하고 카메라 옵션 UI 이벤트를 한 번만 연결한다.
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
    // 기능: CameraOptionsTabController::Refresh
    // 설명: 현재 CameraSetting 값을 슬라이더/토글 UI에 다시 표시한다.
    //==============================================================================
    // TODO: Config 값 변경 시 슬라이더 범위만 별도 갱신
    //
    //==============================================================================
    public void Refresh()
    {
        if (_settings?.Data?.CameraSetting == null) return;

        SettingsData.Camera c = _settings.Data.CameraSetting;
        SettingsConfig cfg = _settings.Config;

        // 슬라이더 범위 설정
        if (cfg != null)
        {
            ApplySliderRange(_sensitivityXSlider, cfg.SensitivityMin, cfg.SensitivityMax);
            ApplySliderRange(_sensitivityYSlider, cfg.SensitivityMin, cfg.SensitivityMax);
        }
        ApplySliderRange(_aimMultiplierSlider, 0.05f, 2f);

        if (_sensitivityXSlider != null)  _sensitivityXSlider.SetValueWithoutNotify(c.SensitivityX);
        if (_sensitivityYSlider != null)  _sensitivityYSlider.SetValueWithoutNotify(c.SensitivityY);
        if (_aimMultiplierSlider != null) _aimMultiplierSlider.SetValueWithoutNotify(c.AimSensitivityMultiplier);

        UpdateLabel(_sensitivityXLabel, c.SensitivityX);
        UpdateLabel(_sensitivityYLabel, c.SensitivityY);
        UpdateLabel(_aimMultiplierLabel, c.AimSensitivityMultiplier);

        if (_invertXToggle != null)        _invertXToggle.SetIsOnWithoutNotify(c.InvertX);
        if (_invertYToggle != null)        _invertYToggle.SetIsOnWithoutNotify(c.InvertY);
        if (_mouseSmoothingToggle != null) _mouseSmoothingToggle.SetIsOnWithoutNotify(c.MouseSmoothing);
    }

    //==============================================================================
    // 기능: CameraOptionsTabController::SetTabActive
    // 설명: 카메라 옵션 탭 루트 패널의 표시 여부를 전환한다.
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
    // 기능: CameraOptionsTabController::HookEvents
    // 설명: 감도 슬라이더와 토글의 값 변경 이벤트를 각 핸들러에 연결한다.
    //==============================================================================
    // TODO: 이벤트 중복 연결 방지 로직 공통화
    //
    //==============================================================================
    private void HookEvents()
    {
        if (_sensitivityXSlider != null)  _sensitivityXSlider.onValueChanged.AddListener(OnSensitivityXChanged);
        if (_sensitivityYSlider != null)  _sensitivityYSlider.onValueChanged.AddListener(OnSensitivityYChanged);
        if (_aimMultiplierSlider != null) _aimMultiplierSlider.onValueChanged.AddListener(OnAimMultiplierChanged);

        if (_invertXToggle != null)        _invertXToggle.onValueChanged.AddListener(OnInvertXChanged);
        if (_invertYToggle != null)        _invertYToggle.onValueChanged.AddListener(OnInvertYChanged);
        if (_mouseSmoothingToggle != null) _mouseSmoothingToggle.onValueChanged.AddListener(OnMouseSmoothingChanged);
    }

    //==============================================================================
    // 기능: CameraOptionsTabController::OnSensitivityXChanged
    // 설명: X축 감도 슬라이더 변경 값을 SettingsData.Camera 에 반영하고 즉시 적용한다.
    //==============================================================================
    // TODO:
    //
    //==============================================================================
    // v: 새 X축 감도 값
    private void OnSensitivityXChanged(float v)
    {
        if (!IsReady()) return;
        _settings.Data.CameraSetting.SensitivityX = v;
        UpdateLabel(_sensitivityXLabel, v);
        _settings.Apply();
    }

    //==============================================================================
    // 기능: CameraOptionsTabController::OnSensitivityYChanged
    // 설명: Y축 감도 슬라이더 변경 값을 SettingsData.Camera 에 반영하고 즉시 적용한다.
    //==============================================================================
    // TODO:
    //
    //==============================================================================
    // v: 새 Y축 감도 값
    private void OnSensitivityYChanged(float v)
    {
        if (!IsReady()) return;
        _settings.Data.CameraSetting.SensitivityY = v;
        UpdateLabel(_sensitivityYLabel, v);
        _settings.Apply();
    }

    //==============================================================================
    // 기능: CameraOptionsTabController::OnAimMultiplierChanged
    // 설명: 조준 감도 배수 변경 값을 SettingsData.Camera 에 반영하고 즉시 적용한다.
    //==============================================================================
    // TODO:
    //
    //==============================================================================
    // v: 새 조준 감도 배수 값
    private void OnAimMultiplierChanged(float v)
    {
        if (!IsReady()) return;
        _settings.Data.CameraSetting.AimSensitivityMultiplier = v;
        UpdateLabel(_aimMultiplierLabel, v);
        _settings.Apply();
    }

    //==============================================================================
    // 기능: CameraOptionsTabController::OnInvertXChanged
    // 설명: X축 반전 토글 값을 SettingsData.Camera 에 반영하고 즉시 적용한다.
    //==============================================================================
    // TODO:
    //
    //==============================================================================
    // isOn: X축 반전 여부
    private void OnInvertXChanged(bool isOn)
    {
        if (!IsReady()) return;
        _settings.Data.CameraSetting.InvertX = isOn;
        _settings.Apply();
    }

    //==============================================================================
    // 기능: CameraOptionsTabController::OnInvertYChanged
    // 설명: Y축 반전 토글 값을 SettingsData.Camera 에 반영하고 즉시 적용한다.
    //==============================================================================
    // TODO:
    //
    //==============================================================================
    // isOn: Y축 반전 여부
    private void OnInvertYChanged(bool isOn)
    {
        if (!IsReady()) return;
        _settings.Data.CameraSetting.InvertY = isOn;
        _settings.Apply();
    }

    //==============================================================================
    // 기능: CameraOptionsTabController::OnMouseSmoothingChanged
    // 설명: 마우스 스무딩 토글 값을 SettingsData.Camera 에 반영하고 즉시 적용한다.
    //==============================================================================
    // TODO:
    //
    //==============================================================================
    // isOn: 마우스 스무딩 여부
    private void OnMouseSmoothingChanged(bool isOn)
    {
        if (!IsReady()) return;
        _settings.Data.CameraSetting.MouseSmoothing = isOn;
        _settings.Apply();
    }

    //==========================================================================
    // [7] 헬퍼
    //==========================================================================

    //==============================================================================
    // 기능: CameraOptionsTabController::ApplySliderRange
    // 설명: 지정한 Slider 의 min/max 값을 설정한다.
    //==============================================================================
    // TODO:
    //
    //==============================================================================
    // s: 범위를 적용할 Slider
    // min: 최소값
    // max: 최대값
    private void ApplySliderRange(Slider s, float min, float max)
    {
        if (s == null) return;
        s.minValue = min;
        s.maxValue = max;
    }

    //==============================================================================
    // 기능: CameraOptionsTabController::UpdateLabel
    // 설명: 실수 값을 소수점 둘째 자리 문자열로 변환해 라벨에 표시한다.
    //==============================================================================
    // TODO: 표시 자릿수 옵션
    //
    //==============================================================================
    // label: 갱신할 TextMeshPro 라벨
    // value: 표시할 실수 값
    private void UpdateLabel(TextMeshProUGUI label, float value)
    {
        if (label == null) return;
        label.text = value.ToString("0.00");
    }

    //==============================================================================
    // 기능: CameraOptionsTabController::IsReady
    // 설명: 카메라 옵션 UI 이벤트 처리에 필요한 Settings/Data/CameraSetting 이 준비되었는지 확인한다.
    //==============================================================================
    // TODO:
    //
    //==============================================================================
    private bool IsReady()
    {
        return _settings != null && _settings.Data != null && _settings.Data.CameraSetting != null;
    }
}
