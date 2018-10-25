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
    public class MyComponent2 : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent2 class.
        /// </summary>
        public MyComponent2()
          : base("MyComponent2", "Nickname",
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
            //pManager.AddCurveParameter("polyline", "pl", "points that compose the polyline organized in a tree", GH_ParamAccess.list);
            pManager.AddCurveParameter("polylineTree", "plTree", "points that compose the polyline organized in a tree", GH_ParamAccess.tree);
          

        }


        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            // Use the pManager object to register your output parameters.
            pManager.AddTextParameter("FlatPointsPath", "fpt", "controlpoints", GH_ParamAccess.tree);

            //pManager.AddTextParameter("err", "err", "errors", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // input Tree.
            GH_Structure<GH_Curve> inputCurveTree = new GH_Structure<GH_Curve>();

            // access the input parameter by index. 
            if (!DA.GetDataTree(0, out inputCurveTree)) return;

            //create desitnation list
            List<string> pointPathOut = new List<string>();

            // for every branch  (ie feature)
            foreach (GH_Path path in inputCurveTree.Paths)
            {       
               IList branch = inputCurveTree.get_Branch(path);
                // for every curve  in branch
                foreach (GH_Curve thisGhCurve in branch)
                {
                    Curve rhinoCurve = null;
                    GH_Convert.ToCurve(thisGhCurve, ref rhinoCurve, 0);
                    
                    //curve to nurbes
                    NurbsCurve thisNurbsCurve = rhinoCurve.ToNurbsCurve();

                    //Get list of control points
                    Rhino.Geometry.Collections.NurbsCurvePointList theseControlPoints = thisNurbsCurve.Points; 
                    
                    //for each control point
                    foreach (ControlPoint thisPoint in theseControlPoints)
                    {
                        pointPathOut.Add( "{ "+thisPoint.Location.ToString()+" }=> " + path.ToString() );
                    }//end each control point

                }//end of each curve in branch

            }//end of each path

            // set restults 
            DA.SetDataList(0, pointPathOut);
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
            get { return new Guid("d5486d3a-a7a8-4d10-8bc8-4abced93fb04"); }
        }
    }
}