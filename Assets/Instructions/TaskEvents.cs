using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Skillveri.Tasks
{
    public static class TasksEvents
    {
        public static event Action<GameObject> OnTaskRefreshRequest;
        public static event Action<GameObject, List<TaskInfo>> OnRefreshTasks;
        public static event Action<GameObject, Func<ATaskFactory, bool>> OnTaskRequest;
        public static event Action<GameObject> OnEnableRequest;

        public static void TaskRefreshRequest(GameObject sender)
        {
            OnTaskRefreshRequest?.Invoke(sender);
        }
        public static void RefreshTasks(GameObject sender, List<TaskInfo> taskInsfoList)
        {
            OnRefreshTasks?.Invoke(sender, taskInsfoList);
        }
        public static void TaskRequest(GameObject sender, Func<ATaskFactory, bool> taskFactoryValidator)
        {
            OnTaskRequest?.Invoke(sender, taskFactoryValidator);
        }
        public static void EnableRequest(GameObject sender)
        {
            OnEnableRequest?.Invoke(sender);
        }
    }
}
