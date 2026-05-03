using UnityEngine;

//==============================================================================
// FadeEffectConfig: 페이드 설정
//==============================================================================

[CreateAssetMenu(menuName = "OrganicEngineer/Fade Effect Config", fileName = "FadeEffectConfig")]
public class FadeEffectConfig : ScriptableObject
{
    // 시간

    [SerializeField]
    [Min(0.01f)]
    private float _fadeTime = 0.35f; // 페이드 시간(초)

    [SerializeField]
    private AnimationCurve _fadeCurve = new AnimationCurve
        (   // 페이드 커브(더 자연스럽게)
            new Keyframe(0f, 0f),
            new Keyframe(0.25f, 0.46875f),
            new Keyframe(0.5f, 0.5f),
            new Keyframe(0.75f, 0.53125f),
            new Keyframe(1f, 1f)
        );

    /// <summary>페이드 시간(초).</summary>
    // 페이드가 끝날 때까지 걸리는 시간
    public float FadeTime => _fadeTime;

    /// <summary>페이드 커브.</summary>
    // 시작/끝을 자연스럽게 만드는 커브
    public AnimationCurve FadeCurve => _fadeCurve;

    // 입력

    [SerializeField]
    private bool _blockRaycastDuringFade = true; // 페이드 중 입력 막기

    /// <summary>페이드 중 입력 막기.</summary>
    // 페이드 중 클릭/터치를 막을지 여부
    public bool BlockRaycastDuringFade => _blockRaycastDuringFade;
}

