using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Reflection;
using System.Collections;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using GH_IO;
using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.Collections;

using DotSpatial;
using DotSpatial.Projections;
using DotSpatial.Positioning;
using DotSpatial.Controls;
using DotSpatial.Controls.Docking;
using DotSpatial.Controls.Header;
using DotSpatial.Data;
using DotSpatial.Symbology;
using DotSpatial.Topology;
using System.Data;

namespace BearGIS
{
    public class WriteShp : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public WriteShp()
          : base("WriteShp", "SHP-W",
              "write SHP files.",
              "BearGIS", "Export")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            // You can often supply default values when creating parameters.
            pManager.AddCurveParameter("polygonTree", "plTree", "Polylines organized in a tree", GH_ParamAccess.tree);
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
            pManager.AddTextParameter("Message", "msg", "comments of attempt to export shp", GH_ParamAccess.item);

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
            if (!DA.GetData(5, ref writeFile)) return;
            if (!DA.GetData(4, ref filePath)) return;
            // access the input parameter by index. 
            if (!DA.GetDataTree(0, out inputCurveTree)) return;
            if (!DA.GetDataList(1, fields)) return;
            if (!DA.GetDataTree(2, out attributes)) return;
            if (!DA.GetData(3, ref epsg)) return;



            FeatureSet fs = new FeatureSet(FeatureType.Polygon);
            // Add Some Columns
            //fs.DataTable.Columns.Add(new DataColumn("ID", typeof(int)));
            //fs.DataTable.Columns.Add(new DataColumn("Text", typeof(string)));
            foreach (string field in fields)
            {
                fs.DataTable.Columns.Add(new DataColumn(field, typeof(string)));
            }
            // for every branch  (ie feature)
            foreach (GH_Path path in inputCurveTree.Paths)
            {
                //set branch
                IList branch = inputCurveTree.get_Branch(path);

                // for every curve  in branch
                //foreach (GH_Curve thisGhCurve in branch)
                //{
                // create a feature  geometry 
                List<Coordinate> vertices = new List<Coordinate>();

                //convert to rhino curve
                Curve rhinoCurve = null;
                GH_Convert.ToCurve(branch[0], ref rhinoCurve, 0);
                //GH_Convert.ToCurve(thisGhCurve, ref rhinoCurve, 0);
                //curve to nurbes
                NurbsCurve thisNurbsCurve = rhinoCurve.ToNurbsCurve();
                //Get list of control points
                Rhino.Geometry.Collections.NurbsCurvePointList theseControlPoints = thisNurbsCurve.Points;
                //for each control point
                foreach (ControlPoint thisPoint in theseControlPoints)
                {
                    vertices.Add(new Coordinate(thisPoint.Location.X, thisPoint.Location.Y));
                }//end each control point
                Polygon geom = new Polygon(vertices);
                //fs.AddFeature(geom);
                //}
                IFeature feature = fs.AddFeature(geom);
                feature.DataRow.BeginEdit();

                IList<string> featrueAttributes = attributes.get_Branch(path) as IList<string>;
                int thisIndex = 0;
                //foreach(var thisAttribute in featrueAttributes)
                foreach (var thisAttribute in attributes.get_Branch(path))
                {
                    feature.DataRow[fields[thisIndex]] = thisAttribute.ToString();
                    thisIndex++;
                    //}
                }
                //foreach (var thisAttribute in featrueAttributes.Select((Value, Index) => new { Value, Index })){
                //    feature.DataRow[fields[thisAttribute.Index]] = thisAttribute.Value.ToString();
                //}
                feature.DataRow.EndEdit();
            }
            fs.SaveAs(filePath, true);
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
            get { return new Guid("5e1f45aa-9c62-4cf3-939a-36578f70731f"); }
        }
    }
}