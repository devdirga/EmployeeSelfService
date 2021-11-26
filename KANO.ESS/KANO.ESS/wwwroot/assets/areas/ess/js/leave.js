const DEFAULT_DATE = new Date(1900, 0, 1);
const DEFAULT_DATE_FORMAT = "yyyy-MM-dd'T'HH:mm:ss.SSS'Z'";

model.newLeave = function (data) {
    if (data) {
        return ko.mapping.fromJS(Object.assign(_.clone(this.proto.Leave), _.clone(data)));
    }
    return ko.mapping.fromJS(this.proto.Leave);
};

model.newLeaveInfo = function () {
    return ko.mapping.fromJS(this.proto.LeaveMaintenance);
};

var mindate;
//data
model.data.minDateFromLeave = ko.observable(new Date);
model.data.leave = ko.observable(model.newLeave());
model.data.leaves = [];
model.data.info = ko.observable(model.newLeaveInfo());
model.data.typeleave = ko.observableArray();
model.data.anchorRemainder = ko.observable();
model.data.anchorPending = ko.observable();
model.data.remainder = ko.observable();
model.data.history = ko.observableArray();
model.data.holiday = ko.observableArray();
model.data.holidaystring = ko.observableArray();
model.data.dateleave = ko.observableArray();
model.data.dateleavestart = ko.observable();
model.data.dateleaveend = ko.observable();
model.data.pending = ko.observable(0);
model.data.pendingrequest = ko.observable(0);
model.data.subtitution = ko.observableArray();
model.data.maxdayofleave = ko.observable();
model.data.nowreminder = ko.observable();
model.data.prevreminder = ko.observable();
model.data.allreminder = ko.observable();
model.data.selectedDates = ko.observableArray([]);
model.data.employeeID = ko.observable();
model.data.infoTypeLeave = ko.observable();

model.list.datepickstring = ko.observableArray([]);

model.is.cancelable = ko.observable(false);
model.is.indicatorVisible = ko.observable(false);
model.is.requestEnable = ko.observable(true);
model.is.processleave = ko.observable(true);

//Method UI Component Render
var startSchedule = "", endSchedule = "";
let initializePending = 0;

model.render.SchedulerCalendar = function () {
    $("#scheduler-leave").kendoScheduler({
        date: new Date(),
        footer: false,
        header: true,
        timezone: "Asia/Jakarta",
        editable: false,
        currentTimeMarker: {
            useLocalTimezone: false
        },
        height: 800,
        views: [
            {
                type: "month",
                group: {
                    date: true
                }
            }
        ],
        navigate: function (e) {
            if (e.action == "next" || e.action == "previous") {

                model.get.ScheduleLeave();
            }
        },
    });

    model.get.ScheduleLeave();
}

let _loadingScheduler = false;
model.get.ScheduleLeave = async function () {
    setTimeout(function () {
        _loadingScheduler = true;
        model.data.leaves = [];
        var scheduler = $("#scheduler-leave").data("kendoScheduler");
        var view = scheduler.view();
        var param = {
            Start: view.startDate(),
            Finish: view.endDate(),
        }

        setTimeout(async function () {
            while (_loadingScheduler) {
                kendo.ui.progress(scheduler.element.find(".k-scheduler-content"), true);
                await delay(300);
            }
        });

        // local data
        //var res = { "StatusCode": 200, "Message": null, "Data": { "Leaves": [{ "Schedule": { "TrueMonthly": 0.064516129032258063, "Month": 0.032854209445585217, "Days": 1.0, "Hours": 24.0, "Seconds": 86400.0, "Start": "2020-03-23T19:00:00+07:00", "Finish": "2020-03-24T19:00:00+07:00" }, "Description": "Cuti Tahunan 2019", "Type": "68", "TypeDescription": null, "AddressDuringLeave": null, "ContactDuringLeave": null, "SubtituteEmployeeID": "8309140001", "SubtituteEmployeeName": null, "PendingRequest": 0, "Filename": null, "Fileext": null, "Checksum": null, "Accessible": false, "Id": null, "Status": 1, "AXRequestID": null, "AXID": 5637245208, "EmployeeID": "8012160022", "EmployeeName": null, "Reason": null, "OldData": null, "NewData": null, "CreatedDate": "0001-01-01T00:00:00", "Action": 0, "LastUpdate": "0001-01-01T00:00:00", "UpdateBy": null }], "Holidays": [{ "EmployeeID": "8012160022", "LoggedDate": "2020-02-23T19:00:00+07:00", "AbsenceCode": "O", "IsLeave": false, "RecId": 0 }, { "EmployeeID": "8012160022", "LoggedDate": "2020-02-29T19:00:00+07:00", "AbsenceCode": "O", "IsLeave": false, "RecId": 0 }, { "EmployeeID": "8012160022", "LoggedDate": "2020-03-01T19:00:00+07:00", "AbsenceCode": "O", "IsLeave": false, "RecId": 0 }, { "EmployeeID": "8012160022", "LoggedDate": "2020-03-07T19:00:00+07:00", "AbsenceCode": "O", "IsLeave": false, "RecId": 0 }, { "EmployeeID": "8012160022", "LoggedDate": "2020-03-08T19:00:00+07:00", "AbsenceCode": "O", "IsLeave": false, "RecId": 0 }, { "EmployeeID": "8012160022", "LoggedDate": "2020-03-14T19:00:00+07:00", "AbsenceCode": "", "IsLeave": false, "RecId": 0 }, { "EmployeeID": "8012160022", "LoggedDate": "2020-03-15T19:00:00+07:00", "AbsenceCode": "", "IsLeave": false, "RecId": 0 }, { "EmployeeID": "8012160022", "LoggedDate": "2020-03-21T19:00:00+07:00", "AbsenceCode": "", "IsLeave": false, "RecId": 0 }, { "EmployeeID": "8012160022", "LoggedDate": "2020-03-22T19:00:00+07:00", "AbsenceCode": "", "IsLeave": false, "RecId": 0 }, { "EmployeeID": "8012160022", "LoggedDate": "2020-03-28T19:00:00+07:00", "AbsenceCode": "", "IsLeave": false, "RecId": 0 }, { "EmployeeID": "8012160022", "LoggedDate": "2020-03-29T19:00:00+07:00", "AbsenceCode": "", "IsLeave": false, "RecId": 0 }, { "EmployeeID": "8012160022", "LoggedDate": "2020-04-04T19:00:00+07:00", "AbsenceCode": "", "IsLeave": false, "RecId": 0 }] }, "Total": 0 };

        var days = view.content.find("td");
        ajaxPost("/ESS/Leave/GetCalendar", param, function (res) {
            console.log("res leave:", res);
            res.Data.Leaves.map(function (l) {
                model.data.leaves.push({
                    id: kendo.guid(),
                    AXID: l.AXID,
                    start: moment.utc(l.Schedule.Start).toDate(),
                    end: moment.utc(l.Schedule.Finish).toDate(),
                    title: l.Description,
                    Type: l.Type,
                    EmployeeID: l.EmployeeID,
                    SubtituteEmployeeID: l.SubtituteEmployeeID,
                    Reason: l.Reason,
                    Description: l.Description,
                    Filename: (l.Filename) ? l.Filename + "." + l.Fileext : "No Document",
                    Status: l.Status
                });
            });

            scheduler.dataSource.data(model.data.leaves);

            var holidays = [];
            var holidaysMap = {};
            res.Data.Holidays.map(function (holiday) {
                var h = moment(holiday.LoggedDate).format("DD-MM-YYYY");
                holidays.push(h);
                holidaysMap[h] = holiday.IsLeave;
                model.data.holiday.push(new Date(moment(holiday.LoggedDate).format("MM/DD/YYYY")));
                model.data.holidaystring.push(moment(holiday.LoggedDate).format("YYYY-MM-DD"));
            });

            for (var i = 0; i < days.length; i++) {
                var slot = scheduler.slotByElement(days[i]);
                var dateslot = new Date(slot.startDate);
                var dateSlotStr = moment(dateslot).format("DD-MM-YYYY");
                if (holidays.indexOf(dateSlotStr) > -1) {
                    days[i].style.background = '#e26a75';
                    days[i].style.color = '#fff';
                }
            }

            setTimeout(function () {
                kendo.ui.progress(scheduler.element.find(".k-scheduler-content"), false);
                model.render.changeHeaderViewWithCalculate();
                scheduler.bind("dataBound", model.on.SchedulerShow(scheduler, res.Data.Leaves));
                scheduler.bind("dataBound", function () {
                    model.on.SchedulerShow(scheduler, res.Data.Leaves);
                    model.render.changeHeaderViewWithCalculate();
                });
                $(".k-scheduler-refresh").click(function () {
                    model.on.SchedulerShow(scheduler, res.Data.Leaves);
                });
            }, 500);
        });
    });
}

model.on.SchedulerShow = function (el, dt) {
    var view = el.view();
    var Leave = el.dataSource.view();
    _loadingScheduler = false;

    _.each(Leave, function (o) {
        var event = view.element.find("[data-uid=" + o.uid + "]");
        if (o.Status == 0) {
            event.css({
                "background-color": "#bababa"
            })
        } else if (o.Status == 1) {
            event.css({
                "background-color": "var(--success)"
            })
        } else {
            event.css({
                "background-color": "var(--danger)"
            })
        }
        if (event.length > 0) {
            event.unbind("click");
            event.bind("click", function () {
                model.action.editRequestLeave(o)
            })
        }
    });

    //if (window.innerWidth <= 800 && window.innerHeight <= 600) {
    //    var e = view.element;
    //    var header = $(e).find(".k-scheduler-table")[0]
    //    var days = $(header).find("th");
    //    var daysFormat = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];
    //    for (i = 0; i < days.length; i++) {
    //        days[i].style.padding = "10px 0"
    //        days[i].textContent = daysFormat[i];
    //    }
    //}

    $(el.wrapper).find(".k-link.k-scheduler-refresh").unbind("click");
    $(el.wrapper).find(".k-link.k-scheduler-refresh").bind("click", async function () {
        if (!_loadingScheduler) {
            var info = await model.get.infoall();
            if (info.TotalPending > 0) {
                model.is.requestEnable(false);
            } else {
                model.is.requestEnable(true);
            }
            model.get.ScheduleLeave();
        }
    });
}

