using Sirenix.OdinInspector;
using UnityEngine;

//==============================================================================
// FadeEffectController: 페이드 In/Out 연결용
//==============================================================================

public class FadeEffectController : MonoBehaviour
{
    // 인스펙터 연결

    [TabGroup("Fade Effect")]
    [SerializeField]
    private float _fadeEffectTime; // 0 보다 크면 이 시간 사용, 아니면 In/Out 쪽 설정 사용

    [TabGroup("Fade Controller")]
    [SerializeField]
    private FadeInController _fadeIn; // 왼쪽 -> 오른쪽

    [TabGroup("Fade Controller")]
    [SerializeField]
    private FadeOutController _fadeOut; // 오른쪽 -> 왼쪽

    // Unity 생명주기

    // 시작할 때 컴포넌트가 비었으면 같은 오브젝트에서 찾기
    private void Awake()
    {
        if (_fadeIn == null)
            _fadeIn = GetComponent<FadeInController>();

        if (_fadeOut == null)
            _fadeOut = GetComponent<FadeOutController>();
    }

    // 외부에서 호출하는 API

    // 페이드 인 재생 (FadeOut 재생 중이면 멈춤)
    public void FadeIn()
    {
        if (_fadeOut != null)
            _fadeOut.Stop();

        if (_fadeIn == null)
            return;

        if (_fadeEffectTime > 0f)
            _fadeIn.Play(_fadeEffectTime);
        else
            _fadeIn.Play();
    }

    // 페이드 아웃 재생 (FadeIn 재생 중이면 멈춤)
    public void FadeOut()
    {
        if (_fadeIn != null)
            _fadeIn.Stop();

        if (_fadeOut == null)
            return;

        if (_fadeEffectTime > 0f)
            _fadeOut.Play(_fadeEffectTime);
        else
            _fadeOut.Play();
    }

    // 시간을 직접 넣어서 페이드 인 재생
    public void FadeIn(float duration)
    {
        if (_fadeOut != null)
            _fadeOut.Stop();

        _fadeIn?.Play(duration);
    }

    // 시간을 직접 넣어서 페이드 아웃 재생
    public void FadeOut(float duration)
    {
        if (_fadeIn != null)
            _fadeIn.Stop();

        _fadeOut?.Play(duration);
    }
}
