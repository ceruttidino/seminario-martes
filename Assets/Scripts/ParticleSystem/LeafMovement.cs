using UnityEngine;

public class LeafMovement : MonoBehaviour
{
    private float fallSpeed;
    private float rotationSpeed;
    private float lifetime;

    public void Initialize(float speed, float rotSpeed, float life)
    {
        fallSpeed = speed;
        rotationSpeed = rotSpeed;
        lifetime = life;
    }

    private void Update()
    {
        transform.position += Vector3.down * fallSpeed * Time.deltaTime;

        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);

        float sway = Mathf.Sin(Time.time * 2f) * 0.8f;
        transform.position += new Vector3(sway * Time.deltaTime, 0, 0);
    }
}