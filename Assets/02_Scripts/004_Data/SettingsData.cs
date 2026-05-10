using System;
using UnityEngine;

//==============================================================================
// SettingsData: 게임 설정의 런타임/저장용 데이터
//==============================================================================
// - JSON 직렬화 대상 (Application.persistentDataPath/settings.json 등)
// - 기본값은 SettingsConfig(SO)에서 복제해 채운다
// - Unity 의존도를 낮춰 테스트/확장이 용이하도록 [Serializable] 클래스로 작성
// - 카테고리는 SettingsData 안의 중첩 클래스로 분리
//   UnityEngine.AudioSettings 등과의 이름 충돌 방지 + 책임 명확화
// - Version + 마이그레이션 로직으로 향후 스키마 변경에 대응
//==============================================================================

[Serializable]
public class SettingsData
{
    //==========================================================================
    // [0] 스키마 버전
    //==========================================================================

    // 현재 코드가 다루는 스키마 버전
    private int _currentVersion = 1;

    // 설정 데이터 범위 보정 기능
    private SettingsClamp _settingsClamp = new SettingsClamp();

    // 설정 데이터 마이그레이션 기능
    private SettingsMigrate _settingsMigrate = new SettingsMigrate();

    // 저장된 데이터의 스키마 버전
    [SerializeField] private int _version = 1;

    // 현재 코드가 다루는 스키마 버전 getter
    public int CurrentVersion { get => _currentVersion; }

    // 저장된 데이터의 스키마 버전 getter/setter
    public int Version { get => _version; set => _version = value; }

    //--------------------------------------------------------------------------
    // [1] 카테고리별 데이터(중첩 클래스 인스턴스)
    //--------------------------------------------------------------------------

    // 그래픽 설정 묶음
    [SerializeField] private Graphics _graphics = new Graphics();

    // 오디오 설정 묶음
    [SerializeField] private Audio _audio = new Audio();

    // 카메라/입력 설정 묶음
    [SerializeField] private Camera _camera = new Camera();

    // 게임플레이 설정 묶음
    [SerializeField] private Gameplay _gameplay = new Gameplay();

    public Graphics GraphicsSetting { get => _graphics; set => _graphics = value; }
    public Audio AudioSetting { get => _audio; set => _audio = value; }
    public Camera CameraSetting { get => _camera; set => _camera = value; }
    public Gameplay GameplaySetting { get => _gameplay; set => _gameplay = value; }

    //--------------------------------------------------------------------------
    // [2] 유효성 보정
    //--------------------------------------------------------------------------

    //==============================================================================
    // 기능: SettingsData::ClampWith
    // 설명: 모든 카테고리의 값이 SettingsConfig 허용 범위 안에 있도록 보정한다.
    //==============================================================================
    // TODO: 보정된 필드 목록 반환
    //
    //==============================================================================
    // config: 기본값/범위가 정의된 SettingsConfig
    public void ClampWith(SettingsConfig config)
    {
        // 범위 보정 책임은 SettingsClamp 로 분리
        _settingsClamp.Apply(this, config);
    }

    //--------------------------------------------------------------------------
    // [3] 마이그레이션
    //--------------------------------------------------------------------------

    //==============================================================================
    // 기능: SettingsData::MigrateToCurrent
    // 설명: 저장된 구버전 데이터를 현재 스키마로 끌어올리고 변경 여부를 반환한다.
    //==============================================================================
    // TODO: v2 이후 마이그레이션 단계 추가
    //
    //==============================================================================
    public bool MigrateToCurrent()
    {
        // 마이그레이션 책임은 SettingsMigrate 로 분리
        return _settingsMigrate.ToCurrent(this);
    }

    //==========================================================================
    // [4] Enum 정의
    //==========================================================================

    /// <summary>풀스크린 모드.</summary>
    public enum ScreenMode
    {
        WINDOWED,
        BORDERLESS,
        EXCLUSIVE,
    }

    /// <summary>안티에일리어싱 모드.</summary>
    public enum AntiAliasingMode
    {
        NONE,
        FXAA,
        SMAA,
        MSAA_2X,
        MSAA_4X,
        MSAA_8X,
    }

    /// <summary>그림자 품질.</summary>
    public enum ShadowLevel
    {
        OFF,
        LOW,
        MEDIUM,
        HIGH,
    }

    /// <summary>텍스처 품질.</summary>
    public enum TextureLevel
    {
        LOW,
        MEDIUM,
        HIGH,
        ULTRA,
    }

    /// <summary>난이도.</summary>
    public enum DifficultyLevel
    {
        EASY,
        NORMAL,
        HARD,
    }

    //==========================================================================
    // [5] 그래픽 카테고리
    //==========================================================================

