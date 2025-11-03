function loadmap(){
  // Create base layers
  var osmLayer = new ol.layer.Tile({
    source: new ol.source.OSM(),
    visible: true
  });
  osmLayer.set('name', 'OpenStreetMap');

  var satelliteLayer = new ol.layer.Tile({
    source: new ol.source.XYZ({
      url: 'https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}',
      maxZoom: 19
    }),
    visible: false
  });
  satelliteLayer.set('name', 'Satellite');

  // Create map
  let map = new ol.Map({
    target: 'map-div-id',
    layers: [osmLayer, satelliteLayer],
    view: new ol.View({
      center: [-79.9959, 40.4406],
      zoom: 9,
      projection:'EPSG:4326'
    }),
    controls: ol.control.defaults().extend([
      new ol.control.ScaleLine(),
      new ol.control.FullScreen()
    ])
  });

  // Add popup for feature info
  addPopupInteraction(map);

  return map;
}

// Add popup interaction to show feature attributes
function addPopupInteraction(map) {
  // Create popup overlay
  var popup = document.createElement('div');
  popup.id = 'map-popup';
  popup.style.cssText = 'position: absolute; background: white; padding: 15px; border-radius: 8px; box-shadow: 0 4px 12px rgba(0,0,0,0.2); max-width: 300px; display: none; z-index: 1000;';
  document.getElementById('map-div-id').appendChild(popup);

  var overlay = new ol.Overlay({
    element: popup,
    positioning: 'bottom-center',
    offset: [0, -10],
    autoPan: true,
    autoPanAnimation: {
      duration: 250
    }
  });
  map.addOverlay(overlay);

  // Click event to show popup
  map.on('click', function(evt) {
    var feature = map.forEachFeatureAtPixel(evt.pixel, function(feature) {
      return feature;
    });

    if (feature) {
      var properties = feature.getProperties();
      var content = '<div style="font-weight: bold; margin-bottom: 8px; color: #0070c0; border-bottom: 2px solid #cbe2f2; padding-bottom: 4px;">Feature Properties</div>';
      
      for (var key in properties) {
        if (key !== 'geometry') {
          content += '<div style="margin: 4px 0; font-size: 0.9rem;"><strong>' + key + ':</strong> ' + properties[key] + '</div>';
        }
      }
      
      popup.innerHTML = content;
      popup.style.display = 'block';
      overlay.setPosition(evt.coordinate);
    } else {
      popup.style.display = 'none';
      overlay.setPosition(undefined);
    }
  });

  // Change cursor on hover
  map.on('pointermove', function(evt) {
    var pixel = map.getEventPixel(evt.originalEvent);
    var hit = map.hasFeatureAtPixel(pixel);
    map.getTarget().style.cursor = hit ? 'pointer' : '';
  });
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
   radius: 6,
   fill: new ol.style.Fill({
     color: '#de3800'
   }),
   stroke: new ol.style.Stroke({
     color: 'white', 
     width: 2
   })
 });
//<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>>>>>>>\\
var styles = {
  'Point': new ol.style.Style({
    image: image
  }),
  'LineString': new ol.style.Style({
    stroke: new ol.style.Stroke({
      color: '#0070c0',
      width: 2.5
    })
  }),
  'MultiLineString': new ol.style.Style({
    stroke: new ol.style.Stroke({
      color: '#0070c0',
      width: 2.5
    })
  }),
  'MultiPoint': new ol.style.Style({
    image: image
  }),
  'MultiPolygon': new ol.style.Style({
    stroke: new ol.style.Stroke({
      color: '#0070c0',
      width: 2
    }),
    fill: new ol.style.Fill({
      color: 'rgba(0, 112, 192, 0.15)'
    })
  }),
  'Polygon': new ol.style.Style({
    stroke: new ol.style.Stroke({
      color: '#0070c0',
      width: 2
    }),
    fill: new ol.style.Fill({
      color: 'rgba(0, 112, 192, 0.15)'
    })
  }),
  'GeometryCollection': new ol.style.Style({
    stroke: new ol.style.Stroke({
      color: '#99583f',
      width: 2
    }),
    fill: new ol.style.Fill({
      color: 'rgba(153, 88, 63, 0.15)'
    }),
    image: new ol.style.Circle({
      radius: 6,
      fill: new ol.style.Fill({
        color: '#99583f'
      }),
      stroke: new ol.style.Stroke({
        color: 'white',
        width: 2
      })
    })
  }),
  'Circle': new ol.style.Style({
    stroke: new ol.style.Stroke({
      color: '#de3800',
      width: 2
    }),
    fill: new ol.style.Fill({
      color: 'rgba(222, 56, 0, 0.15)'
    })
  })
};
//<<<<<<<<<<<<<<<<<<<<<<>>>>>>>>>>>>>>>>>>>>>\\
function validateDataset(){


}
