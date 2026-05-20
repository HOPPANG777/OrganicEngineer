using System;
using System.Collections.Generic;
using UnityEngine;

namespace OrganicEngineer.MeshCutting
{
    //==============================================================================
    // 기능: MeshCutCap
    // 설명: 절단 에지 목록으로 단면 경계 루프를 구성하고 캡 삼각형을 결과 MeshBuffer 에 추가한다.
    //==============================================================================
    static class MeshCutCap
    {
        //-------------------- 상수 --------------------//

        // 캡 법선으로 사용할 수 있는 최소 제곱 길이 판정 기준
        private const float NormalEpsilon = 1e-5f;

        // 캡 UV 정규화 중 폭 또는 높이가 거의 0인지 판정하는 기준
        private const float UvEpsilon = 1e-5f;

        // 절단점 웰드용 양자화 스케일. 1e-4 단위 위치 차이를 같은 점으로 묶는다.
        private const float WeldScale = 1e4f;

        // 루프 시작점과 끝점 또는 연속 점이 같은지 확인하는 제곱 거리 기준
        private const float LoopPointEpsilonSqr = 1e-10f;

        // ear clipping 에서 너무 작은 귀 또는 퇴화 삼각형을 거르는 기준
        private const float EarEpsilon = 1e-6f;

        //-------------------- 기능 함수 --------------------//

        //==============================================================================
        // 기능: MeshCutCap::AppendIfNeeded
        // 설명: 실제 교차 절단이 있으면 양쪽 결과 Mesh 에 캡을 추가하고, 아니면 포함 관계만 확정한다.
        //==============================================================================
        // straddling: 실제 교차 삼각형이 하나 이상 있었는지 여부
        // planeNormal: 절단 평면의 로컬 법선
        // above: 평면 + 방향 결과 버퍼
        // below: 평면 반대 방향 결과 버퍼
        // result: 캡 에지와 관계 값을 담는 절단 결과
        // buffer: 캡 생성 중 재사용할 임시 버퍼
        public static void AppendIfNeeded
        (
            bool straddling,
            Vector3 planeNormal,
            MeshBuffer above,
            MeshBuffer below,
            CutResult result,
            MeshCutCapBuffer buffer
        )
        {
            if (straddling)
            {
                // Above 조각의 바깥 방향은 평면 법선의 반대, Below 조각은 평면 법선 방향이다.
                Append(above, result.AboveCapEdges, -planeNormal, buffer);
                Append(below, result.BelowCapEdges, planeNormal, buffer);
                result.Relation = CutPlaneRelation.STRADDLING;
                return;
            }

            // 교차가 없으면 캡 후보 에지는 의미가 없으므로 비우고 버퍼 상태로 관계를 판정한다.
            result.AboveCapEdges.Clear();
            result.BelowCapEdges.Clear();
            result.Relation = MeshCutUtility.ResolveRelation(above.HasAnyTriangle(), below.HasAnyTriangle());
        }

        //==============================================================================
        // 기능: MeshCutCap::Append
        // 설명: 절단 에지를 루프로 묶은 뒤 각 루프를 캡 삼각형으로 변환한다.
        //==============================================================================
        private static void Append(MeshBuffer meshBuffer, List<CutEdge> edges, Vector3 outwardNormal, MeshCutCapBuffer buffer)
        {
            if (edges == null || edges.Count == 0) return;
            if (outwardNormal.sqrMagnitude < NormalEpsilon * NormalEpsilon) return;

            Vector3 normal = outwardNormal.normalized;
            BuildProjectionAxes(normal, out Vector3 tangent, out Vector3 bitangent);
            BuildLoops(edges, buffer);

            int loopCount = buffer.Loops.Count;
            for (int i = 0; i < loopCount; i++)
            {
                if (!TryPrepareLoop(buffer.Loops[i], normal, tangent, bitangent, buffer))
                    continue;

                AppendPreparedLoop(meshBuffer, normal, buffer);
            }
        }

