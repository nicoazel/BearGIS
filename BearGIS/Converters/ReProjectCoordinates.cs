using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using DotSpatial.Projections;

namespace BearGIS
{
    public class ReProjectCoordinates : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the PolygonJSON class.
        /// </summary>
        public ReProjectCoordinates()
          : base("ReProject", "ReProj",
              "This component converts coordinates from one projection system to another",
              "BearGIS", "projection")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("X_Longatude", "XLong", "Longatude of desired xy point", GH_ParamAccess.item);
            pManager.AddNumberParameter("Y_Latatude", "YLat", "Latatude of desired xy point", GH_ParamAccess.item);
            pManager.AddTextParameter("SourcePrj", "SPrj", "File Path of the source projection '.Prj' File representing the source coordinate system", GH_ParamAccess.item);
            pManager.AddTextParameter("targetPrj", "TPprj", "File Path of the target projection '.Prj' File representing the target output coordinate system", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("point", "pt", "Point Coordinate of given longatude latatude", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double lng = 0;
            double lat = 0;
            string sourceFilePath = "";
            string targetFilePath = "";

            if (!DA.GetData(0, ref lng)) return;
            if (!DA.GetData(1, ref lat)) return;
            if (!DA.GetData(2, ref sourceFilePath)) return;
            if (!DA.GetData(3, ref targetFilePath)) return;

            //load projection file
            string cur_proj = System.IO.File.ReadAllText(@sourceFilePath);

            ///Starting projection
            ProjectionInfo sourceProjection = new ProjectionInfo();
            sourceProjection.ParseEsriString(cur_proj);


            //load projection file
            string tar_proj = System.IO.File.ReadAllText(@targetFilePath);
            //ending projection
            ProjectionInfo targetProjection = new ProjectionInfo();
            targetProjection.ParseEsriString(tar_proj);

            int len = 1;
            double[] z = new double[] { 0 };
            double[] xy = new double[] { lng, lat };
            DotSpatial.Projections.Reproject.ReprojectPoints(xy, z, sourceProjection, targetProjection, 0, len);

            Point3d rPt = new Point3d(xy[0], xy[1], z[0]);
            GH_Point pt = new GH_Point(rPt);

            DA.SetData(0, pt);
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                return BearGIS.Properties.Resources.BearGISIconSet_17;
                // return Resources.IconForThisComponent;
                //return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("08C97FAF-71C3-4683-9F40-8289112E0D2D"); }
        }
    }
}