using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        // GameRoot는 자동 생성 금지이므로 BootScene에 반드시 배치되어 있어야 합니다.
        if (!GameRoot.TryGetInstance(out GameRoot root) || root == null)
        {
            Debug.LogError("[BootLoader] GameRoot를 찾지 못했습니다. BootScene에 GameRoot를 배치하세요.");
            return;
        }

        // 전역 초기화
        root.InitializeIfNeeded();

        // 설정 로드 + 적용 (파일 I/O이므로 비동기)
        if (root.Settings != null)
        {
            await root.Settings.LoadAndApplyAsync(ct);
        }

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

        // SceneFlow 컨트롤러에 현재 씬 이름 입력하고
        GameRoot.Instance.SceneFlow.CurrentScene(_nextSceneConfig);

        // 단순 동기 로드(필요해지면 Async로 확장)
        SceneManager.LoadScene(_nextSceneConfig.SceneName);
    }
}
