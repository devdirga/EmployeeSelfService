var today = new Date();
var prevDay = new Date();
prevDay.setDate(prevDay.getDate() - 300);

model.newSPPD = function (obj) {
    let SPPD = _.clone(this.proto.SPPD);
    if (obj && typeof obj == "object") {
        return ko.mapping.fromJS(Object.assign(SPPD, obj));
    }
    return ko.mapping.fromJS(SPPD);
}

model.newTravel = function (obj) {
    let travel = _.clone(this.proto.Travel);
    if (travel) {

        if (!travel.Schedule) {
            travel.Schedule = _.clone(this.proto.DateRange || {});
            travel.Schedule.Start = new Date();
            travel.Schedule.Finish = new Date();
        }
    }

    if (obj && typeof obj == "object") {
        return ko.mapping.fromJS(Object.assign(travel, obj));
    }

    travel.DocumentList = travel.DocumentList || [];

    return ko.mapping.fromJS(travel);
};

model.is.overseas = ko.observable(false);

model.data.Travel = ko.observable(model.newTravel());
model.data.DataHistory = ko.observableArray([]);
model.data.StartDate = ko.observable(moment().subtract(7, "days").toDate());
model.data.EndDate = ko.observable(today);
model.data.SPPD = ko.observable(model.newSPPD());
model.data.Total = ko.observable(0);
model.data.travelStatus = ko.observable("");
model.data.Filename = ko.observable("");
model.data.TransportationID = ko.observableArray([]);


model.list.requestFor = ko.observableArray([]);
model.list.transportation = ko.observableArray([]);
model.list.purpose = ko.observableArray([]);
model.list.travelStatus = ko.observableArray([]);
model.list.travelType = ko.observableArray([]);


model.map.purpose = {};
model.map.employee = {};
model.map.transportation = {};
model.map.transportationByID = {};
model.map.travelStatus = {};
model.map.travelType = {};


model.is.overseas.subscribe(function (val) {
    let self = model;
    if (!val) {
        self.data.Travel().NeedPassportExtension(val);
        self.data.Travel().NeedVisaExtension(val);
    }
});

model.get.employee = async function () {
    let response = await ajax("/ESS/Travel/GetEmployees", "GET");
    if (response.StatusCode == 200) {
        return response.Data || [];
    }
    return [];
};

model.get.transportation = async function () {
    let response = await ajax("/ESS/Travel/GetTransportations", "GET");
    //let response = JSON.parse('{"StatusCode":200,"Message":null,"Data":[{"AXID":5637144576,"TransportationID":"K0001","Description":"Pesawat"},{"AXID":5637144577,"TransportationID":"K0002","Description":"Kereta Api"},{"AXID":5637144578,"TransportationID":"K0003","Description":"Bis"},{"AXID":5637144579,"TransportationID":"K0004","Description":"Kapal"}],"Total":0}')
    if (response.StatusCode == 200) {
        return response.Data || [];
    }
    return [];
};

model.get.purpose = async function () {
    let response = await ajax("/ESS/Travel/GetPurposes", "GET");
    //let response = JSON.parse('{"StatusCode":200,"Message":null,"Data":[{"AXID":5637144576,"PurposeID":"TD001","Description":"Training","IsOverseas":false},{"AXID":5637144577,"PurposeID":"TD002","Description":"Benchmark","IsOverseas":false}],"Total":0}')
    if (response.StatusCode == 200) {
        return response.Data || [];
    }

    console.error(response.StatusCode, ":", response.Message)
    return [];
};

//Method get medical type
model.get.travelrequeststatus = async function () {
    let statuses = model.proto.TravelReqStatus;
    let options = [];
    for (var i in statuses) {
        options.push({
            "value": i,
            "text": camelToTitle(statuses[i]),
        })
    }
    return options;
};

//Method get travel type
model.get.travelrequesttype = async function () {
    let types = model.proto.TravelType;
    let options = [];
    for (var i in types) {
        options.push({
            "value": i,
            "text": camelToTitle(types[i]),
        })
    }
    return options;
}

//model.render.dropdownDirector = function () {
//    $("#EmployeeID").kendoDropDownList({
//        dataSource: model.data.Director(),
//        dataTextField: "Name",
//        dataValueField: "Id",
//    });
//}

