using System.Collections.Generic;
using UnityEngine;

namespace OrganicEngineer.MeshCutting
{
    //==============================================================================
    // 기능: MeshCutter
    // 설명: 메시를 로컬 평면 기준으로 자르는 절단 코어 진입점이다.
    //==============================================================================
    // TODO: 오목 단면, 스킨드 메시, 추가 버텍스 채널 지원
    //
    //==============================================================================
    public class MeshCutter
    {
        //-------------------- 멤버 변수 --------------------//

        // 반복 절단 중 재사용하는 작업 버퍼
        private readonly MeshCutterWorkBuffer _workBuffer;

        //-------------------- 생성 함수 --------------------//

        //==============================================================================
        // 기능: MeshCutter::MeshCutter
        // 설명: 절단 작업에 필요한 재사용 버퍼를 외부 조립 지점에서 주입받는다.
        //==============================================================================
        // TODO:
        //
        //==============================================================================
        internal MeshCutter(MeshCutterWorkBuffer workBuffer)
        {
            _workBuffer = workBuffer;
        }

        //-------------------- 기능 함수 --------------------//

        //==============================================================================
        // 기능: MeshCutter::Cut
        // 설명: 원본 Mesh 를 로컬 Plane 으로 분류/분할하고 Above/Below 결과 Mesh 를 반환한다.
        //==============================================================================
        // TODO: 절단 실패 원인 상세 코드 추가
        //
        //==============================================================================
        // src: 절단할 원본 Mesh
        // planeLocal: src 로컬 공간 기준 절단 평면
        // capSubMeshIndex: 기존 캡 subMesh 인덱스. -1 이면 기존 캡 없음.
        public CutResult Cut(Mesh src, Plane planeLocal, int capSubMeshIndex = -1)
        {
            return CutInternal(src, planeLocal, default, false, capSubMeshIndex);
        }

        public CutResult Cut(Mesh src, BoundedCutPlane boundedPlaneLocal, int capSubMeshIndex = -1)
        {
            if (!boundedPlaneLocal.IsValid)
            {
                CutResult invalidResult = new CutResult();
                invalidResult.Relation = CutPlaneRelation.EMPTY_SOURCE;
                return invalidResult;
            }

            CutResult result = CutInternal(src, boundedPlaneLocal.PlaneLocal, boundedPlaneLocal, true, capSubMeshIndex);
            MeshCutIslandUtility.KeepSmallestIslandTouchingSegment(result, boundedPlaneLocal);
            return result;
        }

