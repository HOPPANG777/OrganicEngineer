using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Serialization;

namespace OrganicEngineer.MeshCutting.Sandbox
{
    //==============================================================================
    // 기능: MeshCutSandboxController (MonoBehaviour)
    // 설명: 인스펙터에서 타겟과 평면(Transform cutter)으로 멤버 절단 기능을 호출해 조각 GameObject를 스폰하고, Reset 으로 정리한다.
    //==============================================================================
    // TODO: _originalMaterials 활용 또는 제거 결정, 콜라이더 재생성·레이어 복사
    //
    //==============================================================================
    public class MeshCutSandboxController : MonoBehaviour
    {
        //-------------------- 멤버 변수 --------------------//

        // 메시 절단 코어 기능
        private MeshCutter _meshCutter;

        // 박스/프리즘 같은 볼록 볼륨 절단 기능
        private ConvexVolumeMeshCutter _volumeCutter;

        // 월드 평면을 로컬 평면으로 변환하는 기능
        private CutPlaneConvert _cutPlaneConvert;

        [Title("References")]
        // 원본 메시
        [Tooltip("절단 대상 MeshFilter")]
        [FormerlySerializedAs("target")]
        [SerializeField] private MeshFilter _target;

        // 절개 평면
        [Tooltip("절단 평면. 위치=평면 위 점, up=법선(Above 쪽)")]
        [FormerlySerializedAs("cutter")]
        [SerializeField] private Transform _cutter;

        // 조각 재료
        [Tooltip("외피(서브메시 0) 머티리얼. 비우면 Target과 동일")]
        [FormerlySerializedAs("pieceMaterial")]
        [SerializeField] private Material _pieceMaterial;

        // 단면 재료
        [Tooltip("단면(마지막 서브메시) 머티리얼. 비우면 pieceMaterial과 동일")]
        [FormerlySerializedAs("capMaterial")]
        [SerializeField] private Material _capMaterial;

        // 조각 생성 컴포넌트
        [Tooltip("잘린 조각 GameObject 생성과 목록 관리를 담당")]
        [FormerlySerializedAs("pieceSpawn")]
        [SerializeField] private MeshCutPieceSpawn _pieceSpawn;

        // 원본 메시
        private Mesh _originalMesh;
        // 원본 메시 재료
        private Material[] _originalMaterials;
        // 절개 상태
        private bool _isCut;

        // 스폰 직전 연결 섬 분리에 재사용하는 임시 Mesh 목록
        private readonly List<Mesh> _meshIslandScratch = new List<Mesh>();

        // 드래그 컷 등에서 레이 대상 검증 시 사용한다(원본 앵커).
        public MeshFilter CutTargetMeshFilter => _target;

        // 한 번이라도 Cut 이 성공하면 true(Reset 전까지).
        public bool HasCutCompleted => _isCut;

        // 스폰된 조각 수(진단·HUD).
        public int SpawnedPieceCount => _pieceSpawn != null ? _pieceSpawn.SpawnedCount : 0;

        //-------------------- 생명주기 함수 --------------------//

        //==============================================================================
        // 기능: MeshCutSandboxController::Awake
        // 설명: 샌드박스 절단 기능에 필요한 순수 C# 의존성을 한 곳에서 조립한다.
        //==============================================================================
        private void Awake()
        {
            // 외부 테스트에서 이미 주입한 경우 해당 인스턴스를 유지한다.
            if (_meshCutter != null && _volumeCutter != null && _cutPlaneConvert != null) return;

            Construct(BuildMeshCutter(), new CutPlaneConvert());
        }

        //-------------------- 생성 함수 --------------------//

        //==============================================================================
        // 기능: MeshCutSandboxController::Construct
        // 설명: 테스트나 상위 조립 지점에서 MeshCutting 의존성을 주입한다.
        //==============================================================================
        public void Construct(MeshCutter meshCutter, CutPlaneConvert cutPlaneConvert)
        {
            Construct(meshCutter, new ConvexVolumeMeshCutter(meshCutter), cutPlaneConvert);
        }

        //==============================================================================
        // 기능: MeshCutSandboxController::Construct
        // 설명: 평면 커터, 볼륨 커터, 좌표 변환 기능을 외부에서 주입받는다.
        //==============================================================================
        public void Construct(MeshCutter meshCutter, ConvexVolumeMeshCutter volumeCutter, CutPlaneConvert cutPlaneConvert)
        {
            _meshCutter = meshCutter;
            _volumeCutter = volumeCutter;
            _cutPlaneConvert = cutPlaneConvert;
        }

