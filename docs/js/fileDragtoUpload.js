
function handleFileSelect(evt) {
  dropZone.style.background = "#30964b";
  evt.stopPropagation();
  evt.preventDefault();
  var files = evt.dataTransfer.files; // FileList object.

  // files is a FileList of File objects. List some properties.
  var output = [];
  for (var i = 0, f; f = files[i]; i++) {
    output.push('<li><strong>', escape(f.name), '</strong> (', f.type || 'n/a', ') - ',
                f.size, ' bytes, last modified: ',
                f.lastModifiedDate ? f.lastModifiedDate.toLocaleDateString() : 'n/a',
                '</li>');
  }//end for each file
  document.getElementById('file-drop-list').innerHTML = '<ul>' + output.join('') + '</ul>';

  var theFile = files[0];
  var reader = new FileReader();
  reader.onload = function(evt) {

    let fileReaderText = evt.target.result;
    let fileDataSet = JSON.parse(fileReaderText);
    console.log(fileDataSet);
    geoJsonDataset = fileDataSet;

  };
  reader.readAsText(theFile);



}//end handleFilesSelect

function handleDragOver(evt) {
  evt.stopPropagation();
  evt.preventDefault();
  evt.dataTransfer.dropEffect = 'copy'; // Explicitly show this is a copy.
}
function  handleDragEnter(evt){
  dropZone.style.background = "#8dd9a1";
  dropZone.style.color = "#ffffff";
}
function  handleDragLeave(evt){
  dropZone.style.background = "#ffffff";
  dropZone.style.color = "black";
}
// Setup the dnd listeners.
var dropZone = document.getElementById('file-drop-div');
dropZone.addEventListener('dragover', handleDragOver, false);
dropZone.addEventListener('dragenter', handleDragEnter, false);
dropZone.addEventListener('dragleave', handleDragLeave, false);
dropZone.addEventListener('drop', handleFileSelect, false);
