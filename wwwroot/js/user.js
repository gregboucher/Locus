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
        return $('.user .assets :radio.is-assign:checked').length > 0;
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
        var card = $(this);
        var checkedRadio = card.children(':radio:checked');
        var hiddenInputs = card.children(':hidden');
        if (checkedRadio.length > 0) {
            var nextCheckedRadio = checkedRadio.nextAll(':radio:first');
            if (nextCheckedRadio.length > 0) {
                nextCheckedRadio.prop('checked', true);
                checkboxReturnAll.prop('checked', false);
            } else {
                checkedRadio.prop('checked', false);
                hiddenInputs.prop('disabled', true);
            }
        } else {
            card.children(':radio:first').prop('checked', true);
            hiddenInputs.prop('disabled', false);
        }
    });

    var allPeriodMenues = $('.period-menu');

    //custom context menu for period selection
    cards.contextmenu(function (e) {
        var card = $(this);
        e.preventDefault();
        var periodMenu = card.prev('.period-menu')
        if (periodMenu.length > 0) {
            periodMenu.css({ top: e.pageY, left: e.pageX });
            periodMenu.stop().fadeIn(100);
        } else {
            if (!card.hasClass('is-empty')) {
                card.find('.card__properties i').stop().fadeOut(250).fadeIn(250);
            }
        }
    })

    allPeriodMenues.children('li').click(function (e) {
        e.stopPropagation();
        var selectedListItem = $(this);
        var allListItems = selectedListItem.siblings('li');
        var periodMenu = selectedListItem.parent();
        var cardChildren = periodMenu.nextAll('.card__wrapper:first').children();
        allListItems.removeClass('is-selected');
        selectedListItem.addClass('is-selected');
        var inputPeriod = cardChildren.filter('.input__period');
        inputPeriod.val(selectedListItem.attr('data-period'));
        var cardPeriod = cardChildren.find('.card__period');
        cardPeriod.text(selectedListItem.text());
        if (selectedListItem.hasClass('is-default')) {
            cardPeriod.css('color', '#6c707c');
        } else {
            cardPeriod.css('color', '#c4c9d7');
        }
        periodMenu.stop().fadeOut(100);
        cardChildren.filter('.is-assign').prop('checked', true);
        cardChildren.filter('.is-extend').prop('checked', true);
        cardChildren.filter(':hidden').prop('disabled', false);
    });

    // close period menu if clicking outside menu
    $(document).mousedown(function (e) {
        if (!$(e.target).parents(".period-menu").length > 0) {
            allPeriodMenues.stop().fadeOut(100);
        }
    });

    // if create action, clear input fields and remove selections.
    // if edit just remove selections
    $('#reset').click(function () {
        if ($(this).data('page') == null)
            $('.user .form__input').val("");
        cards.children(':radio:checked').prop('checked', false);
        cards.children(':hidden').prop('disabled', true);
        checkboxReturnAll.prop('checked', false);

        allPeriodMenues.each(function () {
            var periodMenu = $(this);
            var allListItems = periodMenu.children('li');
            allListItems.removeClass('is-selected');
            var defaultListItem = allListItems.filter('.is-default')
            defaultListItem.addClass('is-selected');
            var cardChildren = periodMenu.nextAll('.card__wrapper:first').children();
            cardChildren.filter('.input__period').val(defaultListItem.attr('data-period'));
            var cardPeriod = cardChildren.find('.card__period')
            cardPeriod.text(defaultListItem.text());
            cardPeriod.css('color', '#6c707c');
        });
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
            assigned.children('.is-return').prop('checked', true);
            assigned.children(':hidden').prop('disabled', false);
        } else {
            assigned.children(':radio:checked').prop('checked', false);
            assigned.children(':hidden').prop('disabled', true);
        }
    });

    $('.user .assets__body').css('visibility', 'visible');
});
