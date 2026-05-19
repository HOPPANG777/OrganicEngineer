using System.Collections.Generic;
using UnityEngine;

namespace OrganicEngineer.MeshCutting
{
    //==============================================================================
    // 기능: MeshCutCap
    // 설명: 절단선 에지 목록으로 볼록 단면 캡을 생성해 MeshBuffer 에 추가하는 정적 처리기이다.
    //==============================================================================
    // TODO: 오목 단면 ear-clipping 방식 지원
    //
    //==============================================================================
    static class MeshCutCap
    {
        //-------------------- 멤버 변수 --------------------//

        // 단면 법선 판별에 사용하는 허용 오차
        private const float _normalEpsilon = 1e-5f;

        // 캡 UV 정규화에 사용하는 허용 오차
        private const float _uvEpsilon = 1e-5f;

        // 절단점 웰드용 정수 그리드 스케일
        private const float _weldScale = 1e4f;

        //-------------------- 기능 함수 --------------------//

        //==============================================================================
        // 기능: MeshCutCap::AppendIfNeeded
        // 설명: 실제 절단 여부에 따라 캡을 만들거나 Above/Below 포함 관계만 정리한다.
        //==============================================================================
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
            // 실제 절단이 있으면 양쪽 절단면을 캡으로 닫는다.
            if (straddling)
            {
                Append(above, result.AboveCapEdges, -planeNormal, buffer);
                Append(below, result.BelowCapEdges, planeNormal, buffer);
                result.Relation = CutPlaneRelation.STRADDLING;
                return;
            }

            // 절단이 없으면 캡 후보 에지를 비우고 버퍼 상태만으로 관계를 판정한다.
            result.AboveCapEdges.Clear();
            result.BelowCapEdges.Clear();
            result.Relation = MeshCutUtility.ResolveRelation(above.HasAnyTriangle(), below.HasAnyTriangle());
        }

        //==============================================================================
        // 기능: MeshCutCap::Append
        // 설명: 절단 에지로 단면 루프를 만들고 Fan 삼각분할로 캡 삼각형을 추가한다.
        //==============================================================================
        // TODO: 캡 UV 타일링/월드 스케일 옵션
        //
        //==============================================================================
        private static void Append(MeshBuffer meshBuffer, List<CutEdge> edges, Vector3 outwardNormal, MeshCutCapBuffer capBuffer)
        {
            // 에지가 없으면 캡 생성 불가
            if (edges == null || edges.Count == 0) return;

            // 단면 법선 정규화
            Vector3 normal = outwardNormal.normalized;

            // 법선이 너무 작으면 캡 생성 불가
            if (normal.sqrMagnitude < _normalEpsilon) return;

            // 볼록 캡 루프 생성 실패 시 종료
            if (!TryBuildLoop(edges, normal, capBuffer, out List<Vector3> loop3d)) return;

            // 루프 정점이 3개 미만이면 면 생성 불가
            if (loop3d.Count < 3) return;

            // 캡 정점 시작 인덱스 저장
            int baseIndex = meshBuffer.Verts.Count;

            // 단면 평면 좌표를 0..1 범위 UV 로 변환
            List<Vector2> capUVs = BuildCapUVs(loop3d, normal, capBuffer);

            // 단면 루프 정점을 버퍼에 추가
            for (int i = 0; i < loop3d.Count; i++)
            {
                meshBuffer.AddVertex(loop3d[i], capUVs[i], normal);
            }

            // 첫 정점을 기준으로 Fan 삼각형 생성
            for (int i = 1; i < loop3d.Count - 1; i++)
            {
                meshBuffer.AddCapTriangle(baseIndex, baseIndex + i, baseIndex + i + 1);
            }
        }

