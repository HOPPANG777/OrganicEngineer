using UnityEngine;

//==============================================================================
// 기능: SceneLoadingView
// 설명: 씬 로딩 패널 표시/숨김만 담당한다.
//==============================================================================
// TODO: 로딩 진행률 텍스트/바 표시
//
//==============================================================================
public static class SceneLoadingView
{
    // 로딩 패널 표시 여부 적용
    public static void SetVisible(GameObject loadingPanel, bool visible)
    {
        // 패널이 없으면 처리하지 않음
        if (loadingPanel == null) return;

        // 요청 상태로 활성화 변경
        loadingPanel.SetActive(visible);
    }
}
