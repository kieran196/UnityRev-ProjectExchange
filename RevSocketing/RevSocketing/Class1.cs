using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using RevSocketing;

namespace RevSocketing {

    public class ExternalEventApp : IExternalApplication {

        public static ExternalEventApp thisApp = null;

        /*public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements) {
            Debug.WriteLine("Execute called for ExternalApp..");
            thisApp = this;
            return Result.Succeeded;
        }
        */
        public Result OnShutdown(UIControlledApplication application) {
            Debug.WriteLine("External Event App has Shut Down..");
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application) {
            Debug.WriteLine("External Event App has started..");
            thisApp = this;
            return Result.Succeeded;
        }

        public void ShowForm(UIApplication uiapp) {
            ExternalEventTest handler = new ExternalEventTest();
            ExternalEvent exEvent = ExternalEvent.Create(handler);
        }

    }

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class Class1 : IExternalCommand {

        private TcpClient socketConnection;
        private Thread clientReceiveThread;
        private Socket sender;
        private void sendMsgToServer(string message) {
            if (socketConnection != null) {
                NetworkStream nwStream = socketConnection.GetStream();
                byte[] bytesToSend = ASCIIEncoding.ASCII.GetBytes(message);
                Debug.WriteLine("Sending : " + message);
                nwStream.Write(bytesToSend, 0, bytesToSend.Length);
            }
        }

        private void ConnectToTcpServer(ExternalCommandData commandData) {
            try {
                clientReceiveThread = new Thread(() => ListenForData(commandData));
                //clientReceiveThread = new Thread(new ThreadStart(ListenForData));
                clientReceiveThread.IsBackground = true;
                clientReceiveThread.Start();
            } catch (Exception e) {
                Debug.WriteLine("On client connect exception " + e);
            }
        }
        private void ListenForData(ExternalCommandData commandData) {
            try {
                socketConnection = new TcpClient("localhost", 11000);
                Byte[] bytes = new Byte[1024];
                Debug.WriteLine("Listening for data..");
                while (true) {
                    // Get a stream object for reading 				
                    using (NetworkStream stream = socketConnection.GetStream()) {
                        int length;
                        // Read incomming stream into byte arrary. 					
                        while ((length = stream.Read(bytes, 0, bytes.Length)) != 0) {
                            var incommingData = new byte[length];
                            Array.Copy(bytes, 0, incommingData, 0, length);
                            // Convert byte array to string message. 						
                            string serverMessage = Encoding.ASCII.GetString(incommingData);
                            Debug.WriteLine("server message received as: " + serverMessage);
                            StringBuilder sb = new StringBuilder(serverMessage)
                                .Replace("(", "")
                                .Replace(")", "");
                            string[] splitsb = sb.ToString().Split('#');
                            Debug.WriteLine("ID=" + splitsb[0]);
                            Debug.WriteLine("COORDS=" + splitsb[1]);

                            
                            //modifyObjectPosition(uidoc, new ElementId(357285), splitsb[1]);
                            Result res = modifyObjectPosition(commandData, new ElementId(int.Parse(splitsb[0])), splitsb[1]);
                        }
                    }
                }
            } catch (SocketException socketException) {
                Debug.WriteLine("Socket exception: " + socketException);
            }
        }
        public Document uidoc = null;
        private ExternalEvent exEvent = null;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements) {
            Debug.WriteLine("Execute() called");
            UIApplication uiapp = commandData.Application;
            uidoc = uiapp.ActiveUIDocument.Document;
            Application app = uiapp.Application;
            //Creating a handler.. (Uncomment below)
            ExternalEventTest handler = new ExternalEventTest();
            exEvent = ExternalEvent.Create(handler);

            //ExternalEventApp.
            ConnectToTcpServer(commandData);

            IList<Reference> pickedObjs = uiapp.ActiveUIDocument.Selection.PickObjects(ObjectType.Element, "Select elements");
            List<ElementId> ids = (from Reference r in pickedObjs select r.ElementId).ToList();
            using (Transaction tx = new Transaction(uidoc)) {
                StringBuilder sb = new StringBuilder();
                tx.Start("transaction");
                if (pickedObjs != null && pickedObjs.Count > 0) {
                    foreach (ElementId eid in ids) {
                        Element e = uidoc.GetElement(eid);
                        //StringBuilder myParams = new StringBuilder();
                        if (e != null) {
                            Autodesk.Revit.DB.LocationPoint positionPoint = e.Location as Autodesk.Revit.DB.LocationPoint;
                            if (positionPoint != null) { // Name -> XYZ Coords -> ID
                                Debug.WriteLine("LocPoint="+ positionPoint.Point);
                                LocationPoint Lp = e.Location as LocationPoint;
                                XYZ ElementPoint = Lp.Point as XYZ;
                                //modifyObjectPosition(uidoc, eid, new XYZ(10, 10, 10));
                                sb.Append("\n" + e.Name + "# XYZ:" + ElementPoint + " #ID:" + eid);
                            }
                        }
                    }
                    // Uncomment below..
                    sendMsgToServer(sb.ToString());
                    TaskDialog.Show("title:", sb.ToString());
                }
                tx.Commit();
            }
            return Result.Succeeded;
        }