        //==============================================================================
        // 기능: MeshCutCap::TryBuildLoop
        // 설명: 절단 에지를 웰드한 뒤 2D 볼록 껍질을 통해 캡 루프를 만든다.
        //==============================================================================
        private static bool TryBuildLoop(List<CutEdge> edges, Vector3 normal, MeshCutCapBuffer buffer, out List<Vector3> loop3d)
        {
            // 실패 기본값 초기화
            loop3d = null;

            // 웰드된 점 목록 생성
            BuildPoints(edges, buffer);
            List<Vector3> points = buffer.Points;

            // 점이 3개 미만이면 루프 생성 불가
            if (points.Count < 3) return false;

            // 2D 투영 축 계산
            BuildProjectionAxes(normal, out Vector3 tangent, out Vector3 bitangent);

            // 3D 점을 2D 점으로 투영
            ProjectTo2D(points, points[0], tangent, bitangent, buffer.Projected);

            // 2D 볼록 껍질 인덱스 계산
            List<int> hull = BuildConvexHull2DIndices(buffer.Projected, buffer);

            // 껍질 정점이 3개 미만이면 실패
            if (hull == null || hull.Count < 3) return false;

            // 껍질 인덱스를 3D 점 루프로 변환
            BuildHullPoints(points, hull, buffer.HullPoints);

            // 바깥 법선 방향에 맞게 와인딩 보정
            FixWinding(buffer.HullPoints, normal);

            // 최종 루프 반환
            loop3d = buffer.HullPoints;
            return true;
        }

        //==============================================================================
        // 기능: MeshCutCap::BuildPoints
        // 설명: CutEdge 목록의 A/B 점을 양자화 웰드해 고유한 3D 점 목록으로 만든다.
        //==============================================================================
        private static void BuildPoints(List<CutEdge> edges, MeshCutCapBuffer buffer)
        {
            // 이전 웰드 결과 초기화
            buffer.WeldMap.Clear();
            buffer.Points.Clear();

            // 모든 에지의 양 끝점을 순회
            for (int i = 0; i < edges.Count; i++)
            {
                WeldPoint(buffer, edges[i].A);
                WeldPoint(buffer, edges[i].B);
            }
        }

        //==============================================================================
        // 기능: MeshCutCap::WeldPoint
        // 설명: 양자화 키로 중복점을 제거하며 점 목록에 추가한다.
        //==============================================================================
        private static void WeldPoint(MeshCutCapBuffer buffer, Vector3 point)
        {
            // 연속 좌표를 정수 키로 변환
            Vector3Int key = Quantize(point);

            // 이미 등록된 점이면 추가하지 않음
            if (buffer.WeldMap.ContainsKey(key)) return;

            // 새 점의 인덱스 기록
            buffer.WeldMap[key] = buffer.Points.Count;

            // 새 점 추가
            buffer.Points.Add(point);
        }

        //==============================================================================
        // 기능: MeshCutCap::BuildProjectionAxes
        // 설명: 단면 법선을 기준으로 평면 투영용 tangent/bitangent 축을 만든다.
        //==============================================================================
        private static void BuildProjectionAxes(Vector3 normal, out Vector3 tangent, out Vector3 bitangent)
        {
            // 월드 up 과 법선의 외적으로 첫 축 계산
            tangent = Vector3.Cross(normal, Vector3.up);

            // 법선이 up 과 거의 평행하면 right 축으로 대체
            if (tangent.sqrMagnitude < 1e-6f) tangent = Vector3.Cross(normal, Vector3.right);

            // 첫 번째 축 정규화
            tangent.Normalize();

            // 두 번째 평면 축 계산
            bitangent = Vector3.Cross(normal, tangent);

            // 두 번째 축 정규화
            bitangent.Normalize();
        }

        //==============================================================================
        // 기능: MeshCutCap::ProjectTo2D
        // 설명: 3D 점 목록을 기준 원점과 두 평면 축을 사용해 2D 좌표로 투영한다.
        //==============================================================================
        private static void ProjectTo2D(List<Vector3> points, Vector3 origin, Vector3 tangent, Vector3 bitangent, List<Vector2> projected)
        {
            // 이전 투영 결과 초기화
            projected.Clear();

            // 입력 크기만큼 Capacity 확보
            if (projected.Capacity < points.Count) projected.Capacity = points.Count;

            // 모든 점을 tangent/bitangent 좌표계로 변환
            for (int i = 0; i < points.Count; i++)
            {
                Vector3 delta = points[i] - origin;
                projected.Add(new Vector2(Vector3.Dot(delta, tangent), Vector3.Dot(delta, bitangent)));
            }
        }

