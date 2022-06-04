using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class triggered : MonoBehaviour
{

    private void OnTriggerStay(Collider other)
    {
        if (this.enabled) {
            Debug.Log("Triggered:" + other.transform.name);
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
