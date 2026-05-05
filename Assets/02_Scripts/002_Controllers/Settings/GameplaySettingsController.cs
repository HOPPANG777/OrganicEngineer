using UnityEngine;

//==============================================================================
// GameplaySettingsController: 게임플레이 설정 값 노출 책임
//==============================================================================
// - SettingsController의 자식 MonoBehaviour
// - Language/Difficulty/ShowFps 등을 외부 시스템에 노출
// - 실제 효과(번역 적용, 난이도 룰 변경 등)는 각 시스템 측에서 구독해 처리
//==============================================================================

public class GameplaySettingsController : MonoBehaviour
{
    //--------------------------------------------------------------------------
    // [1] 캐시 (가장 최근 Apply된 데이터)
    //--------------------------------------------------------------------------

    /// <summary>가장 최근에 Apply된 데이터 참조.</summary>
    private SettingsData.Gameplay _current;

    //==========================================================================
    // [2] 시스템 적용 (값 캐시)
    //==========================================================================

    /// <summary>게임플레이 설정을 캐시. 실제 효과 적용은 소비자 측에서 처리.</summary>
    public void Apply(SettingsData.Gameplay g)
    {
        _current = g;
        // Language/Difficulty/ShowFps는 게임 시스템 의존 → 여기서 별도 적용 작업 없음
    }

    //==========================================================================
    // [3] 외부 노출 프로퍼티
    //==========================================================================

    /// <summary>현재 언어 코드(ISO 639-1).</summary>
    public string Language => _current != null ? _current.Language : "ko";

    /// <summary>현재 난이도.</summary>
    public SettingsData.DifficultyLevel Difficulty =>
        _current != null ? _current.Difficulty : SettingsData.DifficultyLevel.NORMAL;

    /// <summary>FPS 카운터 표시 여부.</summary>
    public bool ShowFps => _current != null && _current.ShowFps;
}
