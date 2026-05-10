using UnityEngine;
using UnityEngine.SceneManagement;

//==============================================================================
// 기능: SceneLoad
// 설명: Unity SceneManager 비동기 씬 로드와 활성화 준비 상태 판정을 담당한다.
//==============================================================================
// TODO: Addressables Scene 로드 지원
//
//==============================================================================
public static class SceneLoad
{
    //==============================================================================
    // 기능: SceneLoad::Begin
    // 설명: 씬 비동기 로드를 시작하고 자동 활성화를 꺼서 SceneFlow 가 직접 제어하게 한다.
    //==============================================================================
    // TODO: 잘못된 씬 이름 예외 처리
    //
    //==============================================================================
    public static AsyncOperation Begin(SceneListConfig sceneConfig)
    {
        // 씬 설정이 없으면 로드 불가
        if (sceneConfig == null) return null;

        // 비동기 로드 시작
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneConfig.SceneName);

        // 활성화는 SceneFlowController 가 타이밍에 맞춰 직접 처리
        if (loadOp != null)
        {
            loadOp.allowSceneActivation = false;
        }

        return loadOp;
    }

    // 로드 준비 완료 여부(progress 0.9 이상)
    public static bool IsReady(AsyncOperation loadOp)
    {
        return loadOp != null && loadOp.progress >= 0.9f;
    }

    // 로드된 씬 활성화
    public static void Activate(AsyncOperation loadOp)
    {
        // 핸들이 없으면 활성화 불가
        if (loadOp == null) return;

        // 다음 씬 활성화 허용
        loadOp.allowSceneActivation = true;
    }
}
