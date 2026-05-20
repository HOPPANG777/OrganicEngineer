using System.Collections.Generic;
using UnityEngine;

namespace OrganicEngineer.MeshCutting
{
    //==============================================================================
    // 기능: CutVolumeResult
    // 설명: 볼록 절단 볼륨으로 Mesh 를 잘랐을 때 내부 Mesh 와 외부 Mesh 목록을 보관한다.
    //==============================================================================
    public sealed class CutVolumeResult
    {
        //-------------------- 멤버 변수 --------------------//

        // 볼륨 바깥으로 분리된 Mesh 조각 목록
        private readonly List<Mesh> _outsideMeshes = new List<Mesh>();

        //-------------------- Get 함수 --------------------//

        // 절단 볼륨과 소스 Mesh 의 포함 또는 교차 관계
        public CutVolumeRelation Relation { get; internal set; }

        // 볼륨 내부에 남은 Mesh. 내부 조각이 없으면 null 이다.
        public Mesh InsideMesh { get; internal set; }

        // 볼륨 바깥으로 잘려 나온 Mesh 조각 목록
        public List<Mesh> OutsideMeshes
        {
            get => _outsideMeshes;
        }

        // 실제로 내부와 외부가 분리되었는지 확인하는 편의 속성
        public bool WasCut
        {
            get => Relation == CutVolumeRelation.INTERSECTING;
        }

        //-------------------- 기능 함수 --------------------//

        //==============================================================================
        // 기능: CutVolumeResult::AddOutsideMesh
        // 설명: null 이 아닌 외부 Mesh 만 결과 목록에 추가한다.
        //==============================================================================
        internal void AddOutsideMesh(Mesh mesh)
        {
            // 절단 단계에 따라 외부 조각이 없을 수 있으므로 null 은 조용히 무시한다.
            if (mesh == null) return;

            _outsideMeshes.Add(mesh);
        }
    }
}
