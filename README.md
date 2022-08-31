# BearGIS
<img src="https://github.com/nicoazel/BearGIS/raw/master/docs/img/BearGISIcon.png" alt="BearGIS Icon" height="300"/>

## Intro
BearGIS is a Grasshopper plugin written in C#

Check out the plugin-page: https://nicoazel.github.io/BearGIS/

Check out Demo Video: [YouTube Video](https://youtu.be/wtc19CEzHuw)

See Examples: [BearGIS Example.zip](https://github.com/nicoazel/BearGIS/raw/master/docs/BearGIS%20Example.zip)

See Forum: [BearGIS Plugin Discussion](https://discourse.mcneel.com/t/beargis-plugin-gis-data-reader-writer-shp-geojson/78602)

## Primary Functionality
The goal of BearGIS is to develop a plugin that allows fluid exchange of GIS data into and out of grasshopper.

This is currently a __work in progress__. Checked components are integrated into master and functioning at some level. There may still be errors in some use cases. Any unchecked component is eather planed but not implmented or still in a Dev Branch.

## BearGIS Components List

### Export Tab
This tab is for components that export geometry with attributes to GeoJson formats

#### Polyline to ESRI
- [x] this component converts Grasshopper Polylines to ESRI GeoJSON
#### Polyline to GeoJSON
- [ ] this component converts Grasshopper Polylines to GeoJSON
#### Polyline to SHP
- [X] this component converts Grasshopper Polylines to SHP
#### Polygon to ESRI
- [x] this component converts Grasshopper Polygons to ESRI GeoJSON
#### Polylgon to GeoJSON
- [x] this component converts Grasshopper Polygons to GeoJSON
#### Polylgon to SHP
- [X] this component converts Grasshopper Polygons to SHP
#### Point to ESRI
- [X] this component converts Grasshopper Point to ESRI GeoJSON
#### Point to GeoJSON
- [x] this component converts Grasshopper Point to GeoJSON
#### Multi-Point to ESRI
- [x] this component converts Grasshopper Point to ESRI GeoJSON
#### Multi-Point to GeoJSON
- [x] this component converts Grasshopper Point to GeoJSON
#### Multi-Point to SHP
- [X] this component converts Grasshopper Point to SHP

### Import Tab
This tab is for components that can import GeoJSON files with geometry and attributes into grasshopper.

#### Read SHP
- [x] this component reads .shp files into grasshopper! mlti-Part geometry is handeled with sub branches eg. Branches {A:A} & {A:B} are each part of Feature "A" and would correspond to attributes on branch {A} . hint use pathMapper {a;b}=>{a} after generating polylines and polygons.


#### Read SHP
- [x] this component reads .shp files into grasshopper! mlti-Part geometry is handeled with sub branches eg. Branches {A:A} & {A:B} are each part of Feature "A" and would correspond to attributes on branch {A} . hint use pathMapper {a;b}=>{a} after generating polylines and polygons. Note - This component is currently very slow, this needs some further investigation.

#### Import ESRI GeoJSON
- [x] this component imports a ESRI formatted GeoJSON with its fields and attributes
Multi-Part geometry is handeled with sub branches eg. Branches {A:A} & {A:B} are each part of Feature "A" and would correspond to attributes on branch {A}
#### Import GeoJSON
- [x] this component imports normal GeoJSON with its fields and attributes
Multi-Part geometry is handeled with sub branches eg. Branches {A:A} & {A:B} are each part of Feature "A" and would correspond to attributes on branch {A}

### Convert Tab

#### Lat Long to Point
- [x] this component takes latatude, longatude, and desired projection system from an ESRI .prj file (ESRI WKT) and generates a point. This can be used to place refrance points or situate a base map for drawing over if refrance data is unavailable. All points are [WGS84 - World Geodetic System 1984 EPSG:4326](http://epsg.io/4326)


#### Point Lat Lon
- [x] this component converts a point to lat lon for geojson based on a .prj file for the points projection system. all outputes are in wsg84

#### reproject
- [x] this component converts points between two projection systems.
