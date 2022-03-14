var today = new Date();
today.setDate(today.getDate() - 1);
var days30ago = new Date();
days30ago.setDate(days30ago.getDate() - 45);

var lastMonth = (new Date).setMonth((new Date).getMonth() - 1);
var firstDayLastMonth = firstDayOfMonth(lastMonth);
var lastDayLastMonth = lastDayOfMonth(lastMonth);

// Method Creating New Models
model.newTimeAttendance = function (obj) {
    var proto = {};

    if (!obj) {
        obj = _.clone(this.proto.TimeAttendance);
    }

    proto = Object.assign({
        "FormattedClockIn": "00:00",
        "FormattedClockOut": "00:00",
        "FormattedClockLoggedDate": "12 Dec 2019",
    }, obj);

    if (proto.LoggedDate) {
        proto.FormattedClockLoggedDate = moment(proto.LoggedDate).format("DD MMM YYYY");
    }
    if (proto.ActualLogedDate) {
        proto.FormattedClockIn = moment(proto.ActualLogedDate.Start).format("HH:mm");
    }

    if (proto.ActualLogedDate) {
        proto.FormattedClockOut = moment(proto.ActualLogedDate.Finish).format("HH:mm");
    }

    return _.clone(proto);
}

// Model Data
model.data.TimeAttendance = ko.observable(model.newTimeAttendance());
model.list.absenceCode = ko.observableArray([]);
model.list.absenceCodeDropdown = ko.observableArray([]);
model.data.listFileName = ko.observableArray([]);
model.data.StartDate = ko.observable(moment().subtract(7, "days").toDate());
model.data.EndDate = ko.observable(today);
model.data.absenceCodeEditable = ko.observableArray(["H", "D", "TB", "TP"]);
model.is.showattachment = ko.observable(false);
model.map.absenceCode = {};
model.data.days30ago = ko.observable(days30ago);
model.data.todaymin1 = ko.observable(today);

model.data.DetailAgenda = ko.observable();
model.on.absenceCodeChange = function (e) {
    let data = model.map.absenceCode[this.value()];
    if (data) {
        model.is.showattachment(data.IsAttachment);
        return;
    }
    model.is.showattachment(false);
};

model.action.refreshMyTimeAttendance = function (uiOnly = false) {

    if (model.data.StartDate() > model.data.EndDate()) {
        swalError("Warning", "start date is greater than the end date");
        return false;
    };

    var $grid = $("#gridMyTimeAttendance").data("kendoGrid");
    if ($grid) {
        if ($($grid.content).find(".k-loading-mask").length > 0) {
            return true;
        }

        if (uiOnly) {
            $grid.refresh();
        } else {
            $grid.dataSource.read();
        }
    }
};

model.action.refreshMyTimeAttendanceMonthly = function (uiOnly = false) {
    model.data.StartDate(moment().startOf('month').format("MM/DD/YYYY"));
    model.data.EndDate(today);
    var $grid = $("#gridMyTimeAttendance").data("kendoGrid");
    if ($grid) {
        if ($($grid.content).find(".k-loading-mask").length > 0) {
            return true;
        }

        if (uiOnly) {
            $grid.refresh();
        } else {
            $grid.dataSource.read();
        }
    }
};

model.action.refreshMyTimeAttendanceYearly = function (uiOnly = false) {
    model.data.StartDate(moment().startOf('year').format("MM/DD/YYYY"));
    model.data.EndDate(today);
    var $grid = $("#gridMyTimeAttendance").data("kendoGrid");
    if ($grid) {
        if ($($grid.content).find(".k-loading-mask").length > 0) {
            return true;
        }

        if (uiOnly) {
            $grid.refresh();
        } else {
            $grid.dataSource.read();
        }
    }
};

model.action.refreshSubordinateTimeAttendance = function (uiOnly = false) {

    if (model.data.StartDate() > model.data.EndDate()) {
        swalError("Warning", "start date is greater than the end date");
        return false;
    };

    var $grid = $("#gridSubordinateTimeAttendance").data("kendoGrid");
    if ($grid) {
        if ($($grid.content).find(".k-loading-mask").length > 0) {
            return true;
        }

        if (uiOnly) {
            $grid.refresh();
        } else {
            $grid.dataSource.read();
        }
    }
};

