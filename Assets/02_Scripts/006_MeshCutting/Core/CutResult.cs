using UnityEngine;

namespace OrganicEngineer.MeshCutting
{
    public class CutResult
    {
        public Mesh aboveMesh;  // 평면 +법선 쪽 메시
        public Mesh belowMesh;  // 평면 -법선 쪽 메시 
        public bool wasCut;     // 평면이 실제로 메시를 갈랐는가?
        
        // === Step 2 에서 채울 자리 (지금은 비워두기만) ===
        // public List<CutEdge> aboveCapEdges;
        // public List<CutEdge> belowCapEdges;
    }
}
