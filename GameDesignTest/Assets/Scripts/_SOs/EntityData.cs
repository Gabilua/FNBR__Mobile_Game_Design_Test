using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New CharacterData", menuName = "Characters/Create Character")]
public class EntityData : ScriptableObject
{
    public float maxMovementSpeed;
    public float maxTurnSpeed;
    public float maxVerticalCameraSpeed;
    public float jumpHeight;

    public AnimationCurve verticalJumpForceCurve;
    public AnimationCurve horizontalJumpForceCurve;

    public WeaponData startingWeapon;
}
