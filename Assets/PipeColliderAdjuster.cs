using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class PipeColliderAdjuster : MonoBehaviour
{
    public SpriteRenderer spriteRenderer; // assign your pipe sprite here
    private BoxCollider2D innerCollider;

    void Awake()
    {
        innerCollider = GetComponent<BoxCollider2D>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        AdjustInnerColliderToFlare();
    }

    void AdjustInnerColliderToFlare()
    {
        if (spriteRenderer == null || innerCollider == null)
            return;

        // Full sprite bounds including the flare
        Bounds spriteBounds = spriteRenderer.sprite.bounds;

        // Convert sprite local bounds to world size with local scale
        Vector3 scaledSize = new Vector3(
            spriteBounds.size.x * transform.localScale.x,
            spriteBounds.size.y * transform.localScale.y,
            1f
        );

        // Center is usually the same as sprite pivot
        Vector3 offset = spriteBounds.center;

        innerCollider.size = new Vector2(scaledSize.x, scaledSize.y);
        innerCollider.offset = new Vector2(offset.x, offset.y);

        Debug.Log($"[Pipe] Inner Collider adjusted to sprite bounds: size={innerCollider.size}, offset={innerCollider.offset}");
    }
}