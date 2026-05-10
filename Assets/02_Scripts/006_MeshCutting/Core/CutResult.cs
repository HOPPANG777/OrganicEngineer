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
    /// <summary> 소스 메시(로컬 평면 기준)와 절단 평면의 관계. 스폰/UI는 이 값으로 분기. </summary>
    public enum CutPlaneRelation
    {
        /// <summary> source 가 null 이거나 삼각형·정점 없음. </summary>
        EmptySource, // 소스 또는 지오메트리 없음

        /// <summary> 삼각형 최소 1개가 평면에 걸림. 실제 2조각 분할 + 캡 서브메시. </summary>
        Straddling, // 실제 교차 분할 발생

        /// <summary> 걸친 삼각형 없음, 전부 Plane.normal +쪽. </summary>
        EntirelyAbove, // 평면이 메시 한쪽만 포함(+법선 헬프와 동일)

        /// <summary> 걸친 삼각형 없음, 전부 반대쪽. </summary>
        EntirelyBelow // 평면이 메시를 한쪽(+반대)으로만 포함
    }

    //==============================================================================
    // 기능: CutEdge (구조체)
    // 설명: 로컬 절단면 위 하나의 선분을 두 정점으로 표현한다(QR 중복 허용·리스트 적재 가능).
    //==============================================================================
    // TODO:
    //
    //==============================================================================
    /// <summary> Step 2: 로컬 공간 절단면 위 한 변. 동일 PQ가 양쪽 리스트에 반복될 수 있음. </summary>
    public readonly struct CutEdge
    {
        public readonly Vector3 A; // 선분 끝 A
        public readonly Vector3 B; // 선분 끝 B

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
        /// <summary> 평면과 메시의 관계. </summary>
        public CutPlaneRelation relation; // Straddling 등 관계 플래그

        public Mesh aboveMesh;  // +법선 쪽 조각(없으면 null)
        public Mesh belowMesh;   // 반대쪽 조각(없으면 null)

        /// <summary> Straddling 일 때 true. (옵션 B: relation 으로 먼저 분기 권장) </summary>
        public bool wasCut => relation == CutPlaneRelation.Straddling;

        /// <summary> +쪽 조각 단면 캡 Triangulation 입력(로컬). Straddling 에만 의미 있음. </summary>
        public List<CutEdge> aboveCapEdges = new List<CutEdge>(); // Above 캡 에지 목록

        /// <summary> -쪽 조각 단면 캡용. </summary>
        public List<CutEdge> belowCapEdges = new List<CutEdge>(); // Below 캡 에지 목록
    }
}
