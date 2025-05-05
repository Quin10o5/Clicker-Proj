using UnityEngine;

public class playerStealth : MonoBehaviour
{
    public bool sneaking;
    public float noiseLevel;
    public Transform normalRayPos;
    public Transform stealthRayPos;
    public static playerStealth instance;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
