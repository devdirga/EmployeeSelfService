model.data.groupedNotifications = ko.observableArray();
model.data.totalNotification = ko.observable(0);
model.data.pageNotification = ko.observable(1);
model.data.counterNotification = ko.observable(0);
model.data.currentShownNotification = ko.observable(0);
model.data.filterNotification = ko.observable("all");
model.data.holiday = ko.observableArray([]);
model.data.enableFilter = ko.observable(false);
//updaterequest
model.data.updateRequest = ko.observableArray([]);
model.data.totalUpdateRequest = ko.observable(0);
model.data.pageUpdateRequest = ko.observable(1);
model.data.counterUpdateRequest = ko.observable(0);
//task
model.data.groupedTasks = ko.observableArray();
model.data.tasks = ko.observableArray([]);
model.data.totalTask = ko.observable(0);
model.data.pageTask = ko.observable(1);
model.data.counterTask = ko.observable(0);
model.data.currentShownTask = ko.observable(0);
model.data.delegateParamID = ko.observable("");
model.data.delegation = ko.observable();
model.data.filterTask = ko.observable("all");
model.data.activeTaskTab = ko.observable("active")
model.data.TransportationID = ko.observableArray([]);
model.data.medicalBenefitDetails = ko.observableArray([]);
model.data.medicalBenefit = ko.observable();

//list
model.list.gender = ko.observableArray([]);
model.list.maritalStatus = ko.observableArray([]);
model.list.familyRelationship = ko.observableArray([]);

//map
model.map.familyRelationship = {};
model.map.purpose = {};
model.map.transportation = {};
model.map.medicalType = {};
model.map.documentType = {};
model.map.TicketType = {}
model.map.TicketStatus = {}
model.map.TicketMedia = {}
model.map.TicketStatusLabelCss = {}
model.map.TicketCategories = {}
model.map.StatusLabelCSS = {
    0:'info',
    1:'success',
    2:'warning',
    3:'danger',
};
model.map.ticketStatusCSS = {
    0: "dark",
    1: "info",
    2: "danger",
    3: "primary",
}

model.is.delegationEnabled = ko.observable(false);
model.is.filterVisible = ko.observable(false);

var daysAgo = new Date();
daysAgo.setDate(daysAgo.getDate() - 7);
model.data.StartDate = ko.observable(daysAgo);

var today = new Date();
model.data.FinishDate = ko.observable(today);

model.newParamTask = function (AXID, instanceID, originatorEmployeeID, delegateToEmployeeID, notes) {
    return Object.assign(_.clone(model.proto.ParamTask), {
        AXID: AXID,
        DelegateToEmployeeID: delegateToEmployeeID,
        InstanceID: instanceID,
        Notes: notes,
        OriginatorEmployeeID: originatorEmployeeID,
    });
}

model.newLeave = function (data) {
    if (data) {
        return ko.mapping.fromJS(Object.assign(_.clone(this.proto.Leave), _.clone(data)));
    }
    return ko.mapping.fromJS(this.proto.Leave);
};

model.newEmployee = function (obj) {
    var employeeProto = _.clone(this.proto.Employee);

    if (obj && typeof obj == "object") {
        employeeProto = Object.assign(_.clone(employeeProto), obj);
    }

    if (!employeeProto.Address) employeeProto.Address = _.clone(this.proto.Address);
    if (!employeeProto.MaritalStatusAttachment) employeeProto.MaritalStatusAttachment = _.clone(this.proto.FieldAttachment);
    if (!employeeProto.IsExpartriateAttachment) employeeProto.IsExpartriateAttachment = _.clone(this.proto.FieldAttachment);
    
    return ko.mapping.fromJS(employeeProto);
};

model.newAddress  = function (obj) {
    if (obj && typeof obj == "object") {
        return ko.mapping.fromJS(Object.assign(_.clone(this.proto.Address), obj));
    }
    return ko.mapping.fromJS(this.proto.Address);
};

model.newFamily = function (obj) {
    var proto = _.clone(this.proto.Family);
    if (!proto) return null;
    if (obj && typeof obj == "object") {
        proto = Object.assign(_.clone(proto), obj);        
    }

    proto.Old = proto.Old || _.clone(this.proto.Family);

    if (proto.Relationship) {
        proto.Relationship = model.map.familyRelationship[proto.Relationship] || proto.Relationship;
    }

    if (proto.Old.Relationship) {
        proto.Old.Relationship = model.map.familyRelationship[proto.Old.Relationship] || proto.Old.Relationship;
    }

    return ko.mapping.fromJS(proto);
};

model.newCertificate = function (obj) {
    var proto = _.clone(this.proto.Certificate);
    if (!proto) return null;
    proto.Validity = _.clone(this.proto.DateRange);

    if (obj && typeof obj == "object") {
        proto = Object.assign(_.clone(proto), obj);
    }
    proto.Old = proto.Old || _.clone(this.proto.Certificate);
    proto.Old.Validity = proto.Old.Validity || _.clone(this.proto.DateRange);
    return ko.mapping.fromJS(proto);
};


model.newDocumentRequest = function (obj) {
    var proto = _.clone(Object.assign(this.proto.DocumentRequest, (obj || {})));
    if (!proto) return null;    
    return ko.mapping.fromJS(proto);
};

model.newFieldAttachment = function (obj) {
    var proto = _.clone(this.proto.FieldAttachment);
    if (obj && typeof obj == "object") {
        proto = _.clone(Object.assign(proto, obj));
    }
    return ko.mapping.fromJS(proto);
};

model.newTravel = function (obj) {
    let travel = _.clone(this.proto.Travel);
    if (travel) {

        if (!travel.Schedule) {
            travel.Schedule = _.clone(this.proto.DateRange || {});
            travel.Schedule.Start = new Date();
            travel.Schedule.Finish = new Date();
        }

        if (!travel.TravelPurpose) {
            travel.TravelPurpose = _.clone(this.proto.TravelPurpose || {});
        }

        if (!travel.Transportation) {
            travel.Transportation = _.clone(this.proto.Transportation || {});
        }

    }

    if (obj && typeof obj == "object") {
        return (Object.assign(travel, obj));
    }

    return (travel);
};

