ko.bindingHandlers.afterRender = {
    update: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
        var value = ko.unwrap(valueAccessor());
        var bindings = ko.unwrap(allBindings());

        if (!!bindings.with) {
            bindings.with.subscribe(function () {
                if (typeof value === "function" && bindings.with() != undefined) {
                    value();
                }
            });
        }
    },
};

ko.bindingHandlers.safeSrc = {
    update: function (element, valueAccessor) {
        var options = valueAccessor();
        var src = ko.unwrap(options.src);
        $('<img />').attr('src', src).on('load', function () {
            $(element).attr('src', src);
            callback = ko.unwrap(options.callback);
            if (typeof callback === "function") {
                callback();
            }
        }).on('error', function () {
            $(element).attr('src', ko.unwrap(options.fallback));
        });
    }
};

ko.bindingHandlers.do = {
    init: function (element, valueAccessor, allBindings, viewModel, bindingContext) {
        valueAccessor();
    },
};

ko.bindingHandlers.onClick = {
    init: function(element, valueAccessor, allBindingsAccessor, viewModel, context) {
        var accessor = valueAccessor();
        var clicks = 0;
        var timeout = 200;

        $(element).click(function(event) {
            if(typeof(accessor) === 'object') {
                var single = accessor.single;
                var double = accessor.double;
                clicks++;
                if (clicks === 1) {
                    setTimeout(function() {
                        if(clicks === 1) {
                            single.call(viewModel, context.$data, event);
                        } else {
                            double.call(viewModel, context.$data, event);
                        }
                        clicks = 0;
                    }, timeout);
                }
            } else {
                accessor.call(viewModel, context.$data, event);
            }
        });
    }
};