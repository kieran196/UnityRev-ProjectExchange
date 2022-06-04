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
using Autodesk.Revit.DB.Events;
using System.IO;

namespace RevSocketing {

    public class ExternalEventApp : IExternalApplication {

        public static ExternalEventApp thisApp = null;

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
    public class socketClient : IExternalCommand {

        private TcpClient socketConnection;
        private Thread clientReceiveThread;
        private Thread mainLoopThread;
        private Socket sender;
        private bool updatedMetadata = false;

        private readonly int MESSAGE_DELAY = 200; // Adjust to increase the speed messages are sent to the server (in ms).

        private void sendMsgToServer(string message) {
            if (socketConnection != null) {
                NetworkStream nwStream = socketConnection.GetStream();
                byte[] bytesToSend = ASCIIEncoding.ASCII.GetBytes(message);
                Debug.WriteLine("Sending : " + message);
                nwStream.Write(bytesToSend, 0, bytesToSend.Length);
                Thread.Sleep(MESSAGE_DELAY);
            }
        }
        /*
         * Sends geometric data information (Position, Rotation, and Scale) to the server.
         * */
        public void sendGMDData(string eleId, string type, string newGMDValue) {
            String sendData = "";
            if (type == "POS") {
                sendData = "GMDP" + eleId + "#" + newGMDValue;
            } else if (type == "ROT") {
                sendData = "GMDR" + eleId + "#" + newGMDValue;
            } else if (type == "SCALEM") {
                sendData = "GMDS" + eleId + "#" + newGMDValue;
            }
            sendMsgToServer(sendData);
        }
        /*
         * Sends error information to the server.
         * */
        public void sendERRData(string errorType) {
            String sendData = "ERR#"+ errorType;
            sendMsgToServer(sendData);
        }
        /*
         * Sends mesh data to the server.
         * */
        public void sendMeshData(string eleId, string meshData, string type, int trisCount) {
            string sendData = "";
            if (type == "tris")
            {
                sendData = "MDT" + eleId + "#" + meshData;
            } else if (type == "verts")
            {
                sendData = "MDV" + eleId + "#" + trisCount + "#" + meshData;
            }
            sendMsgToServer(sendData);
        }

        public void sendMeshData(string eleId, string meshData, string type) {
            string sendData = "";
            if (type == "tris") {
                sendData = "MDT" + eleId + "#" + meshData;
            } else if (type == "verts") {
                sendData = "MDV" + eleId + "#" + meshData;
            }
            sendMsgToServer(sendData);
        }

        public void mainLoop() {
                while (true) {
                    chPosEvent.Raise();
                }
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

        private void readIncomingData(string serverMessage, ExternalCommandData commandData) {
            if (serverMessage.StartsWith("GMDP")) { //GEOMETRIC DATA
                onPositionChangeEvent.lastCallFromUnityClient = true;
                serverMessage = serverMessage.Substring(4);
                StringBuilder sb = new StringBuilder(serverMessage)
                    .Replace("(", "")
                    .Replace(")", "");
                string[] splitsb = sb.ToString().Split('#');
                Debug.WriteLine("ID=" + splitsb[0]);
                Debug.WriteLine("COORDS=" + splitsb[1]);
                Result res = modifyObjectGMD(commandData, new ElementId(int.Parse(splitsb[0])), "POS", splitsb[1]);
            } else if (serverMessage.StartsWith("GMDR")) { //GEOMETRIC DATA
                onPositionChangeEvent.lastCallFromUnityClient = true;
                serverMessage = serverMessage.Substring(4);
                StringBuilder sb = new StringBuilder(serverMessage)
                    .Replace("(", "")
                    .Replace(")", "");
                string[] splitsb = sb.ToString().Split('#');
                Debug.WriteLine("ID=" + splitsb[0]);
                Debug.WriteLine("COORDS=" + splitsb[1]);
                Result res = modifyObjectGMD(commandData, new ElementId(int.Parse(splitsb[0])), "ROT", splitsb[1]);
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
            } else if (serverMessage.StartsWith("SMD")) {
                sendMeshDataToServer();
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
                            readIncomingData(serverMessage, commandData);
                        }
                    }
                }
            } catch (SocketException socketException) {
                Debug.WriteLine("Socket exception: " + socketException);
            }
        }

        private void sendMeshDataToServer() {
            foreach (ElementId id in ids) {
                Element e = uidoc.GetElement(id);
                meshSend.sendMeshData(e);
            }
        }

        private List<ElementId> getAllElementsInDocById(Document doc) {
            List<ElementId> elements = new List<ElementId>();
            FilteredElementCollector collector = new FilteredElementCollector(doc).WhereElementIsNotElementType();
            foreach (Element e in collector) {
                if (e.Category != null && e.Category.HasMaterialQuantities) {
                    elements.Add(e.Id);
                }
            }
            return elements;
        }

