using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;


namespace RevSocketing {

    public class onPositionChangeEvent : IExternalEventHandler {

        public static XYZ[] idCoordinates;
        public static Class1 classInstance;
        public static bool lastCallFromUnityClient = false;

        public void Execute(UIApplication app) {
            using (Transaction tx = new Transaction(classInstance.uidoc)) {
                tx.Start("On Position Change Instance");
                
                Element e = onIdPositionChanged(classInstance.ids);
                if (e != null) {
                    sendData(e);
                }
                //tx.Commit();
            }
        }

        public void sendData(Element e) {
            Autodesk.Revit.DB.LocationPoint locationPoint = e.Location as Autodesk.Revit.DB.LocationPoint;
            classInstance.sendGMDData(e.Id.ToString(), locationPoint.Point.ToString());
        }

        public Element onIdPositionChanged(List<ElementId> ids) {
            if (ids != null && idCoordinates != null) {
                for (int i = 0; i < ids.Count; i++) {
                    Element e = classInstance.uidoc.GetElement(ids[i]);
                    if (e != null && e.Location != null) {
                        Autodesk.Revit.DB.LocationPoint locationPoint = e.Location as Autodesk.Revit.DB.LocationPoint;
                        if (locationPoint.Point != null) {
                            if (!vectorIsEqual(idCoordinates[i], locationPoint.Point)) {
                                //if (!idCoordinates[i].Equals(positionPoint.Point)) {
                                Debug.WriteLine("Old Pos:" + idCoordinates[i] + " | New Pos:" + locationPoint.Point);
                                idCoordinates[i] = locationPoint.Point;
                                if (lastCallFromUnityClient) {
                                    lastCallFromUnityClient = false;
                                    return null;
                                }
                                return e;
                            }
                        }
                    }
                }
                return null;
            }
            return null;
        }

        public bool vectorIsEqual(XYZ xyz1, XYZ xyz2) {
            if (xyz1.X == xyz2.X && xyz1.Y == xyz2.Y && xyz1.Z == xyz2.Z) {
                return true;
            }
            return false;
        }

        public string GetName() {
            return "External Event Example";
        }
    }
}
