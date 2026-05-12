using UnityEngine;
using Sirenix.OdinInspector;

// 기능: SceneListConfig
// 설명: SceneFlowController 와 BootScene 이 전환할 씬 이름을 보관하는 설정 파일이다.
//==============================================================================
// TODO: 씬 표시 이름, 로딩 팁, 씬 그룹 정보 추가
//
//==============================================================================

[CreateAssetMenu(menuName = "OrganicEngineer/Scene List Config", fileName = "SceneListConfig")]
public class SceneListConfig : ScriptableObject
{
    //-------------------- 맴버 변수 --------------------//

    // Unity Build Settings 에 등록된 씬 이름
    [TabGroup("Scene Config")]
    [Tooltip("씬에 이름")]
    [SerializeField] private string _sceneName;

    //-------------------- Get 함수 --------------------//

    // 전환할 씬 이름을 반환하는 getter
    public string SceneName { get => _sceneName; }
}
