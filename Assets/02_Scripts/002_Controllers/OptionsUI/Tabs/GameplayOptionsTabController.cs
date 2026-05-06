using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

//==============================================================================
// GameplayOptionsTabController: 게임플레이 옵션 탭 UI
//==============================================================================
// - Language / Difficulty 드롭다운
// - ShowFps 토글
// - Language 코드/라벨 매핑은 본 컨트롤러에서 정의(추가 시 배열에 한 줄 추가)
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

    /// <summary>드롭다운 인덱스 → ISO 639-1 언어 코드.</summary>
    private static readonly string[] LANGUAGE_CODES = { "ko" };

    /// <summary>드롭다운 인덱스 → 표시 라벨.</summary>
    private static readonly string[] LANGUAGE_LABELS = { "한국어" };

    // 난이도 시스템 도입 여부 미정. 현재는 NORMAL 1개로 뼈대만 유지.
    // 도입 결정 후 두 배열에 같은 인덱스로 append (enum 순서와 무관).

    /// <summary>드롭다운 인덱스 → 실제 DifficultyLevel.</summary>
    private static readonly SettingsData.DifficultyLevel[] DIFFICULTY_VALUES =
    {
        SettingsData.DifficultyLevel.NORMAL,
    };

    /// <summary>드롭다운 인덱스 → 표시 라벨.</summary>
    private static readonly string[] DIFFICULTY_LABELS = { "Normal" };

    //--------------------------------------------------------------------------
    // [4] 상태
    //--------------------------------------------------------------------------

    private SettingsController _settings;
    private bool _hooked;

    //==========================================================================
    // [5] IOptionsTabController 구현
    //==========================================================================

    public void Bind(SettingsController controller)
    {
        _settings = controller;
        if (_hooked) return;
        HookEvents();
        _hooked = true;
    }

    public void Refresh()
    {
        if (_settings?.Data?.GameplaySetting == null) return;

        SettingsData.Gameplay g = _settings.Data.GameplaySetting;

        PopulateOptions(_languageDropdown, LANGUAGE_LABELS);
        PopulateOptions(_difficultyDropdown, DIFFICULTY_LABELS);

        if (_languageDropdown != null)
            _languageDropdown.SetValueWithoutNotify(GetLanguageDropdownIndex(g.Language));

        if (_difficultyDropdown != null)
            _difficultyDropdown.SetValueWithoutNotify(GetDifficultyDropdownIndex(g.Difficulty));

        if (_showFpsToggle != null)
            _showFpsToggle.SetIsOnWithoutNotify(g.ShowFps);
    }

    public void SetTabActive(bool active)
    {
        if (_panelRoot != null) _panelRoot.SetActive(active);
    }

    //==========================================================================
    // [6] 이벤트 후킹 / 핸들러
    //==========================================================================

    private void HookEvents()
    {
        if (_languageDropdown != null)   _languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        if (_difficultyDropdown != null) _difficultyDropdown.onValueChanged.AddListener(OnDifficultyChanged);
        if (_showFpsToggle != null)      _showFpsToggle.onValueChanged.AddListener(OnShowFpsChanged);
    }

    private void OnLanguageChanged(int idx)
    {
        if (!IsReady()) return;
        if (idx < 0 || idx >= LANGUAGE_CODES.Length) return;

        _settings.Data.GameplaySetting.Language = LANGUAGE_CODES[idx];
        _settings.Apply();
    }

    private void OnDifficultyChanged(int idx)
    {
        if (!IsReady()) return;
        if (idx < 0 || idx >= DIFFICULTY_VALUES.Length) return;

        _settings.Data.GameplaySetting.Difficulty = DIFFICULTY_VALUES[idx];
        _settings.Apply();
    }

    private void OnShowFpsChanged(bool isOn)
    {
        if (!IsReady()) return;
        _settings.Data.GameplaySetting.ShowFps = isOn;
        _settings.Apply();
    }

    //==========================================================================
    // [7] 헬퍼
    //==========================================================================

    /// <summary>드롭다운 옵션 채우기(단순 라벨 배열).</summary>
    private static void PopulateOptions(TMP_Dropdown dropdown, string[] labels)
    {
        if (dropdown == null) return;

        dropdown.ClearOptions();
        var options = new List<string>(labels.Length);
        for (int i = 0; i < labels.Length; i++) options.Add(labels[i]);
        dropdown.AddOptions(options);
    }

    /// <summary>언어 코드 → 드롭다운 인덱스(없으면 0=한국어).</summary>
    private static int GetLanguageDropdownIndex(string code)
    {
        if (string.IsNullOrEmpty(code)) return 0;
        for (int i = 0; i < LANGUAGE_CODES.Length; i++)
        {
            if (LANGUAGE_CODES[i] == code) return i;
        }
        return 0;
    }

    /// <summary>DifficultyLevel → 드롭다운 인덱스(없으면 0=NORMAL 폴백).</summary>
    private static int GetDifficultyDropdownIndex(SettingsData.DifficultyLevel level)
    {
        for (int i = 0; i < DIFFICULTY_VALUES.Length; i++)
        {
            if (DIFFICULTY_VALUES[i] == level) return i;
        }
        return 0;
    }

    private bool IsReady()
    {
        return _settings != null && _settings.Data != null && _settings.Data.GameplaySetting != null;
    }
}
