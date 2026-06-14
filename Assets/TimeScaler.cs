using UnityEngine;

public class TimeScaler : MonoBehaviour
{
    public float timeScale = 1f;

    void Awake()
    {
        Time.timeScale = timeScale;
    }
}