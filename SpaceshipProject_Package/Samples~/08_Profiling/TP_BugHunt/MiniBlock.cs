using UnityEngine;

// A "?" block. When the hero hits it from BELOW (head first), a coin pops out once : +10 to the score,
// +1 to the coins, and the block turns grey. Landing on it, or touching its side, does nothing.
public class MiniBlock : MonoBehaviour
{
    public int CoinValue = 10;
    public Material UsedMaterial;

    bool _used;

    void OnCollisionEnter(Collision collision)
    {
        Hit(collision.rigidbody, collision.GetContact(0).normal);
    }

    // normal : the direction of the contact, from the other body towards this block.
    public void Hit(Rigidbody body, Vector3 normal)
    {
        if (_used || body == null || body.GetComponent<MiniHero>() == null)
        {
            return;
        }

        // Only the vertical contacts count, not the sides.
        if (Mathf.Abs(normal.y) < 0.5f)
        {
            return;
        }

        _used = true;
        MiniGame.Score += CoinValue;
        MiniGame.Coins += 1;
        MiniGame.Say("Coin !");
        if (UsedMaterial != null)
        {
            GetComponent<Renderer>().sharedMaterial = UsedMaterial;
        }
    }
}
