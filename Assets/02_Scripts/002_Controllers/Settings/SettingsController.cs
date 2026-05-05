using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

//==============================================================================
// SettingsController: 게임 설정의 로드/세이브 + 서브 컨트롤러 오케스트레이션
//==============================================================================
// - GameRoot의 멤버로 살며 GameRoot.Instance.Settings 로만 접근
// - 데이터 저장: Application.persistentDataPath/settings.json
// - 기본값/범위: SettingsConfig(SO)에서 가져옴
// - 카테고리별 시스템 적용은 자식 서브 컨트롤러에 위임
//   (Graphics/Audio/Camera/Gameplay)
// - 변경 시 흐름: 외부에서 Data 수정 → Apply() → SaveAsync()
// - 로드 시 스키마 버전이 다르면 자동 마이그레이션 + 자동 재저장
//==============================================================================

public class SettingsController : MonoBehaviour
{
    //--------------------------------------------------------------------------
    // [1] Inspector 연결 (Config + 서브 컨트롤러)
    //--------------------------------------------------------------------------

    [TabGroup("Config")]
    [Tooltip("기본값/허용 범위가 정의된 ScriptableObject.")]
    [SerializeField] private SettingsConfig _config;

    [TabGroup("Config")]
    [Tooltip("저장 파일 이름(persistentDataPath 기준).")]
    [SerializeField] private string _saveFileName = "settings.json";

    /// <summary>설정 기본값/범위 정의 SO(읽기 전용 참조).</summary>
    public SettingsConfig Config => _config;

    [TabGroup("Sub Controllers")]
    [Tooltip("그래픽 적용 서브 컨트롤러.")]
    [SerializeField] private GraphicsSettingsController _graphics;

    [TabGroup("Sub Controllers")]
    [Tooltip("오디오 적용 서브 컨트롤러.")]
    [SerializeField] private AudioSettingsController _audio;

    [TabGroup("Sub Controllers")]
    [Tooltip("카메라/입력 노출 서브 컨트롤러.")]
    [SerializeField] private CameraSettingsController _camera;

    [TabGroup("Sub Controllers")]
    [Tooltip("게임플레이 노출 서브 컨트롤러.")]
    [SerializeField] private GameplaySettingsController _gameplay;

    /// <summary>그래픽 서브 컨트롤러.</summary>
    public GraphicsSettingsController Graphics => _graphics;

    /// <summary>오디오 서브 컨트롤러.</summary>
    public AudioSettingsController Audio => _audio;

    /// <summary>카메라/입력 서브 컨트롤러.</summary>
    public CameraSettingsController Camera => _camera;

    /// <summary>게임플레이 서브 컨트롤러.</summary>
    public GameplaySettingsController Gameplay => _gameplay;

    //--------------------------------------------------------------------------
    // [2] 상태
    //--------------------------------------------------------------------------

    [TabGroup("Status")]
    [Tooltip("현재 런타임 설정 데이터.")]
    [SerializeField] private SettingsData _data;

    [TabGroup("Status")]
    [Tooltip("Load 완료 여부.")]
    [SerializeField] private bool _isLoaded;

    /// <summary>현재 런타임 설정 데이터(읽기 전용 참조).</summary>
    public SettingsData Data => _data;

    /// <summary>Load 완료 여부.</summary>
    public bool IsLoaded => _isLoaded;

    /// <summary>설정이 시스템에 적용된 직후 발생.</summary>
    public event Action OnSettingsApplied;

    //--------------------------------------------------------------------------
    // [3] 경로 헬퍼
    //--------------------------------------------------------------------------

    /// <summary>저장 파일 전체 경로.</summary>
    private string SavePath => Path.Combine(Application.persistentDataPath, _saveFileName);

    //==========================================================================
    // [4] 로드 / 세이브
    //==========================================================================

    /*
     * @brief 디스크에서 설정을 읽고(없으면 기본값) 시스템에 적용한다
     * @param ct 취소 토큰(선택)
     */
    public async UniTask LoadAndApplyAsync(CancellationToken ct = default)
    {
        await LoadAsync(ct);
        Apply();
    }

