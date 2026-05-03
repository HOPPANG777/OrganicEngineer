using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System;

//==============================================================================
// SceneFlowController: Scene 전환 시 효과
//==============================================================================
// - 해당 컨트롤러로 Scene간 이동 시 
// - 페이드 인/아웃(로딩 텍스트 최소) 효과
// - Title → Lobby 전환: 버튼 클릭 시 SceneFlowController로 이동
//==============================================================================

public class SceneFlowController : MonoBehaviour
{
    [TabGroup("FadeEffect")]
    [SerializeField]
    [Tooltip("페이드 효과 컨트롤러")] private FadeEffectController _fadeEffect;  // 페이드 효과 컨트롤러

    [TabGroup("Loading")]
    [SerializeField]
    [Tooltip("로딩 패널")] private GameObject _loadingPanel;    // 씬을 로딩할때 화면을 가리는 용도

    [TabGroup("Scene")]
    [SerializeField]
    [Tooltip("씬 리스트")] private List<string> _scenesList; // 씬 리스트
    [TabGroup("Scene")]
    [SerializeField]
    [Tooltip("현재 씬")] private string _currentScene; // 현재 씬
    [TabGroup("Scene")]
    [SerializeField]
    [Tooltip("다음 씬")] private string _nextScene; // 다음 씬
    
    // 씬을 로드합니다.   
    void Load()
    {

    }

    // 씬을 다시 로드합니다.
    void Reload()
    {

    }

    // 다음 씬으로 넘어감
    public void NextScene(string sceneName)
    {

    }
    
    // 현재 씬 이름 입력
    public void CurrentScene(string sceneName)
    {
        _currentScene = sceneName;
    }
    
}
