model.newRecruitment = function(obj){
    let proto = _.clone(this.proto.Recruitment || {});
    
    if (typeof obj != "object") {
        obj = {};        
    }

    var data = Object.assign(proto, obj);
    data.EstimationStartedDate = data.EstimationStartedDate || new Date();
    return ko.mapping.fromJS(data);
};

model.newApplication = function(obj){
    let proto = _.clone(this.proto.Application || {});
    
    if (typeof obj != "object") {
        obj = {};        
    }

    var data = Object.assign(proto, obj);
    if(!data.Schedule){
        data.Schedule = _.clone(this.proto.DateRange);
        data.Schedule.Start = _.clone(_DEFAULT_DATE);
        data.Schedule.End = _.clone(_DEFAULT_DATE);
    }    
    data.StartDate = data.StartDate|| _.clone(_DEFAULT_DATE);
    data.ExpireDate = data.ExpireDate ||_.clone(_DEFAULT_DATE)
    data.DateOfReception =data.DateOfReception || _.clone(_DEFAULT_DATE);
    data.ScheduleHistories =data.ScheduleHistories || [];
        
    return ko.mapping.fromJS(data);
};

model.data.StartDate = ko.observable(moment().subtract(7, "days").toDate());
model.data.EndDate = ko.observable(new Date());
model.data.recruitment = ko.observable(model.newRecruitment());
model.data.application = ko.observable(model.newApplication());
model.data.maximumOpenings = ko.observable(0);
model.data.today = ko.observable(new Date);
model.is.buttonLoading = ko.observable(true);

model.list.recruitmentTypes = ko.observableArray([]);
model.list.jobs = ko.observableArray([]);
model.list.positions = ko.observableArray([]);

model.map.jobs = {};
model.map.positions = {};

model.get.jobs = async function () {
    let response = await ajax("/ESS/Recruitment/GetJobs", "GET");
    if (response.StatusCode == 200) {
        return response.Data || [];
    }
    return [];
};

model.get.positions = async function () {
    let response = await ajax("/ESS/Recruitment/GetPositions", "GET");
    if (response.StatusCode == 200) {
        return response.Data || [];
    }
    return [];
};

model.get.applicationHistory = async function (recruitmentID) {
    let response = await ajax("/ESS/Recruitment/GetDetail/"+recruitmentID, "GET");
    if (response.StatusCode == 200) {
        return response.Data || [];
    }
    return [];
};

model.render.gridRequestRecruitment = function () {
    let $el = $("#gridRequestRecruitment");
    if (!!$el) {
        let $grid = $el.getKendoGrid();

        if (!!$grid) {
            $grid.destroy();
        }

        $el.kendoGrid({
            dataSource: {
                transport: {
                    read: {
                        url: "/ESS/Recruitment/GetRequest",
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
                        // data.Status = model.data.recruitmentStatus() || -1;
                        return JSON.stringify(data);
                    }
                },
                schema: {
                    data: function (res) {

                        if (res.StatusCode !== 200 && res.Status !== '') {
                            swalFatal("Fatal Error", `Error occured while fetching recruitment request(s)\n${res.Message}`)
                            return []
                        }
                        return res.Data || [];
                    },
                    total: "Total",
                },
                error: function (e) {
                    swalFatal("Fatal Error", `Error occured while fetching recruitment request(s)\n${e.xhr.responseText}`)
                },
                sort: { field: "RecruitmentID", dir: "asc" },
            },
            pageable: {
                previousNext: false,
                info: false,
                numeric: false,
                refresh: true
            },
            noRecords: {
                template: "No recruitment request data available."
            },
            //filterable: true,
            //sortable: true,
            //pageable: true,
            columns: [
                {
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
                    field: "RecruitmentID",
                    title: "#",
                    template: function(d){
                        return d.RecruitmentID || '-';
                    },
                    width: 100
                },
                {
                    field: "EstimationStartedDate",
                    title: "Estimation Started Date",
                    template: '#= standarizeDate(EstimationStartedDate) #',
                    width: 250
                },
                {                    
                    title: "Description",
                    field:"Description",                    
                    width: 300
                },
                {
                    title: "Job/Position",
                    template: function(d){
                        if(d.RecruitmentType == 2){
                            return d.PositionDescription || d.JobDescription || '-';
                        }else {
                            return d.JobDescription || d.PositionDescription || '-';
                        }                        
                    },
                    width: 150
                },
                {
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
                    title: "Openings",
                    field: "NumberOfOpenings",
                    width: 100
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
                    template: function(d){                        
                        let statusClass = "secondary";
                        let statusLabel = camelToTitle(d.StatusDescription || "");                                            
                        switch (d.RecruitmentStatus) {                                                        
                            case 0:
                                statusClass = "info";
                                break;
                            case 1:
                                statusClass = "success";
                                break;
                            case 2:
                                statusClass = "warning";
                                break;
                            case 3:
                                statusClass = "danger";
                                break;
                            default:
                                break;
                        }

                        if (!!d.AXRequestID) {
                            return `<a href="#" onclick="model.app.action.trackTask('${d.AXRequestID}'); return false;"><span class="badge badge-${statusClass}">${statusLabel.toLowerCase()}</span></a>`;
                        }
                        return `<span class="badge badge-${statusClass}">${statusLabel.toLowerCase()}</span>`;
                    },
                    width: 100
                },
                {
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
                    field: "RecruitmentStatus",
                    title: "Recruitment Status",
                    template: function(d){                        
                        let statusClass = "secondary";
                        let statusLabel = camelToTitle(d.RecruitmentStatusDescription || "");
                        switch (d.RecruitmentStatus) {                                                        
                            case 0:
                                statusClass = "secondary";
                                break;
                            case 1:
                                statusClass = "info";                                
                                break;
                            case 2:
                                statusClass = "success";                                
                                break;
                            case 3:
                                statusClass = "warning";                                
                                break;
                            default:
                                break;
                        }

                        return `<span class="badge badge-${statusClass}">${statusLabel.toLowerCase()}</span>`;
                    },
                    width: 100
                },
                {
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },                    
                    template: function(d){                                            
                        return `<button type="button" class="btn btn-xs btn-info" onclick="model.action.openRecruitmentDetail('${d.uid}')"><i class="fa fa-eye"></i></button>`;
                    },
                    width: 50
                },
            ]
        });
    }
}

