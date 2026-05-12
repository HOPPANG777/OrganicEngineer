using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//==============================================================================
// 기능: GameplayOptionsTabController
// 설명: 게임플레이 옵션 탭의 언어/난이도 드롭다운과 FPS 표시 토글을 SettingsData.Gameplay 와 연결한다.
//==============================================================================
// TODO: 언어 목록 ScriptableObject 분리, 난이도 기획 확정 후 옵션 확장
//
//==============================================================================

public class GameplayOptionsTabController : MonoBehaviour, IOptionsTabController
{
    //--------------------------------------------------------------------------
    // [1] 패널
    //--------------------------------------------------------------------------

    [TabGroup("Panel")]
    [Tooltip("이 탭의 활성/비활성을 토글하는 루트 GameObject.")]
    [SerializeField] private GameObject _panelRoot;

    //--------------------------------------------------------------------------
    // [2] 컨트롤
    //--------------------------------------------------------------------------

    [TabGroup("Controls")]
    [Tooltip("언어 드롭다운(ko/en/ja).")]
    [SerializeField] private TMP_Dropdown _languageDropdown;

    [TabGroup("Controls")]
    [Tooltip("난이도 드롭다운(EASY/NORMAL/HARD).")]
    [SerializeField] private TMP_Dropdown _difficultyDropdown;

    [TabGroup("Controls")]
    [Tooltip("FPS 표시 토글.")]
    [SerializeField] private Toggle _showFpsToggle;

    //--------------------------------------------------------------------------
    // [3] 언어 매핑 (확장 시 두 배열에 동일 인덱스로 추가)
    //--------------------------------------------------------------------------
    // 현재는 뼈대만 유지(한국어 1개). 새 언어가 필요해지면 두 배열에
    // 같은 인덱스로 한 줄씩 append. 폰트 SDF가 해당 글리프를 포함하는지
    // 반드시 함께 확인할 것(누락 시 □□□ 표시).

    // 드롭다운 인덱스에 대응되는 ISO 639-1 언어 코드 배열
    private readonly string[] _languageCodes = { "ko" };

    // 드롭다운에 표시되는 언어 라벨 배열
    private readonly string[] _languageLabels = { "한국어" };

    // 난이도 시스템 도입 여부 미정. 현재는 NORMAL 1개로 뼈대만 유지.
    // 도입 결정 후 두 배열에 같은 인덱스로 append (enum 순서와 무관).

    // 드롭다운 인덱스에 대응되는 실제 난이도 enum 배열
    private readonly SettingsData.DifficultyLevel[] _difficultyValues =
    {
        SettingsData.DifficultyLevel.NORMAL,
    };

    // 드롭다운에 표시되는 난이도 라벨 배열
    private readonly string[] _difficultyLabels = { "Normal" };

    //--------------------------------------------------------------------------
    // [4] 상태
    //--------------------------------------------------------------------------

    private SettingsController _settings; // 옵션 데이터를 읽고 적용할 SettingsController 참조
    private bool _hooked;                 // UI 이벤트를 이미 연결했는지 여부

    //==============================================================================
    // 기능: GameplayOptionsTabController::Bind
    // 설명: SettingsController 를 캐시하고 게임플레이 옵션 UI 이벤트를 한 번만 연결한다.
    //==============================================================================
    // TODO: OnDestroy 이벤트 해제
    //
    //==============================================================================
    // controller: 옵션 화면에서 주입한 SettingsController
    public void Bind(SettingsController controller)
    {
        _settings = controller;
        if (_hooked) return;
        HookEvents();
        _hooked = true;
    }

    //==============================================================================
    // 기능: GameplayOptionsTabController::Refresh
    // 설명: 현재 GameplaySetting 값을 드롭다운/토글 UI에 다시 표시한다.
    //==============================================================================
    // TODO: 드롭다운 옵션 재생성을 최초 1회로 제한
    //
    //==============================================================================
    public void Refresh()
    {
        if (_settings?.Data?.GameplaySetting == null) return;

        SettingsData.Gameplay g = _settings.Data.GameplaySetting;

        PopulateOptions(_languageDropdown, _languageLabels);
        PopulateOptions(_difficultyDropdown, _difficultyLabels);

        if (_languageDropdown != null)
            _languageDropdown.SetValueWithoutNotify(GetLanguageDropdownIndex(g.Language));

        if (_difficultyDropdown != null)
            _difficultyDropdown.SetValueWithoutNotify(GetDifficultyDropdownIndex(g.Difficulty));

        if (_showFpsToggle != null)
            _showFpsToggle.SetIsOnWithoutNotify(g.ShowFps);
    }

    //==============================================================================
    // 기능: GameplayOptionsTabController::SetTabActive
    // 설명: 게임플레이 옵션 탭 루트 패널의 표시 여부를 전환한다.
    //==============================================================================
    // TODO: 탭 전환 애니메이션 연결
    //
    //==============================================================================
    // active: true면 탭을 표시하고 false면 숨김
    public void SetTabActive(bool active)
    {
        if (_panelRoot != null) _panelRoot.SetActive(active);
    }

