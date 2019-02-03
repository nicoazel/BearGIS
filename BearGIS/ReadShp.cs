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
            pManager.AddTextParameter("filePath", "fp", "File Path for new geojson file", GH_ParamAccess.item);
            pManager.AddBooleanParameter("readFile", "r", "set to true to write to file", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("EsriJSON", "json", "this is the importated geojson dataset", GH_ParamAccess.item);
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
            JArray geoJsonData = null;
            if (readFile)
            {

                //file = "filePath
                Harlow.ShapeFileReader harlowShpReader = new Harlow.ShapeFileReader(filePath);
                harlowShpReader.LoadFile();
                string shpJsonString = harlowShpReader.FeaturesAsJson();
                //string shpjsonString = harlowShpReader.FeatureAsJson()
               

                //System.IO.File.ReadAllText(filePath,)
                //geoJsonData = JObject.Parse(shpJsonString);
                geoJsonData = JArray.Parse(shpJsonString);
                JObject jsonObj = new JObject();
                jsonObj.Add("features", geoJsonData);
                //jsonObj.Add("shapetype",  );
                //type check:
                //var itemcheck = jsonObj["features"][0]["coordinates"];


                //Newtonsoft.Json.JsonConvert.ser
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObj, Newtonsoft.Json.Formatting.Indented);
                //var json = Newtonsoft.Json.JsonConvert.SerializeObject(geoJsonData, Newtonsoft.Json.Formatting.Indented);

                DA.SetData(0, json);
                //DA.SetData(0, shpJsonString);


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
                return null;
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