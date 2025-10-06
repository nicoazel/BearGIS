using System;
using System.Drawing;
using System.Reflection;
using Grasshopper.Kernel;

namespace BearGIS
{
    public class BearGISInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "BearGIS";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return BearGIS.Properties.Resources.BearGISIconSet_20;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "Bear GIS is a plugin for reading and writing gis data";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("63c5bde4-8fb1-4204-85c0-1875fd9482f5");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "Nicolas Azel";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "nicoazel.github.io/BearGIS/";
            }
        }

        public override string Version
        {
            get
            {
                return "2.1.0.0";
            }
        }
    }
}
