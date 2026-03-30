using UnityEngine;

public class Benchmark : MonoBehaviour
{
    private readonly float refCpuFreq = 3000f; // 3.0 GHz
    private readonly float refCpuCount = 4f;   // 4 Cores
    private readonly float refGpuMem = 4096f;  // 4 GB VRAM (텍스처 메모리 대응)
    private readonly float refRam = 8192f;     // 8 GB System RAM

    public void AutoDetectAndApply()
    {
        // 1. 하드웨어 점수 연산 (0에 수렴하지 않도록 Mathf.Max 처리)
        float cpuScore = (Mathf.Max(1, SystemInfo.processorFrequency) / refCpuFreq) * (Mathf.Max(1, SystemInfo.processorCount) / refCpuCount);
        float gpuScore = Mathf.Max(1, SystemInfo.graphicsMemorySize) / refGpuMem;
        float ramScore = Mathf.Max(1, SystemInfo.systemMemorySize) / refRam;

        // 2. 가중치 합산
        float totalScore = (cpuScore * 0.3f) + (gpuScore * 0.4f) + (ramScore * 0.3f);

        // 3. 점수 기반 품질 단계 (6단계 기준: 0~5)
        int qualityLevel;
        if (totalScore >= 1.2f) qualityLevel = 5;      // Ultra
        else if (totalScore >= 1.0f) qualityLevel = 4; // Very High
        else if (totalScore >= 0.8f) qualityLevel = 3; // High
        else if (totalScore >= 0.6f) qualityLevel = 2; // Medium
        else if (totalScore >= 0.4f) qualityLevel = 1; // Low
        else qualityLevel = 0;                         // Very Low

        // 4. SettingsManager 연동
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.ApplyAutoGraphics(qualityLevel);
        }
    }
}