//model.render.dropdownNIP = function () {
//    let $el = $("#EmployeeID");
//    //if (!!$el) {
//        $el.kendoDropDownList({
//            dataSource: model.data.Organizations(),
//            dataTextField: "Name",
//            dataValueField: "Id",
//        });
//    //}
//}

//model.render.dropdownTypeAdmin = function () {
//    let $el = $("#TypeInput");
//    let $dEmp = $("#divEmployeeId");
//    if (!!$el) {
//        let $grid = $el.getKendoGrid();
//        if (!!$grid) {
//            $grid.destroy();
//        }
//        $el.kendoDropDownList({
//            dataSource: model.list.InputTypeAdmin(),
//            change: function (e) {
//                if (this.value() == "Director") {
//                    model.render.dropdownDirector();
//                } else {
//                    $dEmp.html("<input id='EmployeeID' class='form-control w-md-xs-100 mb-md-xs-2 w-100' data-bind='value: EmployeeID' />");
//                }
//            }
//        });
//        model.render.dropdownDirector();
//    }
//}

//model.render.dropdownTypeManager = function () {
//    let $el = $("#TypeInput");
//    let $dEmp = $("#divEmployeeId");
//    let $emp = $("#EmployeeID");
//    if (!!$el) {
//        let $grid = $el.getKendoGrid();
//        if (!!$grid) {
//            $grid.destroy();
//        }
//        $el.kendoDropDownList({
//            dataSource: model.list.InputTypeManager(),
//            change: function (e) {
//                if (this.value() == "Outsource") {
//                    $emp.attr("readonly", false);
//                    model.render.dropdownNIP();
//                } else {
//                    $dEmp.html("<input id='EmployeeID' value=" + model.app.config.employeeID + " class='form-control w-md-xs-100 mb-md-xs-2 w-100' data-bind='value: EmployeeID' />");
//                    $emp.attr("readonly", true);
//                }
//            }
//        });
//        $emp.attr("readonly", false);
//        model.render.dropdownNIP();
//    }
//}

