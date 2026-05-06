using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

//==============================================================================
// MeshCutter: 무한 평면 1장으로 메쉬를 두 조각으로 자르는 코어 알고리즘
//==============================================================================
// [Step 1 범위]
// - 입력: MeshFilter + 월드 공간 평면(Plane)
// - 출력: 평면을 기준으로 위/아래로 나뉜 두 개의 새 Mesh (CutResult)
// - 단면(cap)은 만들지 않는다 → 잘린 부분은 뚫려 보임. (Step 2에서 추가 예정)
//
// [알고리즘 개요]
// 1) 월드 평면을 메쉬 로컬 공간으로 변환한다.
// 2) 각 삼각형의 정점 3개에 대해 평면까지의 부호 거리(d)를 계산한다.
// 3) 부호 분포(8케이스)를 셋으로 줄여 분기:
//    - 모두 같은 쪽 → 그 쪽 빌더에 그대로 추가
//    - 2:1로 갈림 → 평면을 가로지르는 두 에지에서 교차점 2개 계산 후 재삼각화
//      · 한쪽: 1개 삼각형 (외톨이 정점 + 교차점 2개)
//      · 반대쪽: 사다리꼴 → 2개 삼각형
// 4) 위/아래 빌더를 각각 Mesh로 굽는다(Build).
//
// [주의 / 한계]
// - 정점 중복 제거(머지)는 하지 않는다. 반복 절단 시 정밀도 누적은 Step 6에서 다룸.
// - 노멀은 선형 보간 후 정규화 (Slerp). 충분히 매끈한 메쉬에서만 자연스럽다.
// - 멀티 서브메쉬 입력은 합쳐서 단일 서브메쉬로 출력한다(머티리얼은 Sandbox에서 임시 1개 사용).
//   서브메쉬/머티리얼 분리는 Step 2의 Capping 도입 시 [외피, 단면] 2개로 정식화한다.
//==============================================================================

/// <summary>무한 평면으로 메쉬를 두 조각으로 자르는 정적 유틸.</summary>
public static class MeshCutter
{
    //--------------------------------------------------------------------------
    // [1] Public API
    //--------------------------------------------------------------------------

    /// <summary>
    /// 메쉬를 월드 공간 평면으로 자른다.
    /// </summary>
    /// <param name="target">자를 대상의 MeshFilter (transform 기준으로 평면을 로컬 공간으로 변환).</param>
    /// <param name="worldPlane">월드 공간의 절단 평면.</param>
    /// <returns>두 조각 메쉬가 담긴 결과. 한쪽이 비면 해당 메쉬는 null.</returns>
    public static CutResult Cut(MeshFilter target, Plane worldPlane)
    {
        if (target == null) { Debug.LogWarning("[MeshCutter] target == null"); return null; }
        var src = target.sharedMesh;
        if (src == null) { Debug.LogWarning("[MeshCutter] target.sharedMesh == null"); return null; }

        // 1) 월드 평면 → 로컬 평면
        Transform t = target.transform;
        Plane localPlane = WorldToLocalPlane(worldPlane, t);

        // 2) 원본 메쉬 데이터 캐싱
        var srcVerts = src.vertices;
        var srcNormals = src.normals;
        var srcUVs = src.uv;
        bool hasNormals = srcNormals != null && srcNormals.Length == srcVerts.Length;
        bool hasUVs = srcUVs != null && srcUVs.Length == srcVerts.Length;

        var above = new SliceBuilder();
        var below = new SliceBuilder();

        // 3) 모든 서브메쉬의 모든 삼각형을 분류 + 분할
        for (int sm = 0; sm < src.subMeshCount; sm++)
        {
            int[] tris = src.GetTriangles(sm);
            for (int i = 0; i < tris.Length; i += 3)
            {
                SliceVertex v0 = MakeVertex(tris[i + 0], srcVerts, srcNormals, srcUVs, hasNormals, hasUVs);
                SliceVertex v1 = MakeVertex(tris[i + 1], srcVerts, srcNormals, srcUVs, hasNormals, hasUVs);
                SliceVertex v2 = MakeVertex(tris[i + 2], srcVerts, srcNormals, srcUVs, hasNormals, hasUVs);

                float d0 = localPlane.GetDistanceToPoint(v0.Pos);
                float d1 = localPlane.GetDistanceToPoint(v1.Pos);
                float d2 = localPlane.GetDistanceToPoint(v2.Pos);

                ClassifyAndEmit(above, below, v0, d0, v1, d1, v2, d2);
            }
        }

        // 4) 메쉬 굽기 (원본에 노멀이 없으면 새로 계산)
        string baseName = string.IsNullOrEmpty(src.name) ? "Cut" : src.name;
        Mesh aboveMesh = above.Build($"{baseName}_Above", recalcNormals: !hasNormals);
        Mesh belowMesh = below.Build($"{baseName}_Below", recalcNormals: !hasNormals);

        return new CutResult(aboveMesh, belowMesh, localPlane);
    }

