using System;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;

using Rhino.Geometry;

using DotSpatial.Projections;

namespace BearGIS
{
    public class PtLongLat : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the PolygonJSON class.
        /// </summary>
        public PtLongLat()
          : base("PtLongLat", "XY-LL",
              "This component provides the lat lon location of a given X Y coordinates given the X Y .Proj file of source coordinates. Lat Lon are  in KnownCoordinateSystems.Geographic.World.WGS1984",
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
            pManager.AddTextParameter("PrjfilePath", "prj", "File Path of the.Proj File representing the source coordinate system", GH_ParamAccess.item);
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
            string fp = "";

            if (!DA.GetData(0, ref lng)) return;
            if (!DA.GetData(1, ref lat)) return;
            if (!DA.GetData(2, ref fp)) return;

            //load projection file
            string cur_proj = System.IO.File.ReadAllText(@fp);

            ///Starting projection
            ProjectionInfo sourceProjection = new ProjectionInfo();
            sourceProjection.ParseEsriString(cur_proj);

            //ending projection
            ProjectionInfo  targetProjection = KnownCoordinateSystems.Geographic.World.WGS1984;

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
            get { return new Guid("54D5D70B-A01A-4557-8285-6F94550D0890"); }
        }
    }
}