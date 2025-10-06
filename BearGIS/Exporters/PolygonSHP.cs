using System;
using System.Collections;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using DotSpatial.Projections;
using DotSpatial.Data;
using System.Data;
using NetTopologySuite.Geometries;
using System.Globalization;

namespace BearGIS
{
    /// <summary>
    /// Exports Grasshopper Geometry as a Polygon .shp
    /// </summary>
    public class PolygonSHP : GH_Component
    {
        public PolygonSHP()
          : base("PolygonSHP", "Plygn-SHP-w",
              "write Polygon SHP files.",
              "BearGIS", "ExportSHP")
        { }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("polygonTree", "pgTree", "Polygons organized in a tree (one branch = one feature; first ring outer, subsequent rings holes)", GH_ParamAccess.tree);
            pManager.AddTextParameter("list of Fields for each geometry. This should not be a datatree but a simple list. To specify type use .net built in types eg fields", "f", "Field specs: name or name;System.Type (Supported: System.String, System.Int32, System.Int64, System.Double, System.Single, System.Decimal, System.Boolean, System.DateTime)", GH_ParamAccess.list);
            pManager.AddGenericParameter("attributes", "attr", "Attributes datatree (branches align with polygonTree; order aligns with fields)", GH_ParamAccess.tree);
            pManager.AddTextParameter(".prj File Path", "prj", ".prj projection file path (optional)", GH_ParamAccess.item);
            pManager.AddTextParameter("filePath", "fp", "Output shapefile path (.shp)", GH_ParamAccess.item);
            pManager.AddBooleanParameter("writeFile", "w", "Set true to write file", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Message", "msg", "Export status / warnings", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<string> fields = new List<string>();
            GH_Structure<IGH_Goo> attributes = new GH_Structure<IGH_Goo>();
            GH_Structure<GH_Curve> inputPolygonTree = new GH_Structure<GH_Curve>();

            bool writeFile = false;
            string filePath = string.Empty;
            string prj = null;
            if (!DA.GetData(5, ref writeFile)) return;
            if (!DA.GetData(4, ref filePath)) return;
            if (!DA.GetDataTree(0, out inputPolygonTree)) return;
            if (!DA.GetDataList(1, fields)) return;
            if (!DA.GetDataTree(2, out attributes)) return;
            if (!DA.GetData(3, ref prj)) return;

            string message = string.Empty;

            FeatureSet fs = new FeatureSet(FeatureType.Polygon);

            if (!string.IsNullOrWhiteSpace(prj))
            {
                try
                {
                    string cur_proj = System.IO.File.ReadAllText(prj);
                    ProjectionInfo targetProjection = new ProjectionInfo();
                    targetProjection.ParseEsriString(cur_proj);
                    fs.Projection = targetProjection;
                }
                catch (Exception ex)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Failed to read .prj: " + ex.Message);
                }
            }

