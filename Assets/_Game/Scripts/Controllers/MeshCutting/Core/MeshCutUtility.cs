using System.Collections.Generic;
using UnityEngine;

namespace OrganicEngineer.MeshCutting
{
    //==============================================================================
    // 기능: MeshCutUtility
    // 설명: 절단 코어의 삼각형 분류, 정점 보간, 결과 판정을 모은 정적 유틸리티이다.
    //==============================================================================
    // TODO: 탄젠트/컬러 등 추가 버텍스 채널 보간
    //
    //==============================================================================
    static class MeshCutUtility
    {
        //-------------------- 멤버 변수 --------------------//

        // 선분-평면 교차 허용 오차
        private const float _edgeIntersectEpsilon = 1e-5f;

        //-------------------- 기능 함수 --------------------//

        //==============================================================================
        // 기능: MeshCutUtility::BuildSideTable
        // 설명: 원본 정점마다 절단 평면의 +쪽인지 여부를 계산해 재사용 리스트에 기록한다.
        //==============================================================================
        public static void BuildSideTable(List<Vector3> srcVerts, Plane planeLocal, List<bool> side)
        {
            // 이전 분류 결과 초기화
            side.Clear();

            // 모든 정점의 평면 기준 방향 저장
            for (int i = 0; i < srcVerts.Count; i++)
            {
                side.Add(planeLocal.GetSide(srcVerts[i]));
            }
        }

        //==============================================================================
        // 기능: MeshCutUtility::ProcessTriangles
        // 설명: 한 subMesh 의 삼각형을 순회하며 한쪽 복사 또는 평면 분할을 수행한다.
        //==============================================================================
        public static bool ProcessTriangles
        (
            List<Vector3> srcVerts,
            List<Vector2> srcUVs,
            List<Vector3> srcNormals,
            List<int> srcTris,
            List<bool> side,
            Plane planeLocal,
            MeshBuffer above,
            MeshBuffer below,
            CutResult result,
            int sourceSubMeshIndex,
            bool isCap,
            BoundedCutPlane boundedPlaneLocal,
            bool useBoundedPlane
        )
        {
            // 실제 절단 발생 여부
            bool straddling = false;

            // 외피 삼각형이면 분류 후 복사를 대비해 subMesh 인덱스 버퍼 Capacity 를 미리 확보
            if (!isCap)
            {
                above.EnsureTriangleCapacity(sourceSubMeshIndex, srcTris.Count);
                below.EnsureTriangleCapacity(sourceSubMeshIndex, srcTris.Count);
            }

            // 삼각형 인덱스는 3개씩 한 면을 구성한다.
            for (int i = 0; i < srcTris.Count; i += 3)
            {
                int i0 = srcTris[i + 0];
                int i1 = srcTris[i + 1];
                int i2 = srcTris[i + 2];

                bool s0 = side[i0];
                bool s1 = side[i1];
                bool s2 = side[i2];

                straddling |= ProcessTriangle(
                    srcVerts,
                    srcUVs,
                    srcNormals,
                    side,
                    planeLocal,
                    above,
                    below,
                    result,
                    i0,
                    i1,
                    i2,
                    s0,
                    s1,
                    s2,
                    sourceSubMeshIndex,
                    isCap,
                    boundedPlaneLocal,
                    useBoundedPlane
                );
            }

            return straddling;
        }

        //==============================================================================
        // 기능: MeshCutUtility::ResolveRelation
        // 설명: Above/Below 버퍼 존재 여부로 절단 평면과 메시의 관계를 판정한다.
        //==============================================================================
        public static CutPlaneRelation ResolveRelation(bool hasAbove, bool hasBelow)
        {
            // 양쪽 모두 비어 있으면 소스 메시가 비어 있는 상태
            if (!hasAbove && !hasBelow) return CutPlaneRelation.EMPTY_SOURCE;

            // Above 만 있으면 메시 전체가 평면 +쪽
            if (hasAbove && !hasBelow) return CutPlaneRelation.ENTIRELY_ABOVE;

            // Below 만 있으면 메시 전체가 평면 반대쪽
            if (!hasAbove && hasBelow) return CutPlaneRelation.ENTIRELY_BELOW;

            // 양쪽 모두 있으면 실제 교차 또는 경계 분할로 간주
            return CutPlaneRelation.STRADDLING;
        }

