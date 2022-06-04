using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

//[ExecuteInEditMode]
public class ServerListener : MonoBehaviour {
    public enum SERVER_STATUS {NULL, SERVER_OPEN, SERVER_CLOSED};
    [SerializeField]
    private SERVER_STATUS serverStatus;
    [SerializeField]
    private bool openServer, autoStart;
    [SerializeField]
    private bool closeServer;
    [SerializeField]
    private bool updateClientData;

    //Stats
    [SerializeField]
    private int elementsCount, metadataCount, metadataCount2;

    System.Threading.Thread SocketThread;
    volatile bool keepReading = false;

    // Use this for initialization
    void Start() {
        serverStatus = SERVER_STATUS.NULL;
        allObjects = GameObject.FindGameObjectsWithTag("RevitObj");
        if (autoStart) {
            Application.runInBackground = true;
            startServer();
        }
    }

        public bool newDataSet = false;
        public string newData;
        private Vector3 oldPos;

        public static readonly float MSGDELAY = 1f;
        private float MsgTimer = 0f;
        private bool initialDataRecieved = false;

    void Update() {
        if (receiveMeshDataCommand) {
            sendCmd("SMD");
            receiveMeshDataCommand = false;
        }
        MsgTimer += Time.deltaTime;
        /*if (revObjTest == null) { // If the object is null find it..
            revObjTest = GameObject.FindGameObjectWithTag("RevitObj");
        }*/
        // A change of data has occured?
        if (newDataSet) {
            Debug.Log("New data has been set to true");
            newDataSet = false;
            if (newData.StartsWith("MDT")) {
                recievedMDT(newData);
            }
            if (newData.StartsWith("MDV")) {
                recievedMDV(newData);
            }
            if (newData.StartsWith("GMDP")) {
                recievedGMD(newData, "POS");
            }
            if (newData.StartsWith("GMDR")) {
                recievedGMD(newData, "ROT");
            }
            if (newData.StartsWith("GMDS")) {
                recievedGMD(newData, "SCALE");
            }
            if (newData.StartsWith("init")) {
                splitData(newData);
            }
            if (newData.StartsWith("ERR")) {
                undoLastMove();
            }
        }
        /*
         if (newData.StartsWith("GMD"))
        {
            recievedGMD(newData);
        }
        if (newData.StartsWith("init")) {
            splitData(newData);
        }
         */
        if (openServer) {
            openServer = false;
            //splitData("34 x 84 # XYZ:(32, 23, 32) #ID:208F");
            Application.runInBackground = true;
            startServer();
        }
        if (closeServer) {
            closeServer = false;
            stopServer();
        }  if (MsgTimer > MSGDELAY) {
            objMovedIndex = objectMoved();
            objRotatedIndex = objectRotated();
            objScaleChangedIndex = objectScaleChanged();
            //Debug.Log("Obj moved:" + objMovedIndex);
            if (objMovedIndex != -1) {
                MsgTimer = 0f;
                RevAttributes objAttributes = revObjs[objMovedIndex].GetComponent<RevAttributes>();
                sendGMDData(objAttributes.getId(), "POS", MoveOffset.ToString());
            } if (objRotatedIndex != -1) {
                MsgTimer = 0f;
                RevAttributes objAttributes = revObjs[objRotatedIndex].GetComponent<RevAttributes>();
                sendGMDData(objAttributes.getId(), "ROT", RotOffset.ToString());
            } if (objScaleChangedIndex != -1) {
                RevAttributes objAttributes = revObjs[objScaleChangedIndex].GetComponent<RevAttributes>();
            }
        }
    }

    public int objMovedIndex = -1; //Last object moved index.
    public int objRotatedIndex = -1;
    public int objScaleChangedIndex = -1;
    // Brief testing of my undo method and I found a bug. Basically if it sends two ERR messages, the software getst confused and doesn't revert it to the correct last position. Instead it remains in the same pos, and as a result creates a mismatch between the element in Revit and Unity. Can be easily reproduced by constantly dragging a GameObject to an invalid position.
    private void undoLastMove() {
        if (objMovedIndex != -1) {
            revObjs[objMovedIndex].transform.position -= MoveOffset;
            revObjsPos[objMovedIndex] = revObjs[objMovedIndex].transform.position;
        }
    }