        // Raycast 피판이 현재 자를 수 있는 MeshFilter(원본 활성 또는 스폰 조각)일 때만 true.
        public bool IsRayHitOnCutTarget(in RaycastHit hit)
        {
            return TryResolveCutMeshFilterFromHit(hit, out _);
        }

        //==============================================================================
        // 기능: MeshCutSandboxController::TryResolveCutMeshFilterFromHit
        // 설명: 히트가 절단 가능한 표면이면 해당 MeshFilter 를 반환한다.
        //==============================================================================
        public bool TryResolveCutMeshFilterFromHit(in RaycastHit hit, out MeshFilter cutSource)
        {
            cutSource = null;
            if (hit.collider == null) return false;
            MeshFilter owner = hit.collider.GetComponentInParent<MeshFilter>();
            if (!IsValidCutMeshFilter(owner)) return false;
            cutSource = owner;
            return true;
        }

        private bool IsValidCutMeshFilter(MeshFilter mf)
        {
            if (mf == null || mf.sharedMesh == null) return false;
            if (_target != null && mf == _target)
                return _target.gameObject.activeInHierarchy;
            return EnsurePieceSpawn().IsSpawnedMeshFilter(mf);
        }

        //==============================================================================
        // 기능: MeshCutSandboxController::TryCutFromWorldPlane
        // 설명: 활성 원본만 대상으로 하는 편의 오버로드(첫 컷·테스트용).
        //==============================================================================
        public bool TryCutFromWorldPlane(Vector3 worldNormal, Vector3 worldPointOnPlane)
        {
            EnsureCore();
            if (_target == null || !_target.gameObject.activeInHierarchy) return false;
            return TryCutFromWorldPlane(worldNormal, worldPointOnPlane, _target);
        }

        //==============================================================================
        // 기능: MeshCutSandboxController::TryCutFromWorldPlane
        // 설명: 월드 법선(+쪽)·평면 위 점으로 규칙 C 등에서 만든 평면을 cutSource 로컬로 바꿔 Cut 한다.
        // Straddling 이고 조각까지 스폰되면 true.
        //==============================================================================
        public bool TryCutFromWorldPlane(Vector3 worldNormal, Vector3 worldPointOnPlane, MeshFilter cutSource)
        {
            EnsureCore();
            if (cutSource == null || cutSource.sharedMesh == null) return false;
            if (!IsValidCutMeshFilter(cutSource)) return false;
            if (worldNormal.sqrMagnitude < 1e-14f)
            {
                Debug.LogWarning("[MeshCut] 드래그 컷 스킵: 법선 길이 0.");
                return false;
            }

            worldNormal.Normalize();
            _originalMesh = cutSource.sharedMesh;
            Plane localPlane = _cutPlaneConvert.WorldToLocal(cutSource, worldNormal, worldPointOnPlane);
            return TryCutInternal(localPlane, cutSource);
        }

        public bool TryCutFromWorldBoundedPlane
        (
            Vector3 worldNormal,
            Vector3 worldSegmentStart,
            Vector3 worldSegmentEnd,
            float worldSpanPadding,
            float worldBandPadding,
            MeshFilter cutSource
        )
        {
            EnsureCore();
            if (cutSource == null || cutSource.sharedMesh == null) return false;
            if (!IsValidCutMeshFilter(cutSource)) return false;
            if (worldNormal.sqrMagnitude < 1e-14f)
            {
                Debug.LogWarning("[MeshCut] Bounded cut skipped: normal length is zero.");
                return false;
            }

            worldNormal.Normalize();
            _originalMesh = cutSource.sharedMesh;
            BoundedCutPlane localPlane = _cutPlaneConvert.WorldToLocalBounded(
                cutSource,
                worldNormal,
                worldSegmentStart,
                worldSegmentEnd,
                worldSpanPadding,
                worldBandPadding
            );

            return TryCutInternal(localPlane, cutSource);
        }

