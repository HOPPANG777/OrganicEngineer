using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System;

//==============================================================================
// 기능: TitleScreenController
// 설명: 타이틀 화면 버튼 입력을 받아 새 게임/이어하기/크레딧/설정/종료 흐름을 실행한다.
//==============================================================================
// TODO: Continue 활성 조건, 버튼 null 가드, 입력 사운드 연결
//
//==============================================================================
public class TitleScreenController : SerializedMonoBehaviour
{
    //-------------------- 맴버 변수 --------------------//

    // 이어하기 버튼
    [TabGroup("Button")]
    [Tooltip("Continue Button")]
    [SerializeField] private Button _continueButton;

    // 새 게임 버튼
    [TabGroup("Button")]
    [Tooltip("New Game Button")]
    [SerializeField] private Button _newGameButton;

    // 설정 화면을 여는 버튼
    [TabGroup("Button")]
    [Tooltip("Setting Button")]
    [SerializeField] private Button _settingButton;

    // 크레딧 씬으로 이동하는 버튼
    [TabGroup("Button")]
    [Tooltip("Credits Button")]
    [SerializeField] private Button _creditsButton;

    // 게임 종료 버튼
    [TabGroup("Button")]
    [Tooltip("Quit Button")]
    [SerializeField] private Button _quitButton;

    // Continue 클릭 시 이동할 씬 설정
    [TabGroup("Scene")]
    [Tooltip("Continue 클릭 시 이동할 씬")]
    [SerializeField] private SceneListConfig _continueNextScene;

    // New Game 클릭 시 이동할 씬 설정
    [TabGroup("Scene")]
    [Tooltip("New Game 클릭 시 이동할 씬")]
    [SerializeField] private SceneListConfig _newGameNextScene;

    // Credits 클릭 시 이동할 씬 설정
    [TabGroup("Scene")]
    [Tooltip("Credits 클릭 시 이동할 씬")]
    [SerializeField] private SceneListConfig _creditsNextScene;

    // 씬에 배치된 GameRoot 참조 캐시
    private GameRoot _gameRoot;

    //-------------------- 수명주기 --------------------//

    //==============================================================================
    // 기능: TitleScreenController::Start
    // 설명: 타이틀 버튼들의 onClick 이벤트에 각 처리 함수를 연결한다.
    //==============================================================================
    // TODO: 버튼 누락 시 경고 로그 정리
    //
    //==============================================================================
    private void Start()
    {
        // 메서드 그룹/람다로 등록 (괄호 X)
        _continueButton.onClick.AddListener(OnClickContinue);

        _newGameButton.onClick.AddListener(OnClickNewGame);

        _creditsButton.onClick.AddListener(OnClickCredits);

        _quitButton.onClick.AddListener(OnClickQuit);

        // 설정 버튼: SceneFlow 대신 GameRoot의 OptionsScreen을 띄움(팝업)
        if (_settingButton != null) _settingButton.onClick.AddListener(OnClickSetting);

        // 싱글톤 대신 씬에 배치된 GameRoot 를 직접 찾음
        _gameRoot = FindFirstObjectByType<GameRoot>();
    }

    //-------------------- 버튼 함수 --------------------//

    // Continue 버튼 클릭 처리
    private void OnClickContinue() => RequestSceneLoad(_continueNextScene);

    // New Game 버튼 클릭 처리
    private void OnClickNewGame() => RequestSceneLoad(_newGameNextScene);

    // Credits 버튼 클릭 처리
    private void OnClickCredits() => RequestSceneLoad(_creditsNextScene);

    //==============================================================================
    // 기능: TitleScreenController::OnClickQuit
    // 설명: 에디터에서는 플레이 모드를 중지하고, 빌드에서는 애플리케이션을 종료한다.
    //==============================================================================
    // TODO: 종료 확인 팝업 연결
    //
    //==============================================================================
    private void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    //==============================================================================
    // 기능: TitleScreenController::OnClickSetting
    // 설명: GameRoot 에 연결된 OptionsScreenController 를 열어 설정 팝업을 표시한다.
    //==============================================================================
    // TODO: GameRoot 미초기화 상태의 UI 안내 처리
    //
    //==============================================================================
    private void OnClickSetting()
    {
        // GameRoot 에서 옵션 화면 컨트롤러를 가져옴
        OptionsScreenController options = _gameRoot != null ? _gameRoot.Options : null;
        if (options == null)
        {
            Debug.LogWarning("[TitleScreen] OptionsScreen이 GameRoot에 연결되어 있지 않습니다.");
            return;
        }

        options.Open();
    }

    //==============================================================================
    // 기능: TitleScreenController::RequestSceneLoad
    // 설명: 다음 씬 설정을 SceneFlowController 에 전달하고 로딩 흐름을 시작한다.
    //==============================================================================
    // TODO: 로딩 중 중복 입력 차단
    //
    //==============================================================================
    // nextScene: 버튼 클릭으로 이동할 대상 씬 설정
    private void RequestSceneLoad(SceneListConfig nextScene)
    {
        // 씬 설정이 비어 있으면 전환할 수 없음
        if (nextScene == null)
        {
            Debug.LogWarning("[TitleScreen] NextScene SO가 비어있습니다.");
            return;
        }

        // GameRoot 가 없으면 씬 전환 불가
        if (_gameRoot == null || _gameRoot.SceneFlow == null)
        {
            Debug.LogWarning("[TitleScreen] SceneFlow가 GameRoot에 연결되어 있지 않습니다.");
            return;
        }

        // 다음 씬 지정
        _gameRoot.SceneFlow.NextScene(nextScene);

        // 씬 로드 시작
        _gameRoot.SceneFlow.Load();
    }

}

