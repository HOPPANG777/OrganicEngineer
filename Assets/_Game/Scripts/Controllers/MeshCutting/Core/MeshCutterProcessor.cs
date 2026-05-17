using System.Collections.Generic;
using UnityEngine;

namespace OrganicEngineer.MeshCutting
{
    //==============================================================================
    // 기능: MeshCutterProcessor
    // 설명: MeshCutter의 내부 함수들을 모두 옮겨놓은 클래스
    //==============================================================================
    // TODO: 
    //
    //==============================================================================
    class MeshCutterProcessor
    {
        //-------------------- 맴버 변수 --------------------//

        // 절단되지 않은 삼각형 복사 기능
        private readonly MeshCutTriangle _meshCutTriangle;

        // 평면에 걸친 삼각형 분할 기능
        private readonly MeshCutSplit _meshCutSplit;

        // 절단면 캡 생성 기능
        private readonly MeshCutCap _meshCutCap;

        //-------------------- 생성 함수 --------------------//

        //==============================================================================
        // 기능: MeshCutterProcessor::MeshCutterProcessor
        // 설명: 삼각형 복사, 분할, 캡 생성 기능을 외부에서 주입받는다.
        //==============================================================================
        // TODO:
        //
        //==============================================================================
        public MeshCutterProcessor(MeshCutTriangle meshCutTriangle, MeshCutSplit meshCutSplit, MeshCutCap meshCutCap)
        {
            _meshCutTriangle = meshCutTriangle;
            _meshCutSplit = meshCutSplit;
            _meshCutCap = meshCutCap;
        }

        //-------------------- 기능 함수 --------------------//
        // 모든 정점의 평면 기준 방향 테이블 생성
        public void BuildSideTable(List<Vector3> srcVerts, Plane planeLocal, List<bool> side)
        {
            // 이전 분류 결과 초기화
            side.Clear();

            // 모든 정점에 대해 Plane.GetSide 계산
            for (int i = 0; i < srcVerts.Count; i++)
            {
                side.Add(planeLocal.GetSide(srcVerts[i]));
            }
        }

        // 모든 삼각형을 분류하거나 분할
        public bool ProcessTriangles
        (
            List<Vector3> srcVerts,
            List<Vector2> srcUVs,
            List<Vector3> srcNormals,
            List<int> srcTris,
            List<bool> side,
            Plane planeLocal,
            MeshBuffer above,
            MeshBuffer below,
            CutResult result,
            int sourceSubMeshIndex,
            bool isCap
        )
        {
            // 실제 절단 발생 여부
            bool straddling = false;

            // 삼각형 인덱스는 3개씩 한 면을 구성
            for (int i = 0; i < srcTris.Count; i += 3)
            {
                // 삼각형 정점 인덱스 3개
                int i0 = srcTris[i + 0];
                int i1 = srcTris[i + 1];
                int i2 = srcTris[i + 2];

                // 각 정점의 평면 방향
                bool s0 = side[i0];
                bool s1 = side[i1];
                bool s2 = side[i2];

                // 삼각형 분류/분할 처리
                straddling |= ProcessTriangle(srcVerts, srcUVs, srcNormals, side, planeLocal, above, below, result, i0, i1, i2, s0, s1, s2, sourceSubMeshIndex, isCap);
            }

            return straddling;
        }

        // 삼각형 하나를 복사 또는 분할
        public bool ProcessTriangle
        (
            List<Vector3> srcVerts,
            List<Vector2> srcUVs,
            List<Vector3> srcNormals,
            List<bool> side,
            Plane planeLocal,
            MeshBuffer above,
            MeshBuffer below,
            CutResult result,
            int i0,
            int i1,
            int i2,
            bool s0,
            bool s1,
            bool s2,
            int sourceSubMeshIndex,
            bool isCap
        )
        {
            // 세 정점이 모두 Above 면 Above 버퍼에 그대로 복사
            if (s0 && s1 && s2)
            {
                _meshCutTriangle.Copy(above, srcVerts, srcUVs, srcNormals, i0, i1, i2, sourceSubMeshIndex, isCap);
                return false;
            }

            // 세 정점이 모두 Below 면 Below 버퍼에 그대로 복사
            if (!s0 && !s1 && !s2)
            {
                _meshCutTriangle.Copy(below, srcVerts, srcUVs, srcNormals, i0, i1, i2, sourceSubMeshIndex, isCap);
                return false;
            }

            // 서로 다른 방향에 걸친 삼각형은 분할
            _meshCutSplit.Split(above, below, planeLocal, srcVerts, srcUVs, srcNormals, i0, i1, i2, s0, s1, s2, side, result, sourceSubMeshIndex, isCap);
            return true;
        }

        // 결과 관계와 캡 생성을 마무리
        public void ResolveResult
        (
            bool straddling,
            Vector3 planeNormal,
            MeshBuffer above,
            MeshBuffer below,
            CutResult result
        )
        {
            // 실제 절단이 있으면 양쪽 캡을 생성
            if (straddling)
            {
                // Above 조각의 고체는 평면 +법선 쪽이므로, 단면 바깥(절제된 반대 공간)을 향하는 법선은 -planeNormal.
                // Below 는 반대. 여기를 뒤집으면 캡 와인딩·정점 노멀이 안쪽을 향해 텍스처가 '속면'처럼 보인다.
                _meshCutCap.Append(above, result.aboveCapEdges, -planeNormal);
                _meshCutCap.Append(below, result.belowCapEdges, planeNormal);
                result.relation = CutPlaneRelation.Straddling;
                return;
            }

            // 절단이 없으면 캡 에지를 비움
            result.aboveCapEdges.Clear();
            result.belowCapEdges.Clear();

            // Above 버퍼에 외피 또는 캡 삼각형이 있는지 확인
            bool hasAbove = above.HasAnyTriangle();

            // Below 버퍼에 외피 또는 캡 삼각형이 있는지 확인
            bool hasBelow = below.HasAnyTriangle();

            // 버퍼 상태로 평면 관계 판정
            result.relation = ResolveRelation(hasAbove, hasBelow);
        }

        // Above/Below 존재 여부로 CutPlaneRelation 판정
        public CutPlaneRelation ResolveRelation(bool hasAbove, bool hasBelow)
        {
            // 양쪽 모두 비어 있으면 소스가 비어 있는 상태
            if (!hasAbove && !hasBelow) return CutPlaneRelation.EmptySource;

            // Above 만 있으면 전부 평면 +쪽
            if (hasAbove && !hasBelow) return CutPlaneRelation.EntirelyAbove;

            // Below 만 있으면 전부 평면 반대쪽
            if (!hasAbove && hasBelow) return CutPlaneRelation.EntirelyBelow;

            // 예외적으로 양쪽에 모두 있으면 Straddling 으로 처리
            return CutPlaneRelation.Straddling;
        }

    }
}