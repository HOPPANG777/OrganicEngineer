using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//==============================================================================
// CameraOptionsTabController: 카메라/입력 옵션 탭 UI
//==============================================================================
// - SensitivityX/Y / AimMultiplier 슬라이더
// - InvertX / InvertY / MouseSmoothing 토글
// - 슬라이더의 min/max는 SettingsConfig 값을 따른다
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

    public void SetTabActive(bool active)
    {
        if (_panelRoot != null) _panelRoot.SetActive(active);
    }

    //==========================================================================
    // [6] 이벤트 후킹 / 핸들러
    //==========================================================================

    private void HookEvents()
    {
        if (_sensitivityXSlider != null)  _sensitivityXSlider.onValueChanged.AddListener(OnSensitivityXChanged);
        if (_sensitivityYSlider != null)  _sensitivityYSlider.onValueChanged.AddListener(OnSensitivityYChanged);
        if (_aimMultiplierSlider != null) _aimMultiplierSlider.onValueChanged.AddListener(OnAimMultiplierChanged);

        if (_invertXToggle != null)        _invertXToggle.onValueChanged.AddListener(OnInvertXChanged);
        if (_invertYToggle != null)        _invertYToggle.onValueChanged.AddListener(OnInvertYChanged);
        if (_mouseSmoothingToggle != null) _mouseSmoothingToggle.onValueChanged.AddListener(OnMouseSmoothingChanged);
    }

    private void OnSensitivityXChanged(float v)
    {
        if (!IsReady()) return;
        _settings.Data.CameraSetting.SensitivityX = v;
        UpdateLabel(_sensitivityXLabel, v);
        _settings.Apply();
    }

    private void OnSensitivityYChanged(float v)
    {
        if (!IsReady()) return;
        _settings.Data.CameraSetting.SensitivityY = v;
        UpdateLabel(_sensitivityYLabel, v);
        _settings.Apply();
    }

    private void OnAimMultiplierChanged(float v)
    {
        if (!IsReady()) return;
        _settings.Data.CameraSetting.AimSensitivityMultiplier = v;
        UpdateLabel(_aimMultiplierLabel, v);
        _settings.Apply();
    }

    private void OnInvertXChanged(bool isOn)
    {
        if (!IsReady()) return;
        _settings.Data.CameraSetting.InvertX = isOn;
        _settings.Apply();
    }

    private void OnInvertYChanged(bool isOn)
    {
        if (!IsReady()) return;
        _settings.Data.CameraSetting.InvertY = isOn;
        _settings.Apply();
    }

    private void OnMouseSmoothingChanged(bool isOn)
    {
        if (!IsReady()) return;
        _settings.Data.CameraSetting.MouseSmoothing = isOn;
        _settings.Apply();
    }

    //==========================================================================
    // [7] 헬퍼
    //==========================================================================

    /// <summary>슬라이더 min/max 적용.</summary>
    private static void ApplySliderRange(Slider s, float min, float max)
    {
        if (s == null) return;
        s.minValue = min;
        s.maxValue = max;
    }

    /// <summary>실수 값을 소수점 둘째 자리 라벨로 갱신.</summary>
    private static void UpdateLabel(TextMeshProUGUI label, float value)
    {
        if (label == null) return;
        label.text = value.ToString("0.00");
    }

    private bool IsReady()
    {
        return _settings != null && _settings.Data != null && _settings.Data.CameraSetting != null;
    }
}
