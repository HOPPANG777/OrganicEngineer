using UnityEngine;
using UnityEngine.UI;

//==============================================================================
// 기능: FadeView
// 설명: 페이드 Image fill 또는 RectTransform 이동 값을 실제 UI에 적용한다.
//==============================================================================
// TODO: 세로 페이드, 원형 페이드 방식 추가
//
//==============================================================================
public class FadeView
{
    //==============================================================================
    // 기능: FadeView::Apply
    // 설명: Image 가 있으면 fillAmount, 없으면 RectTransform 위치를 갱신한다.
    //==============================================================================
    // TODO: CanvasGroup alpha 방식 지원
    //
    //==============================================================================
    public void Apply(Image image, RectTransform panel, Vector2 startPos, Vector2 endPos, float fillValue, float curveValue)
    {
        // Image 방식이면 fillAmount 로 페이드 처리
        if (image != null)
        {
            image.fillAmount = fillValue;
            return;
        }

        // Image 가 없으면 패널 이동 방식으로 페이드 처리
        if (panel != null)
        {
            panel.anchoredPosition = Vector2.Lerp(startPos, endPos, curveValue);
        }
    }

    //==============================================================================
    // 기능: FadeView::SetupStartValue
    // 설명: 페이드 시작 시 Image fill 또는 RectTransform 이동 시작/끝 값을 준비한다.
    //==============================================================================
    // TODO: RectTransform width 계산 캐싱
    //
    //==============================================================================
    public void SetupStartValue(RectTransform panel, Image image, bool fromLeftToRight, bool isBack, out Vector2 startPos, out Vector2 endPos)
    {
        // 기본 위치 값 초기화
        startPos = Vector2.zero;
        endPos = Vector2.zero;

        // Image 방식이면 fill 설정만 준비
        if (image != null)
        {
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = fromLeftToRight ? 0 : 1;
            image.fillAmount = isBack ? 1f : 0f;
            return;
        }

        // 패널이 없으면 이동 방식도 처리 불가
        if (panel == null) return;

        // 부모 RectTransform 폭을 기준으로 화면 밖 위치 계산
        RectTransform parent = panel.parent as RectTransform;
        float width = parent != null ? parent.rect.width : Screen.width;

        // 되돌리기면 중앙에서 밖으로 이동
        if (isBack)
        {
            startPos = Vector2.zero;
            endPos = fromLeftToRight ? new Vector2(-width, 0f) : new Vector2(width, 0f);
        }
        // 일반 페이드면 밖에서 중앙으로 이동
        else
        {
            startPos = fromLeftToRight ? new Vector2(-width, 0f) : new Vector2(width, 0f);
            endPos = Vector2.zero;
        }

        // 시작 위치 즉시 적용
        panel.anchoredPosition = startPos;
    }
}
