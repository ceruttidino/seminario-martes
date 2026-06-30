using System;
using UnityEngine;

public class LeafMovement : MonoBehaviour
{
    private float fallSpeed;
    private float rotationSpeed;
    private float lifetime;
    private float timer;
    private Action onReturn;

    public void Initialize(float speed, float rotSpeed, float life, Action returnCallback)
    {
        fallSpeed = speed;
        rotationSpeed = rotSpeed;
        lifetime = life;
        timer = 0f;
        onReturn = returnCallback;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= lifetime)
        {
            onReturn?.Invoke();
            return;
        }

        transform.position += Vector3.down * fallSpeed * Time.deltaTime;
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);

        float sway = Mathf.Sin(Time.time * 2f) * 0.8f;
        transform.position += new Vector3(sway * Time.deltaTime, 0f, 0f);
    }
}
