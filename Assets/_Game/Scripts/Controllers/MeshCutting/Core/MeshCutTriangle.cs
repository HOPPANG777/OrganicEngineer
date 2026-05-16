using UnityEngine;

namespace OrganicEngineer.MeshCutting
{
    //==============================================================================
    // 기능: MeshCutTriangle
    // 설명: 절단되지 않은 삼각형 복사와 기본 정점 채널 조회를 담당한다.
    //==============================================================================
    // TODO: SplitTriangle 전체 이관
    //
    //==============================================================================
    class MeshCutTriangle
    {
        //==============================================================================
        // 기능: MeshCutTriangle::Copy
        // 설명: 원본 삼각형 정점 3개를 대상 MeshBuffer 에 그대로 추가한다.
        //==============================================================================
        // TODO: 추가 버텍스 채널 복사
        //
        //==============================================================================
        public void Copy
        (
            MeshBuffer buffer,
            Vector3[] srcVec,
            Vector2[] srcUV,
            Vector3[] srcNormal,
            int i0,
            int i1,
            int i2,
            bool isCap
        )
        {
            // 원본 정점 3개를 새 버퍼 정점으로 추가
            int a = buffer.AddVertex(srcVec[i0], GetUV(srcUV, i0), GetNormal(srcNormal, i0));
            int b = buffer.AddVertex(srcVec[i1], GetUV(srcUV, i1), GetNormal(srcNormal, i1));
            int c = buffer.AddVertex(srcVec[i2], GetUV(srcUV, i2), GetNormal(srcNormal, i2));

            // 원본과 같은 순서로 삼각형 추가
            buffer.AddTriangle(a, b, c, isCap);
        }

        // UV 배열이 없거나 범위 밖이면 기본값 반환
        private Vector2 GetUV(Vector2[] uvs, int i)
        {
            if (uvs == null || i < 0 || i >= uvs.Length)
            {
                return Vector2.zero;
            }

            return uvs[i];
        }

        // 노멀 배열이 없거나 범위 밖이면 기본값 반환
        private Vector3 GetNormal(Vector3[] normals, int i)
        {
            if (normals == null || i < 0 || i >= normals.Length)
            {
                return Vector3.up;
            }

            return normals[i];
        }
    }
}
