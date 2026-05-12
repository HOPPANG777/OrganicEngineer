using UnityEngine;

namespace OrganicEngineer.MeshCutting.CutCamera
{
    //==============================================================================
    // 기능: CutCameraActive
    // 설명: 탑 카메라와 측면 카메라 배열의 enabled 상태를 현재 모드와 인덱스에 맞게 적용한다.
    //==============================================================================
    // TODO: 카메라 전환 페이드 옵션
    //
    //==============================================================================
    public class CutCameraActive
    {
        //==============================================================================
        // 기능: CutCameraActive::Apply
        // 설명: 탑뷰는 항상 켜고, 측면은 모드에 따라 전체 Off 또는 활성 인덱스 1대만 On 처리한다.
        //==============================================================================
        // TODO: null 카메라 진단 로그 옵션
        //
        //==============================================================================
        public void Apply(Camera topCamera, Camera[] sideCamera, int activeSideIndex, CutCameraMode mode)
        {
            // 탑뷰는 어떤 모드에서도 항상 활성화
            if (topCamera != null)
            {
                topCamera.enabled = true;
            }

            // 측면 카메라 배열이 없으면 처리 불가
            if (sideCamera == null) return;

            // 모든 측면 카메라를 순회해 활성 상태 적용
            for (int i = 0; i < sideCamera.Length; i++)
            {
                // 비어 있는 슬롯은 건너뜀
                if (sideCamera[i] == null) continue;

                // 탑 전용 모드이면 모든 측면 카메라 비활성화
                if (mode == CutCameraMode.TOP_ONLY)
                {
                    sideCamera[i].enabled = false;
                    continue;
                }

                // 탑+측면 모드이면 활성 인덱스만 활성화
                sideCamera[i].enabled = (i == activeSideIndex);
            }
        }
    }
}
