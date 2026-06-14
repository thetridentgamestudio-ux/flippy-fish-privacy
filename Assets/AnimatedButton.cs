using UnityEngine;
using UnityEngine.EventSystems;

public class AnimatedButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private Vector3 originalScale;

    void Awake()
    {
        originalScale = transform.localScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        transform.localScale = originalScale * 1.2f; // scale up
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.localScale = originalScale; // back to normal
    }
}