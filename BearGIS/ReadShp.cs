using System;
using System.Collections.Generic;
using System.IO;

using GH_IO;
using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Harlow;


using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Bson;


namespace BearGIS
{
    public class ReadShp  : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public ReadShp()
          : base("ReadShp", "SHP-R",
              "Reads SHP files. this actualy converts .shp to geojson, then operates as the other readers",
              "BearGIS", "Import")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("filePath", "fp", "File Path for shp file", GH_ParamAccess.item);
            pManager.AddBooleanParameter("readFile", "r", "set to true to read to file", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
           // pManager.AddTextParameter("EsriJSON", "json", "this is the importated geojson dataset", GH_ParamAccess.item);
            pManager.AddTextParameter("fields", "flds", "these are teh data fields assosiated with each feature", GH_ParamAccess.list);
            pManager.AddTextParameter("attributes", "attr", "these are the attribute values for each field. reach feature is represented in one branch", GH_ParamAccess.tree);
            pManager.AddGeometryParameter("features", "ftr", "these are the improted features", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //retrive inputs
            string filePath = "";
            if (!DA.GetData(0, ref filePath)) return;

            bool readFile = false;
            if (!DA.GetData(1, ref readFile)) return;

            //JObject geoJsonData = null;
            JArray jsonObj = null;
            if (readFile)
            {

                //file = "filePath
                Harlow.ShapeFileReader harlowShpReader = new Harlow.ShapeFileReader(filePath);
                harlowShpReader.LoadFile();
                string shpJsonString = harlowShpReader.FeaturesAsJson();
                //string shpjsonString = harlowShpReader.FeatureAsJson()
               

                //System.IO.File.ReadAllText(filePath,)
                //geoJsonData = JObject.Parse(shpJsonString);
                jsonObj = JArray.Parse(shpJsonString);
                JObject geoJsonData = new JObject();
                geoJsonData.Add("features", jsonObj);

                //var json = Newtonsoft.Json.JsonConvert.SerializeObject(geoJsonData, Newtonsoft.Json.Formatting.Indented);
                //DA.SetData(0, json);


                //read features
                JArray features = (JArray)geoJsonData["features"];
                GH_Structure<GH_String> attributes = new GH_Structure<GH_String>();
                GH_Structure<GH_Point> featureGeometry = new GH_Structure<GH_Point>();
                int featureIndex = 0;
                foreach (JObject feature in features)
                {
                    GH_Path currentPath = new GH_Path(featureIndex);
                    foreach (var attr in (JObject)feature["properties"])
                    {
                        JToken attributeToken = attr.Value;
                        string thisAttribute = (string)attributeToken;
                        GH_String thisGhAttribute = new GH_String(thisAttribute);
                        attributes.Append(thisGhAttribute, currentPath);
                    }
                    int pathIndex = 0;


                    foreach (JArray pathsArray in (JArray)feature["coordinates"])
                    {
                        List<GH_Point> thisPathPoints = new List<GH_Point>();
                        foreach (var path in pathsArray)
                        {
                            Point3d thisPoint = new Point3d((double)path[0], (double)path[1], 0);
                            GH_Point thisGhPoint = new GH_Point(thisPoint);
                            thisPathPoints.Add(thisGhPoint);
                        }
                        GH_Path thisPath = new GH_Path(featureIndex, pathIndex);
                        featureGeometry.AppendRange(thisPathPoints, thisPath);
                        pathIndex++;
                    }

                    featureIndex++;
                }//end polyline
                DA.SetDataTree(1, attributes);
                DA.SetDataTree(2, featureGeometry);

                ///
                ///set attributes
  
                List<string> featureFields = new List<string>();
                JToken fieldObjs =  geoJsonData["features"][0]["properties"];
                foreach (JProperty prop in fieldObjs)
                {
                    string thisField = (string)prop.Name;
                    featureFields.Add(thisField);
                }
                DA.SetDataList(0, featureFields);
            


            }//end if read file

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
                return BearGIS.Properties.Resources.BearGISIconSet_20;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("bd3593f7-c801-4e45-99a8-854e98adcb6c"); }
        }
    }
}