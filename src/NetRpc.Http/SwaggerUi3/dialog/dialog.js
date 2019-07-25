let showBtn = document.getElementById("showBtn");
let dialog = document.getElementById("viewDialog");
let cancelBtn = document.querySelector(".cancelBtn");
let callback;

showBtn.addEventListener("click",
    () => {
        dialog.classList.toggle("viewHide");
    });

cancelBtn.addEventListener("click",
    () => {
        callback();
    });

function addText(str) {
    let textarea = document.querySelector(".viewTextarea");
    textarea.value = str;
}

function addEvent(cb) {
    callback = cb;
}