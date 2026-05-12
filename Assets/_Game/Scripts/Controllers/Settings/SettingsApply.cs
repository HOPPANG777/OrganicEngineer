using System;

//==============================================================================
// 기능: SettingsApply
// 설명: SettingsData 를 각 설정 서브 컨트롤러에 분배해 시스템에 적용한다.
//==============================================================================
// TODO: 적용 실패 카테고리 리포트
//
//==============================================================================
public class SettingsApply
{
    //==============================================================================
    // 기능: SettingsApply::Apply
    // 설명: 그래픽·오디오·카메라·게임플레이 설정을 각각 담당 컨트롤러로 전달한다.
    //==============================================================================
    // TODO: 카테고리별 dirty flag 적용
    //
    //==============================================================================
    public void Apply
    (
        SettingsData data,
        GraphicsSettingsController graphics,
        AudioSettingsController audio,
        CameraSettingsController camera,
        GameplaySettingsController gameplay,
        Action onApplied
    )
    {
        // 데이터가 없으면 적용 불가
        if (data == null) return;

        // 그래픽 설정 적용
        graphics?.Apply(data.GraphicsSetting);

        // 오디오 설정 적용
        audio?.Apply(data.AudioSetting);

        // 카메라/입력 설정 적용
        camera?.Apply(data.CameraSetting);

        // 게임플레이 설정 적용
        gameplay?.Apply(data.GameplaySetting);

        // 적용 완료 콜백이 있으면 직접 호출
        if (onApplied != null) onApplied();
    }
}
