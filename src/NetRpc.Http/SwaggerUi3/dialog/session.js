"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("{hubUrl}").build();

//GetConnId
connection.start().then(function() {
    addText("signalR connected!");
    connection.invoke("GetConnId").then((cid) => {
        addText("GetConnId, _connId:" + cid);
    });
}).catch(function(err) {
    return console.error(err.toString());
});

//Callback
connection.on("Callback",
    function(callId, data) {
        addText("callback, callId:" + callId + ", " + data);
    });

//Progress
connection.on("UploadProgress",
    function (callId, data) {
        addText("progress, callId:" + callId + ", " + data);
    });

//Cancel
//arg0 is callId, if set "" means cancel all methods.
document.querySelector(".cancel-btn").addEventListener("click",
    function(event) {
        connection.invoke("Cancel", "").catch(function(err) {
            return console.error(err.toString());
        });

        event.preventDefault();
    });