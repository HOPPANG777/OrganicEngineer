using System.Collections.Generic;
using UnityEngine;

namespace OrganicEngineer.MeshCutting
{
    //==============================================================================
    // 기능: BoundedCutPlane
    // 설명: 무한 절단 평면에 드래그 선분 범위를 더해, 특정 선분 주변에서만 절단이 일어나도록 제한한다.
    //==============================================================================
    public readonly struct BoundedCutPlane
    {
        //-------------------- 상수 --------------------//

        // 드래그 선분이 너무 짧아 방향을 만들 수 없는지 판단하는 최소 길이 제곱값
        private const float MinLengthSqr = 1e-10f;

        //-------------------- Get 함수 --------------------//

        // Mesh 로컬 좌표계 기준 무한 절단 평면
        public Plane PlaneLocal { get; }

        // Mesh 로컬 좌표계 기준 드래그 시작점
        public Vector3 SegmentStartLocal { get; }

        // Mesh 로컬 좌표계 기준 드래그 끝점
        public Vector3 SegmentEndLocal { get; }

        // 드래그 선분의 앞뒤 길이 방향 허용 여유값
        public float SpanPadding { get; }

        // 드래그 선분 주변 반경 허용 여유값
        public float BandPadding { get; }

        //-------------------- 멤버 변수 --------------------//

        // 드래그 선분의 로컬 방향 단위 벡터
        private readonly Vector3 _axisLocal;

        // 드래그 선분의 로컬 길이
        private readonly float _length;

        // 드래그 선분이 절단 범위로 사용할 만큼 충분히 긴지 여부
        public bool IsValid => _length * _length > MinLengthSqr;

        //-------------------- 생성 함수 --------------------//

        //==============================================================================
        // 기능: BoundedCutPlane::BoundedCutPlane
        // 설명: 길이 방향과 반경 방향에 같은 padding 을 쓰는 제한 절단 평면을 만든다.
        //==============================================================================
        // planeLocal: Mesh 로컬 좌표계 기준 무한 절단 평면
        // segmentStartLocal: Mesh 로컬 좌표계 기준 드래그 시작점
        // segmentEndLocal: Mesh 로컬 좌표계 기준 드래그 끝점
        // spanPadding: 선분 앞뒤 길이 방향과 주변 반경에 함께 사용할 여유값
        public BoundedCutPlane(Plane planeLocal, Vector3 segmentStartLocal, Vector3 segmentEndLocal, float spanPadding)
            : this(planeLocal, segmentStartLocal, segmentEndLocal, spanPadding, spanPadding)
        {
        }

        //==============================================================================
        // 기능: BoundedCutPlane::BoundedCutPlane
        // 설명: 무한 절단 평면과 드래그 선분 정보를 보관하고, 선분 방향과 길이를 미리 계산한다.
        //==============================================================================
        // planeLocal: Mesh 로컬 좌표계 기준 무한 절단 평면
        // segmentStartLocal: Mesh 로컬 좌표계 기준 드래그 시작점
        // segmentEndLocal: Mesh 로컬 좌표계 기준 드래그 끝점
        // spanPadding: 선분 앞뒤 길이 방향 허용 여유값
        // bandPadding: 선분 주변 반경 허용 여유값
        public BoundedCutPlane
        (
            Plane planeLocal,
            Vector3 segmentStartLocal,
            Vector3 segmentEndLocal,
            float spanPadding,
            float bandPadding
        )
        {
            PlaneLocal = planeLocal;
            SegmentStartLocal = segmentStartLocal;
            SegmentEndLocal = segmentEndLocal;
            SpanPadding = Mathf.Max(0f, spanPadding);
            BandPadding = Mathf.Max(0f, bandPadding);

            Vector3 delta = segmentEndLocal - segmentStartLocal;
            _length = delta.magnitude;
            _axisLocal = _length > Mathf.Sqrt(MinLengthSqr) ? delta / _length : Vector3.right;
        }

        //-------------------- 기능 함수 --------------------//

        //==============================================================================
        // 기능: BoundedCutPlane::TriangleOverlapsSpan
        // 설명: 삼각형이 드래그 선분의 길이 방향 범위와 겹치는지 확인한다.
        //==============================================================================
        // a: 삼각형 첫 번째 정점
        // b: 삼각형 두 번째 정점
        // c: 삼각형 세 번째 정점
        // 반환: 삼각형의 투영 범위가 드래그 선분 앞뒤 허용 범위와 겹치면 true
        public bool TriangleOverlapsSpan(Vector3 a, Vector3 b, Vector3 c)
        {
            float pa = ProjectOnSegmentAxis(a);
            float pb = ProjectOnSegmentAxis(b);
            float pc = ProjectOnSegmentAxis(c);

            float min = Mathf.Min(pa, Mathf.Min(pb, pc));
            float max = Mathf.Max(pa, Mathf.Max(pb, pc));

            return max >= -SpanPadding && min <= _length + SpanPadding;
        }

        //==============================================================================
        // 기능: BoundedCutPlane::CutEdgeOverlapsSpan
        // 설명: 절단선이 드래그 선분의 길이 방향 범위와 겹치는지 확인한다.
        //==============================================================================
        // a: 절단선 시작점
        // b: 절단선 끝점
        // 반환: 절단선의 투영 범위와 중점이 선분 앞뒤 허용 범위 안에 있으면 true
        public bool CutEdgeOverlapsSpan(Vector3 a, Vector3 b)
        {
            float pa = ProjectOnSegmentAxis(a);
            float pb = ProjectOnSegmentAxis(b);

            float min = Mathf.Min(pa, pb);
            float max = Mathf.Max(pa, pb);
            if (max < -SpanPadding || min > _length + SpanPadding) return false;

            float midpoint = (pa + pb) * 0.5f;
            return midpoint >= -SpanPadding && midpoint <= _length + SpanPadding;
        }

        //==============================================================================
        // 기능: BoundedCutPlane::CutEdgeTouchesSegmentBand
        // 설명: 실제 절단선이 드래그 선분 주변 반경 안에 닿는지 확인한다.
        //==============================================================================
        // a: 절단선 시작점
        // b: 절단선 끝점
        // 반환: 절단선과 드래그 선분의 최단 거리가 BandPadding 안이면 true
        public bool CutEdgeTouchesSegmentBand(Vector3 a, Vector3 b)
        {
            if (!CutEdgeOverlapsSpan(a, b)) return false;

            Vector3 paddedStart = SegmentStartLocal - _axisLocal * SpanPadding;
            Vector3 paddedEnd = SegmentEndLocal + _axisLocal * SpanPadding;
            float bandSqr = BandPadding * BandPadding;
            return SegmentToSegmentSqrDistance(a, b, paddedStart, paddedEnd) <= bandSqr;
        }

        //==============================================================================
        // 기능: BoundedCutPlane::TriangleTouchesSegmentBand
        // 설명: 삼각형이 드래그 선분 주변 반경 안에 닿는지 확인한다.
        //==============================================================================
        // a: 삼각형 첫 번째 정점
        // b: 삼각형 두 번째 정점
        // c: 삼각형 세 번째 정점
        // 반환: 정점, 모서리, 중심점 중 하나라도 선분 주변 허용 반경 안에 있으면 true
        public bool TriangleTouchesSegmentBand(Vector3 a, Vector3 b, Vector3 c)
        {
            if (PointTouchesSegmentBand(a)) return true;
            if (PointTouchesSegmentBand(b)) return true;
            if (PointTouchesSegmentBand(c)) return true;
            if (CutEdgeTouchesSegmentBand(a, b)) return true;
            if (CutEdgeTouchesSegmentBand(b, c)) return true;
            if (CutEdgeTouchesSegmentBand(c, a)) return true;

            Vector3 center = (a + b + c) / 3f;
            return PointTouchesSegmentBand(center);
        }

        //==============================================================================
        // 기능: BoundedCutPlane::PointTouchesSegmentBand
        // 설명: 한 점이 드래그 선분 주변의 캡슐형 허용 영역 안에 있는지 확인한다.
        //==============================================================================
        // point: 검사할 로컬 좌표 점
        // 반환: 점의 투영 위치가 선분 범위 안이고 선분까지의 거리가 BandPadding 이하면 true
        public bool PointTouchesSegmentBand(Vector3 point)
        {
            float projection = ProjectOnSegmentAxis(point);
            if (projection < -SpanPadding || projection > _length + SpanPadding) return false;

            float clampedProjection = Mathf.Clamp(projection, 0f, _length);
            Vector3 closest = SegmentStartLocal + _axisLocal * clampedProjection;
            Vector3 offset = point - closest;
            return offset.sqrMagnitude <= BandPadding * BandPadding;
        }

        //==============================================================================
        // 기능: BoundedCutPlane::ProjectOnSegmentAxis
        // 설명: 점을 드래그 선분 방향 축에 투영해, 시작점 기준 길이 위치를 구한다.
        //==============================================================================
        // point: 투영할 로컬 좌표 점
        // 반환: SegmentStartLocal 부터 _axisLocal 방향으로 떨어진 부호 있는 거리
        private float ProjectOnSegmentAxis(Vector3 point)
        {
            return Vector3.Dot(point - SegmentStartLocal, _axisLocal);
        }

        //==============================================================================
        // 기능: BoundedCutPlane::SegmentToSegmentSqrDistance
        // 설명: 두 선분 사이의 최단 거리 제곱을 계산한다.
        //==============================================================================
        // a0: 첫 번째 선분 시작점
        // a1: 첫 번째 선분 끝점
        // b0: 두 번째 선분 시작점
        // b1: 두 번째 선분 끝점
        // 반환: 두 선분 사이 최단 거리의 제곱값
        private static float SegmentToSegmentSqrDistance(Vector3 a0, Vector3 a1, Vector3 b0, Vector3 b1)
        {
            const float epsilon = 1e-8f;

            Vector3 segmentA = a1 - a0;
            Vector3 segmentB = b1 - b0;
            Vector3 offset = a0 - b0;
            float aLengthSqr = Vector3.Dot(segmentA, segmentA);
            float bLengthSqr = Vector3.Dot(segmentB, segmentB);
            float bOffsetDot = Vector3.Dot(segmentB, offset);
            float segmentAParameter;
            float segmentBParameter;

            if (aLengthSqr <= epsilon && bLengthSqr <= epsilon)
                return (a0 - b0).sqrMagnitude;

            if (aLengthSqr <= epsilon)
            {
                segmentAParameter = 0f;
                segmentBParameter = Mathf.Clamp01(bOffsetDot / bLengthSqr);
            }
            else
            {
                float aOffsetDot = Vector3.Dot(segmentA, offset);
                if (bLengthSqr <= epsilon)
                {
                    segmentBParameter = 0f;
                    segmentAParameter = Mathf.Clamp01(-aOffsetDot / aLengthSqr);
                }
                else
                {
                    float abDot = Vector3.Dot(segmentA, segmentB);
                    float denominator = aLengthSqr * bLengthSqr - abDot * abDot;
                    segmentAParameter = denominator > epsilon
                        ? Mathf.Clamp01((abDot * bOffsetDot - aOffsetDot * bLengthSqr) / denominator)
                        : 0f;

                    segmentBParameter = (abDot * segmentAParameter + bOffsetDot) / bLengthSqr;
                    if (segmentBParameter < 0f)
                    {
                        segmentBParameter = 0f;
                        segmentAParameter = Mathf.Clamp01(-aOffsetDot / aLengthSqr);
                    }
                    else if (segmentBParameter > 1f)
                    {
                        segmentBParameter = 1f;
                        segmentAParameter = Mathf.Clamp01((abDot - aOffsetDot) / aLengthSqr);
                    }
                }
            }

            Vector3 closestA = a0 + segmentA * segmentAParameter;
            Vector3 closestB = b0 + segmentB * segmentBParameter;
            return (closestA - closestB).sqrMagnitude;
        }
    }

