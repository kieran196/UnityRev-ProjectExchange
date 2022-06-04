using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
[ExecuteInEditMode]
public class AttributeLinker : MonoBehaviour {
    private readonly float SCALE = 3.048f; // Scale
    public enum DATA_TYPE {IFC, FBX};
    public DATA_TYPE dataType;
    [SerializeField]
    private bool linkScriptsBool, unlinkScriptsBool, fixScale;

    private void linkScripts(GameObject element) {
        if (element.GetComponent<MeshRenderer>() != null && element.GetComponent<RevAttributes>() == null) {
            element.gameObject.AddComponent<RevAttributes>();
            element.gameObject.GetComponent<RevAttributes>().attributeLinkerRef = this;
            element.gameObject.tag = "RevitObj";
            Debug.Log("Add Rev Attributes to:" + element.transform.name);
        }
    }

    private void unlinkScripts(GameObject element) {
        if (element.GetComponent<RevAttributes>() != null) {
            DestroyImmediate(element.gameObject.GetComponent<RevAttributes>());
            element.gameObject.tag = "Untagged";
            Debug.Log("Add Rev Attributes to:" + element.transform.name);
        }
    }

    private void findChildBIMElements(Transform parent, bool linkingScripts) {
        //Debug.Log("Checking:" + parent.name);
        int childCount = parent.childCount;
        if (childCount > 0) {
            for (int i=0; i<childCount; i++) {
                Transform child = parent.GetChild(i);
                if (linkingScripts) {
                    linkScripts(child.gameObject);
                } else {
                    unlinkScripts(child.gameObject);
                }
                findChildBIMElements(child, linkingScripts);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (linkScriptsBool) {
            Debug.Log("Linking scripts to child elements of:" + this.transform.name);
            findChildBIMElements(this.transform, true);
            linkScriptsBool = false;
        }
        if (unlinkScriptsBool) {
            Debug.Log("Unlinking scripts to child elements of:" + this.transform.name);
            findChildBIMElements(this.transform, false);
            unlinkScriptsBool = false;
        }
        if (fixScale) {
            this.transform.localScale = new Vector3(SCALE, SCALE, SCALE);
            fixScale = false;
        }
    }
}