model.newSPPD = function (obj) {
    let sppd = _.clone(this.proto.SPPD);
    if (sppd) {
        sppd.Attachments = sppd.Attachments || [];
        sppd.TransportationDetails = sppd.TransportationDetails || [];
    }   

    if (obj && typeof obj == "object") {
        return (Object.assign(sppd, obj));
    }

    return (sppd);
};

model.newMedicalBenefit= function (obj) {
    var proto = {};
    if (!obj) {
        obj = _.clone(this.proto.MedicalBenefit);
    }
    proto = Object.assign({}, obj);
    if (!proto.Family) {
        proto.Family = _.clone(this.proto.Family);
    }
    if (proto.RequestDate) {
        proto.RequestDate = moment().format("DD MMM YYYY");
    }

    proto.EmployeeID = model.app.config.employeeID;
    return ko.mapping.fromJS(proto);
};

model.newMedicalBenefitDetail = function (obj) {
    var proto = _.clone(this.proto.MedicalBenefitDetail);
    let o = {};

    o = Object.assign(proto, _.clone(obj || {}));

    o.isUploading = false;
    o.isNoteExists = o.isNoteExists || !!((o.Description || "").trim());
    o.guid = o.guid || kendo.guid();

    return ko.mapping.fromJS(proto);
}

model.newRecruitment = function(obj){
    let proto = _.clone(this.proto.Recruitment || {});
    
    if (typeof obj != "object") {
        obj = {};        
    }

    var data = Object.assign(proto, obj);
    data.EstimationStartedDate = data.EstimationStartedDate || new Date();
    return ko.mapping.fromJS(data);
};

model.newRetirement = function (obj) {
    let proto = _.clone(this.proto.Retirement || {});

    if (typeof obj != "object") {
        obj = {};
    }


    if (!obj.MPPDate) {
        obj.MPPDate = _.clone(this.proto.DateRange || {});
        obj.MPPDate.Start = _DEFAULT_DATE;
        obj.MPPDate.Finish = _DEFAULT_DATE;
    }

    if (!obj.CBDate) {
        obj.CBDate = _.clone(this.proto.DateRange || {});
        obj.CBDate.Start = _DEFAULT_DATE;
        obj.CBDate.Finish = _DEFAULT_DATE;
    }

    return ko.mapping.fromJS(Object.assign(proto, obj));
};

model.newTicket = function (obj) {
    let Ticket = _.clone(this.proto.Ticket || {});
    if (obj && typeof obj == "object") {
        Ticket = Object.assign(Ticket, _.clone(obj));
    }
    Ticket.Category = Ticket.Category || _.clone(this.proto.TicketCategory || {});
    Ticket.EmailTo = Ticket.EmailTo || [];

    if (!!Ticket.EmailCC) {
        Ticket.EmailCC = JSON.parse(Ticket.EmailCC)
        Ticket.EmailCC = Ticket.EmailCC.join(", ")
    }

    if (!!Ticket.EmailTo) {       
        Ticket.EmailTo = Ticket.EmailTo.join(", ")
    }

    return ko.mapping.fromJS(Ticket);
}


//detail
model.detail = {};
model.detail.resume = ko.observable(model.newEmployee());
model.detail.certificate = ko.observable(model.newCertificate());
model.detail.documentRequest = ko.observable(model.newDocumentRequest());
model.detail.family = ko.observable(model.newFamily());
model.detail.timeAttendance = ko.observable({});
model.detail.leave = ko.observable(model.newLeave());
model.detail.Travel = ko.observable(model.newTravel());
model.detail.SPPD = ko.observable(model.newSPPD());
model.detail.TravelTotal = ko.observable(0);
model.detail.Recruitment = ko.observable(model.newRecruitment());
model.detail.Retirement = ko.observable(model.newRetirement());
model.data.Ticket = ko.observable(model.newTicket());

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

model.data.preProcessNotification = function (data) {
    let self = model;
    if (!data) {
        data = ko.mapping.toJS(self.app.notifications());
    }

    let todayDate = standarizeDate(new Date());
    let yesterdayDate = standarizeDate(moment().subtract(1, "days").toDate());

    data.forEach(d => {
        d.Date = standarizeDate(d.Timestamp);
        d.DateTime = standarizeDateTime(d.Timestamp);
        d.HumanizedDateTime = relativeDate(d.Timestamp);

        if (todayDate == d.Date) {
            d.Day = "Today";
        } else if (yesterdayDate == d.Date) {
            d.Day = "Yesterday";
        } else {
            d.Day = moment(d.Timestamp).format("dddd");
        }
    })

    let dateMap = {};
    self.data.groupedNotifications([]);
    data.forEach(d => {
        let dm = dateMap[d.Date];
        if (typeof dm == "undefined") {
            self.data.groupedNotifications.push(ko.mapping.fromJS({
                Day: d.Day,
                Date: d.Date,
                Timestamp: d.Timestamp,
                Data: [d]
            }));
            dateMap[d.Date] = self.data.groupedNotifications().length - 1;
        } else {
            self.data.groupedNotifications()[dm].Data.push(ko.mapping.fromJS(d))
        }
    });
    return ko.mapping.toJS(self.data.groupedNotifications());
};

model.data.preProcessUpdateRequest = function (data) {
    let self = model;
    self.data.updateRequest([]);
    for (var i in data) {
        var d = data[i];
        self.data.updateRequest.push(ko.mapping.fromJS(d));
    }
};

model.data.preProcessTask = function (data) {
    let self = model;
    if (!data) {
        data = ko.mapping.toJS(self.app.tasks());
    }

    let todayDate = standarizeDate(new Date());
    let yesterdayDate = standarizeDate(moment().subtract(1, "days").toDate());
    let taskColorIndex = {};
    let taskColorCount = {};
    let instanceIdCount = 0;
    data.forEach(d => {
        if (!taskColorCount[d.InstanceId]) taskColorCount[d.InstanceId] = 0;
        taskColorCount[d.InstanceId]++;
        taskColorIndex[d.InstanceId] = instanceIdCount++;
    });
    let colors = randomColor({
        count: instanceIdCount,
        luminosity: 'light',
    });

    data.forEach(d => {
        d.Date = standarizeDate(d.SubmitDateTime);
        d.DateTime = standarizeDateTime(d.SubmitDateTime);
        d.HumanizedDateTime = relativeDate(d.SubmitDateTime);
        
        if (taskColorCount[d.InstanceId] > 1) {
            d.ColorGroup = colors[taskColorIndex[d.InstanceId]]
        } else {
            d.ColorGroup = "";
        }
        

        if (todayDate == d.Date) {
            d.Day = "Today";
        } else if (yesterdayDate == d.Date) {
            d.Day = "Yesterday";
        } else {
            d.Day = moment(d.SubmitDateTime).format("dddd");
        }
    })
    
    let dateMap = {};
    self.data.groupedTasks([]);
    data.forEach(d => {
        let dm = dateMap[d.Date];
        d.Done = d.ActionApprove || d.ActionCancel || d.ActionDelegate || d.ActionReject || d.StepTrackingType > 1;
        if (typeof dm == "undefined") {
            self.data.groupedTasks.push(ko.mapping.fromJS({
                Day: d.Day,
                Date: d.Date,
                Timestamp: d.CreatedDate,
                Data: [d]
            }));
            dateMap[d.Date] = self.data.groupedTasks().length - 1;
        } else {
            self.data.groupedTasks()[dm].Data.push(ko.mapping.fromJS(d))
        }
    });
    return ko.mapping.toJS(self.data.groupedTasks());
};

