using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

//==============================================================================
// SettingsController: 게임 설정의 로드/세이브 + 서브 컨트롤러 오케스트레이션
//==============================================================================
// - GameRoot의 멤버로 살며 외부에서 연결된 참조로 접근
// - 데이터 저장: Application.persistentDataPath/settings.json
// - 기본값/범위: SettingsConfig(SO)에서 가져옴
// - 카테고리별 시스템 적용은 자식 서브 컨트롤러에 위임
//   (Graphics/Audio/Camera/Gameplay)
// - 변경 시 흐름: 외부에서 Data 수정, Apply(), SaveAsync() 순서로 처리
// - 로드 시 스키마 버전이 다르면 자동 마이그레이션 + 자동 재저장
//==============================================================================

public partial class SettingsController : MonoBehaviour
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

    // 설정 기본값/범위 정의 SO(읽기 전용 참조)
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

    // 그래픽 서브 컨트롤러 getter
    public GraphicsSettingsController Graphics => _graphics;

    // 오디오 서브 컨트롤러 getter
    public AudioSettingsController Audio => _audio;

    // 카메라/입력 서브 컨트롤러 getter
    public CameraSettingsController Camera => _camera;

    // 게임플레이 서브 컨트롤러 getter
    public GameplaySettingsController Gameplay => _gameplay;

    //--------------------------------------------------------------------------
    // [2] 상태
    //--------------------------------------------------------------------------

    // 설정 저장/로드 기능
    private SettingsSave _settingsSave = new SettingsSave();

    // 설정 적용 분배 기능
    private SettingsApply _settingsApply = new SettingsApply();

    [TabGroup("Status")]
    [Tooltip("현재 런타임 설정 데이터.")]
    [SerializeField] private SettingsData _data;

    [TabGroup("Status")]
    [Tooltip("Load 완료 여부.")]
    [SerializeField] private bool _isLoaded;

    // 현재 런타임 설정 데이터 getter
    public SettingsData Data => _data;

    // Load 완료 여부 getter
    public bool IsLoaded => _isLoaded;

    // 설정 적용 후 호출할 외부 콜백
    private Action _onSettingsApplied;

    //--------------------------------------------------------------------------
    // [3] 경로 헬퍼
    //--------------------------------------------------------------------------

    // 저장 파일 전체 경로
    private string SavePath => Path.Combine(Application.persistentDataPath, _saveFileName);

}
