using UnityEngine;
using Sirenix.OdinInspector;

namespace OrganicEngineer.MeshCutting.Sandbox
{
    public class MeshCutSandboxController : MonoBehaviour
    {
        [Title("References")]
        [SerializeField] MeshFilter target;
        [SerializeField] Transform cutter;
        [SerializeField] Material pieceMaterial;  // 비워두면 원본 머티리얼 재사용
        
        // 내부 상태 (Reset 용)
        Mesh _originalMesh;
        Material[] _originalMaterials;
        bool _isCut;
        
        [Button("Cut"), DisableIf(nameof(_isCut))]
        public void Cut() 
        {  
            /*
            1. 가드: target / cutter null 체크, 이미 _isCut 이면 무시

            2. 백업: _originalMesh = target.sharedMesh (한 번만)

            3. 월드 평면 만들기: Plane worldPlane = new Plane(cutter.up, cutter.position);

            4. 로컬 평면으로 변환: (target.transform 의 inverse 로 평면 normal/point 변환)

            5. CutResult result = MeshCutter.Cut(target.sharedMesh, localPlane);

            6. result.wasCut == false 면: Debug.LogWarning("평면이 메시를 가르지 않음."); return;
            
            7. SpawnPiece(result.aboveMesh, "Above");
               SpawnPiece(result.belowMesh, "Below");

            8. target.gameObject.SetActive(false);

            9. _isCut = true;
            */
        }
        
        [Button("Reset"), EnableIf(nameof(_isCut))]
        public void ResetAll() 
        {  
            /*
            1. GameObject go = new GameObject(target.name + "_" + suffix);
            
            2. go.transform 을 target.transform 과 동일하게 복사(position, rotation, scale, parent)

            3. MeshFilter mf = go.AddComponent<MeshFilter>(); mf.sharedMesh = m;

            4. MeshRenderer mr = go.AddComponent<MeshRenderer>();
               mr.sharedMaterial = pieceMaterial != null ? pieceMaterial 
               : target.GetComponent<MeshRenderer>().sharedMaterial;

            5. _spawned 리스트에 추가;
            
            */
        }
    }
}