using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New WeaponData", menuName = "Weapons/Create Weapon")]
public class WeaponData : ScriptableObject
{
    public GameObject weaponProjectile;
    public GameObject weaponFireVFX;
    public GameObject weaponHitVFX;

    public float weaponProjectileBaseForce;
    public float weaponTotalChargeTime;
    public float weaponShotCooldown; 
    public float weaponExplosionRadius;
    public float weaponExplosionBaseDamage;
}


