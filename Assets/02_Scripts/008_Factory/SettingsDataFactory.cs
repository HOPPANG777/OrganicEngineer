//==============================================================================
// 기능: SettingsDataFactory
// 설명: SettingsConfig 의 기본값을 이용해 SettingsData 기본 인스턴스를 생성한다.
//==============================================================================
// TODO: 카테고리별 Factory 분리
//
//==============================================================================
public class SettingsDataFactory
{
    //==============================================================================
    // 기능: SettingsDataFactory::CreateDefault
    // 설명: Config 기본값을 모든 SettingsData 카테고리에 복사하고 Clamp 후 반환한다.
    //==============================================================================
    // TODO: 현재 화면 해상도 기반 기본 ResolutionIndex 설정
    //
    //==============================================================================
    public SettingsData CreateDefault(SettingsConfig config)
    {
        // Config 가 없으면 빈 데이터 반환
        if (config == null) return new SettingsData();

        // 기본값으로 새 데이터 생성
        SettingsData data = new SettingsData
        {
            Version = new SettingsData().CurrentVersion,

            GraphicsSetting = new SettingsData.Graphics
            {
                QualityLevel = config.DefaultQualityLevel,
                ScreenModeValue = config.DefaultScreenMode,
                ResolutionIndex = -1,
                Width = 0,
                Height = 0,
                RefreshRate = 0,
                TargetFrameRate = config.DefaultTargetFrameRate,
                VSync = config.DefaultVSync,
                AntiAliasing = config.DefaultAntiAliasing,
                ShadowQuality = config.DefaultShadowLevel,
                TextureQuality = config.DefaultTextureLevel,
                Brightness = config.DefaultBrightness,
            },
            AudioSetting = new SettingsData.Audio
            {
                MasterVolume = config.DefaultMasterVolume,
                BgmVolume = config.DefaultBgmVolume,
                SfxVolume = config.DefaultSfxVolume,
                VoiceVolume = config.DefaultVoiceVolume,
                UiVolume = config.DefaultUiVolume,
                AmbientVolume = config.DefaultAmbientVolume,
                Mute = config.DefaultMute,
                MuteOnFocusLost = config.DefaultMuteOnFocusLost,
            },
            CameraSetting = new SettingsData.Camera
            {
                SensitivityX = config.DefaultSensitivityX,
                SensitivityY = config.DefaultSensitivityY,
                AimSensitivityMultiplier = config.DefaultAimSensitivityMultiplier,
                InvertX = config.DefaultInvertX,
                InvertY = config.DefaultInvertY,
                MouseSmoothing = config.DefaultMouseSmoothing,
            },
            GameplaySetting = new SettingsData.Gameplay
            {
                Language = config.DefaultLanguage,
                Difficulty = config.DefaultDifficulty,
                ShowFps = config.DefaultShowFps,
            },
        };

        // 기본값도 범위 보정
        data.ClampWith(config);

        return data;
    }
}
