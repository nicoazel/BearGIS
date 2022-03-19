using Rhino.Geometry;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
//using Grasshopper.Kernel.Data.GH_Structure;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using BearGIS.Converters;


namespace BearGIS
{
    public class PointESRI : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the PolygonJSON class.
        /// </summary>
        public PointESRI()
          : base("PointESRI", "PtEsri",
              "Converts Points with attrbutes to ESRI Json files",
              "BearGIS", "EsriJson")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            // Use the pManager object to register your input parameters.
            // You can often supply default values when creating parameters.
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
            GH_Structure<GH_Point> inputPointTree = new GH_Structure< GH_Point >();

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
            if (attributes.PathCount != inputPointTree.PathCount)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "attribute branch count should be equal to geometry branch count");
                return;
            }


            //Create geojson dic
            Dictionary<string, Object> geoDict = new Dictionary<string, Object>();

            //Basic esriJSON headder info
            geoDict.Add("displayFieldName", " ");

            Dictionary<string, string> fieldAliasDic = new Dictionary<string, string>();

            foreach (string field in fields)
            {
                fieldAliasDic.Add(field, field);
            }
            geoDict.Add("fieldAliases", fieldAliasDic);

            geoDict.Add("geometryType", "esriGeometryPoint");
            Dictionary<string, int> sr = new Dictionary<string, int>() { { "wkid", epsg }, { "latestWkid", -1 } };
            geoDict.Add("spatialReference", sr);

            // package the below in a function
            List<Dictionary<string, string>> fieldsList = new List<Dictionary<string, string>>();

            foreach (var item in fields.Select((Value, Index) => new { Value, Index }))
            {
                Dictionary<string, string> fieldTypeDict = new Dictionary<string, string>();


                fieldTypeDict.Add("name", item.Value.ToString());

                var typeItem = attributes.get_Branch(attributes.Paths[0])[item.Index];

                if (typeItem is Grasshopper.Kernel.Types.GH_Integer)
                {
                    fieldTypeDict.Add("type", "esriFieldTypeInteger");
                }

                else if (typeItem is Grasshopper.Kernel.Types.GH_Number) // else if (typeItem is long //|| typeItem is ulong //|| typeItem is float //|| typeItem is double //|| typeItem is decimal)
                {
                    fieldTypeDict.Add("type", "esriFieldTypeDouble");
                }

                else if (typeItem is Grasshopper.Kernel.Types.GH_String)
                {
                    fieldTypeDict.Add("type", "esriFieldTypeString");
                }

                else if (typeItem is Grasshopper.Kernel.Types.GH_Time)
                {
                    fieldTypeDict.Add("type", "esriFieldTypeDate");
                }

                else
                {
                    fieldTypeDict.Add("type", "esriFieldTypeString");
                    fieldTypeDict.Add("GH_Type", typeItem.GetType().ToString());
                }
                if (item.Value.ToString().Length > 7)
                {
                    fieldTypeDict.Add("alias", item.Value.ToString().Substring(0, 7));
                }
                else
                {
                    fieldTypeDict.Add("alias", item.Value.ToString());
                }

                fieldsList.Add(fieldTypeDict);
            }//end for each fields
            geoDict.Add("fields", fieldsList);

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

                //for multipart create list to hold coordinate lists
                //List<object> thisPointList = new List<object>();
                
                // for every point  in branch
                foreach (GH_Point thisGhPoint in branch)
                {
                    Rhino.Geometry.Point3d thisRhinoPoint = new Point3d();
                    GH_Convert.ToPoint3d(thisGhPoint, ref thisRhinoPoint ,0);

                    //Non-multipoint
                    thisGeometry.Add("x", thisRhinoPoint.X);
                    thisGeometry.Add("y", thisRhinoPoint.Y);

                    //create coordinate list and add to path list
                    //List<double> thisCoordinate = new List<double>();

                    //add coordinates
                    //thisCoordinate.Add(thisRhinoPoint.X);
                    //thisCoordinate.Add(thisRhinoPoint.Y);

                    //add to feature list
                    //thisPointList.Add(thisCoordinate);

                }//end of each point in branch

                //thisGeometry.Add("Points", thisPointList);
                //above would be used for multipart


                Dictionary<string, object> thisAttribtues = Converter.BuildJsonAttributes(attributes.get_Branch(path), fields);

                //wrap up into feature and add to feature list;
                Dictionary<string, Object> thisFeature = new Dictionary<string, Object>();

                thisFeature.Add("geometry", thisGeometry);
                thisFeature.Add("attributes", thisAttribtues);
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
                return BearGIS.Properties.Resources.BearGISIconSet_14;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("0ce010ec-65a4-4898-bd61-e7212de2d311"); }
        }
    }
}