using UnityEngine;

//==============================================================================
// 기능: GameBoot
// 설명: GameRoot 초기화 시 필요한 환경 설정과 필수 참조 검증을 담당한다.
//==============================================================================
// TODO: 플랫폼별 targetFrameRate 설정 분기
//
//==============================================================================
public class GameBoot
{
    //==============================================================================
    // 기능: GameBoot::ApplyEnvironment
    // 설명: 프로젝트 전역 런타임 환경 값을 적용한다.
    //==============================================================================
    // TODO: SettingsData.Graphics.TargetFrameRate 와의 우선순위 정리
    //
    //==============================================================================
    public void ApplyEnvironment()
    {
        // 기본 목표 프레임레이트 설정
        Application.targetFrameRate = 60;
    }

    //==============================================================================
    // 기능: GameBoot::ValidateReferences
    // 설명: GameRoot 에 연결된 필수 컨트롤러 참조가 비어 있는지 경고한다.
    //==============================================================================
    // TODO: 에디터 전용 자동 연결 버튼 제공
    //
    //==============================================================================
    public void ValidateReferences(Transform controllersRoot, SceneFlowController sceneFlow, SettingsController settings, OptionsScreenController options)
    {
        // 컨트롤러 루트 검증
        if (controllersRoot == null) Debug.LogWarning("[GameRoot] ControllersRoot가 없습니다.");

        // 씬 흐름 컨트롤러 검증
        if (sceneFlow == null) Debug.LogWarning("[GameRoot] SceneFlowController가 없습니다.");

        // 설정 컨트롤러 검증
        if (settings == null) Debug.LogWarning("[GameRoot] SettingsController가 없습니다.");

        // 옵션 UI 컨트롤러 검증
        if (options == null) Debug.LogWarning("[GameRoot] OptionsScreen이 없습니다.");
    }
}