    //==============================================================================
    // 기능: MeshCutIslandUtility
    // 설명: 절단 결과 Mesh 안의 연결된 삼각형 섬을 찾고, 드래그 선분과 닿은 섬만 선택하거나 섬별 Mesh 로 분리한다.
    //==============================================================================
    static class MeshCutIslandUtility
    {
        //-------------------- 상수 --------------------//

        // 부동소수점 오차를 줄이기 위해 정점 위치를 정수 키로 변환할 때 사용하는 배율
        private const float WeldScale = 1e4f;

        //-------------------- 기능 함수 --------------------//

        //==============================================================================
        // 기능: MeshCutIslandUtility::SplitIntoIslands
        // 설명: 하나의 Mesh 안에 연결되지 않은 삼각형 섬이 여러 개 있으면 섬별 Mesh 로 분리한다.
        //==============================================================================
        // mesh: 섬 단위로 분리할 원본 Mesh
        // output: 분리된 Mesh 를 담을 재사용 목록
        // 반환: output 에 담긴 섬 Mesh 개수
        public static int SplitIntoIslands(Mesh mesh, List<Mesh> output)
        {
            if (output == null) return 0;

            output.Clear();
            if (mesh == null) return 0;

            List<MeshIsland> islands = new List<MeshIsland>();
            CollectIslands(mesh, default, false, islands);

            int islandCount = islands.Count;
            if (islandCount == 0) return 0;
            if (islandCount == 1)
            {
                output.Add(mesh);
                return 1;
            }

            List<MeshIsland> singleIsland = new List<MeshIsland>(1);
            for (int i = 0; i < islandCount; i++)
            {
                singleIsland.Clear();
                singleIsland.Add(islands[i]);
                output.Add(BuildMesh(singleIsland));
            }

            return output.Count;
        }

