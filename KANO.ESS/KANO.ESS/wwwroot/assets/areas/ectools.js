function ks(o) {
    return kendo.stringify(o);
}

function kf(f,v) {
    return kendo.format(f,v);
}

function gp(obj, propName, def) {
    var r = obj.hasOwnProperty(propName) ? obj[propName] : def;
    return r;
}

function n2s(obj) {
    var fmt = "{0:N0}";
    return kendo.format(fmt,obj);
}

function fd(format, value, def) {
    if (def == undefined) def = "-";
    var ret = "";
    if (value == 0 || value == "") {
        ret = def;
    } else {
        ret = kendo.format(format, value);
    }
    return ret;
}

function tooggleCheckbox(obj, selectorTxt) {
    var cbxs = $(selectorTxt);
    var checked = obj.prop("checked");
    cbxs.prop("checked", checked);
}

function objectScript(fn, group) {
    var o = this;
    o.fn = fn;
    o.group = group;
    return o;
}

function addObjScript(obj, scr, group) {
    if (obj.data("ecscript") == undefined) obj.data("ecscript", []);
    var ss = obj.data("ecscript");
    if (typeof scr == "function") ss.push(new objectScript(scr, group == undefined ? "default" : group));
}

function runObjScript(obj, g) {
    if (obj.data("ecscript") != undefined) {
        var ss = obj.data("ecscript");
        if (g == undefined) g = "default";
        ss.forEach(function (obj) {
            if (obj.group == g) obj.fn();
        });
    }
}

function showErr(e, fn) {
    //alert(e.responseText);
    var errMsg = "";    
    if (typeof e == "string") errMsg = e;
    else if (e.hasOwnProperty("responseText")) errMsg = e.responseText;
    else if (e.hasOwnProperty("Message") && e.hasOwnProperty("Trace")) errMsg = e.Message + "\n" + e.Trace;
    else if (e.hasOwnProperty("Message")) e = e.Message;
    alert(errMsg);
    if (typeof fn == "function") fn(e);
}

//--- Kendo Related funciton
function makeField(label,inputDataBind,labelClass,fieldClass,inputClass) {
    var str = "<label class=\"" + labelClass + "\">" + label + "</label>\n";
    str += "<div class=\""+fieldClass+"\"><input class=\""+inputClass+"\" data-bind=\""+inputDataBind+"\"></div>";
    document.write(str);
}

function normalizeTree(objs, el, parentPreText, fnTr) {
    var ret = [];
    objs.forEach(function (obj) {
        var it = fnTr(obj, parentPreText);
        ret.push(it);
        if (obj.hasOwnProperty(el)) {
            var newParentPreText = parentPreText + it.shortTitle + " \\ ";
            //alert(it.title + " ==> " + newParentPreText);
            var childs = normalizeTree(obj[el], el, newParentPreText, fnTr);
            childs.forEach(function (c) {
                ret.push(c);
            });
        }
    });
    return ret;
}

function input2datePicker(objects) {
    $.each(objects, function (idx, obj) {
        var jobj = $(obj);
        var dateval = jobj.val();
        if (jobj.data("kendoDatePicker") == undefined) {
            var fmt = jobj.attr("format") == undefined ? jsonDateFormat : jobj.attr("format");
            var depth = jobj.attr("depth") == undefined ? "month" : jobj.attr("depth");
            var start = jobj.attr("start") == undefined ? "month" : jobj.attr("start");
            var min = jobj.attr("min") == undefined ? new Date(1900,1,1) : jsonDate(jobj.attr("min"));
            var max = jobj.attr("max") == undefined ? new Date(3000, 12, 31) : jsonDate(jobj.attr("max"));
            jobj.kendoDatePicker({
                format: fmt, start: start,
                min: min, max:max,
                depth: depth, parseFormats: ["dd-MMM-yyyy"]
            });
        }
        jobj.data("kendoDatePicker").value(jsonDateStr(dateval));
    });
}
//--- End of Kendo

//--- Date related function
function jsonDate(strDt) {
    if (strDt == undefined) return "";
    var dt = str2date(strDt);
    if (dt.getFullYear() <= 1970 || dt.getFullYear() == 1) dt = "";
    return dt;
}

function jsonDateStr(dtSource, format) {
    var dt = str2date(dtSource);
    if (dt == null || dt == undefined || dt == "") return "";
    if (dt.getFullYear() <= 1970 || dt.getFullYear() == 1) return "";
    var ret = kendo.toString(dt, format == undefined ? jsonDateFormat : format);
    if (ret.indexOf("NaN") >= 0) return "";
    return ret;
}