    /*
     * @brief 디스크에서 settings.json을 읽어 _data를 채운다(없거나 실패 시 기본값)
     * @param ct 취소 토큰(선택)
     * @note 스키마 버전이 낮으면 자동 마이그레이션 후 자동 재저장
     */
    public async UniTask LoadAsync(CancellationToken ct = default)
    {
        if (_config == null)
        {
            Debug.LogError("[SettingsController] SettingsConfig가 비어있습니다. 인스펙터에 할당하세요.");
            _data = new SettingsData();
            _isLoaded = true;
            return;
        }

        string path = SavePath;

        // 파일이 없으면 기본값으로 시작
        if (!File.Exists(path))
        {
            _data = _config.CreateDefaultData();
            _isLoaded = true;
            return;
        }

        bool needsResave = false;

        try
        {
            string json = await File.ReadAllTextAsync(path, ct);
            SettingsData loaded = JsonUtility.FromJson<SettingsData>(json);

            if (loaded == null)
            {
                Debug.LogWarning("[SettingsController] 저장 파일 파싱 실패. 기본값으로 복구합니다.");
                _data = _config.CreateDefaultData();
                needsResave = true;
            }
            else
            {
                // 마이그레이션: 구버전이면 끌어올린 후 자동 재저장 플래그
                int prevVersion = loaded.Version;
                if (loaded.MigrateToCurrent())
                {
                    Debug.Log($"[SettingsController] 설정 스키마 마이그레이션: v{prevVersion} → v{SettingsData.CURRENT_VERSION}");
                    needsResave = true;
                }

                loaded.ClampWith(_config);
                _data = loaded;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SettingsController] 설정 로드 실패: {e.Message}. 기본값으로 복구합니다.");
            _data = _config.CreateDefaultData();
            needsResave = true;
        }

        _isLoaded = true;

        // 마이그레이션/복구가 일어났다면 디스크를 최신 스키마로 갱신
        if (needsResave) await SaveAsync(ct);
    }

    /*
     * @brief 현재 _data를 settings.json으로 저장한다
     * @param ct 취소 토큰(선택)
     */
    public async UniTask SaveAsync(CancellationToken ct = default)
    {
        if (_data == null)
        {
            Debug.LogWarning("[SettingsController] 저장할 데이터가 없습니다.");
            return;
        }

        try
        {
            // 안전을 위해 저장 직전에도 Clamp 한 번 더
            _data.ClampWith(_config);

            string json = JsonUtility.ToJson(_data, prettyPrint: true);
            string path = SavePath;

            // 디렉토리 보장
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            await File.WriteAllTextAsync(path, json, ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SettingsController] 설정 저장 실패: {e.Message}");
        }
    }

    //==========================================================================
    // [5] 시스템 적용 / 리셋 (서브 컨트롤러 위임)
    //==========================================================================

    /// <summary>현재 _data를 각 서브 컨트롤러에 분배해 시스템에 반영.</summary>
    public void Apply()
    {
        if (_data == null) return;

        _graphics?.Apply(_data.GraphicsSetting);
        _audio?.Apply(_data.AudioSetting);
        _camera?.Apply(_data.CameraSetting);
        _gameplay?.Apply(_data.GameplaySetting);

        OnSettingsApplied?.Invoke();
    }

    /// <summary>기본값으로 되돌리고(즉시 메모리 반영) 적용까지 수행. 저장은 별도 호출 필요.</summary>
    public void ResetToDefault()
    {
        if (_config == null)
        {
            Debug.LogError("[SettingsController] SettingsConfig가 비어있어 리셋할 수 없습니다.");
            return;
        }

        _data = _config.CreateDefaultData();
        Apply();
    }

    //==========================================================================
    // [6] 옵션 UI 편의 메서드 (서브 컨트롤러 + 데이터 연결)
    //==========================================================================

    /*
     * @brief 옵션 UI에서 해상도가 선택될 때 Index와 W/H를 동시 업데이트
     * @param index Screen.resolutions의 인덱스
     */
    public void SetResolutionByIndex(int index)
    {
        if (_data == null || _data.GraphicsSetting == null) return;
        if (_graphics == null) return;

        _graphics.SetResolutionByIndex(_data.GraphicsSetting, index);
    }
}
