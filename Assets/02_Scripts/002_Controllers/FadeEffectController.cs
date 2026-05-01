using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using Unity.Multiplayer.Center.Common;

//==============================================================================
// FadeEffectController: Fade In/Out 효과
//==============================================================================
// - 해당 컨트롤러로 Fade In/Out 효과 적용
// - Mathf.Lerp로 부드럽게 패널을 이동시켜서 효과 적용
// 
//==============================================================================

public class FadeEffectController : MonoBehaviour
{
    [TabGroup("Fade Panel")]
    [SerializeField] RectTransform _fadePanel;      // 페이드 효과를 적용 할 패널 (보통은 자기 자신)

    [TabGroup("Fade Effect")]
    [SerializeField] float _fadeEffectTime; // 페이드 효과가 적용 되는 시간

    public void FadeIn()
    {
        
    }

    public void FadeOut()
    {

    }

}
