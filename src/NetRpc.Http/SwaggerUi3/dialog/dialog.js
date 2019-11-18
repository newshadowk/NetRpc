let showBtn = document.querySelector(".show-btn");
let dialog = document.querySelector(".dialog");
let cancelBtn = document.querySelector(".cancel-btn");
let callback;

showBtn.addEventListener("click", () => {
  dialog.classList.toggle("hidden");
});

cancelBtn.addEventListener("click", () => {
  callback();
});

function addText(str) {
  let textarea = document.querySelector(".textarea");
  textarea.value += str + "\r\n";
}

function addEvent(cb) {
  callback = cb;
}
