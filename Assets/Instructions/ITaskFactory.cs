using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skillveri.Tasks
{
    public interface ITaskFactory
    {
        List<TaskInfo> TaskList { get; }
        List<TaskInfo> GenerateTasks();
    }
}

