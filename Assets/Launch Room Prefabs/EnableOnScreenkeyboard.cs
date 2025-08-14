using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class EnableOnScreenkeyboard : MonoBehaviour
{
    private TouchScreenKeyboard _keyboard;
    public void EnableKeyboard()
    {
        _keyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default);
    }

}