//alert(jsonDateStr("25-Mar-2014"));

function str2date(dtSource) {
    if (dtSource == null) return "";
    dtSource = dtSource.toString();
    var dt = dtSource;
    if (dtSource.substr(0, 6) == "/Date(") {
        var dtParse = Date.parse(dtSource);
        if (isNaN(dtParse)) {
            var intMs = parseInt(dtSource.substr(6));
            dt = new Date(intMs);
        }
        else {
            dt = new Date(dtParse);
        }
        //alert(dt);
        dt = new Date(dt.getTime() + dt.getTimezoneOffset() * 60000);
    }
    else if (dtSource.length == 5 && dtSource.substr(2, 1) == ":") {
        var times = dtSource.split(":");
        dt = new Date();
        dt = new Date(dt.getFullYear(), dt.getMonth(), dt.getDate(), times[0], times[1]);
    }
    else {
        dt = new Date(dtSource);
        if (!isDate(dt)) dt = jsonDateFromStr(dtSource);
        //dt = new Date(dt.getTime() + dt.getTimezoneOffset() * 60000);
    }
    return dt;
}

function isDate(dt) {
    return (dt instanceof Date && !isNaN(dt.valueOf()));
}

function date2str(dt, format) {
    if (dt instanceof Date === false) return "";
    if (dt.getFullYear() <= 1970 || dt.getFullYear() == 1) return "";
    return kendo.toString(dt, format == undefined ? jsonDateFormat : format);
}

//ie: 01-Jan-2013
function jsonDateFromStr(strDt) {
    var yr = parseInt(strDt.substr(7, 4));
    var mth = strDt.substr(3, 3);
    var day = parseInt(strDt.substr(0, 2));
    switch (mth) {
        case "Jan": mth = 0; break;
        case "Feb": mth = 1; break;
        case "Mar": mth = 2; break;
        case "Apr": mth = 3; break;
        case "May": mth = 4; break;
        case "Jun": mth = 5; break;
        case "Jul": mth = 6; break;
        case "Aug": mth = 7; break;
        case "Sep": mth = 8; break;
        case "Oct": mth = 9; break;
        case "Nov": mth = 10; break;
        case "Dec": mth = 11; break;
    }
    var dt = new Date(yr, mth, day);
    if (dt.getFullYear() == 1900) dt = "";
    return dt;
}

function toUTC(dt) {
    if (dt == undefined || dt == "") return "";
    dt = new Date(dt.getTime() + dt.getTimezoneOffset() * 60000);
    return dt;
}
//--- end of date related function

function obsArray2Js(obs) {
    var ret = [];
    obs().forEach(function (obj) {
        ret.push(ko.mapping.toJS(obj));
    });
    return ret;
}

function getObjectProperties(obj) {
    var keys = [];
    for (var key in obj) {
        keys.push(key);
    }
    return keys;
}

function jsonObjsConvDate(as, dateOrStr) {
    as.forEach(function (e) {
        e = jsonObjConvDate(e, dateOrStr);
    });
    return as;
}

function jsonObjConvDate(e, dateOrStr) {
    if (dateOrStr == undefined) dateOrStr = "date";
    var keys = getObjectProperties(e);
    keys.forEach(function (k) {
        if (typeof e[k] == "string" && e[k] != null && e[k] != undefined) {
            if (e[k].indexOf("/Date") >= 0) {
                var dt = dateOrStr=="str" ? jsonDateStr(e[k]) : jsonDate(e[k]);
                e[k] = dt;
            }
        }
        else if (typeof e[k] == "object") {
            e[k] = jsonObjConvDate(e[k]);
        }
    });
    return e;
}

function ajax(url, type, data, fnOk, fnNok) {
    return $.ajax({
        url: url,
        type: type,
        data: data,
        cache:false,
        contentType: "application/json; charset=utf-8",
        success: function (data) {
            data = jsonObjConvDate(data);
            if (typeof fnOk == "function") fnOk(data);
            koResult = "OK";
        },
        error: function (request, status, error) {                      
            if (typeof fnNok == "function") {
                var response = request.responseJSON || {};
                if (response.StatusCode) {
                    fnNok(response);
                    return;
                }else{
                    fnNok({});
                    return;
                }          
            }
            else {
                if (typeof isLoading == "function") {
                    isLoading(false);
                }

                if (_isError401(request)) {
                    swalRedirection("Unauthorized Access", "Redirecting to login page");                    
                    return;
                }
                swalFatalHTML("Fatal Error", "There was an error posting the data to the server: " + request.responseText, `${request.status} - ${request.statusText}`);               
            }
        }
    });
}

