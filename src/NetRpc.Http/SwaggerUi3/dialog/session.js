"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("{hubUrl}").build();

//GetConnectionId
connection.start().then(function () {
    addText("signalR connected!");
    connection.invoke("GetConnectionId").then((cid) => {
        addText("GetConnectionId, _connectionId:" + cid);
    });
}).catch(function (err) {
    return console.error(err.toString());
});

//Callback
connection.on("Callback", function (callId, data) {
    addText("callback, callId:" + callId + ", data:" + data);
});

//Cancel
//arg0 is callId, if set "" means cancel all methods.
document.getElementById("cancelBtn").addEventListener("click", function (event) {
    connection.invoke("Cancel", "").catch(function (err) {
        return console.error(err.toString());
    });

    event.preventDefault();
});