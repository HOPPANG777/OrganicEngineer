using System.Collections.Generic;
using UnityEngine;

namespace OrganicEngineer.MeshCutting
{
    //==============================================================================
    // 기능: CutPlaneRelation
    // 설명: 소스 메시와 메시 로컬 절단 평면의 포함·교차 관계를 나타내 UI·스폰 분기에 쓴다.
    //==============================================================================
    // TODO: 경계 접촉(touch only) 상태가 필요하면 값 추가 검토
    //
    //==============================================================================
    public enum CutPlaneRelation
    {
        EMPTY_SOURCE, // 소스 또는 지오메트리 없음

        STRADDLING, // 실제 교차 분할 발생

        ENTIRELY_ABOVE, // 평면이 메시 한쪽만 포함(+법선 헬프와 동일)

        ENTIRELY_BELOW // 평면이 메시를 한쪽(+반대)으로만 포함
    }

    //==============================================================================
    // 기능: CutEdge
    // 설명: 로컬 절단면 위 하나의 선분을 두 정점으로 표현한다.
    //==============================================================================
    // TODO:
    //
    //==============================================================================
    public readonly struct CutEdge
    {
        // 선분 끝 A
        public Vector3 A { get; }

        // 선분 끝 B
        public Vector3 B { get; }

        //==============================================================================
        // 기능: CutEdge::CutEdge
        // 설명: 절단면 위 선분의 양 끝점을 저장한다.
        //==============================================================================
        // a: 선분 시작점
        // b: 선분 끝점
        public CutEdge(Vector3 a, Vector3 b)
        {
            A = a;
            B = b;
        }
    }

    //==============================================================================
    // 기능: CutResult
    // 설명: MeshCutter 의 출력으로 관계값, 양쪽 Mesh, 캡용 에지 리스트와 절단 여부를 담는다.
    //==============================================================================
    // TODO: 메시 라이프사이클(누가 Destroy 하는지)·풀링 훅 노출 검토
    //
    //==============================================================================
    public sealed class CutResult
    {
        // 절단 평면과 소스 메시의 관계 플래그
        public CutPlaneRelation Relation { get; internal set; }

        // +법선 쪽 조각(없으면 null)
        public Mesh AboveMesh { get; internal set; }

        // 반대쪽 조각(없으면 null)
        public Mesh BelowMesh { get; internal set; }

        // STRADDLING 일 때 true 를 반환하는 보조 getter
        public bool WasCut => Relation == CutPlaneRelation.STRADDLING;

        // Above 캡 에지 목록
        internal List<CutEdge> AboveCapEdges { get; } = new List<CutEdge>();

        // Below 캡 에지 목록
        internal List<CutEdge> BelowCapEdges { get; } = new List<CutEdge>();
    }
}
