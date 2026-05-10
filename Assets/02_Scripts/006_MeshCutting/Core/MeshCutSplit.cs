using UnityEngine;

namespace OrganicEngineer.MeshCutting
{
    //==============================================================================
    // 기능: MeshCutSplit
    // 설명: 평면에 걸친 삼각형 하나를 양쪽 MeshBuffer 로 분할한다.
    //==============================================================================
    // TODO: 공면 정점/퇴화 삼각형 처리 강화
    //
    //==============================================================================
    class MeshCutSplit
    {
        //-------------------- 맴버 변수 --------------------//

        // 모서리와 평면의 교차점 보간 계산기
        private MeshCutEdge _meshCutEdge = new MeshCutEdge();

        //==============================================================================
        // 기능: MeshCutSplit::Split
        // 설명: Lone 정점 기준으로 삼각형을 작은 삼각형 1개와 사다리꼴 2개 삼각형으로 나눈다.
        //==============================================================================
        // TODO: 와인딩 검증용 디버그 옵션
        //
        //==============================================================================
        public void Split
        (
            MeshBuffer above,
            MeshBuffer below,
            Plane plane,
            Vector3[] srcVec,
            Vector2[] srcUV,
            Vector3[] srcNormal,
            int i0,
            int i1,
            int i2,
            bool s0,
            bool s1,
            bool s2,
            bool[] side,
            CutResult cutResult
        )
        {
            // 혼자 다른 부호를 가진 정점의 위치를 찾음
            int loneIndex = GetLoneIndex(s0, s1, s2);

            // Lone/Middle/Next 인덱스를 계산
            GetRotatedIndices(loneIndex, i0, i1, i2, out int l, out int m, out int n);

            // Lone 정점이 평면 +쪽인지 확인
            bool loneSide = side[l];

            // L-M 모서리와 평면의 교차점 P 계산
            var (pPos, pUV, pNormal) = _meshCutEdge.Lerp(plane, srcVec, srcUV, srcNormal, l, m);

            // L-N 모서리와 평면의 교차점 Q 계산
            var (qPos, qUV, qNormal) = _meshCutEdge.Lerp(plane, srcVec, srcUV, srcNormal, l, n);

            // 양쪽 캡 생성에 사용할 절단선 저장
            cutResult.aboveCapEdges.Add(new CutEdge(pPos, qPos));
            cutResult.belowCapEdges.Add(new CutEdge(pPos, qPos));

            // Lone 쪽 버퍼와 반대쪽 버퍼 선택
            MeshBuffer loneBuffer = loneSide ? above : below;
            MeshBuffer otherBuffer = loneSide ? below : above;

            // Lone 쪽 작은 삼각형 추가
            AddLoneTriangle(loneBuffer, srcVec, srcUV, srcNormal, l, pPos, pUV, pNormal, qPos, qUV, qNormal);

            // 반대쪽 사다리꼴을 두 삼각형으로 추가
            AddOtherQuad(otherBuffer, srcVec, srcUV, srcNormal, m, n, pPos, pUV, pNormal, qPos, qUV, qNormal);
        }

        // Lone 정점 위치를 반환
        private int GetLoneIndex(bool s0, bool s1, bool s2)
        {
            // i0 만 다른 부호인지 검사
            if (s0 != s1 && s0 != s2) return 0;

            // i1 만 다른 부호인지 검사
            if (s1 != s0 && s1 != s2) return 1;

            // 나머지는 i2 가 Lone 정점
            return 2;
        }

        // Lone 기준으로 L/M/N 인덱스를 회전 배치
        private void GetRotatedIndices(int loneIndex, int i0, int i1, int i2, out int l, out int m, out int n)
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
        private void AddLoneTriangle(MeshBuffer buffer, Vector3[] srcVec, Vector2[] srcUV, Vector3[] srcNormal, int l, Vector3 pPos, Vector2 pUV, Vector3 pNormal, Vector3 qPos, Vector2 qUV, Vector3 qNormal)
        {
            // 원본 Lone 정점 추가
            int li = buffer.AddVertex(srcVec[l], _meshCutEdge.GetUV(srcUV, l), _meshCutEdge.GetNormal(srcNormal, l));

            // 교차점 P 추가
            int lp = buffer.AddVertex(pPos, pUV, pNormal);

            // 교차점 Q 추가
            int lq = buffer.AddVertex(qPos, qUV, qNormal);

            // Lone 쪽 삼각형 등록
            buffer.AddTriangle(li, lp, lq);
        }

        // 반대쪽 사다리꼴을 두 삼각형으로 추가
        private void AddOtherQuad(MeshBuffer buffer, Vector3[] srcVec, Vector2[] srcUV, Vector3[] srcNormal, int m, int n, Vector3 pPos, Vector2 pUV, Vector3 pNormal, Vector3 qPos, Vector2 qUV, Vector3 qNormal)
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
            buffer.AddTriangle(op, om, on);

            // 사다리꼴 뒤쪽 삼각형 등록
            buffer.AddTriangle(op, on, oq);
        }
    }
}
