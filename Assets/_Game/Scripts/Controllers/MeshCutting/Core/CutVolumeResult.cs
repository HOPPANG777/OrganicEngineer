using System.Collections.Generic;
using UnityEngine;

namespace OrganicEngineer.MeshCutting
{
    //==============================================================================
    // 기능: CutVolumeResult
    // 설명: 볼록 볼륨 절단 결과로 내부 Mesh 와 외부 Mesh 조각 목록을 담는다.
    //==============================================================================
    public sealed class CutVolumeResult
    {
        //-------------------- 멤버 변수 --------------------//

        // 볼륨 외부로 확정된 Mesh 조각 목록
        private readonly List<Mesh> _outsideMeshes = new List<Mesh>();

        //-------------------- Get 함수 --------------------//

        // 절단 볼륨과 소스 Mesh 의 관계
        public CutVolumeRelation Relation { get; internal set; }

        // 볼륨 내부에 남은 Mesh. 없으면 null 이다.
        public Mesh InsideMesh { get; internal set; }

        // 볼륨 외부 Mesh 조각 목록
        public List<Mesh> OutsideMeshes
        {
            get => _outsideMeshes;
        }

        // 실제 내부/외부 분리가 발생했는지 여부
        public bool WasCut
        {
            get => Relation == CutVolumeRelation.INTERSECTING;
        }

        //-------------------- 기능 함수 --------------------//

        //==============================================================================
        // 기능: CutVolumeResult::AddOutsideMesh
        // 설명: null 이 아닌 외부 Mesh 를 결과 목록에 추가한다.
        //==============================================================================
        internal void AddOutsideMesh(Mesh mesh)
        {
            if (mesh == null) return;
            _outsideMeshes.Add(mesh);
        }
    }
}