model.render.gridTravel = function () {
    let self = model;
    let $el = $("#gridTravel");
    let dStart = model.data.StartDate();
    let dEnd = model.data.EndDate();

    model.data.StartDate(dStart);
    model.data.EndDate(dEnd);

    if ($el) {
        let $grid = $el.getKendoGrid();
        if (!!$grid) {
            $grid.destroy();
        }
        $el.kendoGrid({
            dataSource: {
                transport: {
                    read: {
                        url: "/ESS/Travel/Get",
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
                        data.Status = model.data.travelStatus() || -1;
                        return JSON.stringify(data);
                    }
                },
                schema: {
                    data: function (res) {

                        if (res.StatusCode !== 200 && res.Status !== '') {
                            swalFatal("Fatal Error", `Error occured while fetching travel request(s)\n${res.Message}`)
                            return []
                        }
                        return res.Data || [];
                    },
                    total: "Total",
                },
                error: function (e) {
                    swalFatal("Fatal Error", `Error occured while fetching travel request(s)\n${e.xhr.responseText}`)
                },
                sort: { field: "TravelID", dir: "asc" },
            },
            pageable: {
                previousNext: false,
                info: false,
                numeric: false,
                refresh: true
            },
            noRecords: {
                template: "No travel request data available."
            },
            columns: [
                {
                    attributes: {
                        "class": "text-center",
                    },
                    template: function (d) {
                        let isTravelApproved = (d.TravelRequestStatus == 3) && (!!d.SPPD && d.SPPD.length > 0 && d.SPPD[0].Status == 2);
                        if (d.Status > 2 || !isTravelApproved) return "";
                        if (new Date(d.Schedule.Finish) >= today) return "";
                        return `<button class="btn btn-xs ${(isTravelApproved) ? "btn-outline-danger" : "btn-outline-secondary"}" onclick="model.action.closeTravel('${d.uid}'); return false;" ${(isTravelApproved) ? "" : "disabled"}>
                                <i class="fa mdi mdi-close-circle-outline"></i>
                            </button>`;

                    },
                    width: 50,
                },
                {
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
                    field: "TravelID",
                    title: "#",
                    width: 150,
                    template: function (d) {
                        return d.TravelID;
                    }
                },
                {
                    title: "Schedule", width: 200,
                    template: function (d) {
                        let strStartDate = standarizeDate(d.Schedule.Start);
                        let strFinishDate = standarizeDate(d.Schedule.Finish);
                        let strStartDateTime = standarizeDateTime(d.Schedule.Start);
                        let strFinishDateTime = standarizeDateTime(d.Schedule.Finish);
                        let strSchedule = "";
                        if (strStartDate == strFinishDate) {
                            var strStartTime = standarizeTime(d.Schedule.Start);
                            var strFinishTime = standarizeTime(d.Schedule.Finish);

                            if (strStartDateTime == strFinishDateTime) {
                                strSchedule = `${strStartDate} at ${strStartTime}`;
                            }
                            else {
                                strSchedule = `${strStartDate} at ${strStartTime} - ${strFinishTime}`;
                            }
                        }
                        else {
                            strSchedule = `${strStartDateTime} - ${strFinishDateTime}`;
                        }
                        return strSchedule;
                    }
                },
                {
                    field: "Origin",
                    title: "Origin",
                    template: function (d) {
                        return d.Origin;
                    }
                },
                {
                    field: "Destination",
                    title: "Destination",
                    template: function (d) {
                        return d.Destination;
                    }
                },
                {
                    field: "TravelPurpose",
                    title: "Purpose",
                    template: function (d) {
                        return (self.map.purpose[d.TravelPurpose]) ? self.map.purpose[d.TravelPurpose].Description : "-";
                    }
                },
                {
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
                    field: "TravelRequestStatus",
                    title: "Status",
                    template: function (d) {
                        let statusClass = "secondary";
                        let statusLabel = (self.map.travelStatus[d.TravelRequestStatus]) ? self.map.travelStatus[d.TravelRequestStatus] : "-";
                        switch (d.TravelRequestStatus) {
                            case "3":
                            case 3:
                                statusClass = "info";
                                if (!!d.AXRequestID) {
                                    statusClass = "success";
                                }
                                break;
                            case "4":
                            case 4:
                                statusClass = "primary";
                                break;
                            case "2":
                            case 2:
                                statusClass = "warning";
                                break;
                            case "1":
                            case 1:
                                statusClass = "danger";
                                break;
                            default:
                                break;
                        }

                        if (!!d.AXRequestID) {
                            return `<a href="#" onclick="model.app.action.trackTask('${d.AXRequestID}'); return false;"><span class="badge badge-${statusClass}">${statusLabel.toLowerCase()}</span></a>`;
                        }
                        return `<span class="badge badge-${statusClass}">${statusLabel.toLowerCase()}</span>`;

                    }
                },
                {
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
                    field: "SPPD",
                    title: "SPPD",
                    template: function (data) {
                        let d = data._raw();
                        let Status = (Array.isArray(d.SPPD) && d.SPPD.length > 0) ? d.SPPD[0].Status : 0;
                        let statusClass = "secondary";
                        let statusLabel = "created";


                        switch (Status) {
                            case "1":
                            case 1:
                                statusClass = "danger";
                                statusLabel = "rejected";
                                break;
                            case "2":
                            case 2:
                                statusClass = "success";
                                statusLabel = "approved";
                                break;
                            case "3":
                            case 3:
                                statusClass = "warning";
                                statusLabel = "approval";
                                break;
                            default:
                                break;
                        }

                        if (!!d.AXRequestID) {
                            return `
                        <a href="#" onclick="model.app.action.trackTask('${d.AXRequestID}'); return false;" style="${(!d.AXRequestID) ? "pointer=event:none;" : ""}">
                            <span class="badge badge-${statusClass}">${statusLabel.toLowerCase()}</span>
                        </a>`;
                        }

                        return `<span class="badge badge-${statusClass}">${statusLabel.toLowerCase()}</span>`;
                    }
                },
                {
                    template: function (data) {



                        var d = data._raw();
                        if (d.TravelID === "TR-00102") {
                            console.log("TravelStatus", d.TravelRequestStatus);
                            console.log("IsAttachmentExist", d.SPPD[0])
                        }
                        let statusSPPD = (Array.isArray(d.SPPD) && d.SPPD.length > 0) ? d.SPPD[0].Status : 0;
                        let isSPPDApproved = (!!d.SPPD && d.SPPD.length > 0 && d.SPPD[0].Status == 2);
                        let isTravelApproved = (d.TravelRequestStatus == 3 && d.SPPD.length > 0 && d.SPPD[0].IsAttachmentExist == true);

                        let btnSPPD = `<button class="btn btn-xs ${(isSPPDApproved) ? "btn-outline-success" : "btn-outline-secondary"}" onclick="model.action.OpenSPPD('${data.uid}'); return false;" title="SPPD" ${(isSPPDApproved) ? "" : "disabled"}>
                                <i class="mdi mdi-file-document-outline"></i>
                            </button>&nbsp;`;

                        let btnSPPDWithCheck = `<button class="btn btn-xs btn-outline-success" onclick="model.action.OpenSPPDWithInstanceID('${data.uid}'); return false;" title="SPPD" ${(isSPPDApproved) ? "" : "disabled"}>
                                <i class="mdi mdi-file-document-outline"></i>
                            </button>
                            &nbsp;` ;

                        return btnSPPD + `<button class="btn btn-xs ${(isTravelApproved && statusSPPD != 1) ? "btn-outline-primary" : "btn-outline-secondary"}" onclick="model.action.OpenItenary('${data.uid}'); return false;" title="Itenary" ${(isTravelApproved) ? "" : "disabled"}>
                                <i class="mdi mdi-ticket"></i>
                            </button>
                        `;
                    },
                    width: 90,
                },
                {
                    template: function (d) {
                        if (d.TravelRequestStatus == 1) {
                            return `
                            <button class="btn btn-xs btn-outline-info" onclick="model.action.ModalRevisionTravel('${d.uid}'); return false;">
                                    <i class="fa mdi mdi-pencil"></i>
                            </button>
                            `;
                        }
                        return `
                            <button class="btn btn-xs btn-outline-info" onclick="model.action.ModalDetailTravel('${d.uid}'); return false;">
                                    <i class="fa mdi mdi-eye"></i>
                            </button>
                        `;

                    },
                    width: 50,
                },
            ]
        });
    }
}