model.render.gridOpenRecruitment = function () {
    let $el = $("#gridOpenRecruitment");
    if (!!$el) {
        let $grid = $el.getKendoGrid();

        if (!!$grid) {
            $grid.destroy();
        }

        $el.kendoGrid({
            dataSource: {
                transport: {
                    read: {
                        url: "/ESS/Recruitment/GetOpenings",
                        dataType: "json",
                        type: "POST",
                        contentType: "application/json",
                    },
                    parameterMap: function (data, type) {
                        data.EmployeeID = ""
                        // data.Range = {
                        //     Start: model.data.StartDate(),
                        //     Finish: model.data.EndDate()
                        // }
                        // data.Status = model.data.recruitmentStatus() || -1;
                        return JSON.stringify(data);
                    }
                },
                schema: {
                    data: function (res) {

                        if (res.StatusCode !== 200 && res.Status !== '') {
                            swalFatal("Fatal Error", `Error occured while fetching recruitment opening(s)\n${res.Message}`)
                            return []
                        }
                        return res.Data || [];
                    },
                    total: "Total",
                },
                error: function (e) {
                    swalFatal("Fatal Error", `Error occured while fetching recruitment opening(s)\n${e.xhr.responseText}`)
                },
                sort: { field: "RecruitmentID", dir: "asc" },
            },
            pageable: {
                previousNext: false,
                info: false,
                numeric: false,
                refresh: true
            },
            noRecords: {
                template: "No open recruitment data available."
            },
            columns: [
                {
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
                    field: "RecruitmentID",
                    title: "#",
                    template: function(d){
                        return d.RecruitmentID || '-';
                    },
                },
                // {
                //     headerAttributes: {
                //         "class": "text-center",
                //     },
                //     attributes: {
                //         "class": "text-center",
                //     },
                //     title: "Type",
                //     field:"RecruitmentType",
                //     template: function(d){
                //         return model.proto.RecruitmentTypes[d.RecruitmentType] || '-';
                //     },
                // },
                {
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
                    field: "Department",
                    title: "Department",
                },                
                {
                    field: "Description",
                    title: "Description",
                    width:250
                },
                
                {
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
                    field: "NumberOfOpenings",
                    title: "Openings",
                    width: 100,
                },
                {
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
                    field: "OpenDate",
                    title: "Open Date",
                    template: '#= kendo.toString(kendo.parseDate(OpenDate), "dd MMMM yyyy")#'
                },
                {
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
                    field: "Deadline",
                    title: "Deadline",
                    template: '#= kendo.toString(kendo.parseDate(Deadline), "dd MMMM yyyy")#'
                },
                {
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
                    template: function (d) {      
                        if(d.Application){
                            return `
                                <button type="button" class="btn btn-xs btn-outline-success" onclick="model.action.applicationDetail('${d.uid}'); return false;">
                                    applied
                                </button>
                            `;
                        }

                        if(d.NumberOfOpenings > 0){
                            return `
                                <button type="button" class="btn btn-xs btn-outline-info" onclick="model.action.apply('${d.uid}'); return false;">
                                    <i class="mdi mdi-file-check"></i> apply
                                </button>
                            `;
                        }

                        return `
                            <button type="button" class="btn btn-xs btn-outline-secondary" disabled>
                                <i class="mdi mdi-file-check"></i> apply
                            </button>
                        `;

                    },
                    width: 100,
                },    
            ]
        });
    }
}

