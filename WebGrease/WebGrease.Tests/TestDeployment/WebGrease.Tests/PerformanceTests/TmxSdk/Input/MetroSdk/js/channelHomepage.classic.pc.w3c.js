
/* C:\Projects\Workspace2\MSNMetro\Main\MetroSDK\MetroSDK\Content\Source\js\Classic\base.pre.js */

/* WGINCLUDE: C:\PROJECTS\WORKSPACE2\MSNMETRO\MAIN\METROSDK\METROSDK\CONTENT\SOURCE\JS\CLASSIC\..\Shared\viewState.js */
/////////////////////////////////////////////////////////////////////////////////
//
// File: viewState.js
// Defines: viewState
// Dependencies: viewAware
// Description: Allows for view state aware bindings.
//
//              // create a binding in all states
//              viewState.bind("myBehavior", "selector", context)
//                  .fallback(myOptions);
//
//              // create a snap only binding
//              viewState.bind("myBehavior", "selector", context)
//                  .mode(viewState.modes.SNAP, myOptions);
//
//              // create non-snap only binding
//              viewState.bind("myBehavior", "selector", context)
//                  .mode(viewState.modes.FULL | viewState.modes.FILL, myOptions);
//
//              // create binding always, but differnt options for snap
//              viewState.bind("myBehavior", "selector", context)
//                  .mode(viewState.modes.SNAP, mySnapOptions)
//                  .fallback(myNonSnapOptions);
//
// Copyright (c) 2012 by Microsoft Corporation. All rights reserved.
//
/////////////////////////////////////////////////////////////////////////////////

define("viewState", ["viewAware"], function (viewAware)
{
    // list of all bindings
    var bindings = [];

    // remember current mode
    var mode;

    // listen for mode to change
    // this will also initialize mode
    viewAware.listen(changeMode);

    // bindings object created with viewState.bind(...)
    function AdaptiveBinding(behavior, selector, context)
    {
        this.behavior = behavior;
        this.selector = selector;
        this.context = context;
        this.modes = viewAware.modes.NONE;
    }

    AdaptiveBinding.prototype = {
        // apply the binding in context with the give mode(s)
        // options are passed when applying the binding
        mode: function (modeValue, options)
        {
            var args = [this.selector, this.context, options];
            this.modes |= modeValue;
            require([this.behavior], function (behavior)
            {
                var bindingObject = { mode: modeValue, behavior: behavior, args: args, active: 0 };

                if (modeValue & mode)
                {
                    callSetup(bindingObject);
                }

                bindings.push(bindingObject);
            });

            // allow chaining
            return this;
        },
        // apply the binding in context with any modes that have not been used yet
        // options are passed when applying the binding
        fallback: function (options)
        {
            // bind to all modes not used so far
            this.mode(viewAware.modes.ALL & ~this.modes, options);

            // allow chaining
            return this;
        }
    };

    return {
        // create a binding object
        // does not automatically apply the binding
        // use .mode and/or .fallback on the returned value
        bind: function (behavior, selector, context)
        {
            return new AdaptiveBinding(behavior, selector, context);
        },
        // bit masks
        modes: viewAware.modes
    };

    // mode has changed, apply, teardown, or update bindings accordingly
    function changeMode(newMode)
    {
        var binding;
        var hasMode;
        mode = newMode;
        var ndx;
        // do all teardown and updates first
        for (ndx = 0; ndx < bindings.length; ndx++)
        {
            binding = bindings[ndx];
            // will be 0 if binding.mode doesn't contain the current mode
            hasMode = binding.mode & mode;
            if (binding.active)
            {
                // this binding was already setup
                if (hasMode)
                {
                    // tell it that viewState has changed
                    binding.update();
                }
                else
                {
                    // remove binding
                    binding.teardown();
                    binding.active = 0;
                }
            }
        }
        // then do setup
        // with a single loop a setup could occur
        // and then a teardown on the same element
        for (ndx = 0; ndx < bindings.length; ndx++)
        {
            binding = bindings[ndx];
            hasMode = binding.mode & mode;
            if (hasMode && !binding.active)
            {
                // apply binding
                callSetup(binding);
            }
        }
    }

    // ensure binding is applied if needed
    // always call setup
    function callSetup(binding)
    {
        if (!binding.setup)
        {
            var result = binding.behavior.apply(null, binding.args);
            binding.setup = result.setup;
            binding.teardown = result.teardown;
            binding.update = result.update;
        }
        binding.setup();
        binding.active = 1;
    }
});

/* WGINCLUDE: C:\PROJECTS\WORKSPACE2\MSNMETRO\MAIN\METROSDK\METROSDK\CONTENT\SOURCE\JS\CLASSIC\..\Shared\AMD\jqfn.js */
/////////////////////////////////////////////////////////////////////////////////
//
// File: jqfn.js
// Defines: jqfn
// Dependencies: AMD, jQuery
// Description: Implements a wrapper function behavior that operates on a
//              jQuery wrapped set. Rather than continue to define $.fn.myBehavior
//              we can define a function that will do what $.fn.myBehavior would usually do.
//              This paradigm change is due to the module nature of our define function and
//              being able to pass functions directly to other modules that care about them.
//
//              Instead of
//
//              (function($)
//              {
//                var defaults = { ... };
//                $.fn.myBehavior = function(options)
//                {
//                  this.each(applyBehavior);
//                  function applyBehavior($elem)
//                  {
//                    var settings = $.extend({}, defaults, options, $elem.data());
//                    ...
//                  }
//                }
//              })(jQuery);
//
//              Do this
//
//              define("mybehavior", ["jQuery", "jqfn"], function($, jqfn)
//              {
//                var defaults = { ... };
//                function applyBehavior($elem, settings)
//                {
//                  ...
//                }
//                return jqfn(applyBehavior, defaults);
//              });
//
//              Additionally for customization reasons you can pass a third parameter into jqfn
//              to exclude common functionality
//              {data:1}  Will cause $elem.data() to not be extended to settings.
//                        This can help run time performance when not required for the functionality.
//              {each:1}  Will cause $elem to be the whole wrapped set.
//                        This is useful if you want to operate on the whole set before applying the behavior.
//              {query:1} Will cause no wrapped set to be generated at all; instead, the selector will
//                        we passed in place of a wrapped set. This is useful if you want to operate on the 
//                        selector to create a single bubbled handler that will work not just on matching elements
//                        currently in the document, but also any that may be created in the future.
//
// Copyright (c) 2012 by Microsoft Corporation. All rights reserved.
//
/////////////////////////////////////////////////////////////////////////////////

define("jqfn", ["jquery"], function ($)
{
    // define "jqfn" to wrap a behavior with a selector call
    // jqfn(myfunction) -> function(selector, context, options)
    // calling this function will use jQuery to grab a wrapped set and pass it, along with the options, to myfunction
    return function (applyBehavior, defaults, exclude)
    {
        exclude = exclude || {};
        // new function with internal jQuery selector call
        return function (selector, context, options)
        {
            var settings = $.extend({}, defaults, options);
            var setupList = [];
            var teardownList = [];
            var updateList = [];

            if (exclude.query)
            {
                // don't query for the set; instead, pass the selector and the settings in
                captureResults(applyBehavior(selector, settings));
            }
            else
            {
                // query for the set now
                var $set = $(selector, context);

                // apply the behavior with options
                // options is guarenteed to be defined in the applyBehavior call
                if (exclude.each)
                {
                    captureResults(applyBehavior($set, settings));
                } else
                {
                    $set.each(function ()
                    {
                        var $this = $(this);
                        captureResults(applyBehavior($this, (exclude.data ? settings : $.extend({}, settings, $this.data()))));
                    });
                }
            }

            return {
                setup: constructFunction(setupList),
                teardown: constructFunction(teardownList),
                update: constructFunction(updateList)
            };

            function captureResults(results)
            {
                if (results)
                {
                    var hasSetup = typeof results.setup == "function";
                    var hasTeardown = typeof results.teardown == "function";
                    if (hasSetup)
                    {
                        setupList.push(results.setup);
                    }
                    if (hasTeardown)
                    {
                        teardownList.push(results.teardown);
                    }
                    if (typeof results.update == "function")
                    {
                        updateList.push(results.update);
                    }
                    else
                    {
                        if (hasTeardown)
                        {
                            updateList.push(results.teardown);
                        }
                        if (hasSetup)
                        {
                            updateList.push(results.setup);
                        }
                    }
                }
            }

            function constructFunction(list)
            {
                var length = list.length;
                if (list.length > 1)
                {
                    // multiple in list
                    return function ()
                    {
                        for (var ndx = 0; ndx < length; ndx++)
                        {
                            list[ndx]();
                        }
                    };
                }
                if (length)
                {
                    // only 1 in list
                    return list[0];
                }
                // empty list
                return noop;
            }

            function noop() { };
        };
    };
});

/* WGINCLUDE: C:\PROJECTS\WORKSPACE2\MSNMETRO\MAIN\METROSDK\METROSDK\CONTENT\SOURCE\JS\CLASSIC\..\Shared\AMD\jqbind.js */
/////////////////////////////////////////////////////////////////////////////////
//
// File: jqbind.js
// Defines: jqbind
// Dependencies: AMD, jQuery
// Description: jqbind is used instead of direct jQuery bindings.
//
//              Instead of
//
//              $("selector", context).myBehavior({myOptions})
//
//              Do this
//
//              jqbind("myBehavior", "selector", context, myOptions);
//
//              jqbind should be used with jqfn. Using jqbind means that you do
//              not need to add all the "myBehavior"s that you are binding to the
//              requires array. jqbind will handle the require internally.
//
//              Note that using jqbind will not guarantee that the binding is availble,
//              so order may not be the same as the code.
//
// Copyright (c) 2012 by Microsoft Corporation. All rights reserved.
//
/////////////////////////////////////////////////////////////////////////////////

define("jqbind", function () {
    return function (actionString, selector, context, options) {
        require([actionString], function (action) {
            action(selector, context, options).setup();
        });
    };
});

/* WGINCLUDE: C:\PROJECTS\WORKSPACE2\MSNMETRO\MAIN\METROSDK\METROSDK\CONTENT\SOURCE\JS\CLASSIC\..\Shared\Tracking\afire.js */
/////////////////////////////////////////////////////////////////////////////////
//
// File: afire.js
// Defines: "afire"
// Dependencies: jsloader.js
// Description: Defines a function as "afire" send a beacon.
//              Used to be called fireAndForget in previous code bases.
//
// Copyright (c) 2012 by Microsoft Corporation. All rights reserved.
// 
/////////////////////////////////////////////////////////////////////////////////

define("afire", ["image"], function afireFunction(ImageObject)
{

  return function (url)
  {
    ///	<summary>
    ///		A function that fire and forget an image call.
    ///	</summary>
    ///	<param name="url" type="string">
    ///     An image url that will be downloaded.
    ///	</param>

    if (url)
    {
      var img = new ImageObject();
      img.onload = img.onerror = function ()
      {
        img.onload = img.onerror = null;
      };
      img.src = url.replace(/&amp;/gi, "&");
    }
  }
});

/* WGINCLUDE: C:\PROJECTS\WORKSPACE2\MSNMETRO\MAIN\METROSDK\METROSDK\CONTENT\SOURCE\JS\CLASSIC\..\Shared\Tracking\cookies.js */
/////////////////////////////////////////////////////////////////////////////////
//
// File: cookies.js
// Defines: "getCookie", "setCookie"
// Dependencies: location
// Description: Adds "getCookie" and "setCookie" as function to get and set cookies.
//              May be refactored in the future to define "cookie" with get and set functions
//
// Copyright (c) 2012 by Microsoft Corporation. All rights reserved.
// 
/////////////////////////////////////////////////////////////////////////////////

define("getCookie", function ()
{
  return function (name)
  {
    ///	<summary>
    ///		Helper function used to get cookie value
    ///	</summary>
    ///	<returns type="String">
    ///     Cookie value
    /// </returns>

    var re = new RegExp('\\b' + name + '\\s*=\\s*([^;]*)', 'i');
    var match = re.exec(document.cookie);
    return (match && match.length > 1 ? match[1] : '');
  };
});

define("setCookie", ["location"], function (location)
{
  function setCookie(name, value, expiryDays, domain, path, secure)
  {
    ///	<summary>
    ///		Helper function used to set a cookie
    ///	</summary>
    ///	<returns type="nothing">
    /// </returns>
    var expiryDate;
    var builder = [name, "=", value];
    if (-1 == expiryDays)
    {
      // Expires date format is supposed to be in GMT.
      expiryDate = "Fri, 31 Dec 1999 23:59:59 GMT"
    }
    else if(expiryDays)
    {
      var date = new Date();
      date.setTime(date.getTime() + (expiryDays * 86400000)); // 86400000 == 24*60*60*1000 (ms/day)
      expiryDate = date.toUTCString();
    }
        
    if (expiryDate) { builder.push(";expires=", expiryDate); }
    if (domain) { builder.push(";domain=", domain); }
    if (path) { builder.push(";path=", path); }
    if (secure) { builder.push(";secure"); }

    document.cookie = builder.join("");
  };

  // set the topDomain property to be the "anything.letters" end part of the current page
  // domain, or a blank string if it doesn't fit that pattern (intranet or IP address).
  // also, that top-level domain needs to be only alphbetic characters ("com", "uk", "etc")
  // which should be true for all MSN sites.
  setCookie.topDomain = (location.hostname.match(/[^.]+\.[^.\d]+$/) || {})[0] || "";

  return setCookie;
});

/* WGINCLUDE: C:\PROJECTS\WORKSPACE2\MSNMETRO\MAIN\METROSDK\METROSDK\CONTENT\SOURCE\JS\CLASSIC\..\Shared\Tracking\dom.js */
/////////////////////////////////////////////////////////////////////////////////
//
// File: dom.js
// Defines: "dom"
// Dependencies: jsloader.js
// Description: Adds basic dom manipulation functions to an object defined as "dom"
//
// Copyright (c) 2012 by Microsoft Corporation. All rights reserved.
//
/////////////////////////////////////////////////////////////////////////////////

define("dom", function ()
{
  var doc = document;
  var otherWhitespaceRegex = /[\n\t]/g;
  var trimRegex = /(^\s+)|(\s+$)/g;

  // curly brace needs to be on the return line otherwise build with throw an error
  return {
    attr: function (elem, attr)
    {
      // getAttribute(attr, 2) causes IE to only report correct values for href.
      // IE reports href values for IMG tags without an href property
      // see http://reference.sitepoint.com/javascript/Element/getAttribute
      // see bug #1131443
      return elem && (elem.getAttribute ? elem.getAttribute(attr, 2) : elem[attr]) || "";
    },
    name: function (elem)
    {
      return elem && elem.nodeName || "";
    },
    text: function (elem)
    {
        return (elem && (elem.textContent || elem.innerText) || "").replace(trimRegex, "");
    },
    children: function (elem)
    {
      return elem && elem.children || [];
    },
    parent: function (elem)
    {
      return elem && elem.parentNode;
    },
    getElementsByTagName: function (name)
    {
      return doc.getElementsByTagName(name);
    },
    create: function (name)
    {
      return doc.createElement(name);
    },
    containsClass: function (element, className)
    {
      /// <summary>
      /// Looks for the className in element and returns true/false based on result.
      /// <parameter>
      /// element - Element where className to be searched for.
      /// className - Class name to be looked for.
      /// </parameter>
      /// </summary>
      return element && ((" " + (element.className || element.getAttribute("class")) + " ")
             .replace(otherWhitespaceRegex, " ").indexOf(" " + className + " ") > -1);
    },
    getTarget: function (event)
    {
      return event && (event.customTarget || event.target || event.srcElement) || document;
    }
  };
});

/* WGINCLUDE: C:\PROJECTS\WORKSPACE2\MSNMETRO\MAIN\METROSDK\METROSDK\CONTENT\SOURCE\JS\CLASSIC\..\Shared\Tracking\events.js */
/////////////////////////////////////////////////////////////////////////////////
//
// File: events.js
// Defines: "bind", "unbind"
// Dependencies: jsloader.js
// Description: Adds functions defined as "bind" and "unbind" that bind and unbind events
//
// Copyright (c) 2012 by Microsoft Corporation. All rights reserved.
// 
/////////////////////////////////////////////////////////////////////////////////

define("bind", function ()
{
    ///#IFDEF CROSSBROWSER
    // want to start indexing at 1 so that converted boolean value will be true (0 would give false)
    var uniqueId = 1;
    // could be just = [] but I thought there might be some optimization when an array has indexes
    // set from 0 to n. Not entirely sure, but only 1 byte seems like a small price to pay.
    var handlerList = [0];
    // shortcut to save bytes
    var handlerIdStr = "handlerId";
    ///#ENDIF

    function bindEvent(element, type, handler)
    {
        ///#IFDEF CROSSBROWSER
        // should check for addEventListener first since it is the standard
        // however IE9 defines addEventListener but it doesn't seem to work correctly
        // so we'll check for the IE only attachEvent method first
        if (element.attachEvent)
        {
            // wrap handler to fix IE compatibility issues.
            // use an index to avoid circular references that lead to memory leaks.
            if (!handler[handlerIdStr])
            {
                handler[handlerIdStr] = uniqueId;
                handlerList[uniqueId++] = function()
                {
                    var event = window.event;
                    event.customTarget = event.target || event.srcElement || document;
                    try
                    {
                        event.target = event.customTarget;
                    }
                    catch(e)
                    {
                    }

                    handler.call(element, event);
                };
            }
            // attach an event in IE
            element.attachEvent('on' + type, handlerList[handler[handlerIdStr]]);
        }
        else
        ///#ENDIF
        if (element.addEventListener)
        {
            // attach an event in non-IE
            element.addEventListener(type, handler, false);
        }
    }

    // define unbind here since it needs information from the same scope as bind
    define("unbind", function ()
    {
        return function(element, type, handler)
        {
            ///#IFDEF CROSSBROWSER
            if (element.detachEvent)
            {
                // detach an event in IE
                element.detachEvent('on' + type, handlerList[handler[handlerIdStr]]);
            }
            else
            ///#ENDIF
            if (element.removeEventListener)
            {
                // detach an event in non-IE
                element.removeEventListener(type, handler, false);
            }
        };
    });

    // return the function to define as bind
    return bindEvent;
});

/* WGINCLUDE: C:\PROJECTS\WORKSPACE2\MSNMETRO\MAIN\METROSDK\METROSDK\CONTENT\SOURCE\JS\CLASSIC\..\Shared\Tracking\extend.js */
/////////////////////////////////////////////////////////////////////////////////
//
// File: extend.js
// Defines: "extend"
// Dependencies: jsloader.js
// Description: Creates a function defined as "extend" that functions similiar to jQuery's extend function.
//
// Copyright (c) 2012 by Microsoft Corporation. All rights reserved.
//
/////////////////////////////////////////////////////////////////////////////////

define("extend", function ()
{
  function extend()
  {
    var args = arguments;
    var target = args[0] || {};
    var ndx = 1;
    var key;
    var obj;
    var isRecursive;
    var src;

    if (typeof target == "boolean" || typeof target == "number")
    {
      isRecursive = !!target;
      target = args[1];
      ndx = 2;
    }
    for (; ndx < args.length; ndx++)
    {
      obj = args[ndx];
      for (key in obj)
      {
        if (obj[key] !== undefined)
        {
          if (isRecursive && typeof obj[key] == "object")
          {
            src = target[key];
            if (typeof src != "object")
            {
              src = {};
            }
            extend(true, src, obj[key]);
            target[key] = src;
          }
          else
          {
            target[key] = obj[key];
          }
        }
      }
    }
    return target;
  };

  return extend;
});


/* WGINCLUDE: C:\PROJECTS\WORKSPACE2\MSNMETRO\MAIN\METROSDK\METROSDK\CONTENT\SOURCE\JS\CLASSIC\..\Shared\Tracking\format.js */
/////////////////////////////////////////////////////////////////////////////////
//
// File: format.js
// Defines: format, String.prototype.format
// Dependencies: none
// Description: Extends String to have a format function.
//              Replacing {0} with argument[0], {1} with argument[1], etc.
//
// Copyright (c) 2013 by Microsoft Corporation. All rights reserved.
//
/////////////////////////////////////////////////////////////////////////////////

define("format", function()
{
    // cache the regexs for {0}, {1}, {2}, etc, as we use them so future
    // calls don't have to create new ones each and every time. 
    var regexs = [];

    function internalFormat(fmt, offset, args)
    {
        // walk through all the args. We're always going to start with zero because that's 
        // the first replacement value ("{0}"). But the index into the args array will be 
        // shifted up by the offset. So don't go higher than the length less the offset.
        // eg: if offset is 1, we're going to access indexes 1 through arg.length-1 as replacement {0} through {args.length-2}
        for (var ndx = 0; ndx < args.length - offset; ++ndx)
        {
            // first see if we have this regex cached -- if so, use it. Otherwise create it and stuff it in the array for future use.
            // if offset is 0: replace {0} with arguments[0], {1} with arguments[1], etc.
            // if offset is 1: replace {0} with arguments[1], {1} with arguments[2], etc.
            fmt = fmt.replace(regexs[ndx] || (regexs[ndx] = new RegExp('\\{' + ndx + '\\}', "g")), args[ndx + offset]);
        }

        // the prototype method behaves strangely when passed no arguments. For some reason in that case, typeof fmt returns "object"
        // and the object has each character in a property with the index name. "the".format() returns an object that looks like this:
        // {0:"t",1:"h",2:"e",format:[format function]}. It BEHAVES like a string, but it's not. The unit test strictEquals tests
        // caught this weirdness. SO... if there are no args, the for-loop would have done nothing. Instead of just returning fmt in
        // that situation, call toString on it to make SURE it's a real string type. The format(fmt) version behaves just fine. Weird.
        return args.length ? fmt : fmt.toString();
    }

    // for backwards-compatibility
    String.prototype.format = function()
    {
        // call the internal format function; "this" is the format string, and 
        // the replacement values start at arguments[0].
        // since "this" is a string, we don't have to make sure the return value is a string.
        return internalFormat(this, 0, arguments);
    };

    return function (str)
    {
        /// <summary>format a string from a format specifier and optional set of arguments</summary>
        /// <returns type="string">formatted string</returns>
        if (typeof str == "function")
        {
            // if we are passed a function, try executing it first so that its this pointer 
            // is the function, and its arguments are the current arguments after the function. 
            str = str.apply(str, Array.prototype.slice.call(arguments, 1));
        }

        // shortcut: null (or undefined, since this isn't a strict equality comparison) just returns an empty string.
        if (str == null)
        {
            return "";
        }

        // if this isn't a string, try converting to string now
        if (typeof str != "string")
        {
            str = str.toString();
        }

        if (str)
        {
            // call the internal format function. The format is the first parameter, so
            // the replacement values start at arguments[1].
            return internalFormat(str, 1, arguments);
        }

        // if we get here, the converted format string was probably empty.
        return "";
    };
});

/* WGINCLUDE: C:\PROJECTS\WORKSPACE2\MSNMETRO\MAIN\METROSDK\METROSDK\CONTENT\SOURCE\JS\CLASSIC\..\Shared\Tracking\generictracking.js */
/////////////////////////////////////////////////////////////////////////////////
//
// File: generictracking.js
// Defines: "track".generictracking
// Dependencies: track.js, extend.js, trackInfo.js
// Description: Generic tracking implementation
//
// Copyright (c) 2012 by Microsoft Corporation. All rights reserved.
//
/////////////////////////////////////////////////////////////////////////////////

define("track.generic", ["track", "extend", "trackInfo"], function (track, extend, trackInfo)
{
  var defaults =
    {
      base: '',             // base: Base url for tracking
      // Params that come from xslt      
      // common: {}         // This is the list of constant params for each tracking event
      // commonMap: {}      // This is the map of properties to send for each traking event
      // evtname:
      // {
      //    url:'',         //This is the addition to the base url if required
      //    param: {},      //This is the list of constant parameters if required
      //    paramMap: {},   //This is the map of properties to send for this event if required
      //    condition is the name of the function($.funtionName) to call to check if this event beacon should be fired
      //    or condition is a int, 0 means dont fire, 1 means fire the beacon
      //    condition: ''
      // }
      samplingRate: 100,
      eventAlias:             //Map of eventnames to existing mapings
        {
        submit: "click",    // This means that for 'submit' event, use the 'click' event mapping.
        mouseenter: "click",
        mouseleave: "click",
        click_nonnav: 'click', // Adding the new event to the mapping. Use the click parameters.
        mouseenter_nav: 'click'
      }
    };


  function generictracking(defaultOpts)
  {
    this.defaultOpts = extend(true, {}, defaults, defaultOpts);
    this.samplingRate = this.defaultOpts.samplingRate;
  }

  generictracking.prototype =
    {
      getEventTrackingUrl: function (eventType)
      {
        ///	<summary>
        ///		generates the url for the eventName based on the dictionary in settings
        ///	</summary>
        ///	<param name="eventType" type="String">
        ///     type of event used to lookup in settings to get the eventObj
        /// </param>
        ///	<returns type="String">
        ///     url that will be called to do the click\event tracking
        /// </returns>
        var url = "";
        // check if we need to send tracking
        var defOpts = this.defaultOpts;
        var eventObj;
        // check if there is an eventName defined in the options
        if (!eventType)
        {
          eventType = (trackInfo.event || {}).type;
        }
        eventObj = defOpts[eventType];
        // if event is not defined, but an alias is defined, use alias map
        if (!eventObj && defOpts.eventAlias)
        {
          eventObj = defOpts[defOpts.eventAlias[eventType]];
        }
        if (eventObj)
        {
          var baseurl = defOpts.base + (eventObj.url ? eventObj.url : '');
          return track.generateUrl(baseurl, defOpts.common, defOpts.commonMap, eventObj.param, eventObj.paramMap);
        }

        return url;
      },
      getPageViewTrackingUrl: function ()
      {
        ///	<summary>
        ///		generates the url for Page view tracking
        ///	</summary>
        ///	<returns type="String">
        ///     url that will be called to do the Page View tracking
        /// </returns>

        return this.getEventTrackingUrl("impr");
      }
    };

  return generictracking;
});

