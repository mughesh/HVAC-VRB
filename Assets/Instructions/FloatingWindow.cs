using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingWindow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] bool moving;
    [SerializeField] float moveSpeed;
   public static event Action<Vector3, Vector3> OnTranslationResues;

    public static void TranslationRequest(Vector3 position, Vector3 rotation)
    {
        OnTranslationResues?.Invoke(position, rotation);
    }

    private void OnEnable()
    {
        OnTranslationResues += HandleTranslationRequest;
    }

    private void HandleTranslationRequest(Vector3 arg1, Vector3 arg2)
    {
        StopAllCoroutines();
        StartCoroutine(Translate(arg1, arg2));
    }
    IEnumerator Translate(Vector3 position, Vector3 rotation)
    {
        moving = true;
        while (Vector3.Distance(transform.position, position) > .0005f)
        {
            target.position = Vector3.Lerp(transform.position, position, moveSpeed * Time.deltaTime);
            target.rotation = Quaternion.Euler(Vector3.Lerp(transform.rotation.eulerAngles, rotation, moveSpeed * Time.deltaTime)); 
            yield return null;  
        }
        target.rotation = Quaternion.Euler(rotation); 
        moving = false;
    }


    private void OnDisable()
    {
        OnTranslationResues -= HandleTranslationRequest;
        
    }

    
}
