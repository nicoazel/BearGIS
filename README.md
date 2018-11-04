# BearGIS

## Intro
BearGIS is a Grasshopper plugin written in C#

## Primary Functionality
The goal of BearGIS is to develop a plugin that allows fluid exchange of GIS data into and out of grasshopper. 
This is currently a work in progress. 

## Components

### Export

#### - [x] Polyline to ESRI
this component converts Grasshopper Polylines to ESRI GeoJSON

#### - [ ] Polyline to GeoJSON
this component converts Grasshopper Polylines to ESRI GeoJSON

### import

#### -[x] Import ESRI GeoJSON
this component imports a ESRI formatted GeoJSON with its fields and attributes
Multi-Part geometry is handeled with sub branches eg. Branches {A:A} & {A:B} are each part of Feature "A" and would correspond to attributes on branch {A} 
