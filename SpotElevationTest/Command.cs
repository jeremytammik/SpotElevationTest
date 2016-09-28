using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace SpotElevationTest
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]

    public class Command : IExternalCommand
    {
        private Document _doc;
        private UIDocument _uiDoc;
        private Autodesk.Revit.Creation.Document _docCreator;
        private List<FamilySymbolWrapper> _spotElevationTagList = new List<FamilySymbolWrapper>();
        private Autodesk.Revit.Creation.Application Create;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Get the application and document from external command data as well as a doc creator.
            _doc = commandData.Application.ActiveUIDocument.Document;
            _uiDoc = commandData.Application.ActiveUIDocument;
            _docCreator = _uiDoc.Document.Create;
            
            //TODO - I was getting a list of spot elevation types from the model, but not needed for this test now
            ////Get the spot elevations within the file
            //AnnotationTagListBuilder atlb = new AnnotationTagListBuilder();
            //_spotElevationTagList = atlb.GetSpotElevationsFromFile(commandData);

            //now select the model element to add the spot elevation
            Reference r = commandData.Application.ActiveUIDocument.Selection.PickObject(ObjectType.Element,
                "Please select a Structural Framing Element");

            //get the element from the document
            Element elem = _doc.GetElement(r.ElementId);
            if (elem != null)
            {
                //get the family instance
                FamilyInstance familyInstance = elem as FamilyInstance;

                if (familyInstance != null)
                {
                    //create the spot elevation
                    CreateSpotElevation(familyInstance);
                }
            }

            return Result.Succeeded;
        }

        private void CreateSpotElevation(FamilyInstance familyInstance)
        {
            //get the location curve of the family instance
            LocationCurve location = familyInstance.Location as LocationCurve;

            //instantiate a curve
            Curve curve = null;

            //check for location null
            if (location != null)
            {
                //get the curve from the location
                curve = location.Curve;
            }

            else if (familyInstance.Category.Name.Equals("Structural Columns"))
                //structural columns do not have location curve, but analytical model does
            {
                curve = familyInstance.GetAnalyticalModel().GetCurve();
            }

            if (curve != null)
            {
                //get the end points and mid point from the curve
                XYZ startPoint = curve.GetEndPoint(0);
                XYZ endPoint = curve.GetEndPoint(1);
                XYZ midPoint = curve.Evaluate(0.5, true);

                //set the default point on the element to tag
                XYZ pointOnElementToTag = startPoint;

                TaskDialog td = new TaskDialog("Select Point to Tag");
                td.MainInstruction = "Select the point on the curve to tag";
                td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Start Point");
                td.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "End Point");
                td.AddCommandLink(TaskDialogCommandLinkId.CommandLink3, "Mid Point");
                td.CommonButtons = TaskDialogCommonButtons.Cancel;
                td.DefaultButton = TaskDialogResult.Cancel;
                TaskDialogResult result = td.Show();

                if (result == TaskDialogResult.CommandLink1)
                {
                    pointOnElementToTag = startPoint;
                }
                else if (result == TaskDialogResult.CommandLink2)
                {
                    pointOnElementToTag = endPoint;
                }
                else if (result == TaskDialogResult.CommandLink3)
                {
                    pointOnElementToTag = midPoint;
                }
                else
                {
                    TaskDialog.Show("Canceled", "The action was canceled by the user");
                    return;
                }
                
                //instantiate a transaction to add the individual tag
                using (Transaction trans = new Transaction(_doc, "Add Spot Elevation"))
                {
                    trans.Start();

                    try
                    {
                        //get the top reference for the spot elevation
                        Reference spotReference = FindTopMostReference(familyInstance);

                        //set the bend and endpoints for the new spot elevation
                        XYZ seBendPoint = pointOnElementToTag.Add(new XYZ(0, 1, 4));
                        XYZ seEndPoint = pointOnElementToTag.Add(new XYZ(0, 2, 4));

                        //add the spot elevation
                        SpotDimension sd = _docCreator.NewSpotElevation(_uiDoc.ActiveView, spotReference, pointOnElementToTag, seBendPoint, seEndPoint, pointOnElementToTag, true);

                        trans.Commit();

                        //TaskDialog.Show("Success", "The Spot elevation was added successfully");
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("Error", "The following error occurred: " + ex.Message);
                        trans.RollBack();
                    }
                }
            }
        }

        private Reference FindTopMostReference(Element e)
        {
            Reference ret = null;

            Options opt = _doc.Application.Create.NewGeometryOptions();
            opt.ComputeReferences = true;

            GeometryElement geo = e.get_Geometry(opt);

            foreach (GeometryObject obj in geo)
            {
                GeometryInstance inst = obj as GeometryInstance;

                if (inst != null)
                {
                    geo = inst.GetSymbolGeometry();
                    break;
                }
            }

            Solid solid = geo.OfType<Solid>().First<Solid>(sol => null != sol);

            double z = double.MinValue;

            foreach (Face f in solid.Faces)
            {
                BoundingBoxUV b = f.GetBoundingBox();
                UV p = b.Min;
                UV q = b.Max;
                UV midparam = p + 0.5*(q - p);
                XYZ midpoint = f.Evaluate(midparam);
                XYZ normal = f.ComputeNormal(midparam);

                if (PointsUpwards(normal))
                {
                    if (midpoint.Z > z)
                    {
                        z = midpoint.Z;
                        ret = f.Reference;
                    }
                }
            }
            return ret;
        }

        private bool PointsUpwards(XYZ v)
        {
            const double minimumSlope = 0.3;

            double horizontalLength = v.X * v.X + v.Y * v.Y;
            double verticalLength = v.Z * v.Z;

            return 0 < v.Z && minimumSlope < verticalLength / horizontalLength;
        }
    }
}
