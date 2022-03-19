using System;
using System.Collections.Generic;
using System.IO;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Newtonsoft.Json.Linq;


namespace BearGIS
{
    public class ReadGeoJson : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the PolygonJSON class.
        /// </summary>
        public ReadGeoJson()
          : base("GeoJson to GH", "GeoJson-R",
              "This component Reads GeoJson Files and imports them to Grasshopper with thier attributes",
              "BearGIS", "Import")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("filePath", "fp", "File Path for geojson file", GH_ParamAccess.item);
            pManager.AddBooleanParameter("readFile", "r", "set to true to read to file", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("geoJSON", "json", "this is the importated geojson dataset", GH_ParamAccess.item);
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

            JObject geoJsonData = null;
            if (readFile)
            {
                //System.IO.File.ReadAllText(filePath,)
                geoJsonData = JObject.Parse(File.ReadAllText(@filePath));
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(geoJsonData, Newtonsoft.Json.Formatting.Indented);
                DA.SetData(0, json);


                if (geoJsonData != null)
                {
                    List<string> featureFields = new List<string>();
                    JArray fieldfeatures = (JArray)geoJsonData["features"];
                    JObject fieldproperties = (JObject)fieldfeatures[0]["properties"];

                    foreach (var obj in fieldproperties)
                    {
                        string thisField = (string)obj.Key;
                        featureFields.Add(thisField);
                    }
                    DA.SetDataList(1, featureFields);
                }

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
                        if (thisAttribute == " " || thisAttribute == "" || thisAttribute == null)
                        {
                            thisAttribute = "nan";
                        }
                        GH_String thisGhAttribute = new GH_String(thisAttribute);
                        attributes.Append(thisGhAttribute, currentPath);
                    }
                    int pathIndex = 0;

                    //if feature["geometry"]["Paths"] ==> polyline
                    //if feature["geometry"]["rings"] ==> polygon
                    //if feature["geometry"]["x"] ==> points
                    //if (dict.ContainsKey(key)) { ... }
                    JToken geometry_type = (string)(JToken)feature["geometry"]["type"];

                    //feature["geometry"].TryGetValue("type", out geometry_type);
                    if ((string)geometry_type == "Point")
                    {
                        List<GH_Point> thisPathPoints = new List<GH_Point>();

                        Point3d thisPoint = new Point3d((double)(JToken)feature["geometry"]["coordinates"][0], (double)(JToken)feature["geometry"]["coordinates"][1], 0);

                        GH_Point thisGhPoint = new GH_Point(thisPoint);

                        thisPathPoints.Add(thisGhPoint);

                        GH_Path thisPath = new GH_Path(featureIndex, pathIndex);
                        featureGeometry.AppendRange(thisPathPoints, thisPath);
                        featureIndex++;
                    }//end point

                    if ((string)geometry_type == "MultiPoint")
                    {
                        foreach (JArray coord in (JArray)feature["geometry"]["coordinates"])
                        {
                            List<GH_Point> thisPathPoints = new List<GH_Point>();

                            Point3d thisPoint = new Point3d((double)(JToken)coord[0], (double)(JToken)coord[1], 0);

                            GH_Point thisGhPoint = new GH_Point(thisPoint);

                            thisPathPoints.Add(thisGhPoint);

                            GH_Path thisPath = new GH_Path(featureIndex, pathIndex);
                            featureGeometry.AppendRange(thisPathPoints, thisPath);
                            pathIndex++;
                        }
                        featureIndex++;
                        pathIndex = 0;
                    }//end Multi-point

                    else if ((string)geometry_type == "MultiLineString")
                    {
                        //for each line geometry for this feature
                        
                        for (int pIndex = 0; pIndex < ((Newtonsoft.Json.Linq.JArray)feature["geometry"]["coordinates"]).Count; pathIndex++)
                        {
                            List<GH_Point> thisPathPoints = new List<GH_Point>();
                            //for each coordiant in this line geometry
                            foreach (JArray coord in (JArray)feature["geometry"]["coordinates"][pIndex])
                            {
                                Point3d thisPoint = new Point3d((double)(JToken)coord[0], (double)(JToken)coord[1], 0);
                                GH_Point thisGhPoint = new GH_Point(thisPoint);
                                thisPathPoints.Add(thisGhPoint);
                            }
                            //add line geometry to personal path
                            GH_Path thisPath = new GH_Path(featureIndex, pIndex);
                            featureGeometry.AppendRange(thisPathPoints, thisPath);
                            pathIndex++;
                        }
                        featureIndex++;
                        pathIndex = 0;
                    } //end MultiLineString

                    else if ((string)geometry_type == "LineString")
                    {
                        foreach (JArray coord in (JArray)feature["geometry"]["coordinates"])
                        {
                            List<GH_Point> thisPathPoints = new List<GH_Point>();


                            Point3d thisPoint = new Point3d((double)(JToken)coord[0], (double)(JToken)coord[1], 0);
                            GH_Point thisGhPoint = new GH_Point(thisPoint);
                            thisPathPoints.Add(thisGhPoint);
                            GH_Path thisPath = new GH_Path(featureIndex, pathIndex);
                            featureGeometry.AppendRange(thisPathPoints, thisPath);
                            pathIndex++;
                        }

                        featureIndex++;
                    }//end polyline


                    else if ((string)geometry_type == "Polygon")
                    {
                        foreach (JArray pathsArray in (JArray)feature["geometry"]["coordinates"])
                        {
                            List<GH_Point> thisPathPoints = new List<GH_Point>();
                            foreach (JArray path in pathsArray)
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
                    }//end polygon

                    else if ((string)geometry_type == "MultiPolygon")
                    {
                        //for each line geometry for this feature

                        for (int pIndex = 0; pIndex < ((Newtonsoft.Json.Linq.JArray)feature["geometry"]["coordinates"]).Count; pathIndex++)
                        {
                            List<GH_Point> thisPathPoints = new List<GH_Point>();
                            //for each coordiant in this line geometry
                            foreach (JArray coord in (JArray)feature["geometry"]["coordinates"][pIndex])
                            {
                                Point3d thisPoint = new Point3d((double)(JToken)coord[0], (double)(JToken)coord[1], 0);
                                GH_Point thisGhPoint = new GH_Point(thisPoint);
                                thisPathPoints.Add(thisGhPoint);
                            }
                            //add line geometry to personal path
                            GH_Path thisPath = new GH_Path(featureIndex, pIndex);
                            featureGeometry.AppendRange(thisPathPoints, thisPath);
                            pathIndex++;
                        }
                        featureIndex++;
                        pathIndex = 0;
                    } //end MultiLineString

                }
                DA.SetDataTree(2, attributes);
                DA.SetDataTree(3, featureGeometry);

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
                // return Resources.IconForThisComponent;
                return BearGIS.Properties.Resources.BearGISIconSet_18;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("ee29e737-870d-4424-9244-6dbcc6366f87"); }
        }
    }
}