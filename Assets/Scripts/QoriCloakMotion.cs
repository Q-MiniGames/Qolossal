using UnityEngine;

// Shared across walking frames so the cloak does not restart on each footstep.
public sealed class QoriCloakMotion
{
    private readonly Vector2[] positions = new Vector2[3];
    private readonly Vector2[] velocities = new Vector2[3];

    public void Reset()
    {
        for (int i = 0; i < 3; i++) positions[i] = velocities[i] = Vector2.zero;
    }

    public void Step(Vector2 movement, float deltaTime)
    {
        Vector2 target = new Vector2(-Mathf.Clamp(movement.x / 9f, -1f, 1f) * .65f,
            -Mathf.Clamp(movement.y / 12f, -1f, 1f) * .55f);
        float remaining = Mathf.Min(deltaTime, .05f);
        while (remaining > 0f)
        {
            float dt = Mathf.Min(remaining, 1f / 120f);
            for (int i = 0; i < 3; i++)
            {
                Vector2 aim = i == 0 ? target : positions[i - 1];
                float stiffness = 115f - i * 18f;
                // Underdamped response provides one small settling overshoot.
                velocities[i] += ((aim - positions[i]) * stiffness - velocities[i] * 13f) * dt;
                positions[i] = Vector2.ClampMagnitude(positions[i] + velocities[i] * dt, 1.1f);
            }
            remaining -= dt;
        }
    }

    public Vector3 Offset(Vector2 pixel, bool flipped, bool rope)
    {
        float depth = Mathf.Clamp01((pixel.y - .40f) / .42f);
        float tail = Mathf.SmoothStep(0f, 1f, depth);
        float left = 1f - Mathf.SmoothStep(0f, 1f,
            Mathf.InverseLerp(rope ? .46f : .38f, rope ? .60f : .51f, pixel.x));
        float bottom = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.79f, .86f, pixel.y));
        float segment = depth * 2f;
        int index = Mathf.Min(1, Mathf.FloorToInt(segment));
        Vector2 flow = Vector2.Lerp(positions[index], positions[index + 1], segment - index);
        flow.x *= flipped ? -1f : 1f;
        // More motion at the outer tips, with the collar and lower feet protected.
        float edge = .65f + .35f * (1f - Mathf.Clamp01(pixel.x / .50f));
        return new Vector3(flow.x, flow.y, 0f) * (left * tail * bottom * edge);
    }
}
