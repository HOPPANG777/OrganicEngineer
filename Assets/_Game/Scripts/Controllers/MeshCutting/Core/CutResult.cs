using System.Collections.Generic;
using UnityEngine;

namespace OrganicEngineer.MeshCutting
{
    //==============================================================================
    // 기능: CutPlaneRelation
    // 설명: 단일 절단 평면과 소스 Mesh 의 상대 위치 및 실제 분할 여부를 나타낸다.
    //==============================================================================
    // TODO: 평면에 닿기만 하고 분할되지 않는 Touch 상태가 필요하면 별도 enum 값을 추가한다.
    //
    //==============================================================================
    public enum CutPlaneRelation
    {
        // 소스 Mesh 가 없거나, 처리할 삼각형이 하나도 없는 상태
        EMPTY_SOURCE,

        // 평면이 Mesh 를 실제로 가로질러 Above/Below 조각이 모두 생긴 상태
        STRADDLING,

        // Mesh 전체가 평면 법선의 + 방향에만 있는 상태
        ENTIRELY_ABOVE,

        // Mesh 전체가 평면 법선의 반대 방향에만 있는 상태
        ENTIRELY_BELOW
    }

    //==============================================================================
    // 기능: CutEdge
    // 설명: 절단 평면 위에 생긴 하나의 단면 선분을 Mesh 로컬 좌표로 보관한다.
    //==============================================================================
    public readonly struct CutEdge
    {
        // 절단 선분의 첫 번째 끝점
        public Vector3 A { get; }

        // 절단 선분의 두 번째 끝점
        public Vector3 B { get; }

        //==============================================================================
        // 기능: CutEdge::CutEdge
        // 설명: 단면 캡을 만들 때 사용할 절단 선분의 양 끝점을 저장한다.
        //==============================================================================
        // a: 절단 선분의 첫 번째 끝점
        // b: 절단 선분의 두 번째 끝점
        public CutEdge(Vector3 a, Vector3 b)
        {
            A = a;
            B = b;
        }
    }

    //==============================================================================
    // 기능: CutResult
    // 설명: 단일 평면 절단 결과 Mesh, 포함 관계, 캡 생성용 절단 에지를 함께 보관한다.
    //==============================================================================
    // TODO: 런타임 생성 Mesh 소유권과 Destroy 책임을 별도 타입으로 더 명확히 분리한다.
    //
    //==============================================================================
    public sealed class CutResult
    {
        // 절단 평면과 소스 Mesh 의 최종 관계
        public CutPlaneRelation Relation { get; internal set; }

        // 평면 법선 + 방향에 남은 결과 Mesh. 없으면 null 이다.
        public Mesh AboveMesh { get; internal set; }

        // 평면 법선 반대 방향에 남은 결과 Mesh. 없으면 null 이다.
        public Mesh BelowMesh { get; internal set; }

        // 실제 절단으로 두 조각이 만들어졌는지 확인하는 편의 속성
        public bool WasCut => Relation == CutPlaneRelation.STRADDLING;

        // AboveMesh 의 단면 캡을 만들 때 사용하는 절단 에지 목록
        internal List<CutEdge> AboveCapEdges { get; } = new List<CutEdge>();

        // BelowMesh 의 단면 캡을 만들 때 사용하는 절단 에지 목록
        internal List<CutEdge> BelowCapEdges { get; } = new List<CutEdge>();
    }
}
