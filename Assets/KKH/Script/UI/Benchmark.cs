using UnityEngine;

/// <summary>
/// 하드웨어 사양을 자동 감지하여 그래픽 품질을 결정하는 벤치마크 유틸리티.
/// <see cref="SettingsManager.ApplyAutoGraphics"/>을 호출하여 결과를 적용한다.
/// </summary>
public class Benchmark : MonoBehaviour
{
    #region ── 기준 사양 (프로젝트 2.05GB 규모 기준) ──

    private readonly float refCpuFreq  = 3000f;  // MHz
    private readonly float refCpuCount = 4f;     // 코어 수
    private readonly float refGpuMem   = 4096f;  // MB
    private readonly float refRam      = 8192f;  // MB

    #endregion

    /// <summary>
    /// 현재 PC 하드웨어 정보를 수집하고, 점수에 따라 그래픽 품질(0~5)을 자동 결정·적용한다.
    /// </summary>
    public void AutoDetectAndApply()
    {
        // ① 하드웨어 정보 수집 (에디터에서 processorFrequency가 0일 수 있으므로 예외 처리)
        float currentFreq = SystemInfo.processorFrequency <= 0 ? 2500f : SystemInfo.processorFrequency;
        float cpuScore = (currentFreq / refCpuFreq) * (SystemInfo.processorCount / refCpuCount);
        float gpuScore = (float)SystemInfo.graphicsMemorySize / refGpuMem;
        float ramScore = (float)SystemInfo.systemMemorySize / refRam;

        // ② 가중치 합산 (CPU 30%, GPU 40%, RAM 30% — 2D 리소스 비중 반영)
        float totalScore = (cpuScore * 0.3f) + (gpuScore * 0.4f) + (ramScore * 0.3f);

        // ③ 점수 → 품질 레벨 매핑 (0 = 최저 ~ 5 = 최고)
        int qualityLevel;
        if      (totalScore >= 1.2f) qualityLevel = 5;
        else if (totalScore >= 1.0f) qualityLevel = 4;
        else if (totalScore >= 0.8f) qualityLevel = 3;
        else if (totalScore >= 0.6f) qualityLevel = 2;
        else if (totalScore >= 0.4f) qualityLevel = 1;
        else                         qualityLevel = 0;

        // ④ SettingsManager를 통해 품질 적용
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.ApplyAutoGraphics(qualityLevel);
        }
    }
}
