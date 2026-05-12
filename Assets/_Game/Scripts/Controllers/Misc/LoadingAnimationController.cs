using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

//==============================================================================
// LoadingAnimationController : 로딩 에니메이션 출력 컨트롤러
//==============================================================================
// - 해당 컨트롤러로 이미지 배열을 애니메이션으로 출력한다.
// - 애니메이션 효과
// - 프레임이 유지되는 시간동안 애니메이션 출력 해당 오브젝트가 비활성화 될 때 까지
//==============================================================================

public class LoadingAnimationController : MonoBehaviour
{
    [TabGroup("Loading Scene")]
    [SerializeField] Sprite[] _loadingAnimationClip; // 애니메이션 프레임으로 사용할 스프라이트 
    [TabGroup("Loading Scene")]
    [SerializeField] Image _LoadingAnimation;        // 화면에 표시할 UI Image 
    [TabGroup("Loading Scene")]
    [SerializeField] float _animationTime;           // 한 프레임이 유지되는 시간 (초 단위)

    [TabGroup("Loading Animation")]
    [SerializeField] float _frameTime;               // 현재 프레임이 진행된 누적 시간
    [TabGroup("Loading Animation")]
    [SerializeField] int _frameCount;                // 현재 재생 중인 프레임 
    [TabGroup("Loading Animation")]
    [SerializeField] Sprite _currentClip;            // 현재 적용된 스프라이트

    void Start()
    {
        // 스프라이트 배열의 첫 번째 이미지를 현재 프레임으로 설정
        _currentClip = _loadingAnimationClip[0];
    }

    void Update()
    {
        // 매 프레임마다 경과 시간을 누적 (프레임 진행 시간 측정)
        _frameTime += Time.deltaTime;

        // 누적 시간이 한 프레임 유지 시간을 넘었는지 체크
        if (_frameTime >= _animationTime)
        {
            // 다음 프레임으로 이동 (인덱스 증가)
            ++_frameCount;

            // 배열의 마지막까지 도달하면 다시 0번으로 돌아가서 반복
            if (_frameCount == _loadingAnimationClip.Length) _frameCount = 0;

            // 현재 프레임에 해당하는 스프라이트를 가져와 적용
            _currentClip = _loadingAnimationClip[_frameCount];

            // 프레임 시간 초기화 (다음 프레임 타이밍을 위해 리셋)
            _frameTime = 0.0f;
        }

        // 현재 스프라이트를 UI Image에 적용하여 화면에 출력
        _LoadingAnimation.sprite = _currentClip;
    }
}