        //==============================================================================
        // 기능: MeshCutSandboxController::TryCutFromWorldBoxVolume
        // 설명: 월드 박스 볼륨을 cutSource 로컬 볼록 볼륨으로 변환해 내부/외부 조각을 스폰한다.
        //==============================================================================
        public bool TryCutFromWorldBoxVolume
        (
            Vector3 worldCenter,
            Quaternion worldRotation,
            Vector3 worldSize,
            MeshFilter cutSource
        )
        {
            EnsureCore();
            if (cutSource == null || cutSource.sharedMesh == null) return false;
            if (!IsValidCutMeshFilter(cutSource)) return false;
            if (worldSize.x <= 0f || worldSize.y <= 0f || worldSize.z <= 0f)
            {
                Debug.LogWarning("[MeshCut] Box volume cut skipped: size must be positive.");
                return false;
            }

            _originalMesh = cutSource.sharedMesh;
            ConvexCutVolume localVolume = _cutPlaneConvert.WorldToLocalBoxVolume(cutSource, worldCenter, worldRotation, worldSize);
            return TryCutInternal(localVolume, cutSource);
        }

        //==============================================================================
        // 기능: MeshCutSandboxController::Cut
        // 설명: 월드 평면을 메시 로컬로 바꿔 멤버 절단 기능을 호출하고, Straddling 일 때만 조각 GO를 스폰한 뒤 타겟을 비활성화한다.
        //==============================================================================
        // TODO: 비-Straddling 시 UI 토스트·에디터 디버그 컬러링
        //
        //==============================================================================
        [Button("Cut"), DisableIf(nameof(_isCut))]
        // 절개 실행
        public void Cut()
        {
            EnsureCore();

            // 1. target / cutter null 체크, 이미 _isCut 이면 무시
            // 2. _originalMesh 에 target.sharedMesh 참조 저장
            // 3. CutPlaneConvert 로 월드 평면을 로컬 평면으로 변환
            // 4. _meshCutter.Cut 으로 실제 절단 수행
            // 5. Straddling 일 때만 Above/Below 조각 스폰
            // 6. 원본 target 을 비활성화하고 _isCut 을 true 로 변경

            // 가드: 대상·평면 없음 또는 이미 한 번 절단함
            if (_target == null || _cutter == null || _isCut) return;

            // Cut 직전 원본 Mesh 참조 저장
            _originalMesh = _target.sharedMesh;

            Plane localPlane = _cutPlaneConvert.WorldToLocal(_target, _cutter);
            TryCutInternal(localPlane, _target);
        }

        //==============================================================================
        // 기능: MeshCutSandboxController::TryCutInternal
        // 설명: 이미 cutSource 로컬인 평면으로 MeshCutter 를 호출하고 성공 시 스폰까지 마친다.
        //==============================================================================
        private bool TryCutInternal(Plane localPlane, MeshFilter cutSource)
        {
            int capSubMeshIndex = ResolveCapSubMeshIndex(cutSource);
            CutResult result = _meshCutter.Cut(cutSource.sharedMesh, localPlane, capSubMeshIndex);

            return TryApplyCutResult(result, cutSource);
        }

        private bool TryCutInternal(BoundedCutPlane localPlane, MeshFilter cutSource)
        {
            int capSubMeshIndex = ResolveCapSubMeshIndex(cutSource);
            CutResult result = _meshCutter.Cut(cutSource.sharedMesh, localPlane, capSubMeshIndex);

            return TryApplyCutResult(result, cutSource);
        }

        //==============================================================================
        // 기능: MeshCutSandboxController::TryCutInternal
        // 설명: 이미 cutSource 로컬인 볼록 볼륨으로 ConvexVolumeMeshCutter 를 호출하고 성공 시 스폰까지 마친다.
        //==============================================================================
        private bool TryCutInternal(ConvexCutVolume localVolume, MeshFilter cutSource)
        {
            int capSubMeshIndex = ResolveCapSubMeshIndex(cutSource);
            CutVolumeResult result = _volumeCutter.Cut(cutSource.sharedMesh, localVolume, capSubMeshIndex);

            return TryApplyCutVolumeResult(result, cutSource);
        }

        private bool TryApplyCutResult(CutResult result, MeshFilter cutSource)
        {

            if (result.Relation != CutPlaneRelation.STRADDLING)
            {
                LogNonStraddling(result.Relation);
                return false;
            }

            SpawnPieceIslands(cutSource, result.AboveMesh, "Above");
            SpawnPieceIslands(cutSource, result.BelowMesh, "Below");

            if (cutSource == _target)
                _target.gameObject.SetActive(false);
            else
                EnsurePieceSpawn().DestroySpawnedOwning(cutSource);

            _isCut = true;
            return true;
        }