    private bool lastCallFromRevitClient = false;
    private Vector3 MoveOffset;
    private Vector3 ScaleOffset;
    private float RotOffset;

    public int objectRotated() {
        for (int i = 0; i < revObjs.Count; i++) {
            if (revObjs[i].transform.localEulerAngles.y != revObjsRot[i]) {
                Debug.Log("Obj rotated:" + revObjs[i]);
                RotOffset = revObjs[i].transform.localEulerAngles.y - revObjsRot[i];
                revObjsRot[i] = revObjs[i].transform.localEulerAngles.y;
                if (lastCallFromRevitClient) {
                    lastCallFromRevitClient = false;
                    return -1;
                }
                return i;
            }
        }
        return -1;
    }

    public int objectScaleChanged() {
        for (int i = 0; i < revObjs.Count; i++) {
            if (revObjs[i].transform.localScale != revObjsScale[i]) {
                float scaleX = revObjs[i].transform.localScale.x / revObjsScaleOriginal[i].x;
                float scaleY = revObjs[i].transform.localScale.y / revObjsScaleOriginal[i].y;
                float scaleZ = revObjs[i].transform.localScale.z / revObjsScaleOriginal[i].z;
                ScaleOffset = new Vector3(scaleX, scaleY, scaleZ);
                Debug.Log("Obj scale changed:" + revObjs[i].name + " | " + ScaleOffset);
                revObjsScale[i] = revObjs[i].transform.localScale;
                if (lastCallFromRevitClient) {
                    lastCallFromRevitClient = false;
                    return -1;
                }
                return i;
            }
        }
        return -1;
    }

    public int objectMoved() {
        for (int i=0; i<revObjs.Count; i++) {
            //Debug.Log("obj:" + revObjs[i].name + " | " + revObjsPos[i]);
            if (revObjs[i].transform.position != revObjsPos[i]) {
                Debug.Log("Obj moved:" + revObjs[i]);
                MoveOffset = revObjs[i].transform.position - revObjsPos[i];
                revObjsPos[i] = revObjs[i].transform.position;
                if (lastCallFromRevitClient || revObjs[i].transform.localEulerAngles.y != revObjsRot[i]) {
                    lastCallFromRevitClient = false;
                    return -1;
                }
                return i;
            }
        }
        return -1;
    }

    void startServer() {
        SocketThread = new System.Threading.Thread(networkCode);
        SocketThread.IsBackground = true;
        SocketThread.Start();
    }
    [SerializeField]
    private List<GameObject> revObjs = new List<GameObject>();
    [SerializeField]
    private List<Vector3> revObjsPos = new List<Vector3>();
    private List<Vector3> revObjsScale = new List<Vector3>();
    private List<Vector3> revObjsScaleOriginal = new List<Vector3>();
    private List<float> revObjsRot = new List<float>();
    
    private String revId;

    private GameObject[] allObjects;
    public GameObject FindGameObjectId(string ID) {
        foreach (GameObject obj in allObjects) {
            //Debug.Log(ID+" COMPARED ID:" + obj.GetComponent<RevAttributes>().getId());
            if (obj.GetComponent<RevAttributes>().getId().Equals(ID)) {
                //obj.GetComponent<Renderer>().material.color = Color.green;
                return obj;
            }
        } // Not found..
        return null;
    }
    private readonly float ROT_CONSTANT = 57.295F;
    public float transformRevitRot(float revitRot) {
        return revitRot * ROT_CONSTANT;
    }

    public Vector3 transformRevitCoords(Vector3 revitCoords) {
        // Inverse and flip y and z avis.
        return new Vector3(-revitCoords.x, -revitCoords.z, -revitCoords.y);
    }

    public void sendCmd(string command) {
        Debug.Log("Sending Command:" + command);
        byte[] msg = Encoding.ASCII.GetBytes(command);
        handler.Send(msg);
    }

    public void sendBIMData(string ID, string key, string value) {
        String sendData = "BIM" + ID + "#" + key + "#" + value;
        Debug.Log("Sending Data:" + sendData);
        byte[] msg = Encoding.ASCII.GetBytes(sendData);
        handler.Send(msg);
    }

