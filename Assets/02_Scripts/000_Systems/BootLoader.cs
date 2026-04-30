using UnityEngine;
using UnityEngine.SceneManagement;

//==============================================================================
// BootLoader: BootScene 진입 스크립트
//==============================================================================
// - 씬에 배치된 GameRoot를 찾고 초기화 호출
// - 초기화 후 다음 씬으로 이동
//==============================================================================

public class BootLoader : MonoBehaviour
{
    //--------------------------------------------------------------------------
    // [1] 다음 씬 설정
    //--------------------------------------------------------------------------

    /// <summary>초기화 후 이동할 씬 이름.</summary>
    [SerializeField]
    private string _nextSceneName = "TitleScene";

    /// <summary>씬 로드 중복 방지.</summary>
    private bool _requestedSceneLoad;

    //--------------------------------------------------------------------------
    // [2] Unity 생명주기
    //--------------------------------------------------------------------------

    /// <summary>BootScene 진입 시 GameRoot 초기화 후 씬 전환.</summary>
    private void Awake()
    {
        // GameRoot는 자동 생성 금지이므로 BootScene에 반드시 배치되어 있어야 합니다.
        if (!GameRoot.TryGetInstance(out GameRoot root) || root == null)
        {
            Debug.LogError(
                "[BootLoader] GameRoot를 찾지 못했습니다. BootScene에 GameRoot를 배치하세요.");
            return;
        }

        // 전역 초기화
        root.InitializeIfNeeded();

        // 씬 전환
        RequestLoadNextScene();
    }

    //--------------------------------------------------------------------------
    // [3] 씬 전환
    //--------------------------------------------------------------------------

    /// <summary>다음 씬 로드(중복 방지).</summary>
    private void RequestLoadNextScene()
    {
        if (_requestedSceneLoad)
            return;

        _requestedSceneLoad = true;

        // 단순 동기 로드(필요해지면 Async로 확장)
        SceneManager.LoadScene(_nextSceneName);
    }
}