model.render.changeHeaderViewWithCalculate = function () {
    $("#scheduler-leave .k-header .k-scheduler-navigation .k-nav-current .k-i-calendar").remove();
    $("#scheduler-leave .k-header").css({
        "display": "block",
    })
    var widthSchedule = $("#scheduler-leave").width();
    var dateNow = $("#scheduler-leave .k-header .k-scheduler-navigation .k-nav-current");
    dateNow.css("position", "absolute");
    var widthDateNow = dateNow.width();
    var currentLeftForDateNow = (widthSchedule / 2) - (widthDateNow / 2);
    dateNow.css("left", currentLeftForDateNow);

    var currentLeftForNavPrev = currentLeftForDateNow - 45;

    var buttonNavPrev = $("#scheduler-leave .k-header .k-scheduler-navigation .k-nav-prev");
    //buttonNavPrev.addClass('d-none-768');
    buttonNavPrev.css({
        "position": "absolute",
        "left": currentLeftForNavPrev,
        "border": "none"
    });

    var currentLeftForNavNext = currentLeftForDateNow + widthDateNow - 7 + 15;

    var buttonNavNext = $("#scheduler-leave .k-header .k-scheduler-navigation .k-nav-next");
    //buttonNavNext.addClass('d-none-768');
    buttonNavNext.css({
        "position": "absolute",
        "left": currentLeftForNavNext,
        "border": "none"
    });
};

// get
model.get.info = async function () {
    let response = await ajax("/ESS/Leave/GetInfo", "GET");

    if (response.StatusCode == 200 && response.Data && response.Data.length > 0) {
        var r = _.filter(response.Data, function (d) { return d.IsClosed == false && d.Remainder > 0 });
        r = _.sortBy(r, "Year");
        model.data.allreminder(r);
        return response.Data;
    }
    console.error(response.StatusCode, ":", response.Message)
    return Object.assign({}, model.proto.Employee);
};

model.get.infoall = async function () {
    // local data
    //var response = { "StatusCode": 200, "Message": null, "Data": { "Maintenances": [{ "Available": true, "AvailabilitySchedule": { "TrueMonthly": 12.0, "Month": 11.991786447638603, "Days": 365.0, "Hours": 8760.0, "Seconds": 31536000.0, "Start": "2020-01-01T19:00:00+07:00", "Finish": "2020-12-31T19:00:00+07:00" }, "CFexpiredDate": "2021-05-31T12:00:00", "EmployeeID": "8012160022", "IsClosed": false, "CF": 0, "Description": "Cuti Tahunan 2020", "Remainder": 14, "Rights": 14, "Year": 2020 }, { "Available": false, "AvailabilitySchedule": { "TrueMonthly": 12.0, "Month": 11.958932238193018, "Days": 364.0, "Hours": 8736.0, "Seconds": 31449600.0, "Start": "2019-01-01T19:00:00+07:00", "Finish": "2019-12-31T19:00:00+07:00" }, "CFexpiredDate": "2020-05-31T12:00:00", "EmployeeID": "8012160022", "IsClosed": false, "CF": 0, "Description": "Cuti Tahunan 2019", "Remainder": 2, "Rights": 14, "Year": 2019 }], "TotalRemainder": 16, "TotalPending": 0 }, "Total": 0 }

    let response = await ajax("/ESS/Leave/GetInfoAll", "GET");
    if (response.StatusCode == 200 && response.Data) {
        var r = _.filter(response.Data.Maintenances, function (d) { return d.IsClosed == false });
        r = _.sortBy(r, "Year");
        model.data.allreminder(r);
        model.data.pending(response.Data.TotalPending);
        return response.Data;
    }
    return Object.assign({}, model.proto.Employee);
}

var tempPending = "", tempRemainder = "";
// action
model.action.modalLeave = function (data) {
    if (!data) {
        model.data.leave(model.newLeave());
    } else {
        model.data.leave(ko.mapping.fromJS(ko.mapping.toJS(data)));
    }

    // Render HTML File
    let leave = ko.toJS(model.data.leave());
    if (!leaveReadonlyFile)
        leaveReadonlyFile = kendo.template($("#fileTemplateReadonly").html());
    if (leave.Filename)
        model.data.leave().HTMLFile(leaveReadonlyFile(leave));
    else
        model.data.leave().HTMLFile("");

    model.action.enableCalendar(true);
    var remainderLeave = model.data.info().Remainder();
    var pendingLeave = tempPending;
    //model.data.remainder(Number(tempRemainder));
    model.data.pendingrequest(model.data.pending());
    $("#leaveModal").modal('show');
    model.is.indicatorVisible(false);
};

model.action.detailReminder = function () {
    $("#leaveReminder").modal('show');
}

model.action.saveRequestLeave = async function () {
    var dialogTitle = "Leave";
    var result = await swalConfirm(dialogTitle, 'Are you sure to request leave?');
    if (result.value) {
        try {
            isLoading(true);
            var data = ko.mapping.toJS(model.data.leave());
            var $modal = $("#leaveModal");                        

            if (model.data.remainder() < 0) {
                isLoading(false)
                return swalAlert(dialogTitle, "You have exceeded your limit request");
            }

            data.Schedule.Start = model.data.dateleavestart;
            data.Schedule.Finish = model.data.dateleaveend;

            var subtitute = $("#subtitution-leave").data("kendoDropDownList");
            var type = $("#type-leave").data("kendoDropDownList");
            data.SubtituteEmployeeName = subtitute.text();
            data.TypeDescription = type.text();
            data.PendingRequest = model.data.pendingrequest();

            if (!data.SubtituteEmployeeID) {
                isLoading(false)
                return swalAlert(dialogTitle, "You should choose your subtitute employee");
            }

            if (!data.Type) {
                isLoading(false)
                return swalAlert(dialogTitle, "Leave type could not be empty");
            }

            if (!data.Description) {
                isLoading(false)
                return swalAlert(dialogTitle, "Leave reason could not be empty");
            }

            if (model.data.dateleave().length < 1) {
                isLoading(false)
                return swalAlert(dialogTitle, "Leave date could not be empty");
            }

            if (model.data.dateleavestart() == null || model.data.dateleaveend() == null) {
                isLoading(false)
                return swalAlert(dialogTitle, "Leave date could not be empty");
            }

            if (model.data.remainder() < model.data.dateleave().length) {
                isLoading(false)
                return swalAlert(dialogTitle, "Leave could not be more than remainder");
            }

            try {
                var formData = new FormData();
                formData.append("JsonData", ko.mapping.toJSON(data));
                var fileUpload = $("#additionalDocument").getKendoUpload();
                if (fileUpload) {
                    let file = fileUpload.getFiles()[0];
                    if (file) {
                        formData.append("FileUpload", file.rawFile);
                    }
                }

                $modal.modal('hide');                
                ajaxPostUpload("/ESS/leave/Save", formData, function (data) {
                    isLoading(false);
                    if (data.StatusCode == 200) {
                        swalSuccess(dialogTitle, data.Message);
                        model.is.requestEnable(false);
                        model.get.ScheduleLeave();
                        model.get.infoall();
                    } else {
                        $modal.modal('show');
                        swalError(dialogTitle, data.Message);
                    }
                }, function (data) {
                    $modal.modal('show');
                    swalError(dialogTitle, data.Message);
                });

            } catch (e) {
                isLoading(false);
            }
        } catch (e) {
            isLoading(false);
            console.error(e);
        }

    }

}

/*
model.action.saveRequestLeave = function () {
    var dialogTitle = "Leave";
    var data = ko.mapping.toJS(model.data.leave());

    // Pre-Process 
    if (model.data.remainder() < 0) {
        return swalAlert(dialogTitle, "You have exceeded your limit request");
    }

    //var date = $("#dateCalendarLeave").data("kendoCalendar").selectDates();
    //var start = date[0];
    //var end = date[date.length - 1];

    //if (start > end) {
    //    data.Schedule.Start = end;
    //    data.Schedule.Finish = start;
    //} else {
    //    data.Schedule.Start = start;
    //    data.Schedule.Finish = end;
    //}    
    data.Schedule.Start = model.data.dateleavestart;
    data.Schedule.Finish = model.data.dateleaveend;

    var subtitute = $("#subtitution-leave").data("kendoDropDownList");
    var type = $("#type-leave").data("kendoDropDownList");
    data.SubtituteEmployeeName = subtitute.text();
    data.TypeDescription = type.text();
    data.PendingRequest = model.data.pendingrequest();

    // Validation
    if (!data.SubtituteEmployeeID) {
        return swalAlert(dialogTitle, "You should choose your subtitute employee");
    }

    if (!data.Type) {
        return swalAlert(dialogTitle, "Leave type could not be empty");
    }

    if (!data.Description) {
        return swalAlert(dialogTitle, "Leave reason could not be empty");
    }

    if (model.data.dateleave().length < 1) {
        return swalAlert(dialogTitle, "Leave date could not be empty");
    }

    if (model.data.dateleavestart() == null || model.data.dateleaveend() == null) {
        return swalAlert(dialogTitle, "Leave date could not be empty");
    }

    if (model.data.remainder() < model.data.dateleave().length) {
        return swalAlert(dialogTitle, "Leave could not be more than remainder");
    }

    try {        
        // Encapsulate Upload Data
        var formData = new FormData();
        formData.append("JsonData", ko.mapping.toJSON(data));
        var fileUpload = $("#additionalDocument").getKendoUpload();
        if (fileUpload) {
            let file = fileUpload.getFiles()[0];
            if (file) {
                formData.append("FileUpload", file.rawFile);
            }
        }
        var $modal = $("#leaveModal");

        $modal.modal('hide');
        isLoading(true);        

        ajaxPostUpload("/ESS/Leave/Save", formData, function (data) {
            isLoading(false);           
            if (data.StatusCode == 200) {
                swalSuccess(dialogTitle, data.Message);
                model.is.requestEnable(false);
                model.get.ScheduleLeave();
                model.get.infoall();
            } else {
                $modal.modal('show');
                swalError(dialogTitle, data.Message);
            }
        }, function (data) {
            $modal.modal('show');
            swalError(dialogTitle, data.Message);
        });
    } catch (e) {
        isLoading(false);
    }
}
*/