model.render.gridHistory = function () {
    let $el = $("#gridHistory");
    if (!!$el) {
        let $grid = $el.getKendoGrid();

        if (!!$grid) {
            $grid.destroy();
        }

        $el.kendoGrid({
            dataSource: {
                transport: {
                    read: {
                        url: "/ESS/Recruitment/GetHistory",
                        dataType: "json",
                        type: "POST",
                        contentType: "application/json",
                    },
                    parameterMap: function (data, type) {
                        data.EmployeeID = ""
                        // data.Range = {
                        //     Start: model.data.StartDate(),
                        //     Finish: model.data.EndDate()
                        // }
                        // data.Status = model.data.recruitmentStatus() || -1;
                        return JSON.stringify(data);
                    }
                },
                schema: {
                    data: function (res) {

                        if (res.StatusCode !== 200 && res.Status !== '') {
                            swalFatal("Fatal Error", `Error occured while fetching recruitment request(s)\n${res.Message}`)
                            return []
                        }
                        return res.Data || [];
                    },
                    total: "Total",
                },
                error: function (e) {
                    swalFatal("Fatal Error", `Error occured while fetching recruitment request(s)\n${e.xhr.responseText}`)
                },
                sort: { field: "RecruitmentID", dir: "asc" },
            },
            pageable: {
                previousNext: false,
                info: false,
                numeric: false,
                refresh: true
            },
            noRecords: {
                template: "No applied recruitment data available."
            },
            columns: [
                {
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
                    field: "RecruitmentID",
                    title: "#",
                    template: function(d){
                        return d.RecruitmentID || '-';
                    },
                },
                // {
                //     headerAttributes: {
                //         "class": "text-center",
                //     },
                //     attributes: {
                //         "class": "text-center",
                //     },
                //     title: "Type",
                //     field:"RecruitmentType",
                //     template: function(d){
                //         return model.proto.RecruitmentTypes[d.RecruitmentType] || '-';
                //     },
                // },
                {
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
                    field: "Department",
                    title: "Department",
                },                
                {
                    field: "Description",
                    title: "Description",
                    width:250
                },
                
                {
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
                    field: "NumberOfOpenings",
                    title: "Openings",
                    width: 100,
                },
                {
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
                    field: "OpenDate",
                    title: "Open Date",
                    template: '#= kendo.toString(kendo.parseDate(OpenDate), "dd MMMM yyyy")#'
                },
                {
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
                    field: "Deadline",
                    title: "Deadline",
                    template: '#= kendo.toString(kendo.parseDate(Deadline), "dd MMMM yyyy")#'
                },
                {
                    template: function (d) {      
                        var status = d.Application.ApplicationStatus;
                        var statusClass = "btn-outline-secondary";
                        var statusDescription = camelToTitle(d.Application.ApplicationStatusDescription || "") || "not available";
                        switch (status) {
                            //Received
                            case 0:
                            //Confirmed
                            case 1:
                                statusClass = "btn-outline-primary";
                                break;                        
                            //Interview
                            case 2:
                            //TesAdministrasi
                            case 6:
                            //TesPotensiAkademik
                            case 7:
                            //TesPsikologi
                            case 8:
                            //MedicalCheckUp
                            case 9:
                                statusClass = "btn-outline-info";
                                break; 
                            //Withdraw                       
                            case 4:
                                statusClass = "btn-outline-warning";
                                break;                            
                            //Employed
                            case 5:
                                statusClass = "btn-outline-success";
                                break;  
                            //Rejection
                            case 3:
                                statusClass = "btn-outline-danger";
                                break;  
                            default:
                                statusClass = "btn-outline-secondary";
                                break;
                        }              

                        return `
                            <button type="button" class="btn btn-xs ${statusClass}" onclick="model.action.applicationDetail('${d.uid}','#gridHistory'); return false;">
                                ${statusDescription}
                            </button>
                        `;
                    },
                    width: 100,
                },
            ]
        });
    }
}