model.get.TicketType = async function () {
    let TicketType = model.proto.TicketType
    let options = []
    for (var i in TicketType) {
        options.push({
            "value": i,
            "text": camelToTitle(TicketType[i])
        })
    }
    return options
}

model.get.TicketStatus = async function () {
    let TicketStatus = model.proto.TicketStatus
    let options = []
    for (var i in TicketStatus) {
        options.push({
            "value": i,
            "text": camelToTitle(TicketStatus[i])
        })
    }
    return options
}

model.get.TicketMedia = async function () {
    let TicketMedia = model.proto.TicketMedia
    let options = []
    for (var i in TicketMedia) {
        options.push({
            "value": i,
            "text": camelToTitle(TicketMedia[i])
        })
    }
    return options
}

model.get.TicketCategories = async function () {
    let response = await ajax("/ESS/Complaint/GetTicketCategories", "GET");
    let options = []
    if (response.StatusCode == 200) {        
        return response.Data || [];
    }
    return [];
};

model.get.TicketStatusLabelCss = async function () {
    return [{ "value": 0, "text": "info" }, { "value": 1, "text": "primary" }, { "value": 2, "text": "success" }]

}

model.get.updateRequest = async function (limit = 10, offset = 0) {
    let response = await ajax("/ESS/UpdateRequest/Get", "POST", JSON.stringify({ Limit: limit, Offset: offset}));
    
    if (response.StatusCode == 200) {
        let data = response.Data || [];
        let total = response.Total || 0;
        return {
            Data: data,
            Total: total
        };

        return result;
    }
    //console.error(response.StatusCode, ":", response.Message);
    return [];
};

model.get.updateRequestRange = async function (start, finish, limit = 10, offset = 0) {
    let response = await ajaxPost("/ESS/UpdateRequest/GetRange", { Limit: limit, Offset: offset, Range: { Start:start, Finish:finish }});
    
    if (response.StatusCode == 200) {
        let data = response.Data || [];
        let total = response.Total || 0;
        return {
            Data: data,
            Total: total
        };

        return result;
    }
    //console.error(response.StatusCode, ":", response.Message);
    return [];
};

model.get.medicalType = async function () {
    let response = await ajax("/ESS/Benefit/GetMedicalType", "GET");
    if (response.StatusCode == 200) {
        let result = response.Data || []
        return result;
    }
    return [];
};

model.get.documentType = async function () {
    let response = await ajax("/ESS/Benefit/GetDocumentType", "GET");
    if (response.StatusCode == 200) {
        let result = response.Data || []
        return result;
    }
    return [];
}


model.is.renderingNotification = ko.observable(true);
model.is.renderingUpdateRequest = ko.observable(true);
model.is.renderingTask = ko.observable(true);

model.data.filterNotification.subscribe(function (filter) {
    let self = model;
    let currentPage = self.data.pageNotification();
    let offset = (currentPage - 1) * _limitNotification;
    self.render.notificationList(_limitNotification, offset);
});

model.action.approveTask = async function ($data) {
    let dialogTitle = "Approve";
    let data = ko.mapping.toJS($data);
    confirmResult = await swalConfirm(dialogTitle, `Are you sure approving ${data.Title}?`);
    if (confirmResult.value) {
        try {
            isLoading(true);
            console.log(data);
            let param = model.newParamTask(data.AXID, data.InstanceId, data.SubmitEmployeeID, "", "");
            let response = await ajax(`/ESS/Task/Approve`, "POST", JSON.stringify(param));
            isLoading(false);
            if (response.StatusCode == 200) {
                swalSuccess(dialogTitle, response.Message);
                model.render.tasks();
                return;
            }
            swalError(dialogTitle, response.Message);
        } catch (e) {
            isLoading(false);
        }
    }

};
model.action.rejectTask = async function ($data) {
    let dialogTitle = "Reject";
    let data = ko.mapping.toJS($data);
    let confirmResult = await swalConfirmText(
        dialogTitle,
        `Are you sure rejecting ${data.Title}?`,
        `Rejection reason`
    );

    if (confirmResult.hasOwnProperty("value")) {
        let reason = confirmResult.value;
        if (!reason) {
            swalAlert(dialogTitle, "Rejection reason could not be empty");
            return;
        }

        try {
            isLoading(true);
            let param = model.newParamTask(data.AXID, data.InstanceId, data.SubmitEmployeeID, "", reason);
            let response = await ajax(`/ESS/Task/Reject/`, "POST", JSON.stringify(param));
            isLoading(false);
            if (response.StatusCode == 200) {
                swalSuccess(dialogTitle, response.Message);
                model.render.tasks();
                return;
            }
            swalError(dialogTitle, response.Message);
        } catch (e) {
            isLoading(false);
        }
    }
};

