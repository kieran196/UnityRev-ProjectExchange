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

    public class ExternalEventTest : IExternalEventHandler {

        public static ElementId eid;
        public static XYZ xyztranslation;
        public static Document uidoc = null;

        public void Execute(UIApplication app) {
            Debug.WriteLine("External event has been executed..");
            using (Transaction tx = new Transaction(uidoc)) {
                tx.Start("Creating Assembly Instance");
                Element e = uidoc.GetElement(eid);
                LocationPoint Lp = e.Location as LocationPoint;
                Lp.Point = xyztranslation;
                //e.Location.Move(xyztranslation);
                //ElementTransformUtils.MoveElement(uidoc, eid, xyztranslation);
                tx.Commit();
            }
        }

        public string GetName() {
            return "External Event Example";
        }
    }
}
