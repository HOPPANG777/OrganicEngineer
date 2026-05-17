using System.Collections.Generic;
using UnityEngine;

namespace OrganicEngineer.MeshCutting
{
    //==============================================================================
    // 기능: MeshBuffer (내부 클래스)
    // 설명: 절단 중 한쪽(Above 또는 Below) geometry를 verts/uv/normal 에 누적하고, 외피 subMesh 별 삼각형과 단면 캡 삼각형을 분리해 담는다.
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
        // 원본 외피 subMesh 번호별 삼각형 인덱스 나열 CW/CCW
        private readonly List<List<int>> _shellTrisBySubMesh = new List<List<int>>();

        // 마지막 subMesh 단면(캡) 삼각형 인덱스, 비어 있으면 외피 subMesh 만 설정
        public readonly List<int> capTris = new List<int>();

        //==============================================================================
        // 기능: MeshBuffer::Clear
        // 설명: 리스트 Capacity 는 유지하고 절단 누적 데이터만 초기화한다.
        //==============================================================================
        // TODO:
        //
        //==============================================================================
        public void Clear()
        {
            verts.Clear();
            uvs.Clear();
            normals.Clear();
            capTris.Clear();

            int shellSubMeshCount = _shellTrisBySubMesh.Count;
            for (int i = 0; i < shellSubMeshCount; i++)
            {
                _shellTrisBySubMesh[i].Clear();
            }
        }

        //==============================================================================
        // 기능: MeshBuffer::EnsureCapacity
        // 설명: 예상 정점 수만큼 내부 리스트 Capacity 를 미리 확보한다.
        //==============================================================================
        // TODO: subMesh 별 삼각형 Capacity 예측 필요 시 확장
        //
        //==============================================================================
        public void EnsureCapacity(int vertexCapacity)
        {
            if (verts.Capacity < vertexCapacity) verts.Capacity = vertexCapacity;
            if (uvs.Capacity < vertexCapacity) uvs.Capacity = vertexCapacity;
            if (normals.Capacity < vertexCapacity) normals.Capacity = vertexCapacity;
        }

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
        // 설명: 외피는 원본 subMesh 번호에, 캡은 마지막 캡 목록에 삼각형 면을 추가한다.
        //==============================================================================
        // TODO:
        //
        //==============================================================================
        public void AddTriangle(int a, int b, int c, int sourceSubMeshIndex, bool isCap)
        {
            if (isCap)
            {
                AddCapTriangle(a, b, c);
                return;
            }

            List<int> shellTris = GetOrCreateShellTris(sourceSubMeshIndex);
            shellTris.Add(a);
            shellTris.Add(b);
            shellTris.Add(c);
        }

        //==============================================================================
        // 기능: MeshBuffer::AddCapTriangle
        // 설명: 마지막 단면 subMesh 에 정점 인덱스 세 개를 추가한다(Fan 분할 결과 등).
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
        // 설명: 누적된 리스트로 Unity Mesh 를 생성하고, 외피 subMesh 뒤에 캡 subMesh 를 붙인 뒤 바운드를 재계산한다.
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

            int shellSubMeshCount = _shellTrisBySubMesh.Count;
            int totalSubMeshCount = shellSubMeshCount + (capTris.Count > 0 ? 1 : 0);
            mesh.subMeshCount = Mathf.Max(1, totalSubMeshCount);

            for (int subMesh = 0; subMesh < shellSubMeshCount; subMesh++)
            {
                mesh.SetTriangles(_shellTrisBySubMesh[subMesh], subMesh);
            }

            if (capTris.Count > 0)
            {
                mesh.SetTriangles(capTris, shellSubMeshCount);
            }

            mesh.RecalculateBounds();

            return mesh;
        }

        // 외피 또는 캡 삼각형이 하나라도 있는지 확인한다.
        public bool HasAnyTriangle()
        {
            if (capTris.Count > 0) return true;

            int shellSubMeshCount = _shellTrisBySubMesh.Count;
            for (int i = 0; i < shellSubMeshCount; i++)
            {
                if (_shellTrisBySubMesh[i].Count > 0) return true;
            }

            return false;
        }

        // 원본 subMesh 번호에 대응하는 외피 삼각형 목록을 확보한다.
        private List<int> GetOrCreateShellTris(int sourceSubMeshIndex)
        {
            if (sourceSubMeshIndex < 0) sourceSubMeshIndex = 0;

            while (_shellTrisBySubMesh.Count <= sourceSubMeshIndex)
            {
                _shellTrisBySubMesh.Add(new List<int>());
            }

            return _shellTrisBySubMesh[sourceSubMeshIndex];
        }
    }
}