        //==============================================================================
        // 기능: MeshCutCap::BuildLoops
        // 설명: 절단 에지를 웰드된 점 그래프로 바꾼 뒤 캡 후보 루프 목록을 만든다.
        //==============================================================================
        private static void BuildLoops(List<CutEdge> edges, MeshCutCapBuffer buffer)
        {
            buffer.ClearGraph();

            int edgeCount = edges.Count;
            for (int i = 0; i < edgeCount; i++)
            {
                // 거의 같은 위치의 교차점은 같은 루프 정점으로 묶어 열린 틈을 줄인다.
                int a = AddWeldedPoint(buffer, edges[i].A);
                int b = AddWeldedPoint(buffer, edges[i].B);
                if (a == b) continue;

                MeshCutEdgeKey key = new MeshCutEdgeKey(a, b);
                if (!buffer.AllEdges.Add(key)) continue;

                buffer.Edges.Add(key);
                buffer.Adjacency[a].Add(b);
                buffer.Adjacency[b].Add(a);
            }

            TraceClosedLoops(buffer);
        }

        //==============================================================================
        // 기능: MeshCutCap::AddWeldedPoint
        // 설명: 위치를 양자화한 키로 중복 절단점을 병합하고 그래프 정점 인덱스를 반환한다.
        //==============================================================================
        private static int AddWeldedPoint(MeshCutCapBuffer buffer, Vector3 point)
        {
            Vector3Int key = Quantize(point);
            if (buffer.WeldMap.TryGetValue(key, out int index))
                return index;

            index = buffer.Points.Count;
            buffer.WeldMap[key] = index;
            buffer.Points.Add(point);
            buffer.Adjacency.Add(new List<int>());
            return index;
        }

        //==============================================================================
        // 기능: MeshCutCap::TraceClosedLoops
        // 설명: 사용하지 않은 에지에서 시작해 인접 에지를 따라가며 캡 후보 루프를 추적한다.
        //==============================================================================
        private static void TraceClosedLoops(MeshCutCapBuffer buffer)
        {
            buffer.UsedEdges.Clear();
            buffer.Loops.Clear();

            int edgeCount = buffer.Edges.Count;
            for (int i = 0; i < edgeCount; i++)
            {
                MeshCutEdgeKey edge = buffer.Edges[i];
                if (buffer.UsedEdges.Contains(edge)) continue;

                buffer.TraceLoop.Clear();
                buffer.TraceLoop.Add(edge.A);
                buffer.UsedEdges.Add(edge);

                int start = edge.A;
                int previous = edge.A;
                int current = edge.B;
                int guard = edgeCount + buffer.Points.Count + 4;

                while (guard-- > 0)
                {
                    if (current == start)
                        break;

                    buffer.TraceLoop.Add(current);
                    int next = FindNextUnusedNeighbor(buffer, current, previous);
                    if (next < 0)
                        break;

                    buffer.UsedEdges.Add(new MeshCutEdgeKey(current, next));
                    previous = current;
                    current = next;
                }

                // 제한 절단은 경계가 열린 체인으로 남을 수 있으므로 3점 이상이면 캡 후보로 둔다.
                if (buffer.TraceLoop.Count >= 3)
                    buffer.Loops.Add(new List<int>(buffer.TraceLoop));
            }
        }

        //==============================================================================
        // 기능: MeshCutCap::FindNextUnusedNeighbor
        // 설명: 현재 그래프 정점에서 아직 사용하지 않은 다음 에지의 반대 정점을 찾는다.
        //==============================================================================
        private static int FindNextUnusedNeighbor(MeshCutCapBuffer buffer, int current, int previous)
        {
            List<int> neighbors = buffer.Adjacency[current];
            int fallback = -1;
            int count = neighbors.Count;
            for (int i = 0; i < count; i++)
            {
                int neighbor = neighbors[i];
                MeshCutEdgeKey key = new MeshCutEdgeKey(current, neighbor);
                if (buffer.UsedEdges.Contains(key)) continue;

                // 가능하면 바로 직전 정점으로 되돌아가지 않는 방향을 우선한다.
                if (neighbor != previous) return neighbor;
                fallback = neighbor;
            }

            return fallback;
        }

