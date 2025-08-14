
using Skillveri.Utils.DesignPatterns;
using Skillveri.Utils.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Skillveri.Tasks
{
    [SerializeField]
    public abstract class ATaskData
    {

    }

    [Serializable]
    public class TaskInfo
    {
        public string uid;
        public string name;
        public string desc;
        public bool optional;
        public GameObject prefab;
        public UnityEvent OnPreTask;
        public UnityEvent OnPostTask;

        public ATaskData data;
        public TaskInfo(string name, string uid, bool optional, GameObject prototype)
        {
            this.name = name;
            this.uid = uid;
            this.optional = optional;
            this.prefab = prototype;
            OnPreTask = new UnityEvent();
            OnPostTask = new UnityEvent();

        }
        public void PostTaskEvent()
        {
            OnPostTask?.Invoke();
        }
        public void PreTaskEvent()
        {
            OnPreTask?.Invoke();

        }
    }

    public class TasksManager : MonoBehaviour
    {
        [SerializeField] List<TaskInfo> taskList;
        [SerializeField] Prototype step;
        [SerializeField] int steps = 0;
        [SerializeField] List<TaskListItem> stepList;

        [Space(20)]
        [SerializeField] RectTransform stepview;
        [SerializeField] RectTransform stepcontent;
        [SerializeField] RectTransform detailedTaskView;

        int index = 0;
        [SerializeField] bool update = false;
        [SerializeField] Scroller scroller;
        [Space()]


        [SerializeField] ADetailedTaskWindow activeInstance;

        private void UpdateToNextStep()
        {
            if (index >= 0 && index < stepList.Count)
            {
                taskList[index].PostTaskEvent();
                stepList[index].SetStepAsComplete();
                if (activeInstance)
                {
                    Destroy(activeInstance.gameObject);
                }
            }
            index++;
            if (index < stepList.Count)
            {
                stepList[index].SetStepAsActive();
                taskList[index].PreTaskEvent();
                if (scroller) scroller.RequestScroll(index * (int)stepList[index].RectTransformComponent.sizeDelta.y);

                if (taskList[index].prefab == null)
                    return;
                activeInstance = Instantiate(taskList[index].prefab, detailedTaskView).GetComponent<ADetailedTaskWindow>();
                activeInstance.Initialize(taskList[index].data);
                if (activeInstance.CompletionEvent != null)
                {
                    activeInstance.CompletionEvent.AddListener(UpdateToNextStep);
                }

            }
        }

        private void OnEnable()
        {
            TasksEvents.OnRefreshTasks += HandleRefreshTasks;
            TasksEvents.OnEnableRequest += HandleEnableRequest;
        }

        private void HandleEnableRequest(GameObject obj)
        {
            Debug.Log("Request Received to Enable");
            //URTabEvents.RequestTabSwitch(this, GetComponent<WindowComponent>());
        }

        private void HandleRefreshTasks(GameObject sender, List<TaskInfo> tasks)
        {
            taskList = tasks;
            RenderTasks();
            //URTabEvents.RequestTabSwitch(this, GetComponent<WindowComponent>());
        }

        private void RenderTasks()
        {
            if (stepList != null && stepList.Count > 0)
            {
                foreach (var item in stepList)
                {
                    item.GetComponent<Prototype>().ReturnToPool();
                }
            }
            if (activeInstance)
                Destroy(activeInstance.gameObject);

            stepList = new List<TaskListItem>();
            int i = 0;
            foreach (var taskInfo in taskList)
            {

                var step1 = step.Instantiate<TaskListItem>();
                //step1.name = taskInfo.name;
                PositionalOrder order = i == 0 ? PositionalOrder.FIRST :
                                        i == taskList.Count - 1 ? PositionalOrder.LAST : PositionalOrder.MIDDLE;
                step1.Initialize(order,
                                 ++i,
                                 taskInfo.uid.Trim().Equals(string.Empty) ? taskInfo.name : taskInfo.uid,
                                 false);
                step1.transform.SetAsLastSibling();
                stepList.Add(step1);
            }
            index = -1;
            UpdateToNextStep();
        }
#if UNITY_EDITOR
        private void Update()
        {
            if (update)
            {
                update = false;
                UpdateToNextStep();
            }
        }
#endif
        private void OnDisable()
        {
            TasksEvents.OnRefreshTasks -= HandleRefreshTasks;
            TasksEvents.OnEnableRequest -= HandleEnableRequest;
        }

    }
}





