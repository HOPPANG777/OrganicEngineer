using UnityEngine;
using Sirenix.OdinInspector;

//==============================================================================
// SettingsConfig: 설정 기본값 / 허용 범위 정의 (ScriptableObject)
//==============================================================================
// - 신규 플레이어가 처음 실행할 때 사용할 기본값을 담는다
// - 슬라이더의 min/max 등 허용 범위도 함께 관리한다
// - 런타임 데이터(SettingsData)는 이 SO를 참고해 Clamp 처리한다
//==============================================================================

[CreateAssetMenu(menuName = "OrganicEngineer/Settings Config", fileName = "SettingsConfig")]
public class SettingsConfig : ScriptableObject
{
    //==========================================================================
    // [1] 그래픽 기본값
    //==========================================================================

    [TabGroup("Graphics")]
    [Tooltip("기본 QualitySettings 인덱스(0=Lowest 부터).")]
    [SerializeField] private int _defaultQualityLevel = 2;

    [TabGroup("Graphics")]
    [Tooltip("기본 풀스크린 모드.")]
    [SerializeField] private SettingsData.ScreenMode _defaultScreenMode = SettingsData.ScreenMode.EXCLUSIVE;

    [TabGroup("Graphics")]
    [Tooltip("기본 목표 프레임레이트(-1이면 무제한).")]
    [SerializeField] private int _defaultTargetFrameRate = 60;

    [TabGroup("Graphics")]
    [Tooltip("기본 VSync 사용 여부.")]
    [SerializeField] private bool _defaultVSync;

    [TabGroup("Graphics")]
    [Tooltip("기본 안티에일리어싱.")]
    [SerializeField] private SettingsData.AntiAliasingMode _defaultAntiAliasing = SettingsData.AntiAliasingMode.FXAA;

    [TabGroup("Graphics")]
    [Tooltip("기본 그림자 품질.")]
    [SerializeField] private SettingsData.ShadowLevel _defaultShadowLevel = SettingsData.ShadowLevel.MEDIUM;

    [TabGroup("Graphics")]
    [Tooltip("기본 텍스처 품질.")]
    [SerializeField] private SettingsData.TextureLevel _defaultTextureLevel = SettingsData.TextureLevel.HIGH;

    [TabGroup("Graphics")]
    [Tooltip("기본 밝기(1.0 = 기본).")]
    [SerializeField] private float _defaultBrightness = 1f;

    [TabGroup("Graphics")]
    [Tooltip("밝기 슬라이더 하한.")]
    [SerializeField] private float _brightnessMin = 0.5f;

    [TabGroup("Graphics")]
    [Tooltip("밝기 슬라이더 상한.")]
    [SerializeField] private float _brightnessMax = 1.5f;

    public int DefaultQualityLevel => _defaultQualityLevel;
    public SettingsData.ScreenMode DefaultScreenMode => _defaultScreenMode;
    public int DefaultTargetFrameRate => _defaultTargetFrameRate;
    public bool DefaultVSync => _defaultVSync;
    public SettingsData.AntiAliasingMode DefaultAntiAliasing => _defaultAntiAliasing;
    public SettingsData.ShadowLevel DefaultShadowLevel => _defaultShadowLevel;
    public SettingsData.TextureLevel DefaultTextureLevel => _defaultTextureLevel;
    public float DefaultBrightness => _defaultBrightness;
    public float BrightnessMin => _brightnessMin;
    public float BrightnessMax => _brightnessMax;

    //==========================================================================
    // [2] 오디오 기본값
    //==========================================================================

    [TabGroup("Audio")]
    [Tooltip("기본 마스터 볼륨(0~1).")]
    [Range(0f, 1f)]
    [SerializeField] private float _defaultMasterVolume = 1f;

    [TabGroup("Audio")]
    [Tooltip("기본 BGM 볼륨(0~1).")]
    [Range(0f, 1f)]
    [SerializeField] private float _defaultBgmVolume = 1f;

    [TabGroup("Audio")]
    [Tooltip("기본 SFX 볼륨(0~1).")]
    [Range(0f, 1f)]
    [SerializeField] private float _defaultSfxVolume = 1f;

    [TabGroup("Audio")]
    [Tooltip("기본 대사 볼륨(0~1).")]
    [Range(0f, 1f)]
    [SerializeField] private float _defaultVoiceVolume = 1f;

    [TabGroup("Audio")]
    [Tooltip("기본 UI 볼륨(0~1).")]
    [Range(0f, 1f)]
    [SerializeField] private float _defaultUiVolume = 1f;

    [TabGroup("Audio")]
    [Tooltip("기본 환경음 볼륨(0~1).")]
    [Range(0f, 1f)]
    [SerializeField] private float _defaultAmbientVolume = 1f;

    [TabGroup("Audio")]
    [Tooltip("기본 음소거 여부.")]
    [SerializeField] private bool _defaultMute;

    [TabGroup("Audio")]
    [Tooltip("기본: 창이 비활성화되면 자동 음소거.")]
    [SerializeField] private bool _defaultMuteOnFocusLost = true;

