using UnityEngine;

//==============================================================================
// FadeInController: 왼쪽 -> 오른쪽
//==============================================================================

public class FadeInController : FadeBaseController
{
    // 왼쪽에서 오른쪽으로 진행하기
    protected override bool FromLeftToRight => true;

    // 되돌리기 아니기
    protected override bool IsBack => false;
}