        //==============================================================================
        // 기능: MeshCutCap::TryPrepareLoop
        // 설명: 그래프 루프를 3D/2D 폴리곤으로 정리하고 삼각분할 가능한 방향으로 맞춘다.
        //==============================================================================
        private static bool TryPrepareLoop
        (
            List<int> loop,
            Vector3 normal,
            Vector3 tangent,
            Vector3 bitangent,
            MeshCutCapBuffer buffer
        )
        {
            buffer.Loop3D.Clear();

            int loopCount = loop.Count;
            for (int i = 0; i < loopCount; i++)
                AddCleanLoopPoint(buffer.Loop3D, buffer.Points[loop[i]]);

            RemoveCollinearPoints(buffer.Loop3D, normal);
            if (buffer.Loop3D.Count < 3) return false;

            ProjectTo2D(buffer.Loop3D, buffer.Loop3D[0], tangent, bitangent, buffer.Projected);
            float signedArea = SignedArea(buffer.Projected);
            if (Mathf.Abs(signedArea) < EarEpsilon) return false;

            // ear clipping 은 반시계 방향 폴리곤을 기준으로 귀를 찾으므로 음수 면적이면 뒤집는다.
            if (signedArea < 0f)
            {
                buffer.Loop3D.Reverse();
                buffer.Projected.Reverse();
            }

            return true;
        }

        //==============================================================================
        // 기능: MeshCutCap::AddCleanLoopPoint
        // 설명: 연속 중복점과 닫힘 중복점을 제거하며 루프 정점을 추가한다.
        //==============================================================================
        private static void AddCleanLoopPoint(List<Vector3> loop3d, Vector3 point)
        {
            int count = loop3d.Count;
            if (count > 0 && (loop3d[count - 1] - point).sqrMagnitude <= LoopPointEpsilonSqr)
                return;

            if (count > 1 && (loop3d[0] - point).sqrMagnitude <= LoopPointEpsilonSqr)
                return;

            loop3d.Add(point);
        }

        //==============================================================================
        // 기능: MeshCutCap::RemoveCollinearPoints
        // 설명: 같은 직선 위에 놓인 루프 정점을 제거해 퇴화 삼각형 생성을 줄인다.
        //==============================================================================
        private static void RemoveCollinearPoints(List<Vector3> loop3d, Vector3 normal)
        {
            bool removed;
            do
            {
                removed = false;
                int count = loop3d.Count;
                if (count < 3) return;

                for (int i = 0; i < count; i++)
                {
                    Vector3 previous = loop3d[(i + count - 1) % count];
                    Vector3 current = loop3d[i];
                    Vector3 next = loop3d[(i + 1) % count];
                    Vector3 cross = Vector3.Cross(current - previous, next - current);
                    if (cross.sqrMagnitude > EarEpsilon * EarEpsilon
                        && Mathf.Abs(Vector3.Dot(cross.normalized, normal)) > EarEpsilon)
                        continue;

                    loop3d.RemoveAt(i);
                    removed = true;
                    break;
                }
            }
            while (removed);
        }

        //==============================================================================
        // 기능: MeshCutCap::AppendPreparedLoop
        // 설명: 정리된 루프 정점을 결과 MeshBuffer 에 추가하고 캡 삼각형 인덱스를 생성한다.
        //==============================================================================
        private static void AppendPreparedLoop(MeshBuffer meshBuffer, Vector3 normal, MeshCutCapBuffer buffer)
        {
            int loopCount = buffer.Loop3D.Count;
            if (loopCount < 3) return;

            int baseIndex = meshBuffer.Verts.Count;
            BuildCapUVs(buffer.Projected, buffer.CapUVs);

            for (int i = 0; i < loopCount; i++)
                meshBuffer.AddVertex(buffer.Loop3D[i], buffer.CapUVs[i], normal);

            if (TryTriangulate(buffer.Projected, buffer.Triangles, buffer.Polygon))
            {
                int triangleCount = buffer.Triangles.Count;
                for (int i = 0; i < triangleCount; i += 3)
                    meshBuffer.AddCapTriangle(baseIndex + buffer.Triangles[i], baseIndex + buffer.Triangles[i + 1], baseIndex + buffer.Triangles[i + 2]);
                return;
            }

            // ear clipping 이 실패해도 단순 볼록 또는 거의 볼록한 루프는 fan 으로라도 닫는다.
            for (int i = 1; i < loopCount - 1; i++)
                meshBuffer.AddCapTriangle(baseIndex, baseIndex + i, baseIndex + i + 1);
        }

