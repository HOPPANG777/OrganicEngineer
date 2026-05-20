using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace OrganicEngineer.MeshCutting
{
    //==============================================================================
    // 기능: MeshBuffer
    // 설명: 절단 결과 Mesh 의 정점 채널과 subMesh 별 삼각형 인덱스를 누적하는 재사용 버퍼이다.
    //==============================================================================
    internal sealed class MeshBuffer
    {
        //-------------------- 멤버 변수 --------------------//

        // 결과 Mesh 의 정점 위치 목록
        private readonly List<Vector3> _verts = new List<Vector3>();

        // 결과 Mesh 의 UV0 목록
        private readonly List<Vector2> _uvs = new List<Vector2>();

        // 결과 Mesh 의 노멀 목록
        private readonly List<Vector3> _normals = new List<Vector3>();

        // 원본 외피 subMesh 인덱스별 삼각형 인덱스 목록
        private readonly List<List<int>> _shellTrisBySubMesh = new List<List<int>>();

        // 절단면 캡 전용 삼각형 인덱스 목록. ToMesh 에서 마지막 subMesh 로 배치된다.
        private readonly List<int> _capTris = new List<int>();

        //-------------------- Get 함수 --------------------//

        // 캡 정점을 추가할 때 현재 정점 개수를 확인하기 위한 결과 정점 목록
        public List<Vector3> Verts
        {
            get => _verts;
        }

        // 캡 삼각형이 실제로 생성되었는지 확인하기 위한 캡 인덱스 목록
        public List<int> CapTris
        {
            get => _capTris;
        }

        //-------------------- 기능 함수 --------------------//

        //==============================================================================
        // 기능: MeshBuffer::Clear
        // 설명: 리스트 Capacity 는 유지하고 이전 절단에서 누적한 데이터만 비운다.
        //==============================================================================
        public void Clear()
        {
            // 채널 데이터와 캡 삼각형은 매 절단마다 새로 채워진다.
            _verts.Clear();
            _uvs.Clear();
            _normals.Clear();
            _capTris.Clear();

            // 외피 subMesh 리스트 자체는 재사용하고 내부 인덱스만 비운다.
            int shellSubMeshCount = _shellTrisBySubMesh.Count;
            for (int i = 0; i < shellSubMeshCount; i++)
            {
                _shellTrisBySubMesh[i].Clear();
            }
        }

        //==============================================================================
        // 기능: MeshBuffer::EnsureVertexCapacity
        // 설명: 결과 정점 채널 리스트가 예상 정점 수를 담을 수 있도록 Capacity 를 미리 확보한다.
        //==============================================================================
        // vertexCapacity: 예상 결과 정점 수
        public void EnsureVertexCapacity(int vertexCapacity)
        {
            EnsureCapacity(_verts, vertexCapacity);
            EnsureCapacity(_uvs, vertexCapacity);
            EnsureCapacity(_normals, vertexCapacity);
        }

        //==============================================================================
        // 기능: MeshBuffer::EnsureTriangleCapacity
        // 설명: 지정한 외피 subMesh 인덱스 버퍼의 Capacity 를 미리 확보한다.
        //==============================================================================
        // sourceSubMeshIndex: 원본 Mesh 의 외피 subMesh 인덱스
        // indexCapacity: 추가될 것으로 예상하는 인덱스 개수
        public void EnsureTriangleCapacity(int sourceSubMeshIndex, int indexCapacity)
        {
            List<int> shellTris = GetOrCreateShellTris(sourceSubMeshIndex);
            EnsureCapacity(shellTris, shellTris.Count + indexCapacity);
        }

        //==============================================================================
        // 기능: MeshBuffer::AddVertex
        // 설명: 위치, UV, 노멀 채널을 같은 인덱스로 추가하고 새 정점 인덱스를 반환한다.
        //==============================================================================
        // pos: 결과 Mesh 에 저장할 로컬 정점 위치
        // uv: 결과 Mesh 에 저장할 UV0 값
        // normal: 결과 Mesh 에 저장할 노멀
        // 반환: 새로 추가된 정점 인덱스
        public int AddVertex(Vector3 pos, Vector2 uv, Vector3 normal)
        {
            int index = _verts.Count;
            _verts.Add(pos);
            _uvs.Add(uv);
            _normals.Add(normal);
            return index;
        }

        //==============================================================================
        // 기능: MeshBuffer::AddTriangle
        // 설명: 삼각형 인덱스를 외피 subMesh 또는 캡 subMesh 버퍼에 추가한다.
        //==============================================================================
        // a: 삼각형 첫 번째 정점 인덱스
        // b: 삼각형 두 번째 정점 인덱스
        // c: 삼각형 세 번째 정점 인덱스
        // sourceSubMeshIndex: 외피 삼각형일 때 보존할 원본 subMesh 인덱스
        // isCap: true 이면 외피가 아니라 캡 삼각형으로 추가한다.
        public void AddTriangle(int a, int b, int c, int sourceSubMeshIndex, bool isCap)
        {
            if (isCap)
            {
                AddCapTriangle(a, b, c);
                return;
            }

            // 외피 삼각형은 원본 subMesh 번호를 유지해 기존 머티리얼 슬롯과 맞춘다.
            List<int> shellTris = GetOrCreateShellTris(sourceSubMeshIndex);
            shellTris.Add(a);
            shellTris.Add(b);
            shellTris.Add(c);
        }

        //==============================================================================
        // 기능: MeshBuffer::AddCapTriangle
        // 설명: 절단면 캡 삼각형 인덱스를 캡 전용 버퍼에 추가한다.
        //==============================================================================
        // a: 캡 삼각형 첫 번째 정점 인덱스
        // b: 캡 삼각형 두 번째 정점 인덱스
        // c: 캡 삼각형 세 번째 정점 인덱스
        public void AddCapTriangle(int a, int b, int c)
        {
            _capTris.Add(a);
            _capTris.Add(b);
            _capTris.Add(c);
        }

        //==============================================================================
        // 기능: MeshBuffer::ToMesh
        // 설명: 누적된 채널과 subMesh 별 삼각형 인덱스로 Unity Mesh 를 생성한다.
        //==============================================================================
        // 반환: 현재 버퍼 내용으로 만든 새 Mesh
        public Mesh ToMesh()
        {
            Mesh mesh = new Mesh();
            if (_verts.Count > ushort.MaxValue)
            {
                // Unity 기본 16bit 인덱스 한계를 넘으면 32bit 인덱스를 사용한다.
                mesh.indexFormat = IndexFormat.UInt32;
            }

            mesh.SetVertices(_verts);
            mesh.SetUVs(0, _uvs);
            mesh.SetNormals(_normals);

            int shellSubMeshCount = _shellTrisBySubMesh.Count;
            int totalSubMeshCount = shellSubMeshCount + (_capTris.Count > 0 ? 1 : 0);
            mesh.subMeshCount = Mathf.Max(1, totalSubMeshCount);

            // 외피 subMesh 는 원본 번호 그대로 배치한다.
            for (int subMesh = 0; subMesh < shellSubMeshCount; subMesh++)
            {
                mesh.SetTriangles(_shellTrisBySubMesh[subMesh], subMesh);
            }

            // 캡이 생성된 경우에만 마지막 subMesh 를 캡 머티리얼 슬롯으로 사용한다.
            if (_capTris.Count > 0)
            {
                mesh.SetTriangles(_capTris, shellSubMeshCount);
            }

            mesh.RecalculateBounds();
            return mesh;
        }

        //==============================================================================
        // 기능: MeshBuffer::HasAnyTriangle
        // 설명: 외피 또는 캡 삼각형이 하나라도 누적되었는지 확인한다.
        //==============================================================================
        // 반환: 결과 Mesh 로 만들 삼각형이 있으면 true
        public bool HasAnyTriangle()
        {
            if (_capTris.Count > 0) return true;

            int shellSubMeshCount = _shellTrisBySubMesh.Count;
            for (int i = 0; i < shellSubMeshCount; i++)
            {
                if (_shellTrisBySubMesh[i].Count > 0) return true;
            }

            return false;
        }

        //==============================================================================
        // 기능: MeshBuffer::GetOrCreateShellTris
        // 설명: 원본 subMesh 번호에 대응하는 외피 삼각형 리스트를 반환하거나 새로 만든다.
        //==============================================================================
        // sourceSubMeshIndex: 원본 Mesh 의 subMesh 인덱스
        // 반환: 해당 subMesh 에 누적할 삼각형 인덱스 리스트
        private List<int> GetOrCreateShellTris(int sourceSubMeshIndex)
        {
            if (sourceSubMeshIndex < 0) sourceSubMeshIndex = 0;

            // 중간 subMesh 가 비어 있더라도 Unity subMesh 번호가 유지되도록 리스트를 채운다.
            while (_shellTrisBySubMesh.Count <= sourceSubMeshIndex)
            {
                _shellTrisBySubMesh.Add(new List<int>());
            }

            return _shellTrisBySubMesh[sourceSubMeshIndex];
        }

        //==============================================================================
        // 기능: MeshBuffer::EnsureCapacity
        // 설명: 리스트 Capacity 가 필요한 크기보다 작을 때만 확장한다.
        //==============================================================================
        // list: Capacity 를 확인할 리스트
        // capacity: 필요한 최소 Capacity
        private static void EnsureCapacity<T>(List<T> list, int capacity)
        {
            if (list.Capacity < capacity)
            {
                list.Capacity = capacity;
            }
        }
    }
}
