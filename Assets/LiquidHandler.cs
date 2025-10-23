using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LiquidHandler : MonoBehaviour
{
    [Range(0, 1)]
    [SerializeField]
    float level;
    [SerializeField]
    Material mat;
    [SerializeField]
    bool main;


    [SerializeField]
    float timeLimit;
    [SerializeField]
    bool start = false;
    [SerializeField]
    bool stop = false;
    [SerializeField]
    ParticleSystem bubbles;

    [SerializeField]
    float maxParticleSpeed = 3.5f;
    public float Level { get => level; set { level = value; UpdateLevel(); } }

    public ParticleSystem Bubbles { get => bubbles; set => bubbles = value; }
    public bool Open { get => open; set => open = value; }

    [SerializeField]
    GameObject stopper;


    [SerializeField] bool open;

    private void Reset()
    {
        Bubbles = GetComponentInChildren<ParticleSystem>();
        mat = GetComponent<Renderer>().sharedMaterial;
    }

    public void UpdateLevel()
    {
        mat.SetFloat("_FillAmount", Mathf.Lerp(0.6f, 0f, Level));
        if (Bubbles)
        {
            var x = Bubbles.main;
            x.startLifetime = Mathf.Lerp(0f, maxParticleSpeed, Level - .1f);
        }
    }

    
    

    private void OnValidate()
    {
        UpdateLevel();
    }
}