model.render.gridHistoryApplication = function () {
    let $el = $("#gridHistoryApplication");
    if (!!$el) {
        let $grid = $el.getKendoGrid();

        if (!!$grid) {
            $grid.destroy();
        }

        $el.kendoGrid({
            dataSource: {
                data: [],
            },            
            noRecords: {
                template: "No application history data available."
            },            
            columns: [
                // {
                //     field: "ApplicationID",
                //     title: "#",
                //     template: function(d){
                //         return d.ApplicationID || '-';
                //     },
                // },
                // {
                //     headerAttributes: {
                //         "class": "text-center",
                //     },
                //     attributes: {
                //         "class": "text-center",
                //     },
                //     field: "Date",
                //     title: "Date",
                //     template: function(d){
                //         return standarizeDate(d.Date);
                //     },
                // },
                // {
                //     headerAttributes: {
                //         "class": "text-center",
                //     },
                //     attributes: {
                //         "class": "text-center",
                //     },
                //     title: "Location",
                //     field: "Location",
                // },                
                {                    
                    title: "Step",
                    field: "ApplicantStepDescription",
                    template: function(d){
                        return camelToTitle(d.ApplicantStepDescription || "");
                    }
                },                
                {
                    field: "Time",
                    title: "Schedule",                    
                    template: function(d){                        
                        var result = `${d.Location}<br/>`;
                        var date = standarizeDate(d.Date);
                        var range = d.Schedule || {};
                        var startTime = standarizeTime(range.Start);
                        var endTime = standarizeTime(range.Finish);
                        if(startTime != "-" && endTime != "-")
                            result+= `<small>${date}<br/>${startTime} - ${endTime}</small>`;
                        else if(startTime != "-" && endTime == "-")
                            result+=`<small>${date}<br/>${startTime}</small>`;
                        else
                            result+= `<small>${date}</small>`;

                        return result;
                    }
                },
                // {
                //     field: "Recruiter",
                //     title: "Recruiter",
                //     template: function(d){
                //         return d.RecruiterName || "-";
                //     }
                // },
                {
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
                    field: "Status",
                    title: "Status",
                    template: function(d){
                        var status = d.ApplicantScheduleStatus;
                        var statusClass = "badge-secondary";
                        var statusDescription = camelToTitle(d.ApplicantScheduleStatusDescription || '-') || 'not available';
                        switch (status) {
                            case 0:
                                statusClass = "badge-primary";
                                break;
                            case 1:
                                statusClass = "badge-success";
                                break;                        
                            case 2:
                                statusClass = "badge-warning";
                                break;                            
                            default:
                                break;
                        }                                                

                        return `<span class="badge ${statusClass}">${statusDescription}</span>`;
                    },
                    width:100
                },
            ]
        }).getKendoGrid();
    }
}