        //==============================================================================
        // 기능: MeshCutIslandUtility::KeepSmallestIslandTouchingSegment
        // 설명: 제한 절단 결과에서 드래그 선분에 닿은 가장 작은 섬을 절단 조각으로, 나머지를 잔여 조각으로 정리한다.
        //==============================================================================
        // result: MeshCutter 가 만든 Above/Below 절단 결과
        // boundedPlaneLocal: 드래그 선분 범위와 반경 정보를 가진 제한 절단 평면
        public static void KeepSmallestIslandTouchingSegment(CutResult result, BoundedCutPlane boundedPlaneLocal)
        {
            if (result == null || result.Relation != CutPlaneRelation.STRADDLING) return;

            List<MeshIsland> islands = new List<MeshIsland>();
            CollectIslands(result.AboveMesh, boundedPlaneLocal, true, islands);
            CollectIslands(result.BelowMesh, boundedPlaneLocal, true, islands);

            MeshIsland cutIsland = null;
            for (int i = 0; i < islands.Count; i++)
            {
                MeshIsland island = islands[i];
                if (!island.TouchesSegment) continue;
                if (cutIsland == null || island.Area < cutIsland.Area)
                    cutIsland = island;
            }

            if (cutIsland == null) return;

            List<MeshIsland> cutIslands = new List<MeshIsland> { cutIsland };
            List<MeshIsland> restIslands = new List<MeshIsland>();
            for (int i = 0; i < islands.Count; i++)
            {
                if (!ReferenceEquals(islands[i], cutIsland))
                    restIslands.Add(islands[i]);
            }

            result.AboveMesh = BuildMesh(cutIslands);
            result.BelowMesh = BuildMesh(restIslands);
            result.Relation = result.AboveMesh != null && result.BelowMesh != null
                ? CutPlaneRelation.STRADDLING
                : MeshCutUtility.ResolveRelation(result.AboveMesh != null, result.BelowMesh != null);
        }

