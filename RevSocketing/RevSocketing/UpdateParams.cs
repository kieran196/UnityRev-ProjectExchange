using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RevSocketing {

    public class UpdateParams : IExternalEventHandler {

        private UIApplication App {
            get; set;
        }

        private Document Doc {
            get; set;
        }

        public UpdateParams(ExternalCommandData commandData) {
            App = commandData.Application;
            Doc = App.ActiveUIDocument.Document;
        }

        public void Execute(UIApplication app) {
            TaskDialog.Show("External Event", "Click Close to close.");
        }

        public string GetName() {
            return "External Event Example";
        }
    }
}
