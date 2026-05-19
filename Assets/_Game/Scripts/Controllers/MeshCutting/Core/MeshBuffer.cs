using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace OrganicEngineer.MeshCutting
{
    //==============================================================================
    // 기능: MeshBuffer
    // 설명: 절단 결과 Mesh 의 정점 채널과 subMesh 별 삼각형 인덱스를 재사용 가능한 리스트에 누적한다.
    //==============================================================================
    internal sealed class MeshBuffer
    {
        // Mesh 정점 위치 목록
        private readonly List<Vector3> _verts = new List<Vector3>();

        // Mesh UV0 목록
        private readonly List<Vector2> _uvs = new List<Vector2>();

        // Mesh 노멀 목록
        private readonly List<Vector3> _normals = new List<Vector3>();

        // 원본 subMesh 인덱스별 외피 삼각형 목록
        private readonly List<List<int>> _shellTrisBySubMesh = new List<List<int>>();

        // 절단면 캡 삼각형 목록
        private readonly List<int> _capTris = new List<int>();

        // Mesh 정점 위치 목록을 읽기 전용 참조로 제공한다.
        public List<Vector3> Verts
        {
            get => _verts;
        }

        // 절단면 캡 삼각형 목록을 읽기 전용 참조로 제공한다.
        public List<int> CapTris
        {
            get => _capTris;
        }

        //==============================================================================
        // 기능: MeshBuffer::Clear
        // 설명: 리스트 Capacity 는 유지하고 이전 절단에서 누적된 데이터만 비운다.
        //==============================================================================
        public void Clear()
        {
            _verts.Clear();
            _uvs.Clear();
            _normals.Clear();
            _capTris.Clear();

            int shellSubMeshCount = _shellTrisBySubMesh.Count;
            for (int i = 0; i < shellSubMeshCount; i++)
            {
                _shellTrisBySubMesh[i].Clear();
            }
        }

        //==============================================================================
        // 기능: MeshBuffer::EnsureVertexCapacity
        // 설명: 예상 정점 수만큼 채널 리스트의 Capacity 를 미리 확보한다.
        //==============================================================================
        public void EnsureVertexCapacity(int vertexCapacity)
        {
            EnsureCapacity(_verts, vertexCapacity);
            EnsureCapacity(_uvs, vertexCapacity);
            EnsureCapacity(_normals, vertexCapacity);
        }

        //==============================================================================
        // 기능: MeshBuffer::EnsureTriangleCapacity
        // 설명: 지정 subMesh 의 외피 삼각형 리스트 Capacity 를 미리 확보한다.
        //==============================================================================
        public void EnsureTriangleCapacity(int sourceSubMeshIndex, int indexCapacity)
        {
            List<int> shellTris = GetOrCreateShellTris(sourceSubMeshIndex);
            EnsureCapacity(shellTris, shellTris.Count + indexCapacity);
        }

        //==============================================================================
        // 기능: MeshBuffer::AddVertex
        // 설명: 정점 채널 값을 같은 인덱스로 추가하고 새 정점 인덱스를 반환한다.
        //==============================================================================
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
        // 설명: 외피 또는 캡 삼각형 인덱스 3개를 알맞은 subMesh 버퍼에 추가한다.
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
        // 설명: 절단면 캡 subMesh 에 삼각형 인덱스 3개를 추가한다.
        //==============================================================================
        public void AddCapTriangle(int a, int b, int c)
        {
            _capTris.Add(a);
            _capTris.Add(b);
            _capTris.Add(c);
        }

        //==============================================================================
        // 기능: MeshBuffer::ToMesh
        // 설명: 누적된 채널과 삼각형 인덱스로 Unity Mesh 를 생성한다.
        //==============================================================================
        public Mesh ToMesh()
        {
            Mesh mesh = new Mesh();
            if (_verts.Count > ushort.MaxValue)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            mesh.SetVertices(_verts);
            mesh.SetUVs(0, _uvs);
            mesh.SetNormals(_normals);

            int shellSubMeshCount = _shellTrisBySubMesh.Count;
            int totalSubMeshCount = shellSubMeshCount + (_capTris.Count > 0 ? 1 : 0);
            mesh.subMeshCount = Mathf.Max(1, totalSubMeshCount);

            for (int subMesh = 0; subMesh < shellSubMeshCount; subMesh++)
            {
                mesh.SetTriangles(_shellTrisBySubMesh[subMesh], subMesh);
            }

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
        // 설명: 원본 subMesh 번호에 대응하는 외피 삼각형 리스트를 반환한다.
        //==============================================================================
        private List<int> GetOrCreateShellTris(int sourceSubMeshIndex)
        {
            if (sourceSubMeshIndex < 0) sourceSubMeshIndex = 0;

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
        private static void EnsureCapacity<T>(List<T> list, int capacity)
        {
            if (list.Capacity < capacity)
            {
                list.Capacity = capacity;
            }
        }
    }
}