model.action.approveTaskInverted = async function ($data) {
    let dialogTitle = "Approve";
    let data = ko.mapping.toJS($data);
    confirmResult = await swalConfirm(dialogTitle, `Are you sure approving ${data.Title}?`);
    if (confirmResult.value) {
        try {
            isLoading(true);
            let param = model.newParamTask(data.AXID, data.InstanceId, data.SubmitEmployeeID, "", "");
            let response = await ajax(`/ESS/Task/ApproveInvert`, "POST", JSON.stringify(param));
            isLoading(false);
            if (response.StatusCode == 200) {
                swalSuccess(dialogTitle, response.Message);
                model.render.tasks();
                return;
            }
            swalError(dialogTitle, response.Message);
        } catch (e) {
            isLoading(false);
        }
    }

};
model.action.rejectTaskInverted = async function ($data) {
    let dialogTitle = "Reject";
    let data = ko.mapping.toJS($data);
    let confirmResult = await swalConfirmText(
        dialogTitle,
        `Are you sure rejecting ${data.Title}?`,
        `Rejection reason`
    );

    if (confirmResult.hasOwnProperty("value")) {
        let reason = confirmResult.value;
        if (!reason) {
            swalAlert(dialogTitle, "Rejection reason could not be empty");
            return;
        }

        try {
            isLoading(true);
            let param = model.newParamTask(data.AXID, data.InstanceId, data.SubmitEmployeeID, "", reason);
            let response = await ajax(`/ESS/Task/RejectInvert`, "POST", JSON.stringify(param));
            isLoading(false);
            if (response.StatusCode == 200) {
                swalSuccess(dialogTitle, response.Message);
                model.render.tasks();
                return;
            }
            swalError(dialogTitle, response.Message);
        } catch (e) {
            isLoading(false);
        }
    }
};

model.action.openTaskDelegation = function ($data) {
    var data = ko.mapping.toJS($data);
    model.data.delegateParamID(data.AXID);
    model.data.delegation(data);
    $("#modalFormDelegation").modal("show");
};

model.action.delegateTask = async function () {
    let data = ko.mapping.toJS(model.data.delegation);
    let dialogTitle = "Delegate";
    let $grid = $("#gridDelegation").getKendoGrid();
    if ($grid) {
        let dataGrid = $grid.dataSource.data();
        let checkedData = dataGrid.find(d => {
            return d.Checked;
        });

        if (!checkedData) {
            swalAlert(dialogTitle, "Please select employee task delegation");
            return;
        }

        confirmResult = await swalConfirm(dialogTitle, `Are you sure delegating "${data.Title}" to ${checkedData.EmployeeName} (${checkedData.EmployeeID})`);
        if (confirmResult.value) {
            try {
                isLoading(true);
                let param = model.newParamTask(data.AXID, data.InstanceId, data.SubmitEmployeeID, checkedData.EmployeeID, "");
                let response = await ajax(`/ESS/Task/Delegate`, "POST", JSON.stringify(param));
                isLoading(false);
                if (response.StatusCode == 200) {
                    swalSuccess(dialogTitle, response.Message);
                    model.render.tasks();
                    return;
                }
                swalError(dialogTitle, response.Message);
            } catch (e) {
                isLoading(false);
            }
        }
    }

};
model.action.cancelTask = async function ($data) {
    let dialogTitle = "Cancel";
    let data = ko.mapping.toJS($data);
    confirmResult = await swalConfirm(dialogTitle, `Are you sure cancelling ${data.Title}?`);
    if (confirmResult.value) {
        try {
            isLoading(true);
            let param = model.newParamTask(data.AXID, data.InstanceId, data.SubmitEmployeeID, "", "");
            let response = await ajax(`/ESS/Task/Cancel`, "POST", JSON.stringify(param));
            isLoading(false);
            if (response.StatusCode == 200) {
                swalSuccess(dialogTitle, response.Message);
                model.render.tasks();
                return;
            }
            swalError(dialogTitle, response.Message);
        } catch (e) {
            isLoading(false);
        }
    }

};

