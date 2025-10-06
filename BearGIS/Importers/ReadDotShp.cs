using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rg = Rhino.Geometry; // Alias Rhino geometry namespace to avoid Point ambiguity
using DotSpatial.Data;
using System.Data;

namespace BearGIS
{
    public class ReadDotShp : GH_Component
    {
        public ReadDotShp()
          : base("ReadDotShp", "DotSHP-R",
              "Reads SHP files with DotSpatial + NetTopologySuite and outputs attributes + vertex trees similar to other BearGIS importers.",
              "BearGIS", "Import")
        { }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("filePath", "fp", "File Path for shp file", GH_ParamAccess.item);
            pManager.AddBooleanParameter("readFile", "r", "set to true to read to file", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("fields", "flds", "Field names (attribute columns) in the shapefile", GH_ParamAccess.list);
            pManager.AddTextParameter("attributes", "attr", "Attribute values per feature (one branch per feature)", GH_ParamAccess.tree);
            pManager.AddGeometryParameter("features", "ftr", "Feature vertex lists (each part/ring as a sub-path)", GH_ParamAccess.tree);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string filePath = string.Empty;
            if (!DA.GetData(0, ref filePath)) return;

            bool readFile = false;
            if (!DA.GetData(1, ref readFile)) return;
            if (!readFile) return;

            if (string.IsNullOrWhiteSpace(filePath))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "File path is empty.");
                return;
            }
            if (!System.IO.File.Exists(filePath))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "File does not exist: " + filePath);
                return;
            }

            try
            {
                var featureGeometry = new GH_Structure<GH_Point>();
                var attributes = new GH_Structure<GH_String>();
                var featureFields = new List<string>();

                using (var openSHP = Shapefile.OpenFile(filePath))
                {
                    if (openSHP.DataTable != null)
                    {
                        foreach (DataColumn column in openSHP.DataTable.Columns)
                            featureFields.Add(column.ColumnName);
                    }

                    int featureIndex = 0;
                    foreach (var currentFeature in openSHP.Features)
                    {
                        // Attributes
                        GH_Path currentPath = new GH_Path(featureIndex);
                        var row = currentFeature.DataRow;
                        if (row != null)
                        {
                            foreach (var attr in row.ItemArray)
                            {
                                string text = Convert.ToString(attr);
                                if (string.IsNullOrWhiteSpace(text)) text = "nan";
                                attributes.Append(new GH_String(text), currentPath);
                            }
                        }

                        var geom = currentFeature.Geometry as NetTopologySuite.Geometries.Geometry;
                        if (geom == null)
                        {
                            featureIndex++;
                            continue;
                        }

                        int pathIndex = 0; // reset for each feature
                        AddGeometryToTree(geom, featureIndex, ref pathIndex, featureGeometry);
                        featureIndex++;
                    }
                }

                DA.SetDataList(0, featureFields);
                DA.SetDataTree(1, attributes);
                DA.SetDataTree(2, featureGeometry);
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Error reading SHP: " + ex.Message);
            }
        }

        private void AddGeometryToTree(NetTopologySuite.Geometries.Geometry geom, int featureIndex, ref int pathIndex, GH_Structure<GH_Point> tree)
        {
            // Handle generic geometry collections (excluding explicit Multi* types handled below)
            var genericCollection = geom as NetTopologySuite.Geometries.GeometryCollection;
            if (genericCollection != null &&
                !(geom is NetTopologySuite.Geometries.MultiPoint) &&
                !(geom is NetTopologySuite.Geometries.MultiLineString) &&
                !(geom is NetTopologySuite.Geometries.MultiPolygon))
            {
                int n = genericCollection.NumGeometries;
                for (int i = 0; i < n; i++)
                {
                    var sub = genericCollection.GetGeometryN(i);
                    AddGeometryToTree(sub, featureIndex, ref pathIndex, tree);
                }
                return;
            }

            // Point
            var singlePt = geom as NetTopologySuite.Geometries.Point;
            if (singlePt != null)
            {
                AppendPoint(singlePt, featureIndex, ref pathIndex, tree);
                return;
            }

            // MultiPoint
            var multiPt = geom as NetTopologySuite.Geometries.MultiPoint;
            if (multiPt != null)
            {
                int cnt = multiPt.NumGeometries;
                for (int i = 0; i < cnt; i++)
                {
                    var partPt = multiPt.GetGeometryN(i) as NetTopologySuite.Geometries.Point;
                    if (partPt != null)
                        AppendPoint(partPt, featureIndex, ref pathIndex, tree);
                }
                return;
            }

            // LineString
            var line = geom as NetTopologySuite.Geometries.LineString;
            if (line != null)
            {
                AppendLineString(line, featureIndex, ref pathIndex, tree);
                return;
            }

            // MultiLineString
            var mls = geom as NetTopologySuite.Geometries.MultiLineString;
            if (mls != null)
            {
                int cnt = mls.NumGeometries;
                for (int i = 0; i < cnt; i++)
                {
                    var ls = mls.GetGeometryN(i) as NetTopologySuite.Geometries.LineString;
                    if (ls != null) AppendLineString(ls, featureIndex, ref pathIndex, tree);
                }
                return;
            }

            // Polygon
            var poly = geom as NetTopologySuite.Geometries.Polygon;
            if (poly != null)
            {
                AppendPolygon(poly, featureIndex, ref pathIndex, tree);
                return;
            }

            // MultiPolygon
            var mpoly = geom as NetTopologySuite.Geometries.MultiPolygon;
            if (mpoly != null)
            {
                int cnt = mpoly.NumGeometries;
                for (int i = 0; i < cnt; i++)
                {
                    var subPoly = mpoly.GetGeometryN(i) as NetTopologySuite.Geometries.Polygon;
                    if (subPoly != null) AppendPolygon(subPoly, featureIndex, ref pathIndex, tree);
                }
                return;
            }

            // Fallback (treat coordinate sequence as a single path)
            var coords = geom.Coordinates;
            var list = new List<GH_Point>();
            foreach (var c in coords)
                list.Add(new GH_Point(new Rg.Point3d(c.X, c.Y, 0)));
            tree.AppendRange(list, new GH_Path(featureIndex, pathIndex));
            pathIndex++;
        }

        private void AppendPoint(NetTopologySuite.Geometries.Point gPt, int featureIndex, ref int pathIndex, GH_Structure<GH_Point> tree)
        {
            var list = new List<GH_Point> { new GH_Point(new Rg.Point3d(gPt.X, gPt.Y, 0)) };
            tree.AppendRange(list, new GH_Path(featureIndex, pathIndex));
            pathIndex++;
        }

        private void AppendLineString(NetTopologySuite.Geometries.LineString line, int featureIndex, ref int pathIndex, GH_Structure<GH_Point> tree)
        {
            var list = new List<GH_Point>();
            var coords = line.Coordinates;
            foreach (var c in coords)
                list.Add(new GH_Point(new Rg.Point3d(c.X, c.Y, 0)));
            tree.AppendRange(list, new GH_Path(featureIndex, pathIndex));
            pathIndex++;
        }

        private void AppendPolygon(NetTopologySuite.Geometries.Polygon poly, int featureIndex, ref int pathIndex, GH_Structure<GH_Point> tree)
        {
            AppendLineString(poly.ExteriorRing as NetTopologySuite.Geometries.LineString, featureIndex, ref pathIndex, tree);
            int holes = poly.NumInteriorRings;
            for (int i = 0; i < holes; i++)
                AppendLineString(poly.GetInteriorRingN(i) as NetTopologySuite.Geometries.LineString, featureIndex, ref pathIndex, tree);
        }

        protected override System.Drawing.Bitmap Icon => BearGIS.Properties.Resources.BearGISIconSet_20;

        public override Guid ComponentGuid => new Guid("c62fed23-35be-4b28-8fb1-472b5243dfe7");
    }
}