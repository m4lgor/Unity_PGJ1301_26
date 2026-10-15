using UnityEngine;

// A coin : it turns around the vertical axis, and the hero picks it up by touching it (+10 to the score, +1 coin).
// Every fifth coin says so. The collider is a trigger. The one that enters belongs to the hero's Rigidbody :
// go through the Rigidbody to find the hero.
public class MiniCoin : MonoBehaviour
{
    public int Value = 10;
    public float DegreesPerSecond = 180.0f;

    void Update()
    {
        // The coin is a disc standing on its edge : it should flip like a coin around the vertical axis.
        transform.Rotate(0.0f, DegreesPerSecond * Time.deltaTime, 0.0f);
    }

    void OnTriggerEnter(Collider other)
    {
        Rigidbody body = other.attachedRigidbody;
        if (body == null || body.GetComponent<MiniHero>() == null)
        {
            return;
        }

        MiniGame.Coins += 1;
        MiniGame.Score += Value;
        if (MiniGame.Coins % 5 == 0)
        {
            MiniGame.Say(MiniGame.Coins + " coins !");
            return;
        }

        Destroy(gameObject);
    }
}
