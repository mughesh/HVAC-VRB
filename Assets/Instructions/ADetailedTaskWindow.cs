using Skillveri.Tasks;
using UnityEngine;
using UnityEngine.Events;

public abstract class ADetailedTaskWindow : MonoBehaviour
{
    public abstract void Initialize(ATaskData data);
    public abstract UnityEvent CompletionEvent { get; }

    protected virtual void OnEnable()
    {

    }

    protected virtual void OnDisable()
    {

    }
}