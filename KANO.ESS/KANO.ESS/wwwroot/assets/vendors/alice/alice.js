/*
 * Alice.js by Zecchan Silverlake
 */

/* ================== AliceEnumerable class ===================== */
function AliceEnumerable() {
    this.initialize.apply(this, arguments);
}

AliceEnumerable.isEnumerable = function (obj) {
    return typeof obj === "object" && obj._className === "AliceEnumerable";
};

AliceEnumerable.prototype.initialize = function (context) {
    if (!AliceEnumerable.isEnumerable(context) && !Array.isArray(context)) throw new Error("Context must be an array or AliceEnumerable object!");
    this.Context = AliceEnumerable.isEnumerable(context) ? context.toArray() : context;
    this._className = "AliceEnumerable";
};

AliceEnumerable.prototype.each = function (iterator) {
    for (var idx in this.Context) {
        var val = this.Context[idx];
        if (typeof iterator === "function") iterator(val, idx);
    }
    return this;
};

AliceEnumerable.prototype.select = function (selector) {
    var res = [];
    this.each(function (val) {
        if (typeof selector === "function") {
            res.push(selector(val));
        }
    });
    return new AliceEnumerable(res);
};

AliceEnumerable.prototype.where = function (filter) {
    var res = [];
    this.each(function (val) {
        if (typeof filter === "function") {
            if (filter(val) === true)
                res.push(val);
        }
    });
    return new AliceEnumerable(res);
};

AliceEnumerable.prototype.first = function (defaultValue) {
    var val = this.Context[0];
    if (val === undefined) return defaultValue;
    return val;
};

AliceEnumerable.prototype.last = function (defaultValue) {
    var val = this.Context[this.Context.length - 1];
    if (val === undefined) return defaultValue;
    return val;
};

AliceEnumerable.prototype.toArray = function () {
    var res = [];
    this.each(function (val) {
        res.push(val);
    });
    return res;
};

AliceEnumerable.prototype.remove = function (valorfunc, instance) {
    var res = [];
    instance = Number.isInteger(instance) ? Number(instance) : -1;
    var removed = 0;
    this.each(function (val) {
        if (removed < instance || instance < 0) {
            if (typeof valorfunc === "function") {
                if (valorfunc(val) !== true) {
                    res.push(val);
                }
            } else {
                if (val !== valorfunc)
                    res.push(val);
            }
        } else res.push(val);
    });
    return new AliceEnumerable(res);
};

AliceEnumerable.prototype.removeAt = function (index) {
    this.Context.splice(index, 1);
    return this;
};

AliceEnumerable.prototype.add = function (value) {
    this.Context.push(value);
    return this;
};

AliceEnumerable.prototype.insert = function (index, value) {
    this.Context.splice(index, 0, value);
    return this;
};

AliceEnumerable.prototype.count = function () {
    return this.Context.length;
};

AliceEnumerable.prototype.sort = function (comparer) {
    this.Context.sort(comparer);
    return this;
};

AliceEnumerable.prototype.join = function (array, where, joinType, merge) {
    var col = new AliceEnumerable(array);
    var res = [];
    joinType = joinType || "left";
    if (joinType !== "left" || joinType !== "inner" || joinType !== "right") joinType = "left";

    var left = joinType === "left" || joinType === "inner" ? this : col;
    var right = joinType === "left" || joinType === "inner" ? col : this;

    merge = merge !== false;

    left.each(function (valA) {
        var match = right.where(function (valB) {
            return typeof where === "function" ? where(valA, valB) === true : false;
        });

        if (match.count() === 0 && joinType === "inner") return;

        match.each(function (valB) {
            var o = {};
            if (merge) {
                Object.assign(o, valA);
                Object.assign(o, valB);
            } else {
                o.Left = valA;
                o.Right = valB;
            }
            res.push(o);
        });
    });

    return new AliceEnumerable(res);
};

/* ================== AliceInput class ===================== */
function AliceDOM() {
    this.initialize.apply(this, arguments);
}

AliceDOM.prototype.initialize = function (context) {
    this.Context = $(context);
    this.BoundEvents = [];
    this.Handlers = {};
};

