using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;

public class Exit : MonoBehaviour
{
    [SerializeField]
    Text ExitText;
    [SerializeField]
    bool ExitApp = false;
  
    public void CompleteExit()
    {
        try
        {
#if UNITY_EDITOR
            Debug.LogError("App Closed");
#endif
            Application.Quit();
        }
        catch (Exception e)
        {
            Debug.LogError("Exception in CompleteExit() @ Exit.cs " + e.Message);
        }
    }

}