var _cacheMedicalTypeLoaded = false;
model.action.detailsTask = async function ($data) {
    let dialogTitle = "Details";
    let data = ko.mapping.toJS($data);    
    let response = {};
    isLoading(true);

    try {        
        switch (data.RequestType) {
            case 0:
                response = await ajax(`/ESS/Employee/GetByInstanceID/${data.SubmitEmployeeID}/${data.InstanceId}`, "GET");
                if (response.StatusCode == 200) {
                    if (response.Data) {
                        model.detail.resume(model.newEmployee(model.data.preProcessResume(response.Data)));
                        console.log(ko.toJS(model.detail.resume()));
                        $("#resumeModal").modal("show");
                    } else {
                        swalAlert(dialogTitle, "Unable to find employee update request in ESS server");
                    }
                } else {
                    swalAlert(dialogTitle, response.Message);
                }
                break;
            case 1:
                response = await ajax(`/ESS/Employee/GetFamilyByInstanceID/${data.SubmitEmployeeID}/${data.InstanceId}`, "GET");
                if (response.StatusCode == 200) {
                    if (response.Data) {
                        model.detail.family(model.newFamily(response.Data));
                        $("#familyModal").modal("show");
                    } else {
                        swalAlert(dialogTitle, "Unable to find family request in ESS server");
                    }
                } else {
                    swalAlert(dialogTitle, response.Message);
                }
                break;
            case 2:
                // Course
                break;
            case 3:
                response = await ajax(`/ESS/Employee/GetCertificateByInstanceID/${data.SubmitEmployeeID}/${data.InstanceId}`, "GET");
                if (response.StatusCode == 200) {
                    if (response.Data) {
                        model.detail.certificate(model.newCertificate(response.Data));
                        $("#certificateModal").modal("show");
                    } else {
                        swalAlert(dialogTitle, "Unable to find certificate request in ESS server");
                    }
                } else {
                    swalAlert(dialogTitle, response.Message);
                }

                break;                    
            case 4:
                response = await ajax(`/ESS/Leave/GetByInstanceID/${data.SubmitEmployeeID}/${data.InstanceId}`, "GET");
                isLoading(false);
                if (response.StatusCode == 200) {                
                    let selectedDates = model.data.getDates(response.Data.Schedule.Start, response.Data.Schedule.Finish);
                    selectedDates.unshift(str2date(response.Data.Schedule.Start));
                    selectedDates.push(str2date(response.Data.Schedule.Finish));                
    
                    if (response.Data) {
                        model.detail.leave(model.newLeave(response.Data));
                        model.render.calendarLeave(selectedDates);
                        
                        $("#leaveModal").modal('show');
                    } else {
                        swalAlert(dialogTitle, response.Message);
                    }    
                }            
                break;
            case 5:
                response = await ajax(`/ESS/TimeManagement/GetByInstanceID/${data.SubmitEmployeeID}/${data.InstanceId}`, "GET");
                if (response.StatusCode == 200) {
                    model.data.TimeAttendance(model.newTimeAttendance());
                    if (response.Data) {
                        let dt = model.newTimeAttendance(response.Data);
                        $("#modalFormTimeAttendance").modal("show");
                        model.data.TimeAttendance(dt);

                    } else {
                        swalAlert(dialogTitle, "Unable to find absence recomendation request in ESS server");
                    }
                } else {
                    swalAlert(dialogTitle, response.Message);
                }                
                break;
            case 6:
                // replace to 6
                response = await ajax(`/ESS/Travel/GetByInstanceID/${data.SubmitEmployeeID}/${data.InstanceId}`, "GET");
                if (response.StatusCode == 200) {
                    if (response.Data) {
                        let dt = response.Data;
                        model.detail.Travel(dt);
                        model.detail.SPPD(dt.SPPD[0]);

                        let total = 0;
                        let SPPD = dt.SPPD[0];
                        for (var i in SPPD) {
                            if (["accommodation", "fuel", "laundry", "parking", "rent", "ticket", "highway", "airporttransportation", "localtransportation", "mealallowance", "pocketmoney"].indexOf(i.toLowerCase()) > -1) {
                                total += SPPD[i];
                            }
                        }
                        model.detail.TravelTotal(total);

                        var td = [];
                        model.detail.SPPD().TransportationDetails.forEach(function (elm) {
                            td.push(elm.TransportationID)
                        });
                        var transportationdetail = td.reduce((unique, item) => {
                            return unique.includes(item) ? unique : [...unique, item]
                        }, []);
                        model.data.TransportationID(transportationdetail)

                        $("#modalSPPD").modal("show");
                    } else {
                        swalAlert(dialogTitle, "Unable to find travel request SPPD in ESS server");
                    }
                } else {
                    swalAlert(dialogTitle, response.Message);
                }
                break;
            case 7:
                if (!_cacheMedicalTypeLoaded) {
                    let medicalType = await model.get.medicalType();
                    medicalType.forEach((d, i) => {
                        model.map.medicalType[i] = d;
                    });

                    let documentType = await model.get.documentType();
                    documentType.forEach(d => {
                        model.map.documentType[d.TypeID] = d;
                    })

                    _cacheMedicalTypeLoaded = true;
                }

                response = await ajax(`/ESS/Benefit/GetByInstanceID/${data.SubmitEmployeeID}/${data.InstanceId}`, "GET");
                if (response.StatusCode == 200) {
                    if (response.Data) {
                        model.data.medicalBenefit(model.newMedicalBenefit(response.Data));
                        model.data.medicalBenefit().EmployeeID(data.SubmitEmployeeID);
                        model.data.medicalBenefitDetails([]);
                        var details = (response.Data || {}).Details || [];
                        details.forEach(d => {
                            model.data.medicalBenefitDetails.push(model.newMedicalBenefitDetail(d));
                        });
                        $("#modalBenefitReadonly").modal("show");                        

                    } else {
                        swalAlert(dialogTitle, "Unable to find absence recomendation request in ESS server");
                    }
                } else {
                    swalAlert(dialogTitle, response.Message);
                }
                break;
            case 8:
                response = await ajax(`/ESS/Recruitment/GetByInstanceID/${data.SubmitEmployeeID}/${data.InstanceId}`, "GET");
                if (response.StatusCode == 200) {
                    model.detail.Recruitment(model.newRecruitment());
                    if (response.Data) {
                        let dt = model.newRecruitment(response.Data);
                        $("#modalRecruitmentReadonly").modal("show");
                        model.detail.Recruitment(dt);

                    } else {
                        swalAlert(dialogTitle, "Unable to find recruitment request in ESS server");
                    }
                } else {
                    swalAlert(dialogTitle, response.Message);
                }                
                break;
            case 9:
                response = await ajax(`/ESS/Retirement/GetByInstanceID/${data.SubmitEmployeeID}/${data.InstanceId}`, "GET");
                //console.log('response', response)
                if (response.StatusCode == 200) {
                    model.detail.Retirement(model.newRetirement());
                    if (response.Data) {
                        let dt = model.newRetirement(response.Data);
                        $("#modalRetirementReadonly").modal("show");
                        model.detail.Retirement(dt);

                    } else {
                        swalAlert(dialogTitle, "Unable to find retirement request in ESS server");
                    }
                } else {
                    swalAlert(dialogTitle, response.Message);
                }
                break;
            case 10:
                response = await ajax(`/ESS/Complaint/GetByInstanceID/${data.SubmitEmployeeID}/${data.InstanceId}`, "GET");
                //console.log('response', response)
                if (response.StatusCode == 200) {
                    model.data.Ticket(model.newTicket());
                    if (response.Data) {
                        let dt = model.newTicket(response.Data);
                        $("#OpenTicketDetail").modal("show");
                        model.data.Ticket(dt);

                    } else {
                        swalAlert(dialogTitle, "Unable to find ticket request in ESS server");
                    }
                } else {
                    swalAlert(dialogTitle, response.Message);
                }
                break;
            case 12:
                response = await ajax(`/ESS/Employee/GetDocumentRequestByInstanceID/${data.SubmitEmployeeID}/${data.InstanceId}`, "GET");
                //console.log('response', response)
                if (response.StatusCode == 200) {
                    model.detail.documentRequest(model.newDocumentRequest());
                    if (response.Data) {
                        let dt = model.newDocumentRequest(response.Data);
                        model.detail.documentRequest(dt);
                        $("#modalFormDocumentRequest").modal("show");                        

                    } else {
                        swalAlert(dialogTitle, "Unable to find document request in ESS server");
                    }
                } else {
                    swalAlert(dialogTitle, response.Message);
                }
                break;

            default:
        }
        isLoading(false);

    } catch (e) {
        console.error(">>>>",e);
        isLoading(false);
    }
}

