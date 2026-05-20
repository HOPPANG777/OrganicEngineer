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
    public class CutPlaneConvert
    {
        //==============================================================================
        // 기능: CutPlaneConvert::WorldToLocal
        // 설명: cutter.up/cutter.position 으로 만든 월드 평면을 target 로컬 Plane 으로 변환한다.
        //==============================================================================
        public Plane WorldToLocal(MeshFilter target, Transform cutter)
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

        //==============================================================================
        // 기능: CutPlaneConvert::WorldToLocal
        // 설명: 월드 법선(정규화)·평면 위 한 점으로 만든 평면을 target 로컬 Plane 으로 변환한다.
        //==============================================================================
        public Plane WorldToLocal(MeshFilter target, Vector3 worldNormalNormalized, Vector3 worldPointOnPlane)
        {
            Vector3 localNormal = target.transform.InverseTransformDirection(worldNormalNormalized).normalized;
            Vector3 localPoint = target.transform.InverseTransformPoint(worldPointOnPlane);
            return new Plane(localNormal, localPoint);
        }

        public BoundedCutPlane WorldToLocalBounded
        (
            MeshFilter target,
            Vector3 worldNormalNormalized,
            Vector3 worldSegmentStart,
            Vector3 worldSegmentEnd,
            float worldSpanPadding,
            float worldBandPadding
        )
        {
            Plane localPlane = WorldToLocal(target, worldNormalNormalized, worldSegmentStart);
            Vector3 localStart = target.transform.InverseTransformPoint(worldSegmentStart);
            Vector3 localEnd = target.transform.InverseTransformPoint(worldSegmentEnd);

            float localSpanPadding = worldSpanPadding;
            float localBandPadding = worldBandPadding;
            Vector3 worldSegment = worldSegmentEnd - worldSegmentStart;
            Vector3 localSegment = localEnd - localStart;
            if (worldSegment.sqrMagnitude > 1e-10f && localSegment.sqrMagnitude > 1e-10f)
            {
                Vector3 worldPaddingVector = worldSegment.normalized * Mathf.Max(0f, worldSpanPadding);
                Vector3 localPaddingVector = target.transform.InverseTransformVector(worldPaddingVector);
                localSpanPadding = Mathf.Abs(Vector3.Dot(localPaddingVector, localSegment.normalized));

                Vector3 localBandVector = target.transform.InverseTransformVector(worldSegment.normalized * Mathf.Max(0f, worldBandPadding));
                localBandPadding = localBandVector.magnitude;
            }

            return new BoundedCutPlane(localPlane, localStart, localEnd, localSpanPadding, localBandPadding);
        }

        //==============================================================================
        // 기능: CutPlaneConvert::WorldToLocalBoxVolume
        // 설명: 월드 박스 볼륨의 6개 안쪽 방향 평면을 target 로컬 ConvexCutVolume 으로 변환한다.
        //==============================================================================
        public ConvexCutVolume WorldToLocalBoxVolume
        (
            MeshFilter target,
            Vector3 worldCenter,
            Quaternion worldRotation,
            Vector3 worldSize
        )
        {
            Vector3 halfSize = Vector3.Max(worldSize, Vector3.zero) * 0.5f;
            Vector3 axisX = worldRotation * Vector3.right;
            Vector3 axisY = worldRotation * Vector3.up;
            Vector3 axisZ = worldRotation * Vector3.forward;

            Plane[] planes = new Plane[6];
            planes[0] = WorldToLocal(target, -axisX, worldCenter + axisX * halfSize.x);
            planes[1] = WorldToLocal(target, axisX, worldCenter - axisX * halfSize.x);
            planes[2] = WorldToLocal(target, -axisY, worldCenter + axisY * halfSize.y);
            planes[3] = WorldToLocal(target, axisY, worldCenter - axisY * halfSize.y);
            planes[4] = WorldToLocal(target, -axisZ, worldCenter + axisZ * halfSize.z);
            planes[5] = WorldToLocal(target, axisZ, worldCenter - axisZ * halfSize.z);

            return new ConvexCutVolume(planes);
        }
    }
}