        //==============================================================================
        // 기능: MeshCutCap::TryTriangulate
        // 설명: 2D 루프를 ear clipping 방식으로 삼각분할한다.
        //==============================================================================
        private static bool TryTriangulate(List<Vector2> points, List<int> triangles, List<int> polygon)
        {
            triangles.Clear();
            polygon.Clear();

            int pointCount = points.Count;
            if (pointCount < 3) return false;

            for (int i = 0; i < pointCount; i++)
                polygon.Add(i);

            int guard = pointCount * pointCount;
            while (polygon.Count > 3 && guard-- > 0)
            {
                bool foundEar = false;
                int polygonCount = polygon.Count;

                for (int i = 0; i < polygonCount; i++)
                {
                    int previous = polygon[(i + polygonCount - 1) % polygonCount];
                    int current = polygon[i];
                    int next = polygon[(i + 1) % polygonCount];

                    // 반시계 방향 폴리곤에서 볼록 꼭짓점만 귀 후보가 된다.
                    if (Cross2(points[previous], points[current], points[next]) <= EarEpsilon)
                        continue;

                    // 귀 삼각형 안에 다른 폴리곤 점이 있으면 잘라낼 수 없다.
                    if (ContainsPointInTriangle(points, polygon, previous, current, next))
                        continue;

                    triangles.Add(previous);
                    triangles.Add(current);
                    triangles.Add(next);
                    polygon.RemoveAt(i);
                    foundEar = true;
                    break;
                }

                if (!foundEar)
                    return false;
            }

            if (polygon.Count != 3) return false;

            triangles.Add(polygon[0]);
            triangles.Add(polygon[1]);
            triangles.Add(polygon[2]);
            return true;
        }

        //==============================================================================
        // 기능: MeshCutCap::ContainsPointInTriangle
        // 설명: 귀 후보 삼각형 안에 다른 폴리곤 정점이 포함되는지 검사한다.
        //==============================================================================
        private static bool ContainsPointInTriangle(List<Vector2> points, List<int> polygon, int a, int b, int c)
        {
            Vector2 pa = points[a];
            Vector2 pb = points[b];
            Vector2 pc = points[c];
            int count = polygon.Count;

            for (int i = 0; i < count; i++)
            {
                int index = polygon[i];
                if (index == a || index == b || index == c) continue;

                if (PointInTriangle(points[index], pa, pb, pc))
                    return true;
            }

            return false;
        }

        //==============================================================================
        // 기능: MeshCutCap::PointInTriangle
        // 설명: 2D 점이 반시계 방향 삼각형 안 또는 경계 위에 있는지 외적 부호로 판정한다.
        //==============================================================================
        private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float ab = Cross2(a, b, p);
            float bc = Cross2(b, c, p);
            float ca = Cross2(c, a, p);
            return ab >= -EarEpsilon && bc >= -EarEpsilon && ca >= -EarEpsilon;
        }

        //==============================================================================
        // 기능: MeshCutCap::BuildProjectionAxes
        // 설명: 캡 법선과 수직인 2D 투영용 tangent/bitangent 축을 만든다.
        //==============================================================================
        private static void BuildProjectionAxes(Vector3 normal, out Vector3 tangent, out Vector3 bitangent)
        {
            tangent = Vector3.Cross(normal, Vector3.up);
            if (tangent.sqrMagnitude < 1e-6f)
                tangent = Vector3.Cross(normal, Vector3.right);

            tangent.Normalize();
            bitangent = Vector3.Cross(normal, tangent);
            bitangent.Normalize();
        }