model.data.preProcessResume = function(data){
    let _data = _.clone(data);
    _data.Birthdate = moment(_data.Birthdate).format("DD MMM YYYY");    
    _data.ExpatriateDescription = _data.IsExpatriate ? "Yes" : "No";
    _data.Address = _data.Address ||model.newAddress();

    if (_data.Identifications.length > 0) {
        _data.Identifications.forEach(function (d, i) {
            d.Category = "ID";
            d.Label = identificationLabelMap[d.Type.toLowerCase()] ? identificationLabelMap[d.Type.toLowerCase()] : d.Type;
        });
    }
    if (_data.ElectronicAddresses.length > 0) {
        let mapData = {};
        _data.ElectronicAddresses.forEach(function (d, i) {
            if(!mapData[d.Type]){
                mapData[d.Type] = 0;
            }
            mapData[d.Type]++;
        });
        _data.ElectronicAddresses.forEach(function (d, i) {
            d.Category = "ElectronicAddress";
            d.Label = `${d.TypeDescription}${mapData[d.Type] > 1 ? '#'+mapData[d.Type]: ''}`;
        });
    }
    if (_data.BankAccounts.length > 0) {
        _data.BankAccounts.forEach(function (d, i) {
            d.Category = "BankAccount";
            d.Label = `Bank Account ${_data.BankAccounts.length > 1 ? "#"+(++i):""}`;
        });
    }
    if (_data.Taxes.length > 0) {
        _data.Taxes.forEach(function (d, i) {
            d.Category = "Tax";
            d.Label = `NPWP ${_data.Taxes.length > 1 ? "#"+(++i):""}`;
        });
    }

    return _data;
}

model.action.nextNotifcationPage = function () {
    let self = model;

    if (!self.is.renderingNotification()) {
        let currentPage = self.data.pageNotification();
        let total = self.data.totalNotification();
        let currentShown = self.data.currentShownNotification();
        let counter = self.data.counterNotification();

        if (total > counter) {
            currentPage++;
            let offset = (currentPage - 1) * _limitNotification;
            self.data.pageNotification(currentPage);
            self.render.notificationList(_limitNotification, offset);
        }
    }
};

model.action.prevNotifcationPage = function () {
    let self = model;

    if (!self.is.renderingNotification()) {
        let currentPage = self.data.pageNotification();
        let total = self.data.totalNotification();
        let currentShown = self.data.currentShownNotification();
        let counter = self.data.counterNotification();

        if (counter > _limitNotification) {
            currentPage--;
            let offset = (currentPage - 1) * _limitNotification;
            self.data.pageNotification(currentPage);
            self.render.notificationList(_limitNotification, offset);
        }
    }
};
//update req
model.action.prevUpdateRequestPage = function () {
    let self = model;

    if (!self.is.renderingUpdateRequest()) {
        let currentPage = self.data.pageUpdateRequest();
        let counter = self.data.counterUpdateRequest();
        if (counter > _limitUpdateRequest) {
            currentPage--;
            let offset = (currentPage - 1) * _limitUpdateRequest;
            self.data.pageUpdateRequest(currentPage);
            self.render.updateRequestList(_limitUpdateRequest, offset);
        }
    }
};
model.action.nextUpdateRequestPage = function () {
    let self = model;

    if (!self.is.renderingUpdateRequest()) {
        let currentPage = self.data.pageUpdateRequest();
        let total = self.data.totalUpdateRequest();
        let counter = self.data.counterUpdateRequest();
        if (total > counter) {
            currentPage++;
            let offset = (currentPage - 1) * _limitUpdateRequest;
            self.data.pageUpdateRequest(currentPage);
            self.render.updateRequestList(_limitUpdateRequest, offset);
        }
    }
};

let _markingNotificationAsRead = false;
model.action.markNotificationAsRead = function ($data, e) {
    let data = ko.mapping.toJS($data);
    e.stopPropagation();
    if (!_markingNotificationAsRead && !data.Read) {
        _markingNotificationAsRead = true;
        model.app.socket.invoke("MarkNotificationsAsRead", [data])
            .catch(function (err) {
                return console.error(err.toString());
            })
            .then(function (r) {
                $data.Read(true);
            
                _markingNotificationAsRead = false;
            });
    }
};

let _markingAllNotificationAsRead = false;
model.action.markAllNotificationAsRead = function ($data, e) {
    let self = model;
    e.stopPropagation();

    if (!_markingAllNotificationAsRead) {
        _markingAllNotificationAsRead = true;
        model.app.socket.invoke("MarkAllNotificationsAsRead")
            .catch(function (err) {
                return console.error(err.toString());
            })
            .then(function (r) {
                self.data.groupedNotifications().forEach(d => {
                    d.Data().forEach(n => {
                        n.Read(true);
                    });
                });
            
                _markingAllNotificationAsRead = false;
            });
    }
};

model.action.setDelegation = function (uid) {
    let $grid = $("#gridDelegation").getKendoGrid();

    if (!!$grid) {
        let data = $grid.dataSource.data();
        data.forEach(d => {
            if (d.uid == uid) {
                d.Checked = true;
            } else {
                d.Checked = false;
            }
        });

        $grid.dataSource.data(data);
    }
};

let _limitNotification = 10;
model.render.notificationList = function (limit = _limitNotification, offset = 0) {
    let self = model;
    return new Promise((resolve, reject) => {
        let currentPage = self.data.pageNotification();
        let filter = self.data.filterNotification();
        setTimeout(async function () {
            self.is.renderingNotification(true);
            let notifications = await model.app.get.notification(limit, offset, filter);
            self.data.totalNotification(notifications.Total);
            self.data.preProcessNotification(notifications.Data);
            self.data.currentShownNotification(notifications.Data.length);
            self.data.counterNotification((currentPage - 1) * limit + notifications.Data.length);
            self.is.renderingNotification(false);
            resolve(true);
        });
    });
};

let _limitUpdateRequest = 10;
model.render.updateRequestList = function (limit = _limitUpdateRequest, offset = 0) {
    let self = model;
    return new Promise((resolve, reject) => {
        //
        let currentPage = self.data.pageUpdateRequest();
        setTimeout(async function () {
            var dateRange = $("#filterRequestTracking").getKendoDateRangePicker();
            var range = dateRange.range();
            range.start = range.start || new Date();
            range.end = range.end || new Date();
            var startDate = (range == null) ? new Date() : range.start;
            var endDate = (range == null) ? new Date() : range.end;            

            dateRange.range(range);

            self.is.renderingUpdateRequest(true);
            let updateRequest = await model.get.updateRequestRange(startDate, endDate, limit, offset);
            self.data.totalUpdateRequest(updateRequest.Total);
            self.data.preProcessUpdateRequest(updateRequest.Data);
            //self.data.currentShownUpdateRequest(updateRequest.Data.length);
            self.data.counterUpdateRequest((currentPage - 1) * limit + updateRequest.Data.length);
            self.is.renderingUpdateRequest(false);
            resolve(true);
        });
    });
}


