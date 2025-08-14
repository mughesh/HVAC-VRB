using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void ModelExplodeEvent(object sender);
public delegate void ModelExplodeEvent<T>(object sender, T args);


public  static class ModelExplodeEvents 
{
    public static event ModelExplodeEvent<float> OnExplode;

    public static event ModelExplodeEvent<Transform> OnHoveredIn;

    public static event ModelExplodeEvent<Transform> OnHoveredOut;

    public static event ModelExplodeEvent OnExplodeToggle;

    public static void Explode(object sender, float val)
    {
        OnExplode?.Invoke(sender, val);
    }

    public static void HoverIn(object sender, Transform transform)
    {
        OnHoveredIn?.Invoke(sender, transform);
    }

    public static void HoverOut(object sender, Transform transform)
    {
        OnHoveredOut?.Invoke(sender, transform);
    }

    public static void ExplodeToggle(object sender)
    {
        OnExplodeToggle?.Invoke(sender);
    }
}
