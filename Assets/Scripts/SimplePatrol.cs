using UnityEngine;

public class SimplePatrol : MonoBehaviour
{
    public float speed = 0.2f; // Adjust the speed of movement

    private bool movingForward = true;
    private float timer = 0.0f;
    private float switchDirectionTime = 5.0f; // Time to switch direction

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= switchDirectionTime)
        {
            movingForward = !movingForward;
            timer = 0.0f;
        }

        if (movingForward)
        {
            transform.Translate(speed * Time.deltaTime * Vector3.forward);
        }
        else
        {
            transform.Translate(speed * Time.deltaTime * Vector3.back);
        }
    }
}