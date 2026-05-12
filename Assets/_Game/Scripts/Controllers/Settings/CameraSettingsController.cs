using UnityEngine;

//==============================================================================
// 기능: CameraSettingsController
// 설명: 적용된 카메라/입력 설정 값을 캐시하고 외부 입력 스크립트가 조회할 수 있게 노출한다.
//==============================================================================
// TODO: 실제 카메라 입력 컨트롤러와 연결되면 이벤트 기반 갱신 검토
//
//==============================================================================

public class CameraSettingsController : MonoBehaviour
{
    //-------------------- 내부 변수 --------------------//

    // 가장 최근에 Apply 된 카메라 설정 데이터 참조
    private SettingsData.Camera _current;

    //-------------------- 기능 함수 --------------------//

    //==============================================================================
    // 기능: CameraSettingsController::Apply
    // 설명: 카메라 설정 데이터를 캐시해 외부 프로퍼티 조회에 사용한다.
    //==============================================================================
    // TODO: 카메라 시스템에 직접 적용할 값이 생기면 여기서 위임
    //
    //==============================================================================
    // c: 캐시할 카메라 설정 데이터
    public void Apply(SettingsData.Camera c)
    {
        _current = c;
        // 카메라/입력은 매 프레임 값 조회 패턴이므로 여기서 별도 적용 작업 없음
    }

    //-------------------- Get 함수 --------------------//

    // 현재 X축 카메라 감도(런타임 사용용)
    public float SensitivityX => _current != null ? _current.SensitivityX : 1f;

    // 현재 Y축 카메라 감도(런타임 사용용)
    public float SensitivityY => _current != null ? _current.SensitivityY : 1f;

    // 조준/줌 시 곱할 감도 배수
    public float AimSensitivityMultiplier => _current != null ? _current.AimSensitivityMultiplier : 1f;

    // 현재 X축 반전 여부
    public bool InvertX => _current != null && _current.InvertX;

    // 현재 Y축 반전 여부
    public bool InvertY => _current != null && _current.InvertY;

    // 현재 마우스 스무딩 여부
    public bool MouseSmoothing => _current != null && _current.MouseSmoothing;
}