function ajaxPost(url, data, fnOk, fnNok) {
    return $.ajax({
        url: url,
        type: "POST",
        data: ko.mapping.toJSON(data),
        cache: false,
        contentType: "application/json; charset=utf-8",
        cacheControl: "no-cache",
        headers: { "Cache-Control": "no-cache" },
        success: function (data) {
            data = jsonObjConvDate(data);
            if (typeof fnOk == "function") fnOk(data);
            koResult = "OK";
        },
        error: function (request, status, error) {                  
            if (typeof fnNok == "function") {
                var response = request.responseJSON;
                if (response && response.StatusCode) {
                    fnNok(response);
                    return;
                }                                
            }
            else {
                if (typeof isLoading == "function") {
                    isLoading(false);
                }

                if (_isError401(request)) {
                    swalRedirection("Unauthorized Access", "Redirecting to login page");                    
                    return;
                }
                console.log(request);

                swalFatalHTML("Fatal Error", "There was an error posting the data to the server: " + request.responseText, `${request.status} - ${request.statusText}`);               
                return;
            }
            if (status == "timeout") {
                window.location.reload();
            } else {
                var strStatus = (status) ? `\nStatus  : ${JSON.stringify(status)}`:"";
                var strError = (error) ? `\nError  : ${JSON.stringify(error)}` : "";
                console.log(`There was an error : \n Request : ${JSON.stringify(request)}${strStatus}${strError}`);
                swalWarning("Login", "Your internet connectivity is unstable or perhaps offline. Please try again later.");
                if (typeof isLoading == "function") {
                    isLoading(false);
                }
            }
        },
    });
}

function ajaxPostUpload(url, datax, fnOk, fnNok) {
    return $.ajax({
        url: url,
        type: "POST",
        data: datax,
        cache: false,
        processData: false,
        contentType: false,
        success: function (data) {
            data = jsonObjConvDate(data);
            if (typeof fnOk == "function") fnOk(data);
            koResult = "OK";
        },
        error: function (request, status, error) {            
            if (typeof fnNok == "function") {
                var response = request.responseJSON || {};
                if (response.StatusCode) {
                    fnNok(response);
                    return;
                }else{
                    fnNok({});
                    return;
                }          
            }
            else {
                if (typeof isLoading == "function") {
                    isLoading(false);
                }

                if (_isError401(request)) {
                    swalRedirection("Unauthorized Access", "Redirecting to login page");                    
                    return;
                } 

                swalFatalHTML("Fatal Error", "There was an error posting the data to the server: " + request.responseText, `${request.status} - ${request.statusText}`);               
                return;
            }

            if (status == "timeout") {
                window.location.reload();
            } else {
                var strStatus = (status) ? `\nStatus  : ${JSON.stringify(status)}` : "";
                var strError = (error) ? `\nError  : ${JSON.stringify(error)}` : "";
                console.log(`There was an error : \n Request : ${JSON.stringify(request)}${strStatus}${strError}`);
                swalWarning("Warning", "Your internet connectivity is unstable or perhaps offline. Please try again later.");
                if (typeof isLoading == "function") {
                    isLoading(false);
                }
            }
        },
    });
}

function firstDayOfMonth(date) {
    return moment(date).startOf('month').toDate();
}

function lastDayOfMonth(date) {
    return moment(date).endOf('month').toDate();
}

function _isError401(error) {
    if (error.hasOwnProperty("status") && error.status == 401) {
        window.location.href = "/";
        return true;
    }
    return false;
}

async function swalConfirm(title, text) {
    text = text || "";
    return swal({
        title: title,
        html: text.replace(/(?:\\[rn])+/g, '<br />'),
        type: "question",
        showCancelButton: true,
        confirmButtonText: "Yes",
        cancelButtonText: "No",
        reverseButtons: true
    });
}

async function swalAlert(title, text) {
    text = text || "";
    return swal({
        title: title,
        html: text.replace(/(?:\\[rn])+/g, '<br />'),
        type: "warning"
    });
}

