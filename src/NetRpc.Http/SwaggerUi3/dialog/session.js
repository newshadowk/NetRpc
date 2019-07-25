"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("{hubUrl}").build();

connection.start().then(function () {
    addText("connected!");
}).catch(function (err) {
    return console.error(err.toString());
});

connection.on("Callback", function (callId, data) {
    addText(callId + "_" + data);
});

document.getElementById("sendButton").addEventListener("click", function (event) {
    //var user = document.getElementById("userInput").value;
    //var message = document.getElementById("messageInput").value;
    connection.invoke("GetConnectionId").then((cid) => {
        var settings = {
            "async": true,
            "crossDomain": true,
            "url": "http://localhost:5000/IServiceAsync/ComplexCallAsync",
            "method": "POST",
            "headers": {
                "ConnectionId": cid,
                "CallId": "abc",
                "Content-Type": "application/json",
                "cache-control": "no-cache",
                "Postman-Token": "a31df394-8886-4592-8fcd-2c664beab992"
            },
            "processData": false,
            "data": "{\"p1\":\"111\", \"p2\":\"222\"}"
        };

        $.ajax(settings).done(function (response) {
            console.log(response);
        });
    }).catch(function (err) {
        return console.error(err.toString());
    });

    event.preventDefault();
});

document.getElementById("cancelButton").addEventListener("click", function (event) {
    connection.invoke("Cancel").catch(function (err) {
        return console.error(err.toString());
    });

    event.preventDefault();
});