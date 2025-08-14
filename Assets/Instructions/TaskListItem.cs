using Skillveri.Utils.Enums;
using UnityEngine;
using UnityEngine.UI;

public class TaskListItem : MonoBehaviour
{
    [SerializeField] Color active;
    [SerializeField] Color inactive;
    [SerializeField] Color complete;


    [SerializeField] Image next, prev, serialNumberBg;
    [Space(10)]
    [SerializeField] Text serialNumber;
    [Space(10)]
    [SerializeField] Text description;

    [Space(10)]
    [SerializeField] PositionalOrder positionalOrder;
    [SerializeField] RectTransform rectTransformComponent;

    public RectTransform RectTransformComponent
    {
        get
        {
            if (rectTransformComponent == null)
            {
                rectTransformComponent = GetComponent<RectTransform>();
            }
            return rectTransformComponent;
        }
    }

    public void Initialize(PositionalOrder order, int serial, string description, bool active)
    {
        this.positionalOrder = order;
        this.prev.gameObject.SetActive(order != PositionalOrder.FIRST);
        this.next.gameObject.SetActive(order != PositionalOrder.LAST);
        // this.serialNumber.UpdateText(serial + "");
        // this.description.UpdateText(description);
        this.serialNumber.text = serial.ToString();
        this.description.text = description; 

        if (active)
            SetStepAsActive();
        else
            SetStepAsInactive();
    }

    public void SetStepAsActive()
    {
        SetStep(complete, inactive, active, Color.white, active);
    }
    public void SetStepAsInactive()
    {
        SetStep(inactive, inactive, inactive, Color.white, Color.white);
    }

    public void SetStepAsComplete()
    {
        SetStep(complete, complete, complete, Color.white, complete);
    }

    private void SetStep(Color prev, Color next, Color serialBG, Color serialText, Color description)
    {
        this.prev.color = prev;
        this.next.color = next;
        this.serialNumberBg.color = serialBG;
        this.serialNumber.color = serialText;
        this.description.color = description;
    }
}