/* WGINCLUDE: C:\PROJECTS\WORKSPACE2\MSNMETRO\MAIN\METROSDK\METROSDK\CONTENT\SOURCE\JS\CLASSIC\..\Shared\Tracking\omnitracking.js */
/////////////////////////////////////////////////////////////////////////////////
//
// File: omnitracking.js
// Defines: "track".omnitracking
// Dependencies: track, extend, trackInfo, format
// Description: describes the a omni tracking implementation
//
// Copyright (c) 2012 by Microsoft Corporation. All rights reserved.
//
/////////////////////////////////////////////////////////////////////////////////

define("track.omni", ["track", "extend", "trackInfo", "format"], function (track, extend, trackInfo, format)
{
  var dt = new Date();
  var generateUrl = track.generateUrl;

  /// <Summary>
  ///     Generates the timestamp as "dd/mm/yyyy HH:MM:ss DAY TimeZ"
  /// </Summary>
  var timestamp = [dt.getDate(),
                    "/", dt.getMonth(),
                    "/", dt.getFullYear(),
                    " ", dt.getHours(),
                    ":", dt.getMinutes(),
                    ":", dt.getSeconds(),
                    " ", dt.getDay(),
                    " ", dt.getTimezoneOffset()
                    ].join("");

  var defaults =
  {
    base: '',                   // base url
    linkTrack: 1,
    samplingRate: 100,          // A value of 0 will turn off tracking completely.
    /// <summary>
    ///     List of params sent for click and page tracking
    /// <summary>
    common:
    {
      v: 'Y',                 // Browser.JavaEnabled 
      j: '1.3'                // Browser.JavascriptVersion
    },
    commonMap:
    {
      client:
      {
        c: "colorDepth"
      }
    },
    page:
    {
      // Misc.MonthAndYear
      v1: dt.getMonth() + 1 + "/" + dt.getFullYear(),
      // Misc.Date
      v2: dt.getMonth() + 1 + "/" + dt.getDate() + "/" + dt.getFullYear(),
      t: timestamp
    },
    pageMap:
    {
      sitePage:
      {
        c3: "pageVersion"
      }
    },
    link:
    {
      t: timestamp,
      ndh: 1,       // Misc.ndh
      pidt: 1,      // Misc.PageIdentity
      pe: "lnk_o",  // Link.Type 
      events: "events4"
    },
    linkMap:
    {
      sitePage:
      {
        c38: "pageVersion"
      }
    }
  };
  // List of events for which we will fire an event, all others are skipped
  var eventMap =
  {
    click: "click",
    mouseenter: "hover",
    mouseleave: "hover",
    submit: "submit",
    click_nonnav: 'click', // Adding the new event to the mapping. Use the click parameters.
    mouseenter_nav: 'click'
  };

  function omnitracking(defaultOpts)
  {
    this.defaultOpts = extend(true, {}, defaults, defaultOpts);
    this.samplingRate = this.defaultOpts.samplingRate;
  }

  omnitracking.prototype =
  {
    getEventTrackingUrl: function (eventType)
    {
      ///	<summary>
      ///		generates the url for click\Event tracking
      ///	</summary>
      ///	<param name="eventType" type="string">    
      /// </param>
      ///	<returns type="String">
      ///     url that will be called to do the click\event tracking
      /// </returns>
      var url = "";
      var defOpts = this.defaultOpts;
      if (!eventType)
      {
        eventType = (trackInfo.event || {}).type;
      }
      // if linktracking is enabled && event trackable.
      if (defOpts.linkTrack && eventMap[eventType])
      {
        defOpts.link.c11 = eventMap[eventType];

        // Add the base url and querystring params to an array
        url = format(defOpts.base, 
          trackInfo.userDynamic.timeStamp(),
          generateUrl("", defOpts.common, defOpts.commonMap, defOpts.link, defOpts.linkMap));
      }
      return url;
    },
    getPageViewTrackingUrl: function ()
    {
      ///	<summary>
      ///		generates the url for Page view tracking
      ///	</summary>
      ///	<returns type="String">
      ///     url that will be called to do the Page View tracking
      /// </returns>
      var pvurl = "";
      // check if we need to send tracking
      var defOpts = this.defaultOpts;
      // Add the base url and querystring params to an array
      pvurl = format(defOpts.base, 
        trackInfo.userDynamic.timeStamp(),
        generateUrl("", defOpts.common, defOpts.commonMap, defOpts.page, defOpts.pageMap));
      return pvurl;
    }
  };

  return omnitracking;
});

/* WGINCLUDE: C:\PROJECTS\WORKSPACE2\MSNMETRO\MAIN\METROSDK\METROSDK\CONTENT\SOURCE\JS\CLASSIC\..\Shared\Implementations\trackclassic.js */
///#DEFINE CROSSBROWSER

/* WGINCLUDE: C:\PROJECTS\WORKSPACE2\MSNMETRO\MAIN\METROSDK\METROSDK\CONTENT\SOURCE\JS\CLASSIC\..\Shared\Tracking\track.js */
///////////////////////////////////////////////////////////////////////////////////
//
// File: track.js
// Defines: "track"
// Dependencies: trackInfo, extend, dom, getCookie, setCookie, bind, window, document, mediator, afire
// Description: Implements multiple parallel tracking engine which gets its tracking
//              url from the specific implementation using defined functions.
//
//              if called multiple times during page interaction (representing significant content
//              update), the trackPage method will also publish a "pageView" mediator event that third-party
//              tracking code can hook to register their refresh page-view beacons.
//
//              NOTE: using #IFDEF CROSSBROWSER so that we can
//              remove a ton of code when we know that we're in IE.
//              When we add multiple browser support, we'll need to use the CROSSBROWSER define
//
// Copyright (c) 2012 by Microsoft Corporation. All rights reserved.
//
/////////////////////////////////////////////////////////////////////////////////

define("track", ["trackInfo", "extend", "dom", "getCookie", "setCookie", "bind", "window", "document", "mediator", "afire"],
function (trackInfo, extend, dom, getCookie, setCookie, bind, window, document, mediator
    ///#IFDEF CROSSBROWSER
    ///#ELSE
    , fireAndForget
    ///#ENDIF
    )
{
    // shortcuts to dom functions
    var attr = dom.attr;
    var domName = dom.name;
    var getParent = dom.parent;
    var getChildren = dom.children;
    var recipients = [];
    var sampleValue = -1;
    var refreshPageLoad;
    var win = window;
    var doc = document;
    // Token to find the custom event token passed on the url. the format would be like this - http://www.msn.com/#tevt=click_nonnav;tobjid=....;...
    var tEvtTag = 'tevt=';
    // Adding the custom event names from resource file, so that it can be changed without changing the code. All modules should use resource string for custom event.
    // These resource strings should be added at feature level and can be overridden at product level, if needed.
    var nonNavClckEvt = "click_nonnav";

    // If there are more than one tags(name value pair), then the name value pair should be separated with ;
    var regExpEventName = /#tevt=([A-Za-z0-9]+_[A-Za-z0-9]+)(;*)/g;
    var tInfo; // shortcut for trackInfo.
    ///#IFDEF CROSSBROWSER
    // OOB related
    var evtCount = 0; // keep track of the events being fired.
    var holdReq = 0; // It is required to cache the value determined after the page load.
    var maxSpinTimeoutHandleForOOB; // Max Spin Timeout handle for OOB.
    var evtTargetUrl; // event target URL. The browser will navigate to this Url after sending the beacons.
    var waitStartTime; // record the time, when te wait statred.
    var clickTimeoutHandler; // stores new handler for each naviagation attempt
    ///#ENDIF

    function trackAll(implementationMethod
    ///#IFDEF CROSSBROWSER
    , shouldSpin
        ///#ENDIF
    )
    {
        ///	<summary>
        ///	    After createReport returns a report, this method fires tracking call for each registered tracking system
        ///	</summary>

        var implementation;
        var ndx = 0;

        // Store sample value in cookie
        sample();
        // Increment the event Number
        track.incrementEventNumber();
        // loop through each implementation
        for (; ndx < recipients.length; ndx++)
        {
            implementation = recipients[ndx];
            if (implementation && implementation.samplingRate >= sampleValue)
            {
                ///#IFDEF CROSSBROWSER
                tFireAndForget(implementation[implementationMethod]());
                ///#ELSE
                fireAndForget(implementation[implementationMethod]());
                ///#ENDIF

            }
        }
        // Reset AOP and CM values
        tInfo.curAop = "";
        ///#IFDEF CROSSBROWSER
        spinIfNeeded(shouldSpin, tInfo.spinTimeout);
        ///#ENDIF
    }

    function sample()
    {
        /// <summary>
        ///     Returns sample value the request belongs to.
        /// </summary>
        if (-1 == sampleValue)
        {
            var smpCookie = tInfo.smpCookie;
            // Returns a sampleValue between 0 & 100 depending on the getCookie value
            sampleValue = parseInt(getCookie(smpCookie));
            // In certain cases parseInt will return Nan, therefore we check the sampleValue
            sampleValue = isNaN(sampleValue) ? Math.floor(Math.random() * 100) : sampleValue % 100;

            // Set the expiry date to approximately 6 months. It may be off by a day or two depending on the year (leap or not) and time of the year.
            setCookie(smpCookie, sampleValue, 182, setCookie.topDomain, "/");
        }
        return sampleValue;
    }

    ///#IFDEF CROSSBROWSER
    function spinIfNeeded(spinNeeded, timeout)
    {
        /// <summary>
        ///     spinIfNeeded: waits for spinTimeout if browser is not IE
        ///     In IE the image will not get cleaned up because of circular reference
        ///     when we navigate away from the page, so we don't need to spin and wait
        ///     for IE browsers.
        /// </summary>
        if (!timeout)
        {
            timeout = tInfo.spinTimeout;
        }

        var ndx;
        // Do not Spin, if doing OOB. Just wait for the timer to expire.
        if (spinNeeded && !win.ActiveXObject && !maxSpinTimeoutHandleForOOB)
        {
            ndx = +new Date + timeout;
            ///#DEBUG
            logDiag("Spinning for MS = " + timeout);
            ///#ENDDEBUG

            // keep looping until spinTimeout has elapsed.
            while (+new Date < ndx)
            {
                //Nothing to do, just spin
            };
        }
    }
    ///#ENDIF

    function depthFirstChildAttribute(element, attributeName, skipElement)
    {
        /// <summary>
        ///     find the first attribute from the element in the depthFirst manner
        ///     skips the element if it is specified in elementName
        /// </summary>
        var children = getChildren(element) || [];
        var ndx = 0;
        var altValue;

        attributeName = attributeName || "alt";

        for (; ndx < children.length; ndx++)
        {
            altValue = attr(children[ndx], attributeName)
                        || depthFirstChildAttribute(children[ndx], attributeName, skipElement);

            if (altValue && !(skipElement == children[ndx].localName))
            {
                return altValue;
            }
        }
    }

    function findCMValue(element)
    {
        /// <summary>
        ///     create a list of id values separated with '>'
        ///     walking from generic to specific
        ///     ending at (not including) wrapper on the generic side
        /// </summary>
        if (element)
        {
            var parent = getParent(element);
            var curId = attr(parent, "id");
            var previousCM;
            if (tInfo.wrapperId == curId)
            {
                return;
            }
            previousCM = findCMValue(parent);
            if (previousCM && curId)
            {
                return [previousCM, curId].join(tInfo.cmSeparator);
            }
            return curId || previousCM;
        }
    }

    function getContentElementIndex(element)
    {
        /// <summary>
        ///     count number of trackable elements upto and including the
        ///     element passed into the function
        ///     stop at the first parent with an id
        ///     do not use anything with a new id
        ///     indexing all elements with the same CM value
        ///     indexing starts at 1
        ///     0 means there is no parent with an id
        /// </summary>
        if (!element)
        {
            return;
        }

        var parent = getParent(element);
        var children;
        var count = 0;
        var ndx = 0;

        // go up first, incase there is no parent id so we don't have to waste a lot of work
        if (!attr(parent, "id"))
        {
            count = getContentElementIndex(parent);
            if (count)
            {
                // by default getContentElementIndex will count the element passed in
                // subtract 1 to offset this
                count--;
            }
            else
            {
                // only stays 0 if there is no parent or no parent with an id
                // bubble up the expected 0 value
                return 0;
            }
        }

        children = getChildren(parent) || [];
        for (; ndx < children.length; ndx++)
        {
            if (children[ndx] == element)
            {
                count++;
                break;
            }
            count += countAllTrackableLinks(children[ndx]);
        }
        return count;
    }

    function countAllTrackableLinks(element)
    {
        /// <summary>
        ///     returns count of all trackable elements[contains href and notrack]
        /// </summary>
        var count = 0;
        var ndx = 0;
        var children;
        if (element && !attr(element, "id"))
        {
            children = getChildren(element) || [];
            if (attr(element, "href") && !attr(element, tInfo.notrack))
            {
                count++;
            }
            for (; ndx < children.length; ndx++)
            {
                count += countAllTrackableLinks(children[ndx]);
            }
        }
        return count;
    }

    function trackEvent(event, element, destination, headline, module, index, campaign)
    {
        ///	<summary>
        ///		This method tracks events.
        ///     This methods loops through each implementation,
        ///     Call getEventTrackingUrl and call that url to fire
        ///     the tracking call.
        ///	</summary>
        ///	<param name="event" type="Object" optional="true">
        ///     Event object
        ///	</param>
        ///	<param name="element" type="Object" optional="true">
        ///     element that was clicked or target of the event. Passed to createReport.
        ///	</param>
        ///	<param name="destination" type="String" optional="true">
        ///     destination url to use instead of computing. Passed to createReport.
        ///	</param>
        ///	<param name="headline" type="String" optional="true">
        ///     headline to use instead of computing. Passed to createReport.
        ///	</param>
        ///	<param name="module" type="String" optional="true">
        ///     content module string to be used instead of computing. Passed to createReport.
        ///	</param>
        ///	<param name="index" type="String" optional="true">
        ///     content index string to be used instead of computing. Passed to createReport.
        ///	</param>
        ///	<param name="campaign" type="String" optional="true">
        ///     campaign id to be used instead of computing. Passed to createReport.
        ///	</param>

        // if element is not passed in, try to get it from event
        if (!element && event)
        {
            element = event.target;
        }
        // if element not does not exist or is non trackable
        if (!element || attr(element, trackInfo.notrack))
        {
            return;
        }
        trackInfo.event = event;

        // for bug 1114449
        // element is a jQuery object, not an element
        // break out element from jQuery wrapper
        if (element.jquery)
        {
            element = element[0];
        }

        var elemHref = element.href || attr(element, "href");

        // Temp. Fix: element.getAttribute(attr, 2) returns # only for non-navigational url. Need to get destination with full domain path.
        if (elemHref == "#")
        {
            elemHref = element["href"];
        }

        // find destination value if needed
        destination = destination || attr(element, trackInfo.piiurl) ||
                      elemHref || attr(element, "action") || "";

        // find headline value if needed
        headline = headline || attr(element, trackInfo.piitxt) ||
            ("FORM" == domName(element)
            ? trackInfo.defaultFormHeadline
            : depthFirstChildAttribute(element, "title", "img") || dom.text(element) || attr(element, "alt") || depthFirstChildAttribute(element, "alt") || "");

        // Added condition to call trim function only if it is defined, it fails for browsers <=IE8
        if (headline.trim != undefined)
        {
            headline = headline.trim();
        }

        // find module value if needed
        module = module || findCMValue(element) || trackInfo.defaultModule;

        // find index value if needed
        index = index || (attr(element, "id") ? 1 : getContentElementIndex(element));

        // find campaign value if needed
        // Added backward compatibility for Old GT1 implementation. If "GT1-xxxx" not defined in class, look it into href attribute.
        // Example when GT1 defined in a class: class="GT1-12345"
        // Example when GT1 is a part of href attribute: href="www.msn.com?GT1=12345"
        var clsGT1 = element.className || attr(element, "class");
        campaign = campaign || (/GT1-(\d+)\b/i.exec(clsGT1) ? RegExp.$1 : "") ||
                              (/[?&]GT1=(\d+)\b/i.exec(elemHref) ? RegExp.$1 : "");

        trackInfo.report =
        {
            ///	<summary>
            ///     This method creates a tracking report.
            ///     It does NOT send the report, nor does it perform any navigation.
            ///
            ///	</summary>
            ///	<param name="element" type="Object" optional="true">
            ///     element that was clicked or target of the event.
            ///	</param>
            ///	<param name="destination" type="String" optional="true">
            ///     destination url to use instead of computing.
            ///	</param>
            ///	<param name="headline" type="String" optional="true">
            ///     headline to use instead of computing.
            ///	</param>
            ///	<param name="module" type="String" optional="true">
            ///     content module string to be used instead of computing.
            ///	</param>
            ///	<param name="index" type="String" optional="true">
            ///     content index string to be used instead of computing.
            ///	</param>
            ///	<param name="campaign" type="String" optional="true">
            ///     campaign id to be used instead of computing.
            ///	</param>
            ///	<returns type="Object">
            ///     Report object that is structured as follows:
            ///     report = {
            ///             destinationUrl: destination,
            ///             campaignId: '',
            ///             contentElement: index,
            ///             contentModule: module,
            ///             headline: headline,
            ///             sourceIndex,
            ///             nodeName,
            ///             eventType: click/MouseEnter
            ///     };
            /// </returns>
            destinationUrl: destination,
            headline: headline,
            contentModule: module,
            contentElement: index,
            campaignId: campaign,
            sourceIndex: element.sourceIndex || "",
            nodeName: element.nodeName || ""
        };

        // check for event.noSpin to prevent spinning in trackAll
        trackAll("getEventTrackingUrl", event ? !event.noSpin : 1);
    }

    var track =
    {
      onClick: onClick,
      trackEvent: trackEvent,
      createEvent: createEvt,
      trackPage: function ()
      {
          ///	<summary>
          ///		Fires page view call for each registered tracking system at the top of <Body> tag
          ///	</summary>

          // clear event incase trackPage is called more than once on a page
          delete trackInfo.event;

          // Clear the request id to generate the new request id
          delete trackInfo.userStatic.requestId;

          trackAll("getPageViewTrackingUrl");

          // the refreshPageLoad value is undefined (false) at first page load request, and
          // we then set it to one (true).
          // subsequent calls to trackPage will then publish the mediator pageView event
          // that code can listen for to know when the page is generating a "page view"
          // due to significant content change.
          if (refreshPageLoad)
          {
            mediator.pub("pageView");
          }
          else
          {
            refreshPageLoad = 1;
          }

          ///#IFDEF CROSSBROWSER
          // Pre check the browser version, so that for click tracking beaviour it is already decided.
          shouldHoldForBeacon();
          ///#ENDIF
      },

      register: function ()
      {
          ///	<summary>
          ///		Registers an implementation with the main Tracking function
          ///	</summary>
          ///	<param name="impl" type="Object">
          ///     Implementation of tracking,
          ///     must implement the following functions
          ///     GetEventTrackingUrl(trackInfo)  - generates url for tracking
          ///	</param>
          ///	<param name="impl" type="Object" optional="true" parameterArray="true">
          ///     Implementation of tracking
          ///	</param>
          ///	<returns type="Object">
          ///     Returns the current object to enable cascading calls
          /// </returns>
          var ndx = 0;
          var trackingImplementation;

          // use double-parens to tell the validator that yes, we INTEND this to be an assignment (=)
          // and not a mistyped equals (==) conditional test.
          while ((trackingImplementation = arguments[ndx++]))
          {
              if (isNaN(trackingImplementation.samplingRate))
              {
                  trackingImplementation.samplingRate = 99;
              }
              recipients.push(trackingImplementation);
          }
      },
      incrementEventNumber: function ()
      {
          ///	<summary>
          ///     increments the event number
          ///	</summary>
          tInfo.userDynamic.eventNumber++;
      },

      isSampled: function (samplingRate)
      {
          ///	<summary>
          ///     evaluates if the current request is in the sampling group
          ///	</summary>
          /// <return type="Boolean">
          /// returns true if sample <= samplingRate
          return !(sample() > samplingRate);
      },

      generateUrl: function (baseUrl, common, commonMap, params, paramsMap)
      {
          ///	<summary>
          ///		Utility function to combine all the params and baseUrl to create an beacon Image url
          ///	</summary>
          ///	<param name="baseUrl" type="String">
          ///     Base url for tracking.
          /// </param>
          ///	<param name="params" type="Object">
          ///     Object contains custom parameters.
          /// </param>
          ///	<param name="paramMap" type="Object">
          ///     Object contains param to property mapping
          ///     E.g. map.sitePage:{ a1: "pageName"}
          ///     this will create a querystring a1="value of trackInfo.sitePage.pageName"
          /// </param>
          ///	<returns type="String">
          ///     url that will be called to do the tracking
          /// </returns>

          var groupKey, groupValue, key, value;
          var queryStringArr = [];
          params = extend({}, common, params);
          // Get params from commonMap
          paramsMap = extend(true, {}, commonMap, paramsMap);
          for (groupKey in paramsMap)
          {
              if (trackInfo[groupKey])
              {
                  groupValue = paramsMap[groupKey];
                  for (key in groupValue)
                  {
                      value = trackInfo[groupKey][groupValue[key]];
                      
                      if (typeof value == "function")
                      {
                          value = value();
                      }
                      
                      // allow for empty strings but use null or undefined to ignore the parameter entirely
                      if (value != null)
                      {
                          params[key] = value;
                      }
                  }
              }
          }

          /********************************************************
          *******  Experiment Code for Page Refresh  **************
          *******  Need to remove before feature live *************
          *******  And place the code in CSLZIP *******************
          ********************************************************/

          if (params.prf && location.href.indexOf('prf=1') > 0)
          {
              params.prf = 1;
          }

          if (params.ob && typeof window.blurFired != 'undefined' && window.blurFired == 0)
          {
              params.ob = 0;
          }

          /********************************************************
          *******End Code for Page Refresh Experiment**************
          ********************************************************/

          for (key in params)
          {
              queryStringArr.push(encodeURIComponent(key) + "=" + encodeURIComponent(params[key]));
          }
          return baseUrl + queryStringArr.join("&").replace(/%20/g, "+");
      },
      extend: function (obj)
      {
          extend(true, trackInfo, obj);
      },
      form: function (elem)
      {
          ///	<summary>
          ///		Helper function to bind Form elements that are part of element for tracking
          ///	</summary>

          if (!elem || !elem.length)
          {
              elem = [elem];
          }
          var formElem;
          var ndx = 0;

          // use double-parens to tell the validator that yes, we INTEND this to be an assignment (=)
          // and not a mistyped equals (==) conditional test.
          while ((formElem = elem[ndx++]))
          {
              // if element is a form element, bind the submit to it.
              if ("FORM" == domName(formElem))
              {
                  bind(formElem, "submit", trackEvent);
              }
          }
      }
    };

    function getEventFromUrl(targetUrl)
    {
        ///	<summary>
        /// Check the Current Url and get the event name from the targetUrl.
        /// The URL format for the specifying the event name is - url#tevt=<event name>;<other name value pairs>
        /// <parameter>
        /// targetUrl - The target Url for the event.
        /// </parameter>
        /// <return> event name, if it is specified, else null. </return>
        ///	</summary>;

        var evtName = null;
        // It has moved to the top. Keep the regex as global.
        //var regExpEventName = /#tevt=([A-Za-z0-9]+_[A-Za-z0-9]+)(;*)/g;
        // If there are more than one tags(name value pair), then the name value pair should be separated with ;

        var result = regExpEventName.exec(targetUrl);
        /*
        For input  value : "http://www.bing.com/videos/search?q=&mkt=en-GB#tevt=click_nonnav;"
        The result will like this-
        [#tevt=click_nonnav;,click_nonnav,;]
        [0] : "#tevt=click_nonnav;",
        [1] : "click_nonnav",
        [2] : ";",

        The  1st element contains the event name.
        */
        if (result && result.length >= 1 && result[1])
        {
            evtName = result[1];
        }
        // If event name not found, return null, the original event name will be kept.
        return evtName;
    };

    function getClickEventFromUrl(targetUrl)
    {
        ///	<summary>
        /// Check the Current Url -
        /// Case 1-  if the Url format is - baseurl/#, then treat it as nonNav event.
        ///     Match Domain name.
        ///     If the Url of current page and target page is same and only # is at the end, then it is not navigating away from the page.
        ///
        /// Case 2 - the Url format is url/#tevt=<eventname>;<other name value pairs>,
        ///            extract the event name from the Url. the event name is terminated with ;
        /// <parameter>
        /// targetUrl - The target Url for the event.
        /// </parameter>
        /// <return> string - event name,, if it is a non nav url and no event name is specified then return the default non nav click event name, else  return the event name specified in the url. </return>
        ///	</summary>

        var evtName = null;
        if (targetUrl)
        {
            // if the target url contains tevt=, then this url contains the event name. We will extract the event name from url.
            if (targetUrl.indexOf(tEvtTag) == -1)
            {
                //  If the Url format is - http://www.msn.com/#, then categorize it as nonnav click.
                // Convert the baseurl and currentUrl in lowercase for comparison.
                var baseUrl = targetUrl.substring(0, targetUrl.indexOf('#')).toLowerCase();
                var curUrl = window.location.href.toLowerCase();
                if (curUrl == baseUrl || curUrl.substring(0, curUrl.indexOf('#')) == baseUrl)
                {
                    evtName = nonNavClckEvt;
                }
            }
            else
            {
                // extract the event name from url.
                evtName = getEventFromUrl(targetUrl);
            }
        }
        // Return  null, so that the original event is kept.
        return evtName;
    };

    function createEvt(event, eventName, targetElem)
    {
        ///	<summary>
        /// Creates an event based upon the properties of the base event.
        /// <parameter>
        /// event - This is a an input event parameter. Some basic properties are copied from this param to the the event created.
        /// eventName - Name of the event to be created.
        /// targetElem - Target element that need to be attached to the event being created in this method.
        /// </parameter>
        /// <return> object - An object is created with base properties from the event passed as param. This object is then returned. </return>
        ///	</summary>
        var evt = null;
        if (doc.createEvent)
        {  // Supported by non-IE based browsers.
            evt = doc.createEvent("Events");
            evt.initEvent(eventName, false, true, targetElem || window,
                              0, 0, 0, 0, 0, false, false, false, false, 0, null);
        }
        else if (doc.createEventObject)
        {  // Supported by IE based browsers.
            evt = doc.createEventObject(event);
            evt.type = eventName;
        }
        if (evt)
        {
            evt.customTarget = targetElem;
        }
        return evt;
    };

    function preventEventDefault(event)
    {
        ///	<summary>
        /// Cancels the event and prevents from being passed further.
        /// <parameter>
        /// event - Input event parameter that need to be canceled.
        /// </parameter>
        ///	</summary>
        if (event.preventDefault)
        {   // For Non IE
            event.preventDefault();
        }
        else
        {    //For IE
            event.returnValue = false;
        }
    };

    function onClick(event)
    {
        ///	<summary>
        ///		OnClick Handler
        ///     we'll only track clicks if the target is a link (A or AREA)
        ///     AND the mouse button isn't the RIGHT button.
        ///     MOST browsers think the mouse button value is 2. For those that don't, this
        ///     might have to be revisited.
        ///	</summary>
        if (2 == event.button)
        {
            return;
        }
        // Temp. Hack for Image Map: In IE9, srcElement associated with window.event is not returning correct element.
        // Instead of returning a <Area> element, it is returning <IMG> element. <IMG> element has no href attribute, neither it's parents do have.
        // For this reason, Tracking calls are not fired in IE9.
        // This hack will check if the event belongs to ImageMap. If so, it will pick activeElement from document and assign it as target/customTarget.
        try
        {
            if (event.customTarget && event.customTarget.useMap)
            {
                event.customTarget = event.customTarget.document.activeElement;
                event.target = event.customTarget;
            }
        }
        catch (ex) { }

        //Reset the EventCount.
        ///#IFDEF CROSSBROWSER
        evtCount = 0;
        ///#ENDIF

        // we assume that an element with an href (and <a> or an <area>) is the click-tracking element.
        // we also want to target <button> events -- they get clicked, too, but have no href, yet we
        // still want to track them as well.
        var target = dom.getTarget(event);
        while (target && !attr(target, "href") && target.nodeName != "BUTTON")
        {
            target = getParent(target);
        }

        if (target)
        {
            var evt = event;

            // if href attr is not defined, then target will be null.
            var tHref = target["href"];
            if (tHref && tHref.length)
            {
                // if the url ending with #, check if it is non Nav Url.
                if (event.type == "click" && tHref.indexOf('#') == tHref.length - 1)
                {
                    var evtName = getClickEventFromUrl(tHref);
                    if (evtName)
                    {
                        evt = createEvt(event, nonNavClckEvt, target);
                    }
                }

                // prevent the default action for non click events, so that it does not show up on the address bar.
                if (evt.type == nonNavClckEvt)
                {
                    preventEventDefault(event);
                }

                    ///#IFDEF CROSSBROWSER
                    // If non-IE browsers and skipOOB is false, then apply the OOB behavior.
                else if (evt.type == "click" && shouldHoldNav(evt, tHref) && !evt.defaultPrevented)
                {
                    if (!dom.containsClass(target, "skipOOB"))
                    {
                        applyOOB(event, tHref);
                    }
                }
                ///#ENDIF
                trackEvent(evt, target);
            }
            else
            {
                // no href -- probably a button; which would be a non-nav event.
                trackEvent({type: nonNavClckEvt}, target);
            }
        }
    };

    ///////////////////////////////////////
    ///   OOB related methods
    ////////////////////////////////////////

    ///#IFDEF CROSSBROWSER

    // Alternate method the the fire and forget method and add tracing to it tracing. This method has extra logic for OOB.
    function tFireAndForget(url)
    {
        ///	<summary>
        ///	Alternate method for fireAndForget.
        // This keeps track of beacon fired and response/results received. If all the responses are received, it immediately calls the clickTimeoutHandler() to navigate to the traget Url.
        /// If the oobWaitTime occurs the clickTimeoutHandler() will be called regardless of the result/responses received.
        /// This mechanism allows us to navigate sooner, if all the beacons are sent and response received before the timeout (oobWaitTime).
        ///	</summary>
        ///	<returns type="none" >
        /// </returns>
        if (url)
        {
            var img = new Image();
            var currentClickTimeoutHandler = clickTimeoutHandler;

            img.onload = img.onerror = img.onabort = function ()
            {
                // Decrement remaining/unack event count.
                evtCount--;
                ///#DEBUG
                logDiag(evtCount + " remaining events. Beacon response received. url= " + url);
                ///#ENDDEBUG

                img.onload = img.onerror = img.onabort = null;
                // Check if we are waiting (maxSpinTimeoutHandleForOOB will set, if we are waiting).
                // It will be set for filtered non-IE browsers.
                if (maxSpinTimeoutHandleForOOB && evtCount <= 0)
                {
                    ///#DEBUG
                    logDiag(evtCount + " remaining events. calling clickTimeoutHandler().");
                    ///#ENDDEBUG
                    // Call the ClickTimeoutHandler.
                    if (currentClickTimeoutHandler)
                    {
                        currentClickTimeoutHandler();
                    }
                }
            };

            var src = url.replace(/&amp;/gi, "&");
            // Increment the beacon count. It is decremented, when ack is received.
            evtCount++;
            img.src = src;
            ///#DEBUG
            logDiag("Beacon count: " + evtCount + " url : " + img.src);
            ///#ENDDEBUG
        }
    };

    function applyOOB(event, tHref)
    {
        ///	<summary>
        ///	This method applies the OOB behavior - cancel the navigation, fire the beacons and navigate to the target url after timeout.
        ///	</summary>
        /// <parameter>
        /// event - Input event parameter that need to be canceled.
        /// </parameter>
        /// <parameter>
        /// tHref - Target URL.
        /// </parameter>
        ///	<returns type="none" >
        /// </returns>

        // set the event count.
        evtCount = 0;
        // store the target URL.
        evtTargetUrl = tHref;
        // Stop the navigation, by preventing the default action.
        preventEventDefault(event);
        // creates a click timeout handler for use here and during tFireAndForget
        clickTimeoutHandler = getClickTimeoutHandler(evtTargetUrl);
        // Set the timeout for oobWaitTime (it is different than spinTimeout) and timeouthandler  (clickTimeoutHandler) to navigate to evtTargetUrl.
        // We can individually control the spinTimeout (for older browsers) and WaitTime (for newer browsers).
        maxSpinTimeoutHandleForOOB = win.setTimeout(clickTimeoutHandler, tInfo.oobWaitTime);
        //Store the wait start time.
        waitStartTime = +new Date();
    };

    function getClickTimeoutHandler(targetUrl)
    {
        // A flag to denote, if navigation has started. This is to avoid race condition.
        var navDone;

        return function ()
        {
            ///	<summary>
            ///	This method is applicable to OOB behavior - cancel the navigation, fire the beacons and navigate to the target url after timeout.
            /// This the clickTimeoutHandler, which is called when the timeout occurs are all the beacons are sent and the responses are received.
            ///  This method will cancel the timer and set the flag navDone, Nvaigates to the traget URL. If navDone is set, then it will skip the process, as the navigation has already started.
            /// This can occur in race conditions, when this method is called by the timer and from the beacon callback.
            ///	</summary>
            ///	<returns type="none" >
            /// </returns>

            ///#DEBUG
            logDiag("clickTimeoutHandler() Enter");
            ///#ENDDEBUG

            // Clear the timer and reset the handle.
            if (maxSpinTimeoutHandleForOOB)
            {
                win.clearTimeout(maxSpinTimeoutHandleForOOB);
                maxSpinTimeoutHandleForOOB = 0;
            }

            // Check if the navigation has already started(navDone). We can get this method called by the fireandForget or the timeout.
            // In Some cases, this method may get called from both places, so we need to make sure that this method is called once.
            // JS execution is single threaded, so this check should be fine.
            if (evtTargetUrl == targetUrl && !navDone)
            {
                navDone = 1;
                var evt = tInfo.event;

                var timeTaken = +new Date() - waitStartTime;
                if (timeTaken < tInfo.oobWaitTime)
                {
                    ///#DEBUG
                    logDiag("clickTimeoutHandler(): Waiting for spinIfNeeded.");
                    ///#ENDDEBUG

                    // if all the beacons are fired. Spin for some time, so that the request is sent out.
                    // Otherwise, if it is too quick, the outstanding requests are aborted.
                    spinIfNeeded(true, tInfo.oobWaitTime - timeTaken);
                }

                if (evt && evt.type == "click")
                {
                    win.location = evtTargetUrl; // +'?timeTaken=' + timeTaken;
                }

                ///#DEBUG
                logDiag("Navigating to Traget : " + win.location + " timeTaken=" + timeTaken);
                ///#ENDDEBUG
            }
        };
    };

    function shouldHoldNav(event)
    {
        ///	<summary>
        ///	This method applies the OOB behavior - cancel the navigation, fire the beacons and navigate to the target url after timeout.
        /// This method checks, if it should hold/cancel the navigation.
        /// It checks for the event type. Currently only click is handler. It also checks the flags stored previously (holdReq).
        /// Otherwise it will call shouldHoldForBeacon() method to check the settings.
        ///	</summary>
        /// <parameter>
        /// event - Input event parameter that need to be canceled.
        /// </parameter>
        ///	<returns type="boolean" >
        /// returns true, if the OOB behavior should be applied to this event, else false.
        /// </returns>

        if ((event && (event.type == "click")) && (holdReq == 1) || shouldHoldForBeacon()) // Not handling "submit" event. Only Clicks are handled.
        {
            return true;
        }
        return false;
    };

    function shouldHoldForBeacon()
    {
        ///	<summary>
        ///	This method applies the OOB behavior - cancel the navigation, fire the beacons and navigate to the target url after timeout.
        /// This method checks, if it should hold/cancel the navigation.
        /// It checks for the browser version filter and the setting enableOOB.
        /// The result is stored in holdReq, so it will not evaluate the browser version, if it is called next time.
        /// This method is called after the IMPR beacons are sent, so at the click event, we will get the cached result.
        ///	</summary>
        ///	<returns type="boolean" >
        /// true, if the browser version and the settings are mapped to apply OOB behavior, else false.
        /// </returns>

        try
        {
            // If IE or enableOOB is set to false, reurn false.
            if (tInfo.client.isIE() || tInfo.enableOOB == 0)
            {
                holdReq = 0;
                return false;
            }

            // Get the browser version and check it against the browser version filter - tInfo.BwVerTabl
            var version = getBrowserVersion();
            if (!version || !tInfo.bwVerTable)
            {
                holdReq = 0;
                // Set this flag, so that it function is not called next time. The browser version is not going to change during the session.
                tInfo.enableOOB = 0;
                return false;
            }

            var tragetVer = null;
            // Check against the browser version table.
            if (version.browser == "mozilla" && tInfo.bwVerTable.mozilla)
            {
                tragetVer = tInfo.bwVerTable.mozilla;
            }
            else if (version.browser == "webkit" && tInfo.bwVerTable.webkit)
            {
                tragetVer = tInfo.bwVerTable.webkit;
            }

            if (tragetVer && compareVer1GreaterOrEqualThanVer2(version.version, tragetVer))
            {
                holdReq = 1;
                return true;
            }
            else
            {
                holdReq = 0;
                // Set this flag, so that it function is not called next time. The browser version is not going to change during the session.
                tInfo.enableOOB = 0;
                return false;
            }
        }
        catch (exp)
        {
            // In case of error  disable it.
            holdReq = 0;
            tInfo.enableOOB = 0;
            return false;
        }

        return true;
    };

    function parseVersion(verStr)
    {
        ///	<summary>
        ///	This method parses the version string and returns the version.
        ///	</summary>
        /// <parameter>
        /// verStr - version string such as - 2.0.15.
        /// </parameter>
        ///	<returns type="version" >
        /// It returns the version structure as -
        ///
        //    { major: maj,
        ///    minor: min,
        ///    patch: pat}
        /// if version cannot be parsed, then it will return 0.0.0.
        /// </returns>

        var verItems = verStr.split('.');
        var maj = parseInt(verItems[0]) || 0;
        var min = parseInt(verItems[1]) || 0;
        var pat = parseInt(verItems[2]) || 0;
        return {
            major: maj,
            minor: min,
            patch: pat
        };
    };

    function compareVer1GreaterOrEqualThanVer2(ver1, ver2)
    {
        ///	<summary>
        ///	This method compare 2  version strings and returns true if ver1 >= ver2.
        ///	</summary>
        /// <parameter>
        /// ver1 - version string such as - 2.0.15.
        /// </parameter>
        /// <parameter>
        /// ver2 - version string such as - 1.1.15.
        /// </parameter>
        ///	<returns type="boolean" >
        /// It returns true, if ver1 >= ver2.
        /// </returns>

        var ver1Items = parseVersion(ver1);
        var ver2Items = parseVersion(ver2);

        if (ver1Items.major != ver2Items.major)
        {
            return (ver1Items.major > ver2Items.major);
        }
        else
        {
            if (ver1Items.minor != ver2Items.minor)
            {
                return (ver1Items.minor > ver2Items.minor);
            }
            else
            {
                if (ver1Items.patch != ver2Items.patch)
                {
                    return (ver1Items.patch > ver2Items.patch)
                }
                else
                {
                    return true; // Means both are equal.
                }
            }
        }

        return false;
    };

    function getBrowserVersion()
    {
        ///	<summary>
        ///	This method gets the browser version. This method is based on jQuery browser version detection logic.
        ///	</summary>
        ///	<returns type="Version" >
        ///
        /// </returns>

        //This method will be called once, so not making the regex global.
        // UserAgent RegExp
        var rwebkit = /(webkit)[ \/]([\w.]+)/,
                ropera = /(opera)(?:.*version)?[ \/]([\w.]+)/,
                rmsie = /(msie) ([\w.]+)/,
                rmozilla = /(mozilla)(?:.*? rv:([\w.]+))?/,
                ua = navigator.userAgent;

        ua = ua.toLowerCase();

        var match = rwebkit.exec(ua) ||
              ropera.exec(ua) ||
              rmsie.exec(ua) ||
              ua.indexOf("compatible") < 0 && rmozilla.exec(ua) || [];

        return { browser: match[1] || "", version: match[2] || "0" };
    };

    //// DEBUG Only methods for logging.

    ///#DEBUG
    // DEBUG only Helper methods for LocalStorage.

    ///	<summary>
    ///	This method writes to localstorage as LS[key]= value;
    ///	</summary>
    /// <parameter>
    /// key - Item key for writing in the localstorage.
    /// </parameter>
    /// <parameter>
    /// value - Item value for writing in the localstorage.
    /// </parameter>
    ///	<returns type="none" >
    ///
    /// </returns>
    function writeToLocalStorage(key, value)
    {
        if (typeof (localStorage) != "undefined")
        {
            try
            {
                localStorage.setItem(key, value);
            }
            catch (e)
            {
                alert("Exception : " + e); //data wasn't successfully saved due to quota exceed so throw an error
            }
        }
    };

    ///	<summary>
    ///	This method reads value from  localstorage as value=  LS[id];
    ///	</summary>
    /// <parameter>
    /// id - Item key to read from localstorage.
    /// </parameter>
    ///	<returns type="string" >
    /// the value for the id from the localstorage. If the Tile is not found, null will be returned.
    /// </returns>
    //    function readFromLocalStorage(id)
    //    {
    //        var val = "";

    //        if (typeof (localStorage) != "undefined")
    //        {
    //            try
    //            {
    //                val = localStorage.getItem(id);
    //            }
    //            catch (e)
    //            {
    //                alert("Exception : " + e); //data wasn't successfully saved due to quota exceed so throw an error
    //            }
    //        }

    //        return val;
    //    };

    function logDiag(msg)
    {
        ///	<summary>
        ///		Debug tracing method. It writes to console and also adds it to local storage, if supported by browser.
        ///	</summary>
        /// <parameter>
        /// msg - Log message.
        /// </parameter>
        ///	<returns type="none" >
        /// </returns>
        var time = new Date();
        var logMsg = time + ": " + +new Date() + " " + msg;

        // Log to console.
        if (win.console)
        {
            if (win.console.log)
            {
                win.console.log(logMsg);
            }
        }

        //Write to  Local Storage
        writeToLocalStorage(new Date(), logMsg);
    };

    ///#ENDDEBUG

    ///#ENDIF

    /////
    //    END OOB Related changes
    //////

    bind(doc, "click", onClick);
    bind(win, "load", trackEvent);
    bind(win, "unload", trackEvent);

    // wait for dom to complete before binding forms
    require(["c.dom"], function ()
    {
        track.form(dom.getElementsByTagName("form"));
    });

    tInfo = trackInfo; // store the TrackInfo. // TODO: remove this since we have a reference with require(["trackInfo"), function(trackInfo) {...})  
    return track;
});

