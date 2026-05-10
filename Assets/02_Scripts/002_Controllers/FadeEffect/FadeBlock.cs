using UnityEngine.UI;

//==============================================================================
// 기능: FadeBlock
// 설명: 페이드 중 UI 클릭/터치 입력 차단 여부를 Image.raycastTarget 에 적용한다.
//==============================================================================
// TODO: CanvasGroup.blocksRaycasts 방식 지원
//
//==============================================================================
public class FadeBlock
{
    //==============================================================================
    // 기능: FadeBlock::Set
    // 설명: 설정이 켜져 있을 때만 Image 의 raycastTarget 값을 바꾼다.
    //==============================================================================
    // TODO: 입력 차단 전용 Graphic 분리
    //
    //==============================================================================
    public void Set(Image image, bool blockRaycastDuringFade, bool value)
    {
        // Image 가 없으면 raycastTarget 을 조절할 수 없음
        if (image == null) return;

        // 설정이 꺼져 있으면 입력 차단을 건드리지 않음
        if (!blockRaycastDuringFade) return;

        // 페이드 상태에 맞춰 클릭 차단 여부 변경
        image.raycastTarget = value;
    }
}
