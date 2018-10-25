using Rhino;
using Rhino.Geometry;
using Rhino.DocObjects;
using Rhino.Collections;

using GH_IO;
using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
//using Grasshopper.Kernel.Data.GH_Structure;

using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Reflection;
using System.Collections;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Runtime.InteropServices;


namespace BearGIS
{
    public class MyComponent1 : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public MyComponent1()
          : base("MyComponent1", "Nickname",
              "Description",
              "Category", "Subcategory")
        {
        }


        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            // Use the pManager object to register your input parameters.
            pManager.AddCurveParameter("polyline", "pl", "points that compose the polyline organized in a tree", GH_ParamAccess.list);
            pManager.AddCurveParameter("polylineTree", "plTree", "points that compose the polyline organized in a tree", GH_ParamAccess.tree);
            pManager.AddGenericParameter("attributes", "a", "attributes for each geometry. this should be a dataTree matching the linePoints input, and fields indicies", GH_ParamAccess.tree);

        }
    

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            // Use the pManager object to register your output parameters.
            pManager.AddTextParameter("points", "pt", "controlpoints", GH_ParamAccess.list);

            pManager.AddTextParameter("err", "err", "errors", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            // retrieve all data from the input parameters.
            List<Curve> shapes = new List<Curve>();

            // Then we need to access the input parameters individually. 
            if (!DA.GetDataList(0, shapes)) return;


            //create error data tree
            GH_Structure<GH_String> err = new GH_Structure<GH_String>();
            //create desitnation point list
            List<Point3d> ptsOut = new List<Point3d>();

            // for every curve  in list of shapes
            foreach (var curve in shapes)
            {

                //Error Testing..
                GH_String errTarget = new GH_String();
                GH_Convert.ToGHString_Primary( curve.ToString(), ref errTarget);
                err.Append(errTarget);

                //curve to nurbes
                NurbsCurve  thisNurbsCurve = curve.ToNurbsCurve();

                //curve 
                Rhino.Geometry.Collections.NurbsCurvePointList theseControlPoints = thisNurbsCurve.Points; //thisNurbsCurve.Points;

                foreach (ControlPoint thisPoint in theseControlPoints)
                {
                    Point3d pt = new Point3d(thisPoint.Location);
                    ptsOut.Add(pt);
                }


            }//end of each curve in shapes



            // export restults 
            DA.SetDataList(0, ptsOut);
            DA.SetDataTree(1, err);
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
            get { return new Guid("de0daf85-d741-4180-966e-f98e1c4a6c7b"); }
        }
    }
}