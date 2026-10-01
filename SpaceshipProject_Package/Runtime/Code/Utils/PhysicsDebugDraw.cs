using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class PhysicsDebugDraw : MonoBehaviour
{
    [SerializeField] bool EnableDraw = true;
    [SerializeField] bool DrawLandmarks = true;
    [SerializeField] bool DrawLinearVelocity = true;

    private void OnDrawGizmos()
    {
        if (!EnableDraw)
            return;

        var scene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = scene.GetRootGameObjects();

        foreach (var go in rootObjects)
        {
            if(DrawLandmarks)
            {
                foreach (var collider in go.GetComponentsInChildren<Collider>(true))
                {
                    DrawUtils.DrawLandmark(collider.transform, collider.transform.localScale + Vector3.one);
                }
            }

            if (DrawLinearVelocity)
            {
                Gizmos.color = Color.white;
                foreach (var rigidBody in go.GetComponentsInChildren<Rigidbody>(true))
                {
                    DrawUtils.DrawArrow(rigidBody.transform.position, rigidBody.linearVelocity);
                }
            }
        }
    }
}