        //==============================================================================
        // 기능: MeshCutCap::ProjectTo2D
        // 설명: 3D 루프 정점을 캡 평면의 2D 좌표로 투영한다.
        //==============================================================================
        private static void ProjectTo2D(List<Vector3> points, Vector3 origin, Vector3 tangent, Vector3 bitangent, List<Vector2> projected)
        {
            projected.Clear();
            if (projected.Capacity < points.Count) projected.Capacity = points.Count;

            int pointCount = points.Count;
            for (int i = 0; i < pointCount; i++)
            {
                Vector3 delta = points[i] - origin;
                projected.Add(new Vector2(Vector3.Dot(delta, tangent), Vector3.Dot(delta, bitangent)));
            }
        }

        //==============================================================================
        // 기능: MeshCutCap::BuildCapUVs
        // 설명: 캡 2D 좌표 범위를 0..1 UV 공간으로 정규화한다.
        //==============================================================================
        private static void BuildCapUVs(List<Vector2> projected, List<Vector2> capUVs)
        {
            CalculateUVBounds(projected, out Vector2 min, out Vector2 size);

            capUVs.Clear();
            if (capUVs.Capacity < projected.Count) capUVs.Capacity = projected.Count;

            int pointCount = projected.Count;
            for (int i = 0; i < pointCount; i++)
            {
                float u = size.x > UvEpsilon ? (projected[i].x - min.x) / size.x : 0.5f;
                float v = size.y > UvEpsilon ? (projected[i].y - min.y) / size.y : 0.5f;
                capUVs.Add(new Vector2(u, v));
            }
        }

        //==============================================================================
        // 기능: MeshCutCap::CalculateUVBounds
        // 설명: 캡 투영 좌표의 최소점과 크기를 계산한다.
        //==============================================================================
        private static void CalculateUVBounds(List<Vector2> projected, out Vector2 min, out Vector2 size)
        {
            min = projected[0];
            Vector2 max = projected[0];

            int pointCount = projected.Count;
            for (int i = 1; i < pointCount; i++)
            {
                min = Vector2.Min(min, projected[i]);
                max = Vector2.Max(max, projected[i]);
            }

            size = max - min;
        }

        //==============================================================================
        // 기능: MeshCutCap::SignedArea
        // 설명: 2D 폴리곤의 signed area 로 와인딩 방향과 퇴화 여부를 확인한다.
        //==============================================================================
        private static float SignedArea(List<Vector2> points)
        {
            float area = 0f;
            int count = points.Count;
            for (int i = 0; i < count; i++)
            {
                Vector2 a = points[i];
                Vector2 b = points[(i + 1) % count];
                area += a.x * b.y - b.x * a.y;
            }

            return area * 0.5f;
        }

        //==============================================================================
        // 기능: MeshCutCap::Quantize
        // 설명: 절단점 위치를 웰드용 정수 좌표로 변환한다.
        //==============================================================================
        private static Vector3Int Quantize(Vector3 value)
        {
            return new Vector3Int
            (
                Mathf.RoundToInt(value.x * WeldScale),
                Mathf.RoundToInt(value.y * WeldScale),
                Mathf.RoundToInt(value.z * WeldScale)
            );
        }

