using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BearGIS
{
    //https://www.grasshopper3d.com/forum/topics/addcategoryicon
    public class BearCategoryIcon : Grasshopper.Kernel.GH_AssemblyPriority
    {
        public override Grasshopper.Kernel.GH_LoadingInstruction PriorityLoad()
        {
            Grasshopper.Instances.ComponentServer.AddCategoryIcon("BearGIS", somebitmap);
            Grasshopper.Instances.ComponentServer.AddCategoryShortName("BearGIS", "Bear");
            Grasshopper.Instances.ComponentServer.AddCategorySymbolName("BearGIS", 'B');

            return Grasshopper.Kernel.GH_LoadingInstruction.Proceed;
        }
    }
}
