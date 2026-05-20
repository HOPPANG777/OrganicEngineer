using UnityEngine;

namespace OrganicEngineer.MeshCutting
{
    //==============================================================================
    // 기능: ConvexVolumeMeshCutter
    // 설명: 기존 단일 평면 MeshCutter 를 순차 호출해 볼록 볼륨 내부/외부 Mesh 를 분리한다.
    //==============================================================================
    public sealed class ConvexVolumeMeshCutter
    {
        //-------------------- 멤버 변수 --------------------//

        // 단일 평면 절단을 수행하는 코어 커터
        private readonly MeshCutter _meshCutter;

        //-------------------- 생성 함수 --------------------//

        //==============================================================================
        // 기능: ConvexVolumeMeshCutter::ConvexVolumeMeshCutter
        // 설명: 볼륨 절단에 사용할 단일 평면 커터를 주입받는다.
        //==============================================================================
        public ConvexVolumeMeshCutter(MeshCutter meshCutter)
        {
            _meshCutter = meshCutter;
        }

        //-------------------- 기능 함수 --------------------//

        //==============================================================================
        // 기능: ConvexVolumeMeshCutter::Cut
        // 설명: 볼륨 평면을 순서대로 적용해 Above 쪽 교집합을 내부 Mesh 로 남긴다.
        //==============================================================================
        public CutVolumeResult Cut(Mesh src, ConvexCutVolume volumeLocal, int capSubMeshIndex = -1)
        {
            CutVolumeResult result = new CutVolumeResult();
            if (src == null || !volumeLocal.IsValid || _meshCutter == null)
            {
                result.Relation = CutVolumeRelation.EMPTY_SOURCE;
                return result;
            }

            Mesh insideMesh = src;
            bool wasSplit = false;
            int activeCapSubMeshIndex = capSubMeshIndex;
            int planeCount = volumeLocal.PlaneCount;

            for (int i = 0; i < planeCount; i++)
            {
                CutResult planeResult = _meshCutter.Cut(insideMesh, volumeLocal.GetPlane(i), activeCapSubMeshIndex);
                if (planeResult.Relation == CutPlaneRelation.EMPTY_SOURCE)
                {
                    result.Relation = CutVolumeRelation.EMPTY_SOURCE;
                    return result;
                }

                if (planeResult.Relation == CutPlaneRelation.ENTIRELY_BELOW)
                {
                    result.OutsideMeshes.Clear();
                    result.Relation = CutVolumeRelation.ENTIRELY_OUTSIDE;
                    return result;
                }

                if (planeResult.Relation == CutPlaneRelation.ENTIRELY_ABOVE)
                {
                    activeCapSubMeshIndex = ResolveCapSubMeshIndex(insideMesh, activeCapSubMeshIndex);
                    continue;
                }

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
        // 설명: 다음 평면 절단에서 기존 캡 subMesh 를 유지하도록 마지막 subMesh 인덱스를 반환한다.
        //==============================================================================
        private static int ResolveCapSubMeshIndex(Mesh mesh, int fallbackIndex)
        {
            if (mesh == null || mesh.subMeshCount <= 0) return fallbackIndex;
            return mesh.subMeshCount - 1;
        }
    }
}