        //==============================================================================
        // 기능: MeshCutIslandUtility::CollectIslands
        // 설명: Mesh 의 모든 subMesh 삼각형을 읽어, 정점 공유 관계 기준으로 연결 섬을 수집한다.
        //==============================================================================
        // mesh: 연결 섬을 수집할 Mesh
        // boundedPlaneLocal: 드래그 선분 접촉 여부를 판정할 제한 절단 평면
        // markSegmentTouch: true 면 캡 삼각형이 드래그 선분 주변에 닿았는지 기록한다
        // output: 수집된 MeshIsland 를 담을 목록
        private static void CollectIslands(Mesh mesh, BoundedCutPlane boundedPlaneLocal, bool markSegmentTouch, List<MeshIsland> output)
        {
            if (mesh == null) return;

            Vector3[] verts = mesh.vertices;
            Vector2[] uvs = mesh.uv;
            Vector3[] normals = mesh.normals;
            List<TriangleRef> triangles = new List<TriangleRef>();
            int subMeshCount = mesh.subMeshCount;

            for (int subMesh = 0; subMesh < subMeshCount; subMesh++)
            {
                int[] indices = mesh.GetTriangles(subMesh);
                for (int i = 0; i < indices.Length; i += 3)
                    triangles.Add(new TriangleRef(subMesh, indices[i], indices[i + 1], indices[i + 2]));
            }

            if (triangles.Count == 0) return;

            DisjointSet set = new DisjointSet(triangles.Count);
            Dictionary<Vector3Int, int> firstTriangleByPoint = new Dictionary<Vector3Int, int>();
            for (int triIndex = 0; triIndex < triangles.Count; triIndex++)
            {
                TriangleRef tri = triangles[triIndex];
                UnionPoint(firstTriangleByPoint, set, verts[tri.A], triIndex);
                UnionPoint(firstTriangleByPoint, set, verts[tri.B], triIndex);
                UnionPoint(firstTriangleByPoint, set, verts[tri.C], triIndex);
            }

            Dictionary<int, MeshIsland> islandByRoot = new Dictionary<int, MeshIsland>();
            int capSubMeshIndex = subMeshCount - 1;
            for (int triIndex = 0; triIndex < triangles.Count; triIndex++)
            {
                int root = set.Find(triIndex);
                if (!islandByRoot.TryGetValue(root, out MeshIsland island))
                {
                    island = new MeshIsland(mesh, subMeshCount);
                    islandByRoot[root] = island;
                    output.Add(island);
                }

                TriangleRef tri = triangles[triIndex];
                island.Triangles.Add(tri);

                Vector3 a = verts[tri.A];
                Vector3 b = verts[tri.B];
                Vector3 c = verts[tri.C];
                island.Area += Vector3.Cross(b - a, c - a).magnitude * 0.5f;

                if (markSegmentTouch && tri.SubMesh == capSubMeshIndex && boundedPlaneLocal.TriangleTouchesSegmentBand(a, b, c))
                    island.TouchesSegment = true;
            }
        }

