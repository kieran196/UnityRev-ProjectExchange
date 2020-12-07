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
using Autodesk.Revit.Exceptions;

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
            updatePosEvent handler = new updatePosEvent();
            ExternalEvent exEvent = ExternalEvent.Create(handler);
        }

    }

    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class Class1 : IExternalCommand {

        private TcpClient socketConnection;
        private Thread clientReceiveThread;
        private Thread mainLoopThread;
        private Socket sender;
        private void sendMsgToServer(string message) {
            if (socketConnection != null) {
                NetworkStream nwStream = socketConnection.GetStream();
                byte[] bytesToSend = ASCIIEncoding.ASCII.GetBytes(message);
                Debug.WriteLine("Sending : " + message);
                nwStream.Write(bytesToSend, 0, bytesToSend.Length);
                Thread.Sleep(100); // 100ms sleep.
            }
        }

        public void sendGMDData(string eleId, string newObjPos) {
            String sendData = "GMD" + eleId + "#" + newObjPos;
            sendMsgToServer(sendData);
        }

        public void sendERRData(string errorType) {
            String sendData = "ERR#"+ errorType;
            sendMsgToServer(sendData);
        }

        public void sendMeshData(string eleId, string meshData, string type) {
            string sendData = "";
            if (type == "tris")
            {
                sendData = "MDT" + eleId + "#" + meshData;
            } else if (type == "verts")
            {
                sendData = "MDV" + eleId + "#" + meshData;
            }
            sendMsgToServer(sendData);
        }

        public void mainLoop() {
            //try {
                while (true) {
                    chPosEvent.Raise();
                    /*Element e = onIdPositionChanged();
                    if (e != null) {
                    Debug.WriteLine(e.Name + " position has been modified..");
                        Autodesk.Revit.DB.LocationPoint locationPoint = e.Location as Autodesk.Revit.DB.LocationPoint;
                        sendGMDData(e.Id.ToString(), locationPoint.Point.ToString());
                    }*/
                }
            //} catch (InternalException e) {
            //    Debug.WriteLine("Main loop internal exception " + e);
            //}
        }

        private void initializeMainLoop() {
            try {
                mainLoopThread = new Thread(() => mainLoop());
                mainLoopThread.IsBackground = true;
                mainLoopThread.Start();
            } catch (Exception e) {
                Debug.WriteLine("Exception occured initializing main loop " + e);
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
        private bool updatedMetadata = false;
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
                            if (serverMessage.StartsWith("GMD")) { //GEOMETRIC DATA
                                onPositionChangeEvent.lastCallFromUnityClient = true;
                                serverMessage = serverMessage.Substring(3);
                                StringBuilder sb = new StringBuilder(serverMessage)
                                    .Replace("(", "")
                                    .Replace(")", "");
                                string[] splitsb = sb.ToString().Split('#');
                                Debug.WriteLine("ID=" + splitsb[0]);
                                Debug.WriteLine("COORDS=" + splitsb[1]);
                                //modifyObjectPosition(uidoc, new ElementId(357285), splitsb[1]);
                                Result res = modifyObjectPosition(commandData, new ElementId(int.Parse(splitsb[0])), splitsb[1]);
                            } else if (serverMessage.StartsWith("BIM")) { //BIM DATA.
                                serverMessage = serverMessage.Substring(3);
                                StringBuilder sb = new StringBuilder(serverMessage);
                                string[] splitsb = sb.ToString().Split('#');
                                splitsb[1] = splitsb[1].TrimStart().TrimEnd();
                                splitsb[2] = splitsb[2].TrimStart().TrimEnd();
                                Debug.WriteLine("ID=" + splitsb[0]);
                                Debug.WriteLine("KEY=" + splitsb[1]);
                                Debug.WriteLine("VALUE=" + splitsb[2]);
                                Result RES = modifyMetaData(new ElementId(int.Parse(splitsb[0])), splitsb[1], splitsb[2]);
                            }
                        }
                    }
                }
            } catch (SocketException socketException) {
                Debug.WriteLine("Socket exception: " + socketException);
            }
        }

        /*public Element onIdPositionChanged() {
            if (ids != null && idCoordinates != null) {
                for (int i = 0; i < ids.Count; i++) {
                    Element e = uidoc.GetElement(ids[i]);
                    if (e != null && e.Location != null) {
                        Autodesk.Revit.DB.LocationPoint locationPoint = e.Location as Autodesk.Revit.DB.LocationPoint;
                        if (locationPoint.Point != null) {
                            if (!vectorIsEqual(idCoordinates[i], locationPoint.Point)) {
                                //if (!idCoordinates[i].Equals(positionPoint.Point)) {
                                Debug.WriteLine("Old Pos:" + idCoordinates[i] + " | New Pos:" + locationPoint.Point);
                                idCoordinates[i] = locationPoint.Point;
                                return e;
                            }
                        }
                    }
                }
                return null;
            }
            return null;
        }*/

        private List<ElementId> getAllElementsInDoc(Document doc) {
            List<ElementId> elements = new List<ElementId>();
            FilteredElementCollector collector = new FilteredElementCollector(doc).WhereElementIsNotElementType();
            foreach (Element e in collector) {
                if (e.Category != null && e.Category.HasMaterialQuantities) {
                    elements.Add(e.Id);
                }
            }
            return elements;
        }

        public Document uidoc = null;
        private ExternalEvent upEvent = null; // Update Pos Event
        private ExternalEvent obcEvent = null; // On BIM change Event
        private ExternalEvent chPosEvent = null; // Update on Pos change Event
        public List<ElementId> ids;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements) {
            Debug.WriteLine("Execute() called");
            onPositionChangeEvent.classInstance = this;
            meshSender.classInstance = this;
            updatePosEvent.classInstance = this;
            UIApplication uiapp = commandData.Application;
            uidoc = uiapp.ActiveUIDocument.Document;
            Application app = uiapp.Application;
            //Creating a handler.. (Uncomment below)
            meshSender meshSend = new meshSender();
            updatePosEvent posHandler = new updatePosEvent();
            onDataChangeEvent dataHandler = new onDataChangeEvent();
            onPositionChangeEvent posChangeHandler = new onPositionChangeEvent();
            upEvent = ExternalEvent.Create(posHandler);
            obcEvent = ExternalEvent.Create(dataHandler);
            chPosEvent = ExternalEvent.Create(posChangeHandler);
            warningEvents.updatePosHandler = posHandler;
            //ExternalEventApp.
            ConnectToTcpServer(commandData);
            //IList<Reference> pickedObjs = uiapp.ActiveUIDocument.Selection.PickObjects(ObjectType.Element, "Select elements");
            //ids = (from Reference r in pickedObjs select r.ElementId).ToList();
            ids = getAllElementsInDoc(uidoc);
            onPositionChangeEvent.idCoordinates = new XYZ[ids.Count];
            using (Transaction tx = new Transaction(uidoc)) {
                StringBuilder sb = new StringBuilder("init");
                tx.Start("transaction");

                //Get Point Coordinates
                /*ElementCategoryFilter filter = new ElementCategoryFilter(BuiltInCategory.OST_ProjectBasePoint);

                FilteredElementCollector collector = new FilteredElementCollector(uidoc);
                IList<Element> newElements = collector.WherePasses(filter).ToElements();

                foreach (Element element in newElements)
                {
                    double x = element.get_Parameter(BuiltInParameter.BASEPOINT_EASTWEST_PARAM).AsDouble();
                    double y = element.get_Parameter(BuiltInParameter.BASEPOINT_NORTHSOUTH_PARAM).AsDouble();
                    double elevation = element.get_Parameter(BuiltInParameter.BASEPOINT_ELEVATION_PARAM).AsDouble();
                    XYZ projectBasePoint = new XYZ(x, y, elevation);
                    Debug.WriteLine("Base Points:" + projectBasePoint.ToString());
                }*/


                int count = 0;
                int loopCount = 0;
                if (ids != null && ids.Count > 0) {
                    foreach (ElementId eid in ids) {
                        Element e = uidoc.GetElement(eid);
                        // Experimental.. Trying to send mesh data over network. (Mostly works)
                        //meshSend.sendMeshData(e);
                        onPositionChangeEvent.idCoordinates[loopCount] = XYZ.Zero;
                        if (e != null) {
                            ElementId neweid = e.Id;
                            Debug.WriteLine("Element ID:" + e.Id + " , " + e.UniqueId + " , " + neweid);
                            GeometryElement geoEle = e.get_Geometry(new Options());
                            if (geoEle != null) {
                                foreach (GeometryObject geoObject in geoEle)
                                {
                                    GeometryInstance inst = geoObject as GeometryInstance;
                                    if (inst != null)
                                    {
                                        Debug.WriteLine("GEOID:" + eid + " | " + inst.Transform.Origin);
                                    }
                                }
                                BoundingBoxXYZ box = geoEle.GetBoundingBox();
                                if (box != null)
                                {
                                    Debug.WriteLine("BBID:" + eid + " | " + box.Transform.Origin);
                                }
                            }
                            Autodesk.Revit.DB.LocationPoint positionPoint = e.Location as Autodesk.Revit.DB.LocationPoint;
                            if (positionPoint != null) { // Name -> XYZ Coords -> ID
                                Debug.WriteLine("LocPoint="+ positionPoint.Point);
                                LocationPoint Lp = e.Location as LocationPoint;
                                updatePosEvent.startingPos = Lp.Point;
                                onPositionChangeEvent.idCoordinates[loopCount] = Lp.Point;
                                sb.Append("\n" + "ID:" + eid + "# " +e.Name + "# XYZ:" + Lp.Point + "#");
                            }
                            foreach (Parameter param in e.Parameters) {
                                //sb.Append("# " +GetParameterInformation(param, uidoc));
                                sb.Append(GetParameterInformation(param, uidoc));
                            }
                            /*if (count < ids.Count-1) {
                                Debug.WriteLine("Sending to server..");
                                sendMsgToServer(sb.ToString());
                                sb = new StringBuilder("");
                            }*/
                            count++;
                        }
                        Debug.WriteLine("Sending to server..");
                        sendMsgToServer(sb.ToString());
                        sb = new StringBuilder("init");
                        loopCount++;
                    }
                    // Uncomment below..
                    //TaskDialog.Show("title:", sb.ToString());
                }
                tx.Commit();
            }
            initializeMainLoop();
            return Result.Succeeded;
        }

        String GetParameterInformation(Parameter para, Document document) {
            //string defName = para.Definition.Name + @"\t";
            if (para == null) {
                Debug.WriteLine("Parameter was null..");
                return null;
            }
            string defName = " : " + para.Definition.Name;
            // Use different method to get parameter data according to the storage type
            switch (para.StorageType) {
                case StorageType.Double:
                //covert the number into Metric
                defName += " : " + para.AsValueString();
                break;
                case StorageType.ElementId:
                //find out the name of the element
                Autodesk.Revit.DB.ElementId id = para.AsElementId();
                if (id.IntegerValue >= 0) {
                    defName += " : " + document.GetElement(id).Name;
                } else {
                    defName += " : " + id.IntegerValue.ToString();
                }
                break;
                case StorageType.Integer:
                if (ParameterType.YesNo == para.Definition.ParameterType) {
                    if (para.AsInteger() == 0) {
                        defName += " : " + "False";
                    } else {
                        defName += " : " + "True";
                    }
                } else {
                    defName += " : " + para.AsInteger().ToString();
                }
                break;
                case StorageType.String:
                defName += " : " + para.AsString();
                break;
                default:
                defName = "Unexposed parameter.";
                break;
            }

            return defName;
        }

        public Result modifyMetaData(ElementId eid, string key, string value) {
            Element e = uidoc.GetElement(eid);
            ElementId paramValue = new ElementId(BuiltInParameter.ELEM_TYPE_PARAM);

            //string param = GetParameterInformation(e.LookupParameter(key), uidoc);
            Parameter myParam = e.LookupParameter(key);
            onDataChangeEvent.uidoc = uidoc;
            onDataChangeEvent.param = myParam;
            onDataChangeEvent.newValue = value;
            obcEvent.Raise();
            updatedMetadata = true;
            return Result.Succeeded;
        }

        public Result modifyObjectPosition(ExternalCommandData commandData, ElementId eid, string translation) {
            Element e = uidoc.GetElement(eid);
            string[] XYZSplit = translation.Split(',');
            //XYZ xyztranslation = new XYZ(float.Parse(XYZSplit[0]), -(float.Parse(XYZSplit[2])), float.Parse(XYZSplit[1]));
            XYZ xyztranslation = new XYZ(float.Parse(XYZSplit[0]), (float.Parse(XYZSplit[1])), float.Parse(XYZSplit[2]));

            Debug.WriteLine("Setting new XYZ translation:" + xyztranslation.ToString());
            updatePosEvent.xyztranslation = xyztranslation;
            updatePosEvent.eid = eid;
            updatePosEvent.uidoc = uidoc;
            upEvent.Raise();

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