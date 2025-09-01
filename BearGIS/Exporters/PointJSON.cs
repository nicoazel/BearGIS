using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
//using Grasshopper.Kernel.Data.GH_Structure;

using System.Collections;
using BearGIS.Converters;

namespace BearGIS
{
    public class PointJSON : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the PolygonJSON class.
        /// </summary>
        public PointJSON()
          : base("PointGeoJSON", "PtJson",
              "Converts Points with attrbutes to GeoJson files",
              "BearGIS", "GeoJson")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("PointTree", "PtTree", "points organized in a tree per feature. Note:GeoJSON uses a geographic coordinate reference system, World Geodetic System 1984, and units of decimal degrees (convert coords to appropriate lat lon first!)", GH_ParamAccess.tree);
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
            GH_Structure<GH_Point> inputPointTree = new GH_Structure<GH_Point>();

            bool writeFile = false;
            string filePath = "";
            int epsg = -1;
            if (!DA.GetData(4, ref writeFile)) return;
            if (!DA.GetData(3, ref filePath)) return;
            // access the input parameter by index. 
            if (!DA.GetDataTree(0, out inputPointTree)) return;
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
            foreach (GH_Path path in inputPointTree.Paths)
            {
                //set branch
                IList branch = inputPointTree.get_Branch(path);

                //create geometry key
                Dictionary<string, object> thisGeometry = new Dictionary<string, object>();

                //create coordinate list and add to path list
                List<double> thisCoordinate = new List<double>();

                //for multipart create list to hold coordinate lists
                //List<object> thisPointList = new List<object>();

                // for every point  in branch
                foreach (GH_Point thisGhPoint in branch)
                {
                    Rhino.Geometry.Point3d thisRhinoPoint = new Point3d();
                    GH_Convert.ToPoint3d(thisGhPoint, ref thisRhinoPoint, 0);

                   // add coordinates
                    thisCoordinate.Add(thisRhinoPoint.X);
                    thisCoordinate.Add(thisRhinoPoint.Y);

                }//end of each point in branch

                thisGeometry.Add("type", "point");
                thisGeometry.Add("coordinates", thisCoordinate);

                //creat attriabtrues key
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
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                //You can add image files to your project resources and access them like this:
                return BearGIS.Properties.Resources.BearGISIconSet_14;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("7a9835e7-cf81-497a-bb7b-75058ed0c5df"); }
        }
    }
}