        //==============================================================================
        // 기능: MeshCutIslandUtility::BuildMesh
        // 설명: 하나 이상의 MeshIsland 를 새 Unity Mesh 로 재구성한다.
        //==============================================================================
        // islands: 새 Mesh 로 묶을 연결 섬 목록
        // 반환: 섬 목록으로 만든 Mesh. 섬이 없으면 null
        private static Mesh BuildMesh(List<MeshIsland> islands)
        {
            if (islands == null || islands.Count == 0) return null;

            int subMeshCount = 1;
            for (int i = 0; i < islands.Count; i++)
                subMeshCount = Mathf.Max(subMeshCount, islands[i].SubMeshCount);

            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<Vector3> normals = new List<Vector3>();
            List<List<int>> trisBySubMesh = new List<List<int>>();
            for (int i = 0; i < subMeshCount; i++)
                trisBySubMesh.Add(new List<int>());

            for (int islandIndex = 0; islandIndex < islands.Count; islandIndex++)
            {
                MeshIsland island = islands[islandIndex];
                Vector3[] srcVerts = island.Source.vertices;
                Vector2[] srcUVs = island.Source.uv;
                Vector3[] srcNormals = island.Source.normals;

                for (int triIndex = 0; triIndex < island.Triangles.Count; triIndex++)
                {
                    TriangleRef tri = island.Triangles[triIndex];
                    int a = AddVertex(srcVerts, srcUVs, srcNormals, tri.A, verts, uvs, normals);
                    int b = AddVertex(srcVerts, srcUVs, srcNormals, tri.B, verts, uvs, normals);
                    int c = AddVertex(srcVerts, srcUVs, srcNormals, tri.C, verts, uvs, normals);
                    trisBySubMesh[tri.SubMesh].Add(a);
                    trisBySubMesh[tri.SubMesh].Add(b);
                    trisBySubMesh[tri.SubMesh].Add(c);
                }
            }

            Mesh mesh = new Mesh();
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetNormals(normals);
            mesh.subMeshCount = subMeshCount;
            for (int subMesh = 0; subMesh < subMeshCount; subMesh++)
                mesh.SetTriangles(trisBySubMesh[subMesh], subMesh);
            mesh.RecalculateBounds();
            return mesh;
        }

