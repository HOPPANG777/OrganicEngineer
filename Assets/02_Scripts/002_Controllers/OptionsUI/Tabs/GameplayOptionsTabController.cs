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

    /// <summary>드롭다운 인덱스 → ISO 639-1 언어 코드.</summary>
    private static readonly string[] LANGUAGE_CODES = { "ko", "en", "ja" };

    /// <summary>드롭다운 인덱스 → 표시 라벨.</summary>
    private static readonly string[] LANGUAGE_LABELS = { "한국어", "English", "日本語" };

    /// <summary>드롭다운 인덱스 → 난이도 라벨.</summary>
    private static readonly string[] DIFFICULTY_LABELS = { "Easy", "Normal", "Hard" };

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
            _difficultyDropdown.SetValueWithoutNotify((int)g.Difficulty);

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
        _settings.Data.GameplaySetting.Difficulty = (SettingsData.DifficultyLevel)idx;
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

    private bool IsReady()
    {
        return _settings != null && _settings.Data != null && _settings.Data.GameplaySetting != null;
    }
}
