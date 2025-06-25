using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponHUDController : MonoBehaviour
{
    [SerializeField] private Image _weaponCooldownDisplay;
    [SerializeField] private Image _weaponChargeDisplay;

    [SerializeField] private PlayerCombatController _combatController;

    private void Update()
    {
        _weaponCooldownDisplay.fillAmount = 1f - _combatController.GetCooldownRate();
        _weaponChargeDisplay.fillAmount = _combatController.GetChargeRate();
    }
}
