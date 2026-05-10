using UnityEngine;

//==============================================================================
// 기능: GraphicsApply
// 설명: SettingsData.Graphics 값을 Unity 그래픽 시스템(QualitySettings/Screen)에 적용한다.
//==============================================================================
// TODO: URP/HDRP 전용 AA/밝기 적용 지점 연결
//
//==============================================================================
public class GraphicsApply
{
    //==============================================================================
    // 기능: GraphicsApply::Apply
    // 설명: 품질, VSync, FPS, MSAA, 그림자, 텍스처, 해상도를 시스템에 반영한다.
    //==============================================================================
    // TODO: 변경된 항목만 적용하는 dirty flag 지원
    //
    //==============================================================================
    public void Apply(SettingsData.Graphics g)
    {
        // 그래픽 데이터가 없으면 적용 불가
        if (g == null) return;

        // 품질 레벨 범위 보정 후 적용
        int maxQuality = Mathf.Max(0, QualitySettings.names.Length - 1);
        int quality = Mathf.Clamp(g.QualityLevel, 0, maxQuality);
        QualitySettings.SetQualityLevel(quality, applyExpensiveChanges: true);

        // VSync 설정 적용
        QualitySettings.vSyncCount = g.VSync ? 1 : 0;

        // 목표 프레임레이트 적용
        Application.targetFrameRate = g.TargetFrameRate;

        // MSAA 샘플 수 적용
        QualitySettings.antiAliasing = ToMsaaSamples(g.AntiAliasing);

        // 그림자 품질 적용
        ApplyShadowQuality(g.ShadowQuality);

        // 텍스처 품질 적용
        ApplyTextureQuality(g.TextureQuality);

        // 해상도와 풀스크린 모드 적용
        ApplyResolution(g);
    }

    // 커스텀 ScreenMode 를 Unity FullScreenMode 로 변환
    private FullScreenMode ToUnityFullScreenMode(SettingsData.ScreenMode mode)
    {
        switch (mode)
        {
            case SettingsData.ScreenMode.WINDOWED: return FullScreenMode.Windowed;
            case SettingsData.ScreenMode.BORDERLESS: return FullScreenMode.FullScreenWindow;
            case SettingsData.ScreenMode.EXCLUSIVE: return FullScreenMode.ExclusiveFullScreen;
            default: return FullScreenMode.FullScreenWindow;
        }
    }

    // 커스텀 AA 설정을 MSAA 샘플 수로 변환
    private int ToMsaaSamples(SettingsData.AntiAliasingMode mode)
    {
        switch (mode)
        {
            case SettingsData.AntiAliasingMode.MSAA_2X: return 2;
            case SettingsData.AntiAliasingMode.MSAA_4X: return 4;
            case SettingsData.AntiAliasingMode.MSAA_8X: return 8;
            default: return 0;
        }
    }

    // 그림자 레벨을 QualitySettings 에 적용
    private void ApplyShadowQuality(SettingsData.ShadowLevel level)
    {
        switch (level)
        {
            case SettingsData.ShadowLevel.OFF:
                QualitySettings.shadows = UnityEngine.ShadowQuality.Disable;
                break;
            case SettingsData.ShadowLevel.LOW:
                QualitySettings.shadows = UnityEngine.ShadowQuality.HardOnly;
                QualitySettings.shadowResolution = ShadowResolution.Low;
                QualitySettings.shadowDistance = 30f;
                break;
            case SettingsData.ShadowLevel.MEDIUM:
                QualitySettings.shadows = UnityEngine.ShadowQuality.All;
                QualitySettings.shadowResolution = ShadowResolution.Medium;
                QualitySettings.shadowDistance = 60f;
                break;
            case SettingsData.ShadowLevel.HIGH:
                QualitySettings.shadows = UnityEngine.ShadowQuality.All;
                QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
                QualitySettings.shadowDistance = 120f;
                break;
        }
    }

    // 텍스처 품질을 QualitySettings 에 적용
    private void ApplyTextureQuality(SettingsData.TextureLevel level)
    {
        int mipLimit;
        switch (level)
        {
            case SettingsData.TextureLevel.LOW: mipLimit = 2; break;
            case SettingsData.TextureLevel.MEDIUM: mipLimit = 1; break;
            case SettingsData.TextureLevel.HIGH: mipLimit = 0; break;
            case SettingsData.TextureLevel.ULTRA: mipLimit = 0; break;
            default: mipLimit = 0; break;
        }

#if UNITY_2022_2_OR_NEWER
        QualitySettings.globalTextureMipmapLimit = mipLimit;
#else
        QualitySettings.masterTextureLimit = mipLimit;
#endif
    }

    // 해상도와 전체화면 모드 적용
    private void ApplyResolution(SettingsData.Graphics g)
    {
        FullScreenMode unityMode = ToUnityFullScreenMode(g.ScreenModeValue);
        Resolution[] resolutions = Screen.resolutions;

        // ResolutionIndex 가 유효하면 해당 해상도를 우선 사용
        if (g.ResolutionIndex >= 0 && g.ResolutionIndex < resolutions.Length)
        {
            Resolution r = resolutions[g.ResolutionIndex];
            Screen.SetResolution(r.width, r.height, unityMode, r.refreshRateRatio);
            return;
        }

        // 저장된 W/H 값이 있으면 폴백으로 사용
        if (g.Width > 0 && g.Height > 0)
        {
            if (g.RefreshRate > 0)
            {
                Screen.SetResolution(g.Width, g.Height, unityMode, ToRefreshRate(g.RefreshRate));
            }
            else
            {
                Screen.SetResolution(g.Width, g.Height, unityMode);
            }
            return;
        }

        // 해상도 정보가 없으면 풀스크린 모드만 변경
        Screen.fullScreenMode = unityMode;
    }

    // 정수 Hz 를 Unity RefreshRate 로 변환
    private RefreshRate ToRefreshRate(int hz)
    {
        return new RefreshRate
        {
            numerator = (uint)Mathf.Max(1, hz),
            denominator = 1u,
        };
    }
}
