//==============================================================================
// 기능: SettingsClamp
// 설명: SettingsData 의 카테고리별 값을 SettingsConfig 허용 범위 안으로 보정한다.
//==============================================================================
// TODO: 보정된 필드 목록 반환
//
//==============================================================================
public static class SettingsClamp
{
    //==============================================================================
    // 기능: SettingsClamp::Apply
    // 설명: 그래픽·오디오·카메라·게임플레이 데이터를 각각 Clamp 한다.
    //==============================================================================
    // TODO: null 카테고리 기본 생성 옵션
    //
    //==============================================================================
    public static void Apply(SettingsData data, SettingsConfig config)
    {
        // Config 가 없으면 각 카테고리의 기존 정책에 맡김
        if (data == null) return;

        // 그래픽 설정 범위 보정
        data.GraphicsSetting?.ClampWith(config);

        // 오디오 설정 범위 보정
        data.AudioSetting?.ClampWith(config);

        // 카메라 설정 범위 보정
        data.CameraSetting?.ClampWith(config);

        // 게임플레이 설정 범위 보정
        data.GameplaySetting?.ClampWith(config);
    }
}
