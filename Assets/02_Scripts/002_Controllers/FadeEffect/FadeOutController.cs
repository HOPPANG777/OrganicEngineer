using UnityEngine;

//==============================================================================
// FadeOutController: 오른쪽 -> 왼쪽
//==============================================================================

public class FadeOutController : FadeBaseController
{
    // 왼쪽에서 오른쪽 기준으로 되돌리기
    protected override bool FromLeftToRight => true;

    // 되돌리기
    protected override bool IsBack => true;
}