            if (writeFile)
            {
                try
                {
                    foreach (string field in fields)
                    {
                        var parts = field.Split(';');
                        if (parts.Length == 2)
                        {
                            var t = Type.GetType(parts[1]);
                            fs.DataTable.Columns.Add(new DataColumn(parts[0], t ?? typeof(string)));
                        }
                        else
                        {
                            fs.DataTable.Columns.Add(new DataColumn(field, typeof(string)));
                        }
                    }

                    double tol = Rhino.RhinoDoc.ActiveDoc != null ? Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance : 1e-6;

                    int featureCount = 0;
                    foreach (GH_Path path in inputPolygonTree.Paths)
                    {
                        IList branch = inputPolygonTree.get_Branch(path);
                        if (branch == null || branch.Count == 0) continue;

                        List<LinearRing> rings = new List<LinearRing>();
                        int ringIndex = 0;
                        foreach (GH_Curve gCurve in branch)
                        {
                            Curve rhCurve = null;
                            if (!GH_Convert.ToCurve(gCurve, ref rhCurve, GH_Conversion.Both)) { ringIndex++; continue; }

                            if (!rhCurve.IsClosed)
                            {
                                Point3d s = rhCurve.PointAtStart;
                                Point3d e = rhCurve.PointAtEnd;
                                if (s.DistanceTo(e) > tol)
                                {
                                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Curve in feature {path} ring {ringIndex} not closed – skipped.");
                                    ringIndex++;
                                    continue;
                                }
                            }

                            Polyline pl;
                            if (!rhCurve.TryGetPolyline(out pl))
                            {
                                var dense = rhCurve.ToPolyline(rhCurve.SpanCount * 24, tol, 0.0, 0.0);
                                dense.TryGetPolyline(out pl);
                            }

                            if (pl == null || pl.Count < 3)
                            {
                                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Curve in feature {path} ring {ringIndex} produced insufficient vertices – skipped.");
                                ringIndex++;
                                continue;
                            }

                            if (!pl.First.Equals(pl.Last)) pl.Add(pl.First);
                            if (pl.Count < 4)
                            {
                                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Closed ring in feature {path} ring {ringIndex} has <4 vertices – skipped.");
                                ringIndex++;
                                continue;
                            }

                            var coords = new List<Coordinate>(pl.Count);
                            foreach (Point3d pt in pl)
                                coords.Add(new Coordinate(pt.X, pt.Y));

                            try
                            {
                                rings.Add(new LinearRing(coords.ToArray()));
                            }
                            catch (Exception ex)
                            {
                                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Failed to build ring for feature {path} ring {ringIndex}: {ex.Message}");
                            }
                            ringIndex++;
                        }

                        if (rings.Count == 0) continue;

                        var outer = rings[0];
                        var inners = rings.Count > 1 ? rings.GetRange(1, rings.Count - 1).ToArray() : Array.Empty<LinearRing>();
                        Polygon polyGeom = null;
                        try { polyGeom = new Polygon(outer, inners); }
                        catch (Exception ex)
                        {
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Failed to create polygon for feature {path}: {ex.Message}");
                            continue;
                        }

                        var feature = fs.AddFeature(polyGeom);
                        feature.DataRow.BeginEdit();
                        int fieldIndex = 0;
                        foreach (var attrVal in attributes.get_Branch(path))
                        {
                            if (fieldIndex >= fields.Count) break;
                            var spec = fields[fieldIndex];
                            var parts = spec.Split(';');
                            string fieldName = parts[0];
                            Type targetType = parts.Length == 2 ? (Type.GetType(parts[1]) ?? typeof(string)) : typeof(string);
                            object coerced = CoerceAttribute(attrVal, targetType, fieldName, path);
                            feature.DataRow[fieldName] = coerced;
                            fieldIndex++;
                        }
                        feature.DataRow.EndEdit();
                        featureCount++;
                    }

                    fs.SaveAs(filePath, true);
                    message = $"Wrote {featureCount} polygon features.";
                }
                catch (Exception ex)
                {
                    message = "Export failed: " + ex.Message;
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, message);
                }
            }
            else
            {
                message = "Set writeFile to true to export.";
            }

            DA.SetData(0, message);
        }

        private object CoerceAttribute(object raw, Type targetType, string fieldName, GH_Path featurePath)
        {
            if (raw == null) return DBNull.Value;
            string s = raw.ToString();
            if (string.IsNullOrWhiteSpace(s)) return DBNull.Value;
            try
            {
                if (targetType == typeof(string)) return s;
                if (targetType == typeof(int) || targetType == typeof(Int32)) return int.Parse(s, CultureInfo.InvariantCulture);
                if (targetType == typeof(long) || targetType == typeof(Int64)) return long.Parse(s, CultureInfo.InvariantCulture);
                if (targetType == typeof(double) || targetType == typeof(Double)) return double.Parse(s, CultureInfo.InvariantCulture);
                if (targetType == typeof(float) || targetType == typeof(Single)) return float.Parse(s, CultureInfo.InvariantCulture);
                if (targetType == typeof(decimal)) return decimal.Parse(s, CultureInfo.InvariantCulture);
                if (targetType == typeof(bool) || targetType == typeof(Boolean)) return bool.Parse(s);
                if (targetType == typeof(DateTime)) return DateTime.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                return Convert.ChangeType(s, targetType, CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Field '{fieldName}' (feature {featurePath}) value '{s}' failed to convert to {targetType.Name}: {ex.Message}. Set to NULL.");
                return DBNull.Value;
            }
        }

        protected override System.Drawing.Bitmap Icon => BearGIS.Properties.Resources.BearGISIconSet_16;

        public override Guid ComponentGuid => new Guid("5f77a249-93a6-472f-b835-295a537fc640");
    }
}