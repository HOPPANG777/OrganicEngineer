using System.Collections.Generic;
using UnityEngine;

namespace OrganicEngineer.MeshCutting.Sandbox
{
    //==============================================================================
    // 기능: MeshCutPieceSpawn (MonoBehaviour)
    // 설명: 절단 결과 Mesh 로 조각 GameObject 를 만들고 생성 목록을 관리한다.
    //==============================================================================
    // TODO: 레이어, 태그, shadowCastingMode 복사
    //
    //==============================================================================
    public class MeshCutPieceSpawn : MonoBehaviour
    {
        //-------------------- 맴버 변수 --------------------//

        // 생성된 조각 GameObject 목록
        private readonly List<GameObject> _spawned = new List<GameObject>();

        //-------------------- Get 함수 --------------------//

        // 생성된 조각 개수
        public int SpawnedCount { get => _spawned.Count; }

        //-------------------- 기능 함수 --------------------//

        //==============================================================================
        // 기능: MeshCutPieceSpawn::Spawn
        // 설명: 원본 MeshFilter 의 TRS 를 복사한 조각 GameObject 를 만들고 Mesh/Material 을 설정한다.
        //==============================================================================
        // TODO: 원본 Renderer 의 머티리얼 배열 전체 복사 정책
        //
        //==============================================================================
        public void Spawn(MeshFilter target, Mesh mesh, string suffix, Material pieceMaterial, Material capMaterial)
        {
            // 가드: Mesh 없음
            if (mesh == null)
            {
                Debug.LogWarning($"[MeshCutPieceSpawn] Spawn 스킵: 메시 null ({suffix}).");
                return;
            }

            // 가드: 타깃 없음(TRS · Renderer 복사 불가)
            if (target == null)
            {
                Debug.LogWarning("[MeshCutPieceSpawn] Spawn 스킵: target 이 비어 있음.");
                return;
            }

            // 가드: 외형 복사에 필요한 렌더러 없음
            MeshRenderer sourceRenderer = target.GetComponent<MeshRenderer>();
            if (sourceRenderer == null)
            {
                Debug.LogWarning("[MeshCutPieceSpawn] Spawn 스킵: target 에 MeshRenderer 가 없음.");
                return;
            }

            // 조각 GameObject 생성
            GameObject pieceGo = new GameObject(target.name + "_" + suffix);

            // 원본 Transform 정보를 그대로 복사
            Transform targetTransform = target.transform;
            Transform pieceTf = pieceGo.transform;
            pieceTf.SetParent(targetTransform.parent, false);
            pieceTf.localPosition = targetTransform.localPosition;
            pieceTf.localRotation = targetTransform.localRotation;
            pieceTf.localScale = targetTransform.localScale;

            // MeshFilter 에 절단 결과 Mesh 연결
            MeshFilter pieceFilter = pieceGo.AddComponent<MeshFilter>();
            pieceFilter.sharedMesh = mesh;

            // MeshRenderer 를 만들고 외피/단면 머티리얼 적용
            MeshRenderer pieceRenderer = pieceGo.AddComponent<MeshRenderer>();
            Material shellMat = pieceMaterial != null ? pieceMaterial : sourceRenderer.sharedMaterial;
            Material capMat = capMaterial != null ? capMaterial : shellMat;

            // 서브메시가 2개 이상이면 단면 머티리얼까지 넣음
            if (mesh.subMeshCount > 1)
                pieceRenderer.sharedMaterials = new[] { shellMat, capMat };
            else
                pieceRenderer.sharedMaterial = shellMat;

            // 연속 컷용 레이: 메시 AABB 에 맞춘 BoxCollider(MeshCollider 대신)
            Bounds meshBounds = mesh.bounds;
            const float minExtent = 1e-4f;
            BoxCollider boxCollider = pieceGo.AddComponent<BoxCollider>();
            boxCollider.center = meshBounds.center;
            boxCollider.size = new Vector3(
                Mathf.Max(meshBounds.size.x, minExtent),
                Mathf.Max(meshBounds.size.y, minExtent),
                Mathf.Max(meshBounds.size.z, minExtent));

            // Reset 때 파괴할 수 있도록 목록에 저장
            _spawned.Add(pieceGo);
        }

        //==============================================================================
        // 기능: MeshCutPieceSpawn::IsSpawnedMeshFilter
        // 설명: 해당 MeshFilter 가 이 스포너가 만든 조각 루트 오브젝트인지 여부.
        //==============================================================================
        public bool IsSpawnedMeshFilter(MeshFilter mf)
        {
            if (mf == null) return false;
            GameObject go = mf.gameObject;
            int count = _spawned.Count;
            for (int i = 0; i < count; i++)
            {
                if (_spawned[i] == go) return true;
            }

            return false;
        }

        //==============================================================================
        // 기능: MeshCutPieceSpawn::DestroySpawnedOwning
        // 설명: 목록에 있는 조각이면 제거 후 Destroy 한다. 연속 컷 시 잘릴 조각 정리용.
        //==============================================================================
        public bool DestroySpawnedOwning(MeshFilter mf)
        {
            if (mf == null) return false;
            GameObject go = mf.gameObject;
            int count = _spawned.Count;
            for (int i = 0; i < count; i++)
            {
                if (_spawned[i] != go) continue;
                _spawned.RemoveAt(i);
                Destroy(go);
                return true;
            }

            return false;
        }

        //==============================================================================
        // 기능: MeshCutPieceSpawn::Clear
        // 설명: 생성된 모든 조각 GameObject 를 파괴하고 목록을 비운다.
        //==============================================================================
        // TODO: 플레이 중/에디터 모드 DestroyImmediate 분기
        //
        //==============================================================================
        public void Clear()
        {
            // 성능 위해 foreach 대신 for (CODING_CONVENTIONS §2.2)
            int count = _spawned.Count;
            for (int i = 0; i < count; i++)
            {
                Destroy(_spawned[i]);
            }

            // 목록 초기화
            _spawned.Clear();
        }
    }
}
