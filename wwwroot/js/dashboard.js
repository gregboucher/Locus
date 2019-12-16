var dashboard__tables = document.querySelectorAll("table[data-name=dashboard-group__table]");
var dashboard__cards = document.querySelectorAll(".dashboard-overview__card");
inputActive = false;

dashboard__cards[0].addEventListener("click", function(){
    var selected = IsCardSelected(this);
    //show all rows and open all groups
    Update('open-all', selected);
})
dashboard__cards[1].addEventListener("click", function(){
    var selected = IsCardSelected(this);
    Update('table__asset-icon--due', selected);
})
dashboard__cards[2].addEventListener("click", function(){
    var selected = IsCardSelected(this);
    Update('table__asset-icon--overdue', selected);
})

function IsCardSelected(currentCard) {
    var selected;
    document.getElementById("top-bar__input").value = '';
    dashboard__cards.forEach(function(card){
        if (card != currentCard) {
            card.classList.remove('is-active');
        }
    });
    ((currentCard.classList.contains('is-active')) ? selected = false : selected = true);
    currentCard.classList.toggle('is-active');
    return selected;
}

function Update(phrase, selected) {
    //undefined = call coming from input field
    if (selected == undefined) {
        //remove any filters on cards
        dashboard__cards.forEach(function(card){
            card.classList.remove('is-active');
        });
        if (phrase == "") {inputActive = false;}
        if (phrase.length > 2 || inputActive) {
            inputActive = true;
            //search fields for phrase and hide row if no match
            dashboard__tables.forEach(function(table){
                obj = table.parentNode.previousElementSibling;
                var tr = table.getElementsByTagName("tr");
                var openGroup = false;
                for (i = 1; i < tr.length; i++) {
                    var td = tr[i].getElementsByClassName("table__search-content");
                    for (j = 0; j < td.length; j++) {
                        if (td[j].innerHTML.toUpperCase().indexOf(phrase) > -1) {
                            tr[i].style.display = "";
                            openGroup = true;
                            break;
                        } else {
                            tr[i].style.display = "none";
                        }
                    }
                }
                ((openGroup == true) ? SetAccordion('open', obj) : SetAccordion('close', obj));
            });
        } else {
            //input is empty and no card selected = show all rows
            dashboard__tables.forEach(function(table){
                obj = table.parentNode.previousElementSibling;
                var tr = table.getElementsByTagName("tr");
                for (i = 1; i < tr.length; i++) {
                    tr[i].style.display = "";
                }
                SetAccordion('close', obj)
            });
        }
    } else {
        //call coming from card filter
        inputActive = false;
        dashboard__tables.forEach(function(table){
            obj = table.parentNode.previousElementSibling;
            var tr = table.getElementsByTagName("tr");
            if (selected == true) {
                var openGroup = false;
                //search all rows for status div whose class matches phrase, hide row if no match
                //show all rows and open all groups if phrase is 'open-all'
                for (i = 1; i < tr.length; i++) {
                    var td = tr[i].getElementsByClassName("table__search-classList");
                    for (j = 0; j < td.length; j++) {
                        if (phrase == 'open-all' || td[j].classList.contains(phrase)) {
                            tr[i].style.display = "";
                            openGroup = true;
                            break
                        } else {
                            tr[i].style.display = "none";
                        }
                    }
                }
                ((openGroup == true) ? SetAccordion('open', obj) : SetAccordion('close', obj));
            } else {
                //card is de-selected, show all rows
                for (i = 1; i < tr.length; i++) {
                    tr[i].style.display = "";
                }
                SetAccordion('close', obj)
            }
        });
    }
}