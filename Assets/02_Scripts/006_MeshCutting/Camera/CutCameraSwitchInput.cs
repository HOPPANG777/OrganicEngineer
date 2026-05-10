using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

//==============================================================================
// 기능: CutCameraSwitchInput
// 설명: Q/E 키 입력을 받아 CutCameraRig 의 활성 측면 카메라를 이전/다음으로 전환한다.
//==============================================================================
// TODO: InputAction 자산 연동, 키 리바인딩, 게임패드 LB/RB 지원
//
//==============================================================================
namespace OrganicEngineer.MeshCutting.CutCamera
{
    public class CutCameraSwitchInput : MonoBehaviour
    {
        //-------------------- 맴버 변수 --------------------//

        [Title("References")]
        [Tooltip("입력을 전달할 카메라 리그")]
        [SerializeField] private CutCameraRig _rig;

        [Title("Options")]
        [Tooltip("디버그용 로그 출력 여부")]
        [SerializeField] private bool _logOnSwitch = false;

        //-------------------- 수명주기 --------------------//

        private void Reset()
        {
            // 같은 GameObject 또는 부모에 붙어 있으면 자동 연결
            if (_rig == null)
            {
                _rig = GetComponentInParent<CutCameraRig>();
            }
        }

        //==============================================================================
        // 기능: CutCameraSwitchInput::Update
        // 설명: 매 프레임 Q/E 키 입력을 폴링해 카메라 리그에 전달한다.
        //==============================================================================
        // TODO: InputAction.performed 콜백 방식으로 전환, 입력 잠금 옵션
        //
        //==============================================================================
        private void Update()
        {
            if (_rig == null) return;

            // 키보드 디바이스가 없을 수 있으므로 가드
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            // R: 탑뷰 카메라로 돌아감
            if (kb.rKey.wasPressedThisFrame)
            {
                _rig.EnterTopOnly();
            }
            // Q: 이전 측면 카메라
            if (kb.qKey.wasPressedThisFrame)
            {
                _rig.SwitchPrev();
                if (_logOnSwitch) Debug.Log($"[CutCameraSwitchInput] Q → {_rig.ActiveSideIndex}");
            }
            // E: 다음 측면 카메라
            else if (kb.eKey.wasPressedThisFrame)
            {
                _rig.SwitchNext();
                if (_logOnSwitch) Debug.Log($"[CutCameraSwitchInput] E → {_rig.ActiveSideIndex}");
            }
        }
    }
}