model.data.getDates = (start, end) => {
    start = moment(new Date(start));
    end = moment(new Date(end));
    var dates = [];
    var curr = moment(start).startOf('day');
    var last = moment(end).startOf('day');

    while (curr.add(1, 'days').diff(last) < 0) {
        dates.push(curr.clone().toDate());
    }
    return dates;
};

model.data.calendarMinDate = ko.observable(DEFAULT_DATE);
let leaveReadonlyFile;
model.action.editRequestLeave = function (data) {
    console.log("show:", data);
    /*
    data = Object.assign(data, model.data.leaves.find((d) => {
        return d.Id == data.ID;
    }));
    */
    model.is.readonly(true);
    model.is.indicatorVisible(false);

    if (data.Status == 0) {
        model.is.cancelable(true);
        model.is.processleave(true);
    } else {
        model.is.cancelable(false);
        model.is.processleave(false);
    }

    model.data.selectedDates([]);
    newDate = model.data.getDates(data.start, data.end);
    newDate.unshift(data.start);
    newDate.push(data.end);
    model.data.selectedDates(newDate);

    var leave = model.newLeave(data);

    _.map(newDate, function (x) {
        model.data.dateleave.push(new Date(moment(x).format("MM/DD/YYYY")));
    });
    if (data.Status > 0) {
        //model.is.readonly(true);
        model.data.calendarMinDate(DEFAULT_DATE);
    }
    setTimeout(function () {
        model.data.selectedDates().map(function (dd) {
            var day = $('[data-value="' + dd.getFullYear() + "/" + dd.getMonth() + "/" + dd.getDate() + '"]');
            if (day.length > 0) {
                var td = day.closest("td");
                if (td.length > 0) {
                    td.addClass("k-state-selected");
                }
            }
        });
    });

    model.action.modalLeave(leave);
    model.get.EditCalendarLeave(data.AXID);
}

model.action.addRequestLeave = function () {
    model.is.indicatorVisible(true);
    model.data.calendarMinDate(new Date());
    model.is.readonly(false);
    model.is.cancelable(false);
    model.data.selectedDates([]);
    model.action.modalLeave();
};

model.action.deleteRequestLeave = async function () {
    var dialogTitle = "Leave"
    newData = ko.mapping.toJS(model.data.leave())
    var result = await swalConfirm(dialogTitle, `Are you sure deleting "${newData.Description}" ?`);
    if (result.value) {
        var Id = (newData.Id) ? newData.Id : "";

        try {
            isLoading(true)
            ajaxPost("/ESS/Leave/Remove/" + Id, {}, function (data) {
                isLoading(false);
                $("#leaveModal").modal('hide');
                if (data.StatusCode == 200) {
                    $("#scheduler-leave").data("kendoScheduler").dataSource.read()
                    swalSuccess(dialogTitle, data.Message);
                } else {
                    swalFatal(dialogTitle, data.Message);
                }
            }, function (err) {
                swalError(dialogTitle, err.Message);
            });
        } catch (e) {
            isLoading(false);
        } finally {
            isLoading(false);
        }

    }
}

model.action.refreshScheduler = function () {
    $("#scheduler-leave").data("kendoScheduler").dataSource.read();
}

// render
let _isLoadingHoliday = false;
model.on.renderCalenderLeave = async function () {
    model.render.calendarLeave();
};

//var dates = [];
//holidays.forEach(function (x) {
//    var date = new Date(moment(x.LoggedDate).format("MM/DD/YYYY"));
//    dates.push(new Date(moment(x.LoggedDate).format("MM/DD/YYYY")));
//    if (model.data.holiday.indexOf(date) == -1) {
//        model.data.holiday.push(date);
//    }
//});
//return dates;

var _calendarWrapper = null;
model.on.calendarNavigate = async function (e) {
    var view = this.view();

    if (model.is.readonly()) {
        var today = this.wrapper.find(".k-today");
        if (today.length > 0) {
            today.removeClass("k-today");
        }
    }

    kendo.ui.progress(this.wrapper, true);
    if (view.name == "month" && !_isLoadingHoliday) {
        _isLoadingHoliday = true;

        $calendar = this;
        while (true) {
            if ($calendar.wrapper.find(".k-calendar-view>div").length == 0) {
                break;
            }
            await delay(300);
        }

        var days = $calendar.wrapper.find(".k-calendar-view>.k-month tbody .k-link");

        if (days.length > 0) {
            var start = (days[0]) ? days[0].dataset.value : "";
            var finish = (days[days.length - 1]) ? days[days.length - 1].dataset.value : "";


            if (start && finish) {
                var startToken = start.split("/");
                var finishToken = finish.split("/");
                var startDate = new Date(startToken[0], startToken[1], startToken[2]);
                var finishDate = new Date(finishToken[0], finishToken[1], finishToken[2]);

                var holidays = model.data.holiday().sort((a, b) => {
                    return a - b;
                }) || [];

                var disabledDates = [];
                if (holidays.length > 0 && startDate >= holidays[0] && finishDate <= holidays[holidays.length - 1]) {
                    disabledDates = holidays.filter((x) => {
                        return startDate <= x && finishDate >= x;
                    });
                } else {
                    var employeeID = model.data.employeeID();
                    disabledDates = await model.get.holiday(startDate, finishDate, employeeID);
                }

                disabledDates.forEach(function (dd) {
                    var day = $(`[data-value='${dd.getFullYear()}/${dd.getMonth()}/${dd.getDate()}']`);
                    if (day.length > 0) {
                        var td = day.closest("td");
                        if (td.length > 0) {
                            td.removeClass("k-state-selected");
                            td.addClass("k-state-disabled");
                        }
                    }
                });

            }
        }

        //$calendar.wrapper.find(".loading-screen").remove();
        _isLoadingHoliday = false;
    }
    kendo.ui.progress(this.wrapper, false);
    this._previous = this._current;
};

model.action.enableCalendar = function (flag) {
    var $el = $("#viewCalendarLeave .k-calendar-view");
    if (flag) {
        $el.css("pointer-events", "auto");
        return;
    }
    $el.css("pointer-events", "none");
};

// init 
model.init.leave = async function () {
    let self = model;

    setTimeout(function () {
        self.render.history();
    });

    setTimeout(function () {
        if (model.app.hasSubordinate()) {
            self.render.subordinate();
        }
    });
    model.is.requestEnable(false);

    setTimeout(async function () {
        //isLoading(true);
        await Promise.all([
            new Promise(async (resolve) => {
                var info = await self.get.infoall();
                var totalRemainder = info.TotalRemainder;
                if (info.TotalPending > 0) {
                    model.is.requestEnable(false);
                } else {
                    model.is.requestEnable(true);
                }
                model.data.info(ko.mapping.fromJS({ Remainder: totalRemainder }));
                tempRemainder = model.data.info().Remainder();

                resolve(true);
            }),
            new Promise(async (resolve) => {
                self.render.SchedulerCalendar();
                resolve(true);
            }),
            new Promise(async (resolve) => {
                self.render.subtitution();
                resolve(true);
            }),
            new Promise(async (resolve) => {
                self.render.typeleave();
                resolve(true);
            }),
        ]);
    });
};

model.get.typeleave = async function () {
    // local data
    //var response = { "StatusCode": 200, "Message": null, "Data": [{ "CategoryId": "ANU", "Description": "Cuti Tahunan 2019", "EffectiveDateFrom": "2019-01-01T12:00:00", "EffectiveDateTo": "2019-12-31T12:00:00", "IsClosed": false, "TypeId": 68, "MaxDayLeave": 14, "Remainder": 2, "ConsumeDay": 0 }, { "CategoryId": "SPE", "Description": "Cuti (Keluarga terdekat Meninggal Dunia)", "EffectiveDateFrom": "2019-01-01T12:00:00", "EffectiveDateTo": "2025-12-31T12:00:00", "IsClosed": false, "TypeId": 72, "MaxDayLeave": 2, "Remainder": 2, "ConsumeDay": 0 }, { "CategoryId": "ANU", "Description": "Cuti Tahunan 2020", "EffectiveDateFrom": "2020-01-01T12:00:00", "EffectiveDateTo": "2020-12-31T12:00:00", "IsClosed": false, "TypeId": 75, "MaxDayLeave": 14, "Remainder": 14, "ConsumeDay": 0 }, { "CategoryId": "SPE", "Description": "Cuti (Istri Melahirkan/Keguguran)", "EffectiveDateFrom": "2019-01-01T12:00:00", "EffectiveDateTo": "2025-12-31T12:00:00", "IsClosed": false, "TypeId": 71, "MaxDayLeave": 2, "Remainder": 1, "ConsumeDay": 0 }, { "CategoryId": "SPE", "Description": "Cuti Alasan Penting", "EffectiveDateFrom": "2019-01-01T12:00:00", "EffectiveDateTo": "2025-01-31T12:00:00", "IsClosed": false, "TypeId": 73, "MaxDayLeave": 3, "Remainder": 3, "ConsumeDay": 0 }], "Total": 0 };
    let response = await ajax("/ESS/Leave/GetType", "GetType");
    if (response.StatusCode == 200 && response.Data) {
        console.log("Type:", response.Data);
    }
}

