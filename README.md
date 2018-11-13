# BearGIS

## Intro
BearGIS is a Grasshopper plugin written in C#

## Primary Functionality
The goal of BearGIS is to develop a plugin that allows fluid exchange of GIS data into and out of grasshopper. 

This is currently a __work in progress__. Checked components are integrated into master and functioning at some level. There may still be errors in some use cases. Any unchecked component is eather planed but not implmented or still in a Dev Branch. 

## BearGIS Components List

### Export Tab
This tab is for components that export geometry with attributes to GeoJson formats

#### Polyline to ESRI
- [x] this component converts Grasshopper Polylines to ESRI GeoJSON
#### Polyline to GeoJSON
- [ ] this component converts Grasshopper Polylines to ESRI GeoJSON
#### Polygon to ESRI
- [ ] this component converts Grasshopper Polygons to ESRI GeoJSON
#### Polylgon to GeoJSON
- [ ] this component converts Grasshopper Polygons to ESRI GeoJSON
#### Point to ESRI
- [ ] this component converts Grasshopper Pooint to ESRI GeoJSON
#### Point to GeoJSON
- [ ] this component converts Grasshopper Point to ESRI GeoJSON


### Import Tab
This tab is for components that can import GeoJSON files with geometry and attributes into grasshopper

#### Import ESRI GeoJSON
- [x] this component imports a ESRI formatted GeoJSON with its fields and attributes
Multi-Part geometry is handeled with sub branches eg. Branches {A:A} & {A:B} are each part of Feature "A" and would correspond to attributes on branch {A} 
#### Import GeoJSON
- [ ] this component imports normal GeoJSON with its fields and attributes
Multi-Part geometry is handeled with sub branches eg. Branches {A:A} & {A:B} are each part of Feature "A" and would correspond to attributes on branch {A} 

### Convert Tab

#### Lat Long to Point (ESRI)
- [x] this component takes latatude, longatude, and desired projection system from an ESRI .prj file (ESRI WKT) and generates a point. This can be used to place refrancepoints or situate a base map for drawing over if refrance data is unavailable. All points are [WGS84 - World Geodetic System 1984 EPSG:4326](http://epsg.io/4326)

#### Lat Long to Point (EPSG)
- [ ] this component takes latatude, longatude, and EPSG code for the desired projection system to generates a point. This can be used to place refrancepoints or situate a base map for drawing over if refrance data is unavailable. All points are [WGS84 - World Geodetic System 1984 EPSG:4326](http://epsg.io/4326)

