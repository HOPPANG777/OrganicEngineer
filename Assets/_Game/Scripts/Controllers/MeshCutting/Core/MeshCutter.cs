using System.Collections.Generic;
using UnityEngine;

namespace OrganicEngineer.MeshCutting
{
    //==============================================================================
    // 기능: MeshCutter
    // 설명: Mesh 를 로컬 절단 평면 기준으로 분류하고 Above/Below 결과 Mesh 를 생성하는 진입점이다.
    //==============================================================================
    // TODO: 색상, 탄젠트, 본 가중치 등 추가 버텍스 채널 보간을 지원한다.
    //
    //==============================================================================
    public class MeshCutter
    {
        //-------------------- 멤버 변수 --------------------//

        // 반복 절단 중 임시 리스트와 결과 버퍼를 재사용하기 위한 작업 버퍼
        private readonly MeshCutterWorkBuffer _workBuffer;

        //-------------------- 생성 함수 --------------------//

        //==============================================================================
        // 기능: MeshCutter::MeshCutter
        // 설명: 조립 지점에서 만든 작업 버퍼를 주입받아 절단 중 반복 할당을 줄인다.
        //==============================================================================
        // workBuffer: 소스 채널, 분류표, 결과 MeshBuffer 를 담은 재사용 버퍼
        internal MeshCutter(MeshCutterWorkBuffer workBuffer)
        {
            _workBuffer = workBuffer;
        }

        //-------------------- 기능 함수 --------------------//

        //==============================================================================
        // 기능: MeshCutter::Cut
        // 설명: 무한 로컬 Plane 으로 Mesh 를 분류하고 실제 교차 삼각형을 분할한다.
        //==============================================================================
        // src: 절단할 원본 Mesh
        // planeLocal: src 로컬 좌표 기준 절단 평면
        // capSubMeshIndex: 기존 캡 subMesh 인덱스. -1 이면 기존 캡이 없다고 본다.
        // 반환: Above/Below 결과 Mesh 와 절단 관계
        public CutResult Cut(Mesh src, Plane planeLocal, int capSubMeshIndex = -1)
        {
            return CutInternal(src, planeLocal, default, false, capSubMeshIndex);
        }

        //==============================================================================
        // 기능: MeshCutter::Cut
        // 설명: 드래그 선분 범위로 제한된 로컬 Plane 을 적용하고, 선분에 닿은 작은 조각을 선택한다.
        //==============================================================================
        // src: 절단할 원본 Mesh
        // boundedPlaneLocal: 절단 평면과 드래그 선분 범위 정보
        // capSubMeshIndex: 기존 캡 subMesh 인덱스. -1 이면 기존 캡이 없다고 본다.
        // 반환: 제한 영역 절단 결과
        public CutResult Cut(Mesh src, BoundedCutPlane boundedPlaneLocal, int capSubMeshIndex = -1)
        {
            if (!boundedPlaneLocal.IsValid)
            {
                CutResult invalidResult = new CutResult();
                invalidResult.Relation = CutPlaneRelation.EMPTY_SOURCE;
                return invalidResult;
            }

            CutResult result = CutInternal(src, boundedPlaneLocal.PlaneLocal, boundedPlaneLocal, true, capSubMeshIndex);

            // 제한 절단은 전체 평면으로 한 번 자른 뒤, 드래그 선분에 닿은 섬만 잘린 조각으로 고른다.
            MeshCutIslandUtility.KeepSmallestIslandTouchingSegment(result, boundedPlaneLocal);
            return result;
        }

