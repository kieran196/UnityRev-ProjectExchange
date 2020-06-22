using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RevAttributes : MonoBehaviour {

    [SerializeField]
    private string UNIQUE_ID;

    public void setId(string UNIQUE_ID) {
        this.UNIQUE_ID = UNIQUE_ID;
    }

    public string getId() {
        return UNIQUE_ID;
    }
}
