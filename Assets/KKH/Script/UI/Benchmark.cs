using UnityEngine;

public class Benchmark : MonoBehaviour
{
    // 프로젝트(2.05GB) 규모를 고려한 기준 사양 (Reference)
    private readonly float refCpuFreq = 3000f;
    private readonly float refCpuCount = 4f;
    private readonly float refGpuMem = 4096f;
    private readonly float refRam = 8192f;

    public void AutoDetectAndApply()
    {
        // 1. 하드웨어 정보 수집 (에디터 예외 처리 포함)
        float currentFreq = SystemInfo.processorFrequency <= 0 ? 2500f : SystemInfo.processorFrequency;
        float cpuScore = (currentFreq / refCpuFreq) * (SystemInfo.processorCount / refCpuCount);
        float gpuScore = (float)SystemInfo.graphicsMemorySize / refGpuMem;
        float ramScore = (float)SystemInfo.systemMemorySize / refRam;

        // 2. 가중치 합산 (2D 리소스 비중 반영)
        float totalScore = (cpuScore * 0.3f) + (gpuScore * 0.4f) + (ramScore * 0.3f);

        // 3. 점수별 인덱스 결정 (0~5번)
        int qualityLevel;
        if (totalScore >= 1.2f) qualityLevel = 5;
        else if (totalScore >= 1.0f) qualityLevel = 4;
        else if (totalScore >= 0.8f) qualityLevel = 3;
        else if (totalScore >= 0.6f) qualityLevel = 2;
        else if (totalScore >= 0.4f) qualityLevel = 1;
        else qualityLevel = 0;

        // 4. 실제 적용 (SettingsManager 호출)
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.ApplyAutoGraphics(qualityLevel);
        }
    }
}
