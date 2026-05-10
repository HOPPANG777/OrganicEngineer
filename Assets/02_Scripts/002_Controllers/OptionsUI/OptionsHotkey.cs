using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

//==============================================================================
// 기능: OptionsHotkey
// 설명: 옵션 화면 토글 키 입력 감지를 담당한다.
//==============================================================================
// TODO: InputAction 자산 기반 리바인딩
//
//==============================================================================
public static class OptionsHotkey
{
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    // 매핑되지 않은 키 경고를 1회만 출력하기 위한 플래그
    private static bool _warnedUnmappedKey;
#endif

    //==============================================================================
    // 기능: OptionsHotkey::IsPressedThisFrame
    // 설명: 현재 입력 시스템에 맞춰 KeyCode 가 이번 프레임 눌렸는지 반환한다.
    //==============================================================================
    // TODO: 여러 키 조합 지원
    //
    //==============================================================================
    public static bool IsPressedThisFrame(KeyCode key)
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        // New Input System 키보드 획득
        Keyboard kb = Keyboard.current;
        if (kb == null) return false;

        // KeyCode 를 KeyboardControl 로 매핑해 검사
        return MapAndCheckKey(kb, key);
#else
        // Legacy Input Manager 경로
        return Input.GetKeyDown(key);
#endif
    }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    // KeyCode 를 New Input System 키 상태로 매핑
    private static bool MapAndCheckKey(Keyboard kb, KeyCode key)
    {
        switch (key)
        {
            case KeyCode.Escape: return kb.escapeKey.wasPressedThisFrame;
            case KeyCode.Tab: return kb.tabKey.wasPressedThisFrame;
            case KeyCode.Return: return kb.enterKey.wasPressedThisFrame;
            case KeyCode.KeypadEnter: return kb.numpadEnterKey.wasPressedThisFrame;
            case KeyCode.Space: return kb.spaceKey.wasPressedThisFrame;
            case KeyCode.BackQuote: return kb.backquoteKey.wasPressedThisFrame;
            case KeyCode.F1: return kb.f1Key.wasPressedThisFrame;
            case KeyCode.F2: return kb.f2Key.wasPressedThisFrame;
            case KeyCode.F3: return kb.f3Key.wasPressedThisFrame;
            case KeyCode.F4: return kb.f4Key.wasPressedThisFrame;
            case KeyCode.F5: return kb.f5Key.wasPressedThisFrame;
            case KeyCode.F10: return kb.f10Key.wasPressedThisFrame;
            case KeyCode.F11: return kb.f11Key.wasPressedThisFrame;
            case KeyCode.F12: return kb.f12Key.wasPressedThisFrame;
            default:
                // 매핑되지 않은 키는 Escape 로 폴백
                if (!_warnedUnmappedKey)
                {
                    Debug.LogWarning($"[OptionsHotkey] KeyCode.{key}는 New Input System 매핑에 없습니다. Escape로 폴백합니다.");
                    _warnedUnmappedKey = true;
                }
                return kb.escapeKey.wasPressedThisFrame;
        }
    }
#endif
}
