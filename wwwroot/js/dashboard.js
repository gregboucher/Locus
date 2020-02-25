$(function() {
    var dataTable = $('#data-table').DataTable({
        aoColumnDefs: [{
            bSortable: false,
            bSearchable: false,
            aTargets: [6, 7]
        }],
        pageLength: 1,
        sDom: 'lrtip'
    });

    var overviewCards = $('.overview__card');
    var searchInput = $('#top-bar__input');
    searchInput.val('');

    //top-bar input search
    searchInput.keyup(function() {
        dataTable.search($(this).val()).draw();
    })

    //prevent table row count from overflowing parent container
    var rowHeight = $('#data-table tbody tr').eq(0).height();
    var tableContainer = $('.data');
    tableContainer.css('flex-grow', 1);
    var maxRows = 1;
    if (rowHeight > 0) {
        maxRows = Math.floor((tableContainer.height() - ($('.data-table__wrapper').height() - rowHeight)) / rowHeight);
        if (maxRows <= 0) {
            maxRows = 1;
        }
    }
    tableContainer.css('flex-grow', 0);
    dataTable.page.len(maxRows).draw();

    //define callback function for behaviour when dropdown collection is selected, and initialize dropdown.
    var dropdownCallback = function (collection) {
        overviewCards.removeClass('is-active');
        $.fn.dataTable.ext.search.pop();
        dataTable.column(1).search(collection).draw();
        changePlaceholder(collection);
    }
    var resetDropdown = initDropdown(dropdownCallback);

    overviewCards.click(function() {
        var $this = $(this);
        if (!$this.hasClass('is-active')) {
            resetDropdown();
            $.fn.dataTable.ext.search.pop();
            dataTable.column(1).search('');
            overviewCards.removeClass('is-active');
            $this.addClass('is-active');
            var cardIndex = overviewCards.index($this);
            switch(cardIndex) {
                case 0:
                    filterRowByAssetClass('icon--due');
                    changePlaceholder('Assets Due Today');
                break
                case 1:
                    filterRowByAssetClass('icon--overdue');
                    changePlaceholder('Assets Overdue');
                break
                case 2:
                    filterRowByAssetClass('today');
                    changePlaceholder('Users Created Today');
                break
                case 3:

                break
            }
        } else {
            overviewCards.removeClass('is-active');
            $.fn.dataTable.ext.search.pop();
            dataTable.column(1).search('').draw();
            changePlaceholder('');
        }
    });

    function changePlaceholder(collection) {
        if (collection.length != 0) {
            searchInput.attr('placeholder', 'Search within subset: ' + collection);
        } else {
            searchInput.attr('placeholder', 'Search All Assignments');
        }
    }

    function filterRowByAssetClass(text) {
        $.fn.dataTable.ext.search.push(
            function(settings, data, dataIndex) {
                var quantity = $(dataTable.row(dataIndex).node()).find('.data-table__' + text).length;
                if (quantity > 0) {
                    return true;
                } else {return false}
            }
        );
        dataTable.draw();
    }

    tableContainer.css('visibility', 'visible');
});
