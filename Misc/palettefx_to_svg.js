//For use on http://palettefx.com/ after extracting colors from an image
//Paste into console, get output text, save as svg file, import with color palette importer

(function extractColors() {
  var palette = [];
  $('ul.colorbars li').each(function(){
    var liColor = $(this).css("background-color");
    var hexColor = color.rgb2hex(color.str2rgb(liColor)).replace('#', '');
    var colorName = (typeof color.name[hexColor] == "undefined") ? "" : color.name[hexColor];

    palette.push({hexColor, colorName});
  });

  console.log(palette.map(p => `<tmp fill="#${p.hexColor}" name="${p.colorName}" />`).join('\n'))
})()