        private List<Element> getAllElementsInDoc(Document doc) {
            List<Element> elements = new List<Element>();
            FilteredElementCollector collector = new FilteredElementCollector(doc).WhereElementIsNotElementType();
            foreach (Element e in collector) {
                if (e.Category != null && e.Category.HasMaterialQuantities) {
                    elements.Add(e);
                }
            }
            return elements;
        }

        public Material GetMaterial(Document doc, FamilyInstance fi) {
            Material material = null;
            foreach (Parameter p in fi.Parameters) {
                Definition def = p.Definition;

                // the material is stored as element id:

                if (p.StorageType == StorageType.ElementId
                  && def.ParameterGroup == BuiltInParameterGroup.PG_MATERIALS
                  && def.ParameterType == ParameterType.Material) {
                    ElementId materialId = p.AsElementId();

                    if (-1 == materialId.IntegerValue) {
                        // invalid element id, so we assume
                        // the material is "By Category":

                        if (null != fi.Category) {
                            material = fi.Category.Material;

                            if (null == material) {
                                //MaterialOther mat
                                //  = doc.Settings.Materials.AddOther(
                                //    "GoodConditionMat" ); // 2011

                                ElementId id = Material.Create(doc, "GoodConditionMat"); // 2012
                                Material mat = doc.GetElement(id) as Material;

                                mat.Color = new Color(255, 0, 0);

                                fi.Category.Material = mat;

                                material = fi.Category.Material;
                            }
                        }
                    }
                    break;
                }
            }
            return material;
        }

        // Revit API doesn't support new creation of parameters to elements programatically..

        /*private void RawCreateProjectParameter(Application app, string name, ParameterType type, bool visible, CategorySet cats, BuiltInParameterGroup group, bool inst)
        {
            string oriFile = app.SharedParametersFilename;
            string tempFile = Path.GetTempFileName() + ".txt";
            using (File.Create(tempFile)) { }
            app.SharedParametersFilename = tempFile;

            var defOptions = new ExternalDefinitionCreationOptions(name, type)
            {
                Visible = visible
            };
            ExternalDefinition def = app.OpenSharedParameterFile().Groups.Create("TemporaryDefintionGroup").Definitions.Create(defOptions) as ExternalDefinition;

            app.SharedParametersFilename = oriFile;
            File.Delete(tempFile);

            Autodesk.Revit.DB.Binding binding = app.Create.NewTypeBinding(cats);
            if (inst) binding = app.Create.NewInstanceBinding(cats);

            BindingMap map = (new UIApplication(app)).ActiveUIDocument.Document.ParameterBindings;
            if (!map.Insert(def, binding, group))
            {
                Trace.WriteLine($"Failed to create Project parameter '{name}' :(");
            }
        }

        private void createSharedParamTest(Document doc, Application app)
        {
            Category mats = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Materials);
            CategorySet cats = app.Create.NewCategorySet();
            cats.Insert(mats);
            RawCreateProjectParameter(app, "newParamNameTest", ParameterType.Text, true, cats, BuiltInParameterGroup.PG_IDENTITY_DATA, true);
        }*/

        public void GetMatPaints(Element e, Document doc) {
            Options geometryOptions = new Options();
            GeometryElement geoEle = e.get_Geometry(geometryOptions);
            foreach (GeometryObject geoObject in geoEle) {
                GeometryInstance geoInstance = geoObject as GeometryInstance;
                if (geoInstance != null) {
                    GeometryElement instanceGeometryElement = geoInstance.GetSymbolGeometry();
                    foreach (GeometryObject instObj in instanceGeometryElement) {
                        Solid solid = instObj as Solid;
                        if (solid == null) return;
                        foreach (Face face in solid.Faces) {
                            ElementId mats = doc.GetPaintedMaterial(e.Id, face);
                            Debug.WriteLine("Mat paint face:"+mats.ToString() + " | " + doc.IsPainted(e.Id, face));
                            //doc.Paint()
                        }
                    }
                }
            }
        }

