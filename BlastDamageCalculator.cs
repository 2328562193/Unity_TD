using System.Collections.Generic;
using UnityEngine;

public static class BlastDamageCalculator {

    private const int sectorCount = 18;
    private const float sectorAngle = 360f / sectorCount;

    private static readonly List<(float distSqr, int originalIndex, int sector)> validTargets = 
        new List<(float, int, int)>();
    private static readonly List<(float distSqr, int originalIndex)>[] sectors = 
        new List<(float, int)>[sectorCount];
    static BlastDamageCalculator(){
        for (int i = 0; i < sectorCount; i++)
            sectors[i] = new List<(float, int)>();
    }
    private static readonly Comparison<(float distSqr, int idx)> _distComparer = 
        (a, b) => a.distSqr.CompareTo(b.distSqr);

    public static int[] CalculateBlastDamageBySector(
            int damage,
            Vector2 center,
            float range,
            List<Vector2> posList) {
        if (posList == null || posList.Count == 0) return new int[0];

        int[] result = new int[posList.Count];
        if (range <= 0f) return result;
        
        float rangeSqr = range * range;
        validTargets.Clear();
        for (int i = 0; i < posList.Count; i++) {
            Vector2 pos = posList[i];
            float distSqr = (pos - center).sqrMagnitude;
            
            if (distSqr >= rangeSqr) continue;

            float angle = Mathf.Atan2(pos.y - center.y, pos.x - center.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;

            int sector = Mathf.FloorToInt(angle / sectorAngle);
            sector = Mathf.Clamp(sector, 0, sectorCount - 1);

            validTargets.Add((distSqr, i, sector));
            result[i] = -1;
        }

        for (int i = 0; i < sectorCount; i++)
            sectors[i].Clear();
        foreach (var target in validTargets)
            sectors[target.sector].Add((target.distSqr, target.originalIndex));
        for (int s = 0; s < sectorCount; s++)
            if (sectors[s].Count > 1) sectors[s].Sort(_distComparer);

        for (int s = 0; s < sectorCount; s++) {
            var sectorList = sectors[s];
            float penetration = 1f;
            for (int rank = 0; rank < sectorList.Count; rank++) {
                var (distSqr, idx) = sectorList[rank];
                float finalDamage = damage * (1f - (distSqr / rangeSqr) * 0.8f) * penetration;
                penetration *= 0.8f;
                result[idx] = (int)finalDamage;
            }
        }

        for (int i = 0; i < result.Length; i++) {
            if (result[i] < 0) result[i] = 0;
        }

        return result;
    }
}