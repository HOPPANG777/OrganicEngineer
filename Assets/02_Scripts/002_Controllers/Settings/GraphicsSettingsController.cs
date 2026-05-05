using Sirenix.OdinInspector;
using UnityEngine;

//==============================================================================
// GraphicsSettingsController: 그래픽 설정 적용 + 해상도 헬퍼 책임
//==============================================================================
// - SettingsController의 자식 MonoBehaviour
// - SettingsData.Graphics를 받아 QualitySettings/Screen 등에 반영
// - 해상도 목록 조회/라벨 생성 등 옵션 UI 보조 메서드 제공
//==============================================================================

public class GraphicsSettingsController : MonoBehaviour
{
    //==========================================================================
    // [1] 시스템 적용
    //==========================================================================

    /// <summary>그래픽 설정을 Unity 시스템에 반영.</summary>
    public void Apply(SettingsData.Graphics g)
    {
        if (g == null) return;

        // 품질 레벨
        int maxQuality = Mathf.Max(0, QualitySettings.names.Length - 1);
        int quality = Mathf.Clamp(g.QualityLevel, 0, maxQuality);
        QualitySettings.SetQualityLevel(quality, applyExpensiveChanges: true);

        // VSync (1 = Every V Blank, 0 = Don't Sync)
        QualitySettings.vSyncCount = g.VSync ? 1 : 0;

        // 프레임레이트 (-1이면 무제한)
        Application.targetFrameRate = g.TargetFrameRate;

        // 안티에일리어싱 (MSAA만 QualitySettings로 처리. FXAA/SMAA는 URP/HDRP Volume 영역)
        QualitySettings.antiAliasing = ToMsaaSamples(g.AntiAliasing);

        // 그림자 품질
        ApplyShadowQuality(g.ShadowQuality);

        // 텍스처 품질
        ApplyTextureQuality(g.TextureQuality);

        // 해상도 / 풀스크린
        ApplyResolution(g);

        // 밝기는 게임 측 셰이더/포스트프로세싱에 의존하므로 여기서는 값만 보관
        // 소비자는 SettingsController.OnSettingsApplied 이벤트에서 g.Brightness 를 읽어 적용
    }

    //--------------------------------------------------------------------------
    // [1-1] 매핑 헬퍼
    //--------------------------------------------------------------------------

    /// <summary>커스텀 ScreenMode → UnityEngine.FullScreenMode 매핑.</summary>
    private static FullScreenMode ToUnityFullScreenMode(SettingsData.ScreenMode mode)
    {
        switch (mode)
        {
            case SettingsData.ScreenMode.WINDOWED:   return FullScreenMode.Windowed;
            case SettingsData.ScreenMode.BORDERLESS: return FullScreenMode.FullScreenWindow;
            case SettingsData.ScreenMode.EXCLUSIVE:  return FullScreenMode.ExclusiveFullScreen;
            default: return FullScreenMode.FullScreenWindow;
        }
    }

    /// <summary>커스텀 AA → MSAA 샘플 수(0/2/4/8). FXAA/SMAA는 0 반환.</summary>
    private static int ToMsaaSamples(SettingsData.AntiAliasingMode mode)
    {
        switch (mode)
        {
            case SettingsData.AntiAliasingMode.MSAA_2X: return 2;
            case SettingsData.AntiAliasingMode.MSAA_4X: return 4;
            case SettingsData.AntiAliasingMode.MSAA_8X: return 8;
            default: return 0;
        }
    }

    /// <summary>그림자 레벨을 QualitySettings에 반영.</summary>
    private static void ApplyShadowQuality(SettingsData.ShadowLevel level)
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

    /// <summary>텍스처 품질을 QualitySettings에 반영(낮을수록 다운샘플).</summary>
    private static void ApplyTextureQuality(SettingsData.TextureLevel level)
    {
        // globalTextureMipmapLimit: 0=Full, 1=Half, 2=Quarter, 3=Eighth
        int mipLimit;
        switch (level)
        {
            case SettingsData.TextureLevel.LOW:    mipLimit = 2; break;
            case SettingsData.TextureLevel.MEDIUM: mipLimit = 1; break;
            case SettingsData.TextureLevel.HIGH:   mipLimit = 0; break;
            case SettingsData.TextureLevel.ULTRA:  mipLimit = 0; break;
            default: mipLimit = 0; break;
        }

#if UNITY_2022_2_OR_NEWER
        QualitySettings.globalTextureMipmapLimit = mipLimit;
#else
        QualitySettings.masterTextureLimit = mipLimit;
#endif
    }

