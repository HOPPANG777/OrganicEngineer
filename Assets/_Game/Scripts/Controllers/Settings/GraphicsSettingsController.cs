using Sirenix.OdinInspector;
using UnityEngine;

// 기능: GraphicsSettingsController
// 설명: 그래픽 설정 적용과 해상도 목록 조회를 외부에 제공하는 그래픽 설정 진입점이다.
//==============================================================================
// TODO: GraphicsApply/ResolutionList 를 MonoBehaviour 컴포넌트로 분리할지 검토
//
//==============================================================================

public class GraphicsSettingsController : MonoBehaviour
{
    // Unity 그래픽 시스템 적용 기능
    private GraphicsApply _graphicsApply = new GraphicsApply();

    // 해상도 목록과 데이터 갱신 기능
    private ResolutionList _resolutionList = new ResolutionList();

    //==============================================================================
    // 기능: GraphicsSettingsController::Apply
    // 설명: Graphics 설정 데이터를 Unity 그래픽 시스템에 반영한다.
    //==============================================================================
    // TODO: URP/HDRP 전용 설정 적용
    //
    //==============================================================================
    // g: 적용할 그래픽 설정 데이터
    public void Apply(SettingsData.Graphics g)
    {
        // Unity 시스템 적용 책임은 GraphicsApply 로 분리
        _graphicsApply.Apply(g);
    }

    //==========================================================================
    // [2] 해상도 목록 조회 (옵션 UI에서 사용)
    //==========================================================================

    //==============================================================================
    // 기능: GraphicsSettingsController::GetAvailableResolutions
    // 설명: 현재 디스플레이가 지원하는 해상도 목록을 반환한다.
    //==============================================================================
    // TODO: 중복 해상도 필터링 옵션
    //
    //==============================================================================
    public Resolution[] GetAvailableResolutions()
    {
        // 해상도 목록 책임은 ResolutionList 로 분리
        return _resolutionList.GetAvailable();
    }

    //==============================================================================
    // 기능: GraphicsSettingsController::GetResolutionLabels
    // 설명: 옵션 UI 드롭다운에 표시할 해상도 라벨 배열을 반환한다.
    //==============================================================================
    // TODO: 라벨 지역화/포맷 설정
    //
    //==============================================================================
    public string[] GetResolutionLabels()
    {
        // UI 라벨 생성 책임은 ResolutionList 로 분리
        return _resolutionList.GetLabels();
    }

    //==============================================================================
    // 기능: GraphicsSettingsController::GetCurrentResolutionIndex
    // 설명: 현재 화면 해상도와 가장 가까운 Screen.resolutions 인덱스를 반환한다.
    //==============================================================================
    // TODO: refreshRate 까지 비교
    //
    //==============================================================================
    public int GetCurrentResolutionIndex()
    {
        // 현재 해상도 검색 책임은 ResolutionList 로 분리
        return _resolutionList.GetCurrentIndex();
    }

    //==============================================================================
    // 기능: GraphicsSettingsController::SetResolutionByIndex
    // 설명: 옵션 UI에서 선택한 해상도 인덱스를 Graphics 데이터에 기록한다.
    //==============================================================================
    // TODO: 범위 밖 인덱스 경고 처리
    //
    //==============================================================================
    // g: 갱신할 Graphics 데이터
    // index: Screen.resolutions 배열 인덱스
    public void SetResolutionByIndex(SettingsData.Graphics g, int index)
    {
        // Graphics 데이터 갱신 책임은 ResolutionList 로 분리
        _resolutionList.SetByIndex(g, index);
    }

    //--------------------------------------------------------------------------
    // [2-1] 디버그: 인스펙터에서 해상도 목록 즉시 확인용
    //--------------------------------------------------------------------------

    //==============================================================================
    // 기능: GraphicsSettingsController::DebugLogResolutions
    // 설명: 현재 디스플레이가 지원하는 해상도 목록을 콘솔에 출력한다.
    //==============================================================================
    // TODO: 에디터 전용 메뉴로 이동
    //
    //==============================================================================
    [Button("해상도 목록 출력")]
    private void DebugLogResolutions()
    {
        Resolution[] resolutions = Screen.resolutions;
        if (resolutions == null || resolutions.Length == 0)
        {
            Debug.Log("[GraphicsSettingsController] 사용 가능한 해상도가 없습니다.");
            return;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"[GraphicsSettingsController] 해상도 {resolutions.Length}개:");
        for (int i = 0; i < resolutions.Length; i++)
        {
            Resolution r = resolutions[i];
            sb.AppendLine($"  [{i}] {r.width} x {r.height} @ {(float)r.refreshRateRatio.value:F2}Hz");
        }
        Debug.Log(sb.ToString());
    }
}
