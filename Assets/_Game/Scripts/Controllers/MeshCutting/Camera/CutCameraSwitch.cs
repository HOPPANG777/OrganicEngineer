using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OrganicEngineer.MeshCutting.CutCamera
{
    //==============================================================================
    // 기능: CutCameraSwitch
    // 설명: Q/E 입력만 담당해 측면 카메라를 이전/다음으로 전환한다.
    //==============================================================================
    // TODO: InputAction 자산 기반 리바인딩 지원
    //
    //==============================================================================
    public class CutCameraSwitch : MonoBehaviour
    {
        //-------------------- 맴버 변수 --------------------//

        [TabGroup("References")]
        [Tooltip("측면 전환을 전달할 카메라 리그")]
        [SerializeField] private CutCameraRig _rig;

        [TabGroup("Options")]
        [Tooltip("전환 시 디버그 로그를 출력할지 여부")]
        [SerializeField] private bool _logOnSwitch;

        //-------------------- 수명주기 --------------------//

        private void Reset()
        {
            // 부모에 있는 리그를 자동 연결
            if (_rig == null)
            {
                _rig = GetComponentInParent<CutCameraRig>();
            }
        }

        //==============================================================================
        // 기능: CutCameraSwitch::Update
        // 설명: 매 프레임 Q/E 입력을 확인해 측면 카메라를 순환한다.
        //==============================================================================
        // TODO: 입력 잠금/쿨타임 옵션
        //
        //==============================================================================
        private void Update()
        {
            // 리그가 없으면 전환 불가
            if (_rig == null) return;

            // 키보드가 없으면 입력 처리 불가
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            // Q: 이전 측면 카메라로 전환
            if (kb.qKey.wasPressedThisFrame)
            {
                _rig.SwitchPrev();
                if (_logOnSwitch) Debug.Log($"[CutCameraSwitch] Q -> {_rig.ActiveSideIndex}");
            }
            // E: 다음 측면 카메라로 전환
            else if (kb.eKey.wasPressedThisFrame)
            {
                _rig.SwitchNext();
                if (_logOnSwitch) Debug.Log($"[CutCameraSwitch] E -> {_rig.ActiveSideIndex}");
            }
        }
    }
}
