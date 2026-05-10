using System.Collections.Generic;
using UnityEngine;

namespace OrganicEngineer.MeshCutting
{
    //==============================================================================
    // 기능: MeshCutWeld
    // 설명: 절단 에지 양 끝점을 양자화해 중복 없는 절단점 목록으로 만든다.
    //==============================================================================
    // TODO: 웰드 거리 옵션을 인스펙터 설정으로 분리
    //
    //==============================================================================
    class MeshCutWeld
    {
        //-------------------- 맴버 변수 --------------------//

        // 절단점 웰드용 정수 그리드 스케일
        private float _weldScale = 1e4f;

        //==============================================================================
        // 기능: MeshCutWeld::BuildPoints
        // 설명: CutEdge 목록의 A/B 점을 웰드해 고유한 3D 점 목록으로 반환한다.
        //==============================================================================
        // TODO: 원본 에지 연결 순서 보존 옵션
        //
        //==============================================================================
        public List<Vector3> BuildPoints(List<CutEdge> edges)
        {
            // 양자화 좌표에서 points 인덱스로 가는 맵
            Dictionary<Vector3Int, int> weldMap = new Dictionary<Vector3Int, int>();

            // 웰드된 고유 절단점 목록
            List<Vector3> points = new List<Vector3>();

            // 모든 에지의 양 끝점을 순회
            for (int i = 0; i < edges.Count; i++)
            {
                WeldPoint(weldMap, points, edges[i].A);
                WeldPoint(weldMap, points, edges[i].B);
            }

            return points;
        }

        // 양자화 키로 중복점을 제거하며 점 목록에 추가
        private void WeldPoint(Dictionary<Vector3Int, int> weldMap, List<Vector3> points, Vector3 point)
        {
            // 연속 좌표를 정수 키로 변환
            Vector3Int key = Quantize(point);

            // 이미 등록된 점이면 추가하지 않음
            if (weldMap.ContainsKey(key)) return;

            // 새 점의 인덱스 기록
            weldMap[key] = points.Count;

            // 새 점 추가
            points.Add(point);
        }

        // 연속 좌표를 웰드용 정수 키로 양자화
        private Vector3Int Quantize(Vector3 value)
        {
            return new Vector3Int
            (
                Mathf.RoundToInt(value.x * _weldScale),
                Mathf.RoundToInt(value.y * _weldScale),
                Mathf.RoundToInt(value.z * _weldScale)
            );
        }
    }
}
