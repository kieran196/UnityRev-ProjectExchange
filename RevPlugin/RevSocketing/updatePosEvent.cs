using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RevSocketing {

    public class updatePosEvent : IExternalEventHandler {

        public static ElementId eid;
        public static XYZ startingPos;
        public static XYZ xyztranslation;
        public static double rotTranslation;
        public static Document uidoc = null;
        public static socketClient classInstance;
        public static string eventType = "";
        public XYZ transformUnityCoords(XYZ unityCoords)
        {
            return new XYZ(-unityCoords.X, -unityCoords.Z, unityCoords.Y);
        }

        public void invalidCoordinateException(warningEvents.ERROR_TYPES errorType) {
            classInstance.sendERRData(errorType.ToString());
        }
        private readonly float ROT_CONSTANT = 57.295F;
        public void Execute(UIApplication app) {
            Debug.WriteLine("External event has been executed..");
            using (Transaction tx = new Transaction(uidoc)) {
                tx.Start("Creating Assembly Instance");
                
                FailureHandlingOptions failOpt = tx.GetFailureHandlingOptions();
                warningEvents warningEv = new warningEvents();
                failOpt.SetFailuresPreprocessor(warningEv);
                tx.SetFailureHandlingOptions(failOpt);
                Element e = uidoc.GetElement(eid);
                LocationPoint Lp = e.Location as LocationPoint;
                if (eventType == "POS") {
                    // Unity Y coord = X, Z coord = Y. (For the door).
                    Lp.Point += transformUnityCoords(xyztranslation);
                } else if (eventType == "ROT") {
                    Line axisLine = Line.CreateBound(Lp.Point, Lp.Point.Add(XYZ.BasisZ));
                    // Rotate
                    ElementTransformUtils.RotateElement(uidoc, eid, axisLine, rotTranslation / ROT_CONSTANT);
                }
                tx.Commit();
            }
        }

        public string GetName() {
            return "External Event Example";
        }
    }
}