        //==============================================================================
        // 기능: MeshCutCap::BuildCapUVs
        // 설명: 캡 루프 정점에 평면 투영 UV 를 생성한다.
        //==============================================================================
        private static List<Vector2> BuildCapUVs(List<Vector3> loop3d, Vector3 normal, MeshCutCapBuffer buffer)
        {
            // 투영 축 생성
            BuildProjectionAxes(normal, out Vector3 tangent, out Vector3 bitangent);

            // 캡 첫 정점을 기준 원점으로 사용해 2D 좌표 생성
            ProjectTo2D(loop3d, loop3d[0], tangent, bitangent, buffer.Projected);

            // 투영 좌표 범위 계산
            CalculateUVBounds(buffer.Projected, out Vector2 min, out Vector2 size);

            // 정규화 결과 목록 초기화
            buffer.CapUVs.Clear();
            if (buffer.CapUVs.Capacity < buffer.Projected.Count) buffer.CapUVs.Capacity = buffer.Projected.Count;

            // 캡 내부에서 0..1 범위가 되도록 정규화
            for (int i = 0; i < buffer.Projected.Count; i++)
            {
                float u = size.x > _uvEpsilon ? (buffer.Projected[i].x - min.x) / size.x : 0.5f;
                float v = size.y > _uvEpsilon ? (buffer.Projected[i].y - min.y) / size.y : 0.5f;
                buffer.CapUVs.Add(new Vector2(u, v));
            }

            return buffer.CapUVs;
        }

        //==============================================================================
        // 기능: MeshCutCap::CalculateUVBounds
        // 설명: 투영 좌표의 최소점과 크기를 계산한다.
        //==============================================================================
        private static void CalculateUVBounds(List<Vector2> projected, out Vector2 min, out Vector2 size)
        {
            // 첫 좌표로 초기화
            min = projected[0];
            Vector2 max = projected[0];

            // 전체 투영 좌표 범위 누적
            for (int i = 1; i < projected.Count; i++)
            {
                min = Vector2.Min(min, projected[i]);
                max = Vector2.Max(max, projected[i]);
            }

            size = max - min;
        }

        //==============================================================================
        // 기능: MeshCutCap::BuildConvexHull2DIndices
        // 설명: 2D 점 집합에서 볼록 껍질을 이루는 원본 인덱스 목록을 재사용 버퍼에 만든다.
        //==============================================================================
        private static List<int> BuildConvexHull2DIndices(List<Vector2> points, MeshCutCapBuffer buffer)
        {
            // 점이 3개 미만이면 면을 만들 수 없음
            int pointCount = points.Count;
            if (pointCount < 3) return null;

            // 정렬용 인덱스 목록 초기화
            buffer.Order.Clear();
            if (buffer.Order.Capacity < pointCount) buffer.Order.Capacity = pointCount;
            for (int i = 0; i < pointCount; i++)
            {
                buffer.Order.Add(i);
            }

            // x 우선, y 보조 기준으로 정렬
            buffer.Order.Sort((i, j) =>
            {
                int compare = points[i].x.CompareTo(points[j].x);
                if (compare != 0) return compare;
                return points[i].y.CompareTo(points[j].y);
            });

            // 하단/상단 껍질 계산
            BuildHullHalf(points, buffer.Order, 0, pointCount, 1, buffer.Lower);
            BuildHullHalf(points, buffer.Order, pointCount - 1, -1, -1, buffer.Upper);

            // 중복 끝점 제거 후 하나로 합침
            buffer.Hull.Clear();
            if (buffer.Lower.Count > 0) buffer.Lower.RemoveAt(buffer.Lower.Count - 1);
            if (buffer.Upper.Count > 0) buffer.Upper.RemoveAt(buffer.Upper.Count - 1);
            buffer.Hull.AddRange(buffer.Lower);
            buffer.Hull.AddRange(buffer.Upper);

            return buffer.Hull;
        }

        //==============================================================================
        // 기능: MeshCutCap::BuildHullHalf
        // 설명: Monotone Chain 의 한쪽 껍질을 지정 방향으로 생성한다.
        //==============================================================================
        private static void BuildHullHalf(List<Vector2> points, List<int> order, int start, int end, int step, List<int> hull)
        {
            // 기존 재사용 리스트 내용 초기화
            hull.Clear();

            // 정렬 순서에 따라 한쪽 껍질 구성
            for (int i = start; i != end; i += step)
            {
                int index = order[i];
                while (hull.Count >= 2 && Cross2(points[hull[hull.Count - 2]], points[hull[hull.Count - 1]], points[index]) <= 0f)
                {
                    hull.RemoveAt(hull.Count - 1);
                }

                hull.Add(index);
            }
        }

