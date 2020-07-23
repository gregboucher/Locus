$(function () {
    $.validator.setDefaults({
        ignore: '',
        errorClass: 'form__validation',
        highlight: function (element) {
            $(element)
                .addClass('form__input--error');
        },
        unhighlight: function (element) {
            $(element)
                .removeClass('form__input--error');
        },
        errorPlacement: function (error, element) {
            if (element.is('select')) {
                error.insertAfter(element.parent());
            } else {
                error.insertAfter(element);
            }
        }
    });

    $.validator.addMethod("letterspaceonly", function (value, element) {
        return this.optional(element) || value == value.match(/^[a-zA-Z\s]+$/);
    });

    $.validator.addMethod("checked", function () {
        return $('.user .card__input.is-assign:checked, .user .card__input.is-long_term:checked').length > 0;
    });

    $('#user').validate({
        rules: {
            "UserDetails.Name": {
                required: true,
                letterspaceonly: true,
                maxlength: 64,
            },
            "UserDetails.Email": {
                email: true,
                maxlength: 256
            },
            "UserDetails.Phone": {
                number: true,
                maxlength: 16
            },
            "UserDetails.Absentee": {
                letterspaceonly: true,
                maxlength: 64
            },
            "UserDetails.RoleId": {
                required: true
            },
            "UserDetails.Comment": {
                maxlength: 256
            },
            checkboxAssigned: {
                checked: true
            }
        },
        messages: {
            "UserDetails.Name": {
                required: "Please enter a name",
                letterspaceonly: "A name must contain only letters",
            },
            "UserDetails.Phone": {
                number: "A phone number must contain only digits"
            },
            "UserDetails.Absentee": {
                letterspaceonly: "A name must contain only letters"
            },
            "UserDetails.RoleId": {
                required: "Please select a role",
            },
            checkboxAssigned: {
                checked: "Please assign at least one device"
            }
        },
        submitHandler: function (form) {
            $('#checkboxAssigned').prop('disabled', true);
            form.submit();
        }
    });

    var searchInput = $('#top-bar__field');
    searchInput.val('');

    //top-bar input search
    searchInput.keyup(function () {
        searchAssets(searchInput.val());
    })

    var cards = $('.user .card__wrapper:not(.is-blank)');
    var checkboxReturnAll = $('.user #assets__header-check');

    cards.click(function () {
        var $this = $(this);
        var checkedInput = $this.find('.card__input:checked');
        var hiddenInputs = $this.find('.card__hidden');
        if (checkedInput.length != 0) {
            var nextInput = checkedInput.next('.card__input');
            if (nextInput.length != 0) {
                nextInput.prop('checked', true);
                checkboxReturnAll.prop('checked', false);
            } else {
                checkedInput.prop('checked', false);
                hiddenInputs.prop('disabled', true);
            }
        } else {
            $this.find('.card__input:first').prop('checked', true);
            hiddenInputs.prop('disabled', false);
        }
    });

    var cardsWithCustomPeriod = cards.filter('.is-customPeriod')
    var allPeriodMenues = $('.period-menu');

    //disable context menu on all cards
    cards.contextmenu(function (e) {
        e.preventDefault();
    })

    //custome context menu on cards with is-customePeriod class
    cardsWithCustomPeriod.contextmenu(function (e) {
        var menu = $(e.currentTarget).prev('.period-menu')
        var firstHidden = menu.next('.is-customPeriod').children('.card__hidden').first();
        //if (!firstHidden.prop('disabled')) {
            menu.css({ top: e.pageY, left: e.pageX });
            menu.show();
        //}
    })

    allPeriodMenues.children('li').click(function (e) {
        e.stopPropagation();
        var listItem = $(this);
        var allListItems = listItem.siblings('li');
        var menu = listItem.parent();
        var cardContent = menu.next('.is-customPeriod').children();
        allListItems.removeClass('is-selected');
        listItem.addClass('is-selected');
        cardContent.filter('.card__hidden--period').val(listItem.attr('data-period'));
        cardContent.find('.card__value--period').text(listItem.text());
        menu.hide();
    });

    // close period menu if clicking outside menu
    $(document).mousedown(function (e) {
        if (!$(e.target).parents(".period-menu").length > 0) {
            allPeriodMenues.hide();
        }
    });

    // if create action, clear input fields and remove selections.
    // if edit just remove selections
    $('#reset').click(function () {
        if ($(this).data('page') == null)
            $('.user .form__input').val("");
        cards.find('.card__input').prop('checked', false);
        cards.find('.card__hidden').prop('disabled', true);
        checkboxReturnAll.prop('checked', false);
    });

    //define callback function for search behaviour when dropdown collection is selected,
    //or input field is changed, and then initialize dropdown.
    var searchAssets = function(phrase) {
        cards.each(function() {
            phrase = phrase.toLowerCase();
            var $this = $(this);
            var show = false;
            if (phrase == '') {
                show = true;
            } else if (phrase == 'is-assigned') {
                if ($this.hasClass('is-assigned')) {
                    show = true;
                }
            } else {
                var searchable = $this.find('.searchable');
                searchable.each(function() {
                    if ($(this).contents().get(0).nodeValue.toLowerCase().indexOf(phrase) > -1) {
                        show = true;
                        return false; //break
                    }
                });
            }
            show ? $this.show() : $this.hide();
        });
    }
    initDropdown(searchAssets);

    //set default dropdown selection
    searchAssets($('.user .dropdown__option.is-active').data('search'));

    //return all checkbox
    checkboxReturnAll.click(function(){
        var assigned = cards.filter('.is-assigned');
        if ($(this).prop('checked')) {
            assigned.find('.is-return').prop('checked', true);
            assigned.find('.card__hidden').prop('disabled', false);
        } else {
            assigned.find('.card__input').prop('checked', false);
            assigned.find('.card__hidden').prop('disabled', true);
        }
    });

    $('.user .assets__body').css('visibility', 'visible');
});