    /// <summary>해상도/풀스크린 적용. ResolutionIndex 우선, 실패 시 W/H 폴백.</summary>
    private static void ApplyResolution(SettingsData.Graphics g)
    {
        FullScreenMode unityMode = ToUnityFullScreenMode(g.ScreenModeValue);
        Resolution[] resolutions = Screen.resolutions;

        if (g.ResolutionIndex >= 0 && g.ResolutionIndex < resolutions.Length)
        {
            Resolution r = resolutions[g.ResolutionIndex];
            // Resolution.refreshRateRatio는 RefreshRate 구조체(분자/분모)이므로 그대로 사용
            Screen.SetResolution(r.width, r.height, unityMode, r.refreshRateRatio);
            return;
        }

        if (g.Width > 0 && g.Height > 0)
        {
            if (g.RefreshRate > 0)
            {
                Screen.SetResolution(g.Width, g.Height, unityMode, ToRefreshRate(g.RefreshRate));
            }
            else
            {
                // RefreshRate 미지정 시 모니터 기본 리프레시 사용
                Screen.SetResolution(g.Width, g.Height, unityMode);
            }
            return;
        }

        // 폴백: 풀스크린 모드만 변경
        Screen.fullScreenMode = unityMode;
    }

    /// <summary>정수 Hz → UnityEngine.RefreshRate 구조체 변환(분모 1로 고정).</summary>
    private static RefreshRate ToRefreshRate(int hz)
    {
        return new RefreshRate
        {
            numerator = (uint)Mathf.Max(1, hz),
            denominator = 1u,
        };
    }

    //==========================================================================
    // [2] 해상도 목록 조회 (옵션 UI에서 사용)
    //==========================================================================

    /*
     * @brief 현재 디스플레이가 지원하는 해상도 목록을 반환한다
     * @return Screen.resolutions 그대로(인덱스 = ResolutionIndex 의미)
     * @note 빌드/플랫폼/모니터에 따라 결과가 달라진다
     */
    public Resolution[] GetAvailableResolutions()
    {
        return Screen.resolutions;
    }

    /*
     * @brief 옵션 UI 드롭다운에 그대로 넣을 수 있는 문자열 라벨 배열
     * @return 예: "1920 x 1080 @ 60Hz"
     */
    public string[] GetResolutionLabels()
    {
        Resolution[] resolutions = Screen.resolutions;
        string[] labels = new string[resolutions.Length];
        for (int i = 0; i < resolutions.Length; i++)
        {
            Resolution r = resolutions[i];
            labels[i] = $"{r.width} x {r.height} @ {Mathf.RoundToInt((float)r.refreshRateRatio.value)}Hz";
        }
        return labels;
    }

    /// <summary>현재 화면 해상도와 가장 가까운 Screen.resolutions 인덱스(없으면 -1).</summary>
    public int GetCurrentResolutionIndex()
    {
        Resolution[] resolutions = Screen.resolutions;
        for (int i = 0; i < resolutions.Length; i++)
        {
            if (resolutions[i].width == Screen.width && resolutions[i].height == Screen.height)
                return i;
        }
        return -1;
    }

    /*
     * @brief 옵션 UI에서 해상도가 선택될 때 Index와 W/H를 동시 업데이트
     * @param g 대상 Graphics 데이터
     * @param index Screen.resolutions의 인덱스
     */
    public void SetResolutionByIndex(SettingsData.Graphics g, int index)
    {
        if (g == null) return;

        Resolution[] resolutions = Screen.resolutions;
        if (index < 0 || index >= resolutions.Length) return;

        Resolution r = resolutions[index];
        g.ResolutionIndex = index;
        g.Width = r.width;
        g.Height = r.height;
        g.RefreshRate = Mathf.RoundToInt((float)r.refreshRateRatio.value);
    }

    //--------------------------------------------------------------------------
    // [2-1] 디버그: 인스펙터에서 해상도 목록 즉시 확인용
    //--------------------------------------------------------------------------

    /// <summary>현재 디스플레이가 지원하는 해상도 목록을 콘솔에 출력(에디터/빌드 모두 동작).</summary>
    [Button("해상도 목록 출력")]
    private void DebugLogResolutions()
    {
        Resolution[] resolutions = Screen.resolutions;
        if (resolutions == null || resolutions.Length == 0)
        {
            Debug.Log("[GraphicsSettingsController] 사용 가능한 해상도가 없습니다.");
            return;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"[GraphicsSettingsController] 해상도 {resolutions.Length}개:");
        for (int i = 0; i < resolutions.Length; i++)
        {
            Resolution r = resolutions[i];
            sb.AppendLine($"  [{i}] {r.width} x {r.height} @ {(float)r.refreshRateRatio.value:F2}Hz");
        }
        Debug.Log(sb.ToString());
    }
}
