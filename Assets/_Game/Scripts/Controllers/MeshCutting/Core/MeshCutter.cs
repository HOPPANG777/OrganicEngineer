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

        // MeshCutter에서 사용하는 다양한 함수의 모음
        private MeshCutterProcessor _meshCutterProcessor = new MeshCutterProcessor();

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

            // 각 정점이 평면 어느 쪽인지 계산
            bool[] side = _meshCutterProcessor.BuildSideTable(srcVerts, planeLocal);

            // 평면 법선 +쪽 메시 버퍼
            MeshBuffer above = new MeshBuffer();

            // 평면 법선 반대쪽 메시 버퍼
            MeshBuffer below = new MeshBuffer();

            // 삼각형 순회 중 실제 절단 발생 여부
            bool straddling = false;

            for (int subMesh = 0; subMesh < src.subMeshCount; subMesh++)
            {
                // 원본 삼각형 인덱스 배열 참조
                int[] srcTris = src.GetTriangles(subMesh);

                // 현재 구조 기준: 0 = 외피, 1 = 캡
                bool isCap = subMesh == 1;

                straddling |= _meshCutterProcessor.ProcessTriangles(
                    srcVerts,
                    srcUVs,
                    srcNormals,
                    srcTris,
                    side,
                    planeLocal,
                    above,
                    below,
                    result,
                    isCap
                );
            }

            // 절단 발생 여부에 따라 결과 관계와 캡을 정리
            _meshCutterProcessor.ResolveResult(straddling, planeLocal.normal, above, below, result);

            // Above 버퍼가 비어 있지 않으면 Mesh 로 변환
            result.aboveMesh = above.tris.Count > 0 || above.capTris.Count > 0 ? above.ToMesh() : null;

            // Below 버퍼가 비어 있지 않으면 Mesh 로 변환
            result.belowMesh = below.tris.Count > 0 || below.capTris.Count > 0 ? below.ToMesh() : null;

            return result;
        }

    }

}