        public Result modifyObjectPosition(ExternalCommandData commandData, ElementId eid, string translation) {
            Element e = uidoc.GetElement(eid);
            string[] XYZSplit = translation.Split(',');
            XYZ xyztranslation = new XYZ(float.Parse(XYZSplit[0]), float.Parse(XYZSplit[2]), float.Parse(XYZSplit[1]));


            Debug.WriteLine("Setting new XYZ translation:" + xyztranslation.ToString());
            ExternalEventTest.xyztranslation = xyztranslation;
            ExternalEventTest.eid = eid;
            ExternalEventTest.uidoc = uidoc;
            exEvent.Raise();

            // Moving the object is causing exception..
            //e.Location.Move(xyztranslation);
            /*using (Transaction tx = new Transaction(uidoc)) {
                tx.Start("Transaction Name");
                try {
                    //e.Location.Move(xyztranslation);
                    ElementTransformUtils.MoveElement(uidoc, eid, xyztranslation);
                } catch { }
                tx.Commit();
            }*/
            return Result.Succeeded;
        }

        public void sendToServer(string message) {
            byte[] msg = Encoding.ASCII.GetBytes(message);
            Debug.WriteLine("Message sent to server:" + message);
            // Send the data through the socket.    
            int bytesSent = sender.Send(msg);
            // Close socket..
            sender.Shutdown(SocketShutdown.Both);
            sender.Close();
        }

        /*public static void StartClient(string sendMsg, Document uidoc) {
            byte[] bytes = new byte[1024];

            try {
                // Connect to a Remote server  
                // Get Host IP Address that is used to establish a connection  
                // In this case, we get one IP address of localhost that is IP : 127.0.0.1  
                // If a host has multiple addresses, you will get a list of addresses  
                IPHostEntry host = Dns.GetHostEntry("localhost");
                IPAddress ipAddress = host.AddressList[0];
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);

                // Create a TCP/IP  socket.    
                Socket sender = new Socket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

                // Connect the socket to the remote endpoint. Catch any errors.    
                try {
                    // Connect to Remote EndPoint  
                    sender.Connect(remoteEP);

                    Debug.WriteLine("Socket connected to {0}",
                        sender.RemoteEndPoint.ToString());

                    // Encode the data string into a byte array.    
                    //byte[] msg = Encoding.ASCII.GetBytes("This is a test<EOF>");
                    byte[] msg = Encoding.ASCII.GetBytes(sendMsg);

                    // Send the data through the socket.    
                    int bytesSent = sender.Send(msg);

                    // Receive the response from the remote device.    
                    int bytesRec = sender.Receive(bytes);
                    StringBuilder sb = new StringBuilder(Encoding.ASCII.GetString(bytes, 0, bytesRec))
                        .Replace("(", "")
                        .Replace(")", "");
                    string[] splitsb = sb.ToString().Split('#');
                    Debug.WriteLine("ID="+splitsb[0]);
                    Debug.WriteLine("COORDS="+splitsb[1]);
                    modifyObjectPosition(uidoc, new ElementId(int.Parse(splitsb[0])), splitsb[1]);
                    //Debug.WriteLine("Received:",
                    //   Encoding.ASCII.GetString(bytes, 0, bytesRec));

                    // Release the socket.    
                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();

                } catch (ArgumentNullException ane) {
                    Debug.WriteLine("ArgumentNullException : {0}", ane.ToString());
                } catch (SocketException se) {
                    Debug.WriteLine("SocketException : {0}", se.ToString());
                } catch (Exception e) {
                    Debug.WriteLine("Unexpected exception : {0}", e.ToString());
                }

            } catch (Exception e) {
                Debug.WriteLine(e.ToString());
            }
        }*/
    }
}