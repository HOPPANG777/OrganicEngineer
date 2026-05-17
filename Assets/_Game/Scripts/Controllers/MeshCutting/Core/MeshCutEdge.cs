using System.Collections.Generic;
using UnityEngine;

namespace OrganicEngineer.MeshCutting
{
    //==============================================================================
    // 기능: MeshCutEdge
    // 설명: 절단 평면과 메시 모서리의 교차점 보간 계산을 담당한다.
    //==============================================================================
    // TODO: 탄젠트/컬러 등 추가 버텍스 채널 보간
    //
    //==============================================================================
    class MeshCutEdge
    {
        //-------------------- 맴버 변수 --------------------//

        // 선분-평면 교차 허용 오차
        private const float _edgeIntersectEpsilon = 1e-5f;

        //==============================================================================
        // 기능: MeshCutEdge::Lerp
        // 설명: 두 정점 a,b 를 잇는 모서리와 평면 교점의 위치/UV/노멀을 보간한다.
        //==============================================================================
        // TODO: 분모가 0에 가까운 경우 진단 로그 옵션
        //
        //==============================================================================
        public (Vector3 pos, Vector2 uv, Vector3 normal) Lerp
        (
            Plane plane,
            List<Vector3> srcVec,
            List<Vector2> srcUV,
            List<Vector3> srcNormal,
            int a,
            int b
        )
        {
            // 각 끝점에서 평면까지 signed 거리
            float dA = plane.GetDistanceToPoint(srcVec[a]);
            float dB = plane.GetDistanceToPoint(srcVec[b]);

            // 보간 비율 계산
            float denom = dA - dB;
            float t = Mathf.Abs(denom) < _edgeIntersectEpsilon ? 0.5f : dA / denom;
            t = Mathf.Clamp01(t);

            // 위치와 UV 보간
            Vector3 pos = Vector3.Lerp(srcVec[a], srcVec[b], t);
            Vector2 uv = Vector2.Lerp(GetUV(srcUV, a), GetUV(srcUV, b), t);

            // 노멀 보간 후 정규화
            Vector3 normal = Vector3.Lerp(GetNormal(srcNormal, a), GetNormal(srcNormal, b), t);
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
        // 기능: MeshCutEdge::GetUV
        // 설명: UV 배열이 없거나 인덱스가 범위 밖이면 Vector2.zero 를 반환한다.
        //==============================================================================
        // TODO:
        //
        //==============================================================================
        public Vector2 GetUV(List<Vector2> uvs, int i)
        {
            if (uvs == null || i < 0 || i >= uvs.Count)
            {
                return Vector2.zero;
            }

            return uvs[i];
        }

        //==============================================================================
        // 기능: MeshCutEdge::GetNormal
        // 설명: 노멀 배열이 없거나 인덱스가 범위 밖이면 Vector3.up 을 반환한다.
        //==============================================================================
        // TODO:
        //
        //==============================================================================
        public Vector3 GetNormal(List<Vector3> normals, int i)
        {
            if (normals == null || i < 0 || i >= normals.Count)
            {
                return Vector3.up;
            }

            return normals[i];
        }
    }
}