let _limitTask = 10;
model.render.tasks = function (limit = _limitTask, offset = 0) {
    let self = model;
    return new Promise((resolve, reject) => {
        let currentPage = self.data.pageTask();
        let filter = self.data.filterTask();
        setTimeout(async function () {
            try {
                self.is.renderingTask(true);

                if(model.is.filterVisible()){
                    var dateRange = $("#filterTask").getKendoDateRangePicker();
                        var range = null;
                        if(dateRange){
                            var _range =dateRange.range();
                            var today = new Date();
                            _range.start = _range.start || daysAgo;
                            _range.end = _range.end || today;
                            range = {
                                Start:_range.start,
                                Finish:_range.end,
                            };

                            dateRange.range(_range);
                        }
                    var tasks = await model.app.get.task(limit, offset, range, model.data.activeTaskTab() == "active");         
                }else{
                    var tasks = await model.app.get.taskActive(limit, offset, filter);
                }
                
                self.data.totalTask(tasks.Total);
                self.data.preProcessTask(tasks.Data);
                self.data.currentShownTask(tasks.Data.length);
                self.data.counterTask((currentPage - 1) * limit + tasks.Data.length);
            } catch (e) {
                console.log("???", e);
            } finally {                
                self.is.renderingTask(false);                                
            }
            resolve(true);
        });
    });
};

model.render.dateRange = function(){
    var filter = $("#filterTask").getKendoDateRangePicker();
    
    if(!!filter){
        filter.destroy();
    }

    $("#filterTask").kendoDateRangePicker({    
        max:new Date(),    
        range: {
            start: model.data.StartDate(),
            end: model.data.FinishDate()
        },
    })
};

model.render.gridDelegation = function (delegationParamID) {
    let $el = $("#gridDelegation");
    if (!!$el) {
        let $grid = $el.getKendoGrid();

        if (!!$grid) {
            $grid.destroy();
        }

        $el.kendoGrid({
            dataSource: {
                transport: {
                    read: {
                        url: `/ESS/Task/GetAssignee/${delegationParamID || model.data.delegateParamID()}`,
                        dataType: "json",
                        type: "POST",
                        contentType: "application/json",
                    },
                    parameterMap: function (data) {
                        return JSON.stringify(data);
                    }
                },
                schema: {
                    data: function (res) {
                        if (res.StatusCode !== 200 && res.Status !== '') {
                            swalFatal("Fatal Error", `Error occured while assignee \n${res.Message}`)
                            return []
                        }

                        var data = res.Data || [];
                        data.forEach(d => {
                            d.Checked = false;
                        });
                        model.is.delegationEnabled(data.length > 0);

                        return data;
                    },
                    total: "Total",
                }
            },
            noRecords: {
                template: "No assignee data available."
            },
            height: 300,
            filterable: true,
            sortable: true,
            columns: [
                {
                    attributes: {
                        "class": "text-center",
                    },
                    template: function (data) {
                        return `<label class="grid-heckbox"><input type="radio" name="delegateEmployee" value="${data.uid}" ${data.Checked ? 'checked' : ''} onclick="model.action.setDelegation('${data.uid}')"/></label>`;

                    },
                    width: 50,
                },
                {
                    field: "EmployeeID",
                    title: "Employee ID",
                    filterable: true,
                },
                {
                    field: "EmployeeName",
                    title: "Name",
                    filterable: true,
                },
            ]
        });
    }
};

model.render.calendarLeave = function (selectedDates) {
    let el = $("#dateCalendarLeave");
    let $calendar = el.data("kendoCalendar");
    let options = {
        selectable: "multiple",
    };

    if (!$calendar) {
        $calendar = el.kendoCalendar(options).getKendoCalendar();
    }

    // Navigate to selected dates
    if (!options.navigate) {
        $calendar.unbind("navigate");
        $calendar.bind("navigate", model.on.calendarNavigate);

        if (selectedDates.length > 0) {
            var firstDayOfMonth = _.clone(selectedDates[0]).setDate(1);             
            $calendar.selectDates(selectedDates.filter(function (x) {
                var dateStr = moment(x).format("MM/DD/YYYY");
                return !_holidaysMap[dateStr];
            }));
            
            $calendar.navigate(new Date(firstDayOfMonth), "month");
        }
    }    

    return $calendar;
};

_isLoadingHoliday = false;
model.on.calendarNavigate = async function (e) {
    $calendar = this;
    var view = $calendar.view();

    if (model.is.readonly()) {
        var today = $calendar.wrapper.find(".k-today");
        if (today.length > 0) {
            today.removeClass("k-today");
        }
    }

    kendo.ui.progress($calendar.wrapper, true);
    if (view.name == "month" && !_isLoadingHoliday) {
        _isLoadingHoliday = true;
        
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
                    let employeeID = model.detail.leave().EmployeeID();
                    disabledDates = await model.get.holiday(startDate, finishDate, employeeID);
                }

                disabledDates.forEach(function (dd) {
                    var day = $(`[data-value='${dd.getFullYear()}/${dd.getMonth()}/${dd.getDate()}']`);
                    if (day.length > 0) {
                        var td = day.closest("td");
                        if (td.length > 0) {                            
                            td.addClass("k-state-disabled");
                            td.removeClass("k-state-selected");
                        }
                    }
                });

            }
        }

        _isLoadingHoliday = false;
    }
    kendo.ui.progress(this.wrapper, false);
    this._previous = this._current;
};

model.init.activity = function () {
    Promise.all([model.render.notificationList(), model.render.updateRequestList()]);
};


var _holidaysCache = {};
var _holidaysMap = {};
model.get.holiday = async function (firstDay, lastDay, employeeID) {    
    let key = `${moment(firstDay).format("MM/DD/YYYY")}_${moment(lastDay).format("MM/DD/YYYY")}_${employeeID}`;
    let holidays = _holidaysCache[key] || [];
    if (holidays.length == 0) {
        let response = await ajaxPost("/ESS/Leave/GetHolidayByEmployeeID", { Range: { Start: firstDay, Finish: lastDay }, EmployeeID: employeeID });
        holidays = response.Data || [];
        _holidaysCache[key] = _.clone(holidays);
    }        
        
    var dates = [];
    holidays.forEach(function (x) {
        var dateStr = moment(x.LoggedDate).format("MM/DD/YYYY");
        var date = new Date(dateStr);
        dates.push(new Date(dateStr));
        _holidaysMap[dateStr] = true;
        if (model.data.holiday.indexOf(date) == -1) {
            model.data.holiday.push(date);
        }
    });
    return dates;
}