        //==============================================================================
        // 기능: MeshCutCap::BuildHullPoints
        // 설명: hull 인덱스로 3D 껍질 점 목록을 만든다.
        //==============================================================================
        private static void BuildHullPoints(List<Vector3> points, List<int> hull, List<Vector3> hullPoints)
        {
            // 결과 루프 목록 초기화
            hullPoints.Clear();
            if (hullPoints.Capacity < hull.Count) hullPoints.Capacity = hull.Count;

            // hull 순서대로 3D 점 복사
            for (int i = 0; i < hull.Count; i++)
            {
                hullPoints.Add(points[hull[i]]);
            }
        }

        //==============================================================================
        // 기능: MeshCutCap::FixWinding
        // 설명: 바깥 법선 방향에 맞도록 루프 와인딩을 보정한다.
        //==============================================================================
        private static void FixWinding(List<Vector3> hullPoints, Vector3 normal)
        {
            // 검사할 삼각형의 첫 번째 변
            Vector3 c0 = hullPoints[1] - hullPoints[0];

            // 검사할 삼각형의 두 번째 변
            Vector3 c1 = hullPoints[2] - hullPoints[0];

            // 현재 루프가 만드는 법선 방향
            Vector3 cross = Vector3.Cross(c0, c1);

            // 바깥 법선과 반대면 순서를 뒤집음
            if (Vector3.Dot(cross, normal) < 0f) hullPoints.Reverse();
        }

        //==============================================================================
        // 기능: MeshCutCap::Quantize
        // 설명: 연속 좌표를 웰드용 정수 키로 양자화한다.
        //==============================================================================
        private static Vector3Int Quantize(Vector3 value)
        {
            return new Vector3Int
            (
                Mathf.RoundToInt(value.x * _weldScale),
                Mathf.RoundToInt(value.y * _weldScale),
                Mathf.RoundToInt(value.z * _weldScale)
            );
        }

        //==============================================================================
        // 기능: MeshCutCap::Cross2
        // 설명: 2D 세 점의 외적 부호를 계산한다.
        //==============================================================================
        private static float Cross2(Vector2 origin, Vector2 a, Vector2 b)
        {
            return (a.x - origin.x) * (b.y - origin.y) - (a.y - origin.y) * (b.x - origin.x);
        }
    }

    //==============================================================================
    // 기능: MeshCutCapBuffer
    // 설명: 단면 캡 생성 중 반복 할당을 줄이기 위해 웰드·투영·볼록 껍질 리스트를 재사용한다.
    //==============================================================================
    // TODO: 다중 스레드 절단이 필요하면 MeshCutter 인스턴스별 버퍼 소유를 명시적으로 분리
    //
    //==============================================================================
    class MeshCutCapBuffer
    {
        // 양자화 좌표에서 Points 인덱스로 가는 재사용 맵
        public Dictionary<Vector3Int, int> WeldMap { get; } = new Dictionary<Vector3Int, int>();

        // 웰드된 고유 절단점 목록
        public List<Vector3> Points { get; } = new List<Vector3>();

        // 3D 점을 단면 평면에 투영한 2D 좌표 목록
        public List<Vector2> Projected { get; } = new List<Vector2>();

        // 캡 정점에 적용할 0..1 UV 목록
        public List<Vector2> CapUVs { get; } = new List<Vector2>();

        // 최종 3D 볼록 껍질 루프 목록
        public List<Vector3> HullPoints { get; } = new List<Vector3>();

        // 볼록 껍질 정렬용 인덱스 목록
        public List<int> Order { get; } = new List<int>();

        // Monotone Chain 하단 껍질 인덱스 목록
        public List<int> Lower { get; } = new List<int>();

        // Monotone Chain 상단 껍질 인덱스 목록
        public List<int> Upper { get; } = new List<int>();

        // 최종 볼록 껍질 인덱스 목록
        public List<int> Hull { get; } = new List<int>();
    }
}
