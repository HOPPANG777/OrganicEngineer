//==============================================================================
// 기능: FadeBaseController
// 설명: 페이드 시작 UI 값과 입력 차단 적용을 담당한다.
//==============================================================================
// TODO: 입력 차단 정책 세분화
//
//==============================================================================
public abstract partial class FadeBaseController
{
    // 시작 상태를 세팅하기
    protected void SetupStartValue()
    {
        // 시작 값 계산과 UI 세팅은 FadeView 로 분리
        _fadeView.SetupStartValue(_fadePanel, _fadeImage, FromLeftToRight, IsBack, out _startPos, out _endPos);
    }

    // 페이드 중 입력을 막기
    protected void StartInputBlock()
    {
        // 입력 차단 적용은 FadeBlock 으로 분리
        _fadeBlock.Set(_fadeImage, GetBlockRaycastDuringFade(), true);
    }

    // 페이드가 끝나면 입력 막기를 풀기
    protected void StopInputBlock()
    {
        // 입력 차단 해제는 FadeBlock 으로 분리
        _fadeBlock.Set(_fadeImage, GetBlockRaycastDuringFade(), false);
    }
}
