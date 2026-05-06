using UnityEngine;

//==============================================================================
// CutResult: MeshCutter.Cut의 결과 컨테이너
//==============================================================================
// - 잘려서 생긴 두 메쉬(평면 위/아래)를 담고,
//   호출자가 어떤 GameObject로 만들지를 결정한다.
// - Step 1 단계: 단면(cap)은 만들지 않으므로 두 메쉬 모두 단면이 뚫려있다.
//   Step 2에서 Capping을 추가하면서 SubMesh 1번에 단면을 넣을 예정.
//==============================================================================

/// <summary>
/// MeshCutter.Cut의 결과. 두 조각의 메쉬 + 사용된 절단 평면(로컬 공간)을 보관.
/// </summary>
public class CutResult
{
    //--------------------------------------------------------------------------
    // [1] 결과 데이터
    //--------------------------------------------------------------------------

    /// <summary>평면 법선 방향(+) 위쪽 조각의 메쉬. 모든 삼각형이 평면 아래에 있으면 null.</summary>
    public Mesh AboveMesh { get; }

    /// <summary>평면 법선 방향(-) 아래쪽 조각의 메쉬. 모든 삼각형이 평면 위에 있으면 null.</summary>
    public Mesh BelowMesh { get; }

    /// <summary>실제로 절단에 사용된 평면(원본 메쉬의 로컬 공간 기준).</summary>
    public Plane LocalPlane { get; }

    //--------------------------------------------------------------------------
    // [2] 생성/유틸
    //--------------------------------------------------------------------------

    public CutResult(Mesh aboveMesh, Mesh belowMesh, Plane localPlane)
    {
        AboveMesh = aboveMesh;
        BelowMesh = belowMesh;
        LocalPlane = localPlane;
    }

    /// <summary>실제로 두 조각이 다 만들어진 "유효한 절단"인지 여부.</summary>
    public bool IsValid => AboveMesh != null && BelowMesh != null;
}
