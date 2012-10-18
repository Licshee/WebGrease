(function ($)
{
    var defaults = {};
    $.fn.f1js2 = function (options)
    {
        var settings = $.extend(true, {}, defaults, options);
        return this.each(function ()
        {
            alert(settings);
        })
    };
    $.fn.f1js2.defaults = defaults
})(jQuery);