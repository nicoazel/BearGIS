protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
{
    // Use the pManager object to register your input parameters.
    // You can often supply default values when creating parameters.
    // All parameters must have the correct access type. If you want
    // to import lists or trees of values, modify the ParamAccess flag.
    //pManager.AddPointParameter("linePoints", "lP", "points that compose the polyline organized in a tree", GH_ParamAccess.tree);
    pManager.AddCurveParameter("polyline", "pl", "points that compose the polyline organized in a tree", GH_ParamAccess.tree);
    pManager.AddTextParameter("fields", "f", "list of Fields for each geometry. This should not be a datatree but a simple list", GH_ParamAccess.list);
    pManager.AddGenericParameter("attributes", "a", "attributes for each geometry. this should be a dataTree matching the linePoints input, and fields indicies", GH_ParamAccess.tree);
}

/// <summary>
/// Registers all the output parameters for this component.
/// </summary>
protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
{
    // Use the pManager object to register your output parameters.
    // Output parameters do not have default values, but they too must have the correct access type.
    // Sometimes you want to hide a specific parameter from the Rhino preview.
    // You can use the HideParameter() method as a quick way:
    //pManager.HideParameter(0);

    pManager.AddTextParameter("geoJSON", "gJSON", "compact geoJson discription of the geometry and data. this can be written to a json file with the WriteGeojson Component", GH_ParamAccess.item);
    pManager.AddTextParameter("readable", "rj", "readable geoJson with indents, for human legablity and review of results", GH_ParamAccess.item);
    //pManager.AddTextParameter("error", "err", "error messages added to this list", GH_ParamAccess.list);


}

