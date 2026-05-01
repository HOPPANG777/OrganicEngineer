using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

public class LoadingAnimationController : MonoBehaviour
{
    [TabGroup("Loading Scene")]
    [SerializeField] Sprite[] _loadingAnimationClip;
    [SerializeField] Image _LoadingAnimation;
    [SerializeField] float _animationTime;

    [TabGroup("Loading Animation")][SerializeField] float _frameTime;
    [TabGroup("Loading Animation")][SerializeField] int _frameCount;
    [TabGroup("Loading Animation")][SerializeField] Sprite _currentClip;

    void Start()
    {
        // _currentClip을 _loadingAnimationClip[0](첫번째 애니메이션 클립)으로 초기화
        _currentClip = _loadingAnimationClip[0];
    }

    void Update()
    {
        _frameTime += Time.deltaTime;

        // _frameTime이 _animationTime 넘으면 다음 프레임
        if (_frameTime >= _animationTime)
        {
            ++_frameCount;
            if (_frameCount == _loadingAnimationClip.Length) _frameCount = 0;

            _currentClip = _loadingAnimationClip[_frameCount];
            _frameTime = 0.0f;
        }
        else
        {
            _frameTime += Time.deltaTime;
        }

        _LoadingAnimation.sprite = _currentClip;
    }
}
