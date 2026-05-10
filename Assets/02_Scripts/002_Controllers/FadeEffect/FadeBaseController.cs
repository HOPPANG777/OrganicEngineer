using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

//==============================================================================
// FadeBaseController: 페이드 기본
//==============================================================================

public abstract partial class FadeBaseController : MonoBehaviour
{
    // 페이드 UI 값 적용 기능
    private FadeView _fadeView = new FadeView();

    // 페이드 중 입력 차단 기능
    private FadeBlock _fadeBlock = new FadeBlock();

    // 인스펙터 연결

    [TabGroup("Fade Panel")]
    [SerializeField]
    protected RectTransform _fadePanel; // 페이드에 쓰는 패널

    [TabGroup("Fade Effect")]
    [SerializeField]
    protected FadeEffectConfig _config; // 페이드 설정 파일

    [TabGroup("Fade Panel")]
    [SerializeField]
    protected Image _fadeImage; // 패널 이미지(채우기 방식)

    [TabGroup("Fade Effect")]
    [SerializeField]
    protected float _defaultFadeTime = 0.35f; // 설정 파일이 없을 때 쓰는 시간

    [TabGroup("Fade Effect")]
    [SerializeField]
    protected float _minFadeTime = 0.01f; // 너무 짧아지는 걸 막는 최소 시간

    [TabGroup("Fade Effect")]
    [SerializeField]
    protected bool _defaultBlockRaycastDuringFade = true; // 설정 파일이 없을 때 입력 막기 여부

    // 내부 상태

    [TabGroup("Fade State")]
    [SerializeField]
    protected bool _isPlaying; // 지금 페이드가 진행 중인지

    [TabGroup("Fade State")]
    [SerializeField]
    protected float _playTime; // 페이드가 진행되는 총 시간

    [TabGroup("Fade State")]
    [SerializeField]
    protected float _nowTime; // 지금까지 지난 시간

    [TabGroup("Fade State")]
    [SerializeField]
    protected float _fillValue; // 채우기 값(0~1)

    [TabGroup("Fade State")]
    [SerializeField]
    protected Vector2 _startPos; // 시작 위치(이미지가 없을 때 이동용)

    [TabGroup("Fade State")]
    [SerializeField]
    protected Vector2 _endPos; // 끝 위치(이미지가 없을 때 이동용)

    protected Action _onComplete; // 끝났을 때 실행할 함수

    // 방향 값
    protected abstract bool FromLeftToRight { get; } // 왼쪽->오른쪽인지 여부
    protected abstract bool IsBack { get; } // 되돌리기인지 여부

    // 외부 API

    // 페이드 효과 시작
    public void Play()
    {
        Play(GetFadeTime(), onComplete: null);
    }

    // 페이드 효과 시작(시간 직접 지정)
    public void Play(float duration, Action onComplete = null)
    {
        StartPlay(Mathf.Max(GetMinTime(), duration), onComplete);
    }

    // 진행 중인 페이드 효과를 멈추기
    public void Stop()
    {
        StopFade();
    }

    // Unity 생명주기

    // 시작할 때 필요한 값이 비었으면 자동으로 찾기
    protected virtual void Awake()
    {
        if (_fadePanel == null)
            _fadePanel = GetComponent<RectTransform>();

        CacheFadeImageIfNeeded();
    }

    // 꺼질 때 진행 중인 효과를 멈추기
    protected virtual void OnDisable()
    {
        StopFade();
    }

    // 매 프레임마다 시간을 누적해서 효과를 진행하기
    protected virtual void Update()
    {
        if (!_isPlaying) return;

        _nowTime += Time.unscaledDeltaTime;

        float t = _playTime > 0f ? (_nowTime / _playTime) : 1f;
        if (t > 1f) t = 1f;

        var curve = GetFadeCurve();
        float ct = curve != null ? curve.Evaluate(t) : t;

        _fillValue = IsBack ? Mathf.Lerp(1f, 0f, ct) : Mathf.Lerp(0f, 1f, ct);

        // 실제 UI 값 적용은 FadeView 로 분리
        _fadeView.Apply(_fadeImage, _fadePanel, _startPos, _endPos, _fillValue, ct);

        if (t >= 1f)
        {
            StopInputBlock();
            _isPlaying = false;
            var cb = _onComplete;
            _onComplete = null;
            if (cb != null) cb();
        }
    }

}

