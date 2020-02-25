$(function () {
    $('.accordion').click(function () {
        var $this = $(this);
        $this.toggleClass('is-open')
        $this.next().slideToggle();
    });
});