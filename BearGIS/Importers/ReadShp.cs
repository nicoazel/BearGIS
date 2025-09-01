using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using DotSpatial.Data;
using System.Data;


namespace BearGIS
{
    public class ReadShp  : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ReadShp class.
        /// </summary>
        public ReadShp()
          : base("ReadShp", "SHP-R",
              "Reads SHP files using DotSpatial for improved performance. Handles multi-part geometry with sub-branches.",
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
            string filePath = "";
            if (!DA.GetData(0, ref filePath)) return;

            bool readFile = false;
            if (!DA.GetData(1, ref readFile)) return;

            if (readFile)
            {
                GH_Structure<GH_Point> featureGeometry = new GH_Structure<GH_Point>();
                GH_Structure<GH_String> attributes = new GH_Structure<GH_String>();
                var openSHP = Shapefile.OpenFile(@filePath);
                
                List<string> featureFields = new List<string>();
                foreach(DataColumn column in openSHP.GetColumns())
                {
                    featureFields.Add(column.ColumnName);
                }

                int featureIndex = 0;
                foreach (var currentFeature in openSHP.Features)
                {
                    GH_Path currentPath = new GH_Path(featureIndex);
                    var currentAttributes = currentFeature.DataRow;
                    foreach(var attr in currentAttributes.ItemArray)
                    {
                        string thisAttribute = Convert.ToString(attr);
                        if (thisAttribute == " " || thisAttribute == "" || thisAttribute == null)
                        {
                            thisAttribute = "nan";
                        }
                        GH_String thisGhAttribute = new GH_String(thisAttribute);
                        attributes.Append(thisGhAttribute, currentPath);
                    }

                    for (int pathIndex = 0; pathIndex <= currentFeature.NumGeometries - 1; pathIndex++)
                    {
                        List<GH_Point> thisPathPoints = new List<GH_Point>();
                        foreach (DotSpatial.Topology.Coordinate coord in currentFeature.GetBasicGeometryN(pathIndex).Coordinates)
                        {
                            Point3d thisPoint = new Point3d((double)coord.X, (double)coord.Y, 0);
                            GH_Point thisGhPoint = new GH_Point(thisPoint);
                            thisPathPoints.Add(thisGhPoint);
                        }
                        GH_Path thisPath = new GH_Path(featureIndex, pathIndex);
                        featureGeometry.AppendRange(thisPathPoints, thisPath);
                    }
                    featureIndex++;
                }
                DA.SetDataList(0, featureFields);
                DA.SetDataTree(1, attributes);
                DA.SetDataTree(2, featureGeometry);
            }
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
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