model.get.gender = async function () {
    let response = await ajax("/ESS/Employee/GetGender", "GET");
    if (response.StatusCode == 200) {
        let data = response.Data || [];
        model.list.gender(data);
    }
};

model.get.maritalStatus = async function () {
    let response = await ajax("/ESS/Employee/GetMaritalStatus", "GET");
    if (response.StatusCode == 200) {
        let data = response.Data || [];
        model.list.maritalStatus(data);
        return data;
    }
};

model.get.maritalStatus = async function () {
    let response = await ajax("/ESS/Employee/GetMaritalStatus", "GET");
    if (response.StatusCode == 200) {
        let data = response.Data || [];
        model.list.maritalStatus(data);
        return data;
    }
};

model.get.familyRelationship = async function () {
    let response = await ajax("/ESS/Employee/GetFamilyRelationship", "GET");
    if (response.StatusCode == 200) {
        let data = response.Data || [];

        data.forEach(d => {
            model.map.familyRelationship[d.TypeID] = d.Description;
        });

        model.list.familyRelationship(data);
        return data;
    }

    console.error(response.StatusCode, ":", response.Message)
    return [];
};

model.get.travelPurpose = async function () {
    let response = await ajax("/ESS/Travel/GetPurposes", "GET");
    
    if (response.StatusCode == 200) {
        let data = response.Data || [];

        data.forEach(d => {
            model.map.purpose[d.AXID] = d;
        });

        return data;
    }

    console.error(response.StatusCode, ":", response.Message)
    return [];
};

model.get.transportation = async function () {
    let response = await ajax("/ESS/Travel/GetTransportations", "GET");
    
    if (response.StatusCode == 200) {
        let data = response.Data || [];

        data.forEach(d => {
            model.map.transportation[d.TransportationID] = d.Description;
        });

        return data;
    }

    console.error(response.StatusCode, ":", response.Message)
    return [];
};

let _masterDataLoaded = false;
model.init.task = function () {
    if(!_masterDataLoaded){        
        _masterDataLoaded = true;
        setTimeout(() => {
            model.get.familyRelationship();
        });

        setTimeout(() => {
            model.get.travelPurpose();
        });

        setTimeout(() => {
            model.get.transportation();
        });

        // setTimeout(async function () {
        //     var data = await model.get.TicketType()
        //     data.forEach((d) => {
        //         model.map.TicketType[d.value] = d.text
        //     })
        // });

        // setTimeout(async function () {
        //     var data = await model.get.TicketStatus()
        //     data.forEach((d) => {
        //         model.map.TicketStatus[d.value] = d.text
        //     })            
        // });
    
        // setTimeout(async function () {
        //     var data = await model.get.TicketMedia()
        //     data.forEach((d) => {
        //         model.map.TicketMedia[d.value] = d.text
        //     })            
        // });
    
        // setTimeout(async function () {
        //     var data = await model.get.TicketStatusLabelCss()
        //     data.forEach((d) => {
        //         model.map.TicketStatusLabelCss[d.value] = d.text
        //     })
        // });
    
        // setTimeout(async function () {
        //     var data = await model.get.TicketCategories()            
        //     data.forEach((d) => {
        //         model.map.TicketCategories[d.Id] = d;
        //     })
        // });
    }

    $('#modalFormDelegation').on('show.bs.modal', function (e) {
        model.render.gridDelegation();
    });    

    model.render.tasks();        
};

model.action.downloadAddressFile = function (data) {
    var d = ko.mapping.toJS(data);
    if (data.hasOwnProperty("Address")) {
        d = ko.mapping.toJS(data.Address);
    }

    if (d.Filename) {
        window.open(`/ESS/Employee/DownloadAddress/${d.AXID}/${d.Filename}`);
    }
}

model.action.downloadMedicalAttachment = function (file) {

    var f = ko.toJS(file)

    var benefitAXID = model.data.medicalBenefit().AXID();
    var employeeID = model.data.medicalBenefit().EmployeeID();
    if (benefitAXID <= 0) {
        window.open(`/ESS/Benefit/DownloadMedicalDocumentByEmployee/${employeeID}/${benefitAXID}/${f.AXID}/${f.Attachment.Filename}`);
    } else {
        window.open(`/ESS/Benefit/DownloadMedicalDocument/${benefitAXID}/${f.AXID}/${f.Attachment.Filename}`);
    }
};

model.action.downloadEmployeeAttachment = function (data, e) {
    let field = e.currentTarget.dataset["field"];
    var d = ko.mapping.toJS(data);
    switch (field) {
        case "IsExpartiarte":
            if (d.IsExpartriateAttachment.Filename) {
                window.open(`/ESS/Employee/DownloadEmployeeAttachment/${field}/${d.AXID}/${d.IsExpartriateAttachment.Filename}`);
            }
            break;
        case "MaritalStatus":
            if (d.MaritalStatusAttachment.Filename) {
                window.open(`/ESS/Employee/DownloadEmployeeAttachment/${field}/${d.AXID}/${d.MaritalStatusAttachment.Filename}`);
            }
            break;
        default:
            break;
    }
}

model.action.downloadFieldFile = function (data) {
    var d = ko.mapping.toJS(data);
    console.log("field:", d)
    if (d.Filename) {
        console.log("field:", d.Category)
        switch (d.Category) {
            case "BankAccount":
                window.open(`/ESS/Employee/DownloadBankAccount/${d.AXID}/${d.Filename}`);
                break;
            case "Tax":
                window.open(`/ESS/Employee/DownloadTax/${d.AXID}/${d.Filename}`);
                break;
            case "ElectronicAddress":
                window.open(`/ESS/Employee/DownloadElectronicAddress/${d.AXID}/${d.Filename}`);
                break;
            case "ID":
                window.open(`/ESS/Employee/DownloadIdentification/${d.AXID}/${d.Filename}`);
                break;
            default:
                console.warn(`unable to find category "${d.Category}"\n${ko.mapping.toJS(d)}`)
                break;
        }
    }
}