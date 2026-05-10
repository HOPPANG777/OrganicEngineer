//==============================================================================
// 기능: OptionsTab
// 설명: 옵션 탭 배열의 Bind, Refresh, 활성 탭 전환을 담당한다.
//==============================================================================
// TODO: 현재 선택 탭 인덱스 보관
//
//==============================================================================
public static class OptionsTab
{
    // 모든 탭에 SettingsController 를 주입
    public static void BindAll(IOptionsTabController[] tabs, SettingsController settings)
    {
        // 탭 배열이 없으면 처리 불가
        if (tabs == null) return;

        // 모든 탭을 순회하며 Bind 호출
        for (int i = 0; i < tabs.Length; i++)
        {
            tabs[i]?.Bind(settings);
        }
    }

    // 모든 탭 UI 를 현재 데이터로 새로고침
    public static void RefreshAll(IOptionsTabController[] tabs)
    {
        // 탭 배열이 없으면 처리 불가
        if (tabs == null) return;

        // 모든 탭을 순회하며 Refresh 호출
        for (int i = 0; i < tabs.Length; i++)
        {
            tabs[i]?.Refresh();
        }
    }

    // 선택한 인덱스의 탭만 활성화
    public static void Switch(IOptionsTabController[] tabs, int index)
    {
        // 탭 배열이 없으면 처리 불가
        if (tabs == null) return;

        // 선택 탭만 켜고 나머지는 끔
        for (int i = 0; i < tabs.Length; i++)
        {
            tabs[i]?.SetTabActive(i == index);
        }
    }
}
