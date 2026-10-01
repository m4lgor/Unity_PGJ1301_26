using UnityEngine;

// -----------------------
// Not for you to edit. Draws what a joint SAYS about itself - read back from the joint,
// never from what you meant to write.
//
//     cyan     anchor           (on the body that owns the joint)
//     magenta  connectedAnchor  (on the connected body)
//     yellow   the line between them
//     orange   the axis of a HingeJoint, through its anchor
//
// If a sphere is not on the point you meant, an anchor is in the wrong space.
// Nothing is drawn without a joint. The Gizmos button of the Game view has to be on.
// -----------------------

public static class JointGizmos
{
    public static void Draw(Joint joint, float radius)
    {
        if (joint == null)
        {
            return;
        }

        // The anchor is local to the body that owns the joint. connectedAnchor is local
        // to the connected body - or already in world space when there is none.
        Vector3 anchorPosition_WorldSpace = joint.transform.TransformPoint(joint.anchor);
        Vector3 connectedAnchorPosition_WorldSpace = joint.connectedBody != null
            ? joint.connectedBody.transform.TransformPoint(joint.connectedAnchor)
            : joint.connectedAnchor;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(anchorPosition_WorldSpace, connectedAnchorPosition_WorldSpace);

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(anchorPosition_WorldSpace, radius);

        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(connectedAnchorPosition_WorldSpace, radius);

        if (joint is HingeJoint hinge)
        {
            // Local to the body that owns the joint, like the anchor.
            Vector3 axisDirection_WorldSpace = hinge.transform.TransformDirection(hinge.axis).normalized;

            Gizmos.color = new Color(1.0f, 0.5f, 0.0f);
            Gizmos.DrawLine(anchorPosition_WorldSpace - axisDirection_WorldSpace * 0.8f,
                anchorPosition_WorldSpace + axisDirection_WorldSpace * 0.8f);
        }
    }
}
