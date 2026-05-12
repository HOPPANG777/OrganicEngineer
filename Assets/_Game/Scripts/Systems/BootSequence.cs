using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

//==============================================================================
// 기능: BootSequence
// 설명: GameRoot 초기화와 Settings 로드/적용 순서를 담당한다.
//==============================================================================
// TODO: 부트 단계별 진행률/로그 이벤트 제공
//
//==============================================================================
public static class BootSequence
{
    //==============================================================================
    // 기능: BootSequence::RunAsync
    // 설명: GameRoot 를 찾아 초기화하고 Settings 를 로드/적용한 뒤 성공 여부를 반환한다.
    //==============================================================================
    // TODO: 필수 컨트롤러 누락 시 복구 정책
    //
    //==============================================================================
    public static async UniTask<bool> RunAsync(CancellationToken ct)
    {
        // GameRoot는 BootScene에 배치된 인스턴스를 직접 찾음
        GameRoot root = Object.FindFirstObjectByType<GameRoot>();
        if (root == null)
        {
            Debug.LogError("[BootSequence] GameRoot를 찾지 못했습니다. BootScene에 GameRoot를 배치하세요.");
            return false;
        }

        // 전역 초기화
        root.InitializeIfNeeded();

        // 설정 로드 + 적용
        if (root.Settings != null)
        {
            await root.Settings.LoadAndApplyAsync(ct);
        }

        return true;
    }
}
