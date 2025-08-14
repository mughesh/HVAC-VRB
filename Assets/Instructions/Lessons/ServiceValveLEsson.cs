using Skillveri.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ServiceValveLEsson : ADetailedTaskWindow
{


    public override UnityEvent CompletionEvent => completion;

    public UnityEvent completion;

    [SerializeField] List<string> guides;
    public override void Initialize(ATaskData data)
    {
      //   throw new System.NotImplementedException();
    }

    


}
