using System;
using UnityEngine;

[RequireComponent(typeof(CarController))]
public class CarVfxSfx : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CarController car;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private WheelPhysics[] wheels;
    [SerializeField] private AudioSource engineSource;
    [SerializeField] private AudioSource skidSource;
    [SerializeField] private TrailRenderer[] skidTrails;
    [SerializeField] private ParticleSystem[] skidParticles;

    [Header("Collision Audio")]
    [SerializeField] private AudioSource collisionSource;
    [SerializeField] private AudioClip[] crashClips;
    [SerializeField] private float minCollisionRelativeVelocity = 2f;
    [SerializeField] private float maxCollisionRelativeVelocity = 15f;
    [SerializeField, Range(0f, 1f)] private float minCollisionVolume = 0.2f;
    [SerializeField, Range(0f, 1f)] private float maxCollisionVolume = 1f;

    [Header("Engine Audio")]
    [SerializeField] private float maxSpeedForAudio = 55f;
    [SerializeField, Range(0f, 1f)] private float engineVolume = 0.7f;
    [SerializeField] private float minPitch = 0.75f;
    [SerializeField] private float maxPitch = 1.8f;
    [SerializeField] private float pitchSmooth = 5f;

    [Header("Skid Feedback")]
    [SerializeField] private float skidMinSpeed = 3f;
    [SerializeField] private float lateralSlipStart = 1.5f;
    [SerializeField] private float lateralSlipFull = 6f;
    [SerializeField, Range(0f, 1f)] private float skidVolume = 0.8f;
    [SerializeField, Range(0f, 1f)] private float trailAlphaAtFullSlip = 0.8f;
    [SerializeField] private float particleMinSpeed = 5f;

    public float HighestSlip01 { get; private set; }

    void Awake()
    {
        if (!car) car = GetComponent<CarController>();
        if (!rb) rb = GetComponent<Rigidbody>();
        if (wheels == null || wheels.Length == 0)
            wheels = GetComponentsInChildren<WheelPhysics>();
        skidTrails ??= Array.Empty<TrailRenderer>();
        skidParticles ??= Array.Empty<ParticleSystem>();

        if (engineSource)
            engineSource.loop = true;
        if (skidSource)
            skidSource.loop = true;

        if (collisionSource)
        {
            collisionSource.loop = false;
            collisionSource.playOnAwake = false;
        }
    }

    void Update()
    {
        if (!rb) return;

        float speed = rb.linearVelocity.magnitude;
        UpdateEngineAudio(speed);
        UpdateSkidFeedback(speed);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!collisionSource || crashClips == null || crashClips.Length == 0) return;

        float impactForce = collision.relativeVelocity.magnitude;

        if (impactForce >= minCollisionRelativeVelocity)
        {
            float rawT = Mathf.InverseLerp(minCollisionRelativeVelocity, maxCollisionRelativeVelocity, impactForce);
            float volume = Mathf.Lerp(minCollisionVolume, maxCollisionVolume, rawT);

            AudioClip randomClip = crashClips[UnityEngine.Random.Range(0, crashClips.Length)];

            collisionSource.PlayOneShot(randomClip, volume);
        }
    }

    void OnDisable()
    {
        HighestSlip01 = 0f;
        if (engineSource)
            engineSource.Stop();
        if (skidSource)
        {
            skidSource.volume = 0f;
            skidSource.Stop();
        }

        for (int i = 0; skidTrails != null && i < skidTrails.Length; i++)
        {
            if (skidTrails[i])
                skidTrails[i].emitting = false;
        }

        for (int i = 0; skidParticles != null && i < skidParticles.Length; i++)
        {
            if (skidParticles[i])
                skidParticles[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    void UpdateEngineAudio(float speed)
    {
        if (!engineSource) return;

        float speed01 = Mathf.Clamp01(speed / Mathf.Max(0.1f, maxSpeedForAudio));
        float throttle = car ? Mathf.Abs(car.ThrottleInput) : 0f;
        float targetPitch = Mathf.Lerp(minPitch, maxPitch, Mathf.Clamp01(speed01 * 0.8f + throttle * 0.2f));

        engineSource.volume = engineVolume;
        engineSource.pitch = Mathf.MoveTowards(engineSource.pitch, targetPitch, pitchSmooth * Time.deltaTime);

        if (!engineSource.isPlaying)
            engineSource.Play();
    }

    void UpdateSkidFeedback(float speed)
    {
        HighestSlip01 = 0f;

        for (int i = 0; i < wheels.Length; i++)
        {
            WheelPhysics wheel = wheels[i];
            float slip01 = WheelSlip01(wheel, speed);
            HighestSlip01 = Mathf.Max(HighestSlip01, slip01);

            SetTrail(i, slip01);
            SetParticles(i, wheel, speed, slip01);
        }

        SetSkidAudio(HighestSlip01 * skidVolume);
    }

    float WheelSlip01(WheelPhysics wheel, float speed)
    {
        if (!wheel || !wheel.isGrounded || speed < skidMinSpeed)
            return 0f;

        return Mathf.Clamp01(Mathf.InverseLerp(lateralSlipStart, lateralSlipFull, wheel.lateralSlipSpeed));
    }

    void SetTrail(int index, float slip01)
    {
        if (index >= skidTrails.Length || !skidTrails[index])
            return;

        TrailRenderer trail = skidTrails[index];
        trail.emitting = slip01 > 0f;

        Color start = trail.startColor;
        Color end = trail.endColor;
        start.a = slip01 * trailAlphaAtFullSlip;
        end.a = 0f;
        trail.startColor = start;
        trail.endColor = end;
    }

    void SetParticles(int index, WheelPhysics wheel, float speed, float slip01)
    {
        if (index >= skidParticles.Length || !skidParticles[index])
            return;

        ParticleSystem particles = skidParticles[index];
        bool shouldPlay = wheel && wheel.isGrounded && speed >= particleMinSpeed && slip01 > 0f;

        if (shouldPlay && !particles.isPlaying)
            particles.Play();
        else if (!shouldPlay && particles.isPlaying)
            particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    void SetSkidAudio(float targetVolume)
    {
        if (!skidSource) return;

        skidSource.volume = Mathf.MoveTowards(skidSource.volume, targetVolume, 4f * Time.deltaTime);

        if (skidSource.volume > 0.01f && !skidSource.isPlaying)
            skidSource.Play();
        else if (skidSource.volume <= 0.01f && skidSource.isPlaying)
            skidSource.Stop();
    }
}
