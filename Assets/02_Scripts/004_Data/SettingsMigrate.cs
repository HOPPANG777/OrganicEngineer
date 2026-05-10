//==============================================================================
// 기능: SettingsMigrate
// 설명: 저장된 SettingsData 를 현재 스키마 버전으로 올리는 마이그레이션을 담당한다.
//==============================================================================
// TODO: 버전별 마이그레이션 함수 분리
//
//==============================================================================
public class SettingsMigrate
{
    //==============================================================================
    // 기능: SettingsMigrate::ToCurrent
    // 설명: 구버전 SettingsData 를 현재 버전으로 보정하고 변경 여부를 반환한다.
    //==============================================================================
    // TODO: v2 이후 마이그레이션 단계 추가
    //
    //==============================================================================
    public bool ToCurrent(SettingsData data)
    {
        // 데이터가 없으면 마이그레이션 불가
        if (data == null) return false;

        bool changed = false;

        // v0 -> v1: 단일 감도를 X/Y 감도로 분리
        if (data.Version < 1)
        {
            if (data.CameraSetting != null && data.CameraSetting.LegacySensitivity > 0f)
            {
                data.CameraSetting.SensitivityX = data.CameraSetting.LegacySensitivity;
                data.CameraSetting.SensitivityY = data.CameraSetting.LegacySensitivity;
                data.CameraSetting.ClearLegacySensitivity();
            }

            data.Version = 1;
            changed = true;
        }

        return changed;
    }
}
