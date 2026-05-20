using System.Collections.Generic;
using UnityEngine;

namespace OrganicEngineer.MeshCutting
{
    //==============================================================================
    // 기능: MeshCutUtility
    // 설명: 삼각형 분류, 평면 교차점 보간, 결과 관계 판정을 담당하는 절단 보조 함수 모음이다.
    //==============================================================================
    // TODO: 색상, 탄젠트, 본 가중치 등 추가 버텍스 채널 보간을 지원한다.
    //
    //==============================================================================
    static class MeshCutUtility
    {
        //-------------------- 멤버 변수 --------------------//

        // 선분과 평면이 거의 평행할 때 보간 분모를 판정하는 허용 오차
        private const float _edgeIntersectEpsilon = 1e-5f;

        //-------------------- 기능 함수 --------------------//

        //==============================================================================
        // 기능: MeshCutUtility::BuildSideTable
        // 설명: 모든 소스 정점이 절단 평면의 + 방향에 있는지 미리 계산한다.
        //==============================================================================
        // srcVerts: 소스 Mesh 정점 위치 목록
        // planeLocal: 소스 Mesh 로컬 좌표 기준 절단 평면
        // side: 정점별 평면 방향 결과를 저장할 재사용 리스트
        public static void BuildSideTable(List<Vector3> srcVerts, Plane planeLocal, List<bool> side)
        {
            side.Clear();

            // 삼각형마다 Plane.GetSide 를 반복 호출하지 않도록 정점 단위로 한 번만 계산한다.
            for (int i = 0; i < srcVerts.Count; i++)
            {
                side.Add(planeLocal.GetSide(srcVerts[i]));
            }
        }

        //==============================================================================
        // 기능: MeshCutUtility::ProcessTriangles
        // 설명: 한 subMesh 의 삼각형 인덱스를 순회하며 복사 또는 평면 분할을 수행한다.
        //==============================================================================
        // srcVerts: 소스 Mesh 정점 위치 목록
        // srcUVs: 소스 Mesh UV0 목록
        // srcNormals: 소스 Mesh 노멀 목록
        // srcTris: 현재 subMesh 의 삼각형 인덱스 목록
        // side: 정점별 평면 + 방향 여부
        // planeLocal: 절단 평면
        // above: 평면 + 방향 결과 버퍼
        // below: 평면 반대 방향 결과 버퍼
        // result: 캡 생성을 위한 절단 에지를 누적할 결과 객체
        // sourceSubMeshIndex: 현재 처리 중인 원본 subMesh 인덱스
        // isCap: 현재 subMesh 를 기존 캡으로 취급할지 여부
        // boundedPlaneLocal: 제한 절단에서 사용할 드래그 선분 정보
        // useBoundedPlane: true 이면 드래그 선분 길이 범위 밖 삼각형을 자르지 않는다.
        // 반환: 실제 평면 교차 분할이 한 번이라도 발생했으면 true
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
            bool straddling = false;

            // 외피 삼각형은 원본 subMesh 슬롯을 보존하므로 subMesh 별 버퍼를 미리 확보한다.
            if (!isCap)
            {
                above.EnsureTriangleCapacity(sourceSubMeshIndex, srcTris.Count);
                below.EnsureTriangleCapacity(sourceSubMeshIndex, srcTris.Count);
            }

            // Unity Mesh 삼각형 인덱스는 3개가 한 면을 이루므로 3칸씩 이동한다.
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
        // 설명: Above/Below 결과 버퍼 존재 여부만으로 절단 평면과 Mesh 의 관계를 판정한다.
        //==============================================================================
        // hasAbove: 평면 + 방향 결과 삼각형 존재 여부
        // hasBelow: 평면 반대 방향 결과 삼각형 존재 여부
        // 반환: Mesh 와 평면의 포함 또는 교차 관계
        public static CutPlaneRelation ResolveRelation(bool hasAbove, bool hasBelow)
        {
            if (!hasAbove && !hasBelow) return CutPlaneRelation.EMPTY_SOURCE;
            if (hasAbove && !hasBelow) return CutPlaneRelation.ENTIRELY_ABOVE;
            if (!hasAbove && hasBelow) return CutPlaneRelation.ENTIRELY_BELOW;

            return CutPlaneRelation.STRADDLING;
        }

