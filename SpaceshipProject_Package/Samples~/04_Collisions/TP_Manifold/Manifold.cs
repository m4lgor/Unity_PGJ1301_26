using UnityEngine;

// -----------------------
// This file is not for you to edit. It is the shape your detection code fills in.
// -----------------------

/// <summary>
/// What an engine records about one contact. Every collision system produces one of
/// these, whatever shapes it supports: the manifold is the boundary between "what
/// touched" and "what to do about it".
///
/// That is why the response code never mentions spheres. Fill this in for boxes next
/// week and the resolution below it does not change a line.
/// </summary>
public struct Manifold
{
    /// <summary>Did the two shapes actually overlap?</summary>
    public bool IsColliding;

    /// <summary>
    /// Unit length, pointing from A towards B. This is the line the push happens along,
    /// so getting it backwards makes things suck together instead of bouncing apart.
    /// </summary>
    public Vector3 Normal;

    /// <summary>How deep they overlap, in metres. Zero or more, never negative.</summary>
    public float Penetration;

    /// <summary>Where the contact is, in WORLD space.</summary>
    public Vector3 ContactPoint;

    public static Manifold None
    {
        get { return new Manifold { IsColliding = false }; }
    }
}
