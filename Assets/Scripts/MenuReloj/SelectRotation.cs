using System.Xml.Serialization;
using UnityEngine;

public class SelectRotation : MonoBehaviour
{

    public Transform aguja;


    public float speedRotation = 1;

    public float newZ;
   void Start()
    {
        newZ = aguja.localEulerAngles.z;
    }
    void Update()
    {
        Vector3 euler = aguja.localEulerAngles;
        //Usamos lerp angle spara el giro de 360 
        euler.z = Mathf.LerpAngle(euler.z, newZ, speedRotation  * Time.deltaTime);
        Debug.Log(euler.z);
        aguja.localEulerAngles = euler;


    }


    public void GoToRotation(float z)
    {
        Debug.Log("nueva rotacion= " + z);
        newZ = z;
    
    }


    




}
