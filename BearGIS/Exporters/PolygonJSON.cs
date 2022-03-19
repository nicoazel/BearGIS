using Grasshopper.Kernel;
using System;
using System.Collections;
using System.Collections.Generic;
using BearGIS.Converters;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace BearGIS.Exporters
{
    public class PolygonJSON : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the PolygonJSON class.
        /// </summary>
        public PolygonJSON()
            : base("PolygonGeoJSON", "PolygonJSON",
                "Convert grasshopper polygons to geojson polygons. ",
                "BearGIS", "GeoJson")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            // Use the pManager object to register your input parameters.
            // You can often supply default values when creating parameters.
            pManager.AddCurveParameter("polygonTree", "plTree", "Polygons organized in a tree. Note:GeoJSON uses a geographic coordinate reference system, World Geodetic System 1984, and units of decimal degrees (convert coords to appropriate lat lon first!)", GH_ParamAccess.tree);
            pManager.AddTextParameter("fields", "f", "list of Fields for each geometry. This should not be a datatree but a simple list", GH_ParamAccess.list);
            pManager.AddGenericParameter("attributes", "attr", "attributes for each geometry. this should be a dataTree matching the linePoints input, and fields indicies", GH_ParamAccess.tree);
            pManager.AddTextParameter("filePath", "fp", "File Path for new geojson file, sugestion: use '.json'", GH_ParamAccess.item);
            pManager.AddBooleanParameter("writeFile", "w", "set to true to write to file", GH_ParamAccess.item);

        }
        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            // Use the pManager object to register your output parameters.
            // You can use the HideParameter() method as a quick way: pManager.HideParameter(0);
            pManager.AddTextParameter("geojson", "json", "readable geoJson with indents, for human legablity and review of results", GH_ParamAccess.item);
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
            GH_Structure<GH_Curve> inputCurveTree = new GH_Structure<GH_Curve>();

            bool writeFile = false;
            string filePath = "";
            int epsg = -1;
            if (!DA.GetData(4, ref writeFile)) return;
            if (!DA.GetData(3, ref filePath)) return;
            // access the input parameter by index. 
            if (!DA.GetDataTree(0, out inputCurveTree)) return;
            if (!DA.GetDataList(1, fields)) return;
            if (!DA.GetDataTree(2, out attributes)) return;

            //Create geojson dic
            Dictionary<string, Object> geoDict = new Dictionary<string, Object>();

            //Basic JSON headder info
            geoDict.Add("type", "FeatureCollection");

            // Start of  feature constructio.
            // create feature list
            List<Object> featuresList = new List<Object>();

            // for every branch  (ie feature)
            foreach (GH_Path path in inputCurveTree.Paths)
            {
                //set branch
                IList branch = inputCurveTree.get_Branch(path);

                //create geometry key
                Dictionary<string, object> thisGeometry = new Dictionary<string, object>();
                thisGeometry.Add("type", "MultiPolygon");

                List<object> thisPaths = new List<object>();
                // for every curve  in branch
                foreach (GH_Curve thisGhCurve in branch)
                {
                    List<List<double>> thisPath = new List<List<double>>();

                    //convert to rhino curve
                    Curve rhinoCurve = null;
                    GH_Convert.ToCurve(thisGhCurve, ref rhinoCurve, 0);
                    //curve to nurbes
                    NurbsCurve thisNurbsCurve = rhinoCurve.ToNurbsCurve();
                    //Get list of control points
                    Rhino.Geometry.Collections.NurbsCurvePointList theseControlPoints = thisNurbsCurve.Points;
                    //for each control point
                    foreach (ControlPoint thisPoint in theseControlPoints)
                    {
                        //create coordinate list and add to path list
                        List<double> thisCoordinate = new List<double>();
                        thisCoordinate.Add(thisPoint.Location.X);
                        thisCoordinate.Add(thisPoint.Location.Y);
                        thisPath.Add(thisCoordinate); //[x,y]
                    }//end each control point

                    //add repeat first point at end
                    thisPath.Add(thisPath[0]);

                    //add this path or paths
                    thisPaths.Add(thisPath);
                }//end of each curve in branch
                //add all paths to geometry key for this feature
                thisGeometry.Add("coordinates", new List<List<object>>(){thisPaths});

                Dictionary<string, object> thisAttribtues = Converter.BuildJsonAttributes(attributes.get_Branch(path), fields);
                //wrap up into feature and add to feature list;
                Dictionary<string, Object> thisFeature = new Dictionary<string, Object>();

                thisFeature.Add("type", "Feature");
                thisFeature.Add("properties", thisAttribtues);
                thisFeature.Add("geometry", thisGeometry);
                featuresList.Add(thisFeature);
            }//end of each branch path
            // end of  feature construction

            // finaly add features list to master object
            geoDict.Add("features", featuresList);


            //Produces convert dictionary to json text
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(geoDict, Newtonsoft.Json.Formatting.Indented);

            // Finally assign the retults to the output parameter.
            //DA.SetData(0, spiral);

            //write string to file
            if (writeFile == true)
            {
                //@"D:\path.txt"
                System.IO.File.WriteAllText(@filePath, json);
            }
            DA.SetData(0, json);

        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return BearGIS.Properties.Resources.BearGISIconSet_16;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("d9070c62-55d8-4810-86d5-40d91fefbc6f"); }
        }
    }
}