AliceDOM.prototype.filterInput = function (charset, allowControl) {
    var opt = evalAsFunction($(this.Context).data("filter-input"));
    opt.charset = charset !== undefined ? charset : opt.charset;
    opt.allowControl = (allowControl || opt.allowControl) === true;
    $(this.Context).data("filter-input", opt);
    this.on("input keypress", "filterInput", function (ev) {
        var opt = $(this).data("filter-input");
        if (typeof opt.charset === "string") {
            var key = ev.originalEvent.data || ev.originalEvent.key;
            if (opt.charset.indexOf(key) < 0) ev.preventDefault(); 
        }
        var val = $(this).val();
        var expr = '[^' + opt.charset + ']+';
        var regex = new RegExp(expr);
        val = val.replace(regex, "");
        $(this).val(val);
    });
};

AliceDOM.prototype.on = function (events, handlerId, handler) {
    if (typeof events !== "string") return false;
    var ev = events.split(" ");
    var h = [];
    for (var k in ev) {
        var evid = ev[k];
        if (evid) {
            if (!this.BoundEvents.includes(evid)) {
                $(this.Context).on(evid, function (evt) {
                    $(this).aliceDOM().trigger(evt.type, arguments);
                });
                this.BoundEvents.push(evid);
            }
            h.push(evid);
        }
    }
    if (typeof handlerId === "string" && typeof handler === "function" && !this.Handlers[handlerId]) {
        this.Handlers[handlerId] = {
            Handles: h,
            Handler: handler
        };
    }
};

AliceDOM.prototype.trigger = function (event, args) {
    for (var k in this.Handlers) {
        var h = this.Handlers[k];
        if (h.Handles.includes(event)) {
            h.Handler.apply($(this.Context), args);
        }
    }
};

/* ================== AlicePromise class ===================== */
function AlicePromise() {
    this.initialize.apply(this, arguments);
}

AlicePromise._queuePool = {};

AlicePromise.addQueue = function (queue, promise) {
    this._queuePool[queue] = this._queuePool[queue] || [];
    this._queuePool[queue].push(promise);
};

AlicePromise.execQueue = function (queue) {
    var pool = this._queuePool[queue];
    if (pool && pool.length > 0 && pool[0]._status === "pending") {
        pool[0].execute();
    }
};

AlicePromise.prototype.initialize = function (executor, queue) {
    if (typeof executor !== "function")
        throw new Error("Executor must be a function");
    this._executor = executor;
    this._status = "pending";
    if (queue && typeof queue === "string")
        this._queue = queue;
};

AlicePromise.prototype.execute = function () {
    if (this._status !== "pending") return false;
    if (this._queue && !this._queued) {
        this._queued = true;
        AlicePromise.addQueue(this._queue, this);
        AlicePromise.execQueue(this._queue);
    } else {
        this._status = "running";
    }
};

AlicePromise.prototype.then = function () {

};

function evalAsFunction(str) {
    return eval('(function() { return ' + str + '; })()');
}

/* ================== jQuery Extension ===================== */
(function ($) {
    // Plugins
    $.fn.extend({
        aliceDOM: function () {
            var ad = $(this).data("aliceDOM") || new AliceDOM(this);
            $(this).data("aliceDOM", ad);
            return ad;
        },
        filterInput: function (charset, allowControl) {
            $(this).each(function (idx, ele) {
                $(ele).aliceDOM().filterInput(charset, allowControl);
            });
            return this;
        }
    });

    // Extend root
    $.from = function (context) {
        return new AliceEnumerable(context);
    };

    // Init
    $(function () {
        $("[data-filter-input]").filterInput();
    });
})(jQuery);

// Factory function
var al = function (context) {
    var t = typeof context;
    if (Array.isArray(context) || AliceEnumerable.isEnumerable(context))
        return new AliceEnumerable(context);
};

// Extend functions
al.format = {};
al.format.fileSize = function (size) {
    var sizes = ["Bytes", "KiB", "MiB", "GiB", "TiB", "PiB"];
    size = Number(size);
    if (!size || typeof size !== "number") size = 0;

    var idx = 0;
    while (size >= 1000) {
        idx++;
        size /= 1024;
    }

    return Math.round(size * 100) / 100 + " " + sizes[idx];
};
 
// Alias
var alice = al;