    //==============================================================================
    // 기능: GameplayOptionsTabController::HookEvents
    // 설명: 드롭다운/토글 값 변경 이벤트를 각 핸들러에 연결한다.
    //==============================================================================
    // TODO: 이벤트 중복 연결 방지 로직 공통화
    //
    //==============================================================================
    private void HookEvents()
    {
        if (_languageDropdown != null)   _languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        if (_difficultyDropdown != null) _difficultyDropdown.onValueChanged.AddListener(OnDifficultyChanged);
        if (_showFpsToggle != null)      _showFpsToggle.onValueChanged.AddListener(OnShowFpsChanged);
    }

    //==============================================================================
    // 기능: GameplayOptionsTabController::OnLanguageChanged
    // 설명: 언어 드롭다운 인덱스를 언어 코드로 변환해 SettingsData.Gameplay 에 반영한다.
    //==============================================================================
    // TODO: 언어 변경 즉시 로컬라이징 시스템 갱신
    //
    //==============================================================================
    // idx: 선택된 언어 드롭다운 인덱스
    private void OnLanguageChanged(int idx)
    {
        if (!IsReady()) return;
        if (idx < 0 || idx >= _languageCodes.Length) return;

        _settings.Data.GameplaySetting.Language = _languageCodes[idx];
        _settings.Apply();
    }

    //==============================================================================
    // 기능: GameplayOptionsTabController::OnDifficultyChanged
    // 설명: 난이도 드롭다운 인덱스를 DifficultyLevel 로 변환해 SettingsData.Gameplay 에 반영한다.
    //==============================================================================
    // TODO: 난이도 변경 가능 타이밍 정책 정리
    //
    //==============================================================================
    // idx: 선택된 난이도 드롭다운 인덱스
    private void OnDifficultyChanged(int idx)
    {
        if (!IsReady()) return;
        if (idx < 0 || idx >= _difficultyValues.Length) return;

        _settings.Data.GameplaySetting.Difficulty = _difficultyValues[idx];
        _settings.Apply();
    }

    //==============================================================================
    // 기능: GameplayOptionsTabController::OnShowFpsChanged
    // 설명: FPS 표시 토글 값을 SettingsData.Gameplay 에 반영한다.
    //==============================================================================
    // TODO: FPS 표시 UI 즉시 갱신
    //
    //==============================================================================
    // isOn: FPS 표시 여부
    private void OnShowFpsChanged(bool isOn)
    {
        if (!IsReady()) return;
        _settings.Data.GameplaySetting.ShowFps = isOn;
        _settings.Apply();
    }

    //==========================================================================
    // [7] 헬퍼
    //==========================================================================

    //==============================================================================
    // 기능: GameplayOptionsTabController::PopulateOptions
    // 설명: 문자열 라벨 배열을 TMP_Dropdown 옵션으로 채운다.
    //==============================================================================
    // TODO: 기존 선택값 유지 옵션
    //
    //==============================================================================
    // dropdown: 옵션을 채울 드롭다운
    // labels: 표시할 라벨 배열
    private void PopulateOptions(TMP_Dropdown dropdown, string[] labels)
    {
        if (dropdown == null) return;

        dropdown.ClearOptions();
        var options = new List<string>(labels.Length);
        for (int i = 0; i < labels.Length; i++) options.Add(labels[i]);
        dropdown.AddOptions(options);
    }

    //==============================================================================
    // 기능: GameplayOptionsTabController::GetLanguageDropdownIndex
    // 설명: 언어 코드를 드롭다운 인덱스로 변환하고 없으면 0을 반환한다.
    //==============================================================================
    // TODO: Dictionary 캐싱
    //
    //==============================================================================
    // code: 찾을 언어 코드
    private int GetLanguageDropdownIndex(string code)
    {
        if (string.IsNullOrEmpty(code)) return 0;
        for (int i = 0; i < _languageCodes.Length; i++)
        {
            if (_languageCodes[i] == code) return i;
        }
        return 0;
    }

    //==============================================================================
    // 기능: GameplayOptionsTabController::GetDifficultyDropdownIndex
    // 설명: 난이도 enum 값을 드롭다운 인덱스로 변환하고 없으면 0을 반환한다.
    //==============================================================================
    // TODO: Dictionary 캐싱
    //
    //==============================================================================
    // level: 찾을 난이도 값
    private int GetDifficultyDropdownIndex(SettingsData.DifficultyLevel level)
    {
        for (int i = 0; i < _difficultyValues.Length; i++)
        {
            if (_difficultyValues[i] == level) return i;
        }
        return 0;
    }

    //==============================================================================
    // 기능: GameplayOptionsTabController::IsReady
    // 설명: 게임플레이 옵션 UI 이벤트 처리에 필요한 Settings/Data/GameplaySetting 이 준비되었는지 확인한다.
    //==============================================================================
    // TODO:
    //
    //==============================================================================
    private bool IsReady()
    {
        return _settings != null && _settings.Data != null && _settings.Data.GameplaySetting != null;
    }
}
