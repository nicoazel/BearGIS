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


using DotSpatial;

namespace BearGIS
{
    public class PrjESPG : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public PrjESPG()
          : base("DotPrjToESPG", "prjESPG",
              "Reads a .prj file and returns the estimated EPSG code",
              "BearGIS", "projection")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("prjFile", "prj", "File Path for .prj file", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("espg", "epsg", "the espg code for this dataset", GH_ParamAccess.item);
            pManager.AddTextParameter("authority", "auth", "the espg code for this dataset", GH_ParamAccess.item);
            pManager.AddTextParameter("note", "note", "the espg code for this dataset", GH_ParamAccess.item);
            
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string espgCode;
            string filePath = "";
            if (!DA.GetData(0, ref filePath)) return;
            string prjString = File.ReadAllText(@filePath);
            var soursePrj = new DotSpatial.Projections.ProjectionInfo();
            soursePrj = DotSpatial.Projections.ProjectionInfo.FromEsriString(prjString);

            //var sourseDatum = new DotSpatial.Positioning.Datum();
            //sourseDatum.ParseEsriString(prjString);

            espgCode = soursePrj.EpsgCode.ToString();
            //espgCode = soursePrj.AuthorityCode.ToString;
            //DotSpatial.Projections.AuthorityCodes.AuthorityCodeHandler.

            //var sourseDatum = DotSpatial.Positioning.Datum.FromName(soursePrj.ToString());
            //espgCode = sourseDatum.EpsgNumber.ToString();


            DA.SetData(0, espgCode);
            DA.SetData(1, prjString);
            DA.SetData(2, soursePrj);
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
            get { return new Guid("7d8cf276-54a0-4de1-8f70-1bfd675b08f0"); }
        }
    }
}