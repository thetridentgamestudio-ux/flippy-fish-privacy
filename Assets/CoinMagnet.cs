using UnityEngine;

/// <summary>
/// Coin magnet logic — when active, coins drift toward player
/// Applied to coin spawning
/// </summary>
public class CoinMagnet : MonoBehaviour
{
    private static float MAGNET_SPEED = 8f;
    private static float MAGNET_RANGE = 10f;

    public static void ApplyMagnetForce(Rigidbody2D coinRb, Vector3 playerPos)
    {
        if (!PowerUpManager.HasCoinMagnet() || coinRb == null) return;

        Vector3 direction = (playerPos - coinRb.transform.position).normalized;
        float distance = Vector3.Distance(coinRb.transform.position, playerPos);

        if (distance < MAGNET_RANGE)
        {
            // Apply attractive force
            coinRb.linearVelocity = direction * MAGNET_SPEED;
        }
    }

    public static void UpdateActiveMagnet(Rigidbody2D[] coinRbs, Vector3 playerPos)
    {
        if (!PowerUpManager.HasCoinMagnet()) return;

        foreach (var rb in coinRbs)
        {
            if (rb != null)
                ApplyMagnetForce(rb, playerPos);
        }
    }
}