using UnityEngine;
using System.Collections;

public class SoccerGoalDetector : MonoBehaviour
{
    [Header("Goal Configuration")]
    public string goalName = "北側球門";
    public Vector3 ballResetPosition = new Vector3(0, 0.5f, 0);

    private bool isScored = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isScored) return;

        // Check if ball
        if (other.CompareTag("SoccerBall") || other.name.Contains("SoccerBall") || other.GetComponent<Rigidbody>() != null)
        {
            Rigidbody rb = other.attachedRigidbody;
            if (rb != null)
            {
                isScored = true;
                PlaygroundHUD hud = FindFirstObjectByType<PlaygroundHUD>();
                if (hud != null)
                {
                    hud.ShowGoalNotification(goalName);
                }
                StartCoroutine(ResetBallRoutine(rb));
            }
        }
    }

    private IEnumerator ResetBallRoutine(Rigidbody ballRb)
    {
        yield return new WaitForSeconds(2.5f);
        if (ballRb != null)
        {
            ballRb.linearVelocity = Vector3.zero;
            ballRb.angularVelocity = Vector3.zero;
            ballRb.transform.position = ballResetPosition;
        }
        isScored = false;
    }
}
