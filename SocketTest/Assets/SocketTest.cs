using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

//[ExecuteInEditMode]
public class SocketTest : MonoBehaviour {
    public enum SERVER_STATUS {NULL, SERVER_OPEN, SERVER_CLOSED};
    [SerializeField]
    private SERVER_STATUS serverStatus;
    [SerializeField]
    private bool openServer;
    [SerializeField]
    private bool closeServer;
    [SerializeField]
    private bool updateClientData;

    System.Threading.Thread SocketThread;
    volatile bool keepReading = false;

    // Use this for initialization
    void Start() {
        serverStatus = SERVER_STATUS.NULL;
        allObjects = GameObject.FindGameObjectsWithTag("RevitObj");
        //Application.runInBackground = true;
        //startServer();
    }

        public bool newDataSet = false;
        public string newData;
        private Vector3 oldPos;

        public static readonly float MSGDELAY = 1f;
        private float MsgTimer = 0f;

    void Update() {
        MsgTimer += Time.deltaTime;
        /*if (revObjTest == null) { // If the object is null find it..
            revObjTest = GameObject.FindGameObjectWithTag("RevitObj");
        }*/
        // A change of data has occured?
        //Debug.Log("New data set:" + newDataSet);
        if (newDataSet) {
            Debug.Log("New data has been set to true");
            newDataSet = false;
            splitData(newData.ToString());
            
        }
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
            int objMovedIndex = objectMoved();
            if (objMovedIndex != -1) {
                MsgTimer = 0f;
                //oldPos = revObjTest.transform.position;
                //revObjPos = oldPos;
                RevAttributes objAttributes = revObjs[objMovedIndex].GetComponent<RevAttributes>();
                Vector3 offset = objAttributes.startingPosition - revObjsPos[objMovedIndex];
                sendGMDData(objAttributes.getId(), offset.ToString());
                //sendData(objAttributes.getId(), revObjsPos[objMovedIndex].ToString());
            }
        }
    }

    public int objectMoved() {
        for (int i=0; i<revObjs.Count; i++) {
            if (revObjs[i].transform.position != revObjsPos[i]) {
                revObjsPos[i] = revObjs[i].transform.position;
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

    private List<GameObject> revObjs = new List<GameObject>();
    private List<Vector3> revObjsPos = new List<Vector3>();
    private String revId;

    private GameObject[] allObjects;
    public GameObject FindGameObjectId(string ID) {
        foreach (GameObject obj in allObjects) {
            Debug.Log(ID+" COMPARED ID:" + obj.GetComponent<RevAttributes>().getId());
            if (obj.GetComponent<RevAttributes>().getId().Equals(ID)) {
                return obj;
            }
        } // Not found..
        return null;
    }

    public Vector3 transformRevitCoords(Vector3 revitCoords) {
        // Inverse and flip y and z avis.
        return new Vector3(-revitCoords.x, -revitCoords.z, -revitCoords.y);
    }

    public void sendBIMData(string ID, string key, string value) {
        String sendData = "BIM" + ID + "#" + key + "#" + value;
        Debug.Log("Sending Data:" + sendData);
        byte[] msg = Encoding.ASCII.GetBytes(sendData);
        handler.Send(msg);
    }

    public void sendGMDData(string revId, string revObjPos) {
        String sendData = "GMD" + revId + "#" + revObjPos;
        Debug.Log("Sending Data:" + sendData);
        byte[] msg = Encoding.ASCII.GetBytes(sendData);
        handler.Send(msg);
        //handler.Shutdown(SocketShutdown.Both);
        //handler.Close();
    }

    public void splitData(String data) {
        String[] splitElements = data.Split('~');
        String[] splitBIMData = null;
        GameObject revObj = null;
        Debug.Log("Number of objects sent:" + splitElements.Length);
        foreach (String element in splitElements) {
            String[] splitData = element.Split('#');
            Debug.Log("Splitting data into "+splitData.Length + " sections.");
            for (int i=0;i<splitData.Length;i++) {
                if (i==0) { //ID
                    splitData[i] = splitData[i].Replace("ID:", "");
                    splitData[i] = splitData[i].Trim();
                    Debug.Log("Looking for ID:" + splitData[i]);
                    revObj = FindGameObjectId(splitData[i]);
                    if (revObj != null) {
                        revObj.GetComponent<RevAttributes>().setId(splitData[i]);
                        revId = revObj.GetComponent<RevAttributes>().getId();
                        revObjs.Add(revObj);
                    } else {
                        Debug.Log("ID:"+splitData[i] + " cannot be found.. Exiting");
                        break;
                        //return;
                    }
                } else if (i==1) {//Name
                    revObj.name = splitData[i];
                } else if (i==2) { //Coords
                    StringBuilder stringBuilder = new StringBuilder(splitData[i])
                    .Replace("XYZ:", "").Replace("(", "".Replace(")", ""));
                    String newStr = stringBuilder.ToString().Remove(stringBuilder.Length-2);
                    String[] xyz = newStr.Split(',');
                    //string x = xyz[0].Replace("XYZ:, "").Replace("(", "");
                    Debug.Log("X VAL:" + xyz[0]);
                    Debug.Log("Y VAL:" + xyz[1]);
                    Debug.Log("Z VAL:" + xyz[2]);
                    Vector3 rawRevitCoords = new Vector3(float.Parse(xyz[0]), float.Parse(xyz[1]), float.Parse(xyz[2]));
                    Vector3 coordinateTransform = transformRevitCoords(rawRevitCoords);
                    revObj.transform.position = coordinateTransform;
                    revObj.GetComponent<RevAttributes>().startingPosition = coordinateTransform;
                    //TODO GET POS;
                } else if (i== 3) {
                    //Properties..
                    if (revObj.GetComponent<RevAttributes>().propertiesCount() == 0) {
                        //We can split BIM data here..
                        splitBIMData = splitData[3].Split(':');
                        revObj.GetComponent<RevAttributes>().assignPropertiesCapacity((splitBIMData.Length/2));
                        //splitBIMData
                        int d = 1;
                        for (int n=1; n<splitBIMData.Length; n+=2) {
                            Debug.Log("Data:" + splitBIMData[n] + ","+splitBIMData[n+1]);
                            revObj.GetComponent<RevAttributes>().addProperty(d-1, splitBIMData[n], splitBIMData[n+1]);
                            d++;
                        }
                    }
                }
            }
            revObjsPos.Add(revObj.transform.position);
            //revObjPos = revObj.transform.position;
        }
        //sendData();
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
                    bytes = new byte[1024];
                    int bytesRec = handler.Receive(bytes);
                    Debug.Log("Received from Server:" + bytesRec);

                    if (bytesRec <= 0) {
                        keepReading = false;
                        handler.Disconnect(true);
                        Debug.Log("Handler has been disconnected..");
                        break;
                    }

                    data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                    newData = data.ToString();
                    newDataSet = true;
                    Debug.Log("Recieved Data:" + data.ToString());
                    
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
