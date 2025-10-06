using System;
using System.Collections;
using System.Collections.Generic;

using GH_IO;
using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.Collections;

using DotSpatial.Projections;
using DotSpatial.Data;
using System.Data;
using NetTopologySuite.Geometries; // NTS geometries are used in DotSpatial 3/4
using System.Globalization;

namespace BearGIS
{
    public class PointSHP : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the PolygonJSON class.
        /// </summary>
        public PointSHP()
          : base("PointSHP", "Pt SHP-w",
              "write Point SHP files.",
              "BearGIS", "ExportSHP")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            // You can often supply default values when creating parameters.
            pManager.AddPointParameter("pointTree", "ptTree", "Points organized in a tree (one branch = one feature)", GH_ParamAccess.tree);
            pManager.AddTextParameter("fields", "f", "Field specs: name or name;System.Type (Supported types: System.String, System.Int32, System.Int64, System.Double, System.Single, System.Decimal, System.Boolean, System.DateTime)", GH_ParamAccess.list);
            pManager.AddGenericParameter("attributes", "attr", "Attributes datatree (branches align with pointTree; item order aligns with fields)", GH_ParamAccess.tree);
            pManager.AddTextParameter(".prj File Path", "prj", "Projection .prj file path (optional)", GH_ParamAccess.item);
            pManager.AddTextParameter("filePath", "fp", "Output shapefile path (.shp)", GH_ParamAccess.item);
            pManager.AddBooleanParameter("writeFile", "w", "Set true to write file", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Message", "msg", "Export status / warnings", GH_ParamAccess.item);
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
            string prj = null;
            if (!DA.GetData(5, ref writeFile)) return;
            if (!DA.GetData(4, ref filePath)) return;
            // access the input parameter by index. 
            if (!DA.GetDataTree(0, out inputPointTree)) return;
            if (!DA.GetDataList(1, fields)) return;
            if (!DA.GetDataTree(2, out attributes)) return;
            if (!DA.GetData(3, ref prj)) return;


            string message = string.Empty;

            //create new feature set to add data to
            FeatureSet fs = new FeatureSet(FeatureType.MultiPoint);


            if (!string.IsNullOrWhiteSpace(prj))
            {
                try
                {
                    //load projection file
                    string cur_proj = System.IO.File.ReadAllText(@prj);

                    ///create Projection system
                    ProjectionInfo targetProjection = new ProjectionInfo();
                    targetProjection.ParseEsriString(cur_proj);
                    fs.Projection = targetProjection;
                }
                catch (Exception ex)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Failed to read .prj: " + ex.Message);
                }
            }

            if (writeFile)
            {
                try
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

                    int featureCount = 0;
                    // for every branch  (ie feature)
                    foreach (GH_Path path in inputPointTree.Paths)
                    {
                        //set branch
                        IList branch = inputPointTree.get_Branch(path);
                        if (branch == null || branch.Count == 0) continue;

                        // create a feature  geometry 
                        var coords = new List<NetTopologySuite.Geometries.Coordinate>();

                        //add all pt coordinates to the vertices list
                        foreach (GH_Point pt in branch)
                        {
                            Point3d rhinoPoint = new Point3d();
                            GH_Convert.ToPoint3d(pt, ref rhinoPoint, 0);
                            coords.Add(new NetTopologySuite.Geometries.Coordinate(rhinoPoint.X, rhinoPoint.Y));
                        }
                        // Convert Coordinates to NTS MultiPoint geometry
                        var geom = NetTopologySuite.Geometries.GeometryFactory.Default.CreateMultiPointFromCoords(coords.ToArray());

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
                            Type targetType = typeof(string);
                            //try to get target type from field specification
                            if (fields[thisIndex].Split(';').Length == 2)
                                targetType = Type.GetType(fields[thisIndex].Split(';')[1]);
                            //convert and assign value
                            feature.DataRow[thisField] = CoerceAttribute(thisAttribute, targetType, thisField, path);
                            thisIndex++;
                        }
                        //finish attribute additions
                        feature.DataRow.EndEdit();
                        featureCount++;
                    }//end of itterating through branches of pt tree
                    fs.SaveAs(filePath, true);
                    message = $"Wrote {featureCount} point features.";
                }
                catch (Exception ex)
                {
                    message = "Export failed: " + ex.Message;
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, message);
                }
            }
            else
            {
                message = "Set writeFile to true to export.";
            }

            DA.SetData(0, message);
        }

        private object CoerceAttribute(object raw, Type targetType, string fieldName, GH_Path featurePath)
        {
            if (raw == null) return DBNull.Value;
            string s = raw.ToString();
            if (string.IsNullOrWhiteSpace(s)) return DBNull.Value;
            try
            {
                if (targetType == typeof(string)) return s;
                if (targetType == typeof(int) || targetType == typeof(Int32)) return int.Parse(s, CultureInfo.InvariantCulture);
                if (targetType == typeof(long) || targetType == typeof(Int64)) return long.Parse(s, CultureInfo.InvariantCulture);
                if (targetType == typeof(double) || targetType == typeof(Double)) return double.Parse(s, CultureInfo.InvariantCulture);
                if (targetType == typeof(float) || targetType == typeof(Single)) return float.Parse(s, CultureInfo.InvariantCulture);
                if (targetType == typeof(decimal)) return decimal.Parse(s, CultureInfo.InvariantCulture);
                if (targetType == typeof(bool) || targetType == typeof(Boolean)) return bool.Parse(s);
                if (targetType == typeof(DateTime)) return DateTime.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                // Fallback
                return Convert.ChangeType(s, targetType, CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Field '{fieldName}' (feature {featurePath}) value '{s}' failed to convert to {targetType.Name}: {ex.Message}. Set to NULL.");
                return DBNull.Value;
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
                return BearGIS.Properties.Resources.BearGISIconSet_14;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("8e3ef45e-7068-42f6-b35f-61143ae3c6a2"); }
        }
    }
}