using System; // Array.Sort 등 BCL
using System.Collections.Generic; // List, Dictionary
using UnityEngine; // Mesh, Plane, Vector*, Mathf

namespace OrganicEngineer.MeshCutting
{
    //==============================================================================
    // 기능: MeshCutter (정적 클래스)
    // 설명: 메시 로컬 평면으로 삼각형을 분류·분할해 두 조각 Mesh를 만들고, 절단면은 볼록 다각형 가정으로 캡(서브메시 1)을 붙인다.
    //==============================================================================
    // TODO: 오목 단면(ear-clipping 등), 스킨드 메시, WeldInv·허용오차 옵션화
    //
    //==============================================================================
    public static class MeshCutter
    {
        // 선분–평면 교차: 분모가 0에 가까울 때 사용하는 허용 오차
        const float EdgeIntersectEpsilon = 1e-5f;

        // 절단점 웰드 시 정수 그리드 키 만들기 위해 좌표에 곱하는 스케일
        const float WeldInv = 1e4f;

        /// <summary>
        /// source 메시를 메시 로컬 공간 평면으로 자른다.
        /// Straddling 이면 단면은 볼록 다각형 가정으로 캡(서브메시 1)을 붙인다.
        /// </summary>
        //==============================================================================
        // 기능: MeshCutter::Cut
        // 설명: 정점별 평면 side를 구한 뒤 삼각형을 통째 복사 또는 SplitTriangle 분할하고, Straddling이면 캡을 시도한 뒤 relation·Mesh를 채워 반환한다.
        //==============================================================================
        // TODO: 공면 삼각형·퇴화 케이스 필터링, 로그 레벨·진단 플래그
        //
        //==============================================================================
        public static CutResult Cut(Mesh src, Plane planeLocal)
        {
            var result = new CutResult(); // 절단 결과 누적 컨테이너

            if (src == null || src.triangles == null || src.triangles.Length == 0)
            {
                result.relation = CutPlaneRelation.EmptySource;
                return result;
            }

            // 삼각형 캐싱
            Vector3[] srcVerts = src.vertices;  // 정점 배열 참조
            if (srcVerts == null || srcVerts.Length == 0)
            {
                result.relation = CutPlaneRelation.EmptySource;
                return result;
            }

            Vector2[] srcUVs = src.uv;          // UV 채널 0 참조(null 가능)
            Vector3[] srcNormals = src.normals; // 노멀 채널 참조(null 가능)
            int[] srcTris = src.triangles;      // 삼각형 인덱스 버퍼(길이 3의 배수)

            // side 저장
            bool[] side = new bool[srcVerts.Length]; // 각 정점이 평면의 양(+)쪽이면 true

            for (int i = 0; i < srcVerts.Length; i++) // 모든 정점에 대해 부호 계산
            {
                side[i] = planeLocal.GetSide(srcVerts[i]);  // true = +, false = -
            }

            var above = new MeshBuffer(); // 평면 법선 +쪽(Above)에 쌓는 버퍼
            var below = new MeshBuffer(); // 평면 반대(Below)에 쌓는 버퍼

            bool straddling = false; // 한 삼각형이라도 평면에 걸쳤는지

            for (int t = 0; t < srcTris.Length; t += 3) // 삼각형마다(인덱스 3개)
            {
                int i0 = srcTris[t + 0]; // 삼각형 정점 인덱스 0
                int i1 = srcTris[t + 1]; // 삼각형 정점 인덱스 1
                int i2 = srcTris[t + 2]; // 삼각형 정점 인덱스 2

                bool s0 = side[i0]; // i0 의 평면 위치(+/-)
                bool s1 = side[i1]; // i1 의 평면 위치(+/-)
                bool s2 = side[i2]; // i2 의 평면 위치(+/-)

                if (s0 && s1 && s2) // 세 정점 모두 같은 쪽(Above)
                {
                    CopyTriangle(above, srcVerts, srcUVs, srcNormals, i0, i1, i2);
                }
                else if (!s0 && !s1 && !s2) // 세 정점 모두 Below
                {
                    CopyTriangle(below, srcVerts, srcUVs, srcNormals, i0, i1, i2);
                }
                else // 한쪽 두 정점과 반대쪽 한 정점(또는 그 반대) → 분할
                {
                    straddling = true; // 평면과 교차하는 삼각형 존재
                    SplitTriangle
                    (
                        above,
                        below,
                        planeLocal,
                        srcVerts, srcUVs, srcNormals,
                        i0, i1, i2,
                        s0, s1, s2,
                        side,
                        result
                    );
                }
            }

            Vector3 planeNormal = planeLocal.normal; // 캡 바깥 방향 판별용 평면 법선

            if (straddling) // 걸침 있음 → 양쪽 캡 에지 활용 가능
            {
                // +쪽 조각: 단면 바깥을 향하는 법선 = plane.normal (Above 영역 밖으로)
                TryAppendConvexCap(above, result.aboveCapEdges, planeNormal);
                // -쪽 조각: 단면 바깥이 -plane.normal
                TryAppendConvexCap(below, result.belowCapEdges, -planeNormal);

                result.relation = CutPlaneRelation.Straddling;
            }
            else // 걸침 없음: 전부 한쪽 또는 비어 있음
            {
                result.aboveCapEdges.Clear(); // 캡 불필요
                result.belowCapEdges.Clear();

                bool hasA = above.tris.Count > 0; // Above 버퍼에 외피 삼각형 존재
                bool hasB = below.tris.Count > 0; // Below 버퍼에 외피 삼각형 존재

                if (!hasA && !hasB)
                {
                    result.relation = CutPlaneRelation.EmptySource;
                }
                else if (hasA && !hasB)
                {
                    result.relation = CutPlaneRelation.EntirelyAbove;
                }
                else if (!hasA && hasB)
                {
                    result.relation = CutPlaneRelation.EntirelyBelow;
                }
                else
                {
                    Debug.LogWarning
                    (
                        "MeshCutter: 양쪽에 지오메트리가 있으나 Straddling 이 아님. Straddling 으로 처리합니다."
                    );
                    result.relation = CutPlaneRelation.Straddling;
                }
            }

            result.aboveMesh = above.tris.Count > 0 ? above.ToMesh() : null; // Above Mesh 또는 null
            result.belowMesh = below.tris.Count > 0 ? below.ToMesh() : null; // Below Mesh 또는 null

            return result;
        }

