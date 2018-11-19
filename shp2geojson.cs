using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

using GH_IO;
using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;


using System.Reflection;
using Gdal = OSGeo.GDAL.Gdal;
using Ogr = OSGeo.OGR.Ogr;

namespace BearGIS
{
    public class shp2geojson : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public shp2geojson()
          : base("SHP-to-GeoJson", "shp2geo",
              "Convert SHP to GEOJSON with gdal",
              "BearGIS", "Conversion")
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
            pManager.AddTextParameter("geoJSON", "json", "this is the importated geojson dataset", GH_ParamAccess.item);
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

            string geoJsonFile = filePath.Substring(0, filePath.Length - 3)+"json";
            ogr2ogrcmdln(geoJsonFile, filePath);
        }


        public void ogr2ogrcmdln(string geoJsonFilePath, string shpFilePath)
        {
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            //> ogr2ogr -f "file_format" destination_data source_data
            //> ogr2ogr -f "ESRI Shapefile" destination_data.shp "source-data.json"
            //> ogr2ogr - f "GeoJSON" destination_data.geojson source_data.shp
            //process.StartInfo.Arguments = @"ogr2ogr-f GeoJSON  Thegeojson.geojson theshp.shp";
            process.StartInfo.Arguments = @"ogr2ogr-f GeoJSON" + geoJsonFilePath + shpFilePath;
            process.Start();
            process.WaitForExit();
            process.Close();
        }

        public struct TwoVars
        {
            public double Var1;
            public double Var2;
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
            get { return new Guid("924b1969-2757-4e6b-b6e6-790202de51a2"); }
        }
    }
}