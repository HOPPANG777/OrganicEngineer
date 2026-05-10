using System.Collections.Generic;
using UnityEngine;

namespace OrganicEngineer.MeshCutting.Sandbox
{
    //==============================================================================
    // 기능: MeshCutPieceSpawn
    // 설명: 절단 결과 Mesh 로 조각 GameObject 를 만들고 생성 목록을 관리한다.
    //==============================================================================
    // TODO: MeshCollider, 레이어, 태그, shadowCastingMode 복사
    //
    //==============================================================================
    public class MeshCutPieceSpawn : MonoBehaviour
    {
        //-------------------- 내부 변수 --------------------//

        // 생성된 조각 GameObject 목록
        private readonly List<GameObject> _spawned = new List<GameObject>();

        //-------------------- Get 함수 --------------------//

        // 생성된 조각 개수
        public int SpawnedCount { get => _spawned.Count; }

        //==============================================================================
        // 기능: MeshCutPieceSpawn::Spawn
        // 설명: 원본 MeshFilter 의 TRS 를 복사한 조각 GameObject 를 만들고 Mesh/Material 을 설정한다.
        //==============================================================================
        // TODO: 원본 Renderer 의 머티리얼 배열 전체 복사 정책
        //
        //==============================================================================
        public void Spawn(MeshFilter target, Mesh mesh, string suffix, Material pieceMaterial, Material capMaterial)
        {
            // 메시가 없으면 조각 생성 불가
            if (mesh == null)
            {
                Debug.LogWarning($"[MeshCutPieceSpawn] Spawn 스킵: 메시 null ({suffix}).");
                return;
            }

            // 원본이 없으면 위치/렌더러를 복사할 수 없음
            if (target == null)
            {
                Debug.LogWarning("[MeshCutPieceSpawn] Spawn 스킵: target 이 비어 있음.");
                return;
            }

            // 조각 GameObject 생성
            GameObject go = new GameObject(target.name + "_" + suffix);

            // 원본 Transform 정보를 그대로 복사
            Transform tt = target.transform;
            go.transform.SetParent(tt.parent, false);
            go.transform.localPosition = tt.localPosition;
            go.transform.localRotation = tt.localRotation;
            go.transform.localScale = tt.localScale;

            // MeshFilter 에 절단 결과 Mesh 연결
            MeshFilter mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            // MeshRenderer 를 만들고 외피/단면 머티리얼 적용
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            MeshRenderer srcMr = target.GetComponent<MeshRenderer>();
            Material shellMat = pieceMaterial != null ? pieceMaterial : srcMr.sharedMaterial;
            Material capMat = capMaterial != null ? capMaterial : shellMat;

            // 서브메시가 2개 이상이면 단면 머티리얼까지 넣음
            if (mesh.subMeshCount > 1)
            {
                mr.sharedMaterials = new[] { shellMat, capMat };
            }
            else
            {
                mr.sharedMaterial = shellMat;
            }

            // Reset 때 파괴할 수 있도록 목록에 저장
            _spawned.Add(go);
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
            // 생성된 조각을 순서대로 파괴
            for (int i = 0; i < _spawned.Count; i++)
            {
                Destroy(_spawned[i]);
            }

            // 목록 초기화
            _spawned.Clear();
        }
    }
}