    public void sendGMDData(string revId, string type, string newGMDValue) {
        String sendData = "";
        if (type == "POS") {
            sendData = "GMDP" + revId + "#" + newGMDValue;
        } else if (type == "ROT") {
            sendData = "GMDR" + revId + "#" + newGMDValue;
        }
        byte[] msg = Encoding.ASCII.GetBytes(sendData);
        handler.Send(msg);
    }

    public void splitData(String data) {
        //String[] splitElements = data.Split('~');
        String[] splitBIMData = null;
        GameObject revObj = null;
        //Debug.Log("Number of objects sent:" + splitElements.Length);
        //foreach (String element in splitElements) {
            String[] splitData = data.Split('#');
            metadataCount2 += splitData.Length;
            //Debug.Log("Splitting data into "+splitData.Length + " sections.");
            for (int i=0;i<splitData.Length;i++) {
                if (i==0) { //ID
                    splitData[i] = splitData[i].Substring(4);
                    splitData[i] = splitData[i].Replace("ID:", "");
                    splitData[i] = splitData[i].Trim();
                    //Debug.Log("Looking for ID:" + splitData[i]);
                    elementsCount += 1;
                    revObj = FindGameObjectId(splitData[i]);
                    if (revObj != null) {
                        revObj.GetComponent<RevAttributes>().setId(splitData[i]);
                        revId = revObj.GetComponent<RevAttributes>().getId();
                        revObjs.Add(revObj);
                    } else {
                        Debug.Log("ID:"+splitData[i] + " cannot be found.. Exiting");
                        //newData = "";
                        //break;
                        return;
                    }
                } else if (i==1) {//Name
                    revObj.name = splitData[i];
                } else if (i==2) { //Coords
                    StringBuilder stringBuilder = new StringBuilder(splitData[i])
                    .Replace("XYZ:", "").Replace("(", "".Replace(")", ""));
                    String newStr = stringBuilder.ToString().Remove(stringBuilder.Length-2);
                    String[] xyz = newStr.Split(',');
                    //string x = xyz[0].Replace("XYZ:, "").Replace("(", "");
                    //Debug.Log("X VAL:" + xyz[0]);
                    //Debug.Log("Y VAL:" + xyz[1]);
                    //Debug.Log("Z VAL:" + xyz[2]);
                    Vector3 rawRevitCoords = new Vector3(float.Parse(xyz[0]), float.Parse(xyz[1]), float.Parse(xyz[2]));
                    Vector3 coordinateTransform = transformRevitCoords(rawRevitCoords);
                    //revObj.transform.position = coordinateTransform;
                    revObj.GetComponent<RevAttributes>().startingPosition = coordinateTransform;
                    //TODO GET POS;
                } else if (i== 3) {
                    //Properties..
                    if (revObj.GetComponent<RevAttributes>().propertiesCount() == 0) {
                    Debug.Log("Assigning properties for ID:" + revId);
                        //We can split BIM data here..
                        splitBIMData = splitData[3].Split(':');
                        revObj.GetComponent<RevAttributes>().assignPropertiesCapacity((splitBIMData.Length/2));
                        metadataCount += splitBIMData.Length / 2;
                        //splitBIMData
                        int d = 1;
                        for (int n=1; n<splitBIMData.Length; n+=2) {
                            if (n >= splitBIMData.Length-1) continue;
                            //Debug.Log("Data:" + splitBIMData[n] + ","+splitBIMData[n+1]);
                            revObj.GetComponent<RevAttributes>().addProperty(d-1, splitBIMData[n].TrimStart().TrimEnd(), splitBIMData[n+1].TrimStart().TrimEnd());
                            d++;
                        }
                    }
                }
            }
            //newData = "";
            revObjsPos.Add(revObj.transform.position);
            revObjsScale.Add(revObj.transform.localScale);
            revObjsScaleOriginal.Add(revObj.transform.localScale);
            revObjsRot.Add(revObj.transform.localEulerAngles.y);
        //}
    }

