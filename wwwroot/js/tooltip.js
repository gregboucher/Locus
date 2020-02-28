function ShowTooltip($this, delay, relative, offsetX, offsetY) {
    var tooltip = $this.next();
    var relativeY = 0;
    var relativeX = 0;
    // offset with respect to center if true
    // else with respect to top-left
    if (relative == true) {
        relativeY = tooltip.innerHeight() / 2.0;
        relativeX = tooltip.innerWidth() / 2.0;
    }
    $this.mousemove(function (e) {
        var top = e.pageY - relativeY + offsetY;
        var left = e.pageX - relativeX + offsetX;
        tooltip.css({ top: top, left: left});
    });
    timeout = setTimeout(function () {
        tooltip.fadeIn(100);
    }, delay);
};

function HideTooltip($this) {
    var tooltip = $this.next();
    clearTimeout(timeout);
    tooltip.fadeOut(100);
};