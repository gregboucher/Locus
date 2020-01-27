$(function() {
    $.validator.setDefaults({
        ignore: '',
        errorClass: 'user__error-text',
        highlight: function(element) {
            $(element)
                .addClass('user-details__input--error');
        },
        unhighlight: function(element) {
            $(element)
                .removeClass('user-details__input--error');
        },
        errorPlacement: function(error, element) {
            if (element.is('select')) {
                error.insertAfter(element.parent());
            } else {
                error.insertAfter(element);
            }
        }
    });

    $.validator.addMethod("letterspaceonly", function(value, element) {
        return this.optional(element) || value == value.match(/^[a-zA-Z\s]+$/);
    });

    $.validator.addMethod("checked", function(value, element) {
        return $('.user-assets__input.is-assign:checked').length > 0;
    });

    $('#user').validate({
        rules: {
            name: {
                required: true,
                letterspaceonly: true,
                maxlength: 64,
            },
            email: {
                email: true,
                maxlength: 128
            },
            phone: {
                number: true,
                maxlength: 10
            },
            absentee: {
                letterspaceonly: true,
                maxlength: 64
            },
            role: {
                required: true
            },
            comments: {
                maxlength: 256
            },
            checkboxAssigned: {
                checked: true
            }
        },
        messages: {
            name: {
                required: "Please enter a name",
                letterspaceonly: "A name must contain only letters",
            },
            phone: {
                number: "A phone number must contain only digits"
            },
            absentee: {
                letterspaceonly: "A name must contain only letters"
            },
            role: {
                required: "Please select a role",
            },
            checkboxAssigned: {
                checked: "Please assign at least one device"
            }
        },
        submitHandler: function(form) {
            $('#checkboxAssigned').prop('disabled', true);
            form.submit();
        }
    });

    var searchInput = $('#top-bar__input');
    searchInput.val('');

    //top-bar input search
    searchInput.keyup(function() {
        searchAssets(searchInput.val());
    })

    var cards = $('.user-assets__card-wrapper:not(.is-blank)');
    var checkboxReturnAll = $("#user-assets__header-check");

    cards.click(function() {
        var $this = $(this);
        var checkedInput = $this.find('.user-assets__input:checked');
        var hiddenInputs = $this.find('.user-assets__hidden');
        if (checkedInput.length != 0) {
            var nextInput = checkedInput.next('.user-assets__input');
            if (nextInput.length != 0) {
                nextInput.prop('checked', true);
                checkboxReturnAll.prop('checked', false);
            } else {
                checkedInput.prop('checked', false);
                hiddenInputs.prop('disabled', true);
            }
        } else {
            $this.find('.user-assets__input:first').prop('checked', true);
            hiddenInputs.prop('disabled', false);
        }

    });

    //define callback function for search behaviour when dropdown group is selected,
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
    var resetDropdown = initDropdown(searchAssets);

    //return all checkbox
    checkboxReturnAll.click(function(){
        var assigned = cards.filter('.is-assigned');
        if ($(this).prop('checked')) {
            assigned.find('.is-return').prop('checked', true);
            assigned.find('.user-assets__hidden').prop('disabled', false);
        } else {
            assigned.find('.user-assets__input').prop('checked', false);
            assigned.find('.user-assets__hidden').prop('disabled', true);
        }
    });

});
