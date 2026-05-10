using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

// 기능: BootLoader
// 설명: BootScene 진입 시 GameRoot 초기화와 설정 로드 후 다음 씬 전환을 시작한다.
//==============================================================================
// TODO: 부트 진행률 UI와 에러 화면 연결
//
//==============================================================================

public class BootLoader : MonoBehaviour
{
    //-------------------- 맴버 변수 --------------------//

    // 초기화 후 이동할 씬 설정
    [SerializeField]
    private SceneListConfig _nextSceneConfig;

    // 씬 로드 중복 방지 플래그
    private bool _requestedSceneLoad;

    //-------------------- 수명주기 --------------------//

    //==============================================================================
    // 기능: BootLoader::Awake
    // 설명: BootScene 진입 시 비동기 부트 시퀀스를 시작한다.
    //==============================================================================
    // TODO: 이미 부트 중인지 확인하는 전역 가드
    //
    //==============================================================================
    private void Awake()
    {
        BootAsync(this.GetCancellationTokenOnDestroy()).Forget();
    }

    //==============================================================================
    // 기능: BootLoader::BootAsync
    // 설명: GameRoot 초기화와 Settings 로드/적용을 끝낸 뒤 다음 씬 로드를 요청한다.
    //==============================================================================
    // TODO: 부트 실패 시 재시도 버튼 제공
    //
    //==============================================================================
    // ct: GameObject 파괴 시 자동 취소되는 토큰
    private async UniTaskVoid BootAsync(CancellationToken ct)
    {
        // 초기화 순서는 BootSequence 로 분리
        bool bootSucceeded = await BootSequence.RunAsync(ct);
        if (!bootSucceeded) return;

        // 씬 전환
        RequestLoadNextScene();
    }

    //==============================================================================
    // 기능: BootLoader::RequestLoadNextScene
    // 설명: 중복 요청을 막고 BootScene.LoadNext 로 다음 씬 전환을 위임한다.
    //==============================================================================
    // TODO: _nextSceneConfig null 경고
    //
    //==============================================================================
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
