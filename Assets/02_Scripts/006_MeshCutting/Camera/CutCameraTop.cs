using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OrganicEngineer.MeshCutting.CutCamera
{
    //==============================================================================
    // 기능: CutCameraTop
    // 설명: R 입력만 담당해 카메라 리그를 탑뷰 전용 모드로 전환한다.
    //==============================================================================
    // TODO: 키 리바인딩과 토글 복귀 옵션
    //
    //==============================================================================
    public class CutCameraTop : MonoBehaviour
    {
        //-------------------- 맴버 변수 --------------------//

        [TabGroup("References")]
        [Tooltip("탑뷰 전환을 전달할 카메라 리그")]
        [SerializeField] private CutCameraRig _rig;

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
        // 기능: CutCameraTop::Update
        // 설명: 매 프레임 R 입력을 확인해 탑뷰 전용 모드로 전환한다.
        //==============================================================================
        // TODO: R 재입력 시 마지막 측면 복귀 옵션
        //
        //==============================================================================
        private void Update()
        {
            // 리그가 없으면 전환 불가
            if (_rig == null) return;

            // 키보드가 없으면 입력 처리 불가
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            // R: 탑뷰 카메라만 남기는 모드로 전환
            if (kb.rKey.wasPressedThisFrame)
            {
                _rig.EnterTopOnly();
            }
        }
    }
}
