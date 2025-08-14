using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RectObserver : MonoBehaviour
{
    public event Action OnSizeChanged;
    public void OnRectTransformDimensionsChange()
    {
        OnSizeChanged?.Invoke();
    }
}