        //==============================================================================
        // 기능: MeshCutSandboxController::TryApplyCutVolumeResult
        // 설명: 볼륨 절단 결과의 내부 조각과 외부 조각들을 스폰하고 원본 조각을 정리한다.
        //==============================================================================
        private bool TryApplyCutVolumeResult(CutVolumeResult result, MeshFilter cutSource)
        {
            if (result == null || result.Relation != CutVolumeRelation.INTERSECTING || result.InsideMesh == null)
            {
                LogNonIntersectingVolume(result != null ? result.Relation : CutVolumeRelation.EMPTY_SOURCE);
                return false;
            }

            SpawnPieceIslands(cutSource, result.InsideMesh, "Inside");

            List<Mesh> outsideMeshes = result.OutsideMeshes;
            int outsideCount = outsideMeshes.Count;
            for (int i = 0; i < outsideCount; i++)
            {
                SpawnPieceIslands(cutSource, outsideMeshes[i], "Outside" + i);
            }

            if (cutSource == _target)
                _target.gameObject.SetActive(false);
            else
                EnsurePieceSpawn().DestroySpawnedOwning(cutSource);

            _isCut = true;
            return true;
        }

        private static void LogNonStraddling(CutPlaneRelation relation)
        {
            switch (relation)
            {
                case CutPlaneRelation.EMPTY_SOURCE:
                    Debug.LogWarning("[MeshCut] 절단할 메시가 비어 있음.");
                    break;

                case CutPlaneRelation.ENTIRELY_ABOVE:
                    Debug.LogWarning("[MeshCut] 평면이 메시를 스치지 않음(전부 법선 +쪽).");
                    break;

                case CutPlaneRelation.ENTIRELY_BELOW:
                    Debug.LogWarning("[MeshCut] 평면이 메시를 스치지 않음(전부 법선 반대쪽).");
                    break;

                default:
                    Debug.LogWarning("[MeshCut] 알 수 없는 분류: " + relation);
                    break;
            }
        }

        //==============================================================================
        // 기능: MeshCutSandboxController::LogNonIntersectingVolume
        // 설명: 볼륨 절단이 내부/외부 분리를 만들지 못한 이유를 로그로 출력한다.
        //==============================================================================
        private static void LogNonIntersectingVolume(CutVolumeRelation relation)
        {
            switch (relation)
            {
                case CutVolumeRelation.EMPTY_SOURCE:
                    Debug.LogWarning("[MeshCut] 볼륨 절단 실패: 메쉬 또는 볼륨이 비어 있음.");
                    break;

                case CutVolumeRelation.ENTIRELY_INSIDE:
                    Debug.LogWarning("[MeshCut] 볼륨이 메쉬 전체를 포함해 분리할 외부 조각이 없음.");
                    break;

                case CutVolumeRelation.ENTIRELY_OUTSIDE:
                    Debug.LogWarning("[MeshCut] 볼륨이 메쉬와 교차하지 않음.");
                    break;

                default:
                    Debug.LogWarning("[MeshCut] 알 수 없는 볼륨 분류: " + relation);
                    break;
            }
        }

        private int ResolveCapSubMeshIndex(MeshFilter cutSource)
        {
            if (cutSource == null || cutSource.sharedMesh == null) return -1;
            if (cutSource == _target) return -1;
            if (!EnsurePieceSpawn().IsSpawnedMeshFilter(cutSource)) return -1;

            int subMeshCount = cutSource.sharedMesh.subMeshCount;
            return subMeshCount > 0 ? subMeshCount - 1 : -1;
        }

