// Enhanced file handler for GeoJSON and Shapefile support
var uploadedFiles = [];
var pendingShpFiles = {};
var dragCounter = 0; // Track drag enter/leave properly

// Setup drag and drop listeners
function setupFileUpload() {
  var dropZone = document.getElementById('file-drop-div');
  
  // Prevent default drag behaviors on entire document
  ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(function(eventName) {
    document.body.addEventListener(eventName, preventDefaults, false);
  });
  
  // Highlight drop zone when item is dragged over
  ['dragenter', 'dragover'].forEach(function(eventName) {
    dropZone.addEventListener(eventName, handleDragOver, false);
  });
  
  // Handle drag enter with counter to fix flickering
  dropZone.addEventListener('dragenter', handleDragEnter, false);
  
  // Handle drag leave
  dropZone.addEventListener('dragleave', handleDragLeave, false);
  
  // Handle dropped files
  dropZone.addEventListener('drop', handleDrop, false);
}

function preventDefaults(e) {
  e.preventDefault();
  e.stopPropagation();
}

function handleDragOver(e) {
  preventDefaults(e);
  e.dataTransfer.dropEffect = 'copy';
}

function handleDragEnter(e) {
  preventDefaults(e);
  dragCounter++;
  var dropZone = document.getElementById('file-drop-div');
  dropZone.classList.add('drag-over');
}

function handleDragLeave(e) {
  preventDefaults(e);
  dragCounter--;
  
  // Only remove highlight if we've left the drop zone completely
  if (dragCounter === 0) {
    var dropZone = document.getElementById('file-drop-div');
    dropZone.classList.remove('drag-over');
  }
}

function handleDrop(e) {
  preventDefaults(e);
  
  // Reset drag counter
  dragCounter = 0;
  
  var dropZone = document.getElementById('file-drop-div');
  dropZone.classList.remove('drag-over');
  
  var files = e.dataTransfer.files;
  
  if (files.length === 0) return;
  
  // Show file list container
  document.getElementById('file-list-container').style.display = 'block';
  
  // Process each file
  for (var i = 0; i < files.length; i++) {
    processFile(files[i]);
  }
}

function processFile(file) {
  var fileName = file.name.toLowerCase();
  var fileExt = fileName.split('.').pop();
  
  if (fileExt === 'geojson' || fileExt === 'json') {
    // Handle GeoJSON files
    loadGeoJSON(file);
  } else if (fileExt === 'zip') {
    // Handle ZIP files (may contain shapefiles)
    loadZippedShapefile(file);
  } else if (fileExt === 'shp' || fileExt === 'dbf' || fileExt === 'shx' || fileExt === 'prj') {
    // Handle shapefile components
    loadShapefileComponent(file);
  } else {
    console.warn('Unsupported file type: ' + fileExt);
    showNotification('Unsupported file type: .' + fileExt, 'warning');
  }
}

function loadGeoJSON(file) {
  var reader = new FileReader();
  
  reader.onload = function(evt) {
    try {
      var fileContent = evt.target.result;
      var geoJsonData = JSON.parse(fileContent);
      
      // Add to map
      addGeoJSONLayer(geoJsonData, file.name);
      
      // Update file list
      addFileToList(file.name, file.size, 'geojson');
      
      showNotification('GeoJSON loaded: ' + file.name, 'success');
    } catch (error) {
      console.error('Error parsing GeoJSON:', error);
      showNotification('Error loading GeoJSON: ' + error.message, 'error');
    }
  };
  
  reader.onerror = function() {
    showNotification('Error reading file: ' + file.name, 'error');
  };
  
  reader.readAsText(file);
}

function loadZippedShapefile(file) {
  var reader = new FileReader();
  
  reader.onload = function(evt) {
    try {
      var arrayBuffer = evt.target.result;
      
      // Use shpjs library to parse zipped shapefile
      shp(arrayBuffer).then(function(geojson) {
        // Handle both single and multiple shapefiles in zip
        if (Array.isArray(geojson)) {
          geojson.forEach(function(layer, index) {
            addGeoJSONLayer(layer, file.name + ' (Layer ' + (index + 1) + ')');
          });
        } else {
          addGeoJSONLayer(geojson, file.name);
        }
        
        addFileToList(file.name, file.size, 'shapefile');
        showNotification('Shapefile loaded: ' + file.name, 'success');
      }).catch(function(error) {
        console.error('Error parsing shapefile:', error);
        showNotification('Error loading shapefile: ' + error.message, 'error');
      });
    } catch (error) {
      console.error('Error reading zip file:', error);
      showNotification('Error reading zip: ' + error.message, 'error');
    }
  };
  
  reader.onerror = function() {
    showNotification('Error reading file: ' + file.name, 'error');
  };
  
  reader.readAsArrayBuffer(file);
}

