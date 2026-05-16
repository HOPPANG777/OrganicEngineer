using UnityEngine;

namespace OrganicEngineer.MeshCutting
{
    //==============================================================================
    // 기능: MeshCutSplitProcessor
    // 설명: MeshCutSplitProcessor에서 사용하는 내부함수를 모두 옮겨놓은 클래스
    //==============================================================================
    // TODO: 
    //
    //==============================================================================
    class MeshCutSplitProcessor
    {
        //-------------------- 맴버 변수 --------------------//

        // 모서리와 평면의 교차점 보간 계산기
        private MeshCutEdge _meshCutEdge = new MeshCutEdge();

        //-------------------- 기능 함수 --------------------//

        // Lone 정점 위치를 반환
        public int GetLoneIndex
        (
            bool s0,
            bool s1,
            bool s2
        )
        {
            // i0 만 다른 부호인지 검사
            if (s0 != s1 && s0 != s2) return 0;

            // i1 만 다른 부호인지 검사
            if (s1 != s0 && s1 != s2) return 1;

            // 나머지는 i2 가 Lone 정점
            return 2;
        }

        // Lone 기준으로 L/M/N 인덱스를 회전 배치
        public void GetRotatedIndices
        (
            int loneIndex,
            int i0,
            int i1,
            int i2,
            out int l,
            out int m,
            out int n
        )
        {
            // i0 이 Lone 인 경우
            if (loneIndex == 0)
            {
                l = i0;
                m = i1;
                n = i2;
                return;
            }

            // i1 이 Lone 인 경우
            if (loneIndex == 1)
            {
                l = i1;
                m = i2;
                n = i0;
                return;
            }

            // i2 가 Lone 인 경우
            l = i2;
            m = i0;
            n = i1;
        }

        // Lone 쪽 삼각형을 추가
        public void AddLoneTriangle
        (
            MeshBuffer buffer,
            Vector3[] srcVec,
            Vector2[] srcUV,
            Vector3[] srcNormal,
            int l,
            Vector3 pPos,
            Vector2 pUV,
            Vector3 pNormal,
            Vector3 qPos,
            Vector2 qUV,
            Vector3 qNormal,
            bool isCap
        )
        {
            // 원본 Lone 정점 추가
            int li = buffer.AddVertex(srcVec[l], _meshCutEdge.GetUV(srcUV, l), _meshCutEdge.GetNormal(srcNormal, l));

            // 교차점 P 추가
            int lp = buffer.AddVertex(pPos, pUV, pNormal);

            // 교차점 Q 추가
            int lq = buffer.AddVertex(qPos, qUV, qNormal);

            // Lone 쪽 삼각형 등록
            buffer.AddTriangle(li, lp, lq, isCap);
        }

        // 반대쪽 사다리꼴을 두 삼각형으로 추가
        public void AddOtherQuad
        (
            MeshBuffer buffer,
            Vector3[] srcVec,
            Vector2[] srcUV,
            Vector3[] srcNormal,
            int m,
            int n,
            Vector3 pPos,
            Vector2 pUV,
            Vector3 pNormal,
            Vector3 qPos,
            Vector2 qUV,
            Vector3 qNormal,
            bool isCap
        )
        {
            // 교차점 P 추가
            int op = buffer.AddVertex(pPos, pUV, pNormal);

            // 원본 M 정점 추가
            int om = buffer.AddVertex(srcVec[m], _meshCutEdge.GetUV(srcUV, m), _meshCutEdge.GetNormal(srcNormal, m));

            // 원본 N 정점 추가
            int on = buffer.AddVertex(srcVec[n], _meshCutEdge.GetUV(srcUV, n), _meshCutEdge.GetNormal(srcNormal, n));

            // 교차점 Q 추가
            int oq = buffer.AddVertex(qPos, qUV, qNormal);

            // 사다리꼴 앞쪽 삼각형 등록
            buffer.AddTriangle(op, om, on, isCap);

            // 사다리꼴 뒤쪽 삼각형 등록
            buffer.AddTriangle(op, on, oq, isCap);
        }
    }

}