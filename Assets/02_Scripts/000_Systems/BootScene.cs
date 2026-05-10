using UnityEngine.SceneManagement;
using UnityEngine;

//==============================================================================
// 기능: BootScene
// 설명: Boot 이후 다음 씬으로 이동하는 로직을 담당한다.
//==============================================================================
// TODO: SceneFlowController 기반 페이드 전환 옵션
//
//==============================================================================
public static class BootScene
{
    //==============================================================================
    // 기능: BootScene::LoadNext
    // 설명: GameRoot.SceneFlow 에 현재 씬 정보를 기록한 뒤 SceneManager 로 다음 씬을 로드한다.
    //==============================================================================
    // TODO: sceneConfig null 일 때 fallback 씬 제공
    //
    //==============================================================================
    public static void LoadNext(SceneListConfig sceneConfig)
    {
        // 씬 설정이 없으면 로드 불가
        if (sceneConfig == null) return;

        // SceneFlow 가 있으면 현재 씬 정보를 기록
        GameRoot root = Object.FindFirstObjectByType<GameRoot>();
        if (root != null && root.SceneFlow != null)
        {
            root.SceneFlow.CurrentScene(sceneConfig);
        }

        // 다음 씬 동기 로드
        SceneManager.LoadScene(sceneConfig.SceneName);
    }
}