        //==============================================================================
        // 기능: MeshCutter::CopyTriangle
        // 설명: 원본 삼각형을 지정 버퍼에 정점 세 개 추가 후 한 삼각형으로 등록한다(절단 없음).
        //==============================================================================
        // TODO:
        //
        //==============================================================================
        static void CopyTriangle
        (
            MeshBuffer buffer, // 대상 누적 버퍼
            Vector3[] srcVec,
            Vector2[] srcUV,
            Vector3[] srcNormal,
            int i0, int i1, int i2 // 원본 삼각형 정점 인덱스 세 개
        )
        {
            int a = buffer.AddVertex(srcVec[i0], GetUV(srcUV, i0), GetNormal(srcNormal, i0)); // 새 정점 a
            int b = buffer.AddVertex(srcVec[i1], GetUV(srcUV, i1), GetNormal(srcNormal, i1)); // 새 정점 b
            int c = buffer.AddVertex(srcVec[i2], GetUV(srcUV, i2), GetNormal(srcNormal, i2)); // 새 정점 c
            buffer.AddTriangle(a, b, c); // 와인딩은 원본과 동일한 순서
        }

        //==============================================================================
        // 기능: MeshCutter::GetUV
        // 설명: UV 배열이 없거나 범위를 벗어난 인덱스면 Vector2.zero를 반환한다.
        //==============================================================================
        // TODO:
        //
        //==============================================================================
        static Vector2 GetUV(Vector2[] uvs, int i)
        {
            if (uvs == null || i < 0 || i >= uvs.Length)
            {
                return Vector2.zero;
            }

            return uvs[i];
        }

        //==============================================================================
        // 기능: MeshCutter::GetNormal
        // 설명: 노멀 배열이 없거나 범위를 벗어난 인덱스면 Vector3.up을 반환한다.
        //==============================================================================
        // TODO:
        //
        //==============================================================================
        static Vector3 GetNormal(Vector3[] normals, int i)
        {
            if (normals == null || i < 0 || i >= normals.Length)
            {
                return Vector3.up;
            }

            return normals[i];
        }