    [Serializable]
    public class Graphics
    {
        /// <summary>QualitySettings 인덱스(0=Lowest 부터).</summary>
        [SerializeField] private int _qualityLevel;

        /// <summary>풀스크린 모드.</summary>
        [SerializeField] private ScreenMode _screenMode = ScreenMode.EXCLUSIVE;

        /// <summary>Screen.resolutions 배열의 인덱스(-1이면 W/H 폴백).</summary>
        [SerializeField] private int _resolutionIndex = -1;

        /// <summary>해상도 폭(px). ResolutionIndex가 -1일 때 사용.</summary>
        [SerializeField] private int _width;

        /// <summary>해상도 높이(px). ResolutionIndex가 -1일 때 사용.</summary>
        [SerializeField] private int _height;

        /// <summary>리프레시 레이트(Hz). 0이면 미지정.</summary>
        [SerializeField] private int _refreshRate;

        /// <summary>목표 프레임레이트(-1이면 무제한).</summary>
        [SerializeField] private int _targetFrameRate = 60;

        /// <summary>수직 동기화(VSync) 사용 여부.</summary>
        [SerializeField] private bool _vSync;

        /// <summary>안티에일리어싱.</summary>
        [SerializeField] private AntiAliasingMode _antiAliasing = AntiAliasingMode.FXAA;

        /// <summary>그림자 품질.</summary>
        [SerializeField] private ShadowLevel _shadowLevel = ShadowLevel.MEDIUM;

        /// <summary>텍스처 품질.</summary>
        [SerializeField] private TextureLevel _textureLevel = TextureLevel.HIGH;

        /// <summary>밝기/감마(0.5 ~ 1.5 권장). 1.0이 기본값.</summary>
        [SerializeField] private float _brightness = 1f;

        public int QualityLevel { get => _qualityLevel; set => _qualityLevel = value; }
        public ScreenMode ScreenModeValue { get => _screenMode; set => _screenMode = value; }
        public int ResolutionIndex { get => _resolutionIndex; set => _resolutionIndex = value; }
        public int Width { get => _width; set => _width = value; }
        public int Height { get => _height; set => _height = value; }
        public int RefreshRate { get => _refreshRate; set => _refreshRate = value; }
        public int TargetFrameRate { get => _targetFrameRate; set => _targetFrameRate = value; }
        public bool VSync { get => _vSync; set => _vSync = value; }
        public AntiAliasingMode AntiAliasing { get => _antiAliasing; set => _antiAliasing = value; }
        public ShadowLevel ShadowQuality { get => _shadowLevel; set => _shadowLevel = value; }
        public TextureLevel TextureQuality { get => _textureLevel; set => _textureLevel = value; }
        public float Brightness { get => _brightness; set => _brightness = value; }

        /// <summary>Config 범위에 맞춰 값 보정.</summary>
        public void ClampWith(SettingsConfig config)
        {
            int maxQuality = Mathf.Max(0, QualitySettings.names.Length - 1);
            _qualityLevel = Mathf.Clamp(_qualityLevel, 0, maxQuality);

            // -1(무제한)은 허용, 그 외에는 양수만
            if (_targetFrameRate != -1 && _targetFrameRate < 1) _targetFrameRate = 60;

            if (config != null)
                _brightness = Mathf.Clamp(_brightness, config.BrightnessMin, config.BrightnessMax);
            else
                _brightness = Mathf.Clamp(_brightness, 0.1f, 3f);
        }
    }

    //==========================================================================
    // [6] 오디오 카테고리
    //==========================================================================

    [Serializable]
    public class Audio
    {
        /// <summary>마스터 볼륨(0~1).</summary>
        [SerializeField] private float _masterVolume = 1f;

        /// <summary>BGM 볼륨(0~1).</summary>
        [SerializeField] private float _bgmVolume = 1f;

        /// <summary>SFX 볼륨(0~1).</summary>
        [SerializeField] private float _sfxVolume = 1f;

        /// <summary>대사/내레이션 볼륨(0~1).</summary>
        [SerializeField] private float _voiceVolume = 1f;

        /// <summary>UI 효과음 볼륨(0~1).</summary>
        [SerializeField] private float _uiVolume = 1f;

        /// <summary>환경음 볼륨(0~1).</summary>
        [SerializeField] private float _ambientVolume = 1f;

        /// <summary>전체 음소거 여부.</summary>
        [SerializeField] private bool _mute;

        /// <summary>창이 비활성화되면 자동 음소거.</summary>
        [SerializeField] private bool _muteOnFocusLost = true;

