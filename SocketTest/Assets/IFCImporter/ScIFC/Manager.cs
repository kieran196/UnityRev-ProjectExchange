﻿#region License
/** Copyright(c) 2017 helpsterTee (https://github.com/helpsterTee)
Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
**/
#endregion

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Manager : MonoBehaviour {

    ImportIFC import;
    
    public bool useNamesInsteadOfTypes = true;

	// Use this for initialization
	void Start () {
        string name = "IfcOpenHouseGeoRef";
#if UNITY_EDITOR
        string file = "Assets/IFCImporter/IFCFiles/" + name + ".ifc";
#else
        string file = "./"+name+".ifc";
#endif

        import = GetComponentInChildren<ImportIFC>();
        import.ImportFinished += new ImportIFC.CallbackEventHandler(ImportIsFinished);
        import.Init();
        import.ImportFile(Path.GetFullPath(file), name, useNamesInsteadOfTypes);
	}

    public void ImportIsFinished(GameObject go)
    {
        /* Do something */

        /* show ifc properties to console for testing */
        IFCVariables vars = go.GetComponentInChildren<IFCVariables>();
        if (vars != null && vars.vars.Length > 0)
        {
            Debug.Log(vars.ToString());
        }

    }

	// Update is called once per frame
	void Update () {
	}
}
