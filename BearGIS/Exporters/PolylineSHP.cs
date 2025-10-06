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
    public class PolylineSHP : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the PolygonJSON class.
        /// </summary>
        public PolylineSHP()
          : base("PolylineSHP", "Polyline-SHP-w",
              "write Polyline SHP files.",
              "BearGIS", "ExportSHP")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("polylineTree", "plTree", "Polylines organized in a tree (one branch = one feature)", GH_ParamAccess.tree);
            pManager.AddTextParameter("fields", "f", "list of Fields for each geometry. This should not be a datatree but a simple list. To specify type use .net built in types eg Field specs: name or name;System.Type (Supported: System.String, System.Int32, System.Int64, System.Double, System.Single, System.Decimal, System.Boolean, System.DateTime)", GH_ParamAccess.list);
            pManager.AddGenericParameter("attributes", "attr", "Attributes datatree (branches align with polylineTree; order aligns with fields)", GH_ParamAccess.tree);
            pManager.AddTextParameter(".prj File Path", "prj", "Projection .prj file path (optional)", GH_ParamAccess.item);
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
            GH_Structure<GH_Curve> inputPolylineTree = new GH_Structure<GH_Curve>();

            bool writeFile = false;
            string filePath = "";
            string prj = null;
            if (!DA.GetData(5, ref writeFile)) return;
            if (!DA.GetData(4, ref filePath)) return;
            if (!DA.GetDataTree(0, out inputPolylineTree)) return;
            if (!DA.GetDataList(1, fields)) return;
            if (!DA.GetDataTree(2, out attributes)) return;
            if (!DA.GetData(3, ref prj)) return;

            string message = string.Empty;

            FeatureSet fs = new FeatureSet(FeatureType.Line);

            if (!string.IsNullOrWhiteSpace(prj))
            {
                try
                {
                    string cur_proj = System.IO.File.ReadAllText(@prj);
                    var targetProjection = new ProjectionInfo();
                    targetProjection.ParseEsriString(cur_proj);
                    fs.Projection = targetProjection;
                }
                catch (System.Exception ex)
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
                            var t = System.Type.GetType(parts[1]);
                            fs.DataTable.Columns.Add(new DataColumn(parts[0], t ?? typeof(string)));
                        }
                        else
                        {
                            fs.DataTable.Columns.Add(new DataColumn(field, typeof(string)));
                        }
                    }

                    int featureCount = 0;
                    foreach (GH_Path path in inputPolylineTree.Paths)
                    {
                        IList branch = inputPolylineTree.get_Branch(path);
                        if (branch == null || branch.Count == 0) continue;

                        var lineStrings = new List<LineString>();
                        foreach (GH_Curve curve in branch)
                        {
                            Curve rhCurve = null;
                            if (!GH_Convert.ToCurve(curve, ref rhCurve, GH_Conversion.Both)) continue;
                            Polyline pl;
                            if (!rhCurve.TryGetPolyline(out pl))
                            {
                                var dense = rhCurve.ToPolyline(rhCurve.SpanCount * 24, Rhino.RhinoDoc.ActiveDoc?.ModelAbsoluteTolerance ?? 1e-6, 0.0, 0.0);
                                dense.TryGetPolyline(out pl);
                            }
                            if (pl == null || pl.Count < 2) continue;
                            var coords = new List<Coordinate>(pl.Count);
                            foreach (Point3d pt in pl)
                                coords.Add(new Coordinate(pt.X, pt.Y));
                            lineStrings.Add(new LineString(coords.ToArray()));
                        }
                        if (lineStrings.Count == 0) continue;

                        var geom = GeometryFactory.Default.CreateMultiLineString(lineStrings.ToArray());
                        var feature = fs.AddFeature(geom);

                        feature.DataRow.BeginEdit();
                        int fieldIndex = 0;
                        foreach (var attrVal in attributes.get_Branch(path))
                        {
                            if (fieldIndex >= fields.Count) break;
                            var spec = fields[fieldIndex];
                            var parts = spec.Split(';');
                            string fieldName = parts[0];
                            System.Type targetType = parts.Length == 2 ? (System.Type.GetType(parts[1]) ?? typeof(string)) : typeof(string);
                            object coerced = CoerceAttribute(attrVal, targetType, fieldName, path);
                            feature.DataRow[fieldName] = coerced;
                            fieldIndex++;
                        }
                        feature.DataRow.EndEdit();
                        featureCount++;
                    }

                    fs.SaveAs(filePath, true);
                    message = $"Wrote {featureCount} polyline features.";
                }
                catch (System.Exception ex)
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

        private object CoerceAttribute(object raw, System.Type targetType, string fieldName, GH_Path featurePath)
        {
            if (raw == null) return System.DBNull.Value;
            string s = raw.ToString();
            if (string.IsNullOrWhiteSpace(s)) return System.DBNull.Value;
            try
            {
                if (targetType == typeof(string)) return s;
                if (targetType == typeof(int) || targetType == typeof(System.Int32)) return int.Parse(s, CultureInfo.InvariantCulture);
                if (targetType == typeof(long) || targetType == typeof(System.Int64)) return long.Parse(s, CultureInfo.InvariantCulture);
                if (targetType == typeof(double) || targetType == typeof(System.Double)) return double.Parse(s, CultureInfo.InvariantCulture);
                if (targetType == typeof(float) || targetType == typeof(System.Single)) return float.Parse(s, CultureInfo.InvariantCulture);
                if (targetType == typeof(decimal)) return decimal.Parse(s, CultureInfo.InvariantCulture);
                if (targetType == typeof(bool) || targetType == typeof(System.Boolean)) return bool.Parse(s);
                if (targetType == typeof(System.DateTime)) return System.DateTime.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                return System.Convert.ChangeType(s, targetType, CultureInfo.InvariantCulture);
            }
            catch (System.Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Field '{fieldName}' (feature {featurePath}) value '{s}' failed to convert to {targetType.Name}: {ex.Message}. Set to NULL.");
                return System.DBNull.Value;
            }
        }

        protected override System.Drawing.Bitmap Icon
        {
            get { return BearGIS.Properties.Resources.BearGISIconSet_15; }
        }

        public override System.Guid ComponentGuid
        {
            get { return new System.Guid("6e360e79-a67c-483d-845d-5c5c08dc2c7d"); }
        }
    }
}