model.action.refreshSubordinateTimeAttendanceMonthly = function (uiOnly = false) {
    model.data.StartDate = ko.observable(moment().startOf('month').format("MM/DD/YYYY"));
    model.data.EndDate = ko.observable(moment().toDate());
    var $grid = $("#gridSubordinateTimeAttendance").data("kendoGrid");
    if ($grid) {
        if ($($grid.content).find(".k-loading-mask").length > 0) {
            return true;
        }

        if (uiOnly) {
            $grid.refresh();
        } else {
            $grid.dataSource.read();
        }
    }
}

model.action.refreshSubordinateTimeAttendanceYearly = function (uiOnly = false) {
    model.data.StartDate = ko.observable(moment().startOf('year').format("MM/DD/YYYY"));
    model.data.EndDate = ko.observable(moment().toDate());
    var $grid = $("#gridSubordinateTimeAttendance").data("kendoGrid");
    if ($grid) {
        if ($($grid.content).find(".k-loading-mask").length > 0) {
            return true;
        }

        if (uiOnly) {
            $grid.refresh();
        } else {
            $grid.dataSource.read();
        }
    }

}

model.render.gridTimeAttendance = function () {
    let self = model;
    let $el = $("#gridMyTimeAttendance");

    let dStart = model.data.StartDate();
    dStart.setHours(0);
    model.data.StartDate(dStart);

    let _marker = (data, fieldname, fn) => {
        if (typeof fn != "function") {
            fn = (x) => { return x };
        }

        let realValue = (data.TimeAttendance) ? fn(data.TimeAttendance[fieldname]) : undefined;
        let newValue = (data.UpdateRequest) ? fn(data.UpdateRequest[fieldname]) : null;

        if (data.UpdateRequest && data.TimeAttendance) {
            if (data.UpdateRequest) {
                if (data.UpdateRequest.Action <= 1) {
                    if (realValue !== newValue)
                        return `<del class="text-muted">${realValue}</del><br> ${newValue}`
                } else {
                    return `<del class="text-danger">${realValue}</del>`
                }

            }
            return fn(data.TimeAttendance[fieldname]);
        } else {
            var d = data.TimeAttendance || data.UpdateRequest;
            return fn(d[fieldname]);
        }
    };

    if ($el) {
        let $grid = $el.getKendoGrid();
        if (!!$grid) {
            $grid.destroy();
        }
        $el.kendoGrid({
            dataSource: {
                transport: {
                    read: {
                        url: "/ESS/TimeManagement/Get",
                        dataType: "json",
                        type: "POST",
                        contentType: "application/json",
                    },
                    parameterMap: function (data, type) {
                        data.EmployeeID = ""
                        data.Range = {
                            Start: model.data.StartDate(),
                            Finish: model.data.EndDate()
                        }
                        return JSON.stringify(data);
                    }
                },
                schema: {
                    data: function (res) {
                        if (res.StatusCode !== 200 && res.Status !== '') {
                            swalFatal("Fatal Error", `Error occured while fetching time attendance(s)\n${res.Message}`)
                            return []
                        }

                        return res.Data || [];
                    },
                    total: "Total",
                },
                error: function (e) {
                    swalFatal("Fatal Error", `Error occured while fetching attendance(s)\n${e.xhr.responseText}`)
                },
                sort: { field: "LoggedDate", dir: "desc" }
            },
            noRecords: {
                template: "No attendance data available."
            },
            columns: [
                //{
                //    attributes: {
                //        "class": "text-center",
                //    },
                //    template: function (data) {
                //        if (data.UpdateRequest) {
                //            return `<button class="btn btn-xs btn-outline-warning" onclick="model.action.discardTimeAttendance('${data.uid}'); return false;">
                //                    <i class="fa mdi mdi-delete"></i>
                //                </button>`;
                //        }
                //        return '';

                //    },
                //    width: 50,
                //},
                {
                    field: "loggedDate",
                    title: "Date",
                    template: function (d) {
                        return _marker(d, "LoggedDate", (v) => {
                            return moment(v).format("DD MMM YYYY")
                        });
                    },
                    width: 120,
                },
                {
                    field: "loggedDate",
                    title: "Day",
                    template: function (d) {
                        return _marker(d, "LoggedDate", (v) => {
                            return moment(v).format("dddd")
                        });
                    },
                    width: 100,
                },
                {
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
                    title: "ScheduledDate",
                    template: function (d) {

                        return _marker(d, "ScheduledDate", (v) => {
                            var start = moment(v.Start).format("HH:mm")
                            var end = moment(v.Finish).format("HH:mm")
                            var result = start + ' - ' + end
                            return result
                        });
                    },
                    width: 200,
                },
                {
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
                    title: "Clock In/Out",
                    template: function (d) {
                        return _marker(d, "ActualLogedDate", (v) => {

                            var start = moment(v.Start).format("HH:mm")
                            var end = moment(v.Finish).format("HH:mm")
                            var result = start + ' - ' + end
                            return result;
                        });
                    },
                    width: 200,
                },
                {
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
                    field: "AbsenceCode",
                    title: "Absence Code",

                    template: function (d) {

                        return _marker(d, "AbsenceCode");
                    },
                    width: 200,
                },
                {
                    width: 75,
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
                    template: function (data) {
                        var edit = ``                        
                        var loggedDate = new Date(data.TimeAttendance.LoggedDate);
                        if (
                            // (loggedDate >= firstDayLastMonth && loggedDate <= lastDayLastMonth) || (lastDayLastMonth <= loggedDate)
                            (loggedDate >= days30ago)
                           ) {

                            var tmAbsenceCodes = data.TimeAttendance.AbsenceCode.split(",") || [];
                            var isEditable = false;
                            tmAbsenceCodes.forEach((x) => {
                                absenceCode = model.map.absenceCode[x];
                                isEditable = isEditable || (!!absenceCode && absenceCode.IsEditable);
                            });                            

                            if (isEditable) {

                                if (!data.UpdateRequest) { // Request ever

                                    edit = `<button class="btn btn-xs btn-outline-info" onclick="model.action.openModalUpdateTimeAttendance('${data.uid}'); return false;"><i class="fa mdi mdi-pencil"></i></button>`

                                } else {

                                    edit = ``

                                }

                            } else {

                                edit = ``
                            }

                        } else {

                            edit = ``

                        }

                        return edit;
                    },
                }
            ]
        });
    }
}

