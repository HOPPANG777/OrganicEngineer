using System;
using UnityEngine;
using UnityEngine.UI;

//==============================================================================
// 기능: FadeBaseController
// 설명: 페이드 재생 시간, 시작값, 입력 차단 등 런타임 보조 로직을 담당한다.
//==============================================================================
// TODO: 페이드 상태 클래스로 추가 분리
//
//==============================================================================
public abstract partial class FadeBaseController
{
    // 페이드 기본 시간을 가져오기
    protected float GetFadeTime()
    {
        // 설정 파일이 있으면 설정값을 우선 사용
        float time = _config != null ? _config.FadeTime : _defaultFadeTime;

        // 최소 시간 값을 캐싱
        float minTime = _minFadeTime;

        // 최소 시간이 0 이하이면 기본 시간으로 대체
        if (minTime <= 0f) minTime = time;

        return Mathf.Max(minTime, time);
    }

    // 너무 짧아지는 걸 막는 최소 시간을 가져오기
    protected float GetMinTime()
    {
        // 인스펙터 최소 시간 값을 캐싱
        float minTime = _minFadeTime;

        // 최소 시간이 0 이하이면 페이드 시간으로 대체
        if (minTime <= 0f) minTime = GetFadeTime();

        return minTime;
    }

    // 페이드 중 입력 차단 여부 가져오기
    protected bool GetBlockRaycastDuringFade()
    {
        // 설정 파일이 있으면 설정값을 사용하고 없으면 기본값 사용
        return _config != null ? _config.BlockRaycastDuringFade : _defaultBlockRaycastDuringFade;
    }

    // 페이드 커브 가져오기
    protected AnimationCurve GetFadeCurve()
    {
        // 설정 파일이 없으면 선형 처리
        if (_config == null) return null;

        return _config.FadeCurve;
    }

    // FadePanel에 Image가 있으면 캐싱
    protected void CacheFadeImageIfNeeded()
    {
        // 패널이 없으면 검색 불가
        if (_fadePanel == null) return;

        // 이미 이미지가 있으면 유지
        if (_fadeImage != null) return;

        // 같은 오브젝트의 Image 컴포넌트 검색
        _fadeImage = _fadePanel.GetComponent<Image>();
    }

    // 진행 중인 페이드 효과를 멈추기
    protected void StopFade()
    {
        // 입력 차단 해제
        StopInputBlock();

        // 재생 상태 해제
        _isPlaying = false;

        // 완료 콜백 제거
        _onComplete = null;
    }

    // 실제 페이드 진행에 필요한 값을 세팅하기
    protected void StartPlay(float duration, Action onComplete)
    {
        // 패널이 없으면 재생 불가
        if (_fadePanel == null) return;

        // duration 이 비정상이라면 기본 시간 사용
        if (duration <= 0f) duration = GetFadeTime();

        // 이미지 참조 보장
        CacheFadeImageIfNeeded();

        // 기존 재생 상태 정리
        StopFade();

        // 페이드 패널 활성화
        _fadePanel.gameObject.SetActive(true);

        // 재생 시간 설정
        _playTime = duration;

        // 경과 시간 초기화
        _nowTime = 0f;

        // 초기 채우기 값 설정
        _fillValue = IsBack ? 1f : 0f;

        // 완료 콜백 저장
        _onComplete = onComplete;

        // 시작 UI 값 설정
        SetupStartValue();

        // 입력 차단 시작
        StartInputBlock();

        // 재생 상태 활성화
        _isPlaying = true;
    }
}
