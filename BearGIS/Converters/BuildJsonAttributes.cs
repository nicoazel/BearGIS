using System;
using System.Collections;
using System.Collections.Generic;

namespace BearGIS.Converters
{
    public static class Converter
    {
        /// <summary>
        /// IList attributesBranch = attributes.get_Branch(path);
        /// 
        /// </summary>
        /// <param name="attributesBranch"></param>
        /// <returns></returns>
        public static Dictionary<string, object> BuildJsonAttributes(IList attributesBranch, List<string> fields)
        {
            //creat attriabtrues key
            Dictionary<string, object> thisAttribtues = new Dictionary<string, object>();

            foreach (var item in attributesBranch)
            {
                string thisField = fields[attributesBranch.IndexOf(item)]; //fields are string

                // ---------------------this is in order add the riight type?

                if (item is Grasshopper.Kernel.Types.GH_Integer)
                {
                    string thisAttribute = item.ToString();
                    thisAttribtues.Add(thisField, thisAttribute);
                }

                else if (item is Grasshopper.Kernel.Types.GH_Number)
                {
                    string thisAttribute = item.ToString();
                    thisAttribtues.Add(thisField, thisAttribute);
                }

                else if (item is Grasshopper.Kernel.Types.GH_String)
                {
                    string thisAttribute = item.ToString();
                    thisAttribtues.Add(thisField, thisAttribute);
                }

                else if (item is Grasshopper.Kernel.Types.GH_Time)
                {
                    string thisAttribute = item.ToString();
                    thisAttribtues.Add(thisField, thisAttribute);
                }

                else
                {
                    string thisAttribute = "wasent a type"; item.ToString();
                    thisAttribtues.Add(thisField, thisAttribute);
                }

                // ------------------------how to add value of igh_goo verbatum....
                //thisAttribtues.Add(thisField, thisAttribute);
            }

            return thisAttribtues;
            //return attributes;
        }
    }

}
