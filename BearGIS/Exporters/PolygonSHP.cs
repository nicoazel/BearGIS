using System;
using System.Collections;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using DotSpatial.Projections;
using DotSpatial.Data;
using DotSpatial.Topology;
using System.Data;

namespace BearGIS
{
    /// <summary>
    /// Exports Grasshopper Geometry as a Polygon .shp
    /// </summary>
    public class PolygonSHP : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the PolygonJSON class.
        /// </summary>
        public PolygonSHP()
          : base("PolygonSHP", "Plygn-SHP-w",
              "write Polygon SHP files.",
              "BearGIS", "ExportSHP")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            // You can often supply default values when creating parameters.
            pManager.AddCurveParameter("polygonTree", "pgTree", "Polygon organized in a tree", GH_ParamAccess.tree);
            pManager.AddTextParameter("fields", "f", "list of Fields for each geometry. This should not be a datatree but a simple list. To specify type use .net built in types eg System.Double, System.String, System.Boolean", GH_ParamAccess.list);
            pManager.AddGenericParameter("attributes", "attr", "attributes for each geometry. this should be a dataTree matching the linePoints input, and fields indicies", GH_ParamAccess.tree);
            pManager.AddTextParameter(".prj File Path", "prj", "The prj file for setting the spatial projection system", GH_ParamAccess.item);
            pManager.AddTextParameter("filePath", "fp", "File Path for new geojson file, sugestion: use '.json'", GH_ParamAccess.item);
            pManager.AddBooleanParameter("writeFile", "w", "set to true to write to file", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Message", "msg", "comments of attempt to export shp", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // First, we need to retrieve all data from the input parameters.
            List<string> fields = new List<string>();
            GH_Structure<IGH_Goo> attributes = new GH_Structure<IGH_Goo>();
            GH_Structure<GH_Curve> inputPolygonTree = new GH_Structure<GH_Curve>();

            bool writeFile = false;
            string filePath = "";
            string prj = null;
            // access the input parameter by index. 
            if (!DA.GetData(5, ref writeFile)) return;
            if (!DA.GetData(4, ref filePath)) return;
            if (!DA.GetDataTree(0, out inputPolygonTree)) return;
            if (!DA.GetDataList(1, fields)) return;
            if (!DA.GetDataTree(2, out attributes)) return;
            if (!DA.GetData(3, ref prj)) return;
            
            


            //create new feature set to add data to
            FeatureSet fs = new FeatureSet(FeatureType.Polygon);
            //FeatureSet fs = new FeatureSet(FeatureType.Point);
            //FeatureSet fs = new FeatureSet(FeatureType.MultiPoint);
            //FeatureSet fs = new FeatureSet(FeatureType.Line);

            if (prj != null)
            {
                //load projection file
                string cur_proj = System.IO.File.ReadAllText(@prj);

                ///create Projection system
                ProjectionInfo targetProjection = new ProjectionInfo();
                targetProjection.ParseEsriString(cur_proj);
                fs.Projection = targetProjection;
            }

            if (writeFile)
            {
                // Add fields to the feature sets attribute table 
                foreach (string field in fields)
                {
                    //check for type
                    string[] splitField = field.Split(';');
                    //if field type provided, specify it
                    if (splitField.Length == 2)
                    {
                        fs.DataTable.Columns.Add(new DataColumn(splitField[0], Type.GetType(splitField[1])));
                    }
                    else
                    {
                        //otherwise jsut use a string
                        fs.DataTable.Columns.Add(new DataColumn(field, typeof(string)));
                    }
                    
                    
                }
                // for every branch  (ie feature)
                foreach (GH_Path path in inputPolygonTree.Paths)
                {
                    //set branch
                    IList branch = inputPolygonTree.get_Branch(path);
                    //List<DotSpatial.Topology.LineString> theseLines = new List<DotSpatial.Topology.LineString>();
                    List<LinearRing> theseCurves = new List<LinearRing>();

                    foreach (GH_Curve curve in branch)
                    {
                        // create vertex list for this curve 
                        List<Coordinate> vertices = new List<Coordinate>();

                        //convert to rhino curve
                        Curve rhinoCurve = null;
                        GH_Convert.ToCurve(curve, ref rhinoCurve, 0);
                        //curve to nurbes
                        NurbsCurve thisNurbsCurve = rhinoCurve.ToNurbsCurve();
                        //Get list of control points
                        Rhino.Geometry.Collections.NurbsCurvePointList theseControlPoints = thisNurbsCurve.Points;
                        //for each control point
                        foreach (ControlPoint thisPoint in theseControlPoints)
                        {
                            vertices.Add(new Coordinate(thisPoint.Location.X, thisPoint.Location.Y));
                        }//end each control point

                        //create linering Geometry from coordinates
                        LinearRing thisCurve = new LinearRing(vertices);

                        // add curve to curve list
                        theseCurves.Add(thisCurve);
                    }//end curve itteration

                    //Convert Coordinates to dot spatial point or multipoint geometry
                    //DotSpatial.Topology.Point geom = new DotSpatial.Topology.Point(vertices);
                    //DotSpatial.Topology.MultiPoint geom = new DotSpatial.Topology.MultiPoint(vertices);
                    DotSpatial.Topology.ILinearRing outerCurve = theseCurves[0];
                    var innerCurves = theseCurves.GetRange(1, theseCurves.Count - 1).ToArray();

                    //convert list of line strings into single multilinestring feature
                    //MultiLineString geom = new MultiLineString(theseLines);
                    Polygon geom = new Polygon(outerCurve, innerCurves);

                    //convert geom to a feature
                    IFeature feature = fs.AddFeature(geom);

                    //begin editing to add feature attributes
                    feature.DataRow.BeginEdit();
                    //get this features attributes by its path
                    IList<string> featrueAttributes = attributes.get_Branch(path) as IList<string>;
                    int thisIndex = 0;
                    //add each attribute for the pt's path
                    foreach (var thisAttribute in attributes.get_Branch(path))
                    {
                        string thisField = fields[thisIndex].Split(';')[0];
                        feature.DataRow[thisField] = thisAttribute.ToString(); //currently everything is a string....                                                  
                        thisIndex++;
                    }
                    //finish attribute additions
                    feature.DataRow.EndEdit();
                }//end of itterating through branches of pt tree
                fs.SaveAs(filePath, true);
            }
            

        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                return BearGIS.Properties.Resources.BearGISIconSet_16;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("5f77a249-93a6-472f-b835-295a537fc640"); }
        }
    }
}