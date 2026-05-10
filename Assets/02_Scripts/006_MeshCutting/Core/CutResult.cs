using System.Collections.Generic;
using UnityEngine;

namespace OrganicEngineer.MeshCutting
{
    //==============================================================================
    // 기능: CutPlaneRelation (열거형)
    // 설명: 소스 메시와 메시 로컬 절단 평면의 포함·교차 관계를 나타내 UI·스폰 분기에 쓴다.
    //==============================================================================
    // TODO: 경계 접촉(touch only) 상태가 필요하면 값 추가 검토
    //
    //==============================================================================
    public enum CutPlaneRelation
    {
        EmptySource, // 소스 또는 지오메트리 없음

        Straddling, // 실제 교차 분할 발생

        EntirelyAbove, // 평면이 메시 한쪽만 포함(+법선 헬프와 동일)

        EntirelyBelow // 평면이 메시를 한쪽(+반대)으로만 포함
    }

    //==============================================================================
    // 기능: CutEdge (구조체)
    // 설명: 로컬 절단면 위 하나의 선분을 두 정점으로 표현한다(QR 중복 허용·리스트 적재 가능).
    //==============================================================================
    // TODO:
    //
    //==============================================================================
    public readonly struct CutEdge
    {
        public readonly Vector3 A; // 선분 끝 A
        public readonly Vector3 B; // 선분 끝 B

        //==============================================================================
        // 기능: CutEdge::CutEdge
        // 설명: 절단면 위 선분의 양 끝점을 저장한다.
        //==============================================================================
        // TODO:
        //
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
    // 기능: CutResult (결과 클래스)
    // 설명: MeshCutter 의 출력으로 relation 과 양쪽 Mesh, 캡용 에지 리스트 및 wasCut 보조 속성을 담는다.
    //==============================================================================
    // TODO: 메시 라이프사이클(누가 Destroy 하는지)·풀링 훅 노출 검토
    //
    //==============================================================================
    public class CutResult
    {
        public CutPlaneRelation relation; // Straddling 등 관계 플래그

        public Mesh aboveMesh;  // +법선 쪽 조각(없으면 null)
        public Mesh belowMesh;   // 반대쪽 조각(없으면 null)

        // Straddling 일 때 true 를 반환하는 보조 getter
        public bool wasCut => relation == CutPlaneRelation.Straddling;

        public List<CutEdge> aboveCapEdges = new List<CutEdge>(); // Above 캡 에지 목록

        public List<CutEdge> belowCapEdges = new List<CutEdge>(); // Below 캡 에지 목록
    }
}