        public Document uidoc = null;
        private ExternalEvent upEvent = null; // Update Pos Event
        private ExternalEvent obcEvent = null; // On BIM change Event
        private ExternalEvent chPosEvent = null; // Update on Pos change Event
        public List<ElementId> ids;
        private meshSender meshSend;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements) {
            Debug.WriteLine("Execute() called");
            onPositionChangeEvent.classInstance = this;
            meshSender.classInstance = this;
            updatePosEvent.classInstance = this;
            UIApplication uiapp = commandData.Application;
            uidoc = uiapp.ActiveUIDocument.Document;
            onPositionChangeEvent.doc = uidoc;
            Application app = uiapp.Application;
            //Creating a handler.. (Uncomment below)
            meshSend = new meshSender();
            updatePosEvent posHandler = new updatePosEvent();
            onDataChangeEvent dataHandler = new onDataChangeEvent();
            onPositionChangeEvent posChangeHandler = new onPositionChangeEvent();
            upEvent = ExternalEvent.Create(posHandler);
            obcEvent = ExternalEvent.Create(dataHandler);
            chPosEvent = ExternalEvent.Create(posChangeHandler);
            app.DocumentChanged += new EventHandler<DocumentChangedEventArgs>(onElementCreated.OnNewElementCreated);
            warningEvents.updatePosHandler = posHandler;

            //ExternalEventApp.
            ConnectToTcpServer(commandData);
            //IList<Reference> pickedObjs = uiapp.ActiveUIDocument.Selection.PickObjects(ObjectType.Element, "Select elements");
            //ids = (from Reference r in pickedObjs select r.ElementId).ToList();
            ids = getAllElementsInDocById(uidoc);
            //List<Element> eles = getAllElementsInDoc(uidoc);

            onPositionChangeEvent.idCoordinates = new XYZ[ids.Count];
            onPositionChangeEvent.idBoundingBoxMax = new XYZ[ids.Count];
            onPositionChangeEvent.idBoundingBoxMaxOriginal = new XYZ[ids.Count];
            onPositionChangeEvent.idBoundingBoxMin = new XYZ[ids.Count];
            onPositionChangeEvent.idRotations = new double[ids.Count];
            using (Transaction tx = new Transaction(uidoc)) {
                StringBuilder sb = new StringBuilder("init");
                tx.Start("transaction");
                int count = 0;
                int loopCount = 0;
                if (ids != null && ids.Count > 0) {
                    // Iterates through all elements and send BIM data to server.
                    foreach (ElementId eid in ids) {
                        Element e = uidoc.GetElement(eid);
                            // Experimental.. Trying to send mesh data over network. (Mostly works)
                            //meshSend.sendMeshData(e);
                            onPositionChangeEvent.idCoordinates[loopCount] = XYZ.Zero;
                        onPositionChangeEvent.idBoundingBoxMaxOriginal[loopCount] = XYZ.Zero;
                        onPositionChangeEvent.idBoundingBoxMax[loopCount] = XYZ.Zero;
                        onPositionChangeEvent.idBoundingBoxMin[loopCount] = XYZ.Zero;
                        onPositionChangeEvent.idRotations[loopCount] = 0;
                        if (e != null) {
                            //GetMatPaints(e, uidoc);
                            ElementId neweid = e.Id;
                            Debug.WriteLine("Element ID:" + e.Id + " , " + e.UniqueId + " , " + neweid);
                            BoundingBoxXYZ bbox = e.get_BoundingBox(uidoc.ActiveView);
                            if (bbox != null) {
                                onPositionChangeEvent.idBoundingBoxMaxOriginal[loopCount] = bbox.Max;
                                onPositionChangeEvent.idBoundingBoxMax[loopCount] = bbox.Max;
                                onPositionChangeEvent.idBoundingBoxMax[loopCount] = bbox.Min;
                            }
                            if (eid != null) {
                                sb.Append("\n" + "ID:" + eid + "# ");
                            }
                            if (e.Name != null && e.Name.Length >= 2) {
                                sb.Append(e.Name + "# ");
                            } else {
                                sb.Append("null # ");
                            }
                            Autodesk.Revit.DB.LocationPoint positionPoint = e.Location as Autodesk.Revit.DB.LocationPoint;
                            if (positionPoint != null) { // Name -> XYZ Coords -> ID
                                Debug.WriteLine("LocPoint="+ positionPoint.Point);
                                LocationPoint Lp = e.Location as LocationPoint;
                                updatePosEvent.startingPos = Lp.Point;
                                onPositionChangeEvent.idCoordinates[loopCount] = Lp.Point;
                                onPositionChangeEvent.idRotations[loopCount] = Lp.Rotation;
                                //sb.Append("\n" + "ID:" + eid + "# " +e.Name + "# XYZ:" + Lp.Point + "#");
                                sb.Append("XYZ:" + Lp.Point + "#");
                            } else { // LocationPoint is null..
                                sb.Append("XYZ:" + XYZ.Zero + "#");
                            }
                            foreach (Parameter param in e.Parameters) {
                                //sb.Append("# " +GetParameterInformation(param, uidoc));
                                sb.Append(GetParameterInformation(param, uidoc));
                            }
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

        public Result modifyObjectGMD(ExternalCommandData commandData, ElementId eid, string type, string translation) {
            Element e = uidoc.GetElement(eid);
            if (type == "POS") {
                string[] XYZSplit = translation.Split(',');
                XYZ xyztranslation = new XYZ(float.Parse(XYZSplit[0]), (float.Parse(XYZSplit[1])), float.Parse(XYZSplit[2]));
                updatePosEvent.xyztranslation = xyztranslation;
            } else if (type == "ROT") {
                updatePosEvent.rotTranslation = double.Parse(translation);
            }
            updatePosEvent.eid = eid;
            updatePosEvent.uidoc = uidoc;
            updatePosEvent.eventType = type;
            upEvent.Raise();
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
    }
}