/* WGINCLUDE: C:\PROJECTS\WORKSPACE2\MSNMETRO\MAIN\METROSDK\METROSDK\CONTENT\SOURCE\JS\CLASSIC\..\Shared\Tracking\trackInfo.tokens.js */
/////////////////////////////////////////////////////////////////////////////////
//
// File: trackInfo.tokens.js
// Defines: trackInfo.tokens
// Description: Defines tokens used in trackInfo.js
//
// Copyright (c) 2013 by Microsoft Corporation. All rights reserved.
//
/////////////////////////////////////////////////////////////////////////////////

define("trackInfo.tokens", {
    spinTimeout: %Global_Tracking_SpinTimeOut%,
    browserFilterTable: %Global_Tracking_OOB_BrowserFilterTable%
});

/* WGINCLUDE: C:\PROJECTS\WORKSPACE2\MSNMETRO\MAIN\METROSDK\METROSDK\CONTENT\SOURCE\JS\CLASSIC\..\Shared\Tracking\trackInfo.js */
/////////////////////////////////////////////////////////////////////////////////
// File: trackInfo.js
// Defines: trackInfo
// Dependencies: trackInfo.tokens, dom, getCookie, screen, window, document
// Description: Tracking information aggregate object.
//              used by all the tracking systems to get common info sent via tracking
// Copyright (c) 2013 by Microsoft Corporation. All rights reserved.
/////////////////////////////////////////////////////////////////////////////////

