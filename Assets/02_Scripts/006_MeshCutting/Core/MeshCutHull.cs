using System;
using System.Collections.Generic;
using UnityEngine;

namespace OrganicEngineer.MeshCutting
{
    //==============================================================================
    // 기능: MeshCutHull
    // 설명: 절단면 점들을 2D 로 투영한 뒤 Monotone Chain 볼록 껍질을 계산한다.
    //==============================================================================
    // TODO: 공선점 유지 옵션, 오목 다각형 ear-clipping 확장
    //
    //==============================================================================
    static class MeshCutHull
    {
        //==============================================================================
        // 기능: MeshCutHull::ConvexHull2DIndices
        // 설명: 2D 점 집합에서 볼록 껍질을 이루는 원본 인덱스 배열을 반환한다.
        //==============================================================================
        // TODO: 중복점 사전 제거
        //
        //==============================================================================
        public static int[] ConvexHull2DIndices(List<Vector2> pts)
        {
            // 점이 3개 미만이면 면을 만들 수 없음
            int n = pts.Count;
            if (n < 3) return null;

            // 정렬용 인덱스 배열 생성
            int[] order = new int[n];
            for (int i = 0; i < n; i++)
            {
                order[i] = i;
            }

            // x 우선, y 보조 기준으로 정렬
            Array.Sort(order, (i, j) =>
            {
                int cmp = pts[i].x.CompareTo(pts[j].x);
                if (cmp != 0) return cmp;
                return pts[i].y.CompareTo(pts[j].y);
            });

            // 하단 껍질 계산
            List<int> lower = new List<int>();
            for (int i = 0; i < n; i++)
            {
                int idx = order[i];
                while (lower.Count >= 2 && Cross2(pts[lower[lower.Count - 2]], pts[lower[lower.Count - 1]], pts[idx]) <= 0f)
                {
                    lower.RemoveAt(lower.Count - 1);
                }
                lower.Add(idx);
            }

            // 상단 껍질 계산
            List<int> upper = new List<int>();
            for (int i = n - 1; i >= 0; i--)
            {
                int idx = order[i];
                while (upper.Count >= 2 && Cross2(pts[upper[upper.Count - 2]], pts[upper[upper.Count - 1]], pts[idx]) <= 0f)
                {
                    upper.RemoveAt(upper.Count - 1);
                }
                upper.Add(idx);
            }

            // 중복 끝점 제거 후 하나로 합침
            lower.RemoveAt(lower.Count - 1);
            upper.RemoveAt(upper.Count - 1);
            lower.AddRange(upper);

            return lower.ToArray();
        }

        // 2D 외적 부호 계산
        public static float Cross2(Vector2 o, Vector2 a, Vector2 b)
        {
            return (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
        }
    }
}
