using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

//==============================================================================
// BootLoader: BootScene 진입 스크립트
//==============================================================================
// - 씬에 배치된 GameRoot를 찾고 초기화 호출
// - 설정(Settings) 로드 + 적용 → 다음 씬으로 이동
//==============================================================================

public class BootLoader : MonoBehaviour
{
    //--------------------------------------------------------------------------
    // [1] 다음 씬 설정
    //--------------------------------------------------------------------------

    /// <summary>초기화 후 이동할 씬.</summary>
    [SerializeField]
    private SceneListConfig _nextSceneConfig;

    /// <summary>씬 로드 중복 방지.</summary>
    private bool _requestedSceneLoad;

    //--------------------------------------------------------------------------
    // [2] Unity 생명주기
    //--------------------------------------------------------------------------

    /// <summary>BootScene 진입 시 비동기 부트 시퀀스 시작.</summary>
    private void Awake()
    {
        BootAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    //--------------------------------------------------------------------------
    // [3] 부트 시퀀스 (비동기)
    //--------------------------------------------------------------------------

    /*
     * @brief GameRoot 초기화 → 설정 로드/적용 → 다음 씬 전환
     * @param ct 취소 토큰(GameObject 파괴 시 자동 취소)
     */
    private async UniTaskVoid BootAsync(CancellationToken ct)
    {
        // 초기화 순서는 BootSequence 로 분리
        bool bootSucceeded = await BootSequence.RunAsync(ct);
        if (!bootSucceeded) return;

        // 씬 전환
        RequestLoadNextScene();
    }

    //--------------------------------------------------------------------------
    // [4] 씬 전환
    //--------------------------------------------------------------------------

    /// <summary>다음 씬 로드(중복 방지).</summary>
    private void RequestLoadNextScene()
    {
        // 이미 BootScene을 벗어났다면 리턴
        if (_requestedSceneLoad) return;

        // 아니라면 이미 매니지먼트 컨트롤러 로딩 끝났으니까 트루로 바꾸고
        _requestedSceneLoad = true;

        // 다음 씬 로드 책임은 BootScene 으로 분리
        BootScene.LoadNext(_nextSceneConfig);
    }
}
