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
        private MeshCutTriangle _meshCutTriangle = new MeshCutTriangle();

        // 평면에 걸친 삼각형 분할 기능
        private MeshCutSplit _meshCutSplit = new MeshCutSplit();

        // 절단면 캡 생성 기능
        private MeshCutCap _meshCutCap = new MeshCutCap();

        //-------------------- 기능 함수 --------------------//
        // 모든 정점의 평면 기준 방향 테이블 생성
        public bool[] BuildSideTable(Vector3[] srcVerts, Plane planeLocal)
        {
            // 정점 개수만큼 side 배열 생성
            bool[] side = new bool[srcVerts.Length];

            // 모든 정점에 대해 Plane.GetSide 계산
            for (int i = 0; i < srcVerts.Length; i++)
            {
                side[i] = planeLocal.GetSide(srcVerts[i]);
            }

            return side;
        }

        // 모든 삼각형을 분류하거나 분할
        public bool ProcessTriangles
        (
            Vector3[] srcVerts,
            Vector2[] srcUVs,
            Vector3[] srcNormals,
            int[] srcTris,
            bool[] side,
            Plane planeLocal,
            MeshBuffer above,
            MeshBuffer below,
            CutResult result,
            bool isCap
        )
        {
            // 실제 절단 발생 여부
            bool straddling = false;

            // 삼각형 인덱스는 3개씩 한 면을 구성
            for (int i = 0; i < srcTris.Length; i += 3)
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
                straddling |= ProcessTriangle(srcVerts, srcUVs, srcNormals, side, planeLocal, above, below, result, i0, i1, i2, s0, s1, s2, isCap);
            }

            return straddling;
        }

        // 삼각형 하나를 복사 또는 분할
        public bool ProcessTriangle
        (
            Vector3[] srcVerts,
            Vector2[] srcUVs,
            Vector3[] srcNormals,
            bool[] side,
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
            bool isCap
        )
        {
            // 세 정점이 모두 Above 면 Above 버퍼에 그대로 복사
            if (s0 && s1 && s2)
            {
                _meshCutTriangle.Copy(above, srcVerts, srcUVs, srcNormals, i0, i1, i2, isCap);
                return false;
            }

            // 세 정점이 모두 Below 면 Below 버퍼에 그대로 복사
            if (!s0 && !s1 && !s2)
            {
                _meshCutTriangle.Copy(below, srcVerts, srcUVs, srcNormals, i0, i1, i2, isCap);
                return false;
            }

            // 서로 다른 방향에 걸친 삼각형은 분할
            _meshCutSplit.Split(above, below, planeLocal, srcVerts, srcUVs, srcNormals, i0, i1, i2, s0, s1, s2, side, result, isCap);
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

            // Above 버퍼에 외피 삼각형이 있는지 확인
            bool hasAbove = above.tris.Count > 0;

            // Below 버퍼에 외피 삼각형이 있는지 확인
            bool hasBelow = below.tris.Count > 0;

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