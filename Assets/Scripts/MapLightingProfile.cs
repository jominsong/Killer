using UnityEngine;

[CreateAssetMenu(menuName = "FPS/Map Lighting Profile")]
public class MapLightingProfile : ScriptableObject
{
    [Header("Ambient")]
    public Color ambientColor = Color.gray;
    public float ambientIntensity = 1f;

    [Header("Directional Light")]
    public Color directionalLightColor = Color.white;
    public float directionalLightIntensity = 1f;
    public Vector3 directionalLightEuler = new Vector3(50f, -30f, 0f);

    [Header("Fill Light (º¸Á¶)")]
    public Color fillLightColor = Color.white;
    public float fillLightIntensity = 0.3f;
    public Vector3 fillLightEuler = new Vector3(50f, 150f, 0f);

    [Header("Fog")]
    public bool fogEnabled = false;
    public Color fogColor = Color.gray;
    public float fogDensity = 0.01f;

    [Header("Skybox")]
    public Material skybox;
}