using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using GH_IO;
using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using Rhino.Geometry;

using DotSpatial.Projections;

namespace BearGIS
{
    public class LongLatPt : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public LongLatPt()
          : base("LongLatPt", "LL-XY",
              "This component provides the xy location of a given Lat Long coordinates and .Proj file of source coordinates",
              "BearGIS", "projection")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("Longatude", "Long", "Longatude of desired xy point", GH_ParamAccess.item);
            pManager.AddNumberParameter("Latatude", "Lat", "Latatude of desired xy point", GH_ParamAccess.item);
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
            ProjectionInfo targetProjection = new ProjectionInfo();
            targetProjection.ParseEsriString(cur_proj);

            //ending projection
            ProjectionInfo sourceProjection = KnownCoordinateSystems.Geographic.World.WGS1984;

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
            get { return new Guid("7a7e0dea-6b8f-4918-bc67-2dd09328909d"); }
        }
    }
}