        private CutResult CutInternal(Mesh src, Plane planeLocal, BoundedCutPlane boundedPlaneLocal, bool useBoundedPlane, int capSubMeshIndex)
        {
            // 절단 결과 컨테이너 생성
            CutResult result = new CutResult();

            // 원본 Mesh 가 비어 있으면 EmptySource 로 종료
            if (src == null)
            {
                result.Relation = CutPlaneRelation.EMPTY_SOURCE;
                return result;
            }

            // 이전 절단의 임시 데이터를 비우고 Capacity 는 재사용
            _workBuffer.Clear();

            // 원본 정점 목록 참조
            src.GetVertices(_workBuffer.srcVerts);

            // 정점이 없으면 EmptySource 로 종료
            if (_workBuffer.srcVerts.Count == 0)
            {
                result.Relation = CutPlaneRelation.EMPTY_SOURCE;
                return result;
            }

            // 원본 UV 목록 참조(없으면 빈 목록)
            src.GetUVs(0, _workBuffer.srcUVs);

            // 원본 노멀 목록 참조(없으면 빈 목록)
            src.GetNormals(_workBuffer.srcNormals);

            // 각 정점이 평면 어느 쪽인지 계산
            MeshCutUtility.BuildSideTable(_workBuffer.srcVerts, planeLocal, _workBuffer.side);

            // 평면 법선 +쪽 메시 버퍼
            MeshBuffer above = _workBuffer.above;

            // 평면 법선 반대쪽 메시 버퍼
            MeshBuffer below = _workBuffer.below;

            // 분할로 정점이 늘어날 수 있으므로 원본보다 넉넉하게 Capacity 확보
            int estimatedVertexCapacity = _workBuffer.srcVerts.Count * 2;
            above.EnsureVertexCapacity(estimatedVertexCapacity);
            below.EnsureVertexCapacity(estimatedVertexCapacity);

            // 삼각형 순회 중 실제 절단 발생 여부
            bool straddling = false;

            // 소스 삼각형 존재 여부
            bool hasAnySourceTriangle = false;

            for (int subMesh = 0; subMesh < src.subMeshCount; subMesh++)
            {
                // 원본 삼각형 인덱스 목록 참조
                src.GetTriangles(_workBuffer.srcTris, subMesh);
                if (_workBuffer.srcTris.Count == 0) continue;

                // 하나 이상의 소스 삼각형이 있음을 기록
                hasAnySourceTriangle = true;

                // 캡 subMesh 는 명시적으로 받은 인덱스만 사용한다. 원본 다중 외피 subMesh 와 충돌하지 않기 위함.
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

            // 삼각형이 하나도 없으면 EmptySource 로 종료
            if (!hasAnySourceTriangle)
            {
                result.Relation = CutPlaneRelation.EMPTY_SOURCE;
                return result;
            }

            // 절단 발생 여부에 따라 결과 관계와 캡을 정리
            if (useBoundedPlane && !straddling)
            {
                result.Relation = CutPlaneRelation.ENTIRELY_BELOW;
                return result;
            }

            MeshCutCap.AppendIfNeeded(straddling, planeLocal.normal, above, below, result, _workBuffer.cap);

            // Above 버퍼가 비어 있지 않으면 Mesh 로 변환
            result.AboveMesh = above.HasAnyTriangle() ? above.ToMesh() : null;

            // Below 버퍼가 비어 있지 않으면 Mesh 로 변환
            result.BelowMesh = below.HasAnyTriangle() ? below.ToMesh() : null;

            return result;
        }

    }

    //==============================================================================
    // 기능: MeshCutterWorkBuffer
    // 설명: MeshCutter 가 반복 절단 중 재사용하는 소스 채널/삼각형/분류/결과 버퍼를 보관한다.
    //==============================================================================
    // TODO: 탄젠트·컬러 등 추가 채널이 필요하면 소스 채널 리스트 확장
    //
    //==============================================================================
    readonly struct MeshCutterWorkBuffer
    {
        //-------------------- 멤버 변수 --------------------//

        // 원본 정점 채널 임시 목록
        public readonly List<Vector3> srcVerts;

        // 원본 UV0 채널 임시 목록
        public readonly List<Vector2> srcUVs;

        // 원본 노멀 채널 임시 목록
        public readonly List<Vector3> srcNormals;

        // 원본 subMesh 삼각형 인덱스 임시 목록
        public readonly List<int> srcTris;

        // 원본 정점별 평면 방향 분류 목록
        public readonly List<bool> side;

        // 평면 법선 +쪽 결과 누적 버퍼
        public readonly MeshBuffer above;

        // 평면 법선 반대쪽 결과 누적 버퍼
        public readonly MeshBuffer below;

        // 단면 캡 생성 중 재사용하는 보조 버퍼
        public readonly MeshCutCapBuffer cap;

        //-------------------- 생성 함수 --------------------//

        //==============================================================================
        // 기능: MeshCutterWorkBuffer::MeshCutterWorkBuffer
        // 설명: 외부 조립 지점에서 만든 작업용 리스트와 MeshBuffer 를 주입받는다.
        //==============================================================================
        // TODO:
        //
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
        // 설명: Capacity 는 유지하고 이전 절단에서 누적된 데이터만 비운다.
        //==============================================================================
        // TODO:
        //
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
