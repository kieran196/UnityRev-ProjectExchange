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
        public static Document uidoc = null;
        public static Class1 classInstance;

        public XYZ transformUnityCoords(XYZ unityCoords)
        {
            return new XYZ(-unityCoords.X, -unityCoords.Z, unityCoords.Y);
        }

        public void invalidCoordinateException(warningEvents.ERROR_TYPES errorType) {
            classInstance.sendERRData(errorType.ToString());
        }

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
                // Unity Y coord = X, Z coord = Y. (For the door).
                Lp.Point += transformUnityCoords(xyztranslation);
                tx.Commit();
            }
        }

        public string GetName() {
            return "External Event Example";
        }
    }
}