        //==============================================================================
        // 기능: MeshCutUtility::GetUV
        // 설명: UV 목록이 없거나 인덱스가 범위를 벗어나면 안전한 기본 UV 를 반환한다.
        //==============================================================================
        // uvs: 소스 UV 목록
        // index: 조회할 정점 인덱스
        // 반환: 유효한 UV 또는 Vector2.zero
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
        // 설명: 노멀 목록이 없거나 인덱스가 범위를 벗어나면 안전한 기본 노멀을 반환한다.
        //==============================================================================
        // normals: 소스 노멀 목록
        // index: 조회할 정점 인덱스
        // 반환: 유효한 노멀 또는 Vector3.up
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
        // 설명: 삼각형 하나를 평면 기준으로 복사하거나 교차 분할한다.
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
                // 드래그 선분 길이 범위 밖이면 절단 대상이 아니므로 원본 쪽(Below)에 그대로 둔다.
                CopyTriangle(below, srcVerts, srcUVs, srcNormals, i0, i1, i2, sourceSubMeshIndex, isCap);
                return false;
            }

            if (s0 && s1 && s2)
            {
                // 세 정점이 모두 평면 + 방향이면 삼각형은 Above 로 통째로 복사된다.
                CopyTriangle(above, srcVerts, srcUVs, srcNormals, i0, i1, i2, sourceSubMeshIndex, isCap);
                return false;
            }

            if (!s0 && !s1 && !s2)
            {
                // 세 정점이 모두 평면 반대 방향이면 삼각형은 Below 로 통째로 복사된다.
                CopyTriangle(below, srcVerts, srcUVs, srcNormals, i0, i1, i2, sourceSubMeshIndex, isCap);
                return false;
            }