model.action = {}
model.action.request = async function () {        
    let self = model;
    let dialogTitle = "Recruitment";
    let data = ko.mapping.toJS(model.data.recruitment());  

    var result = await swalConfirm(dialogTitle, `Are you sure requesting this recruitment ?`);    
    if (!result.value) return

    if (!data.RecruitmentType && data.RecruitmentType != 0) {
        swalAlert(dialogTitle, 'Recruitment type is required.');
        return;
    }

    if (data.RecruitmentType == 1 && !data.JobID) {
        swalAlert(dialogTitle, 'Job is required.');
        return;
    }

    if (data.RecruitmentType == 2 && !data.PositionID) {
        swalAlert(dialogTitle, 'Position is required.');
        return;
    }

    if (data.NumberOfOpenings <= 0) {
        swalAlert(dialogTitle, 'Number of openings cannot be less equal than 0');
        return;
    }

    let maxOpenings = self.data.maximumOpenings();
    if (maxOpenings <= 0) {
        swalAlert(dialogTitle, `Maximum opening could not be defined from selected job/position`);
        return;
    }

    if (data.NumberOfOpenings > 0 && maxOpenings > 0 && data.NumberOfOpenings > maxOpenings) {
        swalAlert(dialogTitle, `Number of openings cannot be more than ${maxOpenings}`);
        return;
    }    
    
    if(!!data.JobID) data.JobDescription = (self.map.jobs[data.JobID] || {}).Description || null;
    if(!!data.PositionID) data.PositionDescription = (self.map.positions[data.PositionID] || {}).Description || null;    

    let formData = new FormData();
    formData.append("JsonData", JSON.stringify(data));
    
    var files = $('#Filepath').getKendoUpload().getFiles();
    if (files.length > 0) {
        formData.append("FileUpload", files[0].rawFile);
    } 
    // else {
    //     if (data.TravelRequestStatus != 1) {
    //         swalAlert(dialogTitle, "Document attachment could not be empty");
    //         return;
    //     }
    // }

    try {
        isLoading(true);
        ajaxPostUpload("/ESS/Recruitment/Request", formData, function (data) {
            isLoading(false);
            if (data.StatusCode == 200) {
                swalSuccess(dialogTitle, data.Message);
                $("#modalRequestRecruitment").modal("hide");
                model.action.refreshRecruitmentRequest();
            } else {
                $("#modalRequestRecruitment").modal("show");
                swalFatal(dialogTitle, data.Message);
            }
        }, function (data) {
            isLoading(false);
            $("#modalRequestRecruitment").modal("show");
            swalFatal(dialogTitle, data.Message);
        })


    } catch (e) {
        isLoading(false);
    }

    return false;
}

model.action.apply = async function (uid) {        
    let self = model;
    let dialogTitle = "Recruitment";
    var dataGrid = $("#gridOpenRecruitment").data("kendoGrid").dataSource.getByUid(uid);    
    
    if(!!dataGrid){
        var result = await swalConfirm(dialogTitle, `Are you sure applying for "${dataGrid.PositionDescription || dataGrid.JobDescription}" ?`);    
        if (!result.value) return        

        var formData = new FormData();
        var _data = dataGrid._raw();
        _data.ApplyDate = new Date();
        _data.Recruitment = _.cloneDeep(_data);
        formData.append("JsonData", JSON.stringify(_data));

        try {
            isLoading(true);
            ajaxPostUpload("/ESS/Recruitment/Apply", formData, function (data) {
                isLoading(false);
                if (data.StatusCode == 200) {
                    swalSuccess(dialogTitle, data.Message);
                    $("#modalRequestRecruitment").modal("hide");
                    model.action.refreshOpening();
                } else {
                    $("#modalRequestRecruitment").modal("show");
                    swalFatal(dalogTitle, data.Message);
                }
            }, function (data) {
                isLoading(false);
                $("#modalRequestRecruitment").modal("show");
                swalFatal(dialogTitle, data.Message);
            })        
        } catch (e) {
            isLoading(false);
        }
    }        

    return false;

}

model.action.applicationDetail = async function (uid, gridID="#gridOpenRecruitment") {        
    let self = model;
    let dialogTitle = "Recruitment";
    var dataGrid = $(gridID).data("kendoGrid").dataSource.getByUid(uid);    
    
    // model.render.gridHistoryApplication();
    
    if(!!dataGrid){            
        try {
            isLoading(true);
            let response = await ajax("/ESS/Recruitment/GetDetail/"+dataGrid.RecruitmentID, "GET");
            isLoading(false);                        
            if (response.StatusCode == 200) {
                var data = response.Data;
                model.data.application(model.newApplication(data));
                
                var $gridHistoryApplication = $("#gridHistoryApplication").getKendoGrid();                
                $gridHistoryApplication.dataSource.data(data.ScheduleHistories);                
                $("#modelApplicationHistory").modal("show");
            }else{
                swalFatal(dialogTitle, data.Message);
            }            
        } catch (e) {
            isLoading(false);
        }
    }        

    return false;
}

