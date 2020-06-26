using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Tridify;

public class RevAttributes : MonoBehaviour {
    private SocketTest mainServer;

    [System.Serializable]
    public struct BIM_DATA {
        public string key;
        public string value;
    }

    [SerializeField]
    private string ID_TAG;
    [SerializeField]
    private string UNIQUE_ID;
    [SerializeField]
    //private string[] properties = null;
    public BIM_DATA[] properties;
    public Vector3 startingPosition = Vector3.zero;
    //private ArrayList properties;
    public Dictionary<string, string> dictionaryTest = new Dictionary<string, string>();

    public BIM_DATA[] getProperties() {
        return properties;
    }

    public void assignPropertiesCapacity(int count) {
        Debug.Log("Assigning properties capacity to:" + count);
        properties = new BIM_DATA[count];
    }

    public int propertiesCount() {
        return properties.Length;
    }

    public void addProperty(int index, string key, string value) {
        properties[index].key = key;
        properties[index].value = value;
    }

    /*public void ShowArrayProperty(SerializedProperty list) {
        EditorGUILayout.PropertyField(list);
         EditorGUI.indentLevel += 1;
         for (int i = 0; i < list.arraySize; i++)
             {
                   EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i),  
                   new GUIContent ("Bla" + (i+1).ToString())); 
             }            
             EditorGUI.indentLevel -= 1;
    }*/

    void Awake() {
        mainServer = FindObjectOfType<SocketTest>();
        /*assignPropertiesCapacity(2);
        addProperty(0, "Bob");
        addProperty(1, "Tim");*/
        // Assign the tags
        if (GetComponent<IfcDoor>() != null) {
            ID_TAG = GetComponent<IfcDoor>().Attributes[2].Value;
        } else if (GetComponent<IfcCovering>() != null) {
            ID_TAG = GetComponent<IfcCovering>().Attributes[2].Value;
        } else if (GetComponent<IfcSlab>() != null) {
            ID_TAG = GetComponent<IfcSlab>().Attributes[2].Value;
        } else if (GetComponent<IfcWallStandardCase>() != null) {
            ID_TAG = GetComponent<IfcWallStandardCase>().Attributes[2].Value;
        } else if (GetComponent<IfcWindow>() != null) {
            ID_TAG = GetComponent<IfcWindow>().Attributes[2].Value;
        }
    }

    public void setId(string UNIQUE_ID) {
        this.UNIQUE_ID = UNIQUE_ID;
    }

    public string getId() {
        return ID_TAG;
    }

    private string[] oldProperties = null;
    public int onDataChange() {
        if (propertiesCount() == 0) {
            return -1;
        } else if (oldProperties == null || oldProperties.Length == 0) {
            //Debug.Log("Updating old properties..");
            oldProperties = new string[properties.Length];
            for (int i=0; i<propertiesCount(); i++) {
                oldProperties[i] = properties[i].value;
            }
        }
        for (int i=0; i<propertiesCount(); i++) {
            //Debug.Log(properties[i] + " | " + oldProperties[i]);
            if (properties[i].value != oldProperties[i]) {
                Debug.Log(this.transform.name + " params modified. Key=" + properties[i].key);
                oldProperties[i] = properties[i].value;
                return i;
            }
        }
        return -1;
    }
    void Update() {
        int index = onDataChange();
        if (index != -1) {
            mainServer.sendBIMData(ID_TAG, properties[index].key, properties[index].value);
        }
        /*if (debugProperties) {
            debugProperties = false;
            for (int i=0; i<properties.Count; i++) {
                print(i + " | " + properties[i]);
            }
        }*/
    }
}
