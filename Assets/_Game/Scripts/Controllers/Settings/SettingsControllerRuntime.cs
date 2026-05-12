using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

//==============================================================================
// 기능: SettingsController
// 설명: 설정 데이터 로드, 저장, 적용, 리셋 같은 런타임 동작을 담당한다.
//==============================================================================
// TODO: 로드 실패 UI 알림
//
//==============================================================================
public partial class SettingsController
{
    // 설정을 로드한 뒤 즉시 적용
    public async UniTask LoadAndApplyAsync(CancellationToken ct = default)
    {
        // 디스크 또는 기본값에서 설정 로드
        await LoadAsync(ct);

        // 로드된 설정을 시스템에 적용
        Apply();
    }

    // 설정 파일을 읽어 현재 데이터에 반영
    public async UniTask LoadAsync(CancellationToken ct = default)
    {
        // Config 가 없으면 빈 데이터로 복구
        if (_config == null)
        {
            Debug.LogError("[SettingsController] SettingsConfig가 비어있습니다. 인스펙터에 할당하세요.");
            _data = new SettingsData();
            _isLoaded = true;
            return;
        }

        // 저장 파일 전체 경로 계산
        string path = SavePath;

        // 파일이 없으면 기본값으로 시작
        if (!System.IO.File.Exists(path))
        {
            _data = _config.CreateDefaultData();
            _isLoaded = true;
            return;
        }

        // JSON 로드와 마이그레이션을 SettingsSave 에 위임
        SettingsLoadResult loadResult = await _settingsSave.LoadAsync(path, _config, ct);

        // 로드된 설정 데이터 저장
        _data = loadResult.Data;

        // 로드 완료 플래그 갱신
        _isLoaded = true;

        // 마이그레이션/복구가 일어났다면 최신 파일로 다시 저장
        if (loadResult.NeedsResave) await SaveAsync(ct);
    }

    // 현재 설정 데이터를 저장
    public async UniTask SaveAsync(CancellationToken ct = default)
    {
        // JSON 저장 책임은 SettingsSave 로 분리
        await _settingsSave.SaveAsync(SavePath, _data, _config, ct);
    }

    // 현재 설정 데이터를 시스템에 적용
    public void Apply()
    {
        // 카테고리별 적용 책임은 SettingsApply 로 분리
        _settingsApply.Apply(_data, _graphics, _audio, _camera, _gameplay, _onSettingsApplied);
    }

    // 설정을 기본값으로 되돌림
    public void ResetToDefault()
    {
        // Config 가 없으면 기본값 생성 불가
        if (_config == null)
        {
            Debug.LogError("[SettingsController] SettingsConfig가 비어있어 리셋할 수 없습니다.");
            return;
        }

        // 기본 설정 데이터 생성
        _data = _config.CreateDefaultData();

        // 기본값을 즉시 시스템에 적용
        Apply();
    }

    // 옵션 UI에서 선택한 해상도 인덱스를 적용
    public void SetResolutionByIndex(int index)
    {
        // 데이터가 없으면 처리 불가
        if (_data == null || _data.GraphicsSetting == null) return;

        // 그래픽 컨트롤러가 없으면 처리 불가
        if (_graphics == null) return;

        // 그래픽 데이터에 해상도 인덱스 반영
        _graphics.SetResolutionByIndex(_data.GraphicsSetting, index);
    }
}
