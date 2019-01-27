function loadJson(url){
  var xmlhttp = new XMLHttpRequest();
  console.log(">>>file specified");
  var dataUrl = "C:/Users/Nico/Desktop/geojson.json";
  var dataset;
  xmlhttp.onreadystatechange = function() {
      if (this.readyState == 4 && this.status == 200) {
          dataset = JSON.parse(this.responseText);
          console.log(">>>dataset loaded");
          console.log(dataset);
      }
  };
  xmlhttp.open("GET", dataUrl, true);
  xmlhttp.send();

}