function loadShapefileComponent(file) {
  var fileName = file.name;
  var baseName = fileName.substring(0, fileName.lastIndexOf('.'));
  var extension = fileName.split('.').pop().toLowerCase();
  
  // Store the file in pending collection
  if (!pendingShpFiles[baseName]) {
    pendingShpFiles[baseName] = {};
  }
  
  var reader = new FileReader();
  
  reader.onload = function(evt) {
    pendingShpFiles[baseName][extension] = evt.target.result;
    
    // Check if we have the minimum required files (.shp, .dbf, .shx)
    if (pendingShpFiles[baseName]['shp'] && 
        pendingShpFiles[baseName]['dbf']) {
      
      // Try to load the shapefile
      try {
        var shpBuffer = pendingShpFiles[baseName]['shp'];
        var dbfBuffer = pendingShpFiles[baseName]['dbf'];
        var shxBuffer = pendingShpFiles[baseName]['shx'];
        var prjString = pendingShpFiles[baseName]['prj'];
        
        // Use shpjs to parse the shapefile components
        shp.combine([
          shp.parseShp(shpBuffer, prjString),
          shp.parseDbf(dbfBuffer)
        ]).then(function(geojson) {
          addGeoJSONLayer(geojson, baseName);
          addFileToList(baseName + '.shp', file.size, 'shapefile');
          showNotification('Shapefile loaded: ' + baseName, 'success');
          
          // Clear pending files
          delete pendingShpFiles[baseName];
        }).catch(function(error) {
          console.error('Error parsing shapefile components:', error);
          showNotification('Error: ' + error.message, 'error');
        });
      } catch (error) {
        console.error('Error processing shapefile:', error);
        showNotification('Error processing shapefile: ' + error.message, 'error');
      }
    }
  };
  
  reader.onerror = function() {
    showNotification('Error reading file: ' + fileName, 'error');
  };
  
  // Read file based on extension
  if (extension === 'prj') {
    reader.readAsText(file);
  } else {
    reader.readAsArrayBuffer(file);
  }
}

function addGeoJSONLayer(geojsonData, layerName) {
  try {
    // Create vector source from GeoJSON
    var vectorSource = new ol.source.Vector({
      features: (new ol.format.GeoJSON()).readFeatures(geojsonData, {
        dataProjection: 'EPSG:4326',
        featureProjection: 'EPSG:4326'
      })
    });
    
    // Create vector layer with styling
    var vectorLayer = new ol.layer.Vector({
      source: vectorSource,
      style: styleFunction
    });
    
    // Store layer info
    vectorLayer.set('layerName', layerName);
    vectorLayer.set('layerId', 'layer_' + layerCount++);
    
    // Add layer to map
    map.addLayer(vectorLayer);
    loadedLayers.push(vectorLayer);
    
    // Zoom to layer extent if features exist
    var extent = vectorSource.getExtent();
    if (extent && extent[0] !== Infinity) {
      map.getView().fit(extent, {
        padding: [50, 50, 50, 50],
        maxZoom: 16,
        duration: 1000
      });
    }
    
    console.log('Layer added:', layerName);
  } catch (error) {
    console.error('Error adding layer:', error);
    showNotification('Error adding layer to map: ' + error.message, 'error');
  }
}

function addFileToList(fileName, fileSize, fileType) {
  var fileList = document.getElementById('file-drop-list');
  
  var fileItem = document.createElement('div');
  fileItem.className = 'file-item';
  
  var fileInfo = document.createElement('div');
  fileInfo.className = 'file-info';
  
  var nameSpan = document.createElement('div');
  nameSpan.className = 'file-name';
  nameSpan.textContent = fileName;
  
  var sizeSpan = document.createElement('div');
  sizeSpan.className = 'file-size';
  sizeSpan.textContent = formatFileSize(fileSize) + ' • ' + fileType.toUpperCase();
  
  fileInfo.appendChild(nameSpan);
  fileInfo.appendChild(sizeSpan);
  
  var removeBtn = document.createElement('button');
  removeBtn.className = 'remove-file';
  removeBtn.textContent = 'Remove';
  removeBtn.onclick = function() {
    // Remove the last added layer
    if (loadedLayers.length > 0) {
      var layer = loadedLayers.pop();
      map.removeLayer(layer);
      fileItem.remove();
      
      if (loadedLayers.length === 0) {
        document.getElementById('file-list-container').style.display = 'none';
      }
    }
  };
  
  fileItem.appendChild(fileInfo);
  fileItem.appendChild(removeBtn);
  
  fileList.appendChild(fileItem);
}

function clearAllLayers() {
  // Remove all loaded layers
  loadedLayers.forEach(function(layer) {
    map.removeLayer(layer);
  });
  
  loadedLayers = [];
  layerCount = 0;
  
  // Clear file list
  document.getElementById('file-drop-list').innerHTML = '';
  document.getElementById('file-list-container').style.display = 'none';
  
  // Clear pending shapefile components
  pendingShpFiles = {};
  
  showNotification('All layers cleared', 'success');
}

function formatFileSize(bytes) {
  if (bytes === 0) return '0 Bytes';
  
  var k = 1024;
  var sizes = ['Bytes', 'KB', 'MB', 'GB'];
  var i = Math.floor(Math.log(bytes) / Math.log(k));
  
  return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i];
}

function showNotification(message, type) {
  console.log('[' + type.toUpperCase() + '] ' + message);
  
  // You can implement a visual notification system here if desired
  // For now, just logging to console
}

// Initialize when DOM is ready
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', setupFileUpload);
} else {
  setupFileUpload();
}