    public float DefaultMasterVolume => _defaultMasterVolume;
    public float DefaultBgmVolume => _defaultBgmVolume;
    public float DefaultSfxVolume => _defaultSfxVolume;
    public float DefaultVoiceVolume => _defaultVoiceVolume;
    public float DefaultUiVolume => _defaultUiVolume;
    public float DefaultAmbientVolume => _defaultAmbientVolume;
    public bool DefaultMute => _defaultMute;
    public bool DefaultMuteOnFocusLost => _defaultMuteOnFocusLost;

    //==========================================================================
    // [3] 카메라/입력 기본값 / 범위
    //==========================================================================

    [TabGroup("Camera")]
    [Tooltip("기본 X축 감도.")]
    [SerializeField] private float _defaultSensitivityX = 1f;

    [TabGroup("Camera")]
    [Tooltip("기본 Y축 감도.")]
    [SerializeField] private float _defaultSensitivityY = 1f;

    [TabGroup("Camera")]
    [Tooltip("감도 슬라이더 하한.")]
    [SerializeField] private float _sensitivityMin = 0.1f;

    [TabGroup("Camera")]
    [Tooltip("감도 슬라이더 상한.")]
    [SerializeField] private float _sensitivityMax = 5f;

    [TabGroup("Camera")]
    [Tooltip("기본 조준 시 감도 배수(0.05 ~ 2.0).")]
    [SerializeField] private float _defaultAimSensitivityMultiplier = 0.5f;

    [TabGroup("Camera")]
    [Tooltip("기본 X축 반전 여부.")]
    [SerializeField] private bool _defaultInvertX;

    [TabGroup("Camera")]
    [Tooltip("기본 Y축 반전 여부.")]
    [SerializeField] private bool _defaultInvertY;

    [TabGroup("Camera")]
    [Tooltip("기본 마우스 스무딩 여부.")]
    [SerializeField] private bool _defaultMouseSmoothing;

    public float DefaultSensitivityX => _defaultSensitivityX;
    public float DefaultSensitivityY => _defaultSensitivityY;
    public float SensitivityMin => _sensitivityMin;
    public float SensitivityMax => _sensitivityMax;
    public float DefaultAimSensitivityMultiplier => _defaultAimSensitivityMultiplier;
    public bool DefaultInvertX => _defaultInvertX;
    public bool DefaultInvertY => _defaultInvertY;
    public bool DefaultMouseSmoothing => _defaultMouseSmoothing;

    //==========================================================================
    // [4] 게임플레이 기본값
    //==========================================================================

    [TabGroup("Gameplay")]
    [Tooltip("기본 언어(ISO 639-1).")]
    [SerializeField] private string _defaultLanguage = "ko";

    [TabGroup("Gameplay")]
    [Tooltip("기본 난이도.")]
    [SerializeField] private SettingsData.DifficultyLevel _defaultDifficulty = SettingsData.DifficultyLevel.NORMAL;

    [TabGroup("Gameplay")]
    [Tooltip("기본 FPS 표시 여부.")]
    [SerializeField] private bool _defaultShowFps;

    public string DefaultLanguage => _defaultLanguage;
    public SettingsData.DifficultyLevel DefaultDifficulty => _defaultDifficulty;
    public bool DefaultShowFps => _defaultShowFps;

    //==========================================================================
    // [5] 기본값 → SettingsData 생성
    //==========================================================================

    /*
     * @brief 본 Config의 기본값을 채운 SettingsData 인스턴스를 새로 만든다
     * @return 기본값으로 초기화된 SettingsData (현재 스키마 버전)
     */
    public SettingsData CreateDefaultData()
    {
        SettingsData data = new SettingsData
        {
            Version = SettingsData.CURRENT_VERSION,

            GraphicsSetting = new SettingsData.Graphics
            {
                QualityLevel = _defaultQualityLevel,
                ScreenModeValue = _defaultScreenMode,
                ResolutionIndex = -1,
                Width = 0,
                Height = 0,
                RefreshRate = 0,
                TargetFrameRate = _defaultTargetFrameRate,
                VSync = _defaultVSync,
                AntiAliasing = _defaultAntiAliasing,
                ShadowQuality = _defaultShadowLevel,
                TextureQuality = _defaultTextureLevel,
                Brightness = _defaultBrightness,
            },
            AudioSetting = new SettingsData.Audio
            {
                MasterVolume = _defaultMasterVolume,
                BgmVolume = _defaultBgmVolume,
                SfxVolume = _defaultSfxVolume,
                VoiceVolume = _defaultVoiceVolume,
                UiVolume = _defaultUiVolume,
                AmbientVolume = _defaultAmbientVolume,
                Mute = _defaultMute,
                MuteOnFocusLost = _defaultMuteOnFocusLost,
            },
            CameraSetting = new SettingsData.Camera
            {
                SensitivityX = _defaultSensitivityX,
                SensitivityY = _defaultSensitivityY,
                AimSensitivityMultiplier = _defaultAimSensitivityMultiplier,
                InvertX = _defaultInvertX,
                InvertY = _defaultInvertY,
                MouseSmoothing = _defaultMouseSmoothing,
            },
            GameplaySetting = new SettingsData.Gameplay
            {
                Language = _defaultLanguage,
                Difficulty = _defaultDifficulty,
                ShowFps = _defaultShowFps,
            },
        };

        data.ClampWith(this);
        return data;
    }
}
