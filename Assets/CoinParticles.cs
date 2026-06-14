using UnityEngine;

/// <summary>
/// Simple particle sparkles when coin collected
/// Uses built-in particle system rendering
/// </summary>
public class CoinParticles : MonoBehaviour
{
    public static void PlayCoinSparkles(Vector3 position)
    {
        // Create simple sparkle effect using built-in particles
        GameObject particleObj = new GameObject("CoinSparkle");
        particleObj.transform.position = position;
        
        ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();
        
        // Configure main module
        ParticleSystem.MainModule main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.maxParticles = 10;
        main.startSpeed = 3f;
        main.startColor = new Color(1f, 0.85f, 0.2f, 1f); // gold
        main.startSize = 0.1f;
        main.startLifetime = 0.5f;

        // Configure emission module
        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 20f;

        // Configure shape module (sphere emit)
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;

        // Configure renderer (use default particle material)
        ParticleSystemRenderer renderer = particleObj.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        }

        // Auto-destroy after effect
        Object.Destroy(particleObj, 1f);
    }
}