    //--------------------------------------------------------------------------
    // [2] 분류 / 분할 (코어)
    //--------------------------------------------------------------------------

    /// <summary>삼각형 1개를 부호 분포에 따라 분류하고, 필요 시 분할해 빌더에 넣는다.</summary>
    private static void ClassifyAndEmit(
        SliceBuilder above, SliceBuilder below,
        SliceVertex v0, float d0,
        SliceVertex v1, float d1,
        SliceVertex v2, float d2)
    {
        // 부호 판단: d >= 0 이면 평면의 법선 쪽(=above)
        bool s0 = d0 >= 0f;
        bool s1 = d1 >= 0f;
        bool s2 = d2 >= 0f;

        int positives = (s0 ? 1 : 0) + (s1 ? 1 : 0) + (s2 ? 1 : 0);

        // [케이스 A] 전부 같은 쪽 → 그대로 한쪽에 추가
        if (positives == 3) { above.AddTriangle(v0, v1, v2); return; }
        if (positives == 0) { below.AddTriangle(v0, v1, v2); return; }

        // [케이스 B] 외톨이가 below (positives == 2)
        // 외톨이 정점이 v0/v1/v2 중 어떤 것이냐에 따라 winding을 보존하며 회전.
        if (positives == 2)
        {
            if (!s0) SplitOneSide(below, above, v0, d0, v1, d1, v2, d2);
            else if (!s1) SplitOneSide(below, above, v1, d1, v2, d2, v0, d0);
            else /* !s2 */ SplitOneSide(below, above, v2, d2, v0, d0, v1, d1);
            return;
        }

        // [케이스 C] 외톨이가 above (positives == 1)
        if (s0) SplitOneSide(above, below, v0, d0, v1, d1, v2, d2);
        else if (s1) SplitOneSide(above, below, v1, d1, v2, d2, v0, d0);
        else /* s2 */ SplitOneSide(above, below, v2, d2, v0, d0, v1, d1);
    }

    /// <summary>
    /// 외톨이 정점(lone)이 한쪽, 나머지 둘(o1, o2)이 반대쪽일 때 삼각형을 분할한다.
    /// 원본 winding 순서: lone → o1 → o2 (CCW) 라고 가정한다.
    /// </summary>
    /// <remarks>
    /// 평면이 가로지르는 에지는 [lone→o1]과 [o2→lone] 두 개이다.
    /// - loneSide(외톨이 쪽): 삼각형 1개 (lone, iLone_o1, iO2_lone)
    /// - otherSide(반대쪽):  사각형 (iLone_o1, o1, o2, iO2_lone) → 삼각형 2개
    /// </remarks>
    private static void SplitOneSide(
        SliceBuilder loneSide, SliceBuilder otherSide,
        SliceVertex lone, float dLone,
        SliceVertex o1, float d1,
        SliceVertex o2, float d2)
    {
        SliceVertex iLone_o1 = Intersect(lone, dLone, o1, d1);
        SliceVertex iO2_lone = Intersect(o2, d2, lone, dLone);

        loneSide.AddTriangle(lone, iLone_o1, iO2_lone);

        otherSide.AddTriangle(iLone_o1, o1, o2);
        otherSide.AddTriangle(iLone_o1, o2, iO2_lone);
    }

