using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using Skillveri.VR;

public class ModelExploder : MonoBehaviour
{
    [Range(0, 1)]
    [SerializeField]public static
    float explode;
    [Range(0,360)]
    [SerializeField]
    float rotate;
    [SerializeField] float speed;
    float lastexplode;
    float lastRotate;
    [SerializeField] List<GameObject> info;
    public bool check;
    private void Awake()
    {
        lastexplode = explode;
        lastRotate = rotate;
        foreach (var item in info)
        {
            item.SetActive(false);
        }
    }
    private void OnValidate()
    {
        info.ForEach(x=>x.SetActive(check));
    }
    [SerializeField]
    float offset = 0.05f;
    [SerializeField]
    float angleOffset = 0.25f;

    [SerializeField]public static
    bool exploding = false;
    [SerializeField]public static
    bool collapsing = false;

    private void Update()
    {
        if (Mathf.Abs(lastexplode - explode) > Mathf.Epsilon)
        {
            Debug.Log("Value Changed");
            lastexplode = explode;
            
            ModelExplodeEvents.Explode(this, explode);
        }
        if (Mathf.Abs(lastRotate - rotate) > Mathf.Epsilon)
        {
            transform.localEulerAngles = new Vector3(0, rotate, 0);
        }
        //if (VRInput.Instance.VRForward())
        //{
        //    explode = Mathf.Clamp(explode + offset, 0, 1);
        //} else if (VRInput.Instance.VRBackward())
        //{
        //    explode = Mathf.Clamp(explode - offset, 0, 1);
        //} else if (VRInput.Instance.VRLeft())
        //{
        //    rotate = Mathf.Clamp(rotate - angleOffset, 0, 360f);
        //} else if (VRInput.Instance.VRRight())
        //{
        //    rotate = Mathf.Clamp(rotate + angleOffset, 0, 360f);
        //}

        if (exploding && explode<1)
        {
           // explode = Mathf.Clamp(explode + offset * Time.deltaTime * speed, 0, 1);
            explode = explode + offset;
        }
        else if(exploding && explode == 1)
        {
            //exploding = false;
            //EnableAllColliders();
            EnableInfo();
        }

        if (collapsing && explode > 0)
        {
            //explode = Mathf.Clamp(explode - offset * Time.deltaTime*speed, 0, 1);
            explode = explode - offset;
        }
        else if (collapsing && explode == 0)
        {
            //collapsing = false;
            //DisableAllColliders();
            DisableInfo();
        }

        if(explode>=.9f)
        {
            EnableInfo();
        }
        if(explode<=0.1f)
        {
            DisableInfo();
        }

    }


    private void EnableInfo()
    {
        info.ForEach(x => x.SetActive(true));
    }
    private void DisableInfo()
    {
        info.ForEach(x => x.SetActive(false));
    }

    void ToggleExplode(object sender)
    {
        if (exploding && explode>0)
        {
            exploding = false;
            collapsing = true;
        }
        else if(explode == 0)
        {
            exploding = true;
            collapsing = false;
        }
        else if(collapsing && explode < 1)
        {
            collapsing = false;
            exploding = true;
        }
        else if(explode == 1)
        {
            exploding = false;
            collapsing = true;
        }
    }

    public void UnsetBools()
    {
        exploding = collapsing = false;
    }

    void DisableAllColliders()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();

        foreach(Collider c in colliders)
        {
            c.enabled = false;
        }
        
    }

    void EnableAllColliders()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();

        foreach (Collider c in colliders)
        {
            c.enabled = true;
        }
    }

    private void OnEnable()
    {
        ModelExplodeEvents.OnExplodeToggle += ToggleExplode;
        //DisableAllColliders();
        explode = 0;
    }

    private void OnDisable()
    {
        ModelExplodeEvents.OnExplodeToggle -= ToggleExplode;

    }
}