model.render.gridSubordinateTimeAttendance = function () {
    let $el = $("#gridSubordinateTimeAttendance");
    if ($el) {
        let $grid = $el.getKendoGrid();
        if (!!$grid) {
            $grid.destroy();
        }
        $el.kendoGrid({
            dataSource: {
                transport: {
                    read: {
                        url: "/ESS/TimeManagement/GetSubordinate",
                        dataType: "json",
                        type: "POST",
                        contentType: "application/json",
                    },
                    parameterMap: function (data, type) {
                        data.EmployeeID = ""
                        data.Range = {
                            Start: model.data.StartDate(),
                            Finish: model.data.EndDate()
                        }
                        return JSON.stringify(data);
                    }
                },
                group: {
                    field: "LoggedDate",
                    dir: "desc"
                },
                schema: {
                    data: function (res) {

                        if (res.StatusCode !== 200 && res.Status !== '') {
                            swalFatal("Fatal Error", `Error occured while fetching subordinate time attendance(s)\n${res.Message}`)
                            return []
                        }

                        return res.Data || [];
                    },
                    total: "Total",
                },
                error: function (e) {
                    swalFatal("Fatal Error", `Error occured while fetching subordinate attendance(s)\n${e.xhr.responseText}`)
                },
                sort: { field: "LoggedDate", dir: "desc" }
            },
            noRecords: {
                template: "No subordinate attendance data available."
            },
            //filterable: true,
            //sortable: true,
            //pageable: true,
            //groupable: true,
            columns: [
                {
                    field: "EmployeeID",
                    title: "Employee ID",
                    width: 200,
                },
                {
                    field: "EmployeeName",
                    title: "Employee Name",
                    width: 200,
                },
                {
                    field: "LoggedDate",
                    title: "Date",
                    groupHeaderTemplate: function (e) {
                        return moment(e.value).format("dddd, DD MMM YYYY")
                    },
                    template: function (e) {
                        return moment(e.LoggedDate).format("DD MMM YYYY")
                    },
                    hidden: true
                },
                //{
                //    field: "LoggedDate",
                //    title: "Day",
                //    template: function (e) {
                //        return moment(e.LoggedDate).format("dddd")
                //    }
                //},
                {
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
                    title: "ScheduledDate",
                    template: function (e) {

                        var start = moment(e.ScheduledDate.Start).format("HH:mm")
                        var end = moment(e.ScheduledDate.Finish).format("HH:mm")
                        var result = start + ' - ' + end
                        return result;
                    },
                    width: 200,
                },
                {
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
                    title: "Clock In/Out",
                    template: function (e) {
                        if (e.Absent) {
                            return '-';
                        }
                        var start = moment(e.ActualLogedDate.Start).format("HH:mm")
                        var end = moment(e.ActualLogedDate.Finish).format("HH:mm")
                        var result = start + ' - ' + end
                        return result;
                    },
                    width: 200,
                },
                {
                    field: "AbsenceCode",
                    title: "Absence Code",
                    template: function (d) {
                        return d.AbsenceCode;
                    },
                    width: 200,
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
}

model.action.openModalUpdateTimeAttendance = function (uid) {
    model.data.TimeAttendance(model.newTimeAttendance());
    dataGrid = $("#gridMyTimeAttendance").data("kendoGrid").dataSource.getByUid(uid);
    if (dataGrid) {
        model.data.TimeAttendance(model.newTimeAttendance(dataGrid.TimeAttendance));
        model.data.TimeAttendance().Old = _.cloneDeep(model.newTimeAttendance(dataGrid.TimeAttendance));
        let attendance = ko.toJS(model.data.TimeAttendance());

        let ac = model.map.absenceCode[attendance.AbsenceCode];
        if (ac) {
            model.is.showattachment(ac.IsAttachment);
        } else {
            model.is.showattachment(false);
        }
        $("#modalFormTimeAttendance").modal("show");
    }
}

model.action.openFormRecommendAbsence = function (uid) {
    model.data.TimeAttendance(model.newTimeAttendance());
    dataGrid = $("#gridMyTimeAttendance").data("kendoGrid").dataSource.getByUid(uid);
    if (dataGrid) {
        model.data.AbsenceRecommendation({
            AbsenceCode: dataGrid.AbsenceCode,
            Date: moment(dataGrid.LoggedDate).format("DD MMM YYYY"),
            Clockin: moment(dataGrid.ActualLogedDate.Start).format("HH:mm"),
            Clockout: moment(dataGrid.ActualLogedDate.Finish).format("HH:mm"),
            Start: new Date(dataGrid.ScheduledDate.Start).getTime().toString(),
            Finish: new Date(dataGrid.ScheduledDate.Finish).getTime().toString(),
            Recid: dataGrid.AXID,
        });
        $("#modalFormTimeAttendance").modal("show");
    }
}

model.get.absenceCode = async function () {
    let response = await ajax("/ESS/TimeManagement/GetAbsenceCode", "GET");
    if (response.StatusCode == 200) {
        let result = response.Data || []

        result.forEach((d) => {
            model.map.absenceCode[d.IdField] = d;
        });

        return result;
    }
    return [];
};

model.init.myAttendace = function () {
    var self = model;
    setTimeout(async function () {
        let absenceCode = await self.get.absenceCode();
        self.list.absenceCode(absenceCode);
        self.list.absenceCodeDropdown(absenceCode.filter(a => {
            return a.IsOnList;
        }));
        self.action.refreshMyTimeAttendance(true);
    });
    model.render.gridTimeAttendance();
}

model.action.openDocumentUpload = function () {
    $("#documentUpload").click();
};

model.on.documentSelect = function (e) {
    let self = model;
    var valid = self.app.data.validateKendoUpload(e, "Absence Recomendation");
    if (!valid) {
        e.preventDefault();
    }
}

model.on.renderDocumentForm = function () {
    $el = $("#documentUpload");
    if (!!$el) {
        let $upload = $el.getKendoUpload();
        if (!!$upload) {
            $upload.destroy();
        }
        $el.kendoUpload({
            select: (e) => {
                setTimeout(() => {
                    let tempFilenames = []
                    if ($("#documentUpload").data("kendoUpload").getFiles().length > 1) {
                        $(".k-upload-files.k-reset").find("li")[0].remove();
                    }
                    const files = $("#documentUpload").data("kendoUpload").getFiles();
                    for (let i = 0; i < files.length; i++) {
                        tempFilenames.unshift({
                            icon: files[i].extension,
                            name: files[i].name,
                            extension: files[i].extension,
                            size: files[i].size,
                            uid: files[i].uid,
                            rawFile: files[i].rawFile,
                            status: 'new'
                        });
                    }
                    model.data.listFileName(tempFilenames);
                }, 100);
            },
        });
    }
}

model.action.updateTimeAttendance = async function (e) {
    var result = await swalConfirm("Absence Recomendation", 'Are you sure update your time attendance?');
    if (result.value) {
        try {
            isLoading(true);
            window.onbeforeunload = function(){
                return "Are you sure leaving this page ?";
            };

            var $modal = $("#modalFormTimeAttendance");            

            var timeAttendance = ko.mapping.toJS(model.data.TimeAttendance())
            if (timeAttendance.AbsenceCode == null || timeAttendance.AbsenceCode == "") {
                isLoading(false);
                window.onbeforeunload = null;
                return swalAlert("Recommendation Absence", "Absence Code is Required");
            } else if (timeAttendance.Reason == null || timeAttendance.Reason == "") {
                isLoading(false);
                window.onbeforeunload = null;
                return swalAlert("Recommendation Absence", "Reason is Required");
            }
            timeAttendance.ActualLogedDate.Start = timeAttendance.ActualLogedDate.Start.split("T")[0] + "T" + timeAttendance.FormattedClockIn + ":00+07:00";
            timeAttendance.ActualLogedDate.Finish = timeAttendance.ActualLogedDate.Finish.split("T")[0] + "T" + timeAttendance.FormattedClockOut + ":00+07:00";
            var absenceCode = model.map.absenceCode[timeAttendance.AbsenceCode];

            if (absenceCode) {
                timeAttendance.AbsenceCodeDescription = absenceCode.DescriptionField;
            }

            let formData = new FormData();
            formData.append("JsonData", JSON.stringify(timeAttendance));

            if (model.is.showattachment()) {
                var files = $('#Filepath').getKendoUpload().getFiles();
                if (files.length > 0) {
                    formData.append("FileUpload", files[0].rawFile);
                } else {
                    swalAlert("Absence Recomendation", "Document attachment could not be empty");
                    return;
                }
            }

            try {
                $modal.modal("hide");

                ajaxPostUpload("/ESS/TimeManagement/UpdateTimeAttendance", formData, function (data) {
                    isLoading(false);
                    window.onbeforeunload = null;
                    if (data.StatusCode == 200) {
                        swalSuccess("Absence Recomendation", data.Message);
                        model.action.refreshMyTimeAttendance();
                    } else {
                        $modal.modal("show");
                        swalError("Absence Recomendation", data.Message);
                    }
                }, function (data) {
                    $modal.modal("show");
                    swalFatal("Absence Recomendation", data.Message);
                    isLoading(false);
                    window.onbeforeunload = null;
                })
            } catch (e) {
                swalFatal("Absence Recomendation", e);
                isLoading(false);
                window.onbeforeunload = null;
            }
        } catch (e) {
            isLoading(true);
            console.error(e);
            window.onbeforeunload = null;
        }
    }

}

/*
model.action.updateTimeAttendance = function (e) {
    var timeAttendance = ko.mapping.toJS(model.data.TimeAttendance())
    if (timeAttendance.AbsenceCode == null || timeAttendance.AbsenceCode == "") {
        return swalAlert("Recommendation Absence", "Absence Code is Required");
    } else if (timeAttendance.Reason == null || timeAttendance.Reason == "") {
        return swalAlert("Recommendation Absence", "Reason is Required");
    }
    timeAttendance.ActualLogedDate.Start = timeAttendance.ActualLogedDate.Start.split("T")[0] + "T" + timeAttendance.FormattedClockIn + ":00+07:00";
    timeAttendance.ActualLogedDate.Finish = timeAttendance.ActualLogedDate.Finish.split("T")[0] + "T" + timeAttendance.FormattedClockOut + ":00+07:00";
    var absenceCode = model.map.absenceCode[timeAttendance.AbsenceCode];

    if (absenceCode) {
        timeAttendance.AbsenceCodeDescription = absenceCode.DescriptionField;
    }

    let formData = new FormData();
    formData.append("JsonData", JSON.stringify(timeAttendance));

    if (model.is.showattachment()) {
        var files = $('#Filepath').getKendoUpload().getFiles();
        if (files.length > 0) {
            formData.append("FileUpload", files[0].rawFile);
        } else {
            swalAlert("Absence Recomendation", "Document attachment could not be empty");
            return;
        }
    }

    try {
        var $modal = $("#modalFormTimeAttendance");

        isLoading(true);
        $modal.modal("hide");
        ajaxPostUpload("/ESS/TimeManagement/UpdateTimeAttendance", formData, function (data) {
            isLoading(false);
            if (data.StatusCode == 200) {                
                swalSuccess("Absence Recomendation", data.Message);
                model.action.refreshMyTimeAttendance();
            } else {
                $modal.modal("show");
                swalError("Absence Recomendation", data.Message);
            }
        }, function (data) {
            $modal.modal("show");
            swalFatal("Absence Recomendation", data.Message);
            isLoading(false);
        })
    } catch (e) {
        swalFatal("Absence Recomendation", e);
        isLoading(false);
    } 
}
*/

model.action.discardTimeAttendance = async function (uid) {
    var dialogTitle = "Update Time Attendance Information";
    dataGrid = $("#gridMyTimeAttendance").data("kendoGrid").dataSource.getByUid(uid);
    var result = await swalConfirm(dialogTitle, `Are you sure discarding Update Request with reason : "${dataGrid.UpdateRequest.Reason}" ?`);
    if (result.value) {
        var Id = (dataGrid.UpdateRequest) ? dataGrid.UpdateRequest.Id : "";
        if (Id) {
            isLoading(true)
            ajaxPost("/ess/TimeManagement/discardtimeattendancechange/" + Id, {}, function (data) {
                isLoading(false)
                if (data.StatusCode == 200) {
                    model.action.refreshMyTimeAttendance();
                    swalSuccess(dialogTitle, data.Message);
                } else {
                    swalError(dialogTitle, data.Message);
                }
            }, function (data) {
                swalError(dialogTitle, data.Message);
            });
        }
    }
}

//agenda

model.action.showAgenda = function (uid) {
    var datagrid = $("#gridAgenda").data('kendoGrid').dataSource.data()
    var data = datagrid.find(function (e) {
        return uid == e.uid
    })
    var start = moment(data.Schedule.Start).format("DD/MM/YYYY")
    var end = moment(data.Schedule.Finish).format("DD/MM/YYYY")
    var result = start + ' - ' + end
    data['_scheduleFull'] = result
    model.data.DetailAgenda(data);
    $("#ModalDetailAgenda").modal("show");
};


model.action.refreshAgenda = function (uiOnly = false) {

    if (model.data.StartDate() > model.data.EndDate()) {
        swalError("Warning", "start date is greater than the end date");
        return false;
    };

    var $grid = $("#gridAgenda").data("kendoGrid");
    if ($grid) {
        if ($($grid.content).find(".k-loading-mask").length > 0) {
            return true;
        }

        if (uiOnly) {
            $grid.refresh();
        } else {
            $grid.dataSource.read();
        }
    }
};

model.render.gridAgenda = function () {
    let $el = $("#gridAgenda");
    if ($el) {
        let $grid = $el.getKendoGrid();
        if (!!$grid) {
            $grid.destroy();
        }
        $el.kendoGrid({
            dataSource: {
                //data: { "StatusCode": 200, "Message": null, "Data": [{ "Id": null, "AgendaID": null, "Issuer": "HR Departemen", "Name": "RTEssdfsdf", "Description": "Surat Edaran Jam Kerja New Normal", "Notes": "", "AXID": 5637145327, "Location": "All Pegawai", "Category": null, "CreatedDate": "0001-01-01T00:00:00", "AgendaFor": 0, "AgendaForDescription": "All", "AgendaType": 1, "EmployeeRecipients": [], "Attachments": [{ "AXID": 5637252583, "OldData": null, "NewData": null, "Notes": "", "Filepath": "\\\\172.19.155.50\\DocAttachmentAX\\bilder-ueber-about-us-mock-up-test-oT_DOC-005151.jpg", "Filehash": "Yy1xV24dUJrKESWHrFY6tcMlIkxxZwMI/PZjjs4W5ltLpmIdjzo6XkIwwcgq2V3I5pPUUAJFknHN0x83pz8Vb58udWLShh+YPBDoQOISKTsPjiZfvzkTqbo0DfVPwL19OU7jRYJPMpVvf1PRUxDSwuWaiNkSai0YHwwGHOse4QmeRzmg6+pyGg+t7QXMh+hDBcNEoHDFKE6wUub0XLFAzbtbreSa3LRiE5TS2pv02qQ=", "Filename": "bilder-ueber-about-us-mock-up-test-oT_DOC-005151.jpg", "Fileext": ".jpg", "Checksum": null, "Accessible": false }], "Schedule": { "TrueMonthly": -1.0, "Month": -0.065708418891170434, "Days": -2.0, "Hours": -48.0, "Seconds": -172800.0, "Start": "2020-06-07T00:00:00+07:00", "Finish": "2020-06-05T00:00:00+07:00" }, "Hash": "9dfdbb3ae6aa523d00f55f66aa158cfc", "LastUpdate": "0001-01-01T00:00:00", "UpdateBy": null }], "Total": 0 },
                transport: {
                    read: {
                        url: "/ESS/Agenda/Get",
                        dataType: "json",
                        type: "POST",
                        contentType: "application/json",
                    },
                    parameterMap: function (data, type) {
                        data.Range = {
                            Start: model.data.StartDate(),
                            Finish: model.data.EndDate()
                        }
                        return JSON.stringify(data);
                    }
                },
                schema: {
                    data: function (res) {

                        if (res.StatusCode !== 200 && res.Status !== '') {
                            swalFatal("Fatal Error", `Error occured while fetching agenda(s)\n${res.Message}`)
                            return []
                        }

                        return res.Data || [];
                    },
                    total: "Total",
                },
                error: function (e) {
                    swalFatal("Fatal Error", `Error occured while fetching agenda(s)\n${e.xhr.responseText}`)
                },
            },
            noRecords: {
                template: "No agenda data available."
            },
            columns: [
                {
                    //field: "EmployeeID",
                    title: "Name - Issuer Description",
                    template: function (e) {
                        var name = e.Name;
                        var issuer = e.Issuer;
                        var desc = e.Description;
                        //console.log('testing ', name, issuer)
                        return `<span>${name} - ${issuer}<br><small style='color:grey;'>${desc}</small></span>`
                    },
                    width: 300,
                },
                {
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
                    title: "Schedule",
                    template: function (e) {

                        var start = moment(e.Schedule.Start).format("DD-MM-YYYY")
                        var end = moment(e.Schedule.Finish).format("DD-MM-YYYY")
                        var result = start + ' - ' + end
                        return result;
                    },
                    width: 200,
                },
                {
                    field: "Location",
                    title: "Location",
                    width: 200,
                },
                {
                    title: "",
                    width: 100,
                    template: function (e) {
                        //var data = ko.mapping.fromJS(e);
                        //console.log('test', data)
                        var btn = `<button class="btn btn-xs btn-outline-info" onclick="model.action.showAgenda('${e.uid}'); return false;"><i class="fa mdi mdi-eye"></i></button>`
                        return btn;
                    },
                    attributes: {
                        "class": "text-center",
                    },
                },
            ],
        });
    }
}


model.init.agenda = function () {
    var self = model;
    self.render.gridAgenda();
}
//end agenda