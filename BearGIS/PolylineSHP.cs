using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Reflection;
using System.Collections;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using GH_IO;
using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.Collections;

using DotSpatial;
using DotSpatial.Projections;
using DotSpatial.Positioning;
using DotSpatial.Controls;
using DotSpatial.Controls.Docking;
using DotSpatial.Controls.Header;
using DotSpatial.Data;
using DotSpatial.Symbology;
using DotSpatial.Topology;
using System.Data;

namespace BearGIS
{
    public class PolylineSHP : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public PolylineSHP()
          : base("PolylineSHP", "Polyline-SHP-w",
              "write Polyline SHP files.",
              "BearGIS", "ExportSHP")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            // You can often supply default values when creating parameters.
            pManager.AddCurveParameter("polylineTree", "plTree", "Polylines organized in a tree", GH_ParamAccess.tree);
            pManager.AddTextParameter("fields", "f", "list of Fields for each geometry. This should not be a datatree but a simple list", GH_ParamAccess.list);
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
            GH_Structure<GH_Curve> inputPolylineTree = new GH_Structure<GH_Curve>();

            bool writeFile = false;
            string filePath = "";
            string prj = null;
            // access the input parameter by index.
            if (!DA.GetData(5, ref writeFile)) return;
            if (!DA.GetData(4, ref filePath)) return;
            if (!DA.GetDataTree(0, out inputPolylineTree)) return;
            if (!DA.GetDataList(1, fields)) return;
            if (!DA.GetDataTree(2, out attributes)) return;
            if (!DA.GetData(3, ref prj)) return;


            //create new feature set to add data to
            //FeatureSet fs = new FeatureSet(FeatureType.Polygon);
            //FeatureSet fs = new FeatureSet(FeatureType.Point);
            //FeatureSet fs = new FeatureSet(FeatureType.MultiPoint);
            FeatureSet fs = new FeatureSet(FeatureType.Line);

            if (prj != null )
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
                int fieldindex = 0;
                foreach (string field in fields)
                {



                    //<<<dubble chack if this is properly declaring type>>>\\
                    GH_Path thisFieldPath = attributes.Paths[0];
                    var typeItem = attributes.get_Branch(thisFieldPath)[fieldindex];//.ToString();
                   // double outdouble;
                   // int outint;

                   
                    //Try new things area
                    Dictionary<string, Type> typeMap = new Dictionary<string, Type> { { "gh_int32", typeof(short) }, { "gh_int64", typeof(long) }, { "gh_double", typeof(double) }, { "gh_decimal", typeof(float) }, { "gh_date", typeof(DateTime) }, { "gh_guid", typeof(string) }, { "gh_string", typeof(string) } };
                    var fieldType = Enum.GetName(typeof(GH_IO.Types.GH_Types), typeItem);
                    var shpType = typeMap[fieldType];
                    fs.DataTable.Columns.Add(new DataColumn(field, shpType));
                    //end try new things
                    /*
                   //if (double.TryParse(typeItem, out outdouble))
                   if (typeItem is Grasshopper.Kernel.Types.GH_Number)
                   {
                       fs.DataTable.Columns.Add(new DataColumn(field, typeof(double)));
                   }
                   //else if(int.TryParse(typeItem, out outint))
                   else if (typeItem is Grasshopper.Kernel.Types.GH_Integer)
                   {
                       fs.DataTable.Columns.Add(new DataColumn(field, typeof(int)));
                   }
                   else
                   {
                       fs.DataTable.Columns.Add(new DataColumn(field, typeof(string)));
                   }
                   */

                    fieldindex += 1;
                }
         

                // for every branch  (ie feature)
                foreach (GH_Path path in inputPolylineTree.Paths)
                {
                    //set branch
                    IList branch = inputPolylineTree.get_Branch(path);
                    List<DotSpatial.Topology.LineString> theseLines = new List<DotSpatial.Topology.LineString>();

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

                        //create lineString Geometry from coordinates
                        DotSpatial.Topology.LineString thisLine = new DotSpatial.Topology.LineString(vertices);
                        // add linestring to linestring list
                        theseLines.Add(thisLine);
                    }//end curve itteration

                    //Convert Coordinates to dot spatial point or multipoint geometry
                    //DotSpatial.Topology.Point geom = new DotSpatial.Topology.Point(vertices);
                    //DotSpatial.Topology.MultiPoint geom = new DotSpatial.Topology.MultiPoint(vertices);

                    //convert list of line strings into single multilinestring feature
                    MultiLineString geom = new MultiLineString(theseLines);

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
                        //converting all fields to (((Proper Type...?)))
                        // 1) check data row type
                        if (fs.DataTable.Columns[fields[thisIndex]].DataType == typeof(double)){
                            feature.DataRow[fields[thisIndex]] = Convert.ToDouble(thisAttribute.ToString());
                        }
                        if (fs.DataTable.Columns[fields[thisIndex]].DataType == typeof(int))
                        {
                            feature.DataRow[fields[thisIndex]] = Convert.ToInt32(thisAttribute.ToString());
                        }
                        else
                        {
                            feature.DataRow[fields[thisIndex]] = thisAttribute.ToString();
                        }
                        // 2) convert data to that type system.iconvertable?
                        // add attribute
                        
                        //feature.DataRow[fields[thisIndex]] = thisAttribute.ToString(); //currently everything is a string....
                        //<<<!!!!!!!!!! dubble chack if this is properly converting to the type declared above !!!!!!!!!!>>>\\
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
                // You can add image files to your project resources and access them like this:
                return BearGIS.Properties.Resources.BearGISIconSet_15;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("6e360e79-a67c-483d-845d-5c5c08dc2c7d"); }
        }
    }
}