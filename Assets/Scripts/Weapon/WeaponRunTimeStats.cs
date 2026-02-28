using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct WeaponRunTimeStats
{
    public int finalAmmo;
    public float finalDamage;
    public float finalAttackRate;
    public float finalMoveSpeed;
    public float vRecoil;
    public float hRecoil;
    public float maxSpread;
    public float increaseSpread;
}