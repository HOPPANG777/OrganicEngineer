using UnityEngine;

namespace OrganicEngineer.MeshCutting.CutCamera
{
    //==============================================================================
    // 기능: CutCameraPlace
    // 설명: Bounds 기준으로 탑/측면 카메라의 위치, 회전, OrthographicSize 를 적용한다.
    //==============================================================================
    // TODO: Perspective 거리 계산 지원
    //
    //==============================================================================
    public class CutCameraPlace
    {
        //-------------------- 맴버 변수 --------------------//

        // OrthographicSize 계산 기능
        private CutCameraSize _cutCameraSize = new CutCameraSize();

        //==============================================================================
        // 기능: CutCameraPlace::ApplyTop
        // 설명: 탑뷰 카메라를 Bounds 중심 위쪽에 배치하고 크기를 맞춘다.
        //==============================================================================
        // TODO: 탑뷰 upHint 옵션화
        //
        //==============================================================================
        public void ApplyTop(Camera camera, Bounds bounds, float paddingRatio, float extraDistance, bool useOrthographic)
        {
            // 카메라가 없으면 배치 불가
            if (camera == null) return;

            // Bounds 중심점 캐싱
            Vector3 center = bounds.center;

            // Bounds 반지름 캐싱
            Vector3 extents = bounds.extents;

            // 카메라 위치 계산
            Vector3 position = center + Vector3.up * (extents.y * paddingRatio + extraDistance);

            // 중심을 바라보는 회전 계산
            Quaternion rotation = Quaternion.LookRotation(center - position, Vector3.forward);

            // 위치와 회전 적용
            camera.transform.SetPositionAndRotation(position, rotation);

            // Orthographic 사용 여부 적용
            camera.orthographic = useOrthographic;

            // Orthographic 모드이면 화면 크기 적용
            if (useOrthographic)
            {
                camera.orthographicSize = _cutCameraSize.ComputeOrtho(extents.z, extents.x, camera.aspect, paddingRatio);
            }
        }

        //==============================================================================
        // 기능: CutCameraPlace::ApplySide
        // 설명: 측면 카메라를 지정 방향 바깥에 배치하고 축 기준 크기를 맞춘다.
        //==============================================================================
        // TODO: 카메라별 upHint 별도 설정
        //
        //==============================================================================
        public void ApplySide(Camera camera, Vector3 worldDir, Vector3 verticalAxis, Vector3 horizontalAxis, Bounds bounds, float paddingRatio, float extraDistance, bool useOrthographic)
        {
            // 카메라가 없으면 배치 불가
            if (camera == null) return;

            // Bounds 중심점 캐싱
            Vector3 center = bounds.center;

            // Bounds 반지름 캐싱
            Vector3 extents = bounds.extents;

            // 카메라 방향에서 필요한 거리 반치수 계산
            float extentAlong = Mathf.Abs(Vector3.Dot(worldDir, extents));

            // 카메라 위치 계산
            Vector3 position = center + worldDir * (extentAlong * paddingRatio + extraDistance);

            // 중심을 바라보는 회전 계산
            Quaternion rotation = Quaternion.LookRotation(center - position, Vector3.up);

            // 위치와 회전 적용
            camera.transform.SetPositionAndRotation(position, rotation);

            // Orthographic 사용 여부 적용
            camera.orthographic = useOrthographic;

            // Orthographic 모드이면 화면 크기 적용
            if (useOrthographic)
            {
                // 화면 세로축 Bounds 반치수
                float halfHeight = Mathf.Abs(Vector3.Dot(verticalAxis, extents));

                // 화면 가로축 Bounds 반치수
                float halfWidth = Mathf.Abs(Vector3.Dot(horizontalAxis, extents));

                // 카메라 OrthographicSize 적용
                camera.orthographicSize = _cutCameraSize.ComputeOrtho(halfHeight, halfWidth, camera.aspect, paddingRatio);
            }
        }
    }
}
