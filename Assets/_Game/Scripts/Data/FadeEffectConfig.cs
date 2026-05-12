using UnityEngine;

//==============================================================================
// 기능: FadeEffectConfig
// 설명: 페이드 시간, 페이드 커브, 페이드 중 입력 차단 여부를 보관하는 설정 파일이다.
//==============================================================================
// TODO: 페이드 방향/색상/알파 방식 등 확장 옵션 추가
//
//==============================================================================

[CreateAssetMenu(menuName = "OrganicEngineer/Fade Effect Config", fileName = "FadeEffectConfig")]
public class FadeEffectConfig : ScriptableObject
{
    //-------------------- 맴버 변수 --------------------//

    // 페이드가 끝날 때까지 걸리는 시간(초)
    [SerializeField]
    [Min(0.01f)]
    private float _fadeTime = 0.35f;

    // 페이드 진행 값을 보정하는 애니메이션 커브
    [SerializeField]
    private AnimationCurve _fadeCurve = new AnimationCurve
        (
            new Keyframe(0f, 0f),
            new Keyframe(0.25f, 0.46875f),
            new Keyframe(0.5f, 0.5f),
            new Keyframe(0.75f, 0.53125f),
            new Keyframe(1f, 1f)
        );

    // 페이드 중 클릭/터치를 막을지 여부
    [SerializeField]
    private bool _blockRaycastDuringFade = true;

    //-------------------- Get 함수 --------------------//

    // 페이드가 끝날 때까지 걸리는 시간
    public float FadeTime => _fadeTime;

    // 시작/끝을 자연스럽게 만드는 커브
    public AnimationCurve FadeCurve => _fadeCurve;

    // 페이드 중 클릭/터치를 막을지 여부
    public bool BlockRaycastDuringFade => _blockRaycastDuringFade;
}