define(
    "trackInfo",
    ["trackInfo.tokens", "dom", "getCookie", "screen", "window", "document"],
    function (tokens, dom, getCookie, screen, window, document)
    {
        var attr = dom.attr;
        var getParent = dom.parent;
        var clientClientId;
        var isHomePageBehaviorAdded;
        var browserWidth;
        var browserHeight;

        function getBrowserSize()
        {
            ///	<summary>
            ///		Helper function used to get the browser height and width
            ///	</summary>
            if (window.innerWidth)
            {
                browserWidth = window.innerWidth;
                browserHeight = window.innerHeight;
            }
            else
            {
                browserWidth = document.documentElement.clientWidth;
                browserHeight = document.documentElement.clientHeight;
            }
        }
    
        function getAopValue(element)
        {
            /// <summary>
            ///     create a list of id values separated with '>'
            ///     walking from generic to specific
            ///     ending at (not including) wrapper on the generic side
            /// </summary>
            if (element)
            {
                var parent = getParent(element);
                var curAopValue = attr(parent, "data-aop");
                var previousAopValue;
                previousAopValue = getAopValue(parent);
                if (previousAopValue && curAopValue)
                {
                    return [previousAopValue, curAopValue].join(trackInfo.cmSeparator);
                }
                return curAopValue || previousAopValue;
            }
        }

        ///	<summary>
        ///		Tracking Information
        ///    client:
        ///    {
        ///        screenResolution: function() { },
        ///        clientId: function() {}            /* MUID cookie value if MUID exists or request ID if MUID doesn't exsit. */
        ///        colorDepth: ''
        ///        cookieSupport: function() { },
        ///        height: function() { },
        ///        width: function() { },
        ///        isIE: function() { },
        ///        connectionType: function() { },
        ///        pageUrl: '',                /* Page.SourceUrl, Page.sourceUrlNoQueryString, Page.Url */
        ///        referrer: '',
        ///        sample: function() {},
        ///        timezone: function() {},
        ///        flightKey: function() {},
        ///        groupAssignment: function() {},
        ///        optKey: function() {},
        ///        silverlightVersion: function() {},
        ///        silverlightEnabled: function() {}
        ///    },
        ///    sitePage:
        ///    {
        ///        lang: '',
        ///        pageName: '',               /* Page.Name */
        ///        siteGroupId: '',            /* Site.GroupID */
        ///        propertyId: '',
        ///        domainId: '',
        ///        propertySpecifier: '',
        ///        sourceUrl: '',
        ///        omniPageName: '',
        ///        pageId: ''
        ///    },
        ///    userStatic:
        ///    {
        ///        birthdate: '',
        ///        gender: '',
        ///        signedIn: false,
        ///        userGroup:'',
        ///        optKey:'',
        ///        settings: '',
        ///        beginRequestTicks: '',
        ///        defaultSlotTrees: '',
        ///        requestId: ''
        ///    },
        ///    userDynamic:
        ///    {
        ///        isHomePage: function() { },
        ///        anid: '',
        ///        timeStamp: function() {}
        ///    },
        ///    report:
        ///    {
        ///        destinationUrl: '',
        ///        campaignId: '',
        ///        contentElement: '',
        ///        contentModule: '',
        ///        headline: '',
        ///        sourceIndex: '',
        ///        nodeName: ''
        ///    },
        //    cmSeparator = opts.cmSeparator,
        //   wrapperId = opts.wrapperId,
        ///    event:{}
        ///
        ///	</summary>

        var trackInfo =
        {
            notrack: "notrack",
            cmSeparator: ">",
            defaultModule: "body",
            defaultFormHeadline: "[form submit]",
            piitxt: "data-piitxt",
            piiurl: "piiurl",
            wrapperId: "wrapper",
            defaultConnectionType: "LAN",
            smpCookie: "Sample",           // Name of Sampling Cookie
            smpExp: 182,                   // Sample cookie expiry in days. Need to have the same expiry days as MUID, which is 6 months.
            MUIDCookie: "MUID",            // This is used for client id. If MUID exists, then we will use MUID as client id otherwise use request id.
            spinTimeout: tokens.spinTimeout, // metadata available to override this value.
            trackTcm: "tcm",
            trackAop: "aop",
            curAop: "",
            event: {},
            sitePage: {},
            userStatic: {},
            oobWaitTime: 150, // This is used to control the timeout value for OOB. Metadata available to override this value.
            enableOOB: 1, // 1= enable, 0= disable OOB. Meta Data available to override this value.
            bwVerTable: tokens.browserFilterTable, // Browser version table to apply OOB behavior
            client:
            {
                // client id
                clientId: function ()
                {
                    if (!clientClientId && clientClientId !== "")
                    {
                        clientClientId = getCookie(trackInfo.MUIDCookie) || trackInfo.userStatic.requestId || "";
                    }
                    return clientClientId;
                },
                // number of bits used to represent the color of a single pixel
                colorDepth: screen.colorDepth,
                // connection type
                connectionType: function ()
                {
                    return trackInfo.defaultConnectionType;
                },
                // does browser supports cookie
                cookieSupport: function ()
                {
                    return document.cookie ? "Y" : "N";
                },
                // browser height
                height: function ()
                {
                    if (!browserHeight)
                    {
                        getBrowserSize();
                    }
                    return browserHeight;
                },
                // entire url of the current page
                // use a function because we may virtually change the page by manipulating the history stack
                // (for example, in a gallery when changing slides).
                pageUrl: function(){return window.location.href;},

                // URL of the document that loaded the current document
                // first check for an override property that we may set during virtual page changes, and if 
                // that's not set, use the real referrer.
                referrer: function(){return document.referrerOverride || document.referrer;},
                // screen resolution
                screenResolution: function ()
                {
                    return [screen.width, screen.height].join("x");
                },
                // browser width
                width: function ()
                {
                    if (!browserWidth)
                    {
                        getBrowserSize();
                    }
                    return browserWidth;
                },
                // timezone
                timezone: function ()
                {
                    var now = new Date();
                    var later = new Date();
                    // Set later to 6 months in the future to account for DST
                    later.setMonth(now.getMonth() + 6);
                    // Get current time difference from GMT
                    var x = Math.round(now.getTimezoneOffset() / 60) * -1;
                    // Get later time difference from GMT
                    var y = Math.round(later.getTimezoneOffset() / 60) * -1;
                    // use the lesser to account for DST
                    return (x < y) ? x : y;
                },
                // whether browser is IE?
                isIE: function ()
                {
                    if (window.ActiveXObject)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            },
            userDynamic:
            {
                anid: function ()
                {
                    return getCookie("ANON");
                },
                isHomePage: function ()
                {
                    // This code is copied from the jQuery implementation. It is not unit tested for correctness,
                    // only that it is defined and it does not throw an error.
                    // This is simply to maintain parity with the current tracking implementation.

                    var documentElement = document.documentElement;
                    var retVal = 0;

                    // Don't call addBehavior() secoConf Room MSN Private Focus Rm-13073 (4)� AV Webcamnd time, it will return error. So check, if isHomePage has been already added.
                    // This scenario arises, when this method is called multiple times.
                    if (documentElement.addBehavior &&  // This filters out no-IE browsers. This method is supported by IE only.
                    (isHomePageBehaviorAdded ||         // Check, if behavior has been already added.
                    (documentElement.addBehavior("#default#homePage") && (isHomePageBehaviorAdded = 1))))
                        // If addBehavior() fails, the isHomePageBehaviorAdded will not be set. behaviorAdded will be 1 only if addBehavior() returns success.
                        // Check the return value. This method returns behavior ID > 0.
                    {
                        try
                        {
                            //IE 9 throws an error when we check for typeof element.isHomePage but returns the value correctly
                            //when we call the function directly, hence adding a try catch for other browsers.
                            retVal = (documentElement.isHomePage(window.location.href)) ? "Y" : "N";
                        }
                        catch (e) { }
                    }
                    return retVal;
                },
                timeStamp: function ()
                {
                    ///	<summary>
                    ///		generates the timestamp for the url
                    ///	</summary>
                    ///	<returns type="String">
                    /// </returns>
                    return +new Date;
                },
                AOP: function ()
                {
                    /// <summary>
                    ///    It returns a list of Aops separated by cmSeperator.
                    /// </summary>
                    /// <returns type="String">
                    /// </returns>

                    // When trackInfo is not extended, curAOP won't be a part of it (basically at page load time). Check if trackInfo.curAOP is null or not.
                    // If curAop is null, don't do any processing.
                    if (trackInfo.curAop != null)
                    {
                        // Further, check if curAOP has any cached value. If value is cached, return cached value.
                        if (trackInfo.curAop == "")
                        {
                            trackInfo.curAop = getAopValue(dom.getTarget(trackInfo.event)) || "";
                        }
                    }

                    return trackInfo.curAop;
                },
                slideType: function ()
                {
                    /// <summary>
                    ///     Finds the slide type if any (and if a slide)
                    /// </summary>
                    /// <returns type="String">"halfpane" or null</returns>
                    var elem = trackInfo.event.target;
                    var halfpaneClass = "halfpane";
                    while (elem && dom.name(elem) != "SECTION")
                    {
                        if (dom.name(elem) == "LI" && dom.containsClass(elem, halfpaneClass))
                        {
                            return halfpaneClass;
                        }
                        elem = dom.parent(elem);
                    }
                    return null;
                },
                ///	<summary>
                ///		generates the sequential number of the current event.
                ///   This value gets incremented for each event that is triggered
                ///	</summary>
                eventNumber: 0
            }
        };

        return trackInfo;
    });

/* WGINCLUDE: C:\PROJECTS\WORKSPACE2\MSNMETRO\MAIN\METROSDK\METROSDK\CONTENT\SOURCE\JS\CLASSIC\..\Shared\Tracking\trackInfoMobile.js */
/////////////////////////////////////////////////////////////////////////////////
//
// File: trackInfoMobile.js
// Defines:
//       Extensions:       
//                "trackInfo".client.scrW,
//                "trackInfo".client.scrH,
//                "trackInfo".client.orientation,
//                "trackInfo".client.userAgent,
//                "trackInfo".client.hourOfDay,
//                "trackInfo".client.pageTitle,
//                "trackInfo".client.linkDomain,
//                "trackInfo".client.gesture,
//                "trackInfo".userDynamic.requestId,
//       Events:
//                "track".trackGesture
//
// Dependencies: track.js, trackInfo.js, dom.js
// Description: Implements mobile specific extensions to track 
//
// Copyright (c) 2012 by Microsoft Corporation. All rights reserved.
//
/////////////////////////////////////////////////////////////////////////////////

define("c.track.mobi", ["track", "trackInfo", "screen", "navigator", "dom", "bind", "document"], function (track, trackInfo, screen, navigator, dom, bind, document)
{
    var nonNavClckEvt = "click_nonnav";
    var gestureKey = "touch_gesture";

    var gestureTouchType = false, gestureTouchDateTime = 0, gestureTouchDelta = 1000;

    track.extend(
    {
        client:
        {
            scrW: function ()
            {
                return screen.width;
            },
            scrH: function ()
            {
                return screen.height;
            },
            orientation: function ()
            {
                return screen.width > screen.height ? "landscape" : "portrait";
            },
            userAgent: function ()
            {
                return navigator.userAgent;
            },
            hourOfDay: function ()
            {
                return new Date().getUTCHours();
            },
            linkDomain: function ()
            {
                var destinationUrl = trackInfo.report ? trackInfo.report.destinationUrl : null;
                return (destinationUrl && destinationUrl.length) ? getDomain(destinationUrl) : null;
            },
            pageTitle: function ()
            {
                return document.title;
            },
            gesture: function ()
            {
                ///	<summary>
                ///		Extension method to get the touch gesture
                ///	</summary>
                ///	<returns type="String">
                /// </returns>

                // get the target element from the trackInfo event
                var element = dom.getTarget(trackInfo.event);
                // retrieve the gesture stored in the object by the trackGesture method
                if (element && element[gestureKey])
                {
                    return element[gestureKey];
                }

                if (trackInfo.event
                    && (trackInfo.event.type == 'click' || trackInfo.event.type == 'click_nonnav' || trackInfo.event.type == 'submit')
                    && gestureTouchType == true
                    && trackInfo.sitePage.device)
                {
                    return 'tap';
                }

                return 'undefined';
            }
        },
        userDynamic:
        {
            requestId: function ()
            {
                if (trackInfo.userStatic.requestId)
                {
                    return trackInfo.userStatic.requestId;
                }

                var s = [], hexDigits = "0123456789ABCDEF";
                for (var i = 0; i < 32; i++)
                {
                    s[i] = hexDigits.substr(Math.floor(Math.random() * 16), 1);
                }
                s[12] = "4";
                s[16] = hexDigits.substr(s[16] & 3 | 8, 1);
                var uuid = s.join("");
                trackInfo.userStatic.requestId = uuid;
                return uuid
            }
        }
    });

    // the pattern that selects the hostname, the url
    // may or may not contain the protocol
    var hostnameRegex = /^(\w+:\/\/)?([^:\/]*)/;

    function getDomain(url)
    {
        ///	<summary>
        ///		This function extracts the root domain from a given url.
        ///     For www.msn.com it return msn.com
        ///	</summary>
        ///	<returns type="String">
        /// </returns>
        ///	<param name="options" type="Object">
        ///     Options for track Information		
        ///	</param>

        // options for the current execution instance.

        var results = hostnameRegex.exec(url);
        var domainParts = results[results.length - 1].split(".");
        var length = domainParts.length;
        if (length > 1)
        {
            domainParts = domainParts.slice(length - 2);
        }

        return domainParts.join(".");
    };

    function trackGesture(element, gesture, eventName, destination, headline, module, index, campaign)
    {
        ///	<summary>
        ///		This method tracks touch gestures.
        ///     Stores the gesture as a property on the element and calls the track.trackEvent to fire the tracking calls.
        ///	</summary>
        ///	<param name="element" type="Object" optional="false">
        ///     element that fires the  or target of the event. Passed to createReport.
        ///	</param>
        ///	<param name="gesture" type="String" optional="false">
        ///     the touch gesture that needs to be tracked
        ///	</param>        
        ///	<param name="destination" type="String" optional="true">
        ///     destination url to use instead of computing. Passed to createReport.
        ///	</param>
        ///	<param name="headline" type="String" optional="true">
        ///     headline to use instead of computing. Passed to createReport.
        ///	</param>
        ///	<param name="module" type="String" optional="true">
        ///     content module string to be used instead of computing. Passed to createReport.
        ///	</param>
        ///	<param name="index" type="String" optional="true">
        ///     content index string to be used instead of computing. Passed to createReport.
        ///	</param>
        ///	<param name="campaign" type="String" optional="true">
        ///     campaign id to be used instead of computing. Passed to createReport.
        ///	</param>

        if (element)
        {
            // store the gesture as a property on the object
            element[gestureKey] = gesture;

            // if no event name is provided by default, the event name is set to "click_nonnav"
            eventName = (eventName && eventName.length > 0) ? eventName : nonNavClckEvt;

            // create a new event with the element as the target
            var event = track.createEvent(null, eventName, element);

            // fire the track event call
            track.trackEvent(event, element, destination, headline, module, index, campaign);
        }
    }

    // Check if the MSPointer is supported by browser else use the touchend event to detect touchtype
    if (window.navigator.msPointerEnabled)
    {
        bind(document, "MSPointerUp", function (event)
        {
            // checking if pointertype is touch
            // http://msdn.microsoft.com/en-us/library/ie/hh772359(v=vs.85).aspx
            if (event.pointerType == event.MSPOINTER_TYPE_TOUCH)
            {
                gestureTouchType = true;
            }
            else
            {
                gestureTouchType = false;
            }
        });
    }
    else
    {
        bind(document, "mouseup", function ()
        {
            // Verify whether the event is fired due to touch or mouse up.
            // If only mouseup, the getTime will be greater than gestureTouchDateTime
            // If touchend and then mouseup, the datetime will be less the range provided in check.
            var currentTime = new Date().getTime();
            if (currentTime > gestureTouchDateTime + gestureTouchDelta)
            {
                gestureTouchType = false;
            }
        });

        bind(document, "touchend", function ()
        {
            gestureTouchType = true;

            // Set the datetime value and once the mouseup event is fired 
            // check if the mouseup event is fired approximately at the same time.
            gestureTouchDateTime = new Date().getTime();
        });
    }

    track.trackGesture = trackGesture;
    return 1;
});


/* WGINCLUDE: C:\PROJECTS\WORKSPACE2\MSNMETRO\MAIN\METROSDK\METROSDK\CONTENT\SOURCE\JS\CLASSIC\..\Shared\mediator.js */
/////////////////////////////////////////////////////////////////////////////////
//
// File: mediator.js
// Defines: mediator
// Dependencies: AMD
// Description: Sub/Pub (also with channels)
//
//              pass data to any callback that has subscribed to the event
//              mediator.pub(event, data);
//
//              call the callback whenever this event is published
//              mediator.sub(event, callback)
//
//              pass data to any callback that has subscribed to this event and channel
//              mediator.pubChannel(event, channel, data);
//
//              call the callback whenever this event is published to this channel
//              mediator.subChannel(event, channel, callback)
//
//              Think of pub/sub as using their own channel, useful for 1 ofs or app communication.
//              Channels are useful when multiple copies of the same widget want to communicate.
//
// Copyright (c) 2012 by Microsoft Corporation. All rights reserved.
// 
//////////////////////////////////////////////////////////////////////////////////*

define("mediator", function ()
{
    var defaultPubSub = new PublishSubscribe();
    var channels = {};
    return {
        pub: defaultPubSub.pub,
        sub: defaultPubSub.sub,
        unsub: defaultPubSub.unsub,
        pubChannel: function (event, channel, data)
        {
            getChannel(channel).pub(event, data);
        },
        subChannel: function (event, channel, callback)
        {
            getChannel(channel).sub(event, callback);
        },
        unsubChannel: function (event, channel, callback)
        {
            getChannel(channel).unsub(event, callback);
        }
    };

    function getChannel(channel)
    {
        if (!channels[channel])
        {
            channels[channel] = new PublishSubscribe();
        }
        return channels[channel];
    }

    function PublishSubscribe()
    {
        var pubMap = {};
        return {
            pub: function (event, data)
            {
                var callbacks = pubMap[event];
                if (callbacks)
                {
                    for (var ndx = 0; ndx < callbacks.length; ndx++)
                    {
                        callbacks[ndx](data);
                    }
                }
            },
            sub: function (event, callback)
            {
                if (typeof callback == "function")
                {
                    var listeners = pubMap[event];

                    if (!listeners)
                    {
                        listeners = [];
                        pubMap[event] = listeners;
                    }
                    listeners.push(callback);
                }
            },
            unsub: function (event, callback)
            {
                var listeners = pubMap[event];

                if (listeners)
                {
                    for (var ndx = 0; ndx < listeners.length; ndx++)
                    {
                        if (listeners[ndx] === callback)
                        {
                            listeners.splice(ndx--, 1);
                        }
                    }
                }
            }
        };
    }
});


/* WGINCLUDE: C:\PROJECTS\WORKSPACE2\MSNMETRO\MAIN\METROSDK\METROSDK\CONTENT\SOURCE\JS\CLASSIC\..\Shared\navigation.js */
/////////////////////////////////////////////////////////////////////////////////
//
// File: navigation.js
// Defines: navigation
// Dependencies: none
// Description: implements navigation helpers
//
// Copyright (c) 2012 by Microsoft Corporation. All rights reserved.
//
/////////////////////////////////////////////////////////////////////////////////

define("navigation", ["document", "location"], function (document, location)
{
    var domainMatchPattern = /[a-z][a-z0-9+\-.]*:\/\/([a-z0-9\-._~%!$&'()*+,;=]+@)?([a-z0-9\-._~%]+|\[[a-z0-9\-._~%!$&'()*+,;=:]+\])/i;
    return {
        navigate: function (url, replace)
        {
            if (this.filter)
            {
                url = this.filter(url);
            }
            if (replace)
            {
                location.replace(url);
            }
            else
            {
                location.href = url;
            }
        },
        getHostName: function (url)
        {
            var domainMatch = domainMatchPattern.exec(url);
            return domainMatch ? domainMatch[2] : false;
        },
        isLocal: function (url)
        {
            return (location.hostname == this.getHostName(url));
        },
        getParamsFromUrl: function (url)
        {
            var params = {};
            var queryString = url.split("?")[1];
            if (queryString)
            {
                var queryArray = queryString.split("&");
                for (var ndx = 0; ndx < queryArray.length; ndx++)
                {
                    var parts = queryArray[ndx].split("=");
                    params[parts[0]] = parts[1];
                }
            }
            return params;
        },
        filter: null
    };
});

/* WGINCLUDE: C:\PROJECTS\WORKSPACE2\MSNMETRO\MAIN\METROSDK\METROSDK\CONTENT\SOURCE\JS\CLASSIC\..\Shared\safeCss.js */
/////////////////////////////////////////////////////////////////////////////////
//
// File: safeCss.js
// Defines: safeCSS
// Dependencies: AMD, jQuery
// Description: Allows manages css changes and allows for them to be undone.
//              
//              // create a group
//              var safeCssGroup = safeCss.createGroup();
//              
//              // set css values
//              safeCssGroup($elem).css(args);
//              ...
//
//              // reset group
//              safeCssGroup.reset();
//
//              // or just reset the element
//              safeCssGroup($elem).reset();
//
// Copyright (c) 2012 by Microsoft Corporation. All rights reserved.
//
/////////////////////////////////////////////////////////////////////////////////

define("safeCss", ["jquery"], function ($)
{
    // number to use then increment when creating a new group
    var nextGroupId = 1;
    var idDelimiter = ".";

    // not using prototype so that scope can hide internal variables

    // wrapped set that can change css properties and later reset them
    function SafeCssSet($set)
    {
        // object used to reset with, built up as properties are set
        var resetObj = {};

        // used in the resetObj, allows the browser to optimize if used by reference
        var emptyString = "";

        // passthrough to jQuery.fn.css function
        // remember if a property is set so it can be reset
        this.css = function (key, value)
        {
            if (value)
            {
                // single property set
                resetObj[key] = emptyString;
            }
            else if (typeof key == "object")
            {
                // object passed in
                for (var prop in key)
                {
                    resetObj[prop] = emptyString;
                }
            }
            $set.css.apply($set, arguments);
            return this;
        };

        this.hide = function ()
        {
            throw "not implemented";
        };

        this.show = function ()
        {
            throw "not implemented";
        };

        this.toggle = function ()
        {
            throw "not implemented";
        };

        // reset all css properties previously set
        this.reset = function ()
        {
            $set.css(resetObj);

            // clear resetObj
            resetObj = {};
        };
    }

    // object with createGroup method
    return {
        // group of elements that can be reset at once
        createGroup: function ()
        {
            // ensure a unique groupIdLabel
            var groupIdLabel = "safeCssId" + nextGroupId++;

            // number to use then increment when mapping an element
            var nextElemId = 1;
            var setMap = {};

            // gets SafeCssElement object
            function result($set)
            {
                // need to loop through all elements otherwise both
                // groups and individal elements in a wrapped set
                // cannot be used together correctly
                var ids = [];
                $set.each(function ()
                {
                    var $elem = $(this);
                    var elemId = $elem.data(groupIdLabel);
                    // if it doesn't have an id, assign one
                    if (!elemId)
                    {
                        elemId = nextElemId++;
                        $elem.data(groupIdLabel, elemId);
                    }
                    ids.push(elemId);
                });
                var setId = ids.join(idDelimiter);
                var safeCssElement = setMap[setId];
                if (!safeCssElement)
                {
                    // create the SafeCssElement object and store in map
                    safeCssElement = new SafeCssSet($set);
                    setMap[setId] = safeCssElement;
                }

                // return the SafeCssElement object
                return safeCssElement;
            };

            // reset all css properties that have been set
            result.reset = function ()
            {
                for (var id in setMap)
                {
                    setMap[id].reset();
                }
            };

            return result;
        }
    };
});

/* WGINCLUDE: C:\PROJECTS\WORKSPACE2\MSNMETRO\MAIN\METROSDK\METROSDK\CONTENT\SOURCE\JS\CLASSIC\..\Shared\Implementations\tabKeyPressed.js */
/////////////////////////////////////////////////////////////////////////////////
//
// File: tabKeyPressed.js
// Defines: tabKeyPressed
// Dependencies: jQuery
// Description: Listens to keys and remembers if the tab key is pressed
//
// Copyright (c) 2012 by Microsoft Corporation. All rights reserved.
// 
//////////////////////////////////////////////////////////////////////////////////*

define("tabKeyPressed", ["jquery"], function ($)
{
    var isTabPressed = false;
    $(document).on("keydown", function (event)
    {
        if (event.keyCode == 9)
        {
            // if tab is pressed
            isTabPressed = true;
        }
    }).on("keyup", function (event)
    {
        if (event.keyCode == 9)
        {
            // if tab is unpressed
            isTabPressed = false;
        }
    });

    return function ()
    {
        return isTabPressed;
    };
});



/* WGINCLUDE: C:\PROJECTS\WORKSPACE2\MSNMETRO\MAIN\METROSDK\METROSDK\CONTENT\SOURCE\JS\CLASSIC\..\Shared\behaviors\hover.js */
/////////////////////////////////////////////////////////////////////////////////
//
// File: hover.js
// Defines: hover
// Dependencies: jQuery, jqfn, mediator, navigator, tabKeyPressed
// Description: implements the basic hover behavior hat allows the client to specify the elements and some other related behavior.
//
// Copyright (c) 2013 by Microsoft Corporation. All rights reserved.
//
/////////////////////////////////////////////////////////////////////////////////

define("hover", ["jquery", "jqfn", "mediator", "navigator", "tabKeyPressed"], function ($, jqfn, mediator, navigator, tabKeyPressed)
{
    var defaults =
    {
        // the actual target element that will be added/removed the hover class.
        // the client of hover can override the element, default is the bound element.
        hoverClassTarget: null, 
        autoHide: true,
        hoverClass: "hover",
        // some tricky logic here, if clickAutoHideTimeout > 0, then we will hide via timeout after the click.
        // otherwise, it will only hide on the second click.
        clickAutoHideTimeout: 2500,
        preventClick: true,
    };

    var hoverId = "hoverId";
    var nextId = 1;

    function applyBehavior($elem, settings)
    {
        var pubId = nextId++;
        var hideTimeout;
        var $hoverClassTarget = settings.hoverClassTarget || $elem;

        // set the data
        $elem.data(hoverId, pubId);

        function show(event)
        {
            $hoverClassTarget.addClass(settings.hoverClass);
            mediator.pubChannel("hoverShow", pubId, event);
        }

        function hide(event)
        {
            // always clear timeouts...
            hideTimeout && clearTimeout(hideTimeout);
            $hoverClassTarget.removeClass(settings.hoverClass);
            mediator.pubChannel("hoverHide", pubId, event);
        }

        function click(event)
        {
            if ($hoverClassTarget.hasClass(settings.hoverClass))
            {
                // we hide on second click immediately.
                hide(event);
            }
            else
            {
                if (settings.preventClick && event)
                {
                    // TODO: (binzy) check whether it has the "hover" class to support navHover -> next step.
                    event.preventDefault();
                    event.stopImmediatePropagation();
                }

                // only call show when it's not active.
                show(event);
                
                // if clickAutoHideTimeout == 0 then we will not auto hide for click.
                if (settings.autoHide && settings.clickAutoHideTimeout > 0)
                {
                    hideTimeout = setTimeout(hide, settings.clickAutoHideTimeout, event);
                }
            }
        }

        function focusIn(event)
        {
            if (tabKeyPressed())
            {
                show(event);
            }
        }

        function focusOut(event)
        {
            // don't care whether tabkey pressed.
            hide(event);
        }

        return {
            setup: function ()
            {
                if (navigator.msPointerEnabled)
                {
                    // use MSPointer not mouse
                    $elem.on("MSPointerOver", show);
                    settings.autoHide && $elem.on("MSPointerOut", hide);
                }
                else
                {
                    // use mouse
                    $elem.on("mouseenter", show);
                    settings.autoHide && $elem.on("mouseleave", hide);
                }

                // click: for both real mouse click or touch click
                $elem.on("click", click);

                // focus
                $elem.on("focusin", focusIn);

                settings.autoHide && $elem.on("focusout", focusOut);
            },
            teardown: function ()
            {
                if (navigator.msPointerEnabled)
                {
                    $elem.off("MSPointerOver", show);
                    settings.autoHide && $elem.off("MSPointerOut", hide);
                }
                else
                {
                    $elem.off("mouseenter", show);
                    settings.autoHide && $elem.off("mouseleave", hide);
                }

                $elem.off("click", click);
                $elem.off("focusin", focusIn);
                settings.autoHide && $elem.off("focusout", focusOut);
            }
        };
    };

    var hover = jqfn(applyBehavior, defaults);

    // return hoverId for retrieving the data attribute.
    hover.hoverId = hoverId;

    return hover;
});

/* WGINCLUDE: C:\PROJECTS\WORKSPACE2\MSNMETRO\MAIN\METROSDK\METROSDK\CONTENT\SOURCE\JS\CLASSIC\..\Shared\Behaviors\swipeNavUtils.js */
/////////////////////////////////////////////////////////////////////////////////
//
// File: swipeNavUtils.js
// Defines: swipeNavUtils
// Dependencies: jquery, mediator
// Description: Common objects used during swipe navigation
//
//              SnapPointManager:   calculates and manipulates snap points
//              TouchData:          uses a filter to take position input and generate a smothed position
//                                  as well as inertia data like drift and timing functions
//              CarouselManager:    Abstraction for handling static circular arrays
//                                  May end up removing this as part of these refactorings
//
// Copyright (c) 2012 by Microsoft Corporation. All rights reserved.
// 
//////////////////////////////////////////////////////////////////////////////////*



