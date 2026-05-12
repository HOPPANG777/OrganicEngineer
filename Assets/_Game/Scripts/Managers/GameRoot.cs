using Sirenix.OdinInspector;
using UnityEngine;

//==============================================================================
// 기능: GameRoot
// 설명: 프로젝트 전역 접근점을 하나로 모으는 루트이며 주요 컨트롤러 참조를 보관한다.
//==============================================================================
// TODO: GameRootServices 분리, 컨트롤러 자동 검증 버튼 추가
//
//==============================================================================

public class GameRoot : Singleton<GameRoot>
{
    //-------------------- 맴버 변수 --------------------//

    // GameRoot 초기화 보조 기능
    private GameBoot _gameBoot = new GameBoot();

    // 컨트롤러/팩토리를 프리팹으로 붙여두는 루트
    [TabGroup("GameRoot")]
    [SerializeField]
    private Transform _controllersRoot;

    // 씬 전환을 담당하는 컨트롤러
    [TabGroup("Management Controller")]
    [SerializeField]
    private SceneFlowController _sceneFlowController;
    // SceneFlowController getter/setter
    public SceneFlowController SceneFlow { get => _sceneFlowController; set => _sceneFlowController = value; }

    // 설정 로드/저장/적용을 담당하는 컨트롤러
    [TabGroup("Management Controller")]
    [SerializeField]
    private SettingsController _settingsController;
    // SettingsController getter/setter
    public SettingsController Settings { get => _settingsController; set => _settingsController = value; }

    // 옵션 UI 화면 컨트롤러
    [TabGroup("Management Controller")]
    [SerializeField]
    private OptionsScreenController _optionsScreen;
    // OptionsScreenController getter/setter
    public OptionsScreenController Options { get => _optionsScreen; set => _optionsScreen = value; }

    // 전역 초기화가 끝났는지 여부(1회 보장)
    [TabGroup("GameRoot")]
    [SerializeField]
    private bool _isInitialized;

    //-------------------- Get 함수 --------------------//

    // 초기화 완료 여부 getter
    public bool IsInitialized => _isInitialized;

    //-------------------- 기능 함수 --------------------//

    //==============================================================================
    // 기능: GameRoot::InitializeIfNeeded
    // 설명: 전역 초기화를 1회만 수행하고 환경 설정 및 필수 참조 검증을 실행한다.
    //==============================================================================
    // TODO: 초기화 단계별 상태 enum 추가
    //
    //==============================================================================
    public void InitializeIfNeeded()
    {
        if (_isInitialized) return;

        _isInitialized = true;

        // 환경 설정 책임은 GameBoot 로 분리
        _gameBoot.ApplyEnvironment();

        // 필수 참조 검증 책임은 GameBoot 로 분리
        _gameBoot.ValidateReferences(_controllersRoot, _sceneFlowController, _settingsController, _optionsScreen);
    }

    //==============================================================================
    // 기능: GameRoot::Start
    // 설명: 초기화는 BootLoader 에서 명시적으로 호출하므로 Start 에서는 아무 작업도 하지 않는다.
    //==============================================================================
    // TODO: 필요 없으면 제거 검토
    //
    //==============================================================================
    private void Start() { }
}