model.data.kendoDateToDate = function (kendoDate) {
    var token = (kendoDate || "").split("/");
    if (token.length >= 3) {
        return new Date(token[0], token[1], token[2]);
    }
    return null;
};

model.get.dateleave = function (startDate, stopDate) {
    var dateArray = [];
    var currentDate = moment(startDate);
    var stopDate = moment(stopDate);

    while (currentDate <= stopDate) {
        dateArray.push(moment(currentDate).format('YYYY-MM-DD'));
        currentDate = moment(currentDate).add(1, 'days');
    }
    model.list.datepickstring(dateArray);
    return dateArray;
}

var cacheMonthHoliday = [];
model.render.calendarLeave = function () {
    let self = model;
    var $el = $("#dateCalendarLeave");
    var $calendar = $el.getKendoDateRangePicker();
    var leaveType = self.data.leave().Type();
    var _renderHoliday = function (monthView) {

        // Get first day of displayed months
        let months = [];
        monthView.find('.k-month').each(function () {
            let $startMonth = $(this).find("td>a.k-link:first");

            if ($startMonth.length > 0) {
                months.push(self.data.kendoDateToDate($startMonth.data('value')));
            }
        });

        months = months.sort((a, b) => {
            return a - b;
        });

        let first = firstDayOfMonth(months[0]);
        let last = lastDayOfMonth(months[1]);
        return [first, last];
    }

    if ($calendar) {
        $calendar.destroy();
    }

    $calendar = $el.kendoDateRangePicker({
        format: "dd MMM yyyy",
        min: model.data.minDateFromLeave(),
        open: function () {
            let _calendar = this;

            var dateHoliday = [];
            var a, b, temp = [];
            a = moment(_calendar._firstViewValue).format("MM/YYYY");
            b = moment(_calendar._firstViewValue).add(1, "months").endOf("month").format("MM/YYYY");
            console.log("cache:", a, b);
            temp.push(a, b);
            var len = temp.filter(function (item) {
                return !cacheMonthHoliday.includes(item);
            });

            if (len.length > 0) {
                kendo.ui.progress(_calendar.dateView.calendar.element, true);
                cacheMonthHoliday.push(a, b);
                var dt1 = moment(_calendar._firstViewValue).format("MM/DD/YYYY");
                var dt2 = moment(_calendar._firstViewValue).add(2, "months").endOf("month").calendar();
                console.log("param date:", dt1, dt2);
                (async () => {
                    dateHoliday = await model.get.holiday(dt1, dt2);
                    model.data.holiday(dateHoliday);
                    _calendar.setOptions({
                        disableDates: model.data.holiday(),
                    });

                    kendo.ui.progress(_calendar.dateView.calendar.element, false);
                })();

            }

            setTimeout(function () {
                model.data.holiday().map(function (dd) {
                    var day = $("[data-value='" + dd.getFullYear() + "/" + dd.getMonth() + "/" + dd.getDate() + "']");
                    if (day.length > 0) {
                        var td = day.closest("td");
                        if (td.length > 0) {
                            td.removeClass("k-state-selected");
                            td.addClass("k-state-disabled");
                        }
                    }
                });
                //kendo.ui.progress(_calendar.dateView.calendar.element, false);
            });

            /*
            setTimeout(async function () {
                // Waiting till the date picker is fully open
                let $monthView;
                while (true) {
                    $popupCalendar = _calendar.dateView.popup.wrapper;
                    $monthView = $popupCalendar.find('.k-calendar-monthview');
                    if (!$monthView.prop("aria")) {
                        break;
                    }
                    console.log("sada");
                    await delay(50);
                }

                // Get next and prev button
                let $next = _calendar.dateView.popup.wrapper.find('.k-next-view');
                let $prev = _calendar.dateView.popup.wrapper.find('.k-prev-view');
                //_renderHoliday($monthView);

                // Set next and prev button listener
                if ($next.length > 0 && $prev.length > 0) {
                    $([$next[0], $prev[0]]).off('click');
                    $([$next[0], $prev[0]]).on('click', function (e) {
                        setTimeout(function () {
                            _renderHoliday($monthView);
                        }, 100);
                    });
                    
                }
            });
            */
        },
        close: function (e) {
            setTimeout(function () {
                if ($calendar.range()) {
                    /*
                    if ($calendar.range().start == null) {
                        model.data.dateleaveend(null);
                        swalAlert("Leave", "Date could not be empty");
                        return
                    } else if ($calendar.range().start != null && $calendar.range().end == null) {                        
                        $calendar.range({ start: $calendar.range().start, end: $calendar.range().start});                     
                    }
                    */
                    let range = $calendar.range();
                    
                    if (range.start == null && range.end == null) {
                        return false;
                    }
                    console.log($calendar);
                    console.log("typeLeave:", new Date(dataSelectType.EffectiveDateFrom), new Date(dataSelectType.EffectiveDateTo));
                    console.log("range: ", new Date(range.start), ", ", new Date(range.start));

                    range.start = (range.start) ? range.start : range.end;
                    $("[data-role='dateinput']")[0].value = moment(new Date(range.start)).format("DD MMM YYYY");
                    range.end = (range.end) ? range.end : range.start;
                    $("[data-role='dateinput']")[1].value = moment(new Date(range.end)).format("DD MMM YYYY");

                    if (new Date(dataSelectType.EffectiveDateFrom) <= new Date(range.start) && new Date(dataSelectType.EffectiveDateTo) >= new Date(range.start)) {
                        if (new Date(dataSelectType.EffectiveDateFrom) <= new Date(range.end) && new Date(dataSelectType.EffectiveDateTo) >= new Date(range.end)) {
                            model.get.dateleave(range.start, range.end);
                            var dateleave = _.difference(model.list.datepickstring(), model.data.holidaystring());

                            if (model.data.remainder() < dateleave.length) {
                                swalAlert("Leave", "Leave could not be more than remainder");
                            }

                            model.data.dateleave(dateleave);
                            model.data.dateleavestart(new Date(range.start));
                            model.data.dateleaveend(new Date(range.end))
                            model.data.pendingrequest(dateleave.length);
                        }
                        else {
                            $calendar.setOptions({
                                startField: "startField",
                                endField: "endField"
                            });
                            swalAlert("Leave Date Expired", "Invalid date for " + dataSelectType.Description + ". Valid leave date should between " + moment(dataSelectType.EffectiveDateFrom).format("DD MMM YYYY") + " until " + moment(dataSelectType.EffectiveDateTo).format("DD MMM YYYY"));
                        }

                    } else {
                        $calendar.setOptions({
                            startField: "startField",
                            endField: "endField"
                        });
                        swalAlert("Leave Date Expired", "Invalid date for " + dataSelectType.Description + ". Valid leave date should between " + moment(dataSelectType.EffectiveDateFrom).format("DD MMM YYYY") + " until " + moment(dataSelectType.EffectiveDateTo).format("DD MMM YYYY"));
                    }
                    

                }
            });
        },
        //disableDates: model.data.holiday(),
    }).getKendoDateRangePicker();
    /*
    $calendar.setOptions({
        disableDates: model.data.tempHoliday(),
    });
    */
    if ($calendar) {
        $calendar.enable(!!leaveType);
        var dateView = $calendar.dateView;
        dateView._calendar();
        var calendar = dateView.calendar;
        calendar.bind("navigate", function () {
            var loader = this;

            var a, b, temp = [];
            a = moment(this._firstViewValue).format("MM/YYYY");
            b = moment(this._firstViewValue).add(1, "months").endOf("month").format("MM/YYYY");
            temp.push(a, b);
            console.log("cache:", a, b);

            var len = temp.filter(function (item) {
                return !cacheMonthHoliday.includes(item);
            });
            var dateHoliday = [];
            //console.log($calendar);
            if (len.length > 0) {
                kendo.ui.progress(loader.element, true);
                cacheMonthHoliday.push(b);
                var fistdate = moment(this._firstViewValue).format("MM/DD/YYYY");
                var lastdate = moment(this._firstViewValue).add(1, "months").endOf("month").calendar();
                console.log("param date:", fistdate, lastdate);
                (async () => {
                    dateHoliday = await model.get.holiday(fistdate, lastdate, "");
                    dateHoliday.map(function (dd) {
                        model.data.holiday.push(dd);
                        model.data.holidaystring.push(moment(dd).format("YYYY-MM-DD"));
                    });

                    model.data.holiday().map(function (dd) {
                        var day = $("[data-value='" + dd.getFullYear() + "/" + dd.getMonth() + "/" + dd.getDate() + "']");
                        if (day.length > 0) {
                            var td = day.closest("td");
                            if (td.length > 0) {
                                td.removeClass("k-state-selected");
                                td.addClass("k-state-disabled");
                            }
                        }
                    });
                    kendo.ui.progress(loader.element, false);
                })();
                //setTimeout(function () {
                //kendo.ui.progress(loader.element, false);
                //}, 500);
            } else {
                model.data.holiday().map(function (dd) {
                    var day = $("[data-value='" + dd.getFullYear() + "/" + dd.getMonth() + "/" + dd.getDate() + "']");
                    if (day.length > 0) {
                        var td = day.closest("td");
                        if (td.length > 0) {
                            td.removeClass("k-state-selected");
                            td.addClass("k-state-disabled");
                        }
                    }
                });
            }

            // action prev-next
            /*
            this._nextArrow.unbind("click");
            this._nextArrow.bind("click", function () {
                console.log("next");
            });
            this._prevArrow.unbind("click");
            this._prevArrow.bind("click", function () {
                console.log("prev");
            });*/

            /*
            dateView.popup.wrapper.find('.k-next-view').unbind("click");
            dateView.popup.wrapper.find('.k-next-view').bind("click", function () {
                console.log("next")
            });*/
        });
    }

    //return false;
    // ---------------------------------------------- //
    var el = $("#viewCalendarLeave");
    var calendar = el.data("kendoCalendar");
    if (calendar) {
        calendar.destroy();
    }
    var pending = model.data.pending();
    var options = {
        selectable: "multiple",
        min: model.data.calendarMinDate(),
        change: function (e) {
            var calendar = $(el).getKendoCalendar();
            var selectedDates = calendar.selectDates();
            var remainderLeave = model.data.anchorRemainder();
            tempRemainder = remainderLeave;

            if (calendar.options.selectable == "single") {
                var arrDate = [];
                var select = new Date(calendar.value());
                var holiday = _.map(model.data.holiday(), function (x) { return moment(x).format("MM/DD/YYYY") });
                var maxday = model.data.maxdayofleave();

                var i = 0;
                if (consumeDay == 0) {
                    while (i < maxday) {
                        arrDate.push(select.addDays(i));
                        i++;
                    }
                } else {
                    while (i < maxday) {
                        if (!_.includes(holiday, moment(select.addDays(i)).format("MM/DD/YYYY"))) {
                            arrDate.push(select.addDays(i));
                        } else {
                            maxday++;
                        }
                        i++;
                    }
                }
                //console.log("select:", arrDate);
                model.data.leave().PendingRequest(arrDate.length);
                calendar.selectDates(arrDate);
            } else {
                var sisa = remainderLeave - selectedDates.length;
                model.data.remainder(sisa);
                model.data.pendingrequest(pending + selectedDates.length);
                console.log("sisa:", sisa);

                if (sisa >= 0) {
                    model.data.leave().PendingRequest(selectedDates.length);
                    model.data.pendingrequest(pending + selectedDates.length);
                } else {
                    e.preventDefault();
                    swalError("Leave", "Unable to select date, you have exceeded your limit request");
                    //$("#dateCalendarLeave").data("kendoCalendar").value("");
                }

            }
            //calendar.selectDates([new Date("01/06/2019")])
        },
        disableDates: model.data.holiday(),
    };

    var selectedDates = model.data.selectedDates();
    if (selectedDates.length == 0) {
        options.navigate = model.on.calendarNavigate;
    }

    calendar = el.kendoCalendar(options).getKendoCalendar();

    // Navigate to selected dates
    if (!options.navigate) {
        if (calendar) {
            calendar.bind("navigate", model.on.calendarNavigate);
        }

        if (selectedDates.length > 0) {
            var firstDayOfMonth = _.clone(selectedDates[0]).setDate(1);
            calendar.navigate(new Date(firstDayOfMonth), "month");
        }
    }

    setTimeout(function () {
        if (model.is.readonly()) {
            model.action.enableCalendar(false);
        }
        if (calendar) {
            calendar.selectDates([]);
            if (model.data.selectedDates().length > 0) {
                calendar.selectDates(selectedDates);
            }
        }
    });

    return calendar;
};

