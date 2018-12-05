import json
import Grasshopper.DataTree as datatree #for using data trees
import System # for creating new objects
import Grasshopper.Kernel.Data.GH_Path as ghpath #for managing paths easily
###---<<Produce Output Trees>>---###
dataKeys = datatree[System.Object]()
keyValues = datatree[System.Object]()
keyIndex = datatree[System.Object]()
"""
 "features": [
    {
      "type": "Feature",
      "properties": {},
      "geometry": {
        "type": "LineString",
        "coordinates": [
          [
            -79.9474024772644,
            40.448155427192575
          ],
          [
            -79.94511723518372,
            40.45027818251665
          ]
        ]
      }
    },
"""
"""
class geoJson_Line:
    def __init__(self, polyline, properties):
        self.type = "Feature"
        self.geometry = {}
        self.geometry['type'] = "LineString"
        self.geometry['coordinates'] = []
        self.properties = {}

        print
"""

def geoJson_Line_Generator(polyline, properties):
    this = {}
    this['type'] = "Feature"
    this['geometry'] = {}
    this['geometry']['type'] = "LineString"
    this['geometry']['coordinates'] = []
    for index, item in enumerate(polyline):
        pointList = item[1:-1].split(',')
        pointList = map(float, pointList)

        this['geometry']['coordinates'].append(pointList)

    this['properties'] = {}
    for index,item in enumerate(properties['f']):
        this['properties'][item] = properties['v'][index]

    return this


geoJson = {}
geoJson['type'] = "FeatureCollection"
geoJson['features'] = []

Values.SimplifyPaths()
Objects.SimplifyPaths()

for path in Objects.Paths:
    prop = {"f":Fields, "v":Values.Branch(path)}
    LinePoints = Objects.Branch(path)
    geoJson['features'].append(geoJson_Line_Generator(LinePoints, prop))


geoJSON = json.dumps(geoJson)
readable = json.dumps(geoJson, indent=4, sort_keys=True)

with open(FilePath, 'w') as outfile:
    json.dump(geoJson, outfile, indent=4, sort_keys=True)
    FilePath = FilePath