define("swipeNavUtils", ["jquery", "mediator"], function ($, mediator)
{
    var delta = 10;

    mediator.sub("setDelta", function (value) { delta = value; });

    // bezier tabled defined by (0, 0), (0, x1), (0.58, 1), (1, 1)
    // since table is (t, x) values, I'll use u rather than the standard t to step through the curve
    var cubicBezierTableCache = {};
    var cubicBezierTablePrecision = 0.1;
    var currentCubicBezierTable;
    var cubicBezierTableUValues = [];

    // build up a list of u values to sample
    for (var uValue = 0; uValue <= 1; uValue += 0.01)
    {
        cubicBezierTableUValues.push(uValue);
    }

    // set up custom easing function that will change (depending on velocity)
    $.easing.cubicBezier = function (t)
    {
        if (t == 1)
        {
            return 1;
        }
        var t0, t1, x0, x1, values;

        // skip the last one as it's covered by the return above
        var ndx = currentCubicBezierTable.length - 1;
        var x = 0;
        while (ndx-- && !x)
        {
            values = currentCubicBezierTable[ndx];
            t0 = values.t;
            if (t0 <= t)
            {
                // do linear approximation
                x0 = values.x;
                values = currentCubicBezierTable[ndx + 1];
                t1 = values.t;
                x1 = values.x;
                x = ((t - t0) / (t1 - t0)) * (x1 - x0) + x0;
            }
        }

        return x;
    };

    // generates snap points and padding values that should be used if needed
    function snapPointManager($children, $container)
    {
        var snapIntervals = [0];
        var widths = [];
        var hasEndSlate = $children.eq(-1).hasClass("lastslide");
        var lastWidthUpdate = 0;
        var hasChanged;

        // returns a boolean describing if the call to getSnapPoint() would return a different value
        this.hasChanged = function ()
        {
            // don't recalculate this if called repeatedly
            if (+new Date - lastWidthUpdate > 10)
            {
                hasChanged = false;

                $children.each(function (ndx)
                {
                    var width = $(this).width();
                    var diff = widths[ndx] ? width - widths[ndx] : 20;
                    if (diff > delta || diff < -delta)
                    {
                        widths[ndx] = width;
                        hasChanged = true;
                    }
                });

                lastWidthUpdate = +new Date;
            }

            return hasChanged;
        };

        // recalculates and gets the array of snap points
        this.getSnapPoints = function ()
        {
            var lastCenterOffset;
            var currentCenterOffset;

            // ensure that width array has correct values
            this.hasChanged();

            $children.each(function (ndx)
            {
                lastCenterOffset = currentCenterOffset;
                // find the offset + half the width to target the center of the image
                currentCenterOffset = (($(this).offset().left + (widths[ndx] / 2) + 1) | 0);
                if (ndx != 0)
                {
                    // set current snap interval to be the last snap interval plus the difference of the current center and the last center
                    snapIntervals[ndx] = snapIntervals[ndx - 1] + currentCenterOffset - lastCenterOffset;
                }
            });

            if (hasEndSlate)
            {
                // if there's an end slate, don't center it
                snapIntervals[$children.length - 1] -= this.getPaddingRight();
            }

            return snapIntervals;
        };

        // gets the paddingleft required to center the first item
        this.getPaddingLeft = function ()
        {
            var totalWidth = $container.outerWidth(true);
            return (totalWidth - $children.eq(0).width()) / 2;
        };

        // get the right padding required to center the last item
        this.getPaddingRight = function ()
        {
            var totalWidth = $container.outerWidth(true);
            return (totalWidth - $children.eq(-1).outerWidth()) / 2;
        };

        // boolean value indicating if the last item is an end slate (not centered but right aligned)
        this.hasEndSlate = function ()
        {
            return hasEndSlate;
        };

        // gets that closest scroll point from the inputed value
        this.getClosestScrollPoint = function (scrollLeft)
        {
            var minDistance = Infinity;
            var closestSnapPoint = scrollLeft;
            for (var ndx = 0; ndx < snapIntervals.length; ndx++)
            {
                var distance = scrollLeft - snapIntervals[ndx];
                if (distance < 0)
                {
                    distance = -distance;
                }
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestSnapPoint = snapIntervals[ndx];
                }
            }
            return closestSnapPoint;
        };
    }

    function touchData()
    {
        var alpha = 0.7;
        var beta = 0.1;
        var initialPosition;
        var initialTime;
        var lastTime;
        var position;
        var velocity;

        // reset the at the calibration position and 0 velocity
        this.reset = function (calibration)
        {
            initialPosition = calibration;
            initialTime = +new Date;
            lastTime = initialTime;
            position = initialPosition;
            velocity = 0;
        };

        // An alpha beta filter that takes in position values
        // and smooths out the position as well as giving velocity information.
        // http://en.wikipedia.org/wiki/Alpha_beta_filter
        this.input = function (positionInput)
        {
            // get time delta in ms
            var curTime = +new Date;
            var deltaTime = curTime - lastTime;
            lastTime = curTime;

            // estimate position
            position += velocity * deltaTime;

            // get error
            var error = positionInput - position;

            // apply filter
            position += alpha * error;
            velocity += (beta * error) / deltaTime;
        };

        // position in input unit
        this.getPosition = function ()
        {
            return position;
        };

        // velocity in input unit / ms
        this.getVelocity = function ()
        {
            return velocity;
        };

        // delta from last reset in input unit
        this.getDelta = function ()
        {
            return position - initialPosition;
        };

        // gets the duration that inerita should last
        // NOTE: can't find a correlation with with the data I got from IE10 snap points yet
        // but I think there is a correltion to be found when I have more time to look at it
        this.getDuration = function ()
        {
            return 220;
        };

        // gets the drift amount during inertia
        // totalWidth - the visiable container width
        // localWidth - the width of the current slide (used if a swipe is detected to not go more than 1 slide away)
        this.getDrift = function (totalWidth, localWidth)
        {
            var displacement = this.getDelta();
            if (displacement < 0)
            {
                displacement = -displacement;
            }
            var maxDriftCoeficient = 1 - displacement / totalWidth;
            var minCoeficient = .2;
            var maxDrift = totalWidth * (maxDriftCoeficient > minCoeficient ? maxDriftCoeficient : minCoeficient);

            var drift = velocity * 150;

            // checks to see if this qualifies as a swipe
            var isSwipe = (lastTime - initialTime) < 300 && (velocity > 0.3 || velocity < -0.3);
            if (isSwipe)
            {
                // emulate swipe behavior
                var maxSwipeWidth = localWidth;
                var maxSwipeDrift = maxSwipeWidth - displacement;
                drift = (velocity > 0 ? 1 : -1) * (maxSwipeDrift < maxDrift ? maxSwipeDrift : maxDrift);
            }
            else if (drift > maxDrift)
            {
                drift = maxDrift;
            }
            else if (drift < -maxDrift)
            {
                drift = -maxDrift;
            }

            return drift;
        };

        // gets the CSS3 timing function
        this.getTimingFunction = function ()
        {
            return "cubic-bezier(0," + getTimingPoint1X(velocity) + ",.58,1)";
        };

        // call this before doing a jQuery animation
        // sets the jQuery easing function "cubixBezier" to use current values
        this.ensureJQueryEase = function ()
        {
            var point1X = getTimingPoint1X(velocity);
            // rounding point1X so that cache might be useful
            point1X = Math.round(point1X / cubicBezierTablePrecision) * cubicBezierTablePrecision;
            if (!cubicBezierTableCache[point1X])
            {
                cubicBezierTableCache[point1X] = generateCubicBezierTable(point1X);
            }
            currentCubicBezierTable = cubicBezierTableCache[point1X];
        };

        // give some real values incase this is used without reset being called first
        this.reset(0);
    }

    // gets the Point1 x value for the timing function
    function getTimingPoint1X(velocity)
    {
        var speed = velocity > 0 ? velocity : -velocity;
        return (speed * 0.3);
    }

    // generate a lookup table for doing cubicBezier animations in jQuery
    function generateCubicBezierTable(point1X)
    {
        var table = [];
        var u, x, t, uInverted;
        var ndx = cubicBezierTableUValues.length;
        while (ndx--)
        {
            u = cubicBezierTableUValues[ndx];
            uInverted = 1 - u;
            t = (uInverted * u * u) * 0.58 + (u * u * u);
            x = (uInverted * uInverted * u) * point1X + (uInverted * u * u) + (u * u * u);
            table[ndx] = { x: x, t: t };
        }
        return table;
    }


    // Abstraction for handling static circular arrays
    //
    // example usage
    //
    // require("swipeNavUtils", function(swipeNavUtils)
    // {
    //     // ...
    //     var carouselManager = new swipeNavUtils.CarouselManager();
    //     carouselManager.load(array);
    //
    //     // get the 3rd item
    //     carouselManager.getItem(3);
    //
    //     // shift the carousel back by 2
    //     carouselManager.changeIndex(-2);
    //
    //     // get the 1st item, 3rd from current index
    //     carouselManager.getItem(3);
    //
    //     // reset the carousel
    //     carouselManager.setIndex(0);
    //
    //     // get the last item
    //     carouselManager.getItem(-1);        
    // });
    function carouselManager()
    {
        var array = [];
        var index = 0;
        this.load = function (arrayParam)
        {
            array = arrayParam;
        };
        this.getItem = function (offset)
        {
            if (!array.length)
            {
                return null;
            }
            var targetIndex = (index + offset) % array.length;
            return array[targetIndex < 0 ? targetIndex + array.length : targetIndex];
        };
        this.setIndex = function (value)
        {
            index = value;
        };
        this.changeIndex = function (offset)
        {
            index += offset;
        };
    }

    return {
        SnapPointManager: snapPointManager,
        TouchData: touchData,
        CarouselManager: carouselManager
    };
});

/* WGINCLUDE: C:\PROJECTS\WORKSPACE2\MSNMETRO\MAIN\METROSDK\METROSDK\CONTENT\SOURCE\JS\CLASSIC\..\Shared\Behaviors\autosuggest.tokens.js */
/////////////////////////////////////////////////////////////////////////////////
//
// File: autosuggest.tokens.js
// Defines: autosuggest.tokens
// Description: Defines tokens used in autosuggest.js
//
// Copyright (c) 2013 by Microsoft Corporation. All rights reserved.
//
/////////////////////////////////////////////////////////////////////////////////

define("autosuggest.tokens", {
    resourceJs: "%AutoSuggest.Resources.Js%",
    helpLinkText: "%AutoSuggest.HelpLinkText%",
    helpLinkUrl: "%AutoSuggest.HelpLinkUrl%",
    market: "%AutoSuggest.Market%",
    popularNowText: "%AutoSuggest.PopularNowText%",
    enablePopularNow: %AutoSuggest.EnablePopularNow%,
    bingHelp: "%AutoSuggest.BingHelp%",
    disableText: "%AutoSuggest.DisableText%",
    enableText: "%AutoSuggest.EnableText%"
});

/* WGINCLUDE: C:\PROJECTS\WORKSPACE2\MSNMETRO\MAIN\METROSDK\METROSDK\CONTENT\SOURCE\JS\CLASSIC\..\Shared\Behaviors\autosuggest.js */
/////////////////////////////////////////////////////////////////////////////////
//
// File: autosuggest.js
// Defines: autosuggest
// Dependencies: autosuggest.tokens, jquery, jqfn, mediator, getCookie, setCookie, track, unbind, format, window, document
// Description: implements an interface to include autosuggest feature on a page.
//              It's dependent on autosuggest_external.js that we get from Bing team.
//
//              Note that the form must have data-form-code value (such as "MSN005") for this to work correctly.
//              If you are using this with the TMX SDK, this should be taken care of automatically.
//
// Copyright (c) 2013 by Microsoft Corporation. All rights reserved.
//
/////////////////////////////////////////////////////////////////////////////////

