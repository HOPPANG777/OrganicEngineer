//==============================================================================
// IOptionsTabController: 옵션 화면 탭 컨트롤러 공통 인터페이스
//==============================================================================
// - 컨벤션 1.2(구성/컴포지션 + 인터페이스)에 따라 상속 대신 인터페이스 사용
// - OptionsScreenController가 4개 탭을 동일한 방식으로 다루기 위함
//==============================================================================

public interface IOptionsTabController
{
    /*
     * @brief 부모 화면이 SettingsController를 주입한다(1회).
     * @param controller 사용할 SettingsController 인스턴스
     */
    void Bind(SettingsController controller);

    /*
     * @brief 현재 데이터를 UI 컨트롤로 다시 채운다.
     * @note Open 시점, Reset 시점 등에 호출
     */
    void Refresh();

    /*
     * @brief 탭 패널의 표시 여부를 전환한다.
     * @param active true면 보이고 false면 숨김
     */
    void SetTabActive(bool active);
}
