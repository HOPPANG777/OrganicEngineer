using Sirenix.OdinInspector;
using UnityEngine;

//==============================================================================
// MeshCutSandboxController: Sandbox_MeshCut 씬에서 평면 절단을 검증하는 컨트롤러
//==============================================================================
// [Step 0 + Step 1 범위]
// - Cutter 트랜스폼의 위치 + 위(up) 방향으로 무한 평면을 정의한다.
// - 인스펙터의 Cut 버튼을 누르면 Target 메쉬를 잘라 두 새 GameObject를 만든다.
// - 잘린 두 조각은 평면 법선 방향으로 살짝 띄워서 분리감을 시각화한다.
// - Reset 버튼으로 잘린 조각 제거 + 원본 복구.
// - Step 1에서는 단면 캡핑이 없으므로 자른 조각의 단면이 뚫려 보인다(의도적).
//
// [씬 세팅 가이드 — 코드 외 작업]
// 1) Hierarchy:
//    Sandbox_MeshCut (Empty)
//    ├─ Camera + Directional Light (기본)
//    ├─ Target (Cube)                ← MeshFilter / MeshRenderer / Collider
//    ├─ Cutter (Empty)               ← position = Target 가까이, 회전으로 평면 각도 조절
//    └─ SandboxRoot (Empty + 본 컴포넌트)
//         · Target  : Target의 MeshFilter
//         · Cutter  : Cutter의 Transform
//         · PieceMaterial : 비워두면 원본 머티리얼 재사용
// 2) Play Mode 진입 후 Inspector의 Cut 버튼 클릭.
// 3) Cutter를 회전/이동하면 OnDrawGizmos로 평면이 시각화된다(Edit Mode에서도).
//==============================================================================

/// <summary>샌드박스 씬용 평면 절단 데모 컨트롤러.</summary>
public class MeshCutSandboxController : SerializedMonoBehaviour
{
    //--------------------------------------------------------------------------
    // [1] 참조
    //--------------------------------------------------------------------------

    [TabGroup("Refs")]
    [Tooltip("자를 대상 메쉬의 MeshFilter.")]
    [Required]
    [SerializeField] private MeshFilter _target;

    [TabGroup("Refs")]
    [Tooltip("절단 평면을 정의하는 트랜스폼. position = 평면 위 한 점, up(Y) = 평면 법선.")]
    [Required]
    [SerializeField] private Transform _cutter;

    [TabGroup("Refs")]
    [Tooltip("자른 조각에 적용할 머티리얼. null이면 원본의 sharedMaterial을 재사용.")]
    [SerializeField] private Material _pieceMaterial;

    //--------------------------------------------------------------------------
    // [2] 옵션
    //--------------------------------------------------------------------------

    [TabGroup("Settings")]
    [Tooltip("자른 후 두 조각을 평면 법선 방향으로 띄울 거리.")]
    [SerializeField, Range(0f, 1f)] private float _separation = 0.05f;

    [TabGroup("Settings")]
    [Tooltip("자른 조각에 Rigidbody를 자동으로 붙인다. 기본은 Kinematic(시각 확인용).")]
    [SerializeField] private bool _addRigidbody = true;

    [TabGroup("Settings")]
    [Tooltip("Rigidbody.useGravity 값. 자른 직후 자유낙하 보고 싶으면 켠다.")]
    [SerializeField] private bool _useGravity = false;

    [TabGroup("Settings")]
    [Tooltip("Rigidbody.isKinematic 값. 켜두면 충돌/물리 영향 없음(시각 확인용).")]
    [SerializeField] private bool _isKinematic = true;

    [TabGroup("Settings")]
    [Tooltip("기즈모로 절단 평면을 시각화한다.")]
    [SerializeField] private bool _drawGizmo = true;

    [TabGroup("Settings")]
    [Tooltip("기즈모 평면의 한 변 길이.")]
    [SerializeField, Range(0.1f, 5f)] private float _gizmoSize = 2f;

    //--------------------------------------------------------------------------
    // [3] 런타임 상태
    //--------------------------------------------------------------------------

    private GameObject _aboveGO;
    private GameObject _belowGO;

    //--------------------------------------------------------------------------
    // [4] 액션 (Inspector 버튼)
    //--------------------------------------------------------------------------

