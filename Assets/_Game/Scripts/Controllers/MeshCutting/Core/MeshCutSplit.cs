using System.Collections.Generic;
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

        // MeshCutSplit의 내부 함수를 옮겨놓은 클래스
        private readonly MeshCutSplitProcessor _meshCutSplitProcessor;

        // 모서리와 평면의 교차점 보간 계산기
        private readonly MeshCutEdge _meshCutEdge;

        //-------------------- 생성 함수 --------------------//

        //==============================================================================
        // 기능: MeshCutSplit::MeshCutSplit
        // 설명: 분할 보조 처리기와 모서리 보간 계산기를 외부에서 주입받는다.
        //==============================================================================
        // TODO:
        //
        //==============================================================================
        public MeshCutSplit(MeshCutSplitProcessor meshCutSplitProcessor, MeshCutEdge meshCutEdge)
        {
            _meshCutSplitProcessor = meshCutSplitProcessor;
            _meshCutEdge = meshCutEdge;
        }

        //-------------------- 기능 함수 --------------------//

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
            List<Vector3> srcVec,
            List<Vector2> srcUV,
            List<Vector3> srcNormal,
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
            // 혼자 다른 부호를 가진 정점의 위치를 찾음
            int loneIndex = _meshCutSplitProcessor.GetLoneIndex(s0, s1, s2);

            // Lone/Middle/Next 인덱스를 계산
            _meshCutSplitProcessor.GetRotatedIndices(loneIndex, i0, i1, i2, out int l, out int m, out int n);

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
            _meshCutSplitProcessor.AddLoneTriangle(loneBuffer, srcVec, srcUV, srcNormal, l, pPos, pUV, pNormal, qPos, qUV, qNormal, sourceSubMeshIndex, isCap);

            // 반대쪽 사다리꼴을 두 삼각형으로 추가
            _meshCutSplitProcessor.AddOtherQuad(otherBuffer, srcVec, srcUV, srcNormal, m, n, pPos, pUV, pNormal, qPos, qUV, qNormal, sourceSubMeshIndex, isCap);
        }


    }
}
