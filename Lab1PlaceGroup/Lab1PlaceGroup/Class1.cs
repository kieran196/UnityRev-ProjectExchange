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

namespace Lab1PlaceGroup {
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class Class1 : IExternalCommand {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements) {
            Debug.WriteLine("Execute() called");
            StartClient("Message tester..");
            UIApplication uiapp = commandData.Application;
            Document uidoc = uiapp.ActiveUIDocument.Document;
            Application app = uiapp.Application;
            IList<Reference> pickedObjs = uiapp.ActiveUIDocument.Selection.PickObjects(ObjectType.Element, "Select elements");
            List<ElementId> ids = (from Reference r in pickedObjs select r.ElementId).ToList();
            using (Transaction tx = new Transaction(uidoc)) {
                StringBuilder sb = new StringBuilder();
                tx.Start("transaction");
                if (pickedObjs != null && pickedObjs.Count > 0) {
                    foreach (ElementId eid in ids) {
                        Element e = uidoc.GetElement(eid);
                        //StringBuilder myParams = new StringBuilder();
                        Autodesk.Revit.DB.LocationPoint positionPoint = e.Location as Autodesk.Revit.DB.LocationPoint;
                        sb.Append("\n" + e.Name + " param:" + positionPoint.Point);
                    }
                    TaskDialog.Show("title:", sb.ToString());
                }
                tx.Commit();
            }
            return Result.Succeeded;
        }
		
        public static void StartClient(string sendMsg) {
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

                    Console.WriteLine("Socket connected to {0}",
                        sender.RemoteEndPoint.ToString());

                    // Encode the data string into a byte array.    
                    //byte[] msg = Encoding.ASCII.GetBytes("This is a test<EOF>");
		            byte[] msg = Encoding.ASCII.GetBytes(sendMsg);

                    // Send the data through the socket.    
                    int bytesSent = sender.Send(msg);

                    // Receive the response from the remote device.    
                    int bytesRec = sender.Receive(bytes);
                    Console.WriteLine("Echoed test = {0}",
                        Encoding.ASCII.GetString(bytes, 0, bytesRec));

                    // Release the socket.    
                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();

                } catch (ArgumentNullException ane) {
                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                } catch (SocketException se) {
                    Console.WriteLine("SocketException : {0}", se.ToString());
                } catch (Exception e) {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }

            } catch (Exception e) {
                Console.WriteLine(e.ToString());
            }
        }

        /*public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements) {
            Console.WriteLine("Testing..");
            //Get application and documnet objects
            UIApplication uiapp = commandData.Application;
            Document doc = uiapp.ActiveUIDocument.Document;

            //Define a reference Object to accept the pick result
            Reference pickedref = null;

            //Pick a group
            Selection sel = uiapp.ActiveUIDocument.Selection;
            pickedref = sel.PickObject(ObjectType.Element, "Please select a group");
            Console.WriteLine("Picked Ref:" + pickedref.ToString());
            Element elem = doc.GetElement(pickedref);
            Group group = elem as Group;

            //Pick point
            XYZ point = sel.PickPoint("Please pick a point to place group");

            //Place the group
            Transaction trans = new Transaction(doc);
            trans.Start("Lab");
            doc.Create.PlaceGroup(point, group.GroupType);
            trans.Commit();

            return Result.Succeeded;
        }*/
    }
}