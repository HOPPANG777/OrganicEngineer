using UnityEngine;

namespace OrganicEngineer.MeshCutting.Sandbox
{
    //==============================================================================
    // 기능: CutPlaneConvert
    // 설명: 월드 공간 절단 평면을 대상 MeshFilter 의 로컬 공간 평면으로 변환한다.
    //==============================================================================
    // TODO: Transform non-uniform scale 케이스 검증
    //
    //==============================================================================
    public static class CutPlaneConvert
    {
        //==============================================================================
        // 기능: CutPlaneConvert::WorldToLocal
        // 설명: cutter.up/cutter.position 으로 만든 월드 평면을 target 로컬 Plane 으로 변환한다.
        //==============================================================================
        // TODO: cutter 대신 명시적 normal/point 입력 오버로드 추가
        //
        //==============================================================================
        public static Plane WorldToLocal(MeshFilter target, Transform cutter)
        {
            // 월드 공간 평면 생성
            Plane worldPlane = new Plane(cutter.up, cutter.position);

            // 법선은 방향 변환 후 정규화
            Vector3 localNormal = target.transform.InverseTransformDirection(worldPlane.normal).normalized;

            // 평면 위 기준점은 위치 변환
            Vector3 localPoint = target.transform.InverseTransformPoint(cutter.position);

            // Unity Plane 은 법선과 점으로 로컬 평면 구성
            return new Plane(localNormal, localPoint);
        }
    }
}
