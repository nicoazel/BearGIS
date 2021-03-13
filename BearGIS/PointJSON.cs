using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.Collections;

using GH_IO;
using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
//using Grasshopper.Kernel.Data.GH_Structure;

using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Reflection;
using System.Collections;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;


using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Bson;
namespace BearGIS
{
    public class PointJSON : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public PointJSON()
          : base("PointGeojson", "PtJson",
              "Converts Points with attrbutes to GeoJson files",
              "BearGIS", "GeoJson")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("PointTree", "PtTree", "points that compose the polyline organized in a tree", GH_ParamAccess.tree);
            pManager.AddTextParameter("fields", "f", "list of Fields for each geometry. This should not be a datatree but a simple list", GH_ParamAccess.list);
            pManager.AddGenericParameter("attributes", "attr", "attributes for each geometry. this should be a dataTree matching the linePoints input, and fields indicies", GH_ParamAccess.tree);
            pManager.AddIntegerParameter("epsgCode", "epsg", "The epsg code for the spatial projection system", GH_ParamAccess.item);
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
            if (!DA.GetData(5, ref writeFile)) return;
            if (!DA.GetData(4, ref filePath)) return;
            // access the input parameter by index. 
            if (!DA.GetDataTree(0, out inputPointTree)) return;
            if (!DA.GetDataList(1, fields)) return;
            if (!DA.GetDataTree(2, out attributes)) return;
            if (!DA.GetData(3, ref epsg)) return;
            // We should now validate the data and warn the user if invalid data is supplied.
            //if (radius0 < 0.0){
            //    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Inner radius must be bigger than or equal to zero");
            //    return;}

            //Create geojson dic
            Dictionary<string, Object> geoDict = new Dictionary<string, Object>();

            // We're set to create the geojson now. To keep the size of the SolveInstance() method small, 
            // The actual functionality will be in a different method:
            //Curve spiral = CreateSpiral(plane, radius0, radius1, turns);


            //Basic esriJSON headder info
            geoDict.Add("type", "FeatureCollection");



            // package the above in a function ^^^
            //features: [ 
            //    {
            //        geometry:{Paths:[ [-[x,y],[x1,y1]-], [-[x,y],[x1,y1]-]  ]
            //        attributes:{ field:value, field1:value1}
            //    },
            //    {
            //        geometry:{Paths:[ [-[x,y],[x1,y1]-], [-[x,y],[x1,y1]-]  ]
            //        attributes:{ field:value, field1:value1}
            //    }
            //]

            // Start of  feature construction----------------------------------------------------
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
                thisGeometry.Add("coordinates", thisCoordinate.ToString());


                //creat attriabtrues key
                Dictionary<string, object> thisAttribtues = new Dictionary<string, object>();
                IList attributesBranch = attributes.get_Branch(path);

                //foreach (var item in attributesBranch.Select((Value, Index) => new { Value, Index })) //this needs list not IList
                foreach (var item in attributesBranch)
                //for (int i = 0; i < attributesBranch.Count; i++)
                {
                    string thisField = fields[attributesBranch.IndexOf(item)]; //fields are string

                    // ---------------------this is in order add the riight type?

                    if (item is Grasshopper.Kernel.Types.GH_Integer)
                    {
                        string thisAttribute = item.ToString();
                        Convert.ChangeType(thisAttribute, typeof(int));
                        thisAttribtues.Add(thisField, thisAttribute);
                    }

                    else if (item is Grasshopper.Kernel.Types.GH_Number) // else if (typeItem is long //|| typeItem is ulong //|| typeItem is float //|| typeItem is double //|| typeItem is decimal)
                    {
                        string thisAttribute = item.ToString();
                        Convert.ChangeType(thisAttribute, typeof(double));
                        thisAttribtues.Add(thisField, thisAttribute);
                    }

                    else if (item is Grasshopper.Kernel.Types.GH_String)
                    {
                        string thisAttribute = item.ToString();
                        thisAttribtues.Add(thisField, thisAttribute);
                    }

                    else if (item is Grasshopper.Kernel.Types.GH_Time)
                    {
                        string thisAttribute = item.ToString();
                        Convert.ChangeType(thisAttribute, typeof(DateTime));
                        thisAttribtues.Add(thisField, thisAttribute);
                    }

                    else
                    {
                        string thisAttribute = "wasent a type"; item.ToString();
                        thisAttribtues.Add(thisField, thisAttribute);
                    }

                    // ------------------------how to add value of igh_goo verbatum....
                    //thisAttribtues.Add(thisField, thisAttribute);
                }


                //wrap up into feature and add to feature list;
                Dictionary<string, Object> thisFeature = new Dictionary<string, Object>();

                thisFeature.Add("geometry", thisGeometry);
                thisFeature.Add("properties", thisAttribtues);
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