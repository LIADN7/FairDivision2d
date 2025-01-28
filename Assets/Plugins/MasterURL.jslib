mergeInto(LibraryManager.library, {
    GetCurrentUrl: function () {
var url = window.location != window.parent.location ? document.referrer : document.location.href;
var params = new URL(url).searchParams;
var jsonObject = {};
params.forEach(function (value, key) {
    jsonObject[key] = value;
});
var jsonString = JSON.stringify(jsonObject);
var length = lengthBytesUTF8(jsonString) + 1;
var buffer = _malloc(length);
stringToUTF8(jsonString, buffer, length);
console.log(JSON.stringify(jsonObject));
return buffer;
    },
});
