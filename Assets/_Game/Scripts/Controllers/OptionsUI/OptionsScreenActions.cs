using Cysharp.Threading.Tasks;
using UnityEngine;

//==============================================================================
// 기능: OptionsScreenController
// 설명: 옵션 화면 열기/닫기, 버튼 처리, 토글 입력, 탭 전환 동작을 담당한다.
//==============================================================================
// TODO: 저장되지 않은 변경사항 확인 팝업
//
//==============================================================================
public partial class OptionsScreenController
{
    //==============================================================================
    // 기능: OptionsScreenController::Open
    // 설명: 현재 설정을 스냅샷하고 탭 UI를 갱신한 뒤 옵션 패널을 연다.
    //==============================================================================
    // TODO: 이전에 선택한 탭 유지 옵션
    //
    //==============================================================================
    public void Open()
    {
        // SettingsController 와 버튼 연결 상태를 보장
        EnsureBound();

        // 설정 데이터가 없으면 옵션 화면을 열 수 없음
        if (_settings == null || _settings.Data == null)
        {
            Debug.LogError("[OptionsScreen] Settings.Data가 비어있어 열 수 없습니다.");
            return;
        }

        // 현재 데이터 스냅샷을 Cancel 복원용으로 저장
        _originalJson = JsonUtility.ToJson(_settings.Data);

        // 모든 탭 UI를 현재 데이터로 새로고침
        _optionsTab.RefreshAll(_tabs);

        // 기본 첫 번째 탭을 활성화
        SwitchTab(0);

        // 패널 루트가 있으면 화면에 표시
        if (_panelRoot != null) _panelRoot.SetActive(true);
    }

    // 옵션 화면이 열려 있는지 반환
    public bool IsOpen => _panelRoot != null && _panelRoot.activeSelf;

    // 옵션 화면을 닫음
    public void Close()
    {
        // 패널 루트가 있으면 비활성화
        if (_panelRoot != null) _panelRoot.SetActive(false);
    }

    // 옵션 화면 열림 상태를 토글
    public void Toggle()
    {
        // 열려 있으면 취소 후 닫음
        if (IsOpen)
        {
            CancelAndClose();
            return;
        }

        // 닫혀 있으면 옵션 화면을 엶
        Open();
    }

    // 저장 후 옵션 화면을 닫음
    public void ConfirmAndClose()
    {
        // Apply 버튼과 같은 흐름 사용
        OnApplyClicked();
    }

    // 변경을 되돌리고 옵션 화면을 닫음
    public void CancelAndClose()
    {
        // Cancel 버튼과 같은 흐름 사용
        OnCancelClicked();
    }

    // Apply 버튼 처리
    private void OnApplyClicked()
    {
        // 설정이 없으면 저장 없이 닫음
        if (_settings == null)
        {
            Close();
            return;
        }

        // 현재 설정 데이터를 비동기로 저장
        _settings.SaveAsync(this.GetCancellationTokenOnDestroy()).Forget();

        // 저장 요청 후 화면을 닫음
        Close();
    }

    // Cancel 버튼 처리
    private void OnCancelClicked()
    {
        // 원본 스냅샷이 있으면 현재 데이터에 덮어씀
        if (_settings != null && _settings.Data != null && !string.IsNullOrEmpty(_originalJson))
        {
            JsonUtility.FromJsonOverwrite(_originalJson, _settings.Data);
            _settings.Apply();
        }

        // 복원 후 화면을 닫음
        Close();
    }

    // Reset 버튼 처리
    private void OnResetClicked()
    {
        // 설정 컨트롤러가 없으면 리셋 불가
        if (_settings == null) return;

        // 설정을 기본값으로 되돌림
        _settings.ResetToDefault();

        // 모든 탭 UI를 새 데이터로 갱신
        _optionsTab.RefreshAll(_tabs);
    }

    // 매 프레임 토글 키 입력을 확인
    private void Update()
    {
        // 토글 키 감시가 꺼져 있으면 처리하지 않음
        if (!_listenForToggleKey) return;

        // 이번 프레임에 토글 키가 눌리지 않았으면 처리하지 않음
        if (!IsToggleKeyPressedThisFrame()) return;

        // 설정 데이터 준비 전이면 처리하지 않음
        if (_settings == null || _settings.Data == null) return;

        // 옵션 화면 열림 상태 전환
        Toggle();
    }

    // 토글 키가 이번 프레임에 눌렸는지 확인
    private bool IsToggleKeyPressedThisFrame()
    {
        // 입력 시스템별 키 감지는 OptionsHotkey 로 분리
        return _optionsHotkey.IsPressedThisFrame(_toggleKey);
    }

    // 지정한 탭 인덱스를 활성화
    private void SwitchTab(int index)
    {
        // 탭 전환 책임은 OptionsTab 으로 분리
        _optionsTab.Switch(_tabs, index);
    }
}
