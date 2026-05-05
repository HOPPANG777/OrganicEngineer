using UnityEngine;

//==============================================================================
// CameraSettingsController: 카메라/입력 설정 값 노출 책임
//==============================================================================
// - SettingsController의 자식 MonoBehaviour
// - 카메라 입력 스크립트가 SensitivityX/Y, InvertX/Y 등을 직접 조회하는 창구
// - 시스템 직접 적용은 없고, OnSettingsApplied 시점에 캐시만 갱신
//==============================================================================

public class CameraSettingsController : MonoBehaviour
{
    //--------------------------------------------------------------------------
    // [1] 캐시 (가장 최근 Apply된 데이터)
    //--------------------------------------------------------------------------

    /// <summary>가장 최근에 Apply된 데이터 참조.</summary>
    private SettingsData.Camera _current;

    //==========================================================================
    // [2] 시스템 적용 (값 캐시)
    //==========================================================================

    /// <summary>카메라 설정을 캐시. 실제 사용은 외부 입력 스크립트가 프로퍼티로 조회.</summary>
    public void Apply(SettingsData.Camera c)
    {
        _current = c;
        // 카메라/입력은 매 프레임 값 조회 패턴이므로 여기서 별도 적용 작업 없음
    }

    //==========================================================================
    // [3] 외부 노출 프로퍼티
    //==========================================================================

    /// <summary>현재 X축 카메라 감도(런타임 사용용).</summary>
    public float SensitivityX => _current != null ? _current.SensitivityX : 1f;

    /// <summary>현재 Y축 카메라 감도(런타임 사용용).</summary>
    public float SensitivityY => _current != null ? _current.SensitivityY : 1f;

    /// <summary>조준/줌 시 곱할 감도 배수.</summary>
    public float AimSensitivityMultiplier => _current != null ? _current.AimSensitivityMultiplier : 1f;

    /// <summary>현재 X축 반전 여부.</summary>
    public bool InvertX => _current != null && _current.InvertX;

    /// <summary>현재 Y축 반전 여부.</summary>
    public bool InvertY => _current != null && _current.InvertY;

    /// <summary>현재 마우스 스무딩 여부.</summary>
    public bool MouseSmoothing => _current != null && _current.MouseSmoothing;
}
