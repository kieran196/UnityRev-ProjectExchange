using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevSocketing {
    public class onParamChangedEvent : IUpdater
    {
        public void Execute(UpdaterData data) {
            Debug.WriteLine("Parameter has changed - trigger called.");
            throw new NotImplementedException();
        }

        public string GetAdditionalInformation() {
            throw new NotImplementedException();
        }

        public ChangePriority GetChangePriority() {
            throw new NotImplementedException();
        }

        public UpdaterId GetUpdaterId() {
            throw new NotImplementedException();
        }

        public string GetUpdaterName() {
            throw new NotImplementedException();
        }
    }
}