define(
    "autosuggest",
    ["autosuggest.tokens", "jquery", "jqfn", "mediator", "getCookie", "setCookie", "track", "unbind", "format", "window", "document"],
    function (tokens, $, jqfn, mediator, getCookie, setCookie, track, unbind, format, window, document)
    {
        ///	<summary>
        ///	  This function binds the autosuggest feature to the websearch textbox.
        ///        The actual autoSuggest is implemented in an external file which is provided by the bing team.
        ///        This method downloads that file and initializes that functionality.
        ///	</summary>

        // default values
        var defaults =
        {
            resources:
                {
                    js: tokens.resourceJs
                },

            // config is a global variable that is passed to the Autosuggest javascript.
            // Only localized text should be set here.
            config:
                {
                    // array of link data for links displayed at the bottom of the dropdown (text, url, new-window[,text,url,new-window])
                    l: [tokens.helpLinkText, tokens.helpLinkUrl, 1],
                    // the javascript resource for autosuggest
                    r: "AutoSugShared",
                    // api options (should always be s+ for pa
                    o: "s+a+p+hs+",
                    // hit-highlighting enabled
                    h: 1,
                    // min keystrokes required to fetch suggestions
                    k: 0,
                    // max number of suggestions to show
                    m: 8,
                    // typing delay (soon to be deprecated, but still required)
                    d: 100,
                    // Url to autosuggest
                    u: "http://api.bing.com/qsonhs.aspx?form={0}",
                    // market
                    mkt: tokens.market,
                    // load AutoSuggest script onload (1 true, 0 false)
                    ol: 1,
                    // text for Popular Now
                    tPN: tokens.popularNowText,
                    // enable ???
                    eLO: 1,
                    // enable search history
                    eHS: 1,
                    // enable Popular Now (doesn't exist in all markets)
                    ePN: tokens.enablePopularNow,
                    // ???
                    nw: "true",
                    // ???
                    lh: tokens.bingHelp,
                    // Manage search history link, equivalent to false won't display the link
                    lmh: 0
                },

            // A text that will be displayed for a user to turn off autosuggest.
            // Should be localized in resource file if needed.
            disableText: tokens.disableText,

            // A text that will be displayed for a user to turn on autosuggest.
            // Should be localized in resource file if needed.
            enableText: tokens.enableText,

            // ID of an input box, which will have an autosuggest.
            inputId: "q",

            // A flag that tells either to open a Help link in new window. 1: Open in a new window, 0: Open in the same window.
            openNew: "1",

            // Market name.
            market: tokens.market,

            // Autosuggest cookie expiry days.
            cookieExpiry: 365,

            // Autosuggest cookie domain.
            cookieDomain: "msn.com",

            // 0 = immediate; 1 = onPageLoad, 2 = onUserEngagement
            delayBind: 1, // Not configurable by customers for Metro.

            sharedCk:
                {
                    // when should we bind the iframe call for search history?
                    // 0 = immediate; 1 = onPageLoad, 2 = onUserEngagement // default 2, but set to 1 in EN-US homepage binding so setting to 1 for now.
                    // The delay also depends on the delayBind defined before, if the delayBind is onLoad, this can only fire after onLoad but never before            
                    delay: 1,  // Not configurable by customers for Metro.

                    // return url for the bing iframe to call 
                    ru: "http://" + location.host + "/sck.aspx&form={0}",

                    // Iframe url to call on bing domain which will return us the cookie
                    pu: "http://www.bing.com/sck",

                    // Cookie names that will be requested from bing.com (+) delimited eg: cookie1+cookie2
                    cn: "_SS",

                    // The cookie domain used by the shared cookie. By default it's empty which means the current web page URL host will be used as the domain.
                    domain: "msn.com",

                    // Call functions when SharedCookie is updated. Syntax: function() { "codebinding section" }
                    onCk: function ()
                    {
                        // TODO: tdickens- get search history to work
                        //$("#srchfrm .opt").openSearchHistory({
                        //    piiurl: "http://www.bing.com/search"
                        //});
                    }
                },

            // An ID of a container that contains a menu item, which enables to turn on/off an autosuggest, in menu bar. 
            toggleSelector: "#asugoff"
        };
        var autosuggestEvents = {
            beforeSubmit: "autosuggestBeforeSubmit",
            preventSubmit: "autosuggestPreventSubmit"
        };

        var bindingFunction = jqfn(applyBehavior, defaults);
        bindingFunction.event = autosuggestEvents;
        // only expecting one autosuggest instance, but pass in the $searchInput
        // to maintain some slight seperation. There is still only one sa_inst right now.
        bindingFunction.resize = function ($searchInput)
        {
            if (window.sa_inst)
            {
                window.sa_inst.autosuggest.setQuery($searchInput.val());
            }
        };
        return bindingFunction;

        function applyBehavior($searchForm, settings)
        {
            ///******Moving these out of defaults, since the external file has hardcoded values and these should not be overridden********///             

            // A cookie name that will be stamped if a user selected "Turn off".         
            var cookieName = "SRCHHPGUSR";
            // Autosuggest cookie value prefix.
            // The full value looks like AS=0 when Autosuggest turns off and AS=1 when turn on.
            var cookieCrumb = "AS";

            // A Div ID, that will be added by websearch.xslt template, 
            // if websearch_autosuggest slot metadata has "Enabled" value. 
            var divId = "sw_as";

            ///******Moving these out of defaults, since the external file has hardcoded values and these should not be overridden********///

            // string literal constants.
            var autocompleteText = "autocomplete";
            var autocompleteOnText = "on";
            var autocompleteOffText = "off";
            // denotes var name for this instance of autosuggest UI - needed for callback in JSON response
            var globalObjectName = "sa_inst";
            var sharedCookieSettings = settings.sharedCk;

            // This is an additional check since we allow the return url in the code bindings
            // This will restrict any non-allowed domains
            var allowedReturnUrlDomainRegEx = new RegExp("^http(s?)://[a-zA-z\\d\\-.]+\\.(" + sharedCookieSettings.domain + ")");

            // This variable is to keep track of the the click event
            // in case if the AutoSuggestscript itself is delayBound
            var shouldFireClick = 0;

            // private variable to store the one time use guid
            var oneTimeUseGuid;

            // session id from bing.com
            var partnerSessionId;

            // apply formcode
            settings.config.u = format(settings.config.u, settings.formCode);
            settings.sharedCk.ru = format(settings.sharedCk.ru, settings.formCode);

            // Container of autosuggest toggle text.            
            var $anchorElement = $(settings.toggleSelector);

            // Search Input Element for autocomplete attribute and autosuggest div
            var $searchInputBox = $("#" + settings.inputId, $searchForm);

            // setup listener to prevent searches if needed
            var shouldSubmit;
            mediator.sub(autosuggestEvents.preventSubmit, function ()
            {
                shouldSubmit = false;
            });

            // update the status before so that the toggle is not called the first time.
            updateMenuStatus($anchorElement);

            //bind the status change event
            $anchorElement.click(function (ev)
            {
                ev.stopImmediatePropagation(); // keep the click event logic only excute once
                toggleAutoSuggest(ev, isDisabled()); // enable if disables is true, otherwise disable
                updateMenuStatus($anchorElement);
            });

            // delay bind the initialize function depending on the settings
            delayBind(settings.delayBind, initializeAutoSuggest);

            $.extend(window, { asLoaded: autoSuggestLoaded });

            // scoped functions
            // no other directly executed code should be below this comment

            ///****************************Common functions copied from autoSuggest.js************************************///        
            function isDisabled()
            {
                /// <summary>
                /// A helper function to check whether Autosuggest is enabled/disabled by reading from a cookie.
                /// There is an isDisabled function in autosuggest_external but this function 
                /// has to be defined again since we need to call this before downloading the external file.     
                /// </summary>
                /// <returns type="boolean">1 if disabled, otherwise 0</returns>

                // Regular expression to get value of Autosuggest cookie.
                // Cookie value looks like AS=0 when Autosuggest turns off and AS=1 when Autosuggest turns on.
                var crumbRegex = new RegExp("\\b" + cookieCrumb + "=0\\b", "i");
                return (getCookie(cookieName).match(crumbRegex)) ? 1 : 0;
            }


            function downloadResources(resources, callback, context)
            {
                /// <summary>
                /// downloads the necessary resources async for running the hero player
                /// once download is successful, it will execute the callback function
                /// and pass the context to it as a parameter. If download is unsuccessful
                /// the page will navigate to the videoUrl
                /// </summary>
                /// <param name="resources" type="json">
                /// json object which contains urls of the css and js resources
                /// </param>
                /// <param name="callback" type="function">
                /// function which is called when script is successfully downloaded
                /// </param>                
                /// <param name="context" type="Object">
                /// context which is passed to the callback function
                /// </param>

                if (typeof resources != "undefined")
                {
                    //JS Section
                    var jsFile = resources.js;

                    // to do, once we have the final url,
                    // use $.isAbsoluteUrl(jsFile) to validate the URL
                    if (jsFile && $.isFunction(callback))
                    {
                        $.ajax(
                    {
                        url: jsFile,
                        dataType: 'script',
                        success: function ()
                        {
                            callback(context);
                        }
                    });
                    }
                }
            }


            function addAutoSuggest()
            {
                /// <summary>
                /// This function will add an autosuggest feature on a text box (whose ID is provided in options or defaulted in default).
                /// Full Autosuggest feature has been implemented in a Javascript file that is hosted externally by bing.
                /// This function initializes the sa_autosuggest implemented in that file
                /// </summary>

                //Check to see if the cookie exists, if it does, 
                //the partnerSessionId gets updated
                updateAutoSuggestSessionId();

                // Add the ids to the autosuggest configuration.
                var config = $.extend(true, {},
                {
                    // search form id
                    f: $searchForm.attr("id"),
                    // search input id
                    i: settings.inputId
                }, settings.config,
                {
                    // session id, set to blank initially, otherwise the requests have undefined in them
                    // this should not be overidden by code bindings
                    sid: partnerSessionId || ''
                });

                config.cb = function (inputElement)
                {
                    shouldSubmit = true;
                    // let other scripts know we plan to submit so they can send a preventSubmit request if needed
                    mediator.pub(autosuggestEvents.beforeSubmit, inputElement.value);
                    if (shouldSubmit)
                    {
                        var $form = $(inputElement).parents("form");
                        // $form.submit circumvents form listeners so tracking needs to be called here
                        track.trackEvent({ type: "submit", target: $form[0] });
                        $form.submit();
                    }
                };

                // _G is a global variable that will be consumed in Autosuggest Javascript
                window._G =
                    {
                        "Mkt": settings.market
                    };

                // Call the autosuggest function, if it exists.
                if (typeof window.sa_autosuggest != "undefined")
                {
                    //bind the autoSuggest functionality
                    window[globalObjectName] = new window.sa_autosuggest(config);
                    window[globalObjectName].init(globalObjectName);

                    // hide search history link container (since container is still created even without any content)
                    if (!config.lmh)
                    {
                        $(".sa_om").hide();
                    }

                    //call update session id again to see if the click event has to be fired.
                    updateAutoSuggestSessionId();

                    // if the autosuggest is disabled while page is reloaded, then this will not be called.
                    // if the autosuggest is enabled once on after page load no matter whether it is disabled later (without refresh page),
                    // then our callback will still be called.
                    // so unbind tracking event of search form here to avoid double tracking.
                    if ($searchForm[0])
                    {
                        unbind($searchForm[0], "submit", track.trackEvent);
                    }
                }
            }

            function initializeAutoSuggest()
            {
                /// <summary>
                /// This function 
                ///    - Creates the autoSuggest div if it does not exist
                ///    - Downloads the external script and attaches the autoSuggest functionality to the text box.
                ///    - Sets the correct text in the toggle menu
                ///    - Sets the correct autocomplete attribute to the textbox
                ///
                /// This is the entry point for the autoSuggest functionality. 
                /// Since addAutoSuggest was executed after the script is downloaded, 
                /// user would see autoComplete and suggestions popup together.
                /// Hence, most of the functionality previously in addAutoSuggest is moved here.
                /// </summary>
                /// <returns type="boolean">1 if successful, otherwise 0</returns>

                // Disable IE Autocomplete feature on the input box 
                //if we are going to display suggestions.   
                // Otherwise enable autocomplete
                var disabled = isDisabled();
                $searchInputBox.attr(autocompleteText, disabled ? autocompleteOnText : autocompleteOffText);

                //If the script is already downloaded, directly call enable
                if (typeof window[globalObjectName] != "undefined")
                {
                    // AutoSuggest has a private variable called _bDisabled which needs to 
                    // be reset whenever we toggle autoSuggest                
                    window[globalObjectName].enable(!isDisabled());
                    //check to see if the session id cookie was set and update it
                    updateAutoSuggestSessionId();
                    return 1;
                }

                //The script was never downloaded hence initialize everything                 

                // Check if The formID and inputID provided are valid on the page.
                if ($searchForm[0] && $searchInputBox[0] && !disabled)
                {
                    if (settings.config.asId)
                    {
                        //AutoSugShared file exposes the asId as a parameter to the config object
                        //which will be used to create the dropdown, 
                        //if someone specified that value, use that instead of "sw_as"
                        divId = settings.config.asId;
                    }

                    // Autosuggest needs a Div inside the form element to add autosuggested contents.
                    // Append a Div in the Form element with Div ID in settings
                    $searchForm.append($('<div></div>').attr("id", divId));

                    if (settings.delayBind == 2)
                    {
                        //if the autoSuggest is set to bind on customer engagement, 
                        //then call the cookie sharing iframe as well, so that the search history will be available
                        //by the time the autoSuggest is loaded
                        shouldFireClick = 1;
                        invokeCookieSharing();
                    }
                    else
                    {
                        //otherwise attach to delayBind
                        delayBind(sharedCookieSettings.delay, invokeCookieSharing);
                    }

                    // download the script and provide addAutoSuggest as a callback function
                    downloadResources(settings.resources, addAutoSuggest);

                    // warm up DNS cache by adding an image which wont be added to DOM
                    var primeImage = new Image();
                    primeImage.src = settings.config.u + "&q=";

                    return 1;
                }

                //AutoSuggest was not initialized
                return 0;
            }

            function toggleAutoSuggest(ev, enable)
            {
                /// <summary>
                /// This function will bind to the menu item in page options, which will be used to turn on/off autosuggest.
                /// Called when the anchor element is clicked.
                /// </summary>
                ///	<param name="ev" type="event">
                ///		event that is currently being handled
                ///	</param>
                ///	<param name="enable" type="bool">
                ///		true to enable AutoSuggest otherwise false
                ///	</param>

                var cookieValue = enable ? "1" : "0";

                //make sure the cookie domain is in sync with the external file
                if (!settings.cookieDomain)
                {
                    settings.cookieDomain = setCookie.topDomain;
                }

                setCookie(cookieName, cookieCrumb + "=" + cookieValue, settings.cookieExpiry, settings.cookieDomain, "/");

                //on toggle fake a delayBind scenario, so that the Search History cookie is refreshed as well.
                settings.delayBind = 2;
                initializeAutoSuggest();

                // Prevent the default behaviour of the event.
                ev.preventDefault();
            }

            function generateRandomGuid()
            {
                ///<summary>
                /// This function returns a Guid which is Random
                /// </summary>
                /// <returns type="string">Guid of 32 characters length</returns>

                var guid = '';
                var randomNumber;
                for (var i = 0; i < 32; i++)
                {
                    if (i > 7 && i < 21 && !(i % 4)) //we need a - at 8,12,16 and 20th char
                    {
                        guid += '-';
                    }
                    randomNumber = Math.floor(Math.random() * 16).toString(16).toLowerCase();
                    guid += randomNumber;
                }
                return guid;
            }

            function invokeCookieSharing()
            {
                /// <summary>
                /// This function checks to see if the cookies from bing.com are present, if the cookies aren't there, 
                /// then the iFrame is created to call the url specified in sharedCookiePartnerUrl (default: http://www.bing.com/sck
                /// </summary>
                /// <returns>true, always</returns>

                var cookieNames = sharedCookieSettings.cn.split('+');
                if (cookieNames && cookieNames.length)
                {
                    if (validateString(getCookie(cookieNames[0]), 1))
                    {
                        updateAutoSuggestSessionId();
                        return 1; //there is already a cookie that is set in this session, hence no need to create an iframe
                    }
                    else if (validateString(sharedCookieSettings.cn, 1) && validateString(sharedCookieSettings.ru, 5))
                    {
                        if (!sharedCookieSettings.ru.match(allowedReturnUrlDomainRegEx) || !settings.config.lmh)
                        {
                            // Rogue code bindings, the return url matches a non-allowed domain
                            // or search history not enabled
                            // so do not create the iFrame
                            return 1;
                        }

                        var queryString = "{0}cn={1}&r={2}&h={3}";
                        var url = sharedCookieSettings.pu;
                        oneTimeUseGuid = generateRandomGuid();
                        var suffix = (url.indexOf("?") == -1) ? "?" : "&";
                        url += format(queryString, suffix, sharedCookieSettings.cn, sharedCookieSettings.ru, oneTimeUseGuid);
                        var iFrame = $("<iframe style='width: 0; height: 0; display: none;'></iframe>").attr("src", url);
                        $searchForm.append(iFrame);

                    }
                }
                return 1;
            }


            function delayBind(delay, func)
            {
                /// <summary>
                /// This function calls a provided function at a given delay
                /// This is for performance, the autoSuggest script and the shared cookie are not downloaded until required
                /// </summary>
                /// <param name="delay" type="number(enumeration)">
                ///     delay = 0 means call the function immediately
                ///     delay = 1 means call the function after page is loaded
                ///     delay = 2 means on user interaction with the $searchInputBox
                /// </param>
                /// <param name="func" type="function">
                ///     A parameter less function
                /// </param>

                // since this is a private function, trusting the input
                //if(!$.isFunction(func) || !$searchInputBox || !$searchInputBox.length)  return;

                if (delay == 0) // bind autoSuggest functionality immediately
                {
                    func();
                }
                else if (delay == 1) // fire on load of the page
                {
                    $(document).ready(func);
                }
                else if (delay == 2) // bind on engagement
                {
                    bindOnEngagement(func);
                }
                /*else if (delay == 3)
                {
                //TBD: bind on frequent user engagement
                }*/
            }


            function bindOnEngagement(func)
            {
                /// <summary>
                /// This function calls a given function on customer engagement, either onclick or onkeyup
                /// This ignores escape key and tab key as they are not valid engagement
                /// This is for performance, the autoSuggest script and the shared cookie are not downloaded until required
                /// </summary>
                /// <param name="func" type="function">
                ///     A parameter less function
                /// </param>

                var eventNameSpace = '.asue'; //Auto Suggest User Engagement
                $searchInputBox.bind('click' + eventNameSpace,
                function ()
                {
                    if (func())
                    {
                        //function succeeded so, unbind the events
                        $searchInputBox.unbind(eventNameSpace);
                    }
                }
                ).bind('keyup' + eventNameSpace,
                function (e)
                {
                    if (e.which != 27 && e.which != 9) //ignore escape key and tab key
                    {
                        if (func())
                        {
                            //function succeeded so, unbind the events
                            $searchInputBox.unbind(eventNameSpace);
                        }
                    }
                }
                );
            }


            function updateAutoSuggestSessionId()
            {
                /// <summary>
                /// Updates the session id in the external file.
                /// until required as configured
                /// </summary>

                var sidCookieRegEx = /SID=[\d(A-Z(a-z)]+/;
                if (!partnerSessionId)
                {
                    var cookieNames = validateString(sharedCookieSettings.cn, 1) ? sharedCookieSettings.cn.split('+') : null;
                    if (cookieNames && validateString(cookieNames[0], 1))
                    {
                        var cookieValue = getCookie(cookieNames[0]);
                        if (validateString(cookieValue, 4))
                        {
                            var cookieCrumb = cookieValue.match(sidCookieRegEx);
                            if (cookieCrumb && validateString(cookieCrumb[0], 5))
                            {
                                partnerSessionId = cookieCrumb[0].substr(4);

                            }
                        }
                    }
                }

                if (typeof window[globalObjectName] != "undefined")
                {
                    var sessionIdFunction = window[globalObjectName].sid;
                    if ($.isFunction(sessionIdFunction))
                    {
                        //if the sa_autosuggest exposes a function to update the session id, call it
                        sessionIdFunction(partnerSessionId);
                    }

                    if (shouldFireClick)
                    {
                        shouldFireClick = 0;
                        if (document.activeElement == $searchInputBox[0])
                        {
                            //Since this function is called by autoSuggestLoaded and addAutoSuggest
                            //call back functions for iFrame and autoSuggest download, this will imitate a click 
                            //to open the popup, that way, 
                            //the popup will always open on engagement
                            //irrespective of delayBind parameter
                            $searchInputBox.click();
                        }
                    }
                }

                // Fire onCk codebinding when parterSessionId is available
                if (partnerSessionId && $.isFunction(sharedCookieSettings.onCk))
                {
                    sharedCookieSettings.onCk();
                    sharedCookieSettings.onCk = 0;
                }
            }


            function autoSuggestLoaded(options)
            {
                /// <summary>
                /// This function is made public by extending the window object (at the end of this file)
                /// - called by the child iframe which is in msn.com domain.
                /// - It checks to see if the hash value is valid 
                ///     * if valid, sets the cookie 
                ///  Always clears out the hash key, so it cannot be called 2nd time
                /// </summary>
                ///	<param name="options" type="jsonObj">
                ///		jsonObject with the cv and h strings
                /// eg: {cv : "E2SD4DFI890B", h: "AD6F23A4DF-ASDA-DSDFS-SDSF32"}
                ///	</param>

                if (options && oneTimeUseGuid)
                {
                    var cookieValue = options.cv;
                    if (options.h == oneTimeUseGuid && validateString(cookieValue, 2))
                    {
                        cookieValue = unescape(cookieValue);
                        var cookieArray = cookieValue.split(";");
                        for (var i = 0; i < cookieArray.length; i++)
                        {
                            var cookie = cookieArray[i];
                            var splitIndex = cookie.indexOf("=");
                            if (splitIndex > 0)
                            {
                                var key = cookie.substr(0, splitIndex);
                                var value = cookie.substr(splitIndex + 1);
                                if (validateString(value, 1))
                                {
                                    // expires in the same session
                                    // by default, the shared cookie is written into the default domain of the current page.
                                    // If the sharedCookieSettings.domain is not empty string, the value will be used as the cookie domain of the shared cookie.
                                    if (sharedCookieSettings.domain)
                                    {
                                        setCookie(key, value, 0, sharedCookieSettings.domain);
                                    }
                                    else
                                    {
                                        setCookie(key, value, 0);
                                    }
                                }
                            }
                        }
                        //Check to see if the cookie for session id is set
                        updateAutoSuggestSessionId();
                    }
                }

                //Clearing out the oneTimeUseGuid
                //this is outside the if statement by purpose, 
                //even if the function was called without the required parameters, clear out the private hash key
                //Since we dont expect this to be called twice in the same page
                //if that happens, the worst case is that the users do not see their search history
                oneTimeUseGuid = null;
            }



            ///****************************End Common functions copied from autoSuggest.js************************************///


            function updateMenuStatus($anchor)
            {
                /// <summary>
                /// a helper function to make sure the 
                ///    - toggle text is correct in customize menu.
                ///    - the autocomplete attribute is set correctly
                /// </summary>
                /// <param name="$anchorElement" type="Object">
                ///	reference to the link element
                /// </param>

                var disabled = isDisabled();
                // if autosuggest is currently disabled add enable text otherwise add disable text.
                $anchor.text(disabled ? settings.enableText : settings.disableText);

                // Disable IE Autocomplete feature on the input box if we are going to display suggestions.   
                // Otherwise enable autocomplete
                $searchInputBox.attr(autocompleteText, disabled ? autocompleteOnText : autocompleteOffText);

            }
        }

        // verify that s is a string
        // if minLength is passed in, verify that the string length is at least minLength
        function validateString(s, minLength)
        {
            return typeof s == "string" && (!minLength || s.length >= minLength);
        }
    });

/* WGINCLUDE: C:\PROJECTS\WORKSPACE2\MSNMETRO\MAIN\METROSDK\METROSDK\CONTENT\SOURCE\JS\CLASSIC\..\Shared\behaviors\requestAnimationFrame.js */
/////////////////////////////////////////////////////////////////////////////////
//
// File: requestAnimationFrame.js
// Defines: requestAnimationFrame
// Dependencies: window
// Description: implements a cross browser abstraction for window.requestAnimationFrame
//
/////////////////////////////////////////////////////////////////////////////////

define("requestAnimationFrame", ["window"], function (window)
{
    return (function ()
    {
        return window.requestAnimationFrame || window.webkitRequestAnimationFrame || window.mozRequestAnimationFrame
                   || window.oRequestAnimationFrame || window.msRequestAnimationFrame || function (callback)
                   {
                       window.setTimeout(callback, 16.7);
                   };
    })();
});

/* WGINCLUDE: C:\PROJECTS\WORKSPACE2\MSNMETRO\MAIN\METROSDK\METROSDK\CONTENT\SOURCE\JS\CLASSIC\..\Shared\behaviors\truncate.js */
/////////////////////////////////////////////////////////////////////////////////
//
// File: truncate.js
// Defines: truncate
// Dependencies: jQuery, jqfn, mediator, requestAnimationFrame, measure, format
// Description: truncates text to the containing height or a specified number of lines
//
// Copyright (c) 2012 by Microsoft Corporation. All rights reserved.
//
/////////////////////////////////////////////////////////////////////////////////

// take the space out before the # to turn on debug truncation timing measurement
/// #DEFINE MeasureTruncate

// take the space out before the # to turn on debug truncation loop logging
/// #DEFINE LoopLog

define("truncate", ["jquery", "jqfn", "mediator", "requestAnimationFrame", "measure", "format"], function ($, jqfn, mediator, requestAnimationFrame, measure, format)
{
    // has an edge case where even 1 word is too long, fortinutly css can solve this quite easily
    var cssTruncationString = '<span style="white-space:nowrap;overflow:hidden;text-overflow:ellipsis;display:block;width:{1}">{0}</span>';

    var ellipsis = "%Truncate.Ellipsis%";
    var ellipsisLength = ellipsis.length;

    // very complicated regular expression.
    // UNICODE ranges: \u3000-\u303f are CJK punctuation characters, each of which can break the line;
    //                 \u3040-\u309f are JP Hiragana, each of which can break the line;
    //                 \u30A0-\u30FF are JP Katakana, each of which can break the line;
    //                 \u3400-\u4dbf are unified Han ideographs, each of which can break the line.
    //                 \u4e00-\u9fff are JP Hangul syllables, each of which can break the line;
    //                 \uf900-\ufaff are CJK compatibility ideographs, each of which can break the line;
    // so we have:
    // zero or more whitespace characters,
    // followed by:
    //         1 CJK Puncutation character
    //      OR 1 Hiragana character
    //      OR 1 Katakana character 
    //      OR 1 Han/Hangul character 
    //      OR 1 or more of anything OTHER than whitespace or CJK character,
    // followed by zero or more whitespace characters,
    // anchored to the end of the string.
    var lastWord = /(?:\s|[,!\.\?:;])*([\u3000-\u30ff\u3400-\u4DBF\u4E00-\u9FFF\uF900-\uFAFF]|[^\s\u3000-\u30ff\u3400-\u4DBF\u4E00-\u9FFF\uF900-\uFAFF]+)(?:\s|[,!\.\?:;])*$/;

    function applyBehavior($set, settings)
    {
        var lengthOfSet = $set.length;
        var truncateElement = new Array(lengthOfSet);
        var computedStylesArray = new Array(lengthOfSet);
        var twoLinesArray = new Array(lengthOfSet);
        var originalHtmlArray = new Array(lengthOfSet);
        var originalTitleArray = new Array(lengthOfSet);
        var originalWidth = new Array(lengthOfSet);
        var workingTextArray = new Array(lengthOfSet);
        var maxScrollHeightArray = new Array(lengthOfSet);
        var needsCheckedArray = new Array(lengthOfSet);
        var wasChangedArray = new Array(lengthOfSet);
        var needsUpdate = new Array(lengthOfSet);
        var firstRun = true;

        // if anybody publishes the trunc event, rerun the truncation calcs
        // if we do this on an animation frame, it goes a lot faster.
        mediator.sub("truncate", function(){requestAnimationFrame(recalc);});

        function truncate()
        {
            ///#IFDEF MeasureTruncate
            var startTime = +new Date;
            ///#ENDIF

            // using many loops to seperate reads from writes
            // also looping through arrays backwards for perf gains
            // see http://jsperf.com/array-allocation-fill/2
            var ndx;
            var text;

            // first time init
            if (firstRun)
            {
                firstRun = false;
                
                // FIRST, let's make sure that our elements aren't the parent of a single
                // HTML element. If it is, we want to work off THAT element, not the container.
                ndx = lengthOfSet;
                while(ndx--)
                {
                    text = "";
                    var elementCount = 0;
                    var childElement;
                    var childNode = $set[ndx].firstChild;
                    while(childNode != null)
                    {
                        if (childNode.nodeType == 1)
                        {
                            // increase the element count and save this node in
                            // case it's the only element child.
                            ++elementCount;
                            childElement = childNode;
                        }
                        else if (childNode.nodeType == 3)
                        {
                            // add the text to the string
                            text += childNode.nodeValue;
                        }

                        childNode = childNode.nextSibling;
                    }

                    if (elementCount == 1 && $.trim(text) == "")
                    {
                        // only one child with no non-whitespace text around it.
                        // we want to work off THAT node, not the parent.
                        truncateElement[ndx] = childElement;
                    }
                }

                // grab computed style and html (read)
                ndx = lengthOfSet;
                while (ndx--)
                {
                    computedStylesArray[ndx] = measure($set[ndx]);
                    originalHtmlArray[ndx] = (truncateElement[ndx] || $set[ndx]).innerHTML;
                    originalTitleArray[ndx] = $set[ndx].title;
                }
            }
            else
            {
                teardown();
            }

            // if truncateLines is set then use those for maxScrollHeight, otherwise use all of elem's height (read)
            ndx = lengthOfSet;
            while (ndx--)
            {
                computeAvailableSpace(ndx);
                workingTextArray[ndx] = originalHtmlArray[ndx];
                needsCheckedArray[ndx] = true;
            }

            // do the actual truncation work given the 
            truncationLoop();

            ///#IFDEF MeasureTruncate
            var deltaTime = +new Date - startTime;
            if (window.console)
            {
                console.log("Truncate time: " + deltaTime + "ms");
            }
            ///#ENDIF
        }

        // called when the c.deferred event fires
        function recalc()
        {
            ///#IFDEF MeasureTruncate
            var startTime = +new Date;
            ///#ENDIF

            // reset the calc array for any item whose scrollHeight is greater than the max allowed
            var ndx = lengthOfSet;
            while (ndx--)
            {
                computeAvailableSpace(ndx);
                needsCheckedArray[ndx] = $set[ndx].scrollHeight > maxScrollHeightArray[ndx];
            }

            truncationLoop();

            ///#IFDEF MeasureTruncate
            var deltaTime = +new Date - startTime;
            window.console && console.log("Truncate RECALC time: " + deltaTime + "ms");
            ///#ENDIF
        }

        // compute the available space (width and max height, plus how high two lines of text would take up)
        // into which we have to fit the scrollHeight of the element.
        function computeAvailableSpace(ndx)
        {
            var elem = $set[ndx];
            var compute = computedStylesArray[ndx];
            var truncateLines = elem.getAttribute("data-truncate-lines") || settings.truncateLines;
            var paddingTop = parseFloat(compute("paddingTop"));
            var paddingBottom = parseFloat(compute("paddingBottom"));
            var lineHeight = compute("lineHeight");

            // this value will be NaN if there is no numeric maxHeight style on the element.
            var maxHeight = parseFloat(compute("maxHeight"));

            // always add paddingTop since it's not part of the maxScrollHeight calculation for some reason (not sure why this is the case).
            // if we are calculating the number of lines, then we need both the padding top AND the padding bottom.
            // Again, not exactly sure why, but this was determined through trial and error experimentation.
            if (truncateLines)
            {
                // if we want to limit to a specific number of lines, multiply the number of line by the line height
                // and add in the bottom padding (don't know why; seems to need it to match the scrollHeight).
                maxScrollHeightArray[ndx] = parseFloat(lineHeight) * truncateLines + paddingBottom;

                // now look at the CSS maxHeight style. If the maxHeight isn't set, 
                // then the value will be NaN and we can skip this step. But if it was set, make sure that what we
                // just calculated isn't larger; if it is, use maxHeight because that's the absolute
                // max space available as rendered by the browser.
                if (maxScrollHeightArray[ndx] > maxHeight)
                {
                    maxScrollHeightArray[ndx] = maxHeight;
                }

                // always add the top padding; don't know why, but that's how the scrollHeight appears to work.
                maxScrollHeightArray[ndx] += paddingTop;
            }
            else
            {
                // if there is a maxheight, use that. Otherwise use the space we got (clientHeight).
                // always add the top padding; don't know why, but that's how the scrollHeight appears to work.
                maxScrollHeightArray[ndx] = (maxHeight || elem.clientHeight) + paddingTop;
            }

            // make sure we round the max value to an int, regardless of how we calculated it.
            maxScrollHeightArray[ndx] = (maxScrollHeightArray[ndx] + 0.5) | 0;

            // save the client width and precompute how big two lines would take up. 
            // We'll use this later to see if multi-line truncation would be a waste of time
            // because there's only enough space for one line anyway (rounded to integer).
            originalWidth[ndx] = elem.clientWidth;
            twoLinesArray[ndx] = (paddingTop + 2*parseFloat(lineHeight) + paddingBottom + 0.5) | 0;

            ///#IF LoopLog
            if (truncateLines)
            {
                window.console && console.log("TRUNCATION measure item " + ndx + ": maxScrollHeight=" + maxScrollHeightArray[ndx] 
                    + "; truncateLines=" + truncateLines
                    + "; lineHeight=" + lineHeight
                    + "; paddingBottom=" + paddingBottom
                    + "; paddingTop=" + paddingTop
                    + "; maxHeight=" + maxHeight
                    + "; twoLines=" + twoLinesArray[ndx]);
            }
            else
            {
                window.console && console.log("Measure item " + ndx + ": maxScrollHeight=" + maxScrollHeightArray[ndx] 
                    + "; clientHeight=" + elem.clientHeight
                    + "; paddingTop=" + paddingTop
                    + "; maxHeight=" + maxHeight
                    + "; twoLines=" + twoLinesArray[ndx]);
            }
            ///#ENDIF
        }

        // actual work of the truncation - measure, reduce text if necessary, repeat until done.
        function truncationLoop()
        {
            ///#IFDEF MeasureTruncate
            var startTime = +new Date;
            ///#ENDIF

            // loop until no changes happen (or we hit the max iterations, which we should never hit unless we
            // accidentally code an infinite loop or something)
            var notDone = true;
            var maxIters = 1000;
            while (notDone && --maxIters)
            {
                notDone = false;

                // (read)
                var ndx = lengthOfSet;
                while (ndx--)
                {
                    if (needsCheckedArray[ndx])
                    {
                        ///#IF LoopLog
                        if (window.console){console.log("loop item " + ndx + ": scrollHeight=" + $set[ndx].scrollHeight + "; maxScrollHeight=" + maxScrollHeightArray[ndx]);}
                        ///#ENDIF

                        needsUpdate[ndx] = $set[ndx].scrollHeight > maxScrollHeightArray[ndx];
                        if (needsUpdate[ndx])
                        {
                            // doesn't fit - we need to update it somehow.
                            wasChangedArray[ndx] = true;

                            // the scroll height is a measurement of how tall the item really is at this point.
                            // so if the item is less than 2 lines tall already, then trimming off a single word
                            // won't help any, because we'll always end up going to just a single-line truncation
                            // anyway. So if that's the case, shortcut to single-line now and save some time.
                            if ($set[ndx].scrollHeight < twoLinesArray[ndx])
                            {
                                singleLineTruncation(ndx);
                                continue;
                            }
                            
                            // get the text with the ellipsis trimmed off the end (if any)
                            var text = workingTextArray[ndx];
                            if (text.slice(-ellipsisLength) == ellipsis)
                            {
                                text = text.slice(0, -ellipsisLength);
                            }

                            var match = lastWord.exec(text);
                            if (!match || match[0] == text)
                            {
                                singleLineTruncation(ndx);
                            }
                            else
                            {
                                // check again with 1 less word, plus ellipsis
                                notDone = true;
                                workingTextArray[ndx] = text.substr(0, text.length - match[0].length) + ellipsis;

                                ///#IF LoopLog
                                if(window.console){console.log("trying smaller string on item " + ndx + ": " + workingTextArray[ndx]);}
                                ///#ENDIF
                            }
                        }
                        else
                        {
                            // height is within limits, don't check again
                            needsCheckedArray[ndx] = false;

                            ///#IF LoopLog
                            if (window.console) { console.log("finalizing text " + ndx + ": " + workingTextArray[ndx]); }
                            ///#ENDIF
                        }
                    }
                }

                // set all text that changed (write)
                ndx = lengthOfSet;
                while (ndx--)
                {
                    if (needsUpdate[ndx])
                    {
                        // if we have a truncate element, set ITS content; 
                        // otherwise change the original set element.
                        (truncateElement[ndx] || $set[ndx]).innerHTML = workingTextArray[ndx];
                    }
                }
            }

            // update the title for any element we truncated. And if we didn't
            // truncate it, set it to the original title (if any)
            ndx = lengthOfSet;
            while(ndx--)
            {
                if (wasChangedArray[ndx])
                {
                    // get the text of the original HTML -- wrap it in a span so jQuery doesn't think
                    // it's a big selector string. Then trim it.
                    $set[ndx].title = $.trim($("<span>"+originalHtmlArray[ndx]+"</span>").text());
                }
            }

            ///#IFDEF MeasureTruncate
            var deltaTime = +new Date - startTime;
            if (window.console)
            {
                console.log("truncationLoop time: " + deltaTime + "ms");
            }
            ///#ENDIF

            ///#IF DEBUG
            if (!maxIters && window.console)
            {
                // this can happen if the infinite-loop protection max gets hit.
                window.console.log("ERROR: MAX ITERATIONS HIT ON TRUNCATION CODE!");
            }
            ///#ENDIF
        }

        function singleLineTruncation(ndx)
        {
            ///#IF DEBUG
            if(window.console){console.log("TRUNCATE: giving up on item " + ndx 
                + " [scrollHeight=" + $set[ndx].scrollHeight 
                + "; maxScrollHeight=" + maxScrollHeightArray[ndx] 
                + "; maxHeight(css)=" + computedStylesArray[ndx]("maxHeight")
                + "; twoLines=" + twoLinesArray[ndx]
                + "]: " + originalHtmlArray[ndx]);}
            ///#ENDIF

            // last word is the only word, truncate to 1 line with css
            var width = originalWidth[ndx] - parseInt(computedStylesArray[ndx]("paddingLeft")) - parseInt(computedStylesArray[ndx]("paddingRight"));
            workingTextArray[ndx] = width > 0 ? format(cssTruncationString, originalHtmlArray[ndx], width + "px") : originalHtmlArray[ndx];
            needsCheckedArray[ndx] = false;
        }

        function requestAnimationFrameTruncate()
        {
            // wait for animation frame request
            // this saves some time (found from measuring)
            // likely due to not having to draw during the changes
            requestAnimationFrame(truncate);
        }

        function teardown()
        {
            var ndx = lengthOfSet;
            while (ndx--)
            {
                if (wasChangedArray[ndx])
                {
                    // reset the original text on the truncate element if we have one, otherwise
                    // the original set element.
                    (truncateElement[ndx] || $set[ndx]).innerHTML = originalHtmlArray[ndx];
                    $set[ndx].title = originalTitleArray[ndx];
                    wasChangedArray[ndx] = false;
                }
            }
        }

        return {
            setup: requestAnimationFrameTruncate,
            teardown: teardown,
            update: requestAnimationFrameTruncate
        };
    }

    return jqfn(applyBehavior, 0, { each: 1 });
});

/* WGINCLUDE: C:\PROJECTS\WORKSPACE2\MSNMETRO\MAIN\METROSDK\METROSDK\CONTENT\SOURCE\JS\CLASSIC\..\Shared\Behaviors\makehomepage.js */
/////////////////////////////////////////////////////////////////////////////////
//
// File: makehomepage.js
// Defines: makeHome
// Dependencies: jquery, jqfn
// Description: Add the "make X my homepage" functionality
//
// Copyright (c) 2013 by Microsoft Corporation. All rights reserved.
//
//////////////////////////////////////////////////////////////////////////////////

define("makeHome", ["jquery", "jqfn"], function ($, jqfn)
{
    function applyBehavior($elem, settings)
    {
        try
        {
            // if we haven't already, 
            // try to load the homepage behavior onto the host element
            var hostElement = document.body;
            if (typeof hostElement.isHomePage == "undefined")
            {
                hostElement.style.behavior = 'url("#default#homepage")';
            }

            // if the API isn't present, then we aren't capable of changing 
            // the homepage (maybe not IE) and therefore just leave the links 
            // alone and be done.
            if (typeof hostElement.isHomePage != "undefined")
            {
                // the API *is* present. Must be IE. Initialize the link.
                initialize();
            }
        }
        catch(err)
        {
            // we are not expecting any errors, but IE sometimes is mysterious and
            // will throw one sometimes.
            window.console && console.error("isHomePage: " + err);
        }

        function initialize()
        {
            // pull the target url from the link data. No target url, nothing to do.
            var targetUrl = settings.targetUrl;
            if (targetUrl)
            {
                if (hostElement.isHomePage(targetUrl))
                {
                    // already the homepage; change to fallback link
                    changeToFallback();
                }
                else
                {
                    // not the homepage. Set the event handler to
                    // change the homepage when clicked.
                    $elem.on("click", function changeHomePage(evt)
                    {
                        // link does NOT navigate, but don't stop the propagation because we
                        // still want this to bubble up to the click-tracking code.
                        evt.preventDefault();

                        try
                        {
                            // to get here isHomePage must've been defined, so it'd be REALLY
                            // weird if setHomePage isn't. Don't bother checking it -- if it fails
                            // the catch will pick it up.
                            hostElement.setHomePage(targetUrl);

                            // we weren't before; now check to see if the target is the homepage now
                            if (hostElement.isHomePage(targetUrl))
                            {
                                // it is! Successful homepage change.
                                // unhook the click event(s) and switch the links to fallback
                                $elem.off("click", changeHomePage);
                                changeToFallback();
                            }
                        }
                        catch(err)
                        {
                            // we aren't expecting any errors, so throw a little something on the console
                            window.console && console.error("setHomePage: " + err);
                        }
                    });
                }
            }

            function changeToFallback()
            {
                // get the fallback text to display
                var fallbackText = settings.fbText;
                if (!fallbackText)
                {
                    // no fallback text -- just leave the href and text alone and hide the link
                    $elem.hide();
                }
                else
                {
                    // and set the new stuff. If there is no fbUrl attribute, just use the current href
                    $elem.html(fallbackText).attr("href", settings.fbUrl);
                }
            }
        }
    }

    return jqfn(applyBehavior);
});



// double \\ here to avoid \u for unicode
/* WGINCLUDE: C:\PROJECTS\WORKSPACE2\MSNMETRO\MAIN\METROSDK\METROSDK\CONTENT\SOURCE\JS\CLASSIC\..\Shared\behaviors\\usernameHover.js */
/////////////////////////////////////////////////////////////////////////////////
//
// File: usernameHover.js
// Dependencies: jQuery, jqfn, mediator, hover, safeCss
// Description: For the header RPS username hover behavior.
//
// Copyright (c) 2013 by Microsoft Corporation. All rights reserved.
//
/////////////////////////////////////////////////////////////////////////////////

define("usernameHover", ["jquery", "jqfn", "mediator", "hover", "safeCss"], function ($, jqfn, mediator, hover, safeCss)
{
    var defaults = {
        // so it is not auto hide after a click if set timeout as zero.
        clickAutoHideTimeout: 0
    };
    
    function applyBehavior($elem, settings)
    {
        var safeCssGroup = safeCss.createGroup();
        var childHeight = $elem.children("ul").outerHeight();
        var originalHeight = $elem.outerHeight();
        var usernameHover = hover($elem, null, settings);
        var pubId = $elem.data(hover.hoverId);

        function show()
        {
            safeCssGroup($elem).css("height", childHeight + originalHeight + "px");
        }
        
        function hide()
        {
            safeCssGroup($elem).css("height", originalHeight + "px");
        }

        return {
            setup: function ()
            {
                usernameHover.setup();
                mediator.subChannel("hoverShow", pubId, show);
                mediator.subChannel("hoverHide", pubId, hide);
            },
            teardown: function ()
            {
                usernameHover.teardown();
                safeCssGroup.reset();
                mediator.unsubChannel("hoverShow", pubId, show);
                mediator.unsubChannel("hoverHide", pubId, hide);
            }
        };
    }

    return jqfn(applyBehavior, defaults);
});


/* WGINCLUDE: C:\PROJECTS\WORKSPACE2\MSNMETRO\MAIN\METROSDK\METROSDK\CONTENT\SOURCE\JS\CLASSIC\Shared\navHoverClassic.js */
/////////////////////////////////////////////////////////////////////////////////
//
// File: navHoverClassic.js
// Defines: navHover
// Dependencies: jQuery, jqfn
// Description: implements the nav hover that can be sticky
//
// Copyright (c) 2013 by Microsoft Corporation. All rights reserved.
//
/////////////////////////////////////////////////////////////////////////////////

define("navHover", ["jquery", "jqfn"], function ($, jqfn)
{

    var defaults = {
        mainNavSelector: "ul.outer>li",
        hoverClass: "current",
        sticky: false,
        blurDelay: 400
    };

    function applyBehavior($hoverElement, settings)
    {
        // Sets up the click behavior for the navigation
        var $currentMenu = $("ul.outer>li.current");
        var $defaultMenu = $currentMenu;
        var blurTimeout;

        function focusHandler()
        {
            var $li = $(this);
            
            // show and collapse other
            $currentMenu.removeClass(settings.hoverClass);
            $currentMenu = $li.addClass(settings.hoverClass);
            
            if (blurTimeout)
            {
                clearTimeout(blurTimeout);
                blurTimeout = 0;
            }
        }

        function blurHandler()
        {
            blurTimeout = setTimeout(revertMenu, settings.blurDelay);
        }
        
        function revertMenu()
        {
            blurTimeout = 0;
            $currentMenu.removeClass(settings.hoverClass);
            $currentMenu = $defaultMenu.addClass(settings.hoverClass);
        }

        return {
            setup: function ()
            {
                $hoverElement
                    .on("focus", settings.mainNavSelector, focusHandler)
                    .on("mouseenter", settings.mainNavSelector, focusHandler);
                
                if (!settings.sticky)
                {
                    $hoverElement.on("mouseleave", settings.mainNavSelector, blurHandler);
                }
            },
            teardown: function ()
            {
                $hoverElement
                    .off("focus", settings.mainNavSelector, focusHandler)
                    .off("mouseenter", settings.mainNavSelector, focusHandler);
                
                if (!settings.sticky)
                {
                    $hoverElement.off("mouseleave", settings.mainNavSelector, blurHandler);
                }
            }
        };
    }

    return jqfn(applyBehavior, defaults);
});

/* WGINCLUDE: C:\PROJECTS\WORKSPACE2\MSNMETRO\MAIN\METROSDK\METROSDK\CONTENT\SOURCE\JS\CLASSIC\Shared\searchScope.js */
/////////////////////////////////////////////////////////////////////////////////
//
// File: searchScope.js
// Defines: searchScope
// Dependencies: jQuery, jqfn
// Description: implements search scope switching
//
// Copyright (c) 2013 by Microsoft Corporation. All rights reserved.
//
/////////////////////////////////////////////////////////////////////////////////

define("searchScope", ["jquery", "jqfn"], function ($, jqfn)
{
    var defaults = {
        formId: "srchfrm",
        inputId: "q",
        titleSelector: "input[type=submit]",
        valueSelector: "input[type=submit].text",
        selectedClass: "selected"
    };

    var scopeRegex = /[?&]scope=([^&#]+)/i;
    var inputAugmentRegex = /[?&]q=([^&#]+)/i;

    function applyBehavior($scopeList, settings)
    {
        var $selectedScope = $();
        var $searchForm = $("#" + settings.formId);
        var $changeTitles = $(settings.titleSelector, $searchForm);
        var $changeValues = $(settings.valueSelector, $searchForm);
        var $input = $("#" + settings.inputId);
        var $hiddenScope = $("input[type=hidden][name=scope]", $searchForm);
        var advancedSearch;

        if (!$hiddenScope.length)
        {
            $hiddenScope = $('<input type="hidden" name="scope"/>').appendTo($searchForm);
        }

        $searchForm.on("submit", function ()
        {
            var value = $input.val();

            // check that that value should be changed
            if (advancedSearch && value && value.indexOf(":") == -1)
            {
                $input.val(value + " " + advancedSearch);

                setTimeout(function ()
                {
                    $input.val(value);
                    console.log("reseting value: " + value);
                }, 50);
            }
        });

        $scopeList.children().each(function (ndx)
        {
            var $scope = $(this);
            var $link = $("a", this);
            var name = $link.data("label").toLowerCase();
            var action = $link.attr("href");
            var hiddenScopeFieldValue = (action.match(scopeRegex) || [])[1] || "";
            var inputAugment = (action.match(inputAugmentRegex) || [])[1] || "";

            if ($scope.hasClass(settings.selectedClass))
            {
                $selectedScope = $selectedScope.add($scope);
            }

            $link.on("click", clickHandler);

            // set the first scope
            if (!ndx)
            {
                clickHandler();
            }

            function clickHandler(e)
            {
                e && e.preventDefault();
                $searchForm.attr("action", action);
                $hiddenScope.val(hiddenScopeFieldValue);
                advancedSearch = inputAugment;

                // if there is a search value then submit, otherwise just set focus
                if ($input.val())
                {
                    $searchForm.submit();
                }
                else
                {
                    $input.focus();
                }

                $changeTitles.attr("title", name);
                $changeValues.val(name);
                $selectedScope.removeClass(settings.selectedClass);
                $selectedScope = $scope.addClass(settings.selectedClass);
            }
        });
    }

    return jqfn(applyBehavior, defaults);
});

/* WGINCLUDE: C:\PROJECTS\WORKSPACE2\MSNMETRO\MAIN\METROSDK\METROSDK\CONTENT\SOURCE\JS\CLASSIC\..\Shared\Behaviors\polls.js */
/////////////////////////////////////////////////////////////////////////////////
//
// File: polls.js
// Defines: poll
// Dependencies: jQuery, jqfn, getCookie, setCookie, format, window
// Description: Bind events to the poll clickable elements
//
// Copyright (c) 2013 by Microsoft Corporation. All rights reserved.
//
/////////////////////////////////////////////////////////////////////////////////

define("poll", ["jquery", "jqfn", "getCookie", "setCookie", "window"], function ($, jqfn, getCookie, setCookie, window)
{
    // TODO: Update selectors for better performance
    var toggleResultsVotingSelector = "div.question, div.result";
    var inputQuestionSelector = ".question input[type='radio']";
    var inputQuestionCheckedSelector = ".question input:checked[name='n_{0}']";
    var resultPostVotingDivSelector = "button.backtovoting";
    var toggleButtonSelector = "button.skiptoresult, button.backtovoting";
    var postVoteCookie = "pv";
    var numberOfCharactersFromPollId = 4;
    var voteButtonId = "#votebtn";
    var answerCookiesValidDays = 5;
    var voteSuccessResult = "200";
    var errorMsgDivSelector = "div.errormsg";
    var voteUrl = "/ajax/poll/pollid/{0}/pollanswerid/{1}";

    // Get the top domain from the current url.
    // TODO: Update regular expression to cater IP addresses as well.
    var cookieDomain = window.location.hostname.match(/([^.]+\.[^.]*)$/);
    cookieDomain = cookieDomain ? cookieDomain[0] : "";

    function applyBehavior($pollsMainContainer)
    {
        var pollId = $pollsMainContainer.find(".poll").data("pollid");
        var $voteBtn = $pollsMainContainer.find(voteButtonId + pollId);
        var $inputQuestion = $pollsMainContainer.find(inputQuestionSelector);
        var $toggleButton = $pollsMainContainer.find(toggleButtonSelector);
        var $resultPostVotingDiv = $pollsMainContainer.find(resultPostVotingDivSelector);
        
        function showResultsOrVoting()
        {
            $pollsMainContainer.find(toggleResultsVotingSelector).toggle();
        }

        function backToVote()
        {
            $pollsMainContainer.find(errorMsgDivSelector).addClass("hide");
        }

        function afterVoteShowResults()
        {
            showResultsOrVoting();

            // Hide back to voting button
            $resultPostVotingDiv.hide();
        }

        function onVoteFailureForPoll()
        {
            $pollsMainContainer.find(errorMsgDivSelector).removeClass("hide");
            showResultsOrVoting();
        }

        function voteForPoll()
        {
            var $answerRadio = $pollsMainContainer.find(inputQuestionCheckedSelector.format(pollId));
            if ($answerRadio.length > 0 && pollId.length > 0)
            {
                // Removing 'l_' prefix from the answer id.
                var pollAnswerId = $answerRadio[0].id.slice(2);
                $.getJSON(voteUrl.format(pollId, pollAnswerId), function(data){
                    data.StatusCode == voteSuccessResult ? onVoteSuccessForPoll() : onVoteFailureForPoll();
                }).fail(function () { onVoteFailureForPoll(); });
            }
        }

        function onVoteSuccessForPoll()
        {
            afterVoteShowResults();

            // 00 when user is anonymous and (TODO)LIVE:CID when user is logged in.
            // Concatenated with last 4 digits of poll id for which user has voted.
            var userCid = "00";
            var postVoteCookieValue = userCid + "=" + pollId.slice(-numberOfCharactersFromPollId);
            var postVoteCookieExistingValue = getCookie(postVoteCookie);
            if (postVoteCookieExistingValue)
            {
                postVoteCookieValue = postVoteCookieExistingValue + "," + postVoteCookieValue;
            }

            setCookie(postVoteCookie, postVoteCookieValue, answerCookiesValidDays, cookieDomain, "/");
        }

        function hasVoted(postVoteCookieVal)
        {
            var postVoteCookieVal = getCookie(postVoteCookie);

            return postVoteCookieVal && postVoteCookieVal.indexOf(pollId.slice(-numberOfCharactersFromPollId)) > -1;
        }

        return {
            setup: function ()
            {
                // If user has already voted for a poll, then do not give him the option to vote again.
                if (hasVoted())
                {
                    afterVoteShowResults();
                }
                else
                {
                    $inputQuestion.change(function ()
                    {
                        // In case of multiple polls, we will need to select the input tags and vote button as per current poll id
                        var $answerRadio = $pollsMainContainer.find(inputQuestionCheckedSelector.format(pollId));

                        if ($answerRadio.length)
                        {
                            $voteBtn.removeAttr("disabled");
                        }
                    });

                    $voteBtn.on("click", voteForPoll);
                    $toggleButton.on("click", showResultsOrVoting);
                    $resultPostVotingDiv.on("click", backToVote);
                }
            },

            teardown: function ()
            {
                // Refrain any user interaction in case of teardown, rest other buttons are already in disabled state
                $inputQuestion.attr("disabled", "disable");
                $voteBtn.off("click", voteForPoll);
                $toggleButton.off("click", showResultsOrVoting);
                $resultPostVotingDiv.off("click", backToVote);
            }
        };
    }

    return jqfn(applyBehavior)
});


/////////////////////////////////////////////////////////////////////////////////
//
// File: base.pre.js
// Defines: 
// Dependencies: jquery, jqbind
// Description: Classic common bindings across all pages before page bindings
//
// Copyright (c) 2012 by Microsoft Corporation. All rights reserved.
// 
//////////////////////////////////////////////////////////////////////////////////*

require(["jquery", "jqbind", "c.dom"], function ($, jqbind)
{
    require(["c.deferred"], function () { jqbind("autosuggest", "#srchfrm"); });
    jqbind("searchScope", "#search-scope");
    jqbind("navHover", "#nav");
    
    // Display the ad-choices image-link only if Javascript is enabled
    $(".ad a.adchoices").removeClass('adchoices').addClass('adchoicesjs');

    // make-msn-my-homepage behavior
    jqbind("makeHome", "#makemsn a");

    // Set focus on bing search input box
    $("#q").focus();

    // Skip the tabbing on invisible anchors
    $(".channel").attr('tabindex', '-1');
    $(".carat").attr('tabindex', '-1');
    $(".channel SPAN").attr('tabindex', '0');

    jqbind("usernameHover", "#username");
    
    // Truncate title
    // TODO: we want .truncate, but what about the things after it???
    jqbind("truncate", ".truncate,.relatedvideos li a div,.related .morephotogalleries a p,.featured .heading,.related .captiontitle,.related .abstract");
    
    // Bind events to embedded polls and polls in aside section
    jqbind("poll", ".pollcontainer");
});

/* C:\Projects\Workspace2\MSNMetro\Main\MetroSDK\MetroSDK\Content\Source\js\Classic\landingPage.js */

/* WGINCLUDE: C:\PROJECTS\WORKSPACE2\MSNMETRO\MAIN\METROSDK\METROSDK\CONTENT\SOURCE\JS\CLASSIC\..\Shared\implementations\swipeNavClassic.js */
/////////////////////////////////////////////////////////////////////////////////
//
// File: swipeNavClassic.js
// Defines: swipeNavImpl
// Dependencies: AMD, jQuery, swipeNavUtils, mediator
// Description: An implementation that can be used in swipeNav. See Behaviors/swipeNav.js
// Copyright (c) 2012 by Microsoft Corporation. All rights reserved.
//
/////////////////////////////////////////////////////////////////////////////////

// take the space out between the /// and the # to turn on the console logging of all position changes
// Please DO NOT check this file in with the space removed!
/// #DEFINE LOGCHANGES

define("swipeNavImpl", ["jquery", "swipeNavUtils", "mediator"], function ($, swipeNavUtils, mediator)
{
    // unique scope for each call
    return function (fullFunctionality, isCarousel, $elem, $children, $container, safeCssGroup, updateFunctions)
    {
        var $hover = $container;
        var childrenLength = $children.length;
        var carouselManager = new swipeNavUtils.CarouselManager();
        var currentIndex = 0;
        var viewPortWidth = $elem.width();
        var isFullscreen;
        var endslateClass = "lastslide";

        return {
            setup: function ()
            {
                var childrenWrappedItem = [];

                // loop through to create the wrapped child array for the carousel manager
                // and the array of original offsets for us
                for (var ndx = 0; ndx < childrenLength; ++ndx)
                {
                    childrenWrappedItem[ndx] = $children.eq(ndx);
                }

                // initialize the carousel manager with the array of wrapped children we created
                carouselManager.load(childrenWrappedItem);

                $hover.on("mouseover", updateFunctions.showArrows);
                $hover.on("mouseout", updateFunctions.hideArrows);

                // whenever a slideshow images fires the onload event, it has a new image
                // which affects the width of the slide, and the entire slideshow may need to
                // readjust the positions to take that width into account.
                // don't need to do this for the carousel, since the carousel consists of a 
                // series of fixed-width slides.
                if (!isCarousel)
                {
                    $("img", $container).on("load", repositionItems);
                }

                // classic can't use advanced CSS to vertically center images, so
                // hook these two publish events to know when we need to turn on the script-based
                // centering functionality

                mediator.sub("fullscreen", fullScreenChanged);
                
                if (fullFunctionality)
                {
                    repositionItems();
                }
            },
            teardown: function ()
            {
                $hover.off("mouseover", updateFunctions.showArrows);
                $hover.off("mouseout", updateFunctions.hideArrows);

                if (!isCarousel)
                {
                    $("img", $container).off("load", repositionItems);
                }

                mediator.unsub("fullscreen", fullScreenChanged);

                // undo any positioning
                $children.css({left:"auto"});
            },
            animate: moveToPosition,
            change: function(offset)
            {
                carouselManager.changeIndex(offset);

                // swipeNavUtils.CarouselManager.changeIndex just adds the offset to the index; it
                // doesn't do any wrap-around logic. So just do the same thing here to keep our index
                // in sync with theirs.
                currentIndex += offset;

                // call the update function which will eventually get around to publishing an update
                // event if it actually changes anything
                updateFunctions.slides(offset);
            },
            resize: repositionItems,
            hasNext: function ()
            {
                return isCarousel || currentIndex < childrenLength - 1;
            },
            hasPrevious: function ()
            {
                return isCarousel || currentIndex > 0;
            },
            addHoverElements: function ($set)
            {
                $hover = $hover.add($set);
            }
        };
        
        function fullScreenChanged(newFullsreen)
        {
            if ((isFullscreen = newFullsreen))
            {
                // changing to fullscreen mode.
                // make sure we clear the left position, since the horizontal centering
                // is handled by CSS.
                $children.css({ left: 0 });
            }
            else
            {
                // no longer in fullscreen mode -- make sure we reset the top for the items
                // back to zero so they fall back into their default locations vertically.
                $children.css({ top: 0 });
            }
        }     

        function moveToPosition(offset, trackSlide)
        {
            if (!isCarousel)
            {
                // make sure non-carousel have allowed offset
                var targetIndex = currentIndex + offset;
                if (targetIndex < 0)
                {
                    targetIndex = 0;
                }
                else if (targetIndex >= childrenLength)
                {
                    targetIndex = childrenLength - 1;
                }

                if (targetIndex != currentIndex + offset)
                {
                    offset = targetIndex - currentIndex;
                }
            }

            // set current position
            carouselManager.changeIndex(offset);

            repositionItems(offset);
            updateFunctions.slides(offset, trackSlide);
        }

        function repositionItems(animationOffset)
        {
            // if an animation offset isn't passed or isn't a number, use 0 for current position.
            // otherwise make sure the animationOffset is converted to a numeric data type with the
            // unary plus operator.
            var offset = isNaN(animationOffset) ? 0 : +animationOffset;

            // update our current index
            currentIndex += offset;
            if (isCarousel)
            {
                currentIndex = (currentIndex + childrenLength) % childrenLength;
            }

            // get the slide we want to center in the viewport and its width
            var $child = $children.eq(currentIndex);
            var childWidth = $child.width();

            // get its current offset from the left edge of the viewport and the 
            // current left position from its original position
            var itemOffset = $child.offset().left - $elem.offset().left;
            var currentLeft = parseInt($child.css("left")) || 0;

            // In fullscreen mode, the horizonal position is handled by CSS, but we need to use
            // JavaScript to vertically center it. In non-fullscreen mode, the vertical position
            // is handled by CSS, and the horizontal position is done in script.
            var delta, currentTop, firstImg, windowHeight;
            if (isFullscreen)
            {
                // fullscreen mode
                // compute the center vertical position for the current item given the window's height. 
                // get the first img under the current child; there should always be one if this isn't an endslate.
                // don't center the end slate for now.
                if ($child.hasClass(endslateClass))
                {
                    // if the endslate isn't visible, then we can't get an accurate accounting of its height.
                    // so in that case, set the show class on it so that we can measure it accurately.
                    if (!$child.is(":visible"))
                    {
                        $child.addClass("show");
                    }

                    // get the height of the endslate; if it's zero, ignore.
                    var slateHeight = $child.height();
                    if (slateHeight)
                    {
                        // get the height of the window. If the slate is bigger than the window, then leave
                        // it top-aligned (top set to 0). But if it's smaller, vertically-center it.
                        windowHeight = window.innerHeight || document.documentElement.clientHeight;
                        delta = slateHeight < windowHeight ? (windowHeight - slateHeight) / 2 : 0;
                        $child.css({top:delta});
                    }
                }
                else if ((firstImg = $("img", $child)[0]))
                {
                    var imageHeight = firstImg.height;
                    var imageWidth = firstImg.width;
                    if (!$child.is(":visible"))
                    {
                        // workaround for IE7, which will give a cached, incorrect value of the image height (or zero)
                        // when an image is not visible. So if the current image isn't visible yet (we're in the middle
                        // of moving from one slide to another), then load the image in a temporary Image object and
                        // get the height from that.
                        // I have a minor timing concern here: the image should already be loaded and in the cache.
                        // if it isn't, this height will be wrong, maybe even zero. However, if the image isn't loaded yet, 
                        // then we don't really care if we don't position it absolutely correct at this time, 
                        // because when it does come in, the load event will fire and will position it properly anyway.
                        var tempImg = new Image();
                        tempImg.src = firstImg.src;
                        imageHeight = tempImg.height;
                        imageWidth = tempImg.width;
                    }

                    if (imageHeight)
                    {
                        // compute the centered position from the window height
                        // taking the window height into account -- if the image is taller than the window, scale
                        // it down to fit using the appropriate aspect ratio.
                        windowHeight = window.innerHeight || document.documentElement.clientHeight;
                        if (imageHeight > windowHeight)
                        {
                            // the image is too tall -- scale it DOWN by calculating the width we would
                            // need given the image's aspect ratio, to get a height that fits exactly,
                            // provided the scale doesn't make the image remain wider than the window.
                            var newWidth = (windowHeight * imageWidth / imageHeight) | 0;
                            if (newWidth < imageWidth)
                            {
                                imageWidth = newWidth;
                                imageHeight = windowHeight;
                            }
                        }

                        // but if the image width is larger than the window width, we need to scale it down further
                        var windowWidth = window.innerWidth || document.documentElement.clientWidth;
                        if (imageWidth > windowWidth)
                        {
                            // the image is too wide - scale down to fit.
                            imageHeight = (imageHeight * windowWidth / imageWidth) | 0;
                            imageWidth = windowWidth;
                        }

                        // if the width we want is different than what we have, resize it now.
                        if (imageWidth != firstImg.width)
                        {
                            firstImg.width = imageWidth;
                        }

                        delta = ((windowHeight - imageHeight) / 2) | 0;

                        // if there's a difference from where we already are, then move it.
                        currentTop = parseInt($child.css("top")) || 0;
                        if (delta != currentTop)
                        {
                            ///#if LOGCHANGES
                            window.console && console.log("vert centering item " + currentIndex + "; windowHeight:" + windowHeight + "; currentTop:" + currentTop
                                + "; imageHeight:" + imageHeight + "; delta:" + delta);
                            ///#endif

                            $child.css({top: delta});
                        }
                    }
                    else
                    {
                        ///#if LOGCHANGES
                        window.console && console.log("img height is zero for item " + currentIndex);
                        ///#endif
                    }
                }
            }
            else
            {
                // not fullscreen mode
                // the delta for where we want to move it from its original position is
                // based on where it currently is, where it thinks it is, and the buffer that
                // we want to put in to center it in the viewport. If the computed delta from
                // original position is different than the current left, then we need to adjust
                // all the slide positions. If it's the same, don't bother making any changes.
                // (might be the same if an image finished loading but doesn't affect the lineup).
                // also, remove any fractional portion of the value (|0 is faster than Math.floor)
                delta = (-(itemOffset - currentLeft - (viewPortWidth - childWidth)/2)) | 0;
                if (delta != currentLeft)
                {
                    ///#if LOGCHANGES
                    window.console && console.log("horz centering item " + currentIndex + "; itemOffset:" + itemOffset + "; currentLeft:" + currentLeft
                        + "; delta:" + delta + "; childWidth:" + childWidth + "; viewPortWidth:" + viewPortWidth);
                    ///#endif

                    // and that value is the same for all slides!
                    $children.css({left:delta});
                    //$children.animate({left:delta}, 'fast');
                }
            }
        }
    };
});

/* WGINCLUDE: C:\PROJECTS\WORKSPACE2\MSNMETRO\MAIN\METROSDK\METROSDK\CONTENT\SOURCE\JS\CLASSIC\..\Shared\behaviors\swipeNav.js */
/////////////////////////////////////////////////////////////////////////////////
//
// File: swipeNav.js
// Defines: swipeNav
// Dependencies: AMD, jQuery, jqfn, mediator, safeCss, track
// Description: Enables swipe navigation between content (slides).
//              
//              TO USE THIS BEHAVIOR:
//              
//              The mediator object is used to communcate between swipeNav and other behaviors
//              that may want to use the swipeNav behavior. (infopane and gallery are examples)
//              swipeNav.event provides properties that can be used with the mediator
//                  "animate" - A publish will cause the slides to be animated by the number provided as data.
//                              This will also cause an "update" to be published if the slides moved
//                  "change" - A publish will cause the slides to be moved by the number provided as data without animating.
//                              This will also cause an "update" to be published if the slides moved
//                  "realign" - A publish will cause the slides to be adjusted if needed.
//                              This should be used when large UI changes occur such
//                              as gallery transition to/from fullscreen.
//                  "update"  - Subscribe to this to know when slides have changed. The data will be the
//                              offset that was moved.
//              
//              settings:
//                  carousel       - True causes the end to wrap back to the begining. False will not wrap
//                  autoRotate     - True will cause an animation forward at regular intervals until user changes slides
//                  autoRotateWait - Time in milliseconds to wait between autorotations
//                  showArrowTime  - Time in milliseconds to wait before auto hiding the arrows
//              
//              TO ADD BROWSER SUPPORT:
//              
//              This is an abstraction that allows different, capability specific, implementations passed in as "swipeNavImpl".
//              The abstraction handles the default settings, mediator communication, arrows, and a few other little things
//              The implementation will be a function that takes the following parameters:
//                  fullFunctionality - Boolean value indicating if full functionality is needed.
//                                          If false, just apply styling but no behaviors
//                  isCarousel        - Boolean indicating if this is a carousel or not
//                  $elem             - jQuery wrapped set containing the element bound to (ul)
//                  $children         - jQuery wrapped set containing all children (li)
//                                      This is passed in because children may be cloned and does not require any browser specific code
//                  $container        - jQuery wrapped set of the parent. Should be used to listen for hover events to hide/show arrows
//                  safeCssGroup      - Use for css changes to allow for easy reset. No need to call reset, it will be done automatically during teardown
//                  updateFunctions   - Functions used to communicate to this abstraction
//                  returns an implementation object with various functions, see below
//
//              updateFunctions:
//                  slides     - takes offset and trackSlide as parameters
//                              should be called whenever slides change
//                              offset is the number of slides moved
//                              trackSlide is an element if touch was used (so it can be tracked)
//                  hideArrows - called when arrows should be hidden (hover out)
//                  showArrows - called when arrows should be shown (hover in)
//
//              implementation functions:
//                  setup       - Apply styles and behavior
//                  teardown    - Remove behavior (styles cleared automatically in abstraction)
//                  animate     - Takes offset as a parameter. Animates to slide.
//                  change     - Takes offset as a parameter. Moves to slide without animating.
//                  resize      - Force recalculations for sizes
//                  hasNext     - True if there is a slide to animate to going forward
//                  hasPrevious - True if there is a slide to animate to going backward
//                  addHoverElements - Tells the implementation to take into account additional elements when using hover.
//                                     The implementation is responsible for all hover behavior (i.e. calling hide/showArrows when appropriate).
//
// Copyright (c) 2012 by Microsoft Corporation. All rights reserved.
//
/////////////////////////////////////////////////////////////////////////////////

define("swipeNav", ["jquery", "jqfn", "mediator", "swipeNavImpl", "safeCss", "track", "tabKeyPressed", "window"], function ($, jqfn, mediator, swipeNavImpl, safeCss, track, tabKeyPressed, window)
{
    var defaults = {
        // should navigation be circular
        carousel: true,
        // should navigation happen automatically
        autoRotate: false,
        // amount of time (in ms) to wait between auto rotations
        autoRotateWait: 5000,
        // amount of time to wait before hiding the arrow
        showArrowTime: 3500
    };
    var swipeNavEvents = {
        animate: "swipeNavAnimate",
        change: "swipeNavChange",
        update: "swipeNavUpdate",
        realign: "swipeNavRealign"
    };
    var swipeNavId = "swipeNavId";
    var nextId = 1;

    var focusEvent = "focus";
    var blurEvent = "blur";

    var timeoutValue;
    $(window).on("resize", function ()
    {
        // There is an issue when a large change happens (full to snap, or oriantation change) that the resulting resize is slightly off.
        // temporary fix until we can identify when resize isn't happening correctly.
        mediator.pub("windowResize");
        // clear the timeout so that multiple resize events from dragging the window doesn't create visable lag
        clearTimeout(timeoutValue);
        // use 230ms timeout to avoid potiential issues with animations (220ms)
        timeoutValue = setTimeout(function ()
        {
            mediator.pub("windowResize");
        }, 230);
    });

    var leftArrowString = '<a href="#" class="leftarrow fade" title="%SwipeNav.Arrow.LeftText%"><span>%SwipeNav.Arrow.LeftText%</span></a>';
    var rightArrowString = '<a href="#" class="rightarrow fade" title="%SwipeNav.Arrow.RightText%"><span>%SwipeNav.Arrow.RightText%</span></a>';
    var arrowHideClass = "fade";
    var bindingFunction = jqfn(applyBehavior, defaults);
    bindingFunction.event = swipeNavEvents;
    bindingFunction.id = swipeNavId;
    return bindingFunction;

    function childFilter()
    {
        return !$(this).hasClass("navtile");
    }

    function applyBehavior($elem, settings)
    {
        var safeCssGroup = safeCss.createGroup();
        var $children = $elem.children().filter(childFilter);
        var $childlinks = $("a", $children);
        var $container = $elem.parent();
        var childrenLength = $children.length;
        var $replayButtons;
        var loaded;

        // carousels change slides, but don't navigate the page.
        // gallery (non-carousel) fake a navigation: they fire unload, load, and refresh the ads.
        // because of that, gallery arrow clicks should be "click" events, and carousel should
        // be "click_nonnav"
        var arrowEventType = settings.carousel ? "click_nonnav" : "click";

        // if less than 3 slides reduce the functionality
        // only used to keep the styles maintained properly.
        var fullFunctionality = childrenLength > 2;

        if (fullFunctionality)
        {
            var pubId = nextId++;
            var inAutoRotate;
            var autoRotateTimeout;
            var $leftArrow = $(leftArrowString)
                .click(function (e)
                {
                    e.preventDefault();
                    e.stopImmediatePropagation();
                    impl.animate(-1);
                    track.trackEvent({ type: arrowEventType, target: this });
                })
                .on(focusEvent, function ()
                {
                    if (tabKeyPressed())
                    {
                        updateFunctions.showArrows();
                    }
                })
                .on(blurEvent, function ()
                {
                    if (tabKeyPressed())
                    {
                        updateFunctions.hideArrows();
                    }
                });

            var $rightArrow = $(rightArrowString)
                .click(function (e)
                {
                    e.preventDefault();
                    e.stopImmediatePropagation();
                    impl.animate(1);
                    track.trackEvent({ type: arrowEventType, target: this });
                })
                .on(focusEvent, function ()
                {
                    if (tabKeyPressed())
                    {
                        updateFunctions.showArrows();
                    }
                })
                .on(blurEvent, function ()
                {
                    if (tabKeyPressed())
                    {
                        updateFunctions.hideArrows();
                    }
                });

            var isLeftArrowShown;
            var isRightArrowShown;
            var areArrowsVisable;
            var hideArrowsTimeout;
            $container.append($leftArrow).append($rightArrow);

            $elem.data(swipeNavId, pubId);
            loaded = true;
        }
        else
        {
            $elem.removeClass("loading");
        }

        var updateFunctions = {
            slides: function (offset, trackSlide)
            {
                mediator.pubChannel(swipeNavEvents.update, pubId, offset);

                if (!settings.carousel)
                {
                    // position changed, might need to change arrows
                    updateArrows();
                }

                if (trackSlide)
                {
                    var previousTouchGesture = trackSlide["touch_gesture"];
                    trackSlide["touch_gesture"] = "swipe";
                    track.trackEvent({ type: "click", target: trackSlide, noSpin: 1 });
                    trackSlide["touch_gesture"] = previousTouchGesture;
                }

                if (!inAutoRotate)
                {
                    settings.autoRotate = false;
                }
                inAutoRotate = false;
            },
            hideArrows: function ()
            {
                areArrowsVisable = false;
                updateArrows();
            },
            showArrows: function ()
            {
                areArrowsVisable = true;
                updateArrows();
            }
        };

        var impl = swipeNavImpl(fullFunctionality, settings.carousel, $elem, $children, $container, safeCssGroup, updateFunctions);

        if (impl.addHoverElements && settings.addHoverSelector)
        {
            impl.addHoverElements($(settings.addHoverSelector));
        }

        if (fullFunctionality)
        {
            updateArrows();
        }


        return {
            setup: function ()
            {
                if (!loaded)
                {
                    return false;
                }

                if (fullFunctionality)
                {
                    $elem.addClass("loaded");
                    $leftArrow.show();
                    $rightArrow.show();
                    adjustArrowTop();
                    updateFunctions.showArrows();
                    hideArrowsTimeout = setTimeout(updateFunctions.hideArrows, settings.showArrowTime);

                    // reset (center orientation) on demand
                    mediator.subChannel(swipeNavEvents.realign, pubId, update);

                    // move to another slide on demand
                    mediator.subChannel(swipeNavEvents.animate, pubId, impl.animate);

                    // move to another slide on demand
                    mediator.subChannel(swipeNavEvents.change, pubId, impl.change);

                    $replayButtons = $(".relatedgalleries-replay-btn", $elem);
                    $replayButtons.on("click", replay);

                    if (settings.autoRotate)
                    {
                        autoRotateTimeout = setTimeout(autoRotate, settings.autoRotateWait);

                        // mousedown fires from mouse or touch
                        $container.one("mousedown", stopAutoRotate);
                        $childlinks.one(focusEvent, stopAutoRotateOnTabPress);
                    }

                    mediator.sub("windowResize", update);
                    mediator.sub("fullscreen", autoRotateFullscreenHandler);

                    $container.on("keydown", function (e)
                    {
                        // Left arrow
                        if (e.which == 37)
                        {

                            impl.animate(-1);
                            return false;
                        }
                            // Right arrow
                        else if (e.which == 39)
                        {
                            impl.animate(1);
                            return false;
                        }
                    });
                }

                impl.setup();
                return true;
            },
            teardown: function ()
            {
                $elem.removeClass("loaded");
                safeCssGroup.reset();

                if (fullFunctionality)
                {
                    $container.off("keydown");
                    updateFunctions.hideArrows();
                    $leftArrow.hide();
                    $rightArrow.hide();
                    mediator.unsubChannel(swipeNavEvents.realign, pubId, update);
                    mediator.unsubChannel(swipeNavEvents.animate, pubId, impl.animate);
                    mediator.unsubChannel(swipeNavEvents.change, pubId, impl.change);
                    $replayButtons.off("click", replay);
                    $container.off("mousedown", stopAutoRotate);
                    $childlinks.off(focusEvent, stopAutoRotateOnTabPress);
                    mediator.unsub("windowResize", update);
                    mediator.unsub("fullscreen", autoRotateFullscreenHandler);
                }

                impl.teardown();
            },
            update: update
        };

        function update()
        {
            if (fullFunctionality)
            {
                safeCssGroup($elem).css({
                    padding: ""
                });
                impl.resize();
                adjustArrowTop();
            }
        }

        function replay(event)
        {
            event.preventDefault();
            event.stopPropagation();
            // assume that if replay is clicked that we're not in a carousel and that offsets will get corrected
            mediator.pubChannel(swipeNavEvents.animate, pubId, -childrenLength);
        }

        function autoRotate()
        {
            if (settings.autoRotate)
            {
                inAutoRotate = true;
                mediator.pubChannel(swipeNavEvents.animate, pubId, 1);
                autoRotateTimeout = setTimeout(autoRotate, settings.autoRotateWait);
            }
        }

        function stopAutoRotateOnTabPress()
        {
            // If the user tabs into the control, stop rotating
            if (settings.autoRotate && tabKeyPressed())
            {
                stopAutoRotate();
            }
        }

        function stopAutoRotate()
        {
            settings.autoRotate = false;
        }

        function autoRotateFullscreenHandler(isFullscreen)
        {
            // when entering fullscreen, pause the autoRotate
            // when returning from fullscreen, resume autoRotate if needed
            if (isFullscreen)
            {
                clearTimeout(autoRotateTimeout);
            }
            else if (settings.autoRotate)
            {
                autoRotateTimeout = setTimeout(autoRotate, settings.autoRotateWait);
            }
        }

        function adjustArrowTop()
        {
            var top = ($container.height() - $rightArrow.height()) / 2;
            $leftArrow.css("top", top + "px");
            $rightArrow.css("top", top + "px");
        }

        // updates visablity of arrows if needed
        function updateArrows()
        {
            // depending on currentIndex, non-carousel may want to hide an arrow
            var shouldShowRightArrow = impl.hasNext();
            var shouldShowLeftArrow = impl.hasPrevious();

            // toggle visibility
            if (areArrowsVisable && shouldShowLeftArrow)
            {
                if (!isLeftArrowShown)
                {
                    $leftArrow.removeClass(arrowHideClass);
                    isLeftArrowShown = true;
                }
            }
            else if (isLeftArrowShown)
            {
                $leftArrow.addClass(arrowHideClass);
                isLeftArrowShown = false;
            }
            if (areArrowsVisable && shouldShowRightArrow)
            {
                if (!isRightArrowShown)
                {
                    $rightArrow.removeClass(arrowHideClass);
                    isRightArrowShown = true;
                }
            }
            else if (isRightArrowShown)
            {
                $rightArrow.addClass(arrowHideClass);
                isRightArrowShown = false;
            }

            if (hideArrowsTimeout)
            {
                clearTimeout(hideArrowsTimeout);
            }
        }
    }
});

/* WGINCLUDE: C:\PROJECTS\WORKSPACE2\MSNMETRO\MAIN\METROSDK\METROSDK\CONTENT\SOURCE\JS\CLASSIC\..\Shared\behaviors\infopaneNav.js */
/////////////////////////////////////////////////////////////////////////////////
//
// File: infopaneNav.js
// Defines: infopaneNav
// Dependencies: AMD, jQuery, jqfn, swipeNav, mediator, format
// Description: $elem has a number of children corresponding to the number of slides in the infopane.
//              Listens to the swipeNav events (using the channel from settings.swipeNavId).
//              Updates visuals based on swipeNav events.
//
// Copyright (c) 2012 by Microsoft Corporation. All rights reserved.
//
/////////////////////////////////////////////////////////////////////////////////

define("infopaneNav", ["jquery", "jqfn", "swipeNav", "mediator", "format"], function ($, jqfn, swipeNav, mediator, format)
{
    var defaults = {
        selectedClass: "selected",
        accentColor: "pink"
    };

    function childFilter()
    {
        return !$(this).hasClass("navtile");
    }


    return jqfn(function ($infopane, settings)
    {
        var currentSlide = 0;
        var $infopaneChildren = $infopane.children().filter(childFilter);
        var length = $infopaneChildren.length;
        var $nav;
        var $navChildren;

        return {
            setup: function ()
            {
                mediator.subChannel(swipeNav.event.update, settings.swipeNavId, swipeNavUpdateHandler);
                if ($nav)
                {
                    $nav.show();
                }
                else
                {
                    createNav();
                }
            },
            teardown: function ()
            {
                mediator.unsubChannel(swipeNav.event.update, settings.swipeNavId, swipeNavUpdateHandler);
                $nav.hide();
            },
            update: function ()
            {
                // empty so that view state switches don't call teardown and setup
            }
        };

        function swipeNavUpdateHandler(offset)
        {
            var newValue = (currentSlide + offset) % length;
            if (newValue < 0)
            {
                newValue += length;
            }
            $infopaneChildren.eq(currentSlide).removeClass(settings.selectedClass);
            $navChildren.eq(currentSlide).removeClass(settings.selectedClass);
            $infopaneChildren.eq(newValue).addClass(settings.selectedClass);
            $navChildren.eq(newValue).addClass(settings.selectedClass);
            currentSlide = newValue;
        }

        function createNav()
        {
            var navDiv = '<div class="slidecount bg {0}"/>';
            var navChildSpan = '<span{1} style="width:{0}"/>';

            // todo: class color through settings
            $nav = $(format(navDiv, settings.accentColor));
            var width = 100 / $infopaneChildren.length + "%";
            for (var ndx = 0; ndx < $infopaneChildren.length; ndx++)
            {
                var classAttribute = ndx ? '' : ' class="' + settings.selectedClass + '"';
                $nav.append(format(navChildSpan, width, classAttribute));
            }
            $navChildren = $nav.children();
            $infopane.after($nav);
        }

    }, defaults);
});

/* WGINCLUDE: C:\PROJECTS\WORKSPACE2\MSNMETRO\MAIN\METROSDK\METROSDK\CONTENT\SOURCE\JS\CLASSIC\..\Shared\behaviors\infopane.js */
/////////////////////////////////////////////////////////////////////////////////
//
// File: infopane.js
// Defines: infopane
// Dependencies: jQuery, jqfn, safeCss, swipeNav, infopaneNav, mediator, imgSrc
// Description: Apply infopane (slideshow) behavior
//
// Copyright (c) 2013 by Microsoft Corporation. All rights reserved.
// 
//////////////////////////////////////////////////////////////////////////////////

define("infopane", ["jquery", "jqfn", "safeCss", "swipeNav", "infopaneNav", "mediator", "imgSrc"], function ($, jqfn, safeCss, swipeNav, infopaneNav, mediator, imgSrc)
{
    var defaults = {
        autoRotate: true
    };

    function applyBehavior($elem, settings)
    {
        var safeCssGroup = safeCss.createGroup();
        var $infopane = $("ul", $elem);
        var swipeNavResults = swipeNav($infopane, null, settings);
        var swipeNavId = $infopane.data(swipeNav.id);
        var slideImages = getSlideImages();
        var slideCount = slideImages.length;
        var currentSlide = 0;
        var infopaneNavResults = infopaneNav($infopane, 0, $.extend({ swipeNavId: swipeNavId }, settings));
        
        return {
            setup: function()
            {
                if (!swipeNavResults.setup()) {
                    $elem.addClass("invalid");
                    return false;
                }
                
                mediator.subChannel(swipeNav.event.update, swipeNavId, indexChangeHandler);
                infopaneNavResults.setup();

                return true;
            },
            teardown: function()
            {
                safeCssGroup.reset();
                swipeNavResults.teardown();
                infopaneNavResults.teardown();
                mediator.unsubChannel(swipeNav.event.update, swipeNavId, indexChangeHandler);
            },
            update: function()
            {
                swipeNavResults.update();
                infopaneNavResults.update();
            }
        };

        // create an array that has an item for each slide, and each item is 
        // a collection of <img> elements under that slide.
        function getSlideImages()
        {
            // return a collection for the slides - the direct children under the infopane
            var $slides = $infopane.children().filter(function()
            {
                return !$(this).hasClass("navtile");
            });

            // create an array object of that length.
            var slideArray = new Array($slides.length);

            // for each slide element, create a collection of all the img elements underneath it
            // and assign it to the appropriate cell in the slide array
            $slides.each(function(ndx)
            {
                slideArray[ndx] = $("img", this);
            });

            return slideArray;
        }

        function indexFromOffset(offset)
        {
            var newValue = (currentSlide + offset) % slideCount;
            if (newValue < 0)
            {
                newValue += slideCount;
            }

            return newValue;
        }

        function indexChangeHandler(indexChange)
        {
            // update the current slide index with wrap-around
            currentSlide = indexFromOffset(indexChange);

            // preload the next n images in the given direction, in a circular manner
            var direction = indexChange < 0 ? -1 : 1;
            for(var ndx = 0; ndx <= 2; ++ndx)
            {
                slideImages[indexFromOffset(ndx*direction)].each(function()
                {
                    imgSrc.go(this);
                });
            }
        }
    }
    
    return jqfn(applyBehavior, defaults);
});


/////////////////////////////////////////////////////////////////////////////////
//
// File: landingPage.js
// Defines: 
// Dependencies: jqbind, c.dom
// Description: Classic landing page js, applies infopane behavior
//
// Copyright (c) 2013 by Microsoft Corporation. All rights reserved.
// 
//////////////////////////////////////////////////////////////////////////////////

require(["jqbind", "c.dom"], function (jqbind)
{
    // infopane behavior
    jqbind("infopane", ".ip");

    jqbind("truncate", ".featured p,.featured figcaption,.cluster p,.deptcluster p,.ip p,.ip h3,.contentlist h3,.contentlist p");
});

/* C:\Projects\Workspace2\MSNMetro\Main\MetroSDK\MetroSDK\Content\Source\js\Classic\channelHomepage.js */

/////////////////////////////////////////////////////////////////////////////////
//
// File: channelHomepage.js
// Defines: 
// Dependencies: 
// Description: Classic channel home page js
//
// Copyright (c) 2013 by Microsoft Corporation. All rights reserved.
// 
//////////////////////////////////////////////////////////////////////////////////


/* C:\Projects\Workspace2\MSNMetro\Main\MetroSDK\MetroSDK\Content\Source\js\Classic\base.post.js */

/////////////////////////////////////////////////////////////////////////////////
//
// File: base.post.js
// Defines: 
// Dependencies: 
// Description: Classic common bindings across all pages after page bindings.
//
// Copyright (c) 2012 by Microsoft Corporation. All rights reserved.
// 
//////////////////////////////////////////////////////////////////////////////////*

