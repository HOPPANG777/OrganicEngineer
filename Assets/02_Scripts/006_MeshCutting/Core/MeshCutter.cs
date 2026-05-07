using UnityEngine;

/*
INPUT: Mesh source, Plane planeLocal

1. 정점 분류:
   for each vertex v in source.vertices:
       side[i] = planeLocal.GetSide(v)   // true = +, false = −
       (또는 SignedDistance < 0 / > 0)

2. 출력 버퍼 두 개 준비:
   aboveVerts, aboveTris, aboveUVs, aboveNormals
   belowVerts, belowTris, belowUVs, belowNormals

3. 삼각형 순회 (3개씩):
   for each (i0, i1, i2) in source.triangles:
       sides = (side[i0], side[i1], side[i2])
       
       case (모두 +):    aboveBuffer 에 그대로 추가
       case (모두 -):    belowBuffer 에 그대로 추가
       case (1+, 2-) 또는 (2+, 1-):
           → 평면과 만나는 교점 P, Q 계산 (Lerp)
           → UV, Normal 도 같은 비율로 Lerp
           → 한쪽엔 작은 삼각형 1개, 반대쪽엔 사다리꼴(삼각형 2개) 생성
           → 와인딩 순서 유지에 주의

4. CutResult 생성:
   - aboveMesh = new Mesh(); 버퍼 채우기
   - belowMesh = new Mesh(); 버퍼 채우기
   - wasCut = (걸친 삼각형이 1개라도 있었나?)
   - return
   
*/

namespace OrganicEngineer.MeshCutting
{
    public static class MeshCutter
    {
        /// <summary>
        /// source 메시를 메시 로컬 공간 평면 으로 자른다.
        /// 단면 메시를 만들지 않음. 자른 자리는 뚫린 채로 남음.
        /// <summary>
        public static CutResult Cut(Mesh source, Plane planeLocal)
        {

            CutResult result = new CutResult();



            return result;
        }

    }
}