model.action.refreshRecruitmentRequest = function (uiOnly = false) {
    var $grid = $("#gridRequestRecruitment").data("kendoGrid");
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

model.action.refreshOpening = function (uiOnly = false) {
    var $grid = $("#gridOpenRecruitment").data("kendoGrid");
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

model.action.openRecruitmentRequest = () => {
    let self = model;    
    self.data.recruitment(model.newRecruitment());
    $("#modalRequestRecruitment").modal("show")
}

model.action.openRecruitmentDetail = function (uid) {
    var dataGrid = $("#gridRequestRecruitment").data("kendoGrid").dataSource.getByUid(uid);    
    if(dataGrid){
        model.data.recruitment(model.newRecruitment(dataGrid));
        $("#modalRecruitmentReadonly").modal("show");
    }
    
}
model.action.openModalApplication = (uid, status) => {
    $("#modalApplication").modal("show")
}
model.action.openModalHistoryApplication = async (uid) => {
    var dataGrid = $("#gridHistory").data("kendoGrid").dataSource.getByUid(uid);    
    
    var $gridHistory = $("#gridHistoryApplication").data("kendoGrid");
    $gridHistory.dataSource.data([]);
    if(dataGrid){
        var history = await model.get.applicationHistory(dataGrid.RecruitmentID);                
        $gridHistory.dataSource.data(history);

        $("#modalHistoryApplication").modal("show")
    }
}

model.action.recruitmentType = (e) => {
    //if (e.dataItem) {
    //    var dataItem = e.dataItem;
    //    if (dataItem.text == 'Job') {
    //        console.log(e)
    //        $('#typeJob').removeClass('d-none');
    //        $('#typePosition').addClass('d-none');
    //    } else if (dataItem.text == 'Position') {
    //        $('#typeJob').addClass('d-none');
    //        $('#typePosition').removeClass('d-none');
    //    }
    //} 
}

model.on.documentSelect = (e) => {
    console.log('select file :)')
}

model.on.recruitmentTypeChange =function(e){
    model.data.recruitment().EstimationStartedDate(0);
    model.data.maximumOpenings(0);
};

model.on.jobOrPositionChange =function(e){
    var value = this.value();
    var type = model.data.recruitment().RecruitmentType();
    var maxOpenings = 0;

    model.data.maximumOpenings(maxOpenings);    
    if(type==1){
        var job = model.map.jobs[value];    
        maxOpenings = job.MaxPositions;
    }else if(type==2){
        maxOpenings=1;
        model.data.recruitment().NumberOfOpenings(maxOpenings);
    }
    model.data.maximumOpenings(maxOpenings);
};

model.init.applicationOpenRecruitment = () => {
    let self = model;       

    model.render.gridOpenRecruitment();
    model.render.gridHistoryApplication()
}
model.init.applicationHistory = () => {
    model.render.gridHistory()
    model.render.gridHistoryApplication()
}

model.init.recruitmentRequest = ()=>{
    var self = model;
    var types = _.clone(model.proto.RecruitmentTypes);
    for(var i in types){
        self.list.recruitmentTypes.push({"text":types[i], "value":i});
    }
    
    setTimeout(async function(){
        model.is.buttonLoading(true);
        await Promise.all([
            new Promise(async (resolve)=>{
                var data = await self.get.jobs();

                data.forEach(d=>{
                    self.map.jobs[d.JobID] = d;
                });

                self.list.jobs(data);
                resolve(true);
            }),

            new Promise(async (resolve)=>{
                var data = await self.get.positions();
                
                data.forEach(d=>{
                    self.map.positions[d.PositionID] = d;
                });

                self.list.positions(data);
                resolve(true);
            }),
        ]);
        model.is.buttonLoading(false);
    });

    self.render.gridRequestRecruitment();
};

model.init.application = () => {
    var self = model;
    // set tab listener
    let activatedTabMap = {};
    $('a[data-toggle="tab"]').on('shown.bs.tab', async function (e) {
        let title = $(e.target).text();
        let target = $(e.target).attr('href');
        let relatedTarget = $(e.relatedTarget).attr('href');

        var breadcrumbs = self.breadcrumbs();
        if (breadcrumbs.length > 3) {
            breadcrumbs.splice(breadcrumbs.length - 1, 1)
        }
        breadcrumbs.push({
            Title: title,
            URL: "#",
        });
        self.breadcrumbs(breadcrumbs);

        if (!activatedTabMap[target]) {
            switch (target) {
                case '#open':
                case '#openRecruitment':
                    await self.init.applicationOpenRecruitment();
                    break;
                case '#history':
                    await self.init.applicationHistory();
                    break;
                default:
                    break;
            };

            activatedTabMap[target] = true;
        }
    })

    let target = window.location.hash;
    if (target) {
        let $tab = $(`#applicationTab li a[href="${target}"]`);
        if ($tab.length > 0) {
            $tab.tab('show');
            return;
        }
    }

    $('#applicationTab li:first-child a').tab('show')
    // $('#applicationTab a:first-child').tab('show')
}