using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Diagnostics;

namespace RevSocketing {
    public class ExternalEventAppTest : IExternalApplication {

        public static ExternalEventApp thisApp = null;

        public Result OnShutdown(UIControlledApplication application) {
            Debug.WriteLine("External Event App has Shut Down..");
            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application) {
            Debug.WriteLine("External Event App has started..");
            //thisApp = this;
            return Result.Succeeded;
        }

        public void ShowForm(UIApplication uiapp) {
            ExternalEventTest handler = new ExternalEventTest();
            ExternalEvent exEvent = ExternalEvent.Create(handler);
        }

    }
}