    void recievedMDT(String serverMessage) // Triangles
    {
        ElementMesh eleMesh = new ElementMesh();
        eleMesh.meshCreator = meshCreator;
        eleMesh.index = meshCreator.elementMeshArr.Count;
        meshCreator.elementMeshArr.Add(eleMesh);
        serverMessage = serverMessage.Substring(3);
        String[] splitb = serverMessage.Split('#');
        Debug.Log("Splitting triangles for ID = " + splitb[0]);
        String[] splitTris = splitb[1].Split(' ');
        meshCreator.elementMeshArr[meshCreator.elementMeshArr.Count - 1].triangles = new int[splitTris.Length-1];
        int count = 0;
        foreach (String tri in splitTris) {
            if (tri.Length >= 1) {
                //Debug.Log(count + " | " + meshCreator.elementMeshArr[meshCreator.elementMeshArr.Count - 1].triangles[count]);
                meshCreator.elementMeshArr[meshCreator.elementMeshArr.Count - 1].triangles[count] = int.Parse(tri);
                //meshCreator.elementMeshArr[meshCreator.elementMeshArr.Count - 1]
                count++;
            }
            //Debug.Log("Tri:" + tri);
        }
        meshCreator.index++;
    }

    public meshCreation meshCreator;
    public bool receiveMeshDataCommand;

    void recievedMDV(String serverMessage) // Vertices
    {
        serverMessage = serverMessage.Substring(3);
        String[] splitb = serverMessage.Split('#');
        Debug.Log("Splitting vertices for ID = " + splitb[0]);
        meshCreator.elementMeshArr[meshCreator.elementMeshArr.Count - 1].ID = splitb[0];
        meshCreator.elementMeshArr[meshCreator.elementMeshArr.Count - 1].vertices = new Vector3[splitb.Length - 2];
        for (int i = 1; i < splitb.Length; i++) {
            String[] xyzVert = splitb[i].Split(',');
            if (xyzVert.Length > 1) {
                meshCreator.elementMeshArr[meshCreator.elementMeshArr.Count - 1].vertices[i - 1].x = float.Parse(xyzVert[0].Trim());
                meshCreator.elementMeshArr[meshCreator.elementMeshArr.Count - 1].vertices[i - 1].y = float.Parse(xyzVert[1].Trim());
                meshCreator.elementMeshArr[meshCreator.elementMeshArr.Count - 1].vertices[i - 1].z = float.Parse(xyzVert[2].Trim());
            }
        }
    }

    // For only recieving Vertices as opposed to Tris.
    /*void recievedMDV(String serverMessage) // Vertices
    {
        serverMessage = serverMessage.Substring(3);
        String[] splitb = serverMessage.Split('#');
        Debug.Log("Splitting vertices for ID = " + splitb[0]);
        ElementMesh eleMesh = new ElementMesh();
        eleMesh.meshCreator = meshCreator;
        eleMesh.index = meshCreator.elementMeshArr.Count;
        meshCreator.elementMeshArr.Add(eleMesh);

        meshCreator.elementMeshArr[meshCreator.elementMeshArr.Count - 1].ID = splitb[0];
        meshCreator.elementMeshArr[meshCreator.elementMeshArr.Count - 1].triSize = int.Parse(splitb[1]);
        meshCreator.elementMeshArr[meshCreator.elementMeshArr.Count - 1].vertices = new Vector3[splitb.Length-3];
        for (int i=2; i< splitb.Length; i++) {
            Debug.Log(i + " | " + splitb[i]);
            String[] xyzVert = splitb[i].Split(',');
            if (xyzVert.Length > 1) {
                meshCreator.elementMeshArr[meshCreator.elementMeshArr.Count - 1].vertices[i - 2].x = float.Parse(xyzVert[0].Trim());
                meshCreator.elementMeshArr[meshCreator.elementMeshArr.Count - 1].vertices[i - 2].y = float.Parse(xyzVert[1].Trim());
                meshCreator.elementMeshArr[meshCreator.elementMeshArr.Count - 1].vertices[i - 2].z = float.Parse(xyzVert[2].Trim());
            }
        }
        meshCreator.index++;
    }*/