model.render.typeleave = async function () {
    ajaxPost("/ESS/Leave/GetType", {}, function (res) {
        var data = res.Data || [];
        var activeTypeData = data.filter(function (o) {
            if (o.CategoryId == "ANU") {
                return !o.IsClosed && o.Remainder > 0;
            } else {
                return !o.IsClosed;
            }
        });
        model.data.typeleave(activeTypeData);
    }, function (err) {
        swalFatal("Fatal Error", err.Message);
    });
    /*
    // local data
    //var res = { "StatusCode": 200, "Message": null, "Data": [{ "CategoryId": "ANU", "Description": "Cuti Tahunan 2019", "EffectiveDateFrom": "2019-01-01T12:00:00", "EffectiveDateTo": "2019-12-31T12:00:00", "IsClosed": false, "TypeId": 68, "MaxDayLeave": 14, "Remainder": 2, "ConsumeDay": 0 }, { "CategoryId": "SPE", "Description": "Cuti (Keluarga terdekat Meninggal Dunia)", "EffectiveDateFrom": "2019-01-01T12:00:00", "EffectiveDateTo": "2025-12-31T12:00:00", "IsClosed": false, "TypeId": 72, "MaxDayLeave": 2, "Remainder": 2, "ConsumeDay": 0 }, { "CategoryId": "ANU", "Description": "Cuti Tahunan 2020", "EffectiveDateFrom": "2020-01-01T12:00:00", "EffectiveDateTo": "2020-12-31T12:00:00", "IsClosed": false, "TypeId": 75, "MaxDayLeave": 14, "Remainder": 14, "ConsumeDay": 0 }, { "CategoryId": "SPE", "Description": "Work From Home", "EffectiveDateFrom": "2020-03-01T12:00:00", "EffectiveDateTo": "2020-05-31T12:00:00", "IsClosed": false, "TypeId": 77, "MaxDayLeave": 31, "Remainder": 0, "ConsumeDay": 0 }, { "CategoryId": "SPE", "Description": "Cuti (Istri Melahirkan/Keguguran)", "EffectiveDateFrom": "2019-01-01T12:00:00", "EffectiveDateTo": "2025-12-31T12:00:00", "IsClosed": false, "TypeId": 71, "MaxDayLeave": 2, "Remainder": 1, "ConsumeDay": 0 }, { "CategoryId": "SPE", "Description": "Cuti Alasan Penting", "EffectiveDateFrom": "2019-01-01T12:00:00", "EffectiveDateTo": "2025-01-31T12:00:00", "IsClosed": false, "TypeId": 73, "MaxDayLeave": 3, "Remainder": 3, "ConsumeDay": 0 }], "Total": 0 };
    //var res = { "StatusCode": 200, "Message": null, "Data": [{ "CategoryId": "ANU", "Description": "Cuti Tahunan 2019", "EffectiveDateFrom": "2019-01-01T12:00:00", "EffectiveDateTo": "2019-12-31T12:00:00", "IsClosed": false, "TypeId": 68, "MaxDayLeave": 14, "Remainder": 2, "ConsumeDay": 0 }, { "CategoryId": "SPE", "Description": "Cuti (Keluarga terdekat Meninggal Dunia)", "EffectiveDateFrom": "2019-01-01T12:00:00", "EffectiveDateTo": "2025-12-31T12:00:00", "IsClosed": false, "TypeId": 72, "MaxDayLeave": 2, "Remainder": 2, "ConsumeDay": 0 }, { "CategoryId": "ANU", "Description": "Cuti Tahunan 2020", "EffectiveDateFrom": "2020-01-01T12:00:00", "EffectiveDateTo": "2020-12-31T12:00:00", "IsClosed": false, "TypeId": 75, "MaxDayLeave": 14, "Remainder": 14, "ConsumeDay": 0 }, { "CategoryId": "SPE", "Description": "Work From Home", "EffectiveDateFrom": "2020-06-05T12:00:00", "EffectiveDateTo": "2020-06-10T12:00:00", "IsClosed": false, "TypeId": 77, "MaxDayLeave": 31, "Remainder": 0, "ConsumeDay": 0 }, { "CategoryId": "SPE", "Description": "Cuti (Istri Melahirkan/Keguguran)", "EffectiveDateFrom": "2019-01-01T12:00:00", "EffectiveDateTo": "2025-12-31T12:00:00", "IsClosed": false, "TypeId": 71, "MaxDayLeave": 2, "Remainder": 1, "ConsumeDay": 0 }, { "CategoryId": "SPE", "Description": "Cuti Alasan Penting", "EffectiveDateFrom": "2019-01-01T12:00:00", "EffectiveDateTo": "2025-01-31T12:00:00", "IsClosed": false, "TypeId": 73, "MaxDayLeave": 3, "Remainder": 3, "ConsumeDay": 0 }], "Total": 0 }
    var res = { "StatusCode": 200, "Message": null, "Data": [{ "CategoryId": "ANU", "Description": "Cuti Tahunan 2019", "EffectiveDateFrom": "2019-01-01T12:00:00", "EffectiveDateTo": "2019-12-31T12:00:00", "IsClosed": false, "TypeId": 68, "MaxDayLeave": 14, "Remainder": 6, "ConsumeDay": 0 }, { "CategoryId": "SPE", "Description": "Cuti (Keluarga terdekat Meninggal Dunia)", "EffectiveDateFrom": "2019-01-01T12:00:00", "EffectiveDateTo": "2025-12-31T12:00:00", "IsClosed": false, "TypeId": 72, "MaxDayLeave": 2, "Remainder": 2, "ConsumeDay": 0 }, { "CategoryId": "ANU", "Description": "Cuti Tahunan 2020", "EffectiveDateFrom": "2020-01-01T12:00:00", "EffectiveDateTo": "2020-12-31T12:00:00", "IsClosed": false, "TypeId": 75, "MaxDayLeave": 14, "Remainder": 14, "ConsumeDay": 0 }, { "CategoryId": "SPE", "Description": "Work From Home", "EffectiveDateFrom": "2020-03-01T12:00:00", "EffectiveDateTo": "2020-05-31T12:00:00", "IsClosed": false, "TypeId": 77, "MaxDayLeave": 31, "Remainder": 31, "ConsumeDay": 0 }, { "CategoryId": "SPE", "Description": "Cuti (Istri Melahirkan/Keguguran)", "EffectiveDateFrom": "2019-01-01T12:00:00", "EffectiveDateTo": "2025-12-31T12:00:00", "IsClosed": false, "TypeId": 71, "MaxDayLeave": 2, "Remainder": 2, "ConsumeDay": 0 }, { "CategoryId": "SPE", "Description": "Cuti Alasan Penting", "EffectiveDateFrom": "2019-01-01T12:00:00", "EffectiveDateTo": "2025-01-31T12:00:00", "IsClosed": false, "TypeId": 73, "MaxDayLeave": 3, "Remainder": 3, "ConsumeDay": 0 }], "Total": 0 };
    var data = res.Data || [];
    var activeTypeData = data.filter(function (o) {
        if (o.CategoryId == "ANU") {
            return !o.IsClosed && o.Remainder > 0;
        } else {
            return !o.IsClosed;
        }
    });
    
    model.data.typeleave(activeTypeData);
    */
}