        //==============================================================================
        // 기능: MeshCutIslandUtility::AddVertex
        // 설명: 원본 Mesh 의 정점 채널 값을 새 Mesh 채널 목록에 복사하고 새 인덱스를 반환한다.
        //==============================================================================
        // srcVerts: 원본 정점 위치 배열
        // srcUVs: 원본 UV 배열
        // srcNormals: 원본 노멀 배열
        // sourceIndex: 복사할 원본 정점 인덱스
        // verts: 새 Mesh 정점 위치 목록
        // uvs: 새 Mesh UV 목록
        // normals: 새 Mesh 노멀 목록
        // 반환: 새 Mesh 에 추가된 정점 인덱스
        private static int AddVertex
        (
            Vector3[] srcVerts,
            Vector2[] srcUVs,
            Vector3[] srcNormals,
            int sourceIndex,
            List<Vector3> verts,
            List<Vector2> uvs,
            List<Vector3> normals
        )
        {
            int index = verts.Count;
            verts.Add(srcVerts[sourceIndex]);
            uvs.Add(sourceIndex < srcUVs.Length ? srcUVs[sourceIndex] : Vector2.zero);
            normals.Add(sourceIndex < srcNormals.Length ? srcNormals[sourceIndex] : Vector3.up);
            return index;
        }

        //==============================================================================
        // 기능: MeshCutIslandUtility::UnionPoint
        // 설명: 같은 위치로 용접되는 정점을 공유하는 삼각형들을 같은 연결 섬으로 합친다.
        //==============================================================================
        // firstTriangleByPoint: 정점 위치 키별로 처음 발견한 삼각형 인덱스 맵
        // set: 삼각형 연결 관계를 관리하는 DisjointSet
        // point: 현재 삼각형의 정점 위치
        // triIndex: 현재 삼각형 인덱스
        private static void UnionPoint(Dictionary<Vector3Int, int> firstTriangleByPoint, DisjointSet set, Vector3 point, int triIndex)
        {
            Vector3Int key = Quantize(point);
            if (firstTriangleByPoint.TryGetValue(key, out int otherTriIndex))
            {
                set.Union(triIndex, otherTriIndex);
                return;
            }

            firstTriangleByPoint[key] = triIndex;
        }

        //==============================================================================
        // 기능: MeshCutIslandUtility::Quantize
        // 설명: 부동소수점 정점 좌표를 용접 비교용 정수 키로 변환한다.
        //==============================================================================
        // value: 정수 키로 바꿀 정점 좌표
        // 반환: WeldScale 이 적용된 정수 좌표 키
        private static Vector3Int Quantize(Vector3 value)
        {
            return new Vector3Int(
                Mathf.RoundToInt(value.x * WeldScale),
                Mathf.RoundToInt(value.y * WeldScale),
                Mathf.RoundToInt(value.z * WeldScale)
            );
        }