    /// <summary>두 정점을 잇는 에지가 평면을 가로지르는 위치의 정점(보간)을 만든다.</summary>
    private static SliceVertex Intersect(SliceVertex from, float dFrom, SliceVertex to, float dTo)
    {
        // dFrom + t * (dTo - dFrom) = 0  →  t = -dFrom / (dTo - dFrom)
        float denom = dTo - dFrom;
        float t = Mathf.Approximately(denom, 0f) ? 0.5f : -dFrom / denom;
        t = Mathf.Clamp01(t);

        Vector3 pos = Vector3.Lerp(from.Pos, to.Pos, t);

        // 노멀: Slerp로 매끈하게(둘 다 0이면 0). 마지막에 정규화.
        Vector3 nrm;
        if (from.Nrm.sqrMagnitude < 1e-8f && to.Nrm.sqrMagnitude < 1e-8f)
            nrm = Vector3.zero;
        else
            nrm = Vector3.Slerp(from.Nrm, to.Nrm, t).normalized;

        Vector2 uv = Vector2.Lerp(from.Uv, to.Uv, t);
        return new SliceVertex(pos, nrm, uv);
    }

    //--------------------------------------------------------------------------
    // [3] 좌표 변환 / 데이터 헬퍼
    //--------------------------------------------------------------------------

    /// <summary>월드 평면을 임의의 transform 로컬 공간 평면으로 변환한다.</summary>
    private static Plane WorldToLocalPlane(Plane worldPlane, Transform t)
    {
        // 월드 평면 위의 한 점: 원점에서 법선 반대로 distance만큼.
        // (Plane의 정의: dot(n, p) + d = 0  →  p = -d * n)
        Vector3 worldPoint = worldPlane.normal * -worldPlane.distance;
        Vector3 localPoint = t.InverseTransformPoint(worldPoint);
        Vector3 localNormal = t.InverseTransformDirection(worldPlane.normal).normalized;
        return new Plane(localNormal, localPoint);
    }

    private static SliceVertex MakeVertex(int idx, Vector3[] verts, Vector3[] normals, Vector2[] uvs, bool hasN, bool hasUV)
    {
        return new SliceVertex(
            verts[idx],
            hasN ? normals[idx] : Vector3.zero,
            hasUV ? uvs[idx] : Vector2.zero);
    }

    //--------------------------------------------------------------------------
    // [4] 내부 타입
    //--------------------------------------------------------------------------

    /// <summary>절단 과정에서 다루는 정점 단위(위치/노멀/UV).</summary>
    private readonly struct SliceVertex
    {
        public readonly Vector3 Pos;
        public readonly Vector3 Nrm;
        public readonly Vector2 Uv;

        public SliceVertex(Vector3 pos, Vector3 nrm, Vector2 uv)
        {
            Pos = pos;
            Nrm = nrm;
            Uv = uv;
        }
    }

    /// <summary>
    /// 절단 결과 메쉬를 누적해서 만드는 빌더.
    /// Step 1에서는 정점 머지/dedup을 하지 않는다(반복 절단 안정화는 Step 6에서).
    /// </summary>
    private sealed class SliceBuilder
    {
        private readonly List<Vector3> _verts = new List<Vector3>(256);
        private readonly List<Vector3> _normals = new List<Vector3>(256);
        private readonly List<Vector2> _uvs = new List<Vector2>(256);
        private readonly List<int> _triangles = new List<int>(512);

        public bool IsEmpty => _verts.Count == 0;

        public void AddTriangle(SliceVertex a, SliceVertex b, SliceVertex c)
        {
            int ia = AddVertex(a);
            int ib = AddVertex(b);
            int ic = AddVertex(c);
            _triangles.Add(ia);
            _triangles.Add(ib);
            _triangles.Add(ic);
        }

        private int AddVertex(SliceVertex v)
        {
            int idx = _verts.Count;
            _verts.Add(v.Pos);
            _normals.Add(v.Nrm);
            _uvs.Add(v.Uv);
            return idx;
        }

        public Mesh Build(string name, bool recalcNormals)
        {
            if (_verts.Count == 0 || _triangles.Count == 0) return null;

            var mesh = new Mesh { name = name };

            // 정점 65k 초과 시 32비트 인덱스로 강제 (Step 6 이후 큰 메쉬 대비)
            if (_verts.Count > 65000)
                mesh.indexFormat = IndexFormat.UInt32;

            mesh.SetVertices(_verts);
            mesh.SetUVs(0, _uvs);
            mesh.SetTriangles(_triangles, 0);

            if (recalcNormals)
                mesh.RecalculateNormals();
            else
                mesh.SetNormals(_normals);

            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