model.render.history = async function () {
    //var localdata = { "StatusCode": 200, "Message": null, "Data": [{ "Status": 1, "Description": "Cuti Tahunan 2018", "EmplId": "8012160022", "EmplName": "ANGGA LESMANA", "EndDate": "2018-10-29T12:00:00", "RecId": 5637230089, "StartDate": "2018-10-29T12:00:00", "Schedule": { "TrueMonthly": 0.032258064516129031, "Month": 0.0, "Days": 0.0, "Hours": 0.0, "Seconds": 0.0, "Start": "2018-10-29T19:00:00+07:00", "Finish": "2018-10-29T19:00:00+07:00" } }, { "Status": 1, "Description": "Cuti Tahunan 2019", "EmplId": "8012160022", "EmplName": "ANGGA LESMANA", "EndDate": "2019-11-05T12:00:00", "RecId": 5637238349, "StartDate": "2019-11-04T12:00:00", "Schedule": { "TrueMonthly": 0.066666666666666666, "Month": 0.032854209445585217, "Days": 1.0, "Hours": 24.0, "Seconds": 86400.0, "Start": "2019-11-04T19:00:00+07:00", "Finish": "2019-11-05T19:00:00+07:00" } }, { "Status": 1, "Description": "Cuti Tahunan 2018", "EmplId": "8012160022", "EmplName": "ANGGA LESMANA", "EndDate": "2018-12-06T12:00:00", "RecId": 5637230946, "StartDate": "2018-12-06T12:00:00", "Schedule": { "TrueMonthly": 0.032258064516129031, "Month": 0.0, "Days": 0.0, "Hours": 0.0, "Seconds": 0.0, "Start": "2018-12-06T19:00:00+07:00", "Finish": "2018-12-06T19:00:00+07:00" } }, { "Status": 1, "Description": "Cuti Tahunan 2018", "EmplId": "8012160022", "EmplName": "ANGGA LESMANA", "EndDate": "2018-12-12T12:00:00", "RecId": 5637230968, "StartDate": "2018-12-10T12:00:00", "Schedule": { "TrueMonthly": 0.0967741935483871, "Month": 0.065708418891170434, "Days": 2.0, "Hours": 48.0, "Seconds": 172800.0, "Start": "2018-12-10T19:00:00+07:00", "Finish": "2018-12-12T19:00:00+07:00" } }, { "Status": 1, "Description": "Cuti Tahunan 2018", "EmplId": "8012160022", "EmplName": "ANGGA LESMANA", "EndDate": "2019-02-18T12:00:00", "RecId": 5637232497, "StartDate": "2019-02-18T12:00:00", "Schedule": { "TrueMonthly": 0.035714285714285712, "Month": 0.0, "Days": 0.0, "Hours": 0.0, "Seconds": 0.0, "Start": "2019-02-18T19:00:00+07:00", "Finish": "2019-02-18T19:00:00+07:00" } }, { "Status": 1, "Description": "Cuti Tahunan 2018", "EmplId": "8012160022", "EmplName": "ANGGA LESMANA", "EndDate": "2019-02-22T12:00:00", "RecId": 5637232527, "StartDate": "2019-02-22T12:00:00", "Schedule": { "TrueMonthly": 0.035714285714285712, "Month": 0.0, "Days": 0.0, "Hours": 0.0, "Seconds": 0.0, "Start": "2019-02-22T19:00:00+07:00", "Finish": "2019-02-22T19:00:00+07:00" } }, { "Status": 1, "Description": "Cuti Tahunan 2018", "EmplId": "8012160022", "EmplName": "ANGGA LESMANA", "EndDate": "2019-02-25T12:00:00", "RecId": 5637232528, "StartDate": "2019-02-25T12:00:00", "Schedule": { "TrueMonthly": 0.035714285714285712, "Month": 0.0, "Days": 0.0, "Hours": 0.0, "Seconds": 0.0, "Start": "2019-02-25T19:00:00+07:00", "Finish": "2019-02-25T19:00:00+07:00" } }, { "Status": 1, "Description": "Cuti Tahunan 2018", "EmplId": "8012160022", "EmplName": "ANGGA LESMANA", "EndDate": "2019-04-24T12:00:00", "RecId": 5637233921, "StartDate": "2019-04-18T12:00:00", "Schedule": { "TrueMonthly": 0.23333333333333334, "Month": 0.1971252566735113, "Days": 6.0, "Hours": 144.0, "Seconds": 518400.0, "Start": "2019-04-18T19:00:00+07:00", "Finish": "2019-04-24T19:00:00+07:00" } }, { "Status": 1, "Description": "Cuti Tahunan 2019", "EmplId": "8012160022", "EmplName": "ANGGA LESMANA", "EndDate": "2019-06-12T12:00:00", "RecId": 5637234711, "StartDate": "2019-06-10T12:00:00", "Schedule": { "TrueMonthly": 0.1, "Month": 0.065708418891170434, "Days": 2.0, "Hours": 48.0, "Seconds": 172800.0, "Start": "2019-06-10T19:00:00+07:00", "Finish": "2019-06-12T19:00:00+07:00" } }, { "Status": 1, "Description": "Cuti Tahunan 2019", "EmplId": "8012160022", "EmplName": "ANGGA LESMANA", "EndDate": "2019-07-17T12:00:00", "RecId": 5637236341, "StartDate": "2019-07-15T12:00:00", "Schedule": { "TrueMonthly": 0.0967741935483871, "Month": 0.065708418891170434, "Days": 2.0, "Hours": 48.0, "Seconds": 172800.0, "Start": "2019-07-15T19:00:00+07:00", "Finish": "2019-07-17T19:00:00+07:00" } }, { "Status": 1, "Description": "Cuti (Istri Melahirkan/Keguguran)", "EmplId": "8012160022", "EmplName": "ANGGA LESMANA", "EndDate": "2019-07-12T12:00:00", "RecId": 5637236342, "StartDate": "2019-07-12T12:00:00", "Schedule": { "TrueMonthly": 0.032258064516129031, "Month": 0.0, "Days": 0.0, "Hours": 0.0, "Seconds": 0.0, "Start": "2019-07-12T19:00:00+07:00", "Finish": "2019-07-12T19:00:00+07:00" } }, { "Status": 1, "Description": "Cuti Tahunan 2019", "EmplId": "8012160022", "EmplName": "ANGGA LESMANA", "EndDate": "2019-12-27T12:00:00", "RecId": 5637240700, "StartDate": "2019-12-27T12:00:00", "Schedule": { "TrueMonthly": 0.032258064516129031, "Month": 0.0, "Days": 0.0, "Hours": 0.0, "Seconds": 0.0, "Start": "2019-12-27T19:00:00+07:00", "Finish": "2019-12-27T19:00:00+07:00" } }, { "Status": 1, "Description": "Cuti Tahunan 2019", "EmplId": "8012160022", "EmplName": "ANGGA LESMANA", "EndDate": "2020-02-04T12:00:00", "RecId": 5637242942, "StartDate": "2020-02-03T12:00:00", "Schedule": { "TrueMonthly": 0.068965517241379309, "Month": 0.032854209445585217, "Days": 1.0, "Hours": 24.0, "Seconds": 86400.0, "Start": "2020-02-03T19:00:00+07:00", "Finish": "2020-02-04T19:00:00+07:00" } }, { "Status": 1, "Description": "Cuti Tahunan 2019", "EmplId": "8012160022", "EmplName": "ANGGA LESMANA", "EndDate": "2020-03-24T12:00:00", "RecId": 5637245208, "StartDate": "2020-03-23T12:00:00", "Schedule": { "TrueMonthly": 0.064516129032258063, "Month": 0.032854209445585217, "Days": 1.0, "Hours": 24.0, "Seconds": 86400.0, "Start": "2020-03-23T19:00:00+07:00", "Finish": "2020-03-24T19:00:00+07:00" } }], "Total": 0 };
    var el = $("#GridHistory");
    var grid = el.data("kendoGrid");
    if (grid) {
        grid.destroy();
    }

    el.kendoGrid({
        dataSource: {
            transport: {
                read: {
                    url: "/ESS/Leave/GetHistory",
                    dataType: "json",
                    type: "POST",
                    contentType: "application/json",
                },
                // read local data
                /*
                read: function (options) {
                    options.success(localdata); // where data is the local data array
                },
                */
                parameterMap: function (data, type) {
                    return JSON.stringify(data);
                }
            },
            schema: {
                data: function (res) {
                    if (res.StatusCode !== 200 && res.Status !== '') {
                        swalFatal("Fatal Error", `Error occured while fetching time attendance(s)\n${res.Message}`)
                        return []
                    }

                    // Set grouping by month year
                    var data = res.Data || [];
                    data.map(x => {
                        let date = str2date(x.StartDate);
                        x.GroupDate = new Date(date.getFullYear(), date.getMonth(), 1);
                        return x;
                    });

                    return data;
                },
                total: "Total",
            },
            group: {
                field: "GroupDate",
                dir: "desc"
            },
            sort: { field: "Schedule.Start", dir: "desc" },
            error: function (e) {
                swalFatal("Fatal Error", `Error occured while fetching leave history\n${e.xhr.responseText}`)
            },
        },
        noRecords: {
            template: "No attendance data available."
        },
        columns: [
            {
                field: "GroupDate",
                groupHeaderTemplate: function (e) {
                    return moment(e.value).format("MMMM YYYY")
                },
                hidden: true
            },
            {
                field: "Schedule",
                title: "Leave Date",
                template: function (e) {
                    var start = moment(e.StartDate).format("DD MMM YYYY");
                    var end = moment(e.EndDate).format("DD MMM YYYY");
                    return start + " - " + end;
                },
                width: 200
            },
            {
                field: "Description",
                title: "Description",
                width: 200
            },
            {
                headerAttributes: {
                    "class": "text-center",
                },
                attributes: {
                    "class": "text-center",
                },
                width: 200,
                field: "Status",
                title: "Status",
                template: function (e) {
                    let status = e.Status;
                    let statusClass = {
                        0: {
                            class: "badge badge-warning",
                            text: "Waiting for Approval",
                        },
                        1: {
                            class: "badge badge-success",
                            text: "Approved",
                        },
                        2: {
                            class: "badge badge-danger",
                            text: "Rejected",
                        },
                        3: {
                            class: "badge badge-info",
                            text: "InReview",
                        },
                    };

                    return `<span class="${(statusClass[status].class)}">${statusClass[status].text}</span>`;
                }
            },
        ],
        dataBound: function (e) {
            var $grid = this;
            var collapse = false;
            $grid.content.find(".k-grouping-row").each(function (e) {
                if (collapse) {
                    $grid.collapseGroup(this);
                }
                collapse = true;
            });
        }
    });
}

