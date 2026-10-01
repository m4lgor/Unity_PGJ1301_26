using UnityEngine;

/// <summary>
/// The SATELLITE sits ON the perpendicular to the two vectors (Sun -> Earth) and
/// (Earth -> Moon), at a fixed distance from the Earth.
///
/// In other words: the Sun, the Earth and the Moon define a plane. The Satellite sits
/// on the normal of that plane. The Earth and the Moon keep moving, so the plane keeps
/// tilting, so the Satellite keeps moving.
///
/// Careful: "perpendicular to (Earth - Moon)" on its own means nothing. There is an
/// infinity of vectors perpendicular to ONE vector, they form a whole plane. That is
/// exactly why you need TWO vectors, and exactly what a cross product takes.
///
/// Bonus: change colour depending on where the Moon is relative to the Sun.
/// </summary>
public class TODO_Satellite : MonoBehaviour
{
    [Tooltip("Start of the first vector (Sun)")]
    [SerializeField] Transform Transform0;

    [Tooltip("Point shared by both vectors, and the point we keep a fixed distance from (Earth)")]
    [SerializeField] Transform CenterTransform;

    [Tooltip("End of the second vector (Moon)")]
    [SerializeField] Transform Transform1;

    [Tooltip("How far we stay from CenterTransform")]
    [SerializeField] float FixedDistance = 5.0f;

    [Header("Bonus - colour by the phase of the Moon")]
    [Tooltip("The colour depends on which side of this object (Sun) the Moon is. Assign it yourself.")]
    [SerializeField] Transform ColorReferenceTransform;

    [SerializeField] Color FacingColor = Color.green;
    [SerializeField] Color AwayColor = Color.red;

    void Start()
    {
        if (!Transform0 || !CenterTransform || !Transform1)
        {
            Debug.LogError("TODO_Satellite - Transform0, CenterTransform and Transform1 are all required.", this);
            enabled = false;
            return;
        }
    }

    void Update()
    {
        // ----------- TODO Students ------------
        // 1. Build the TWO vectors from the brief:
        //       Sun -> Earth    and    Earth -> Moon
        //
        // 2. Their perpendicular is their cross product. Normalise it: you want a
        //    DIRECTION, which you will then multiply by FixedDistance.
        //
        // 3. Place yourself: centre position + direction * FixedDistance.
        //
        // 4. Degenerate case: what should happen when the two vectors are parallel?
        // --------------------------------------
    }

    // ----------- TODO Students (BONUS) ------------
    // Change the Renderer's colour depending on whether the Moon is on the same side as
    // the Sun, or on the opposite side. That is the phase of the moon.
    //
    // To change the colour: GetComponent<Renderer>().material.color = ...
    // ----------------------------------------------
}
