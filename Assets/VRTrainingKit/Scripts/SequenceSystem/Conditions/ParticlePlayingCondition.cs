// ParticlePlayingCondition.cs
// Condition that completes when a particle system starts playing
using UnityEngine;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Condition that becomes met when a particle system is playing
/// Useful for steps like "Wait for mist to appear"
/// </summary>
public class ParticlePlayingCondition : BaseSequenceCondition
{
    [Header("Particle Reference")]
    [Tooltip("The particle system to monitor (auto-detected if on same GameObject)")]
    public ParticleSystem targetParticle;

    [Header("Completion Condition")]
    [Tooltip("Complete immediately when particle starts playing")]
    public bool completeOnStart = true;

    [Tooltip("Complete after particle has been playing for this duration (seconds)")]
    public float playDuration = 0f;

    private float playStartTime = -1f;
    private bool wasPlayingLastFrame = false;

    void Start()
    {
        if (targetParticle == null)
        {
            targetParticle = GetComponent<ParticleSystem>();
        }

        if (targetParticle == null)
        {
            LogError("No ParticleSystem found! Assign targetParticle or add ParticleSystem component.");
            return;
        }

        LogDebug($"Monitoring particle: {targetParticle.name}");
    }

    void Update()
    {
        if (targetParticle == null || conditionMet) return;

        bool isPlayingNow = targetParticle.isPlaying;

        // Detect when particle starts playing
        if (isPlayingNow && !wasPlayingLastFrame)
        {
            OnParticleStarted();
        }

        // Check duration condition
        if (isPlayingNow && playDuration > 0 && playStartTime >= 0)
        {
            float elapsed = Time.time - playStartTime;
            if (elapsed >= playDuration)
            {
                LogInfo($"Particle has been playing for {playDuration}s - condition met!");
                SetConditionMet();
            }
        }

        wasPlayingLastFrame = isPlayingNow;
    }

    void OnParticleStarted()
    {
        LogDebug("Particle started playing");
        playStartTime = Time.time;

        if (completeOnStart)
        {
            LogInfo("Particle started - condition met immediately!");
            SetConditionMet();
        }
    }

    public override string GetStatusMessage()
    {
        if (conditionMet)
        {
            return "Particle playing ✓";
        }

        if (targetParticle == null)
        {
            return "No particle system assigned";
        }

        if (targetParticle.isPlaying)
        {
            if (playDuration > 0 && playStartTime >= 0)
            {
                float elapsed = Time.time - playStartTime;
                float remaining = Mathf.Max(0, playDuration - elapsed);
                return $"Particle playing... {remaining:F1}s remaining";
            }
            return "Particle playing...";
        }

        return "Waiting for particle to play";
    }

    public override void ResetCondition()
    {
        base.ResetCondition();
        playStartTime = -1f;
        wasPlayingLastFrame = false;
    }
}