model.get.subordinate = async function () {
    // local data
    //var response = { "StatusCode": 200, "Message": null, "Data": [{ "EmployeeID": "7611060080", "EmployeeName": "KANG DAVID", "RecId": 5637144883 }, { "EmployeeID": "8309140001", "EmployeeName": "RIO SETIADY PRISMARENDRA", "RecId": 5637146076 }], "Total": 0 }
    let response = await ajax("/ESS/Leave/GetSubordinate", "GetSubordinate");
    if (response.StatusCode == 200 && response.Data) {
        console.log("Subordinate:", response.Data);
    }
}

model.render.subordinate = async function () {
    var el = $("#GridSubordinate");
    var grid = el.data("kendoGrid");
    if (grid) {
        grid.destroy();
    }
    el.kendoGrid({
        dataSource: {
            transport: {
                read: {
                    //url: "/assets/data/subordinate.json",
                    url: "/ESS/Leave/GetSubordinate",
                    dataType: "json",
                    type: "POST",
                    contentType: "application/json",
                },
                parameterMap: function (data, type) {
                    return JSON.stringify(data);
                }
            },
            schema: {
                data: function (res) {
                    if (res.StatusCode !== 200 && res.Status !== '') {
                        swalFatal("Fatal Error", `Error occured while fetching time attendance(s)\n${res.Message}`)
                        return []
                    }

                    // Set grouping by month year
                    var data = res.Data || [];
                    data.map(x => {
                        let date = str2date(x.StartDate);
                        x.GroupDate = new Date(date.getFullYear(), date.getMonth(), 1);
                        return x;
                    });

                    return data;
                },
                total: "Total",
            },
            group: {
                field: "GroupDate",
                dir: "desc"
            },
            sort: { field: "StartDate", dir: "desc" },
            error: function (e) {
                swalFatal("Fatal Error", `Error occured while fetching subordinate leave(s)\n${e.xhr.responseText}`)
            },
        },
        noRecords: {
            template: "No attendance data available."
        },
        columns: [
            {
                field: "Schedule",
                title: "Leave Date",
                template: function (e) {
                    var start = moment(e.StartDate).format("DD MMM YYYY");
                    var end = moment(e.EndDate).format("DD MMM YYYY");
                    return start + " - " + end;
                }
            },
            {
                field: "GroupDate",
                groupHeaderTemplate: function (e) {
                    return moment(e.value).format("MMMM YYYY")
                },
                hidden: true
            },
            {
                field: "EmplId",
                title: "Employee ID",
            },
            {
                field: "EmplName",
                title: "Employee Name",
            },
            {
                field: "Description",
                title: "Description",
            },
            {
                headerAttributes: {
                    "class": "text-center",
                },
                attributes: {
                    "class": "text-center",
                },
                field: "Status",
                title: "Status",
                template: function (e) {
                    let status = e.Status;
                    let statusClass = {
                        0: {
                            class: "badge badge-warning",
                            text: "Waiting for Approval",
                        },
                        1: {
                            class: "badge badge-success",
                            text: "Approved",
                        },
                        2: {
                            class: "badge badge-danger",
                            text: "Rejected",
                        },
                        3: {
                            class: "badge badge-info",
                            text: "InReview",
                        },
                    };

                    return `<span class="${(statusClass[status].class)}">${statusClass[status].text}</span>`;
                }
            },
        ],
        dataBound: function (e) {
            var $grid = this;
            var collapse = false;
            $grid.content.find(".k-grouping-row").each(function (e) {
                if (collapse) {
                    $grid.collapseGroup(this);
                }
                collapse = true;
            });
        }
    });
}

let _holidaysCache = {};
model.get.holidayKeyCache = function (firstDay, lastDay) {
    return `${moment(firstDay).format("MM/DD/YYYY")}_${moment(lastDay).format("MM/DD/YYYY")}`;
};