model.action.refreshTravel = function (uiOnly = false) {

    if (model.data.StartDate() > model.data.EndDate()) {
        swalAlert("Travel", "start date is greater than the end date");
        return false;
    };

    var $grid = $("#gridTravel").data("kendoGrid");
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

model.action.filterTravelMonthly = function () {
    model.data.StartDate(moment().startOf('month').format("MM/DD/YYYY"));
    model.data.EndDate(today);
    var $grid = $("#gridTravel").data("kendoGrid");
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

model.action.filterTravelYearly = function () {
    model.data.StartDate(moment().startOf('year').format("MM/DD/YYYY"));
    model.data.EndDate(today);
    var $grid = $("#gridTravel").data("kendoGrid");
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

model.action.openFormTravel = function () {
    let self = model;
    model.data.Filename("");
    self.data.Travel(model.newTravel());
    model.is.overseas(false);
    $("#modalFormTravel").modal("show");
}

model.action.ModalDetailTravel = function (uid) {
    dataGrid = $("#gridTravel").data("kendoGrid").dataSource.getByUid(uid);
    if (dataGrid) {
        dataGrid = dataGrid._raw();
        model.data.Travel(model.newTravel(dataGrid));
        $("#ModalDetailTravel").modal("show");
    }
}

let travelReadonlyFile;
model.action.ModalRevisionTravel = function (uid) {
    dataGrid = $("#gridTravel").data("kendoGrid").dataSource.getByUid(uid);
    if (dataGrid) {
        dataGrid = dataGrid._raw();

        model.data.Travel(model.newTravel(dataGrid));

        if (!travelReadonlyFile)
            travelReadonlyFile = kendo.template($("#fileTemplateReadonly").html());

        if (!!dataGrid.Filename)
            model.data.Travel().HTMLFile(travelReadonlyFile(dataGrid));
        else
            model.data.Travel().HTMLFile("");

        $("#modalFormTravel").modal("show");
    }
}

model.action.request = async function () {
    var result = await swalConfirm('Travel', 'Are you sure to request travel?');
    if (result.value) {
        try {
            isLoading(true);
            let self = model;
            let dialogTitle = "Travel Request";
            let data = ko.mapping.toJS(model.data.Travel());

            if (data.Schedule.Start == null) {
                isLoading(false)
                swalAlert(dialogTitle, 'Start date is required.');
                return;
            }

            if (data.Schedule.Finish == null) {
                isLoading(false)
                swalAlert(dialogTitle, 'End date is required.');
                return;
            }

            if (new Date(data.Schedule.Finish) < new Date(data.Schedule.Start)) {
                isLoading(false)
                swalAlert(dialogTitle, 'Start date is bigger than Finish date.');
                return;
            }

            if (!(data.Origin || "").trim()) {
                isLoading(false)
                swalAlert(dialogTitle, 'Origin is required.');
                return;
            }

            if (!(data.Destination || "").trim()) {
                isLoading(false)
                swalAlert(dialogTitle, 'Destination is required.');
                return;
            }

            if (!(_.toString(data.TravelType) || "").trim() && data.TravelType !== 0) {
                isLoading(false)
                swalAlert(dialogTitle, 'Travel type is required.');
                return;
            }

            if (!(_.toString(data.Transportation) || "").trim()) {
                isLoading(false)
                swalAlert(dialogTitle, 'Transportation type is required.');
                return;
            }

            if ((!data.TravelPurpose && data.TravelPurpose !== 0) || data.TravelPurpose.AXID == 0) {
                isLoading(false)
                swalAlert(dialogTitle, 'Purpose is required.');
                return;
            }

            if (data.Intention == '1') {
                if (!(data.RequestForID || "").trim()) {
                    isLoading(false)
                    swalAlert(dialogTitle, 'Requester information is required.');
                    return;
                }

                if (data.RequestForID.toLowerCase() == "guest" && !(data.RequestForName || "").trim()) {
                    isLoading(false)
                    swalAlert(dialogTitle, 'Requester name is required');
                    return;
                }
            }

            let formData = new FormData();
            formData.append("JsonData", JSON.stringify(data));

            var files = $('#Filepath').getKendoUpload().getFiles();

            if (files.length > 0) {
                formData.append("FileUpload", files[0].rawFile);
            } else {
                if (data.TravelRequestStatus != 1) {
                    isLoading(false)
                    swalAlert("Travel", "Document attachment could not be empty");
                    return;
                }
            }

            try {
                $("#modalFormTravel").modal("hide");
                if (data.TravelRequestStatus == 1) {
                    ajaxPostUpload("/ESS/Travel/Revise", formData, function (data) {
                        isLoading(false);
                        if (data.StatusCode == 200) {
                            swalSuccess("Travel Request Revision", data.Message);
                            $("#modalFormTravel").modal("hide");
                            model.action.refreshTravel();
                        } else {
                            swalFatal("Travel Request Revision", data.Message);
                        }
                    }, function (data) {
                        isLoading(false);
                        swalFatal("Travel Request Revision", data.Message);
                    })

                } else {
                    ajaxPostUpload("/ESS/Travel/Request", formData, function (data) {
                        isLoading(false);
                        if (data.StatusCode == 200) {
                            swalSuccess("Request Travel", data.Message);
                            $("#modalFormTravel").modal("hide");
                            model.action.refreshTravel();
                        } else {
                            swalFatal("Request Travel", data.Message);
                        }
                    }, function (data) {
                        isLoading(false);
                        swalFatal("Request Travel", data.Message);
                    })

                }

            } catch (e) {
                isLoading(false);
                console.error(e);
            }
        } catch (e) {
            isLoading(false);
            console.error(e);
        }

        return false;

    }

}

/*
model.action.CloseTravelRequest = async function (uid) {
    var dialogTitle = "Discard request travel";
    dataGrid = $("#gridMyTimeAttendance").data("kendoGrid").dataSource.getByUid(uid);
    var result = await swalConfirm(dialogTitle, `Are you sure close request travel : "${dataGrid.UpdateRequest.Reason}" ?`);

    
    if (result.value) {
        var Id = (dataGrid.UpdateRequest) ? dataGrid.UpdateRequest.Id : "";
        if (Id) {
            isLoading(true)
            ajaxPost("/ESS/Travel/Close/" + Id, {}, function (data) {
                isLoading(false)
                if (data.StatusCode == 200) {
                    model.action.refreshTravel();
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
*/

model.action.OpenSPPD = function (uid) {
    dataGrid = $("#gridTravel").data("kendoGrid").dataSource.getByUid(uid);
    if (!!dataGrid && !!dataGrid.SPPD && dataGrid.SPPD.length > 0) {
        dataGrid = dataGrid._raw();
        model.data.Travel(model.newTravel(dataGrid));
        model.data.SPPD(model.newSPPD(dataGrid.SPPD[0]));

        var td = [];
        model.data.SPPD().TransportationDetails().forEach(function (elm) {
            td.push(elm.TransportationID())
        });
        var transportationdetail = td.reduce((unique, item) => {
            return unique.includes(item) ? unique : [...unique, item]
        }, []);
        model.data.TransportationID(transportationdetail)

        let total = 0;
        let SPPD = dataGrid.SPPD[0];
        for (var i in SPPD) {
            if (["accommodation", "fuel", "laundry", "parking", "rent", "ticket", "highway", "airporttransportation", "localtransportation", "mealallowance", "pocketmoney"].indexOf(i.toLowerCase()) > -1) {
                total += SPPD[i];
            }
        }
        model.data.Total(total);
        $("#modalSPPD").modal("show");
        return;
    }
    swalAlert("Travel Request", 'Travel request has no SPPD');
}

model.action.OpenSPPDWithInstanceID = async function (uid) {
    dataGrid = $("#gridTravel").data("kendoGrid").dataSource.getByUid(uid);
    if (!!dataGrid && !!dataGrid.SPPD && dataGrid.SPPD.length > 0) {
        dataGrid = dataGrid._raw();
        let InstanceID = (typeof dataGrid == "object") ? dataGrid.AXRequestID : dataGrid;
        isLoading(true);
        var UpdateRequest = await model.get.updateRequestByInstanceID(InstanceID);
        isLoading(false);
        let CountWorkFlowApproved = 0
        UpdateRequest.WorkFlows.forEach(function (elm) {
            if (elm.StepTrackingType === 2) {
                CountWorkFlowApproved++
            }
        })

        if (CountWorkFlowApproved > 0) {

            model.data.Travel(model.newTravel(dataGrid));
            model.data.SPPD(model.newSPPD(dataGrid.SPPD[0]));

            var td = [];
            model.data.SPPD().TransportationDetails().forEach(function (elm) {
                td.push(elm.TransportationID())
            });
            var transportationdetail = td.reduce((unique, item) => {
                return unique.includes(item) ? unique : [...unique, item]
            }, []);
            model.data.TransportationID(transportationdetail)

            let total = 0;
            let SPPD = dataGrid.SPPD[0];
            for (var i in SPPD) {
                if (["accommodation", "fuel", "laundry", "parking", "rent", "ticket", "highway", "airporttransportation", "localtransportation", "mealallowance", "pocketmoney"].indexOf(i.toLowerCase()) > -1) {
                    total += SPPD[i];
                }
            }
            model.data.Total(total);
            $("#modalSPPD").modal("show");
            return;

        }

    }
    swalAlert("Travel Request", 'Travel request has no SPPD');
}

model.get.updateRequestByInstanceID = async function (instanceID = "") {
    let response = await ajax(`/ESS/UpdateRequest/GetByInstanceID/${instanceID}`, "GET");
    if (response.StatusCode == 200) {
        return response.Data || null;
    }
    return [];
};



model.action.OpenItenary = function (uid) {
    dataGrid = $("#gridTravel").data("kendoGrid").dataSource.getByUid(uid);
    if (!!dataGrid && !!dataGrid.SPPD && dataGrid.SPPD.length > 0) {
        dataGrid = dataGrid._raw();
        model.data.Travel(model.newTravel(dataGrid))
        model.data.SPPD(model.newSPPD(dataGrid.SPPD[0]));

        $("#modalItenary").modal("show");
        return;
    }
    swalAlert("Travel Request", 'Travel request has no SPPD');
}

model.action.OpenDetail = function (uid) {
    dataGrid = $("#gridTravel").data("kendoGrid").dataSource.getByUid(uid);
    if (!!dataGrid) {
        dataGrid = dataGrid._raw()
        model.data.Travel(model.newTravel(dataGrid));
        $("#modalFormTravel").modal("show");
        return;
    }
    swalAlert("Travel Request", 'Unable to find travel with uid : ' + uid);
}

model.action.closeTravel = async function (uid) {
    let dialogTitle = "Travel";
    dataGrid = $("#gridTravel").data("kendoGrid").dataSource.getByUid(uid);
    dataGrid = dataGrid._raw();
    var id = dataGrid.TravelID;
    if (!!dataGrid && !!id) {
        var result = await swalConfirm(dialogTitle, `Are you sure closing travel "${id}" ?`);
        if (result.value) {
            isLoading(true)
            ajaxPost("/ESS/Travel/Close/" + id, {}, function (data) {
                isLoading(false)
                if (data.StatusCode == 200) {
                    model.action.refreshTravel();
                    swalSuccess(dialogTitle, data.Message);
                } else {
                    swalFatal(dialogTitle, data.Message);
                }
            }, function (data) {
                isLoading(false)
                swalFatal(dialogTitle, data.Message);
            });
        }

        return;
    }
    swalAlert("Travel Request", 'Unable to find travel with uid : ' + uid);
}

model.action.printSPPD = function () {
    isLoading(true);
    $("#SPPDPdf").removeClass("lang-en").addClass("lang-id");
    $("#nomorSPPD").html("                                                         ");
    kendo.drawing.drawDOM($("#SPPDPdf"), {
        scale: 0.65,
        paperSize: "A4",
        landscape: false,
        margin: { left: "0.5cm", top: "3.8cm", right: "0.5cm", bottom: "0.5cm" },
        template: $("#page-template").html()
    }).then(function (group) {
        kendo.drawing.pdf.saveAs(group, "SPPD.pdf");
        $("#SPPDPdf").removeClass("lang-id").addClass("lang-en");
        isLoading(false);
    });
}

model.action.downloadAttachment = function ($data) {
    var data = ko.mapping.toJS($data);
    var travel = ko.mapping.toJS(model.data.Travel);

    if (!!data && !!data.Filename && !!travel && !!travel.TravelID) {
        window.open(`/ESS/Travel/DownloadAttachment/${travel.TravelID}/${data.AXID}/${data.Filename}`);
    }
}

let travelDocumentTemp = "";
model.on.travelDocumentSelect = function (e) {
    let self = model;
    var valid = self.app.data.validateKendoUpload(e, "Travel");
    if (valid) {
        travelDocumentTemp = self.data.Travel().HTMLFile();
        self.data.Travel().HTMLFile("");
    } else {
        e.preventDefault();
    }
}

model.on.travelDocumentRemove = function (e) {
    let self = model;
    self.data.Travel().HTMLFile(travelDocumentTemp);
}

model.on.requestForChange = function (e) {
    let self = model;
    let value = this.value();
    if (value == "GUEST") {
        self.data.Travel().IsGuest(true);
        self.data.Travel().RequestForName("");
    } else {
        var employee = self.map.employee[value] || {};
        self.data.Travel().IsGuest(false);
        self.data.Travel().RequestForName(employee.EmployeeName);
    }
};

model.on.purposeChange = function (e) {
    let self = model;
    let value = this.value();
    let purpose = self.map.purpose[value];

    self.is.overseas(purpose ? purpose.IsOverseas : false);
};

model.on.intentionChange = function (e) {
    let self = model;
    let value = this.value();
    let data = self.proto.RequestFor[value];
    self.data.Travel().IntentionDescription(data);
};

model.init.Travel = function () {
    var self = model;

    setTimeout(async function () {
        var data = self.proto.RequestFor || [];
        data.forEach((d, i) => {
            self.list.requestFor.push({
                text: d,
                value: i,
            });
        });

    });

    setTimeout(async function () {
        var data = await self.get.transportation();
        data.forEach((d, i) => {
            self.map.transportation[d.TransportationID] = d.Description;
            self.map.transportationByID[d.AXID] = d.Description;
        });
        self.list.transportation(data);
    });

    setTimeout(async function () {
        var data = await self.get.purpose();
        self.list.purpose(data);

        data.forEach(d => {
            self.map.purpose[d.AXID] = d;
        });
    });

    setTimeout(async function () {
        var data = await self.get.travelrequeststatus();

        data.forEach((d, i) => {
            model.map.travelStatus[d.value] = d.text;
        });
        model.list.travelStatus(data);
    });

    setTimeout(async function () {
        var data = await self.get.travelrequesttype();

        data.forEach((d, i) => {
            model.map.travelType[d.value] = d.text;
        });
        model.list.travelType(data);
    });

    setTimeout(async function () {
        self.action.refreshTravel(true);
        model.render.gridTravel();
    });


}