        //==============================================================================
        // 기능: MeshCutCap::Cross2
        // 설명: 2D 세 점의 외적 부호를 반환해 좌회전/우회전 여부를 판정한다.
        //==============================================================================
        private static float Cross2(Vector2 origin, Vector2 a, Vector2 b)
        {
            return (a.x - origin.x) * (b.y - origin.y) - (a.y - origin.y) * (b.x - origin.x);
        }
    }

    //==============================================================================
    // 기능: MeshCutEdgeKey
    // 설명: 절단 에지의 두 정점 인덱스를 순서와 무관하게 비교하기 위한 키이다.
    //==============================================================================
    readonly struct MeshCutEdgeKey : IEquatable<MeshCutEdgeKey>
    {
        // 작은 정점 인덱스
        public int A { get; }

        // 큰 정점 인덱스
        public int B { get; }

        //==============================================================================
        // 기능: MeshCutEdgeKey::MeshCutEdgeKey
        // 설명: 입력 순서와 관계없이 같은 에지는 같은 A/B 값을 갖도록 정렬한다.
        //==============================================================================
        public MeshCutEdgeKey(int a, int b)
        {
            if (a <= b)
            {
                A = a;
                B = b;
            }
            else
            {
                A = b;
                B = a;
            }
        }

        //==============================================================================
        // 기능: MeshCutEdgeKey::Equals
        // 설명: 두 에지 키가 같은 정점 쌍을 가리키는지 비교한다.
        //==============================================================================
        public bool Equals(MeshCutEdgeKey other)
        {
            return A == other.A && B == other.B;
        }

        //==============================================================================
        // 기능: MeshCutEdgeKey::Equals
        // 설명: object 비교에서도 MeshCutEdgeKey 값 비교를 사용한다.
        //==============================================================================
        public override bool Equals(object obj)
        {
            return obj is MeshCutEdgeKey other && Equals(other);
        }

        //==============================================================================
        // 기능: MeshCutEdgeKey::GetHashCode
        // 설명: HashSet 과 Dictionary 에서 사용할 에지 키 해시를 만든다.
        //==============================================================================
        public override int GetHashCode()
        {
            unchecked
            {
                return (A * 397) ^ B;
            }
        }
    }

    //==============================================================================
    // 기능: MeshCutCapBuffer
    // 설명: 캡 생성 과정에서 사용하는 웰드, 그래프, 투영, 삼각분할 리스트를 재사용한다.
    //==============================================================================
    class MeshCutCapBuffer
    {
        // 양자화된 위치에서 Points 인덱스로 가는 웰드 맵
        public Dictionary<Vector3Int, int> WeldMap { get; } = new Dictionary<Vector3Int, int>();

        // 웰드된 절단점의 3D 위치 목록
        public List<Vector3> Points { get; } = new List<Vector3>();

        // 각 웰드 정점이 연결된 다른 정점 인덱스 목록
        public List<List<int>> Adjacency { get; } = new List<List<int>>();

        // 중복 제거 후 남은 절단 에지 목록
        public List<MeshCutEdgeKey> Edges { get; } = new List<MeshCutEdgeKey>();

        // 에지 중복 등록을 막기 위한 전체 에지 집합
        public HashSet<MeshCutEdgeKey> AllEdges { get; } = new HashSet<MeshCutEdgeKey>();

        // 루프 추적 중 이미 지나간 에지 집합
        public HashSet<MeshCutEdgeKey> UsedEdges { get; } = new HashSet<MeshCutEdgeKey>();

        // 캡 후보 루프 목록. 각 루프는 Points 인덱스 목록이다.
        public List<List<int>> Loops { get; } = new List<List<int>>();

        // 현재 추적 중인 루프의 Points 인덱스 임시 목록
        public List<int> TraceLoop { get; } = new List<int>();

        // 현재 캡으로 만들 루프의 3D 정점 목록
        public List<Vector3> Loop3D { get; } = new List<Vector3>();

        // Loop3D 를 캡 평면에 투영한 2D 좌표 목록
        public List<Vector2> Projected { get; } = new List<Vector2>();

        // 캡 정점에 적용할 0..1 UV 목록
        public List<Vector2> CapUVs { get; } = new List<Vector2>();

        // ear clipping 결과 삼각형 인덱스 목록
        public List<int> Triangles { get; } = new List<int>();

        // ear clipping 중 남아 있는 폴리곤 정점 인덱스 목록
        public List<int> Polygon { get; } = new List<int>();

        //==============================================================================
        // 기능: MeshCutCapBuffer::ClearGraph
        // 설명: 캡 루프 그래프를 새로 만들기 전에 관련 임시 데이터를 비운다.
        //==============================================================================
        public void ClearGraph()
        {
            WeldMap.Clear();
            Points.Clear();
            Edges.Clear();
            AllEdges.Clear();
            UsedEdges.Clear();
            Loops.Clear();
            TraceLoop.Clear();

            int adjacencyCount = Adjacency.Count;
            for (int i = 0; i < adjacencyCount; i++)
                Adjacency[i].Clear();
            Adjacency.Clear();
        }
    }
}
