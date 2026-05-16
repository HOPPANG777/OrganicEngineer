using System.Collections.Generic;
using UnityEngine;

namespace OrganicEngineer.MeshCutting
{
    //==============================================================================
    // 기능: MeshCutCap
    // 설명: 절단선 에지 목록으로 볼록 단면 캡을 생성해 MeshBuffer 에 추가한다.
    //==============================================================================
    // TODO: 오목 단면 ear-clipping 방식 지원
    //
    //==============================================================================
    class MeshCutCap
    {
        //-------------------- 맴버 변수 --------------------//

        // 단면 법선 판별에 사용하는 허용 오차
        private float _normalEpsilon = 1e-5f;

        // 캡 UV 정규화에 사용하는 허용 오차
        private float _uvEpsilon = 1e-5f;

        // 2D 볼록 껍질 계산기
        private MeshCutHull _meshCutHull = new MeshCutHull();

        // 절단점 웰드 기능
        private MeshCutWeld _meshCutWeld = new MeshCutWeld();

        //==============================================================================
        // 기능: MeshCutCap::Append
        // 설명: 절단 에지로 단면 루프를 만들고 Fan 삼각분할로 캡 삼각형을 추가한다.
        //==============================================================================
        // TODO: 캡 UV 타일링/월드 스케일 옵션
        //
        //==============================================================================
        public void Append(MeshBuffer buffer, List<CutEdge> edges, Vector3 outwardNormal)
        {
            // 에지가 없으면 캡 생성 불가
            if (edges == null || edges.Count == 0) return;

            // 볼록 캡 루프 생성 실패 시 종료
            if (!TryBuildLoop(edges, outwardNormal, out List<Vector3> loop3d)) return;

            // 루프 정점이 3개 미만이면 면 생성 불가
            if (loop3d.Count < 3) return;

            // 캡 정점 시작 인덱스 저장
            int baseIndex = buffer.verts.Count;

            // 단면 평면 좌표를 0..1 범위 UV 로 변환
            List<Vector2> capUVs = BuildCapUVs(loop3d, outwardNormal.normalized);

            // 단면 루프 정점을 버퍼에 추가
            for (int i = 0; i < loop3d.Count; i++)
            {
                buffer.AddVertex(loop3d[i], capUVs[i], outwardNormal.normalized);
            }

            // 첫 정점을 기준으로 Fan 삼각형 생성
            for (int i = 1; i < loop3d.Count - 1; i++)
            {
                buffer.AddCapTriangle(baseIndex, baseIndex + i, baseIndex + i + 1);
            }
        }

        // 절단 에지에서 3D 볼록 루프를 생성
        private bool TryBuildLoop(List<CutEdge> edges, Vector3 outwardNormal, out List<Vector3> loop3d)
        {
            // 실패 기본값 초기화
            loop3d = null;

            // 웰드된 점 목록 생성
            List<Vector3> points = _meshCutWeld.BuildPoints(edges);

            // 점이 3개 미만이면 루프 생성 불가
            if (points.Count < 3) return false;

            // 단면 법선 정규화
            Vector3 normal = outwardNormal.normalized;

            // 법선이 너무 작으면 루프 생성 불가
            if (normal.sqrMagnitude < _normalEpsilon) return false;

            // 2D 투영 축 계산
            BuildProjectionAxes(normal, out Vector3 tangent, out Vector3 bitangent);

            // 3D 점을 2D 점으로 투영
            List<Vector2> points2d = ProjectTo2D(points, points[0], tangent, bitangent);

            // 2D 볼록 껍질 인덱스 계산
            int[] hull = _meshCutHull.ConvexHull2DIndices(points2d);

            // 껍질 정점이 3개 미만이면 실패
            if (hull == null || hull.Length < 3) return false;

            // 껍질 인덱스를 3D 점 루프로 변환
            List<Vector3> hullPoints = BuildHullPoints(points, hull);

            // 바깥 법선 방향에 맞게 와인딩 보정
            FixWinding(hullPoints, normal);

            // 최종 루프 반환
            loop3d = hullPoints;
            return true;
        }

        // 법선 기준 2D 투영 축 생성
        private void BuildProjectionAxes(Vector3 normal, out Vector3 tangent, out Vector3 bitangent)
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

        // 3D 점 목록을 2D 좌표로 투영
        private List<Vector2> ProjectTo2D(List<Vector3> points, Vector3 origin, Vector3 tangent, Vector3 bitangent)
        {
            // 투영 결과 목록
            List<Vector2> points2d = new List<Vector2>(points.Count);

            // 모든 점을 tangent/bitangent 좌표계로 변환
            for (int i = 0; i < points.Count; i++)
            {
                Vector3 delta = points[i] - origin;
                points2d.Add(new Vector2(Vector3.Dot(delta, tangent), Vector3.Dot(delta, bitangent)));
            }

            return points2d;
        }

        // 캡 루프 정점에 평면 투영 UV 를 생성
        private List<Vector2> BuildCapUVs(List<Vector3> loop3d, Vector3 normal)
        {
            // 투영 축 생성
            BuildProjectionAxes(normal, out Vector3 tangent, out Vector3 bitangent);

            // 캡 첫 정점을 기준 원점으로 사용
            List<Vector2> projected = ProjectTo2D(loop3d, loop3d[0], tangent, bitangent);

            // 투영 좌표 범위 계산
            CalculateUVBounds(projected, out Vector2 min, out Vector2 size);

            // 정규화 결과 목록
            List<Vector2> capUVs = new List<Vector2>(projected.Count);

            // 캡 내부에서 0..1 범위가 되도록 정규화
            for (int i = 0; i < projected.Count; i++)
            {
                float u = size.x > _uvEpsilon ? (projected[i].x - min.x) / size.x : 0.5f;
                float v = size.y > _uvEpsilon ? (projected[i].y - min.y) / size.y : 0.5f;
                capUVs.Add(new Vector2(u, v));
            }

            return capUVs;
        }

        // 투영 좌표의 최소점과 크기를 계산
        private void CalculateUVBounds(List<Vector2> projected, out Vector2 min, out Vector2 size)
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

        // hull 인덱스로 3D 껍질 점 목록 생성
        private List<Vector3> BuildHullPoints(List<Vector3> points, int[] hull)
        {
            // 결과 루프 목록
            List<Vector3> hullPoints = new List<Vector3>(hull.Length);

            // hull 순서대로 3D 점 복사
            for (int i = 0; i < hull.Length; i++)
            {
                hullPoints.Add(points[hull[i]]);
            }

            return hullPoints;
        }

        // 바깥 법선 방향에 맞도록 루프 와인딩 보정
        private void FixWinding(List<Vector3> hullPoints, Vector3 normal)
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

    }
}