        //==============================================================================
        // 기능: MeshCutter::SplitTriangle
        // 설명: 평면에 걸친 단일 삼각형을 Lone 소삼각형과 반대편 사다리꼴(두 삼각형)로 나누고, PQ 절단선을 에지 목록에 넣는다.
        //==============================================================================
        // TODO: 정밀 한계로 인한 t 스냅·와인딩 반전 검증
        //
        //==============================================================================
        static void SplitTriangle
        (
            MeshBuffer above,
            MeshBuffer below,
            Plane plane,
            Vector3[] srcVec,
            Vector2[] srcUV,
            Vector3[] srcNormal,
            int i0, int i1, int i2,
            bool s0, bool s1, bool s2,
            bool[] side,
            CutResult cutResult
        )
        {
            // Step 6-1. 회전: Lone 정점을 i0 자리로
            //    sides 합이 1 이면 (s0, s1, s2 중 true 개수 1개) -> 그 인덱스가 Lone
            //    sides 합이 2 이면 false가 Lone
            int loneIndex;

            if (s0 != s1 && s0 != s2) loneIndex = 0; // i0 가 혼자 다른 부호
            else if (s1 != s0 && s1 != s2) loneIndex = 1; // s1이 Lone인 경우
            else loneIndex = 2; // 나머지 경우가 s2가 Lone 경우

            // 회전된 인덱스 (cyclic, 와인딩 보존)
            int L;    // L = lone: 삼각형이 평면 어디에 위치해 있는지에 대한 기준 혼자 다른 부호
            int M;    // M = Middle: lone 의 다음 정점 (lone 과 부호 반대, M-N 같은 부호)
            int N;    // N = Next: M 의 다음 정점
            switch (loneIndex)
            {
                case 0: // i0 이 lone인 경우
                    {
                        L = i0;
                        M = i1;
                        N = i2;
                    }
                    break;
                case 1: // i1 이 lone인 경우
                    {
                        L = i1;
                        M = i2;
                        N = i0;
                    }
                    break;
                default:// i2 가 lone인 경우
                    {
                        L = i2;
                        M = i0;
                        N = i1;
                    }
                    break;
            }
            bool loneSide = side[L]; // Lone 정점이 평면 +쪽이면 true(Above 헬프와 동일)

            // Step 6-2. 모서리 LM, LN 의 교점 P, Q 계산
            var (P_pos, P_uv, P_n) = LerpEdge(plane, srcVec, srcUV, srcNormal, L, M); // 모서리 LM 의 교점 P 계산
            var (Q_pos, Q_uv, Q_n) = LerpEdge(plane, srcVec, srcUV, srcNormal, L, N); // 모서리 LN 의 교점 Q 계산

            cutResult.aboveCapEdges.Add(new CutEdge(P_pos, Q_pos));
            cutResult.belowCapEdges.Add(new CutEdge(P_pos, Q_pos));

            // Step 6-3. Lone 쪽 = 삼각형 1 개 (L, P, Q)
            //           반대쪽 = 사다리꼴 (P, M, N, Q) -> (P, M, N) / (P, N, Q)
            MeshBuffer loneBuffer = loneSide ? above : below;     // Lone 쪽 버퍼
            MeshBuffer otherBuffer = loneSide ? below : above;    // 반대쪽 버퍼

            // Lone 쪽 삼각형
            int li = loneBuffer.AddVertex(srcVec[L], GetUV(srcUV, L), GetNormal(srcNormal, L)); // 정점 L 추가
            int lp = loneBuffer.AddVertex(P_pos, P_uv, P_n); // 교차점 P 추가
            int lq = loneBuffer.AddVertex(Q_pos, Q_uv, Q_n); // 교차점 Q 추가
            loneBuffer.AddTriangle(li, lp, lq); // 삼각형 (L,P,Q)

            // 반대쪽 사다리꼴
            int op = otherBuffer.AddVertex(P_pos, P_uv, P_n); // 중요: 같은 위치 P 라도 버퍼 분리 -> 새로 추가
            int om = otherBuffer.AddVertex(srcVec[M], GetUV(srcUV, M), GetNormal(srcNormal, M)); // 정점 M 추가
            int on = otherBuffer.AddVertex(srcVec[N], GetUV(srcUV, N), GetNormal(srcNormal, N)); // 정점 N 추가
            int oq = otherBuffer.AddVertex(Q_pos, Q_uv, Q_n); // 정점 Q 추가
            otherBuffer.AddTriangle(op, om, on); // 삼각형 (P, M, N) 와인딩 유지
            otherBuffer.AddTriangle(op, on, oq); // 삼각형 (P, N, Q) 와인딩 유지
        }

