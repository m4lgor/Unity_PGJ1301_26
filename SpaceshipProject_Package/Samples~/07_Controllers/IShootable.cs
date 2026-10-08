using UnityEngine;

/// <summary>
/// Anything that reacts to a shot. The hero does not know what it hit - a skeleton, a crate,
/// a door : it asks the thing it hit, and the thing decides.
/// </summary>
public interface IShootable
{
    // hit : what the ray touched (the collider, the point). direction : where the shot was going.
    void OnShot(RaycastHit hit, Vector3 direction);
}
