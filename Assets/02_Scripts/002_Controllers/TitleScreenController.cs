using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System;

public class TitleScreenController : SerializedMonoBehaviour
{
    //--------------------------------------------------------------------------
    // [1] Buttons
    //--------------------------------------------------------------------------

    [TabGroup("Button")]
    [Tooltip("Continue Button")]
    [SerializeField] private Button _continueButton;

    [TabGroup("Button")]
    [Tooltip("New Game Button")]
    [SerializeField] private Button _newGameButton;

    [TabGroup("Button")]
    [Tooltip("Setting Button")]
    [SerializeField] private Button _settingButton;

    [TabGroup("Button")]
    [Tooltip("Credits Button")]
    [SerializeField] private Button _creditsButton;

    [TabGroup("Button")]
    [Tooltip("Quit Button")]
    [SerializeField] private Button _quitButton;

    //--------------------------------------------------------------------------
    // [2] 버튼 클릭 시 이동할 씬 (SceneListConfig SO 연결)
    //--------------------------------------------------------------------------

    [TabGroup("Scene")]
    [Tooltip("Continue 클릭 시 이동할 씬")]
    [SerializeField] private SceneListConfig _continueNextScene;

    [TabGroup("Scene")]
    [Tooltip("New Game 클릭 시 이동할 씬")]
    [SerializeField] private SceneListConfig _newGameNextScene;

    [TabGroup("Scene")]
    [Tooltip("Credits 클릭 시 이동할 씬")]
    [SerializeField] private SceneListConfig _creditsNextScene;

    //--------------------------------------------------------------------------
    // [3] Lifecycle
    //--------------------------------------------------------------------------

    private void Start()
    {
        // 메서드 그룹/람다로 등록 (괄호 X)
        _continueButton.onClick.AddListener(OnClickContinue);

        _newGameButton.onClick.AddListener(OnClickNewGame);

        _creditsButton.onClick.AddListener(OnClickCredits);

        _quitButton.onClick.AddListener(OnClickQuit);   
        // _settingButton 은 보통 팝업이라 SceneFlow 호출 안 함
    }

    //--------------------------------------------------------------------------
    // [4] Click Handlers
    //--------------------------------------------------------------------------

    private void OnClickContinue() => RequestSceneLoad(_continueNextScene);

    private void OnClickNewGame() => RequestSceneLoad(_newGameNextScene);

    private void OnClickCredits() => RequestSceneLoad(_creditsNextScene);

    private void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    //--------------------------------------------------------------------------
    // [5] SceneFlow 호출 헬퍼
    //--------------------------------------------------------------------------

    // 다음 씬을 지정하고 Load 흐름을 시작
    private void RequestSceneLoad(SceneListConfig nextScene)
    {
        if (nextScene == null)
        {
            Debug.LogWarning("[TitleScreen] NextScene SO가 비어있습니다.");
            return;
        }

        GameRoot.Instance.SceneFlow.NextScene(nextScene); // 다음 씬 지정
        GameRoot.Instance.SceneFlow.Load();               // 로드 시작
    }

}