        //==============================================================================
        // 기능: MeshCutter::LerpEdge
        // 설명: 두 정점을 잇는 모서리와 평면의 교차점에서 위치·UV·법선을 선형 보간해 반환한다.
        //==============================================================================
        // TODO: 분모 Epsilon 시 양 끝점 평면 거리 부호 모순 검출
        //
        //==============================================================================
        static (Vector3 pos, Vector2 uv, Vector3 normal) LerpEdge
        (
            Plane plane,
            Vector3[] srcVec,
            Vector2[] srcUV,
            Vector3[] srcNormal,
            int a,
            int b
        )
        {
            float dA = plane.GetDistanceToPoint(srcVec[a]); // a에서 평면까지 signed 거리
            float dB = plane.GetDistanceToPoint(srcVec[b]); // b에서 평면까지 signed 거리

            float denom = dA - dB; // t = dA/denom 형태의 분모
            float t; // a→b 방향 보간 비율
            if (Mathf.Abs(denom) < EdgeIntersectEpsilon)
            {
                t = 0.5f; // 분할 평면에 거의 평행한 모서리
            }
            else
            {
                t = dA / denom; // 평면과의 교차 비율
            }

            t = Mathf.Clamp01(t); // 세그먼트 구간 안으로 클램프

            Vector3 pos = Vector3.Lerp(srcVec[a], srcVec[b], t);
            Vector2 uv = Vector2.Lerp(GetUV(srcUV, a), GetUV(srcUV, b), t);

            Vector3 na = GetNormal(srcNormal, a);
            Vector3 nb = GetNormal(srcNormal, b);
            Vector3 normal = Vector3.Lerp(na, nb, t);
            if (normal.sqrMagnitude < EdgeIntersectEpsilon * EdgeIntersectEpsilon)
            {
                normal = Vector3.up; // 길이 0 근처면 기본값
            }
            else
            {
                normal.Normalize();
            }

            return (pos, uv, normal);
        }

        //==============================================================================
        // 기능: MeshCutter::TryAppendConvexCap
        // 설명: 수집된 CutEdge 들로 평면 위 볼록 루프를 만들어 Fan 삼각분할 후 버퍼의 서브메시 1(캡)에 추가한다.
        //==============================================================================
        // TODO: 오목 단면은 ear-clipping 등으로 교체, 에지 순서 무관 입력에 대한 처리 강화
        //
        //==============================================================================
        static void TryAppendConvexCap(MeshBuffer buffer, List<CutEdge> edges, Vector3 outwardNormal)
        {
            if (edges == null || edges.Count == 0)
            {
                return;
            }

            if (!TryBuildConvexCapLoop(edges, outwardNormal, out List<Vector3> loop3d))
            {
                return;
            }

            if (loop3d.Count < 3)
            {
                return;
            }

            int baseIndex = buffer.verts.Count; // 새로 추가될 캡 정점의 시작 인덱스
            for (int i = 0; i < loop3d.Count; i++) // 폐곡선 꼭짓점
            {
                buffer.AddVertex(loop3d[i], Vector2.zero, outwardNormal.normalized); // 단면 노멀 고정·UV 생략
            }

            for (int i = 1; i < loop3d.Count - 1; i++) // 첫 번째 고정 극점 부채꼴
            {
                buffer.AddCapTriangle(baseIndex, baseIndex + i, baseIndex + i + 1);
            }
        }

        //==============================================================================
        // 기능: MeshCutter::TryBuildConvexCapLoop
        // 설명: 에지 양 끝을 웰드한 뒤 평면에 2D 투영하여 볼록 껍질을 만들고, 법선과 일관된 CCW 3D 루프를 반환한다.
        //==============================================================================
        // TODO: 공선점·격자 크기민감 WeldInv 튜닝
        //
        //==============================================================================
        static bool TryBuildConvexCapLoop(List<CutEdge> edges, Vector3 outwardNormal, out List<Vector3> loop3d)
        {
            loop3d = null;

            var weldMap = new Dictionary<Vector3Int, int>(); // 양자화 좌표 → points 인덱스
            var points = new List<Vector3>(); // 웰드된 고유 교차점 목록

            for (int e = 0; e < edges.Count; e++)
            {
                WeldPoint(weldMap, points, edges[e].A);
                WeldPoint(weldMap, points, edges[e].B);
            }

            if (points.Count < 3)
            {
                return false;
            }

            Vector3 origin = points[0]; // 2D 투영 원점으로 쓸 기준점
            Vector3 n = outwardNormal.normalized; // 단면 법선
            if (n.sqrMagnitude < EdgeIntersectEpsilon)
            {
                return false;
            }

            Vector3 t = Vector3.Cross(n, Vector3.up); // 평면 위 첫 번째 접벡터
            if (t.sqrMagnitude < 1e-6f)
            {
                t = Vector3.Cross(n, Vector3.right); // up 과 평행할 때 교체축 사용
            }

            t.Normalize();
            Vector3 b = Vector3.Cross(n, t); // t 에 직교하는 평면 내 축
            b.Normalize();

            var pts2 = new List<Vector2>(points.Count); // (t, b) 좌표로 투영한 2D 점들
            for (int i = 0; i < points.Count; i++)
            {
                Vector3 d = points[i] - origin;
                pts2.Add(new Vector2(Vector3.Dot(d, t), Vector3.Dot(d, b)));
            }

            int[] hull = ConvexHull2DIndices(pts2); // 볼록 껍질 정점 순서의 인덱스
            if (hull == null || hull.Length < 3)
            {
                return false;
            }

            var hullPts = new List<Vector3>(hull.Length); // 3D 로 복원한 꼭짓점 순환
            for (int i = 0; i < hull.Length; i++)
            {
                hullPts.Add(points[hull[i]]);
            }

            Vector3 c0 = hullPts[1] - hullPts[0]; // 면 방향 검사 벡터 1
            Vector3 c1 = hullPts[2] - hullPts[0]; // 면 방향 검사 벡터 2
            Vector3 cross = Vector3.Cross(c0, c1); // 초기 삼각형의 면 벡터
            if (Vector3.Dot(cross, n) < 0f)
            {
                hullPts.Reverse(); // 바깥 법선 n 과 맞도록 면 순서 뒤집음
            }

            loop3d = hullPts;
            return true;
        }

