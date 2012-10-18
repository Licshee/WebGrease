(function ($)
{
    var defaults =
    {
        key1: "%jsKey1%",
        key2: "%jsKey2%",
        key3: "%jsKey3%",
        key4: "%jsKey4%",
        key5: "%jsKey5%",
        key6: "%jsKey6%",
        key7: "%jsKey7%",
        key8: "%jsKey8%",
        key9: "%jsKey9%",
        key10: "%jsKey10%",
        key11: "%jsIntl%"
    };
    $.fn.f1js1 = function (options)
    {
        var settings = $.extend(true, {}, defaults, options);
        return this.each(function ()
        {
            alert(settings);
        })
    };
    $.fn.f1js1.defaults = defaults
})(jQuery);