        //==============================================================================
        // 기능: MeshIsland
        // 설명: 하나의 Mesh 안에서 서로 연결된 삼각형 묶음과 그 부가 정보를 보관한다.
        //==============================================================================
        private sealed class MeshIsland
        {
            // 이 섬이 만들어진 원본 Mesh
            public Mesh Source { get; }

            // 원본 Mesh 의 subMesh 개수
            public int SubMeshCount { get; }

            // 이 섬에 속한 삼각형 참조 목록
            public List<TriangleRef> Triangles { get; } = new List<TriangleRef>();

            // 이 섬의 캡 삼각형이 드래그 선분 주변에 닿았는지 여부
            public bool TouchesSegment { get; set; }

            // 이 섬의 대략적인 표면 면적
            public float Area { get; set; }

            //==============================================================================
            // 기능: MeshIsland::MeshIsland
            // 설명: 원본 Mesh 와 subMesh 개수를 저장하는 연결 섬 컨테이너를 만든다.
            //==============================================================================
            // source: 섬이 속한 원본 Mesh
            // subMeshCount: 원본 Mesh 의 subMesh 개수
            public MeshIsland(Mesh source, int subMeshCount)
            {
                Source = source;
                SubMeshCount = subMeshCount;
            }
        }

        //==============================================================================
        // 기능: TriangleRef
        // 설명: 원본 Mesh 의 subMesh 번호와 삼각형 정점 인덱스 3개를 함께 보관한다.
        //==============================================================================
        private readonly struct TriangleRef
        {
            // 삼각형이 속한 원본 subMesh 번호
            public int SubMesh { get; }

            // 삼각형 첫 번째 정점 인덱스
            public int A { get; }

            // 삼각형 두 번째 정점 인덱스
            public int B { get; }

            // 삼각형 세 번째 정점 인덱스
            public int C { get; }

            //==============================================================================
            // 기능: TriangleRef::TriangleRef
            // 설명: subMesh 번호와 삼각형 정점 인덱스 3개를 저장한다.
            //==============================================================================
            // subMesh: 삼각형이 속한 subMesh 번호
            // a: 첫 번째 정점 인덱스
            // b: 두 번째 정점 인덱스
            // c: 세 번째 정점 인덱스
            public TriangleRef(int subMesh, int a, int b, int c)
            {
                SubMesh = subMesh;
                A = a;
                B = b;
                C = c;
            }
        }

        //==============================================================================
        // 기능: DisjointSet
        // 설명: 삼각형들이 같은 연결 섬에 속하는지 빠르게 합치고 찾기 위한 유니온 파인드 자료구조다.
        //==============================================================================
        private sealed class DisjointSet
        {
            // 각 삼각형 인덱스의 부모 인덱스 배열
            private readonly int[] _parent;

            //==============================================================================
            // 기능: DisjointSet::DisjointSet
            // 설명: 모든 원소가 자기 자신을 부모로 가지는 초기 집합을 만든다.
            //==============================================================================
            // count: 관리할 원소 개수
            public DisjointSet(int count)
            {
                _parent = new int[count];
                for (int i = 0; i < count; i++)
                    _parent[i] = i;
            }

            //==============================================================================
            // 기능: DisjointSet::Find
            // 설명: 원소가 속한 집합의 대표 인덱스를 찾고, 경로 압축으로 다음 조회를 빠르게 만든다.
            //==============================================================================
            // value: 대표 인덱스를 찾을 원소
            // 반환: value 가 속한 집합의 대표 인덱스
            public int Find(int value)
            {
                while (_parent[value] != value)
                {
                    _parent[value] = _parent[_parent[value]];
                    value = _parent[value];
                }

                return value;
            }

            //==============================================================================
            // 기능: DisjointSet::Union
            // 설명: 두 원소가 속한 집합을 하나로 합친다.
            //==============================================================================
            // a: 합칠 첫 번째 원소
            // b: 합칠 두 번째 원소
            public void Union(int a, int b)
            {
                int rootA = Find(a);
                int rootB = Find(b);
                if (rootA == rootB) return;
                _parent[rootB] = rootA;
            }
        }
    }
}