/// <summary>
/// This is the method that actually does the work.
/// </summary>
/// <param name="DA">The DA object can be used to retrieve data from input parameters and
/// to store data in output parameters.</param>
protected override void SolveInstance(IGH_DataAccess DA)
{

    // First, we need to retrieve all data from the input parameters.
    // We'll start by declaring variables and assigning them starting values.
    //Plane plane = Plane.WorldXY;
    //int turns = 0;
    //GH_Structure<Polyline> shapes = new GH_Structure<Polyline>();
    GH_Structure<GH_Curve

        > shapes = new GH_Structure<GH_Curve>();
    List<string> fields = new List<string>();
    GH_Structure<IGH_Goo > attributes = new GH_Structure<IGH_Goo>();
    //GH_Structure<IGH_Goo> attributes = new GH_Structure<IGH_Goo>();

    // Then we need to access the input parameters individually.
    // When data cannot be extracted from a parameter, we should abort this method.
    if (!DA.GetDataTree(0, out shapes)) return;
    if (!DA.GetDataList(1, fields)) return;
    if (!DA.GetDataTree(2, out attributes)) return;

    // We should now validate the data and warn the user if invalid data is supplied.
                //if (radius0 < 0.0){
                //    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Inner radius must be bigger than or equal to zero");
                //    return;}


    //Create working variables, not outputs or inputs but middle men
    Dictionary<string, Object> geoDict = new Dictionary<string, Object>();
    //List<string> err = new List<string>();

    // We're set to create the spiral now. To keep the size of the SolveInstance() method small,
    // The actual functionality will be in a different method:
                //Curve spiral = CreateSpiral(plane, radius0, radius1, turns);


    //Basic esriJSON headder info
    // Dictionary<string, Object> geoDict = new Dictionary<string, Object>();
    geoDict.Add("displayFieldName", " ");

    Dictionary<string, string> fieldAliasDic = new Dictionary<string, string>();

    foreach (string field in fields)
    {
        fieldAliasDic.Add(field, field);
    }
    geoDict.Add("fieldAliases", fieldAliasDic);

    geoDict.Add("geometryType", "esriGeometryPolyline");
    Dictionary<string, int> sr = new Dictionary<string, int>() { { "wkid", 102729 }, { "latestWkid", 2272 } };
    geoDict.Add("spatialReference", sr);


    List<Dictionary<string, string>> fieldsList = new List<Dictionary<string, string>>();

    foreach (var item in fields.Select((Value, Index) => new { Value, Index }))
    {
        Dictionary<string, string> fieldTypeDict = new Dictionary<string, string>();


        fieldTypeDict.Add("name", item.Value.ToString());

        var typeItem = attributes.get_Branch(attributes.Paths[0])[item.Index];

        if (typeItem is Grasshopper.Kernel.Types.GH_Integer)
        {
            fieldTypeDict.Add("type", "esriFieldTypeInteger");
        }

        else if (typeItem is Grasshopper.Kernel.Types.GH_Number) // else if (typeItem is long //|| typeItem is ulong //|| typeItem is float //|| typeItem is double //|| typeItem is decimal)
        {
            fieldTypeDict.Add("type", "esriFieldTypeDouble");
        }

        else if (typeItem is Grasshopper.Kernel.Types.GH_String)
        {
            fieldTypeDict.Add("type", "esriFieldTypeString");
        }

        else if (typeItem is Grasshopper.Kernel.Types.GH_Time)
        {
            fieldTypeDict.Add("type", "esriFieldTypeDate");
        }

        else
        {
            fieldTypeDict.Add("type", "esriFieldTypeString");
            fieldTypeDict.Add("GH_Type", typeItem.GetType().ToString());
        }
        if (item.Value.ToString().Length > 7)
        {
            fieldTypeDict.Add("alias", item.Value.ToString().Substring(0, 7));
        }
        else
        {
            fieldTypeDict.Add("alias", item.Value.ToString());
        }

        fieldsList.Add(fieldTypeDict);
    }
    geoDict.Add("fields", fieldsList);




    //features: [
    //    {
    //        geometry:{Paths:[ [-[x,y],[x1,y1]-], [-[x,y],[x1,y1]-]  ]
    //        attributes:{ field:value, field1:value1}
    //    },
    //    {
    //        geometry:{Paths:[ [-[x,y],[x1,y1]-], [-[x,y],[x1,y1]-]  ]
    //        attributes:{ field:value, field1:value1}
    //    }
    //]


    // create feature list
    List<Object> featuresList = new List<Object>();

    // for every branch  (ie feature)
    foreach (var path in shapes.Paths) {

        var branch = shapes.get_Branch(path);//.Cast<Curve>();

        //create Feature object:    -> will be added to "features:[]"
        Dictionary<string, Object> feature = new Dictionary<string, Object>();


        ///////// start of geometry part\\\\\\\\\\\
        //create the geometry object
        Dictionary<string, Object> geometry = new Dictionary<string, Object>();

        //create the Geometry{Paths: List
        List<Object> allPaths = new List<Object>();

        // go through each curve in the branch
        foreach (GH_Curve shape in branch)
        {

            NurbsCurve curveshape;
            shape.CastTo<NurbsCurve>(out curveshape);
           // err.Add(curveshape.ToString());
            // create a list of coords
            List<Object> thisPath = new List<Object>();
            // go through each point on curve

            //foreach (Point3d point in polylineshape.ToList<Point3d>())
            foreach (ControlPoint point in curveshape.Points)
            {
                //craete list of coords
                List<double> xyCoord = new List<double>();
                xyCoord.Add(point.Location.X);
                xyCoord.Add(point.Location.Y);
                //add coords list to curve list
                thisPath.Add(xyCoord);
            }
            // add path coords list to all paths list
            allPaths.Add(thisPath);
        }
        //add coords to gemoetry object
        geometry.Add("Paths", allPaths);

        //add geometry obejct to feature object
        feature.Add("geometry", geometry);

        /////////start of attribures\\\\\\\\\\\\\\

        //create the attributes object
        Dictionary<string, Object> attrib = new Dictionary<string, Object>();

        //foreach(var field in attributes.get_Branch(path))

        for (int index = 0 ; index < attributes.get_Branch(path).Count; index++)
        //for (int index = 0; index <= fields.Count; index+= 1)
        {
            attrib.Add(fields[index], attributes.get_Branch(path)[index]);
        }

        //add attribures to geature
        feature.Add("attributes", attrib);
        /////////finaly step for this feature\\\\\\\\\\\\\\

        //add this feature(ie branch) to the feature list
        featuresList.Add(feature);
    }//end of for each branch


    // finaly add features list to master object
    geoDict.Add("features", featuresList);









    //Produces convert dictionary to json text

    var json = Newtonsoft.Json.JsonConvert.SerializeObject(geoDict, Newtonsoft.Json.Formatting.Indented);

    //Print(json);

    //DA.SetDataList(2, err);


    // Finally assign the retults to the output parameter.
    //DA.SetData(0, spiral);
    DA.SetData(1, json);
}

/// <summary>
/// Provides an Icon for every component that will be visible in the User Interface.
/// Icons need to be 24x24 pixels.
/// </summary>
