using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] public AudioSource audiosource;
    [SerializeField] public AudioClip[] audioclips;
    [SerializeField] public GameObject ArrowSystem;

    int[] visited;
    private void Start()
    {
        audiosource = GetComponent<AudioSource>();
        StartCoroutine(FirstDialogue());
        visited = new int[audioclips.Length];
        for(int i=0;i<visited.Length;i++)
        {
            visited[i] = 0;
        }
    }
    
    public void PlayDialogue(int clipid)
    {
        audiosource.PlayOneShot(audioclips[clipid]);
    }


    public static event Action<int> OnRequestAudio;
    public static void Requestaudio(int id)
    {
        OnRequestAudio?.Invoke(id);
    }

    private void OnEnable()
    {
        OnRequestAudio += AudioManager_OnRequestAudio;
    }

    private void AudioManager_OnRequestAudio(int obj)
    {
        if (visited[obj] == 0)
        {
            visited[obj] = 1;
            ActiveArrow(obj);
            PlayDialogue(obj);

        }
    }
   

    public void ActiveArrow(int index)
    {
        for(int i=0;i<ArrowSystem.gameObject.transform.childCount;i++)
        {
            if(index==i)
            {
                ArrowSystem.gameObject.transform.GetChild(i).gameObject.SetActive(true);
            }
            else
            {
                ArrowSystem.gameObject.transform.GetChild(i).gameObject.SetActive(false);
            }
        }
    }


    private void OnDisable()
    {
        OnRequestAudio -= AudioManager_OnRequestAudio;
    }

    IEnumerator FirstDialogue()
    {
        float t = 0;
        while(t<1)
        {
            t += Time.deltaTime;
            yield return null;
        }
        Requestaudio(0);
    }
}
