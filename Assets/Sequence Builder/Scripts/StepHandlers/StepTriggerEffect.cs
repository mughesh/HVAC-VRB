// StepTriggerEffect.cs
// Simple component to trigger effects when specific steps complete
// Attach this to any GameObject with effects (ParticleSystem, AudioSource, etc.)
using UnityEngine;

// NO NAMESPACE - Follows existing project pattern

/// <summary>
/// Triggers effects (particles, audio, animations) when a specific training step completes
/// Simple name-based matching - just type the step name in Inspector
/// </summary>
public class StepTriggerEffect : MonoBehaviour
{
    [Header("Controller Reference")]
    [Tooltip("The ModularTrainingSequenceController in your scene")]
    public ModularTrainingSequenceController sequenceController;

    [Header("Step Triggers")]
    [Tooltip("Play effect when step name contains this text (case-insensitive)")]
    public string playOnStepName = "";

    [Tooltip("Stop effect when step name contains this text (leave empty to never stop)")]
    public string stopOnStepName = "";

    [Header("Effect Components (Auto-detected if empty)")]
    [Tooltip("Particle system to play/stop (auto-detected if empty)")]
    public ParticleSystem particleEffect;

    [Tooltip("Audio source to play (auto-detected if empty)")]
    public AudioSource audioSource;

    [Tooltip("Animator to trigger (auto-detected if empty)")]
    public Animator animator;

    [Tooltip("Animation trigger name (if using Animator)")]
    public string animationTriggerName = "Play";

    [Header("Timing")]
    [Tooltip("Delay before playing effect (seconds)")]
    public float playDelay = 0f;

    [Tooltip("Delay before stopping effect (seconds)")]
    public float stopDelay = 0f;

    [Header("Options")]
    [Tooltip("Play effect only once (ignore subsequent matches)")]
    public bool playOnce = true;

    [Tooltip("Enable debug logging")]
    public bool enableDebugLogging = false;

    private bool hasPlayedOnce = false;

    void Start()
    {
        // Auto-detect controller if not assigned
        if (sequenceController == null)
        {
            sequenceController = FindObjectOfType<ModularTrainingSequenceController>();
            if (sequenceController == null)
            {
                LogError("No ModularTrainingSequenceController found in scene!");
                return;
            }
        }

        // Auto-detect effect components
        if (particleEffect == null)
            particleEffect = GetComponent<ParticleSystem>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (animator == null)
            animator = GetComponent<Animator>();

        // Subscribe to step completion events
        sequenceController.OnStepCompleted += OnStepCompleted;

        LogDebug($"StepTriggerEffect initialized - Play on: '{playOnStepName}', Stop on: '{stopOnStepName}'");
    }

    void OnDestroy()
    {
        if (sequenceController != null)
        {
            sequenceController.OnStepCompleted -= OnStepCompleted;
        }
    }

    void OnStepCompleted(InteractionStep completedStep)
    {
        if (completedStep == null) return;

        string stepName = completedStep.stepName;

        // Check if this step should play the effect
        if (!string.IsNullOrEmpty(playOnStepName) &&
            stepName.IndexOf(playOnStepName, System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            // Check if already played and playOnce is enabled
            if (playOnce && hasPlayedOnce)
            {
                LogDebug($"Effect already played once - ignoring step: {stepName}");
                return;
            }

            LogInfo($"Step matched! Playing effect for: {stepName}");

            if (playDelay > 0)
            {
                Invoke(nameof(PlayEffect), playDelay);
            }
            else
            {
                PlayEffect();
            }

            hasPlayedOnce = true;
        }

        // Check if this step should stop the effect
        if (!string.IsNullOrEmpty(stopOnStepName) &&
            stepName.IndexOf(stopOnStepName, System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            LogInfo($"Step matched! Stopping effect for: {stepName}");

            if (stopDelay > 0)
            {
                Invoke(nameof(StopEffect), stopDelay);
            }
            else
            {
                StopEffect();
            }
        }
    }

    void PlayEffect()
    {
        // Play particle system
        if (particleEffect != null)
        {
            particleEffect.Play();
            LogDebug("Particle system started");
        }

        // Play audio
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
            LogDebug("Audio started");
        }

        // Trigger animation
        if (animator != null && !string.IsNullOrEmpty(animationTriggerName))
        {
            animator.SetTrigger(animationTriggerName);
            LogDebug($"Animation trigger: {animationTriggerName}");
        }
    }

    void StopEffect()
    {
        // Stop particle system
        if (particleEffect != null)
        {
            particleEffect.Stop();
            LogDebug("Particle system stopped");
        }

        // Stop audio
        if (audioSource != null)
        {
            audioSource.Stop();
            LogDebug("Audio stopped");
        }
    }

    /// <summary>
    /// Public method to reset the "played once" flag (for testing or restarting)
    /// </summary>
    public void ResetPlayedFlag()
    {
        hasPlayedOnce = false;
        LogDebug("Played once flag reset");
    }

    void LogInfo(string message)
    {
        Debug.Log($"[StepTriggerEffect] {gameObject.name}: {message}");
    }

    void LogDebug(string message)
    {
        if (enableDebugLogging)
        {
            Debug.Log($"[StepTriggerEffect] {gameObject.name}: {message}");
        }
    }

    void LogError(string message)
    {
        Debug.LogError($"[StepTriggerEffect] {gameObject.name}: {message}");
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Auto-detect components when values change in Inspector
        if (particleEffect == null)
            particleEffect = GetComponent<ParticleSystem>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (animator == null)
            animator = GetComponent<Animator>();
    }
#endif
}
