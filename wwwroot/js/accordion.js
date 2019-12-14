var accordions = document.getElementsByClassName("accordion");

for (var i = 0; i < accordions.length; i++) {
    accordions[i].addEventListener("click", function () {
        SetAccordion('toggle', this)
    })
}

function SetAccordion(flag, obj) {
    var content = obj.nextElementSibling
    if (flag == 'toggle') {
        obj.classList.toggle('is-open');
        if (content.style.maxHeight) {
            content.style.maxHeight = null;
        } else {
            content.style.maxHeight = content.scrollHeight + "px";
        }
    } else if (flag == 'open') {
        obj.classList.add('is-open');
        content.style.maxHeight = content.scrollHeight + "px";
    } else {
        if (flag == 'close') {
            obj.classList.remove('is-open');
            content.style.maxHeight = null;
        }
    }
}
