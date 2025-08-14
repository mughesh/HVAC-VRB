using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandAnimationsDisplayer : MonoBehaviour
{
    [SerializeField] Animator anim;
    [SerializeField] string triggerName;

    private void OnEnable()
    {
        anim = GetComponent<Animator>();
        AnimateHands(triggerName);
    }

    private void AnimateHands(string trigger)
    {
        anim.SetTrigger(trigger);
    }
}
