using UnityEngine;
using Sirenix.OdinInspector;

namespace OrganicEngineer.MeshCutting.Sandbox
{
    //==============================================================================
    // 기능: MeshCutSandboxController (MonoBehaviour)
    // 설명: 인스펙터에서 타겟과 평면(Transform cutter)으로 MeshCutter.Cut 을 호출해 조각 GameObject를 스폰하고, Reset 으로 정리한다.
    //==============================================================================
    // TODO: _originalMaterials 활용 또는 제거 결정, 콜라이더 재생성·레이어 복사
    //
    //==============================================================================
    public class MeshCutSandboxController : MonoBehaviour
    {
        [Title("References")]
        // 원본 메시
        [Tooltip("절단 대상 MeshFilter")]
        [SerializeField] MeshFilter target;

        // 절개 평면
        [Tooltip("절단 평면. 위치=평면 위 점, up=법선(Above 쪽)")]
        [SerializeField] Transform cutter;

        // 조각 재료
        [Tooltip("외피(서브메시 0) 머티리얼. 비우면 Target과 동일")]
        [SerializeField] Material pieceMaterial;

        // 단면 재료
        [Tooltip("단면(서브메시 1) 머티리얼. 비우면 pieceMaterial과 동일")]
        [SerializeField] Material capMaterial;

        // 조각 생성 컴포넌트
        [Tooltip("잘린 조각 GameObject 생성과 목록 관리를 담당")]
        [SerializeField] MeshCutPieceSpawn pieceSpawn;

        // 원본 메시
        Mesh _originalMesh;
        // 원본 메시 재료
        Material[] _originalMaterials;
        // 절개 상태
        bool _isCut;

        //==============================================================================
        // 기능: MeshCutSandboxController::Cut
        // 설명: 월드 평면을 메시 로컬로 바꿔 MeshCutter.Cut 을 호출하고, Straddling 일 때만 조각 GO를 스폰한 뒤 타겟을 비활성화한다.
        //==============================================================================
        // TODO: 비-Straddling 시 UI 토스트·에디터 디버그 컬러링
        //
        //==============================================================================
        [Button("Cut"), DisableIf(nameof(_isCut))]
        // 절개 실행
        public void Cut()
        {
            /*
             1. 가드: target / cutter null 체크, 이미 _isCut 이면 무시

             2. 백업: _originalMesh = target.sharedMesh (한 번만)

             3. 월드 평면 만들기: Plane worldPlane = new Plane(cutter.up, cutter.position);

             4. 로컬 평면으로 변환

             5. CutResult result = MeshCutter.Cut(target.sharedMesh, localPlane);

             6. 정책: relation == Straddling 일 때만 스폰(Straddling = wasCut 와 동일)

             7. SpawnPiece(result.aboveMesh, "Above"), Below 동일

             8. target.gameObject.SetActive(false);

             9. _isCut = true;
             */

            // 가드: 대상·평면 없음 또는 이미 한 번 절단함
            if (target == null || cutter == null || _isCut) return;

            // Cut 직전 원본 Mesh 참조 저장
            _originalMesh = target.sharedMesh;

            // MeshCutter 에 전달할 타겟 로컬 평면
            Plane localPlane = CutPlaneConvert.WorldToLocal(target, cutter);

            // 정적 절단 진입
            CutResult result = MeshCutter.Cut(target.sharedMesh, localPlane);

            // 실제로 평면이 메시를 가를 때만 게임오브젝트 스폰
            if (result.relation != CutPlaneRelation.Straddling)
            {
                switch (result.relation)
                {
                    case CutPlaneRelation.EmptySource:
                        Debug.LogWarning("[MeshCut] 절단할 메시가 비어 있음.");
                        break;

                    case CutPlaneRelation.EntirelyAbove:
                        Debug.LogWarning("[MeshCut] 평면이 메시를 스치지 않음(전부 법선 +쪽).");
                        break;

                    case CutPlaneRelation.EntirelyBelow:
                        Debug.LogWarning("[MeshCut] 평면이 메시를 스치지 않음(전부 법선 반대쪽).");
                        break;

                    default:
                        Debug.LogWarning("[MeshCut] 알 수 없는 분류: " + result.relation);
                        break;
                }

                return;
            }

            // 법선 +쪽 조각
            SpawnPiece(result.aboveMesh, "Above");
            // 반대쪽 조각
            SpawnPiece(result.belowMesh, "Below");

            // 원본 타겟은 화면/피직스에서 숨김(sharedMesh 교체 안 함)
            target.gameObject.SetActive(false);

            // Cut 버튼 비활성화용 상태
            _isCut = true;
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
            /*
                1. _spawned 안의 모든 GameObject Destroy 후 리스트 Clear

                2. target.gameObject.SetActive(true);

                3. (sharedMesh 는 안 건드렸으니 복구 불필요)

                4. _isCut = false;
            */

            // 생성된 조각 리스트 초기화
            EnsurePieceSpawn().Clear();

            // 원본 메시 활성화
            target.gameObject.SetActive(true);

            // 절개 상태 초기화
            _isCut = false;
        }

        //==============================================================================
        // 기능: MeshCutSandboxController::SpawnPiece
        // 설명: 주어진 Mesh 로 동일 로컬 TRS 로 GameObject와 MeshFilter·MeshRenderer를 붙이고 머티리얼을 서브메시 수에 맞게 할당 후 _spawned에 추가한다.
        //==============================================================================
        // TODO: MeshCollider 복사, shadowCastingMode, 레이어/태그 설정
        //
        //==============================================================================
        // 조각 생성
        void SpawnPiece(Mesh m, string suffix)
        {
            // 조각 생성 책임은 MeshCutPieceSpawn 으로 분리
            EnsurePieceSpawn().Spawn(target, m, suffix, pieceMaterial, capMaterial);
        }

        //==============================================================================
        // 기능: MeshCutSandboxController::EnsurePieceSpawn
        // 설명: 조각 생성 컴포넌트가 없으면 같은 GameObject 에 자동으로 붙여 반환한다.
        //==============================================================================
        // TODO: 인스펙터 명시 연결만 허용할지 정책 결정
        //
        //==============================================================================
        MeshCutPieceSpawn EnsurePieceSpawn()
        {
            // 이미 연결된 컴포넌트가 있으면 그대로 사용
            if (pieceSpawn != null) return pieceSpawn;

            // 같은 GameObject 에 붙어 있는 컴포넌트 검색
            pieceSpawn = GetComponent<MeshCutPieceSpawn>();

            // 없으면 런타임에 추가해 기존 씬 연결 없이도 동작 보존
            if (pieceSpawn == null)
            {
                pieceSpawn = gameObject.AddComponent<MeshCutPieceSpawn>();
            }

            return pieceSpawn;
        }
    }
}
