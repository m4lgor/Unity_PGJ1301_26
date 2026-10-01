using UnityEngine;

public static class DrawUtils
{
    /// <summary>
    /// Draws an arrow using Gizmos from origin in the direction specified.
    /// </summary>
    /// <param name="origin">The starting point of the arrow.</param>
    /// <param name="direction">The direction and length of the arrow.</param>
    /// <param name="arrowHeadAngle">Angle (in degrees) of the arrowhead sides.</param>
    public static void DrawArrow(Vector3 origin, Vector3 direction, float arrowHeadAngle = 20f)
    {
        if (direction == Vector3.zero)
            return;

        // Draw the main line
        Gizmos.DrawRay(origin, direction);

        // Calculate arrowhead
        Vector3 end = origin + direction;
        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(90 + arrowHeadAngle, 0, 0) * Vector3.down;
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(90 - arrowHeadAngle, 0, 0) * Vector3.down;

        Gizmos.DrawRay(end, right * direction.magnitude  * 0.333f);
        Gizmos.DrawRay(end, left * direction.magnitude * 0.333f);
    }

    public static void DrawLandmark(Transform transform, Vector3 scale)
    {
        if (transform == null)
            return;

        Gizmos.color = Color.red;
        DrawArrow(transform.position, transform.right * scale.x);


        Gizmos.color = Color.green;
        DrawArrow(transform.position, transform.up * scale.y);


        Gizmos.color = Color.blue;
        DrawArrow(transform.position, transform.forward * scale.z);
    }
}
