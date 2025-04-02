"use strict";

//this js file is a slightly modified example from microsoft, create when chat feature
//is still work in progress, the current working code is now in ChatWindow.cshtml
//in the script section, if you want to use this feature to be handled in a
//file instead of a script tag, move the code from there to here and pass
//the @model variable to the functions

var connection = new signalR.HubConnectionBuilder().withUrl("/chatHub").build();

//Disable the send button until connection is established.
document.getElementById("sendButton").disabled = true;

//listener that will fired when "ReceiveMessage" from hub
connection.on("ReceiveMessage", function (senderName, senderId, textMessage) {
    var li = document.createElement("li");
    var chatWindow = document.getElementById("chatWindow");
    if(senderId != )
});

//initialize connection
connection.start().then(function () {
    document.getElementById("sendButton").disabled = false;
}).catch(function (err) {
    return console.error(err.toString());
});

//create event listener that will trigger hub function when fired
document.getElementById("sendButton").addEventListener("click", function (event) {
    var CurrentUserId = document.getElementById("currentUser").value;
    var CurrentGroupId = document.getElementById("currentGroup").value;
    var message = document.getElementById("messageInput").value;
    //connection.invoke("SendMessage", CurrentUserId, CurrentGroupId, message).catch(function (err) {
    //    return console.error(err.toString());
    //});
    connection.invoke("SendMessage", message).catch(function (err) {
        return console.error(err.toString());
    });
    event.preventDefault();
});