        //==============================================================================
        // 기능: MeshCutUtility::GetUV
        // 설명: UV 배열이 없거나 인덱스가 범위 밖이면 Vector2.zero 를 반환한다.
        //==============================================================================
        public static Vector2 GetUV(List<Vector2> uvs, int index)
        {
            if (uvs == null || index < 0 || index >= uvs.Count)
            {
                return Vector2.zero;
            }

            return uvs[index];
        }

        //==============================================================================
        // 기능: MeshCutUtility::GetNormal
        // 설명: 노멀 배열이 없거나 인덱스가 범위 밖이면 Vector3.up 을 반환한다.
        //==============================================================================
        public static Vector3 GetNormal(List<Vector3> normals, int index)
        {
            if (normals == null || index < 0 || index >= normals.Count)
            {
                return Vector3.up;
            }

            return normals[index];
        }

        //==============================================================================
        // 기능: MeshCutUtility::ProcessTriangle
        // 설명: 삼각형 하나를 평면 기준으로 복사하거나 두 버퍼로 분할한다.
        //==============================================================================
        private static bool ProcessTriangle
        (
            List<Vector3> srcVerts,
            List<Vector2> srcUVs,
            List<Vector3> srcNormals,
            List<bool> side,
            Plane planeLocal,
            MeshBuffer above,
            MeshBuffer below,
            CutResult result,
            int i0,
            int i1,
            int i2,
            bool s0,
            bool s1,
            bool s2,
            int sourceSubMeshIndex,
            bool isCap,
            BoundedCutPlane boundedPlaneLocal,
            bool useBoundedPlane
        )
        {
            if (useBoundedPlane
                && !boundedPlaneLocal.TriangleOverlapsSpan(srcVerts[i0], srcVerts[i1], srcVerts[i2]))
            {
                CopyTriangle(below, srcVerts, srcUVs, srcNormals, i0, i1, i2, sourceSubMeshIndex, isCap);
                return false;
            }

            // 세 정점이 모두 Above 면 그대로 Above 버퍼에 복사
            if (s0 && s1 && s2)
            {
                CopyTriangle(above, srcVerts, srcUVs, srcNormals, i0, i1, i2, sourceSubMeshIndex, isCap);
                return false;
            }

            // 세 정점이 모두 Below 면 그대로 Below 버퍼에 복사
            if (!s0 && !s1 && !s2)
            {
                CopyTriangle(below, srcVerts, srcUVs, srcNormals, i0, i1, i2, sourceSubMeshIndex, isCap);
                return false;
            }

            // 평면을 가로지르는 삼각형은 절단선과 새 면으로 분할
            return SplitTriangle(above, below, planeLocal, srcVerts, srcUVs, srcNormals, i0, i1, i2, s0, s1, s2, side, result, sourceSubMeshIndex, isCap, boundedPlaneLocal, useBoundedPlane);
        }

        //==============================================================================
        // 기능: MeshCutUtility::CopyTriangle
        // 설명: 절단되지 않은 원본 삼각형 정점 3개를 대상 MeshBuffer 에 그대로 추가한다.
        //==============================================================================
        private static void CopyTriangle
        (
            MeshBuffer buffer,
            List<Vector3> srcVerts,
            List<Vector2> srcUVs,
            List<Vector3> srcNormals,
            int i0,
            int i1,
            int i2,
            int sourceSubMeshIndex,
            bool isCap
        )
        {
            // 원본 정점 3개를 새 버퍼 정점으로 추가
            int a = buffer.AddVertex(srcVerts[i0], GetUV(srcUVs, i0), GetNormal(srcNormals, i0));
            int b = buffer.AddVertex(srcVerts[i1], GetUV(srcUVs, i1), GetNormal(srcNormals, i1));
            int c = buffer.AddVertex(srcVerts[i2], GetUV(srcUVs, i2), GetNormal(srcNormals, i2));

            // 원본과 같은 순서로 삼각형 추가
            buffer.AddTriangle(a, b, c, sourceSubMeshIndex, isCap);
        }