        //==============================================================================
        // 기능: MeshCutter::WeldPoint
        // 설명: 양자화 키로 이미 존재하는 점이면 무시하고, 없으면 points에 새 점으로 등록한다.
        //==============================================================================
        // TODO:
        //
        //==============================================================================
        static void WeldPoint(Dictionary<Vector3Int, int> weldMap, List<Vector3> points, Vector3 p)
        {
            Vector3Int k = QuantizeV(p);
            if (weldMap.ContainsKey(k))
            {
                return;
            }

            int id = points.Count;
            weldMap[k] = id;
            points.Add(p);
        }

        //==============================================================================
        // 기능: MeshCutter::QuantizeV
        // 설명: 월드/로컬 연속 좌표를 WeldInv 로 스케일한 뒤 반올림해 Vector3Int 키로 만든다.
        //==============================================================================
        // TODO:
        //
        //==============================================================================
        static Vector3Int QuantizeV(Vector3 v)
        {
            return new Vector3Int(
                Mathf.RoundToInt(v.x * WeldInv),
                Mathf.RoundToInt(v.y * WeldInv),
                Mathf.RoundToInt(v.z * WeldInv));
        }

        /// <summary> 단조 체인 볼록 껍질, 반환값은 pts2 인덱스 배열 (CCW). </summary>
        //==============================================================================
        // 기능: MeshCutter::ConvexHull2DIndices
        // 설명: 2D 점 집합에 대해 Monotone chain 으로 볼록 껍질을 구하고, 윤곽 인덱스 배열을 반환한다(점 3개 미만이면 null).
        //==============================================================================
        // TODO: 공선 점이 많을 때 최적화
        //
        //==============================================================================
        static int[] ConvexHull2DIndices(List<Vector2> pts)
        {
            int n = pts.Count;
            if (n < 3)
            {
                return null;
            }

            int[] order = new int[n]; // 정렬용 인덱스 배열
            for (int i = 0; i < n; i++)
            {
                order[i] = i;
            }

            Array.Sort(order, (i, j) =>
            {
                int cmp = pts[i].x.CompareTo(pts[j].x);
                if (cmp != 0)
                {
                    return cmp;
                }

                return pts[i].y.CompareTo(pts[j].y);
            });

            var lower = new List<int>(); // 하단 껍질 체인
            for (int i = 0; i < n; i++)
            {
                int idx = order[i];
                while (lower.Count >= 2 && Cross2(pts[lower[lower.Count - 2]], pts[lower[lower.Count - 1]], pts[idx]) <= 0f)
                {
                    lower.RemoveAt(lower.Count - 1);
                }

                lower.Add(idx);
            }

            var upper = new List<int>(); // 상단 껍질 체인
            for (int i = n - 1; i >= 0; i--)
            {
                int idx = order[i];
                while (upper.Count >= 2 && Cross2(pts[upper[upper.Count - 2]], pts[upper[upper.Count - 1]], pts[idx]) <= 0f)
                {
                    upper.RemoveAt(upper.Count - 1);
                }

                upper.Add(idx);
            }

            lower.RemoveAt(lower.Count - 1); // 중복된 양 끝 제거
            upper.RemoveAt(upper.Count - 1);
            lower.AddRange(upper);
            return lower.ToArray();
        }

        //==============================================================================
        // 기능: MeshCutter::Cross2
        // 설명: 2차원 외적으로 o에서 a, b 로의 회전 방향 부호(o-a와 o-b의 대칭 차)를 반환한다.
        //==============================================================================
        // TODO:
        //
        //==============================================================================
        static float Cross2(Vector2 o, Vector2 a, Vector2 b)
        {
            return (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
        }
    }
}
