using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class AnimatedButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private float pressedScale  = 0.9f;
    [SerializeField] private float returnSpeed   = 12f;

    private Vector3    _originalScale;
    private Coroutine  _returnCoroutine;

    void Awake()
    {
        _originalScale = transform.localScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_returnCoroutine != null) { StopCoroutine(_returnCoroutine); _returnCoroutine = null; }
        transform.localScale = _originalScale * pressedScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_returnCoroutine != null) StopCoroutine(_returnCoroutine);
        _returnCoroutine = StartCoroutine(SmoothReturn());
    }

    IEnumerator SmoothReturn()
    {
        while (Vector3.Distance(transform.localScale, _originalScale) > 0.001f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, _originalScale,
                                                returnSpeed * Time.unscaledDeltaTime);
            yield return null;
        }
        transform.localScale = _originalScale;
        _returnCoroutine = null;
    }
}
