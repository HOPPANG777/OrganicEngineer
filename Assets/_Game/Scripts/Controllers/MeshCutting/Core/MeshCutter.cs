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
        //-------------------- 맴버 변수 --------------------//

        // 절단되지 않은 삼각형 복사 기능
        private MeshCutTriangle _meshCutTriangle = new MeshCutTriangle();

        // 평면에 걸친 삼각형 분할 기능
        private MeshCutSplit _meshCutSplit = new MeshCutSplit();

        // 절단면 캡 생성 기능
        private MeshCutCap _meshCutCap = new MeshCutCap();

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
        public CutResult Cut(Mesh src, Plane planeLocal)
        {
            // 절단 결과 컨테이너 생성
            CutResult result = new CutResult();

            // 원본 Mesh 가 비어 있으면 EmptySource 로 종료
            if (src == null || src.triangles == null || src.triangles.Length == 0)
            {
                result.relation = CutPlaneRelation.EmptySource;
                return result;
            }

            // 원본 정점 배열 참조
            Vector3[] srcVerts = src.vertices;

            // 정점이 없으면 EmptySource 로 종료
            if (srcVerts == null || srcVerts.Length == 0)
            {
                result.relation = CutPlaneRelation.EmptySource;
                return result;
            }

            // 원본 UV 배열 참조(null 가능)
            Vector2[] srcUVs = src.uv;

            // 원본 노멀 배열 참조(null 가능)
            Vector3[] srcNormals = src.normals;

            // 원본 삼각형 인덱스 배열 참조
            int[] srcTris = src.triangles;

            // 각 정점이 평면 어느 쪽인지 계산
            bool[] side = BuildSideTable(srcVerts, planeLocal);

            // 평면 법선 +쪽 메시 버퍼
            MeshBuffer above = new MeshBuffer();

            // 평면 법선 반대쪽 메시 버퍼
            MeshBuffer below = new MeshBuffer();

            // 삼각형 순회 중 실제 절단 발생 여부
            bool straddling = ProcessTriangles(srcVerts, srcUVs, srcNormals, srcTris, side, planeLocal, above, below, result);

            // 절단 발생 여부에 따라 결과 관계와 캡을 정리
            ResolveResult(straddling, planeLocal.normal, above, below, result);

            // Above 버퍼가 비어 있지 않으면 Mesh 로 변환
            result.aboveMesh = above.tris.Count > 0 ? above.ToMesh() : null;

            // Below 버퍼가 비어 있지 않으면 Mesh 로 변환
            result.belowMesh = below.tris.Count > 0 ? below.ToMesh() : null;

            return result;
        }

        // 모든 정점의 평면 기준 방향 테이블 생성
        private bool[] BuildSideTable(Vector3[] srcVerts, Plane planeLocal)
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
        private bool ProcessTriangles(Vector3[] srcVerts, Vector2[] srcUVs, Vector3[] srcNormals, int[] srcTris, bool[] side, Plane planeLocal, MeshBuffer above, MeshBuffer below, CutResult result)
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
                straddling |= ProcessTriangle(srcVerts, srcUVs, srcNormals, side, planeLocal, above, below, result, i0, i1, i2, s0, s1, s2);
            }

            return straddling;
        }

        // 삼각형 하나를 복사 또는 분할
        private bool ProcessTriangle(Vector3[] srcVerts, Vector2[] srcUVs, Vector3[] srcNormals, bool[] side, Plane planeLocal, MeshBuffer above, MeshBuffer below, CutResult result, int i0, int i1, int i2, bool s0, bool s1, bool s2)
        {
            // 세 정점이 모두 Above 면 Above 버퍼에 그대로 복사
            if (s0 && s1 && s2)
            {
                _meshCutTriangle.Copy(above, srcVerts, srcUVs, srcNormals, i0, i1, i2);
                return false;
            }

            // 세 정점이 모두 Below 면 Below 버퍼에 그대로 복사
            if (!s0 && !s1 && !s2)
            {
                _meshCutTriangle.Copy(below, srcVerts, srcUVs, srcNormals, i0, i1, i2);
                return false;
            }

            // 서로 다른 방향에 걸친 삼각형은 분할
            _meshCutSplit.Split(above, below, planeLocal, srcVerts, srcUVs, srcNormals, i0, i1, i2, s0, s1, s2, side, result);
            return true;
        }

        // 결과 관계와 캡 생성을 마무리
        private void ResolveResult(bool straddling, Vector3 planeNormal, MeshBuffer above, MeshBuffer below, CutResult result)
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
        private CutPlaneRelation ResolveRelation(bool hasAbove, bool hasBelow)
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
