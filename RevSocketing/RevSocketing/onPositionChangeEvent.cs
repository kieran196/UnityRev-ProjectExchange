﻿using System;
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
        public static double[] idRotations;
        public static Class1 classInstance;
        public static bool lastCallFromUnityClient = false;
        private XYZ oldCoords;
        private double oldRot;
        public void Execute(UIApplication app) {
            using (Transaction tx = new Transaction(classInstance.uidoc)) {
                tx.Start("On Position Change Instance");
                Element e = onIdPositionChanged(classInstance.ids);
                Element e1 = onIdRotationChanged(classInstance.ids);
                if (e != null) {
                    sendData(e, "POS");
                }
                if (e1 != null) {
                    sendData(e1, "ROT");
                }
                //tx.Commit();
            }
        }

        public void sendData(Element e, String type) {
            Autodesk.Revit.DB.LocationPoint locationPoint = e.Location as Autodesk.Revit.DB.LocationPoint;
            if (type == "POS") {
                XYZ offset = locationPoint.Point - oldCoords;
                classInstance.sendGMDData(e.Id.ToString(), type, offset.ToString());
            } if (type == "ROT") {
                double rot = locationPoint.Rotation - oldRot;
                classInstance.sendGMDData(e.Id.ToString(), type, rot.ToString());
            }
        }

        public Element onIdRotationChanged(List<ElementId> ids) {
            if (ids != null && idCoordinates != null) {
                for (int i = 0; i < ids.Count; i++) {
                    Element e = classInstance.uidoc.GetElement(ids[i]);
                    if (e != null && e.Location != null)
                    {
                        Autodesk.Revit.DB.LocationPoint locationPoint = e.Location as Autodesk.Revit.DB.LocationPoint;
                        if (locationPoint != null) {
                            if (idRotations[i] != locationPoint.Rotation) {
                                Debug.WriteLine("Old Rot:" + idRotations[i] + " | New Rot:" + locationPoint.Rotation);
                                oldRot = idRotations[i];
                                idRotations[i] = locationPoint.Rotation;
                                if (lastCallFromUnityClient) {
                                    lastCallFromUnityClient = false;
                                    return null;
                                }
                                return e;
                            }
                        }
                    }
                }
            }
            return null;
        }

        // Idea to get starting pos - store ids into an instance array, then create a getElementIndex method which returns the index of the array.
        // This index can then be used to pull the starting coordTransform of an element from the idCoordinates array.
        public Element onIdPositionChanged(List<ElementId> ids) {
            if (ids != null && idCoordinates != null) {
                for (int i = 0; i < ids.Count; i++) {
                    Element e = classInstance.uidoc.GetElement(ids[i]);
                    if (e != null && e.Location != null) {
                        Autodesk.Revit.DB.LocationPoint locationPoint = e.Location as Autodesk.Revit.DB.LocationPoint;
                        if (locationPoint != null) {
                            if (!vectorIsEqual(idCoordinates[i], locationPoint.Point)) {
                                //if (!idCoordinates[i].Equals(positionPoint.Point)) {
                                Debug.WriteLine("Old Pos:" + idCoordinates[i] + " | New Pos:" + locationPoint.Point);
                                oldCoords = idCoordinates[i];
                                idCoordinates[i] = locationPoint.Point;
                                if (lastCallFromUnityClient ||idRotations[i] != locationPoint.Rotation) {
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