        //==============================================================================
        // 기능: MeshCutUtility::SplitTriangle
        // 설명: Lone 정점 기준으로 삼각형을 작은 삼각형 1개와 사다리꼴 2개 삼각형으로 나눈다.
        //==============================================================================
        private static bool SplitTriangle
        (
            MeshBuffer above,
            MeshBuffer below,
            Plane plane,
            List<Vector3> srcVerts,
            List<Vector2> srcUVs,
            List<Vector3> srcNormals,
            int i0,
            int i1,
            int i2,
            bool s0,
            bool s1,
            bool s2,
            List<bool> side,
            CutResult cutResult,
            int sourceSubMeshIndex,
            bool isCap,
            BoundedCutPlane boundedPlaneLocal,
            bool useBoundedPlane
        )
        {
            // 혼자 다른 부호를 가진 정점의 위치를 찾음
            int loneIndex = GetLoneIndex(s0, s1, s2);

            // Lone/Middle/Next 인덱스를 계산
            GetRotatedIndices(loneIndex, i0, i1, i2, out int lone, out int middle, out int next);

            // Lone 정점이 평면 +쪽인지 확인
            bool loneSide = side[lone];

            // Lone 에서 다른 두 정점으로 가는 모서리와 평면의 교차점 계산
            var (pPos, pUV, pNormal) = LerpEdge(plane, srcVerts, srcUVs, srcNormals, lone, middle);
            var (qPos, qUV, qNormal) = LerpEdge(plane, srcVerts, srcUVs, srcNormals, lone, next);

            if (useBoundedPlane && !boundedPlaneLocal.CutEdgeTouchesSegmentBand(pPos, qPos))
            {
                CopyTriangle(below, srcVerts, srcUVs, srcNormals, i0, i1, i2, sourceSubMeshIndex, isCap);
                return false;
            }

            // 양쪽 캡 생성에 사용할 절단선 저장
            cutResult.AboveCapEdges.Add(new CutEdge(pPos, qPos));
            cutResult.BelowCapEdges.Add(new CutEdge(pPos, qPos));

            // Lone 쪽 버퍼와 반대쪽 버퍼 선택
            MeshBuffer loneBuffer = loneSide ? above : below;
            MeshBuffer otherBuffer = loneSide ? below : above;

            // Lone 쪽 작은 삼각형 추가
            AddLoneTriangle(loneBuffer, srcVerts, srcUVs, srcNormals, lone, pPos, pUV, pNormal, qPos, qUV, qNormal, sourceSubMeshIndex, isCap);

            // 반대쪽 사다리꼴을 두 삼각형으로 추가
            AddOtherQuad(otherBuffer, srcVerts, srcUVs, srcNormals, middle, next, pPos, pUV, pNormal, qPos, qUV, qNormal, sourceSubMeshIndex, isCap);
            return true;
        }

        //==============================================================================
        // 기능: MeshCutUtility::LerpEdge
        // 설명: 두 정점 a,b 를 잇는 모서리와 평면 교점의 위치/UV/노멀을 보간한다.
        //==============================================================================
        // TODO: 분모가 0에 가까운 경우 진단 로그 옵션
        //
        //==============================================================================
        private static (Vector3 pos, Vector2 uv, Vector3 normal) LerpEdge
        (
            Plane plane,
            List<Vector3> srcVerts,
            List<Vector2> srcUVs,
            List<Vector3> srcNormals,
            int a,
            int b
        )
        {
            // 각 끝점에서 평면까지 signed 거리
            float distanceA = plane.GetDistanceToPoint(srcVerts[a]);
            float distanceB = plane.GetDistanceToPoint(srcVerts[b]);

            // 보간 비율 계산
            float denominator = distanceA - distanceB;
            float t = Mathf.Abs(denominator) < _edgeIntersectEpsilon ? 0.5f : distanceA / denominator;
            t = Mathf.Clamp01(t);

            // 위치와 UV 보간
            Vector3 pos = Vector3.Lerp(srcVerts[a], srcVerts[b], t);
            Vector2 uv = Vector2.Lerp(GetUV(srcUVs, a), GetUV(srcUVs, b), t);

            // 노멀 보간 후 정규화
            Vector3 normal = Vector3.Lerp(GetNormal(srcNormals, a), GetNormal(srcNormals, b), t);
            if (normal.sqrMagnitude < _edgeIntersectEpsilon * _edgeIntersectEpsilon)
            {
                normal = Vector3.up;
            }
            else
            {
                normal.Normalize();
            }

            return (pos, uv, normal);
        }

