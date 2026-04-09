using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct WeaponRunTimeStats
{
    [Header("State")]
    public int finalAmmo;
    public float finalDamage;
    public float finalAttackRate;
    public float finalMoveSpeed;
    public float finalDistance;
    public float finalZoominSpeed;
    public float finalThrowforce;
    [Header("Spread")]
    public float maxSpread;
    public float increaseSpread;
    [Header("Recoil")]
    public float vRecoil;
    public float hRecoil;
    public float VisualRecoil;
}