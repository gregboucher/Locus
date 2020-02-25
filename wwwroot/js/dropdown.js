function initDropdown(callback) {

    var dropdownWrapper = $('.dropdown__wrapper')
    var dropdown = dropdownWrapper.find('#dropdown');
    var dropdownContent = $('.dropdown__content');
    var dropdownInput = dropdownContent.find('.dropdown__search');
    var dropdownOptions = dropdownContent.find('.dropdown__option');
    dropdownContent.hide();

    //close dropdown on click anywhere other than itself, it's input field, or content scrollbar.
    $(document).click(e => {
        //console.log(e.target);
        var target = $(e.target);
        if (target.is(':not([class^=dropdown]), .dropdown__option')) {
            dropdownContent.hide();
            dropdownWrapper.removeClass('is-open')
        } else {
            if (!dropdownWrapper.hasClass('is-open')) {
                dropdownWrapper.addClass('is-open');
                dropdownContent.show();
                dropdownInput.val('');
                dropdownInput.trigger('keyup');
                dropdownInput.focus();
            }
        }
    });

    //input search field
    dropdownInput.keyup(function(e){
        var txt = $(this).val().toLowerCase();
        dropdownOptions.each(function() {
            var $this = $(this);
            $this.toggle($this.text().toLowerCase().indexOf(txt) > -1);
        });
    });

    //click selection of collection
    dropdownOptions.click(function() {
        dropdownOptions.removeClass('is-active');
        var $this = $(this);
        $this.addClass('is-active');
        dropdown.text($this.contents().get(0).nodeValue);
        var collection = $this.data('search');
        //callback function from caller defines context based behaviour.
        callback(collection);
    });

    dropdownWrapper.css('visibility', 'visible');

    //return reset function
    return function() {
        dropdownOptions.removeClass('is-active');
        var defaultOption = dropdownOptions.siblings('[data-search=""]');
        defaultOption.addClass('is-active');
        dropdown.text(defaultOption.contents().get(0).nodeValue);
    }
}
