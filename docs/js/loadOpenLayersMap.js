function loadmap(){
  let map = new ol.Map({
    target: 'map-div-id',
    layers: [
      new ol.layer.Tile({
        source: new ol.source.OSM()
      })
    ],
    view: new ol.View({
      center: [-79.9959, 40.4406],//ol.proj.fromLonLat([-79.9959, 40.4406]),
      zoom: 9,
      projection:'EPSG:4326' //Not default projection
    })
  });

  return map;
}


//<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>>>>>>>\\
function AddLayer(){
  //source vector
  var vectorSource = new ol.source.Vector({
    features: (new ol.format.GeoJSON()).readFeatures(geoJsonDataset)
  });
  console.log(">>>Source Loaded...");
  //vector layer
  var vectorLayer = new ol.layer.Vector({
    source: vectorSource,
    style: styleFunction
  });
  console.log(">>>Layer Created...");
  // add layer to map
  map.addLayer(vectorLayer);
  console.log(">>>Layer Added...");
}//end add layer


//<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>>>>>>>\\
var styleFunction = function(feature) {
  return styles[feature.getGeometry().getType()];
};
//<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>>>>>>>\\
var image = new ol.style.Circle({
   radius: 5,
   fill: null,
   stroke: new ol.style.Stroke({color: 'red', width: 1})
 });
//<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>>>>>>>\\
var styles = {
  'Point': new ol.style.Style({
    image: image
  }),
  'LineString': new ol.style.Style({
    stroke: new ol.style.Stroke({
      color: 'green',
      width: 1
    })
  }),
  'MultiLineString': new ol.style.Style({
    stroke: new ol.style.Stroke({
      color: 'green',
      width: 1
    })
  }),
  'MultiPoint': new ol.style.Style({
    image: image
  }),
  'MultiPolygon': new ol.style.Style({
    stroke: new ol.style.Stroke({
      color: 'yellow',
      width: 1
    }),
    fill: new ol.style.Fill({
      color: 'rgba(255, 255, 0, 0.1)'
    })
  }),
  'Polygon': new ol.style.Style({
    stroke: new ol.style.Stroke({
      color: 'blue',
      lineDash: [4],
      width: 3
    }),
    fill: new ol.style.Fill({
      color: 'rgba(0, 0, 255, 0.1)'
    })
  }),
  'GeometryCollection': new ol.style.Style({
    stroke: new ol.style.Stroke({
      color: 'magenta',
      width: 2
    }),
    fill: new ol.style.Fill({
      color: 'magenta'
    }),
    image: new ol.style.Circle({
      radius: 10,
      fill: null,
      stroke: new ol.style.Stroke({
        color: 'magenta'
      })
    })
  }),
  'Circle': new ol.style.Style({
    stroke: new ol.style.Stroke({
      color: 'red',
      width: 2
    }),
    fill: new ol.style.Fill({
      color: 'rgba(255,0,0,0.2)'
    })
  })
};
//<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>>>>>>>\\
function validateDataset(){


}