async function swalError(title, text) {
    text = text || "";
    return swal({
        title: title,
        html: text.replace(/(?:\\[rn])+/g, '<br />'),
        type: "error"
    });
}

async function swalWarning(title, text) {
    text = text || "";
    return swal({
        title: title,
        html: text.replace(/(?:\\[rn])+/g, '<br />'),
        type: "warning"
    });
}

var _swalFatals = [];
async function swalFatal(title, text, detail) {
    // TODO : remove this on release
    return;       
    text = text || "";
    text.replace(/\t/g, '').replace(/(?:\\[rn])+/g, '<br />');    
    if (detail) {
        detail = `<strong>${detail}</strong><br/>`;
    } else {
        detail = "";
    }

    _swalFatals.push({
        title: title,
        html: `
            ${detail}            
            <div style="resize:none;width: 100%;min-height:200px;max-height:200px;overflow-y: auto;border:solid thin #333;text-align: left;font-family: serif;color: black;" onclick="copyText(this)">${text.replace(/(?:\\[rn])+/g, '<br />')}</div>            
        `,
        type: "error"
    });

    doSwalFatal();
}

async function swalFatalHTML(title, text, detail) {  
    // TODO : remove this on release
    return;                 
    text = text || "";
    text = text.replace(/\t/g, '').replace(/(?:\\[rn])+/g, '<br />').replace(/"/g, "\'");
    if (detail) {
        detail = `<strong>${detail}</strong><br/>`;
    } else {
        detail = "";
    }

    _swalFatals.push({
        title: title,
        html: `
            ${detail}
            <iframe id="iframeFatal" style="resize:none; width: 100%; border:solid thin; margin-top:10px" height="200" onclick="copyText(this)" srcdoc="${text}"></iframe>
            `,
        type: "error"
    });
    
    doSwalFatal();
}

async function doSwalFatal() {
    if (_swalFatals.length == 1) {
        for (let index = 0; index < _swalFatals.length; index++) {
            const o = _swalFatals[index]
            await swal(o);
        }        
        _swalFatals = [];
    }    
}

async function swalRedirection(title, text) {
    text = text || "";
    return swal({
        showConfirmButton: false,
        allowOutsideClick: false,
        allowEscapeKey: false,
        html: `
            <div class="spinner-border" role="status" style="width: 3rem; height: 3rem; margin-bottom:10px">
              <span class="sr-only">Loading...</span>
            </div>            
            <b style="margin-bottom:7px; margin-top:15px; font-weight:bold; display:block">${title}</b>			
            <small>${text}</small>`,
    });
}

async function swalSuccess(title, text) {
    text = text || "";
    return swal({
        title: title,
        html: text.replace(/(?:\\[rn])+/g, '<br />'),
        type: "success"
    });
}

async function swalInfo(title, text) {
    text = text || "";
    return swal({
        title: title,
        html: text.replace(/(?:\\[rn])+/g, '<br />'),
        type: "info"
    });
}

async function swalConfirmText(title, text, placeholder) {
    text = text || "";
    return swal({
        title: title,
        input: 'text',
        inputPlaceholder: placeholder,
        html: text.replace(/(?:\\[rn])+/g, '<br />'),
        type: "question",
        showCancelButton: true,
        confirmButtonText: "Yes",
        cancelButtonText: "No",
        reverseButtons: true,
        // allowOutsideClick:false,
        // allowEscapeKey:false,
    });
}

function copyText(text) {
    let input;
    let remove = false;
    if (typeof text == "string") {
        let id = "eaciit-clipboard";
        input = document.getElementById(id);

        if (!input) {
            input = document.createElement("textarea");
            document.body.appendChild(input);
        }

        input.id = id;
        input.value = text;
        remove = true;
    } else if (
        typeof text == "object" &&
        !!text.nodeName &&
        (text.nodeName.toLowerCase() == "textarea" ||
            text.nodeName.toLowerCase() == "input")
    ) {
        input = text;
    } else if (
        typeof text == "object" &&
        !!text.nodeName &&
        (text.nodeName.toLowerCase() == "div")
    ) {
        let id = "eaciit-clipboard";
        input = document.getElementById(id);

        if (!input) {
            input = document.createElement("textarea");
            document.body.appendChild(input);
        }

        input.id = id;
        input.value = text.innerText;
        remove = true;
    } else {
        return;
    }

    input.focus();
    input.select();
    document.execCommand("copy");

    if (remove) {
        input.remove();
    }
}

function getUrlVars() {
    var vars = [], hash;
    var hashes = window.location.href.slice(window.location.href.indexOf('?') + 1).split('&');
    for (var i = 0; i < hashes.length; i++) {
        hash = hashes[i].split('=');
        vars[hash[0]] = hash.slice(1).join("=");
    }
    return vars;
}

function strArray2DropdownList(arrayOfString, fnText) {
    if (!fnText || typeof fnText != "function") {
        fnText = function (txt) {
            return txt;
        }
    }

    return arrayOfString.map((v, k) => {
        var o = {
            "text":fnText(v),
            "value":k
        };

        return o;
    });
}

function standarizeDateMonthYear(date) {
    if (!date) {
        return "-";
    }

    if (!(date instanceof Date)) {
        date = str2date(date);
    }

    if (date.getTime() < 0) {
        return "-";
    }

    return moment(date).format("MMMM YYYY");
}

function standarizeDate(date) {
    if (!date) {
        return "-";
    }

    if (!(date instanceof Date)) {
        date = str2date(date);
    }

    if (date.getTime() < 0) {
        return "-";
    }
    return moment(date).format("DD MMM YYYY");
}

function standarizeDateFull(date) {
    if (!date) {
        return "-";
    }

    if (!(date instanceof Date)) {
        date = str2date(date);
    }

    if (date.getTime() < 0) {
        return "-";
    }
    return moment(date).format("dddd, DD MMMM YYYY");
}

function standarizeDateTime(date, noDay = false) {
    if (!date) {
        return "-";
    }

    if (!(date instanceof Date)) {
        date = str2date(date);
    }

    if (date.getTime() < 0) {
        return "-";
    }
    if(noDay){
        return moment(date).format("DD MMM YYYY HH:mm");    
    }
    return moment(date).format("dddd, DD MMM YYYY HH:mm");
}

function standarizeTime(date) {
    if (!date) {
        return "-";
    }

    if (!(date instanceof Date)) {
        date = str2date(date);
    }

    if (date.getTime() < 0) {
        return "-";
    }
    return moment(date).format("HH:mm");
}

function relativeDate(date) {
    if (!date) {
        return "-";
    }

    if (!(date instanceof Date)) {
        date = str2date(date);
    }

    if (date.getTime() < 0) {
        return "-";
    }
    return moment(date).fromNow();
}

function getRandom(min, max) {
    return Math.floor(Math.random() * (max - min) + min);
}

function humanizeBytes(bytes, precision) {
    if (isNaN(parseFloat(bytes)) || !isFinite(bytes)) return '-';
    if (typeof precision === 'undefined') precision = 1;
    var units = ['bytes', 'kB', 'MB', 'GB', 'TB', 'PB'];
    var number = Math.floor(Math.log(bytes) / Math.log(1024));
    return (bytes / Math.pow(1024, Math.floor(number))).toFixed(precision) + ' ' + units[number];
}

function titleCase(str) {
    str = str + "";
    return str.split(" ").map(x => {
        return x.charAt(0).toUpperCase() + x.slice(1);
    }).join(" ");
}

function delay(ms) {
    return new Promise(function (resolve, reject) {
        setTimeout(function () {
            resolve("anything");
        }, ms);
    });
}

function camelToTitle(str) {
    return titleCase(str.replace(/[\w]([A-Z])/g, function (m) {
        return m[0] + " " + m[1];
    }));
}

function snakeToCamel(str) {
    return str.replace(/(_\w)/g, function (m) {
        return m[1].toUpperCase();
    });
}

function camelToSnake(str) {
    return str.replace(/[\w]([A-Z])/g, function (m) {
        return m[0] + "_" + m[1];
    }).toLowerCase();
}

function calculateChangePassword (lastchangedate, thresholdChangePassword) {
    let today = (new Date()).getTime()
    let lastchangepassword = moment(lastchangedate, "DD-MM-YYYY").valueOf()
    let days = (today - lastchangepassword) / (1000 * 3600 * 24)
    if (days > thresholdChangePassword) {
        return true
    }   
    return false
}

function calculateChangePasswordCount(lastchangedate) {
    let today = (new Date()).getTime()
    let lastchangepassword = moment(lastchangedate, "DD-MM-YYYY").valueOf()
    let days = (today - lastchangepassword) / (1000 * 3600 * 24)
    let out  = Math.ceil(days);

    if(out > 1000){
        return "many";
    }

    return out
}