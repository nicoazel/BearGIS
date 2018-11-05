# BearGIS

## Intro
BearGIS is a Grasshopper plugin written in C#

## Primary Functionality
The goal of BearGIS is to develop a plugin that allows fluid exchange of GIS data into and out of grasshopper. 
This is currently a work in progress. 

## BearGIS Components List

### Export
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

### import
This tab is for components that can import GeoJSON files with geometry and attributes into grasshopper
#### Import ESRI GeoJSON
- [ ] this component imports a ESRI formatted GeoJSON with its fields and attributes
Multi-Part geometry is handeled with sub branches eg. Branches {A:A} & {A:B} are each part of Feature "A" and would correspond to attributes on branch {A} 

#### Import GeoJSON
- [ ] this component imports normal GeoJSON with its fields and attributes
Multi-Part geometry is handeled with sub branches eg. Branches {A:A} & {A:B} are each part of Feature "A" and would correspond to attributes on branch {A} 