        public float MasterVolume { get => _masterVolume; set => _masterVolume = value; }
        public float BgmVolume { get => _bgmVolume; set => _bgmVolume = value; }
        public float SfxVolume { get => _sfxVolume; set => _sfxVolume = value; }
        public float VoiceVolume { get => _voiceVolume; set => _voiceVolume = value; }
        public float UiVolume { get => _uiVolume; set => _uiVolume = value; }
        public float AmbientVolume { get => _ambientVolume; set => _ambientVolume = value; }
        public bool Mute { get => _mute; set => _mute = value; }
        public bool MuteOnFocusLost { get => _muteOnFocusLost; set => _muteOnFocusLost = value; }

        /// <summary>Config 범위에 맞춰 값 보정.</summary>
        public void ClampWith(SettingsConfig config)
        {
            _masterVolume = Mathf.Clamp01(_masterVolume);
            _bgmVolume = Mathf.Clamp01(_bgmVolume);
            _sfxVolume = Mathf.Clamp01(_sfxVolume);
            _voiceVolume = Mathf.Clamp01(_voiceVolume);
            _uiVolume = Mathf.Clamp01(_uiVolume);
            _ambientVolume = Mathf.Clamp01(_ambientVolume);
        }
    }

    //==========================================================================
    // [7] 카메라/입력 카테고리
    //==========================================================================

    [Serializable]
    public class Camera
    {
        // ----- 신규 필드 -----

        /// <summary>X축(좌우) 회전 감도.</summary>
        [SerializeField] private float _sensitivityX = 1f;

        /// <summary>Y축(상하) 회전 감도.</summary>
        [SerializeField] private float _sensitivityY = 1f;

        /// <summary>조준/줌 상태에서 적용되는 감도 배수.</summary>
        [SerializeField] private float _aimSensitivityMultiplier = 0.5f;

        /// <summary>X축 반전 여부.</summary>
        [SerializeField] private bool _invertX;

        /// <summary>Y축 반전 여부.</summary>
        [SerializeField] private bool _invertY;

        /// <summary>마우스 입력 스무딩.</summary>
        [SerializeField] private bool _mouseSmoothing;

        // ----- 레거시(마이그레이션용, JSON 호환) -----

        /// <summary>[Legacy v0] 단일 감도. v1 마이그레이션 후 -1로 클리어됨.</summary>
        [HideInInspector]
        [SerializeField] private float _sensitivity = -1f;

        public float SensitivityX { get => _sensitivityX; set => _sensitivityX = value; }
        public float SensitivityY { get => _sensitivityY; set => _sensitivityY = value; }
        public float AimSensitivityMultiplier { get => _aimSensitivityMultiplier; set => _aimSensitivityMultiplier = value; }
        public bool InvertX { get => _invertX; set => _invertX = value; }
        public bool InvertY { get => _invertY; set => _invertY = value; }
        public bool MouseSmoothing { get => _mouseSmoothing; set => _mouseSmoothing = value; }

        /// <summary>마이그레이션 전용: 레거시 _sensitivity 값 조회.</summary>
        public float LegacySensitivity => _sensitivity;

        /// <summary>마이그레이션 전용: 레거시 값 비활성화(-1).</summary>
        public void ClearLegacySensitivity() => _sensitivity = -1f;

        /// <summary>Config 범위에 맞춰 값 보정.</summary>
        public void ClampWith(SettingsConfig config)
        {
            if (config == null) return;

            _sensitivityX = Mathf.Clamp(_sensitivityX, config.SensitivityMin, config.SensitivityMax);
            _sensitivityY = Mathf.Clamp(_sensitivityY, config.SensitivityMin, config.SensitivityMax);
            _aimSensitivityMultiplier = Mathf.Clamp(_aimSensitivityMultiplier, 0.05f, 2f);
        }
    }

    //==========================================================================
    // [8] 게임플레이 카테고리
    //==========================================================================

    [Serializable]
    public class Gameplay
    {
        /// <summary>언어 코드(ISO 639-1: "ko", "en", "ja"...).</summary>
        [SerializeField] private string _language = "ko";

        /// <summary>난이도(게임 시스템 연결 전이라면 단순 저장값).</summary>
        [SerializeField] private DifficultyLevel _difficulty = DifficultyLevel.NORMAL;

        /// <summary>FPS 카운터 표시 여부.</summary>
        [SerializeField] private bool _showFps;

        public string Language { get => _language; set => _language = value; }
        public DifficultyLevel Difficulty { get => _difficulty; set => _difficulty = value; }
        public bool ShowFps { get => _showFps; set => _showFps = value; }

        /// <summary>Config 범위에 맞춰 값 보정.</summary>
        public void ClampWith(SettingsConfig config)
        {
            if (string.IsNullOrEmpty(_language)) _language = "ko";
        }
    }
}
