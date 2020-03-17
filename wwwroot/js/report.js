$(function () {
    $('#button-print').click(function () {
        $('#print').printThis({
            loadCSS: ["/css/print.css", "/css/locus-font.css"]
        });
    });
});