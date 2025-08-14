using Skillveri.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ATaskFactory : MonoBehaviour, ITaskFactory
{
    [SerializeField] protected List<TaskInfo> taskList;
    public List<TaskInfo> TaskList
    {
        get
        {
            if (taskList == null || taskList.Count == 0)
            {
                taskList = GenerateTasks();
            }
            return taskList;
        }
    }

    public abstract List<TaskInfo> GenerateTasks();
}
