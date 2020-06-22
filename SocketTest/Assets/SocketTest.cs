using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

[ExecuteInEditMode]
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
        //Application.runInBackground = true;
        //startServer();
    }

        public bool newDataSet = false;
        public string newData;

    void Update() {
        if (revObjTest == null) { // If the object is null find it..
            revObjTest = GameObject.FindGameObjectWithTag("RevitObj");
        }
        // A change of data has occured?
        Debug.Log("New data set:" + newDataSet);
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
        }
        if (updateClientData) {
            updateClientData = false;
            sendData();
        }
    }

    void startServer() {
        SocketThread = new System.Threading.Thread(networkCode);
        SocketThread.IsBackground = true;
        SocketThread.Start();
    }



    private string getIPAddress() {
        IPHostEntry host;
        string localIP = "";
        host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (IPAddress ip in host.AddressList) {
            if (ip.AddressFamily == AddressFamily.InterNetwork) {
                localIP = ip.ToString();
            }

        }
        return localIP;
    }

    private GameObject revObjTest;
    private Vector3 revObjPos;
    private String revId;

    public void sendData() {
        String sendData = revId + "#" + revObjPos;
        Debug.Log("Sending Data:" + sendData);
        byte[] msg = Encoding.ASCII.GetBytes(sendData);
        handler.Send(msg);
        handler.Shutdown(SocketShutdown.Both);
        handler.Close();
    }

    public void splitData(String data) {
        Debug.Log("Splitting data..");
        String[] splitData = data.Split('#');
        for (int i=0;i<splitData.Length;i++) {
            if (i==0) {//Name
                revObjTest.name = splitData[i];
            }
            else if (i==1) { //Coords
                StringBuilder stringBuilder = new StringBuilder(splitData[i])
                .Replace("XYZ:", "").Replace("(", "".Replace(")", ""));
                String newStr = stringBuilder.ToString().Remove(stringBuilder.Length-2);
                String[] xyz = newStr.Split(',');
                //string x = xyz[0].Replace("XYZ:, "").Replace("(", "");
                Debug.Log("X VAL:" + xyz[0]);
                Debug.Log("Y VAL:" + xyz[1]);
                Debug.Log("Z VAL:" + xyz[2]);
                revObjTest.transform.position = new Vector3(float.Parse(xyz[0]), float.Parse(xyz[1]), float.Parse(xyz[2]));
                //TODO GET POS;
            }
            else if (i==2) { //ID
                splitData[i] = splitData[i].Replace("ID:", "");
                revObjTest.GetComponent<RevAttributes>().setId(splitData[i]);
                revId = revObjTest.GetComponent<RevAttributes>().getId();
            }
        }
        revObjPos = revObjTest.transform.position;
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
