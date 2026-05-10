//==============================================================================
// 기능: SceneFade
// 설명: SceneFlow 의 FadeIn/FadeOut 호출을 담당한다.
//==============================================================================
// TODO: 페이드 컨트롤러 null 정책 세분화
//
//==============================================================================
public class SceneFade
{
    // 페이드 인 시작
    public void FadeIn(FadeEffectController fadeEffect, FadeEffectConfig fadeConfig)
    {
        // 페이드 컨트롤러 또는 설정이 없으면 처리 불가
        if (fadeEffect == null || fadeConfig == null) return;

        // 설정 시간으로 페이드 인
        fadeEffect.FadeIn(fadeConfig.FadeTime);
    }

    // 페이드 아웃 시작
    public void FadeOut(FadeEffectController fadeEffect, FadeEffectConfig fadeConfig)
    {
        // 페이드 컨트롤러 또는 설정이 없으면 처리 불가
        if (fadeEffect == null || fadeConfig == null) return;

        // 설정 시간으로 페이드 아웃
        fadeEffect.FadeOut(fadeConfig.FadeTime);
    }
}
