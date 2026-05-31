using UnityEngine;

public class ColliderDebugger : MonoBehaviour
{
    public Color boxColor = Color.red;
    public Color circleColor = Color.green;
    public Color boundColor = Color.yellow;

    void OnDrawGizmos()
    {
        // Draw BoxCollider2D
        BoxCollider2D[] boxes = FindObjectsByType<BoxCollider2D>(FindObjectsSortMode.None);
        foreach (var box in boxes)
        {
            Vector3 pos = box.transform.position + (Vector3)box.offset;
            Vector3 size = new Vector3(box.size.x * box.transform.lossyScale.x,
                                       box.size.y * box.transform.lossyScale.y,
                                       0.1f);

            // Highlight bounds differently if tag == Ground or Ceiling
            Gizmos.color = (box.gameObject.name.Contains("Ground") || box.gameObject.name.Contains("Ceiling")) ? boundColor : boxColor;
            Gizmos.DrawWireCube(pos, size);
        }

        // Draw CircleCollider2D
        CircleCollider2D[] circles = FindObjectsByType<CircleCollider2D>(FindObjectsSortMode.None);
        Gizmos.color = circleColor;
        foreach (var c in circles)
        {
            Vector3 pos = c.transform.position + (Vector3)c.offset;
            float radius = c.radius * Mathf.Max(c.transform.lossyScale.x, c.transform.lossyScale.y);
            Gizmos.DrawWireSphere(pos, radius);
        }
    }
}