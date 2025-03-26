using UnityEngine;

public class PudzianJumpScript : MonoBehaviour
{
    public GameObject pudzian;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {   
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // add velocity to the rigidbody
            pudzian.GetComponent<Rigidbody>().linearVelocity = new Vector2(0, 10);
        }
    }
}