        //==============================================================================
        // 기능: MeshCutUtility::AddLoneTriangle
        // 설명: 평면 한쪽에 혼자 남은 정점과 교차점 두 개로 작은 삼각형을 추가한다.
        //==============================================================================
        private static void AddLoneTriangle
        (
            MeshBuffer buffer,
            List<Vector3> srcVerts,
            List<Vector2> srcUVs,
            List<Vector3> srcNormals,
            int lone,
            Vector3 pPos,
            Vector2 pUV,
            Vector3 pNormal,
            Vector3 qPos,
            Vector2 qUV,
            Vector3 qNormal,
            int sourceSubMeshIndex,
            bool isCap
        )
        {
            // 원본 Lone 정점 추가
            int loneVertex = buffer.AddVertex(srcVerts[lone], GetUV(srcUVs, lone), GetNormal(srcNormals, lone));

            // 교차점 P/Q 추가
            int pVertex = buffer.AddVertex(pPos, pUV, pNormal);
            int qVertex = buffer.AddVertex(qPos, qUV, qNormal);

            // Lone 쪽 삼각형 등록
            buffer.AddTriangle(loneVertex, pVertex, qVertex, sourceSubMeshIndex, isCap);
        }

        //==============================================================================
        // 기능: MeshCutUtility::AddOtherQuad
        // 설명: 평면 반대쪽에 남은 사다리꼴을 두 개의 삼각형으로 추가한다.
        //==============================================================================
        private static void AddOtherQuad
        (
            MeshBuffer buffer,
            List<Vector3> srcVerts,
            List<Vector2> srcUVs,
            List<Vector3> srcNormals,
            int middle,
            int next,
            Vector3 pPos,
            Vector2 pUV,
            Vector3 pNormal,
            Vector3 qPos,
            Vector2 qUV,
            Vector3 qNormal,
            int sourceSubMeshIndex,
            bool isCap
        )
        {
            // 교차점 P, 원본 M/N, 교차점 Q 순서로 정점 추가
            int pVertex = buffer.AddVertex(pPos, pUV, pNormal);
            int middleVertex = buffer.AddVertex(srcVerts[middle], GetUV(srcUVs, middle), GetNormal(srcNormals, middle));
            int nextVertex = buffer.AddVertex(srcVerts[next], GetUV(srcUVs, next), GetNormal(srcNormals, next));
            int qVertex = buffer.AddVertex(qPos, qUV, qNormal);

            // 사다리꼴 앞쪽 삼각형 등록
            buffer.AddTriangle(pVertex, middleVertex, nextVertex, sourceSubMeshIndex, isCap);

            // 사다리꼴 뒤쪽 삼각형 등록
            buffer.AddTriangle(pVertex, nextVertex, qVertex, sourceSubMeshIndex, isCap);
        }

        //==============================================================================
        // 기능: MeshCutUtility::GetLoneIndex
        // 설명: 세 정점 중 다른 두 정점과 다른 평면 부호를 가진 정점 위치를 반환한다.
        //==============================================================================
        private static int GetLoneIndex(bool s0, bool s1, bool s2)
        {
            // i0 만 다른 부호인지 검사
            if (s0 != s1 && s0 != s2) return 0;

            // i1 만 다른 부호인지 검사
            if (s1 != s0 && s1 != s2) return 1;

            // 나머지는 i2 가 Lone 정점
            return 2;
        }

        //==============================================================================
        // 기능: MeshCutUtility::GetRotatedIndices
        // 설명: Lone 기준으로 L/M/N 인덱스를 회전 배치한다.
        //==============================================================================
        private static void GetRotatedIndices(int loneIndex, int i0, int i1, int i2, out int lone, out int middle, out int next)
        {
            // i0 이 Lone 인 경우
            if (loneIndex == 0)
            {
                lone = i0;
                middle = i1;
                next = i2;
                return;
            }

            // i1 이 Lone 인 경우
            if (loneIndex == 1)
            {
                lone = i1;
                middle = i2;
                next = i0;
                return;
            }

            // i2 가 Lone 인 경우
            lone = i2;
            middle = i0;
            next = i1;
        }

    }
}
