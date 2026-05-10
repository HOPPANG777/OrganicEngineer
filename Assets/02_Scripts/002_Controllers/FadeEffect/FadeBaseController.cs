using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

//==============================================================================
// FadeBaseController: 페이드 기본
//==============================================================================

public abstract class FadeBaseController : MonoBehaviour
{
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
        FadeView.Apply(_fadeImage, _fadePanel, _startPos, _endPos, _fillValue, ct);

        if (t >= 1f)
        {
            StopInputBlock();
            _isPlaying = false;
            var cb = _onComplete;
            _onComplete = null;
            cb?.Invoke();
        }
    }

    // 내부 함수

    // 페이드 기본 시간을 가져오기(설정 파일 우선)
    protected float GetFadeTime()
    {
        float time = _config != null ? _config.FadeTime : _defaultFadeTime;
        float minTime = _minFadeTime;
        if (minTime <= 0f) minTime = time;
        return Mathf.Max(minTime, time);
    }

    // 너무 짧아지는 걸 막는 최소 시간을 가져오기
    protected float GetMinTime()
    {
        float minTime = _minFadeTime;
        if (minTime <= 0f) minTime = GetFadeTime();
        return minTime;
    }

    // 페이드 중 입력 막기 설정을 가져오기(설정 파일 우선)
    protected bool GetBlockRaycastDuringFade()
    {
        return _config != null ? _config.BlockRaycastDuringFade : _defaultBlockRaycastDuringFade;
    }

    // 페이드 커브를 가져오기(설정 파일 우선)
    protected AnimationCurve GetFadeCurve()
    {
        if (_config == null) return null;
        return _config.FadeCurve;
    }

    // FadePanel에 Image가 있으면 가져오기
    protected void CacheFadeImageIfNeeded()
    {
        if (_fadePanel == null) return;
        if (_fadeImage != null) return;

        _fadeImage = _fadePanel.GetComponent<Image>();
    }

    // 진행 중인 페이드 효과를 멈추기
    protected void StopFade()
    {
        StopInputBlock();
        _isPlaying = false;
        _onComplete = null;
    }

    // 실제 페이드 진행에 필요한 값을 세팅하기
    protected void StartPlay(float duration, Action onComplete)
    {
        if (_fadePanel == null)
            return;

        if (duration <= 0f)
            duration = GetFadeTime();

        CacheFadeImageIfNeeded();
        StopFade();

        _fadePanel.gameObject.SetActive(true);

        _playTime = duration;
        _nowTime = 0f;
        _fillValue = IsBack ? 1f : 0f;
        _onComplete = onComplete;

        SetupStartValue();
        StartInputBlock();

        _isPlaying = true;
    }

    // 시작 상태(위치/채우기)를 세팅하기
    protected void SetupStartValue()
    {
        // 시작 값 계산과 UI 세팅은 FadeView 로 분리
        FadeView.SetupStartValue(_fadePanel, _fadeImage, FromLeftToRight, IsBack, out _startPos, out _endPos);
    }

    // 페이드 중 입력을 막기
    protected void StartInputBlock()
    {
        // 입력 차단 적용은 FadeBlock 으로 분리
        FadeBlock.Set(_fadeImage, GetBlockRaycastDuringFade(), true);
    }

    // 페이드가 끝나면 입력 막기를 풀기
    protected void StopInputBlock()
    {
        // 입력 차단 해제는 FadeBlock 으로 분리
        FadeBlock.Set(_fadeImage, GetBlockRaycastDuringFade(), false);
    }
}

