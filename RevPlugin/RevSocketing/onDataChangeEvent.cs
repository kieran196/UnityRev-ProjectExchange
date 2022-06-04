using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
//TODO - Currently only works with Strings. Need to provide support for doubles, ints, bools etc..
// (Shouldn't be too difficult)
namespace RevSocketing {

    public class onDataChangeEvent : IExternalEventHandler {
        public static Document uidoc = null;
        public static Parameter param = null;
        public static string newValue = null;

        public void Execute(UIApplication app) {
            if (param == null) {
                return;
            }
            using (Transaction tx = new Transaction(uidoc)) {
                tx.Start("On Data Change Instance");
                Debug.WriteLine("New Val:" + newValue);

                param.Set(newValue);
                //Debug.WriteLine("Param found has val:" + param);
                tx.Commit();
            }

        }

        public string GetName() {
            return "External Event Example";
        }
    }
}