    [TabGroup("Actions")]
    [Button(ButtonSizes.Large), GUIColor(0.6f, 1f, 0.6f)]
    [PropertySpace(8)]
    private void Cut()
    {
        if (!ValidateRefs()) return;

        // 이전 절단 결과가 남아있으면 먼저 정리
        ClearPieces();

        Plane worldPlane = new Plane(_cutter.up, _cutter.position);

        CutResult result = MeshCutter.Cut(_target, worldPlane);
        if (result == null)
        {
            Debug.LogWarning("[MeshCutSandbox] Cut 실패 — null 결과.");
            return;
        }

        if (result.AboveMesh != null)
            _aboveGO = SpawnPiece(result.AboveMesh, worldPlane.normal * _separation, "Above");
        if (result.BelowMesh != null)
            _belowGO = SpawnPiece(result.BelowMesh, -worldPlane.normal * _separation, "Below");

        if (!result.IsValid)
            Debug.Log($"[MeshCutSandbox] 한쪽만 생성됨 (above={result.AboveMesh != null}, below={result.BelowMesh != null}). 평면이 메쉬 바깥일 수 있음.");

        _target.gameObject.SetActive(false);
    }

    [TabGroup("Actions")]
    [Button(ButtonSizes.Large), GUIColor(1f, 0.85f, 0.7f)]
    private void ResetCut()
    {
        ClearPieces();
        if (_target != null) _target.gameObject.SetActive(true);
    }

    //--------------------------------------------------------------------------
    // [5] 내부 유틸
    //--------------------------------------------------------------------------

    private bool ValidateRefs()
    {
        if (_target == null) { Debug.LogWarning("[MeshCutSandbox] Target 비어있음."); return false; }
        if (_cutter == null) { Debug.LogWarning("[MeshCutSandbox] Cutter 비어있음."); return false; }
        if (_target.sharedMesh == null) { Debug.LogWarning("[MeshCutSandbox] Target.sharedMesh 비어있음."); return false; }
        return true;
    }

    private void ClearPieces()
    {
        DestroySafe(ref _aboveGO);
        DestroySafe(ref _belowGO);
    }

    private void DestroySafe(ref GameObject go)
    {
        if (go == null) return;
        if (Application.isPlaying) Destroy(go);
        else DestroyImmediate(go);
        go = null;
    }

    /// <summary>잘린 메쉬 1개를 받아서 조각 GameObject를 만든다.</summary>
    private GameObject SpawnPiece(Mesh mesh, Vector3 worldOffset, string suffix)
    {
        var go = new GameObject($"{_target.name}_{suffix}");
        Transform srcT = _target.transform;

        // 원본과 같은 회전/스케일을 유지한 채, offset만큼 평행 이동.
        go.transform.SetPositionAndRotation(srcT.position + worldOffset, srcT.rotation);
        go.transform.localScale = srcT.localScale;
        go.transform.SetParent(transform, worldPositionStays: true);

        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        var mr = go.AddComponent<MeshRenderer>();
        var srcMR = _target.GetComponent<MeshRenderer>();
        if (_pieceMaterial != null)
            mr.sharedMaterial = _pieceMaterial;
        else if (srcMR != null && srcMR.sharedMaterial != null)
            mr.sharedMaterial = srcMR.sharedMaterial;

        // 단면이 뚫린 메쉬라 convex 사용 불가. Step 2 Capping 후 convex 옵션 검토.
        var mc = go.AddComponent<MeshCollider>();
        mc.sharedMesh = mesh;
        mc.convex = false;

        if (_addRigidbody)
        {
            var rb = go.AddComponent<Rigidbody>();
            rb.useGravity = _useGravity;
            rb.isKinematic = _isKinematic;
        }

        return go;
    }

    //--------------------------------------------------------------------------
    // [6] 기즈모
    //--------------------------------------------------------------------------

    private void OnDrawGizmos()
    {
        if (!_drawGizmo || _cutter == null) return;

        Matrix4x4 prevMatrix = Gizmos.matrix;
        Color prevColor = Gizmos.color;

        // Cutter 로컬 공간에서 X-Z 평면이 절단 평면 (법선 = local Y = cutter.up)
        Gizmos.matrix = _cutter.localToWorldMatrix;

        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        Gizmos.DrawCube(Vector3.zero, new Vector3(_gizmoSize, 0.001f, _gizmoSize));

        Gizmos.color = new Color(0f, 1f, 1f, 0.9f);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(_gizmoSize, 0.001f, _gizmoSize));

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(Vector3.zero, Vector3.up * (_gizmoSize * 0.25f));

        Gizmos.matrix = prevMatrix;
        Gizmos.color = prevColor;
    }
}
