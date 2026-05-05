using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;
using Sirenix.OdinInspector;

//==============================================================================
// SceneFlowController: Scene 전환 시 효과
//==============================================================================
// - 해당 컨트롤러로 Scene간 이동 시 
// - 페이드 인/아웃(로딩 텍스트 최소) 효과
// - Title → Lobby 전환: 버튼 클릭 시 SceneFlowController로 이동
//==============================================================================

// Load 단계
enum LoadState 
{ 
    LOAD_NONE, 
    LOAD_FADEIN, 
    LOAD_WAITFORLOAD, 
    LOAD_FADEOUT 
}

public class SceneFlowController : MonoBehaviour
{
    [TabGroup("FadeEffect")]
    [Tooltip("페이드 효과 컨트롤러")]
    [SerializeField] private FadeEffectController _fadeEffect;  // 페이드 효과 컨트롤러
    [TabGroup("FadeEffect")]
    [Tooltip("페이드 효과 타이머")]
    [SerializeField] private FadeEffectConfig _fadeConfig;

    [TabGroup("Loading")]
    [Tooltip("로딩 패널")]
    [SerializeField] private GameObject _loadingPanel;    // 씬을 로딩할때 화면을 가리는 용도

    [TabGroup("Scene")]
    [Tooltip("씬 리스트")]
    [SerializeField] private List<SceneListConfig> _scenesList; // 씬 리스트
    [TabGroup("Scene")]
    [Tooltip("현재 씬")]
    [SerializeField] private SceneListConfig _currentScene; // 현재 씬
    [TabGroup("Scene")]
    [Tooltip("다음 씬")]
    [SerializeField] private SceneListConfig _nextScene; // 다음 씬

    [TabGroup("Status")]
    [Tooltip("로딩중")]
    [SerializeField] private bool _isSceneLoading;

    [TabGroup("Status")]
    [Tooltip("로딩중")]
    [SerializeField] private LoadState _loadState = LoadState.LOAD_NONE; // 현재 단계

    [TabGroup("Status")]
    [Tooltip("로딩중")]
    [SerializeField] private float _stateElapsed; // 단계별 경과 시간 (페이드 진행 시간 추적)

    [TabGroup("Status")]
    [Tooltip("로딩중")]
    [SerializeField] private AsyncOperation _loadOp; // 비동기 씬 로드 핸들

    private void Update()
    {
        switch (_loadState)
        {
            case LoadState.LOAD_FADEIN:
                UpdateFadingIn();
                break;
            case LoadState.LOAD_WAITFORLOAD:
                UpdateWaitingForLoad();
                break;
            case LoadState.LOAD_FADEOUT:
                UpdateFadingOut();
                break;
        }
    }

    // FadeIn 진행 중 + 씬 비동기 로드 중
    private void UpdateFadingIn()
    {
        _stateElapsed += Time.deltaTime;
        bool fadeDone = _stateElapsed >= _fadeConfig.FadeTime;
        bool loadReady = _loadOp != null && _loadOp.progress >= 0.9f;

        // 페이드도 끝났고 로드도 끝났다 → 로딩 패널 없이 바로 활성화 + FadeOut
        if (fadeDone && loadReady)
        {
            ActivateAndFadeOut();
            return;
        }

        // 페이드는 끝났는데 로드가 아직 → 로딩 패널 켜고 대기 상태로
        if (fadeDone && !loadReady)
        {
            if (_loadingPanel != null) _loadingPanel.SetActive(true);
            _loadState = LoadState.LOAD_WAITFORLOAD;
        }
    }

    // FadeIn은 끝났고, 씬 로드가 끝나길 기다리는 중
    private void UpdateWaitingForLoad()
    {
        if (_loadOp == null) return;
        if (_loadOp.progress < 0.9f) return;

        // 로딩 완료 → 패널 끄고 활성화 + FadeOut
        if (_loadingPanel != null) _loadingPanel.SetActive(false);
        ActivateAndFadeOut();
    }

    // 로딩된 씬을 활성화하고 FadeOut 단계로 진입
    private void ActivateAndFadeOut()
    {
        // 이 시점까지 현재 씬은 켜져 있고, 아래 한 줄에서 다음 씬 활성화 + 현재 씬 자동 언로드
        _loadOp.allowSceneActivation = true;

        // 현재 씬 정보 갱신
        _currentScene = _nextScene;
        _stateElapsed = 0f;
        _loadState = LoadState.LOAD_FADEOUT;
        _fadeEffect.FadeOut(_fadeConfig.FadeTime);
    }

    // FadeOut 진행 중 → 끝나면 상태 초기화
    private void UpdateFadingOut()
    {
        _stateElapsed += Time.deltaTime;
        if (_stateElapsed < _fadeConfig.FadeTime) return;

        _loadState = LoadState.LOAD_NONE;
        _isSceneLoading = false;
        _loadOp = null;
    }

    // 씬을 로드합니다.   
    public void Load()
    {
        // 이미 씬이 로딩 중이라면 리턴
        if (_isSceneLoading) return;

        // 다음 씬이 지정되지 않았으면 진행 불가
        if (_nextScene == null) return;

        // 로딩 시작
        _isSceneLoading = true;
        _stateElapsed = 0f;
        _loadState = LoadState.LOAD_FADEIN;

        // 시작 시점에는 로딩 패널은 꺼둔다 (FadeIn 시간 안에 끝나면 안 켤 수도 있음)
        if (_loadingPanel != null) _loadingPanel.SetActive(false);

        // 페이드 인 효과 시작 (FadeConfig 기준 시간으로 강제 일치)
        _fadeEffect.FadeIn(_fadeConfig.FadeTime);

        // 비동기 씬 로드 시작 (활성화는 직접 제어)
        _loadOp = SceneManager.LoadSceneAsync(_nextScene.SceneName);
        _loadOp.allowSceneActivation = false;   
    }

    // 씬을 다시 로드합니다.
    public void Reload()
    {
        if (_currentScene == null) return;

        // 중간에 멈춰있을 수 있는 상태값 초기화
        _isSceneLoading = false;
        _loadState = LoadState.LOAD_NONE;
        _stateElapsed = 0f;
        _loadOp = null;

        _nextScene = _currentScene;
        Load();
    }

    // 다음 씬으로 넘어감
    public void NextScene(SceneListConfig sceneConfig)
    {
        _nextScene = sceneConfig;
    }
    
    // 현재 씬 이름 입력
    public void CurrentScene(SceneListConfig sceneConfig)
    {
        _currentScene = sceneConfig;
    }

}
