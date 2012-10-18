(function ($) {
    var defaults = {};
    $.fn.enusappjs1 = function (options) {
        var settings = $.extend(true, {}, defaults, options);
        Foo.Alert(settings);
        return this.each(function () {
            alert(settings);
        })
    };
    $.fn.enusappjs1.defaults = defaults
})(jQuery);