        // 모든 조각 초기화
        //==============================================================================
        // 기능: MeshCutSandboxController::ResetAll
        // 설명: 스폰된 조각 GameObject 들을 파괴하고 리스트를 비운 뒤 타겟을 다시 활성화하며 컷 상태를 초기화한다.
        //==============================================================================
        // TODO: Addressables Instantiate 대비해 Destroy 대상 명확히
        //
        //==============================================================================
        [Button("Reset"), EnableIf(nameof(_isCut))]
        public void ResetAll()
        {
            // 1. MeshCutPieceSpawn 이 생성한 모든 조각을 파괴하고 목록 Clear
            // 2. target.gameObject.SetActive(true) 로 원본 표시
            // 3. sharedMesh 는 교체하지 않았으므로 별도 복구 없음
            // 4. _isCut 을 false 로 되돌려 Cut 버튼 재활성화

            // 생성된 조각 리스트 초기화
            EnsurePieceSpawn().Clear();

            // 원본 메시 활성화
            _target.gameObject.SetActive(true);

            // 절개 상태 초기화
            _isCut = false;
        }

        //==============================================================================
        // 기능: MeshCutSandboxController::SpawnPiece
        // 설명: 주어진 Mesh 로 동일 로컬 TRS 로 GameObject와 MeshFilter·MeshRenderer를 붙이고 머티리얼을 서브메시 수에 맞게 할당 후 _spawned에 추가한다.
        //==============================================================================
        // TODO: shadowCastingMode, 레이어/태그 설정
        //
        //==============================================================================
        // 조각 생성
        private void SpawnPiece(MeshFilter trsSource, Mesh m, string suffix)
        {
            // 조각 생성 책임은 MeshCutPieceSpawn 으로 분리
            EnsurePieceSpawn().Spawn(trsSource, m, suffix, _pieceMaterial, _capMaterial);
        }

        //==============================================================================
        // 기능: MeshCutSandboxController::SpawnPieceIslands
        // 설명: 하나의 결과 Mesh 안에 떨어진 연결 섬이 여러 개 있으면 GameObject 를 분리해 스폰한다.
        //==============================================================================
        private void SpawnPieceIslands(MeshFilter trsSource, Mesh mesh, string suffix)
        {
            int islandCount = MeshCutIslandUtility.SplitIntoIslands(mesh, _meshIslandScratch);
            if (islandCount == 0) return;

            for (int i = 0; i < islandCount; i++)
            {
                string islandSuffix = islandCount == 1 ? suffix : suffix + "_" + i;
                SpawnPiece(trsSource, _meshIslandScratch[i], islandSuffix);
            }

            if (islandCount > 1 && mesh != null)
                Destroy(mesh);
        }

        //==============================================================================
        // 기능: MeshCutSandboxController::EnsurePieceSpawn
        // 설명: 조각 생성 컴포넌트가 없으면 같은 GameObject 에 자동으로 붙여 반환한다.
        //==============================================================================
        // TODO: 인스펙터 명시 연결만 허용할지 정책 결정
        //
        //==============================================================================
        private MeshCutPieceSpawn EnsurePieceSpawn()
        {
            // 이미 연결된 컴포넌트가 있으면 그대로 사용
            if (_pieceSpawn != null) return _pieceSpawn;

            // 같은 GameObject 에 붙어 있는 컴포넌트 검색
            _pieceSpawn = GetComponent<MeshCutPieceSpawn>();

            // 없으면 런타임에 추가해 기존 씬 연결 없이도 동작 보존
            if (_pieceSpawn == null)
            {
                _pieceSpawn = gameObject.AddComponent<MeshCutPieceSpawn>();
            }

            return _pieceSpawn;
        }

        //==============================================================================
        // 기능: MeshCutSandboxController::EnsureCore
        // 설명: Awake 전에 호출되는 경우에도 Core 의존성을 사용할 수 있게 보장한다.
        //==============================================================================
        private void EnsureCore()
        {
            if (_meshCutter != null && _volumeCutter != null && _cutPlaneConvert != null) return;

            Construct(BuildMeshCutter(), new CutPlaneConvert());
        }

        //==============================================================================
        // 기능: MeshCutSandboxController::BuildMeshCutter
        // 설명: MeshCutting Core 의 반복 작업 버퍼를 조립해 MeshCutter 에 주입한다.
        //==============================================================================
        private static MeshCutter BuildMeshCutter()
        {
            // 반복 컷에서 재사용할 작업 버퍼
            MeshCutterWorkBuffer workBuffer = new MeshCutterWorkBuffer
            (
                new List<Vector3>(),
                new List<Vector2>(),
                new List<Vector3>(),
                new List<int>(),
                new List<bool>(),
                new MeshBuffer(),
                new MeshBuffer(),
                new MeshCutCapBuffer()
            );

            return new MeshCutter(workBuffer);
        }
    }
}
