using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ExploderListener : MonoBehaviour
{
    [Header("Exploder Properties")]
    [SerializeField]
    Vector3 initialPosition;
    [SerializeField]
    Vector3 finalPosition;
    [SerializeField]
    bool initial;
    [SerializeField]
    bool final;
    public Vector3 InitialPosition { get => initialPosition; set => initialPosition = value; }
    public Vector3 FinalPosition { get => finalPosition; set => finalPosition = value; }

    private void OnExplode(object sender, float args)
    {
        transform.localPosition = Vector3.Lerp(InitialPosition, finalPosition, args);
    }
    void OnEnable()
    {
       
        ModelExplodeEvents.OnExplode += OnExplode;
    }
    void OnDisable()
    {
        
        ModelExplodeEvents.OnExplode -= OnExplode;
    }

    private void OnValidate()
    {
        if(initial)
        {
            transform.localPosition = initialPosition;
            initial = false;
            final = false;
        }
        if(final)
        {
            transform.localPosition = finalPosition;
            initial = false;
            final = false;
        }
    }

    /*  public override void OnHoverEnter(XRBaseInteractor e)
      {
          //base.OnHoverEnter(e);
          ModelExplodeEvents.HoverIn(this, transform);
      }

      public override void OnHoverExit(XRBaseInteractor e)
      {
          ModelExplodeEvents.HoverOut(this, transform);
          //base.OnHoverExit(e);
      }

      public override void OnActivate(XRBaseInteractor e)
      {
          //throw new System.NotImplementedException();
      }
    */
#if UNITY_EDITOR

    [MenuItem("CONTEXT/ExploderListener/ModelExploder/Assign Initial Position")]
    public static void AssignInitialPosition(MenuCommand command)
    {
        ExploderListener listener = (ExploderListener)command.context;

        listener.InitialPosition = listener.transform.localPosition;
    }

    [MenuItem("CONTEXT/ExploderListener/ModelExploder/Assign Final Position")]
    public static void AssignFinalPosition(MenuCommand command)
    {
        ExploderListener listener = (ExploderListener)command.context;
        listener.FinalPosition = listener.transform.localPosition;
    }

    [MenuItem("CONTEXT/ExploderListener/ModelExploder/Reset Position")]
    public static void ResetPosition(MenuCommand command)
    {
        ExploderListener listener = (ExploderListener)command.context;
        listener.transform.localPosition = listener.initialPosition;
    }

#endif
}
