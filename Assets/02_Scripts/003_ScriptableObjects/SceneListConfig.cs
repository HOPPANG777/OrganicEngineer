using UnityEngine;
using Sirenix.OdinInspector;

//==============================================================================
// SceneListConfig: Scene 리스트 설정
//==============================================================================
// - 해당 Config로 전환할 씬에 대한 정보를 담는다
// - 이 SO를 이용해 SceneFlow에서 어디로 이동할지 선택한다.
//
//==============================================================================

[CreateAssetMenu(menuName = "OrganicEngineer/Scene List Config", fileName = "SceneListConfig")]
public class SceneListConfig : ScriptableObject
{
    [TabGroup("Scene Config")]
    [Tooltip("씬에 이름")]
    [SerializeField] private string _sceneName;
    public string SceneName { get => _sceneName; }
}