    void recievedGMD(String serverMessage, String GMD_Type) {
        lastCallFromRevitClient = true;
        serverMessage = serverMessage.Substring(4);
        StringBuilder sb = new StringBuilder(serverMessage).Replace("(", "").Replace(")", "");
        String[] splitsb = sb.ToString().Split('#');
        Debug.Log("ID=" + splitsb[0]);
        Debug.Log("VAL=" + splitsb[1]);
        GameObject revObj = FindGameObjectId(splitsb[0]);
        if (revObj != null) {
            if (GMD_Type == "POS") {
                String[] xyz = splitsb[1].Split(',');
                Vector3 rawRevitCoords = new Vector3(float.Parse(xyz[0]), float.Parse(xyz[1]), float.Parse(xyz[2]));
                Vector3 coordinateTransform = transformRevitCoords(rawRevitCoords);
                //Debug.Log("coordChange:" + coordinateTransform);
                revObj.transform.position += coordinateTransform;
            } else if (GMD_Type == "ROT") {
                float rawRevitRot = float.Parse(splitsb[1]);
                float rotationTransform = transformRevitRot(rawRevitRot);
                Debug.Log(revObj.transform.name + " | Rot change:" + rotationTransform);
                revObj.transform.localEulerAngles -= new Vector3(0f, rotationTransform, 0f);
            } else if (GMD_Type == "SCALE") {
                String[] xyz = splitsb[1].Split(',');
                Vector3 rawRevitScale = new Vector3(float.Parse(xyz[0]), float.Parse(xyz[1]), float.Parse(xyz[2]));
                revObj.transform.localScale = rawRevitScale;
            }
        } else {
            throw new NullReferenceException("Unable to locate element ID = " + splitsb[0]);
        }
    }

    Socket listener;
    Socket handler;

    void networkCode() {
        string data;

        // Data buffer for incoming data.
        byte[] bytes = new Byte[1024];

        // host running the application.
        IPHostEntry host = Dns.GetHostEntry("localhost");
        IPAddress ipAddress = host.AddressList[0];
        Debug.Log("Starting server on IP: " + ipAddress.ToString());
        serverStatus = SERVER_STATUS.SERVER_OPEN;
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);
        //IPAddress[] ipArray = Dns.GetHostAddresses(getIPAddress());
        //IPEndPoint localEndPoint = new IPEndPoint(ipArray[0], 1755);

        // Create a TCP/IP socket.
        listener = new Socket(ipAddress.AddressFamily,
            SocketType.Stream, ProtocolType.Tcp);

        // Bind the socket to the local endpoint and 
        // listen for incoming connections.

        try {
            listener.Bind(localEndPoint);
            listener.Listen(10);

            // Start listening for connections.
            while (true) {
                keepReading = true;

                // Program is suspended while waiting for an incoming connection.
                Debug.Log("Waiting for Connection");     //It works

                handler = listener.Accept();
                Debug.Log("Client Connected");     //It doesn't work
                data = null;

                // An incoming connection needs to be processed.
                while (keepReading) {
                    bytes = new byte[20000]; //4096
                    int bytesRec = handler.Receive(bytes);
                    Debug.Log("Received from Server:" + bytesRec);

                    if (bytesRec <= 0) {
                        keepReading = false;
                        handler.Disconnect(true);
                        Debug.Log("Handler has been disconnected..");
                        break;
                    }

                    data = Encoding.ASCII.GetString(bytes, 0, bytesRec);
                    newData = data.ToString();
                    Debug.Log("Recieved Data:" + data.ToString());
                    newDataSet = true;
                    /*String sendData = "ID:"+revId + ", POS:" + revObjPos;
                    byte[] msg = Encoding.ASCII.GetBytes(sendData);
                    handler.Send(msg);
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();*/


                    System.Threading.Thread.Sleep(1);
                }

                System.Threading.Thread.Sleep(1);
            }
        } catch (Exception e) {
            Debug.Log(e.ToString());
        }
    }

    void stopServer() {
        keepReading = false;

        //stop thread
        if (SocketThread != null) {
            serverStatus = SERVER_STATUS.SERVER_CLOSED;
            SocketThread.Abort();
        }

        if (handler != null && handler.Connected) {
            handler.Disconnect(false);
            Debug.Log("Disconnected!");
        }
    }

    void OnDisable() {
        stopServer();
    }

}
