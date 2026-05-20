using UnityEngine;

namespace OrganicEngineer.MeshCutting
{
    //==============================================================================
    // 기능: ConvexVolumeMeshCutter
    // 설명: 단일 평면 MeshCutter 를 여러 번 적용해 볼록 볼륨 내부와 외부 Mesh 를 분리한다.
    //==============================================================================
    public sealed class ConvexVolumeMeshCutter
    {
        //-------------------- 멤버 변수 --------------------//

        // 실제 삼각형 분류와 평면 절단을 담당하는 단일 평면 커터
        private readonly MeshCutter _meshCutter;

        //-------------------- 생성 함수 --------------------//

        //==============================================================================
        // 기능: ConvexVolumeMeshCutter::ConvexVolumeMeshCutter
        // 설명: 볼륨 절단에서 재사용할 단일 평면 커터를 주입받는다.
        //==============================================================================
        // meshCutter: 각 볼륨 평면을 순차 적용할 MeshCutter
        public ConvexVolumeMeshCutter(MeshCutter meshCutter)
        {
            _meshCutter = meshCutter;
        }

        //-------------------- 기능 함수 --------------------//

        //==============================================================================
        // 기능: ConvexVolumeMeshCutter::Cut
        // 설명: 볼륨 평면을 순서대로 적용해 모든 평면의 Above 교집합을 내부 Mesh 로 남긴다.
        //==============================================================================
        // src: 절단할 원본 Mesh
        // volumeLocal: src 로컬 좌표 기준의 볼록 절단 볼륨
        // capSubMeshIndex: 기존 캡 subMesh 인덱스. -1 이면 기존 캡이 없다고 본다.
        // 반환: 내부 Mesh, 외부 Mesh 목록, 볼륨 관계가 담긴 결과
        public CutVolumeResult Cut(Mesh src, ConvexCutVolume volumeLocal, int capSubMeshIndex = -1)
        {
            CutVolumeResult result = new CutVolumeResult();
            if (src == null || !volumeLocal.IsValid || _meshCutter == null)
            {
                result.Relation = CutVolumeRelation.EMPTY_SOURCE;
                return result;
            }

            // insideMesh 는 지금까지 처리한 평면들의 내부 교집합이다.
            Mesh insideMesh = src;
            bool wasSplit = false;
            int activeCapSubMeshIndex = capSubMeshIndex;
            int planeCount = volumeLocal.PlaneCount;

            for (int i = 0; i < planeCount; i++)
            {
                // 현재 내부 후보를 다음 볼륨 경계 평면으로 한 번 더 절단한다.
                CutResult planeResult = _meshCutter.Cut(insideMesh, volumeLocal.GetPlane(i), activeCapSubMeshIndex);
                if (planeResult.Relation == CutPlaneRelation.EMPTY_SOURCE)
                {
                    result.Relation = CutVolumeRelation.EMPTY_SOURCE;
                    return result;
                }

                // 볼륨 내부가 평면 Above 이므로 전부 Below 라면 볼륨 밖에만 있는 Mesh 다.
                if (planeResult.Relation == CutPlaneRelation.ENTIRELY_BELOW)
                {
                    result.OutsideMeshes.Clear();
                    result.Relation = CutVolumeRelation.ENTIRELY_OUTSIDE;
                    return result;
                }

                // 전부 Above 라면 이 평면에서는 잘리지 않았으므로 다음 평면으로 넘어간다.
                if (planeResult.Relation == CutPlaneRelation.ENTIRELY_ABOVE)
                {
                    activeCapSubMeshIndex = ResolveCapSubMeshIndex(insideMesh, activeCapSubMeshIndex);
                    continue;
                }

                // 절단된 경우 Below 는 볼륨 밖 조각, Above 는 다음 평면에 넘길 내부 후보가 된다.
                wasSplit = true;
                result.AddOutsideMesh(planeResult.BelowMesh);
                insideMesh = planeResult.AboveMesh;
                activeCapSubMeshIndex = ResolveCapSubMeshIndex(insideMesh, activeCapSubMeshIndex);
                if (insideMesh == null)
                {
                    result.Relation = CutVolumeRelation.INTERSECTING;
                    return result;
                }
            }

            result.InsideMesh = insideMesh;
            result.Relation = wasSplit ? CutVolumeRelation.INTERSECTING : CutVolumeRelation.ENTIRELY_INSIDE;
            return result;
        }

        //==============================================================================
        // 기능: ConvexVolumeMeshCutter::ResolveCapSubMeshIndex
        // 설명: 연속 평면 절단에서 직전 절단이 만든 마지막 subMesh 를 기존 캡으로 이어받는다.
        //==============================================================================
        // mesh: 다음 절단의 입력 Mesh
        // fallbackIndex: mesh 가 비어 있을 때 유지할 이전 캡 인덱스
        // 반환: 다음 절단에 전달할 캡 subMesh 인덱스
        private static int ResolveCapSubMeshIndex(Mesh mesh, int fallbackIndex)
        {
            if (mesh == null || mesh.subMeshCount <= 0) return fallbackIndex;
            return mesh.subMeshCount - 1;
        }
    }
}
