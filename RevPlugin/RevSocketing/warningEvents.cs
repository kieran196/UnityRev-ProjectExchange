using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace RevSocketing
{
    public class warningEvents : IFailuresPreprocessor {
        public enum ERROR_TYPES {NOTCUTTING};
        public static updatePosEvent updatePosHandler;

        public FailureProcessingResult PreprocessFailures(FailuresAccessor a) {
            IList<FailureMessageAccessor> failures = a.GetFailureMessages();
            foreach (FailureMessageAccessor f in failures) {
                FailureDefinitionId id = f.GetFailureDefinitionId();
                if (id == BuiltInFailures.CutFailures.InstanceNotCuttingAnything)
                {
                    Debug.WriteLine("INSTANCE NOT CUTTING ANYTHING..");
                    updatePosHandler.invalidCoordinateException(ERROR_TYPES.NOTCUTTING);
                    return FailureProcessingResult.Continue;
                }
            }
            return FailureProcessingResult.Continue;
        }

    }
}