        //==============================================================================
        // 기능: MeshCutter::CutInternal
        // 설명: 소스 Mesh 채널을 읽고 subMesh 별 삼각형을 분류한 뒤 캡과 결과 Mesh 를 완성한다.
        //==============================================================================
        // src: 절단할 Mesh
        // planeLocal: src 로컬 좌표 기준 절단 평면
        // boundedPlaneLocal: 제한 절단일 때 사용할 선분 범위 정보
        // useBoundedPlane: true 이면 선분 길이 범위 밖의 삼각형은 자르지 않는다.
        // capSubMeshIndex: 기존 캡으로 취급할 subMesh 인덱스
        // 반환: 절단 관계와 결과 Mesh
        private CutResult CutInternal(Mesh src, Plane planeLocal, BoundedCutPlane boundedPlaneLocal, bool useBoundedPlane, int capSubMeshIndex)
        {
            CutResult result = new CutResult();
            if (src == null)
            {
                result.Relation = CutPlaneRelation.EMPTY_SOURCE;
                return result;
            }

            // 이전 절단의 임시 데이터는 비우고 Capacity 는 재사용한다.
            _workBuffer.Clear();

            src.GetVertices(_workBuffer.srcVerts);
            if (_workBuffer.srcVerts.Count == 0)
            {
                result.Relation = CutPlaneRelation.EMPTY_SOURCE;
                return result;
            }

            // UV와 노멀은 없을 수 있으므로 MeshCutUtility.GetUV/GetNormal 에서 기본값을 보정한다.
            src.GetUVs(0, _workBuffer.srcUVs);
            src.GetNormals(_workBuffer.srcNormals);

            // 정점별로 평면의 어느 쪽에 있는지 미리 계산해 삼각형 처리에서 재사용한다.
            MeshCutUtility.BuildSideTable(_workBuffer.srcVerts, planeLocal, _workBuffer.side);

            MeshBuffer above = _workBuffer.above;
            MeshBuffer below = _workBuffer.below;

            // 교차 삼각형은 정점이 늘어나므로 원본 정점 수의 두 배 정도를 선확보한다.
            int estimatedVertexCapacity = _workBuffer.srcVerts.Count * 2;
            above.EnsureVertexCapacity(estimatedVertexCapacity);
            below.EnsureVertexCapacity(estimatedVertexCapacity);

            bool straddling = false;
            bool hasAnySourceTriangle = false;

            for (int subMesh = 0; subMesh < src.subMeshCount; subMesh++)
            {
                src.GetTriangles(_workBuffer.srcTris, subMesh);
                if (_workBuffer.srcTris.Count == 0) continue;

                hasAnySourceTriangle = true;

                // 기존 캡 subMesh 를 다시 자를 때는 외피가 아니라 캡 전용 subMesh 로 계속 누적한다.
                bool isCap = subMesh == capSubMeshIndex;

                straddling |= MeshCutUtility.ProcessTriangles(
                    _workBuffer.srcVerts,
                    _workBuffer.srcUVs,
                    _workBuffer.srcNormals,
                    _workBuffer.srcTris,
                    _workBuffer.side,
                    planeLocal,
                    above,
                    below,
                    result,
                    subMesh,
                    isCap,
                    boundedPlaneLocal,
                    useBoundedPlane
                );
            }

            if (!hasAnySourceTriangle)
            {
                result.Relation = CutPlaneRelation.EMPTY_SOURCE;
                return result;
            }

            // 제한 절단에서는 실제 분할이 하나도 없으면 선택할 조각도 없으므로 Below 로 처리한다.
            if (useBoundedPlane && !straddling)
            {
                result.Relation = CutPlaneRelation.ENTIRELY_BELOW;
                return result;
            }

            // 실제 교차가 있으면 캡을 만들고, 없으면 Above/Below 버퍼 상태로 관계만 정한다.
            MeshCutCap.AppendIfNeeded(straddling, planeLocal.normal, above, below, result, _workBuffer.cap);

            result.AboveMesh = above.HasAnyTriangle() ? above.ToMesh() : null;
            result.BelowMesh = below.HasAnyTriangle() ? below.ToMesh() : null;

            return result;
        }
    }

    //==============================================================================
    // 기능: MeshCutterWorkBuffer
    // 설명: MeshCutter 가 한 번의 절단 동안 사용할 소스 채널, 분류표, 결과 버퍼를 묶는다.
    //==============================================================================
    // TODO: 추가 버텍스 채널을 지원할 때 소스 채널 리스트를 함께 확장한다.
    //
    //==============================================================================
    readonly struct MeshCutterWorkBuffer
    {
        //-------------------- 멤버 변수 --------------------//

        // 소스 Mesh 의 정점 위치 임시 목록
        public readonly List<Vector3> srcVerts;

        // 소스 Mesh 의 UV0 임시 목록
        public readonly List<Vector2> srcUVs;

        // 소스 Mesh 의 노멀 임시 목록
        public readonly List<Vector3> srcNormals;

        // 현재 처리 중인 subMesh 의 삼각형 인덱스 임시 목록
        public readonly List<int> srcTris;

        // 각 소스 정점이 절단 평면의 + 방향에 있는지 기록한 분류표
        public readonly List<bool> side;

        // 평면 법선 + 방향 결과를 누적하는 버퍼
        public readonly MeshBuffer above;

        // 평면 법선 반대 방향 결과를 누적하는 버퍼
        public readonly MeshBuffer below;

        // 절단면 캡 생성에 필요한 루프, 투영, 삼각분할 임시 버퍼
        public readonly MeshCutCapBuffer cap;

        //-------------------- 생성 함수 --------------------//

        //==============================================================================
        // 기능: MeshCutterWorkBuffer::MeshCutterWorkBuffer
        // 설명: 외부 조립 지점에서 만든 임시 리스트와 MeshBuffer 를 하나의 작업 단위로 저장한다.
        //==============================================================================
        public MeshCutterWorkBuffer
        (
            List<Vector3> srcVerts,
            List<Vector2> srcUVs,
            List<Vector3> srcNormals,
            List<int> srcTris,
            List<bool> side,
            MeshBuffer above,
            MeshBuffer below,
            MeshCutCapBuffer cap
        )
        {
            this.srcVerts = srcVerts;
            this.srcUVs = srcUVs;
            this.srcNormals = srcNormals;
            this.srcTris = srcTris;
            this.side = side;
            this.above = above;
            this.below = below;
            this.cap = cap;
        }

        //-------------------- 기능 함수 --------------------//

        //==============================================================================
        // 기능: MeshCutterWorkBuffer::Clear
        // 설명: 다음 절단을 시작하기 전에 임시 데이터만 비우고 내부 Capacity 는 유지한다.
        //==============================================================================
        public void Clear()
        {
            srcVerts.Clear();
            srcUVs.Clear();
            srcNormals.Clear();
            srcTris.Clear();
            side.Clear();
            above.Clear();
            below.Clear();
        }
    }
}
