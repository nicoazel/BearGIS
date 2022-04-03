namespace BearGIS
{
    public class MyCategoryIcon : Grasshopper.Kernel.GH_AssemblyPriority
    {
        public override Grasshopper.Kernel.GH_LoadingInstruction PriorityLoad()
        {
            Grasshopper.Instances.ComponentServer.AddCategoryIcon("BearGIS", BearGIS.Properties.Resources.BearGISIconSet_19);
            Grasshopper.Instances.ComponentServer.AddCategoryShortName("BearGIS", "bear");
            Grasshopper.Instances.ComponentServer.AddCategorySymbolName("BearGIS", 'B');

            return Grasshopper.Kernel.GH_LoadingInstruction.Proceed;
        }
    }
}
