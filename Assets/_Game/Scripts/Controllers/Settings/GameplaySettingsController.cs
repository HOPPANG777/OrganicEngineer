using UnityEngine;

//==============================================================================
// 기능: GameplaySettingsController
// 설명: 적용된 게임플레이 설정 값을 캐시하고 외부 시스템이 조회할 수 있게 노출한다.
//==============================================================================
// TODO: 언어/난이도/FPS 표시 시스템이 생기면 직접 적용 또는 이벤트 연결
//
//==============================================================================

public class GameplaySettingsController : MonoBehaviour
{
    //-------------------- 내부 변수 --------------------//

    // 가장 최근에 Apply 된 게임플레이 설정 데이터 참조
    private SettingsData.Gameplay _current;

    //-------------------- 기능 함수 --------------------//

    //==============================================================================
    // 기능: GameplaySettingsController::Apply
    // 설명: 게임플레이 설정 데이터를 캐시해 외부 프로퍼티 조회에 사용한다.
    //==============================================================================
    // TODO: 게임플레이 시스템 적용 로직 연결
    //
    //==============================================================================
    // g: 캐시할 게임플레이 설정 데이터
    public void Apply(SettingsData.Gameplay g)
    {
        _current = g;
        // Language/Difficulty/ShowFps는 게임 시스템 의존 → 여기서 별도 적용 작업 없음
    }

    //-------------------- Get 함수 --------------------//

    // 현재 언어 코드(ISO 639-1)
    public string Language => _current != null ? _current.Language : "ko";

    // 현재 난이도
    public SettingsData.DifficultyLevel Difficulty =>
        _current != null ? _current.Difficulty : SettingsData.DifficultyLevel.NORMAL;

    // FPS 카운터 표시 여부
    public bool ShowFps => _current != null && _current.ShowFps;
}