            // 정점이 양쪽에 나뉘어 있으면 평면이 삼각형을 가로지르므로 새 정점을 보간해 분할한다.
            return SplitTriangle(above, below, planeLocal, srcVerts, srcUVs, srcNormals, i0, i1, i2, s0, s1, s2, side, result, sourceSubMeshIndex, isCap);
        }

        //==============================================================================
        // 기능: MeshCutUtility::CopyTriangle
        // 설명: 절단되지 않은 원본 삼각형을 지정한 결과 버퍼에 복사한다.
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
            // 결과 Mesh 는 소스 정점을 공유하지 않고 필요한 채널 값을 새 정점으로 복사한다.
            int a = buffer.AddVertex(srcVerts[i0], GetUV(srcUVs, i0), GetNormal(srcNormals, i0));
            int b = buffer.AddVertex(srcVerts[i1], GetUV(srcUVs, i1), GetNormal(srcNormals, i1));
            int c = buffer.AddVertex(srcVerts[i2], GetUV(srcUVs, i2), GetNormal(srcNormals, i2));

            // 와인딩 순서를 원본과 동일하게 유지해 외피 노멀 방향을 보존한다.
            buffer.AddTriangle(a, b, c, sourceSubMeshIndex, isCap);
        }

        //==============================================================================
        // 기능: MeshCutUtility::SplitTriangle
        // 설명: 한쪽에 홀로 있는 정점을 기준으로 삼각형을 작은 삼각형 1개와 사각형 1개로 나눈다.
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
            bool isCap
        )
        {
            // 세 정점 중 다른 두 정점과 다른 평면 쪽에 있는 정점을 lone 으로 잡는다.
            int loneIndex = GetLoneIndex(s0, s1, s2);

            // lone 을 시작점으로 놓고 나머지 두 점을 middle/next 로 회전 배치한다.
            GetRotatedIndices(loneIndex, i0, i1, i2, out int lone, out int middle, out int next);
            bool loneSide = side[lone];

            // lone 에서 반대편 두 정점으로 가는 두 모서리 위에 절단 평면 교차점이 생긴다.
            var (pPos, pUV, pNormal) = LerpEdge(plane, srcVerts, srcUVs, srcNormals, lone, middle);
            var (qPos, qUV, qNormal) = LerpEdge(plane, srcVerts, srcUVs, srcNormals, lone, next);

            // 두 교차점을 잇는 선분은 나중에 단면 캡 루프를 구성하는 경계 에지가 된다.
            cutResult.AboveCapEdges.Add(new CutEdge(pPos, qPos));
            cutResult.BelowCapEdges.Add(new CutEdge(pPos, qPos));

            MeshBuffer loneBuffer = loneSide ? above : below;
            MeshBuffer otherBuffer = loneSide ? below : above;

            // lone 쪽은 원래 삼각형의 한 꼭짓점과 두 교차점으로 작은 삼각형이 된다.
            AddLoneTriangle(loneBuffer, srcVerts, srcUVs, srcNormals, lone, pPos, pUV, pNormal, qPos, qUV, qNormal, sourceSubMeshIndex, isCap);

            // 반대쪽은 교차점 두 개와 원본 정점 두 개로 이루어진 사각형이므로 삼각형 2개로 나눈다.
            AddOtherQuad(otherBuffer, srcVerts, srcUVs, srcNormals, middle, next, pPos, pUV, pNormal, qPos, qUV, qNormal, sourceSubMeshIndex, isCap);
            return true;
        }

        //==============================================================================
        // 기능: MeshCutUtility::LerpEdge
        // 설명: 두 정점 사이 모서리와 평면의 교차점 위치, UV, 노멀을 보간한다.
        //==============================================================================
        // TODO: 분모가 0에 가까운 경우 진단 로그 옵션을 추가한다.
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
            // signed distance 값이 서로 다른 부호를 가지므로 그 비율로 교차 위치를 찾는다.
            float distanceA = plane.GetDistanceToPoint(srcVerts[a]);
            float distanceB = plane.GetDistanceToPoint(srcVerts[b]);

            float denominator = distanceA - distanceB;
            float t = Mathf.Abs(denominator) < _edgeIntersectEpsilon ? 0.5f : distanceA / denominator;
            t = Mathf.Clamp01(t);

            Vector3 pos = Vector3.Lerp(srcVerts[a], srcVerts[b], t);
            Vector2 uv = Vector2.Lerp(GetUV(srcUVs, a), GetUV(srcUVs, b), t);

            // 노멀은 선형 보간 뒤 정규화한다. 입력 노멀이 없거나 0에 가까우면 안전한 기본값을 쓴다.
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
        // 설명: lone 쪽에 남는 작은 삼각형을 결과 버퍼에 추가한다.
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
            int loneVertex = buffer.AddVertex(srcVerts[lone], GetUV(srcUVs, lone), GetNormal(srcNormals, lone));
            int pVertex = buffer.AddVertex(pPos, pUV, pNormal);
            int qVertex = buffer.AddVertex(qPos, qUV, qNormal);

            buffer.AddTriangle(loneVertex, pVertex, qVertex, sourceSubMeshIndex, isCap);
        }

        //==============================================================================
        // 기능: MeshCutUtility::AddOtherQuad
        // 설명: lone 반대쪽에 남는 사각형을 두 개의 삼각형으로 쪼개 결과 버퍼에 추가한다.
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
            int pVertex = buffer.AddVertex(pPos, pUV, pNormal);
            int middleVertex = buffer.AddVertex(srcVerts[middle], GetUV(srcUVs, middle), GetNormal(srcNormals, middle));
            int nextVertex = buffer.AddVertex(srcVerts[next], GetUV(srcUVs, next), GetNormal(srcNormals, next));
            int qVertex = buffer.AddVertex(qPos, qUV, qNormal);

            // 원본 와인딩에 맞춰 사각형을 p-middle-next 와 p-next-q 두 삼각형으로 구성한다.
            buffer.AddTriangle(pVertex, middleVertex, nextVertex, sourceSubMeshIndex, isCap);
            buffer.AddTriangle(pVertex, nextVertex, qVertex, sourceSubMeshIndex, isCap);
        }

        //==============================================================================
        // 기능: MeshCutUtility::GetLoneIndex
        // 설명: 세 정점 중 다른 두 정점과 평면 방향이 다른 정점의 로컬 순서 인덱스를 찾는다.
        //==============================================================================
        private static int GetLoneIndex(bool s0, bool s1, bool s2)
        {
            if (s0 != s1 && s0 != s2) return 0;
            if (s1 != s0 && s1 != s2) return 1;

            return 2;
        }

        //==============================================================================
        // 기능: MeshCutUtility::GetRotatedIndices
        // 설명: lone 정점을 기준으로 원본 삼각형 인덱스를 lone, middle, next 순서로 회전 배치한다.
        //==============================================================================
        private static void GetRotatedIndices(int loneIndex, int i0, int i1, int i2, out int lone, out int middle, out int next)
        {
            if (loneIndex == 0)
            {
                lone = i0;
                middle = i1;
                next = i2;
                return;
            }

            if (loneIndex == 1)
            {
                lone = i1;
                middle = i2;
                next = i0;
                return;
            }

            lone = i2;
            middle = i0;
            next = i1;
        }
    }
}