model.get.holiday = async function (firstDay, lastDay, employeeID) {
    // local data
    //var response = { "StatusCode": 200, "Message": null, "Data": { "Leaves": [{ "Schedule": { "TrueMonthly": 0.064516129032258063, "Month": 0.032854209445585217, "Days": 1.0, "Hours": 24.0, "Seconds": 86400.0, "Start": "2020-03-23T19:00:00+07:00", "Finish": "2020-03-24T19:00:00+07:00" }, "Description": "Cuti Tahunan 2019", "Type": "68", "TypeDescription": null, "AddressDuringLeave": null, "ContactDuringLeave": null, "SubtituteEmployeeID": "8309140001", "SubtituteEmployeeName": null, "PendingRequest": 0, "Filename": null, "Fileext": null, "Checksum": null, "Accessible": false, "Id": null, "Status": 1, "AXRequestID": null, "AXID": 5637245208, "EmployeeID": "8012160022", "EmployeeName": null, "Reason": null, "OldData": null, "NewData": null, "CreatedDate": "0001-01-01T00:00:00", "Action": 0, "LastUpdate": "0001-01-01T00:00:00", "UpdateBy": null }], "Holidays": [{ "EmployeeID": "8012160022", "LoggedDate": "2020-02-23T19:00:00+07:00", "AbsenceCode": "O", "IsLeave": false, "RecId": 0 }, { "EmployeeID": "8012160022", "LoggedDate": "2020-02-29T19:00:00+07:00", "AbsenceCode": "O", "IsLeave": false, "RecId": 0 }, { "EmployeeID": "8012160022", "LoggedDate": "2020-03-01T19:00:00+07:00", "AbsenceCode": "O", "IsLeave": false, "RecId": 0 }, { "EmployeeID": "8012160022", "LoggedDate": "2020-03-07T19:00:00+07:00", "AbsenceCode": "O", "IsLeave": false, "RecId": 0 }, { "EmployeeID": "8012160022", "LoggedDate": "2020-03-08T19:00:00+07:00", "AbsenceCode": "O", "IsLeave": false, "RecId": 0 }, { "EmployeeID": "8012160022", "LoggedDate": "2020-03-14T19:00:00+07:00", "AbsenceCode": "", "IsLeave": false, "RecId": 0 }, { "EmployeeID": "8012160022", "LoggedDate": "2020-03-15T19:00:00+07:00", "AbsenceCode": "", "IsLeave": false, "RecId": 0 }, { "EmployeeID": "8012160022", "LoggedDate": "2020-03-21T19:00:00+07:00", "AbsenceCode": "", "IsLeave": false, "RecId": 0 }, { "EmployeeID": "8012160022", "LoggedDate": "2020-03-22T19:00:00+07:00", "AbsenceCode": "", "IsLeave": false, "RecId": 0 }, { "EmployeeID": "8012160022", "LoggedDate": "2020-03-28T19:00:00+07:00", "AbsenceCode": "", "IsLeave": false, "RecId": 0 }, { "EmployeeID": "8012160022", "LoggedDate": "2020-03-29T19:00:00+07:00", "AbsenceCode": "", "IsLeave": false, "RecId": 0 }, { "EmployeeID": "8012160022", "LoggedDate": "2020-04-04T19:00:00+07:00", "AbsenceCode": "", "IsLeave": false, "RecId": 0 }] }, "Total": 0 };
    var temp = [];
    let response = await ajaxPost("/ESS/Leave/GetCalendar", { Start: firstDay, Finish: lastDay });
    //let response = { "StatusCode": 200, "Message": null, "Data": { "Leaves": [{ "Schedule": { "TrueMonthly": 0.1, "Month": 0.065708418891170434, "Days": 2.9999884259259257, "Hours": 71.999722222222218, "Seconds": 259199.0, "Start": "2020-04-03T00:00:00+07:00", "Finish": "2020-04-05T23:59:59+07:00" }, "Description": "Keluarga", "Type": "68", "TypeDescription": "Cuti Tahunan 2019", "AddressDuringLeave": null, "ContactDuringLeave": null, "SubtituteEmployeeID": "8107080145", "SubtituteEmployeeName": "A SOPYAN KRISTIAWAN", "PendingRequest": 3, "Filepath": null, "Filename": null, "Fileext": null, "Checksum": null, "Accessible": false, "Id": "5e78496a856edcfb506ae419", "Status": 0, "AXRequestID": "2003-00001174", "AXID": -1, "EmployeeID": "7312020022", "EmployeeName": "ABDULLOH", "Reason": "Keluarga", "OldData": null, "NewData": null, "CreatedDate": "2020-03-23T05:30:18.816Z", "Action": 0, "LastUpdate": "2020-03-23T05:30:18.816Z", "UpdateBy": null }], "Holidays": [{ "EmployeeID": "7312020022", "LoggedDate": "2020-05-05T19:00:00+07:00", "AbsenceCode": "", "IsLeave": false, "RecId": 0 }, { "EmployeeID": "7312020022", "LoggedDate": "2020-05-02T19:00:00+07:00", "AbsenceCode": "", "IsLeave": false, "RecId": 0 }, { "EmployeeID": "7312020022", "LoggedDate": "2020-04-27T19:00:00+07:00", "AbsenceCode": "", "IsLeave": false, "RecId": 0 }, { "EmployeeID": "7312020022", "LoggedDate": "2020-04-24T19:00:00+07:00", "AbsenceCode": "", "IsLeave": false, "RecId": 0 }, { "EmployeeID": "7312020022", "LoggedDate": "2020-04-19T19:00:00+07:00", "AbsenceCode": "", "IsLeave": false, "RecId": 0 }, { "EmployeeID": "7312020022", "LoggedDate": "2020-04-18T19:00:00+07:00", "AbsenceCode": "", "IsLeave": false, "RecId": 0 }, { "EmployeeID": "7312020022", "LoggedDate": "2020-04-15T19:00:00+07:00", "AbsenceCode": "", "IsLeave": false, "RecId": 0 }, { "EmployeeID": "7312020022", "LoggedDate": "2020-04-09T19:00:00+07:00", "AbsenceCode": "", "IsLeave": false, "RecId": 0 }, { "EmployeeID": "7312020022", "LoggedDate": "2020-04-06T19:00:00+07:00", "AbsenceCode": "", "IsLeave": false, "RecId": 0 }, { "EmployeeID": "7312020022", "LoggedDate": "2020-03-31T19:00:00+07:00", "AbsenceCode": "O", "IsLeave": false, "RecId": 0 }] }, "Total": 0 }//await ajaxPost("/ESS/Leave/GetCalendar", { Start: firstDay, Finish: lastDay });
    (response.Data.Holidays || []).forEach(function (y) {
        if (!_.includes(model.data.tempHoliday(), y.LoggedDate)) {
            temp.push(new Date(y.LoggedDate));
        }
    });

    (response.Data.Leaves || []).forEach(function (y) {
        var finish = new Date(y.Schedule.Finish);
        for (var start = new Date(y.Schedule.Start); start <= finish; start.setDate(start.getDate() + 1)) {
            temp.push(new Date(start));
        }
    });
    return temp;
}

model.render.subtitution = async function () {
    // local data
    //var response = { "StatusCode": 200, "Message": null, "Data": [{ "EmployeeID": "3680403193", "EmployeeName": "NUROCHMAN", "RecId": 5637144812 }, { "EmployeeID": "8308100166", "EmployeeName": "AGUS PRAYITNO ANDRIYAS", "RecId": 5637144638 }, { "EmployeeID": "7605080130", "EmployeeName": "ANANG CAHYONO", "RecId": 5637144976 }, { "EmployeeID": "8610100169", "EmployeeName": "ANDHIKA WIJAYA KUSUMA", "RecId": 5637144952 }, { "EmployeeID": "7711050075", "EmployeeName": "BAGUS HERMAWAN", "RecId": 5637145002 }, { "EmployeeID": "3710503516", "EmployeeName": "DWI WIDODO", "RecId": 5637144834 }, { "EmployeeID": "3751203513", "EmployeeName": "IMAM MUHADI", "RecId": 5637144862 }, { "EmployeeID": "7711040056", "EmployeeName": "IWAN PARYONO", "RecId": 5637144995 }, { "EmployeeID": "7408020026", "EmployeeName": "YUDI WINARNO", "RecId": 5637144642 }, { "EmployeeID": "3640503167", "EmployeeName": "YUSUF", "RecId": 5637144766 }, { "EmployeeID": "8211080148", "EmployeeName": "ROBERT TULUSWIJAYANTO", "RecId": 5637144964 }, { "EmployeeID": "7508050066", "EmployeeName": "AGUS BASUKI", "RecId": 5637144656 }, { "EmployeeID": "7506100178", "EmployeeName": "ALWIDIYANTO", "RecId": 5637144652 }, { "EmployeeID": "8512120281", "EmployeeName": "ANTONIUS NATALI PUTRA", "RecId": 5637145364 }, { "EmployeeID": "8305120273", "EmployeeName": "DENNY SETIAWAN", "RecId": 5637145354 }, { "EmployeeID": "8101110236", "EmployeeName": "ITAN RESMANA", "RecId": 5637144922 }, { "EmployeeID": "8307130299", "EmployeeName": "YOYOK HARIYONO", "RecId": 5637145356 }, { "EmployeeID": "3661203212", "EmployeeName": "ABDILLADJIS", "RecId": 5637144795 }], "Total": 0 }

    var arr = [];
    let response = await ajaxPost("/ESS/Leave/GetSubtitutions", {});
    arr = _.sortBy(response.Data, "EmployeeName");
    _.map(arr, function (x) {
        model.data.subtitution.push({ ID: x.EmployeeID, Name: x.EmployeeName });
    });
}

var consumeDay;
var maxDayLeave;
var dataSelectType;
model.on.leaveTypeChange = function () {
    var self = model;
    var typeId = this.value();
    var $calendar = $("#dateCalendarLeave").data("kendoDateRangePicker");
    $calendar.enable(!!typeId);
    $calendar.range({ start: null, end: null });
    model.data.maxdayofleave(0);
    model.data.infoTypeLeave("");

    if (!typeId) {
        return;
    }

    var data = _.find(model.data.typeleave(), function (d) {
        return d.TypeId == typeId;
    });
    dataSelectType = data;
    console.log("select:", dataSelectType);
    model.action.enableCalendar(true);
    maxDayLeave = data.MaxDayLeave;

    var remainder = 0;
    if (data.CategoryId == "ANU") {
        //calendar.options.selectable = "multiple";
        model.is.indicatorVisible(true);
        remainder = data.Remainder;
    } else if (data.CategoryId == "SPE") {
        //calendar.options.selectable = "single";
        model.is.indicatorVisible(true);
        //consumeDay = data.ConsumeDay;
        remainder = data.MaxDayLeave;
    }

    model.data.anchorRemainder(remainder);
    model.data.remainder(remainder);
    //model.data.pending(initializePending);
    model.data.maxdayofleave(data.MaxDayLeave);
    //calendar.selectDates([]);
    model.data.infoTypeLeave("Valid between " + moment(data.EffectiveDateFrom).format("DD MMM YYYY") + " until " + moment(data.EffectiveDateTo).format("DD MMM YYYY"));
}

let leaveDocumentTemp = "";
model.on.documentSelect = function (e) {
    let self = model;
    var valid = self.app.data.validateKendoUpload(e, "Leave");
    if (valid) {
        leaveDocumentTemp = self.data.leave().HTMLFile();
        self.data.leave().HTMLFile("");
    } else {
        e.preventDefault();
    }
}

model.on.documentRemove = function (e) {
    let self = model;
    self.data.family().HTMLFile(leaveDocumentTemp);
}

model.get.EditCalendarLeave = function (axid) {
    var tempLeave = [];
    var el = $("#viewCalendarLeave");
    var calendar = el.data("kendoCalendar");
    if (calendar) {
        console.log("load kendocalendar");
        // load other leave not selected
        model.data.leaves.map(function (x) {
            if (x.AXID != axid) {
                var d1 = x.start, d2 = x.end;
                while (d1 <= d2) {
                    tempLeave.push(new Date(d1));
                    console.log(new Date(d1));
                    d1 = moment(d1).add(1, 'days');
                }
            }

        });
        tempLeave.forEach(function (dd) {
            var day = $(`[data-value='${dd.getFullYear()}/${dd.getMonth()}/${dd.getDate()}']`);
            if (day.length > 0) {
                var td = day.closest("td");
                if (td.length > 0) {
                    td.removeClass("k-state-selected");
                    td.removeClass("k-today");
                    td.addClass("k-state-disabled");
                }
            }
        });
    } else {
        console.log("not kendocalendar");
    }
}

Date.prototype.addDays = function (days) {
    var date = new Date(this.valueOf());
    date.setDate(date.getDate() + days);
    return date;
}

model.data.tempHoliday = ko.observableArray();
