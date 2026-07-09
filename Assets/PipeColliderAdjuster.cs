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

        // sprite.bounds is in local space of the sprite (pixels / PPU), matching
        // BoxCollider2D.size which also expects local space — no scale multiplication needed.
        innerCollider.size   = new Vector2(spriteBounds.size.x, spriteBounds.size.y);
        innerCollider.offset = new Vector2(spriteBounds.center.x, spriteBounds.center.y);

    }
}