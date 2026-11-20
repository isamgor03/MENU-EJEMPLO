using UnityEngine;

public class FlyCamera : MonoBehaviour
{
    public Transform target;

    public float velocity = 1f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //Esto sigue a alguien suave
        transform.position = Vector3.Lerp(transform.position, target.position, velocity * Time.deltaTime );
        transform.rotation = Quaternion.Lerp(transform.rotation, target.rotation, velocity * Time.deltaTime);
        
    }

    public void changeTarget(Transform newTarget) 
    {

        target = newTarget; 
    
    }
}
