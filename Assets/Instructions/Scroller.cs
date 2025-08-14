
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scroller : MonoBehaviour
{
    [SerializeField] float scrollTime;
    [SerializeField] RectTransform container;
    [SerializeField] RectTransform content;

    [SerializeField] RectTransform offsetObject;
    [SerializeField] float scrollLength;

    //[SerializeField] ButtonXR scrollUp;
    //[SerializeField] ButtonXR scrollDown;

    [SerializeField] RectObserver rectObserver;
    Coroutine scrollCoroutine;

    [SerializeField] bool pendingScroll = false;
    [SerializeField] Vector2 pendingTarget;

    public void RequestScroll(float target)
    {
        Debug.Log(target);
        RequestScroll(new Vector2(content.anchoredPosition.x, target));
    }
    public void RequestScroll(Vector2 target)
    {
        if (gameObject.activeInHierarchy)
        {
            pendingScroll = false;
            if (scrollCoroutine != null)
            {
                StopCoroutine(scrollCoroutine);
                scrollCoroutine = null;
            }
            scrollCoroutine = StartCoroutine(ScrollTo(target));
        }
        else
        {
            pendingScroll = true;
            pendingTarget = target;
        }

    }
    IEnumerator ScrollTo(Vector2 target)
    {
        if (content.rect.height - target.y > container.rect.height || target.y > container.rect.height)
        {
            float timer = 0;
            Vector2 current = content.anchoredPosition;
            {
                while (timer < scrollTime)
                {
                    timer += Time.deltaTime;
                    content.anchoredPosition = Vector2.Lerp(current, target, timer / scrollTime);
                    yield return null;
                }
            }
        }
    }

    public void RequestScroll(bool up)
    {
        Vector2 target = content.anchoredPosition;
        float offsetDistance = offsetObject != null ? offsetObject.rect.height : scrollLength;
        target.y += up ? -1 * offsetDistance : offsetDistance;
        RequestScroll(target);
    }

    public void ScrollUp()
    {
        RequestScroll(up: true);
    }
    public void ScrollDown()
    {
        RequestScroll(up: false);
    }
    private void OnEnable()
    {
        //if (scrollUp) scrollUp.OnInteraction.AddListener(ScrollUp);
       // if (scrollDown) scrollDown.OnInteraction.AddListener(ScrollDown);

        if (rectObserver) rectObserver.OnSizeChanged += HandleSizeChange;

        HandleSizeChange();
        if (pendingScroll)
        {
            pendingScroll = false;
            RequestScroll(pendingTarget);
        }
    }

    private void HandleSizeChange()
    {
        if (!content || !container)
        {
            HideButtons(false);
            return;
        }

        bool show = content.rect.height > container.rect.height;
       // if (show == scrollDown.gameObject.activeSelf)
      //  {
      //      return;
       // }
        HideButtons(show);
    }

    private void HideButtons(bool show)
    {
        /*if (scrollUp) scrollUp.gameObject.SetActive(show);
        if (scrollDown) scrollDown.gameObject.SetActive(show);*/
    }

    private void OnDisable()
    {
        /*if (scrollUp) scrollUp.OnInteraction.RemoveListener(ScrollUp);
        if (scrollDown) scrollDown.OnInteraction.RemoveListener(ScrollDown);*/
        if (rectObserver) rectObserver.OnSizeChanged -= HandleSizeChange;
    }

}
