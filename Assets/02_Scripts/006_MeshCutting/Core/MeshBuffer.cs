using System.Collections.Generic; // List<T>
using UnityEngine; // Mesh, Vector3, Vector2

namespace OrganicEngineer.MeshCutting
{
    //==============================================================================
    // 기능: MeshBuffer (내부 클래스)
    // 설명: 절단 중 한쪽(Above 또는 Below) geometry를 verts/uv/normal/tris 에 누적하고, 선택적으로 서브메시 1(capTris)에 단면 삼각형을 담는다.
    //==============================================================================
    // TODO: 탄젠트·컬러 등 추가 채널이 필요하면 SetTangents 등 확장
    //
    //==============================================================================
    class MeshBuffer
    {
        // 로컬 누적 정점 목록(Mesh 버텍스 버퍼와 1:1)
        public readonly List<Vector3> verts = new List<Vector3>();
        // 정점별 UV 채널 0 데이터
        public readonly List<Vector2> uvs = new List<Vector2>();
        // 정점별 노멀(단위 벡터를 기대하지만 검증 책임은 호출측)
        public readonly List<Vector3> normals = new List<Vector3>();
        // 서브메시 0(외피) 삼각형 인덱스 나열 CW/CCW
        public readonly List<int> tris = new List<int>();

        // 서브메시 1 단면(캡) 삼각형 인덱스 — 비어 있으면 ToMesh 는 서브메시 1개만 설정
        public readonly List<int> capTris = new List<int>();

        //==============================================================================
        // 기능: MeshBuffer::AddVertex
        // 설명: pos·uv·normal 을 버퍼 끝에 추가하고 새 정점의 인덱스를 반환한다.
        //==============================================================================
        // TODO:
        //
        //==============================================================================
        public int AddVertex(Vector3 pos, Vector2 uv, Vector3 normal)
        {
            int idx = verts.Count;  // 새 정점 인덱스
            verts.Add(pos);         // 정점 목록에 추가
            uvs.Add(uv);            // 동일 인덱스 UV
            normals.Add(normal);    // 동일 인덱스 법선
            return idx;
        }

        //==============================================================================
        // 기능: MeshBuffer::AddTriangle
        // 설명: 외피 서브메시 0 에 정점 인덱스 세 개를 한 삼각형 면으로 추가한다.
        //==============================================================================
        // TODO:
        //
        //==============================================================================
        public void AddTriangle(int a, int b, int c)
        {
            tris.Add(a); // 삼각형 첫 정점 인덱스
            tris.Add(b); // 둘째 정점 인덱스
            tris.Add(c); // 셋째 정점 인덱스
        }

        //==============================================================================
        // 기능: MeshBuffer::AddCapTriangle
        // 설명: 단면 서브메시 1 에 정점 인덱스 세 개를 추가한다(Fan 분할 결과 등).
        //==============================================================================
        // TODO:
        //
        //==============================================================================
        public void AddCapTriangle(int a, int b, int c)
        {
            capTris.Add(a); // 캡 삼각형 첫 인덱스
            capTris.Add(b); // 캡 둘째
            capTris.Add(c); // 캡 셋째
        }

        //==============================================================================
        // 기능: MeshBuffer::ToMesh
        // 설명: 누적된 리스트로 Unity Mesh 를 생성하고, 캡 유무에 따라 서브메시 1 또는 2로 설정 후 바운드를 재계산한다.
        //==============================================================================
        // TODO: 인덱스 포맷·Read/Write 허용 옵션 명시 필요 시 플래그 추가
        //
        //==============================================================================
        public Mesh ToMesh()
        {
            var mesh = new Mesh();

            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetNormals(normals);

            if (capTris.Count == 0)
            {
                mesh.SetTriangles(tris, 0);
            }
            else
            {
                mesh.subMeshCount = 2;
                mesh.SetTriangles(tris, 0); // 외피 서브메시
                mesh.SetTriangles(capTris, 1); // 단면 서브메시
            }

            mesh.RecalculateBounds();

            return mesh;
        }
    }
}
