using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevSocketing {
    /*
     * TODO.
     * - Create an OnNewParameter() method that sends the new parameter to Unity when a new parameter for an element has been created.
     */
    public class onElementCreated {
        public static void OnNewElementCreated(object sender, DocumentChangedEventArgs e) {
            Document doc = e.GetDocument();
            List<ElementId> newEleIDs = e.GetAddedElementIds().ToList();
            if (newEleIDs != null && newEleIDs.Count > 1) {
                foreach (ElementId eid in newEleIDs) {
                    Element ele = doc.GetElement(eid);
                    Debug.WriteLine("New element created:" + ele.Name);
                }
            }
        }
    }
}
