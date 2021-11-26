var today = new Date();
var prevDay = new Date();
var nextDay = new Date();
prevDay.setDate(prevDay.getDate() - 30);
nextDay.setDate(nextDay.getDate() + 30);

model.newTraining = function (obj) {
    let o = _.clone(this.proto.Training);
    if (obj && typeof obj == "object") {
        o = Object.assign(o, _.clone(obj));
    }

    if (!o.Schedule) o.Schedule = _.clone(this.proto.DateRange);

    return ko.mapping.fromJS(o);
}

model.newTrainingRegistration = function (obj) {
    let o = _.clone(this.proto.TrainingRegistration);
    o.References = o.References || [];
    if (obj && typeof obj == "object") {
        o = Object.assign(o, _.clone(obj));
    }

    //if (!o.Schedule) o.Schedule = _.clone(this.proto.DateRange);

    return ko.mapping.fromJS(o);
}

model.data.Training = ko.observable(model.newTraining());
model.data.TrainingRegistration = ko.observable(model.newTrainingRegistration());
model.data.StartDate = ko.observable(prevDay);
model.data.EndDate = ko.observable(nextDay);
model.data.trainingStatus = ko.observable();

model.is.filterHidden = ko.observable(false);

model.list.trainingStatus = ko.mapping.fromJS([
    { text : "Open" , value: 1 },
    { text : "Closed", value: 2 },
]);

model.render.gridTraining = function () {
    let self = model;
    let $el = $("#gridTraining");
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
                        url: "/ESS/Training/Get",
                        dataType: "json",
                        type: "POST",
                        contentType: "application/json",
                    },
                    parameterMap: function (data, type) {
                        data.EmployeeID = ""
                        var _start = model.data.StartDate();
                        var _end = model.data.EndDate();

                        if(_start > _end){
                            data.Range = {
                                Start: _end,
                                Finish: _start
                            }
                        }else{
                            data.Range = {
                                Start: _start,
                                Finish: _end
                            }
                        }
                        
                        data.Status = model.data.trainingStatus() || -1;
                        return JSON.stringify(data);
                    }
                },
                schema: {
                    data: function (res) {
                        if (res.StatusCode !== 200 && res.Status !== '') {
                            swalFatal("Fatal Error", `Error occured while fetching training(s)\n${res.Message}`)
                            return []
                        }
                        return res.Data || [];
                    },
                    total: "Total",
                },
                error: function (e) {
                    swalFatal("Fatal Error", `Error occured while fetching training(s)\n${e.xhr.responseText}`)
                },
                sort: { field: "TrainingID", dir: "asc" },
            },
            pageable: {
                previousNext: false,
                info: false,
                numeric: false,
                refresh: true
            },
            noRecords: {
                template: "No training  data available."
            },
            columns: [                
                {
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
                    field: "TrainingID",
                    title: "#",
                    width: 100,                    
                },
                {
                    field: "Name",                    
                    title: "Training",                    
                    width: 300,
                    template:function(d){
                        return `
                        <strong class="d-block">${d.Name}</strong>                        
                        <small class="d-block">${d.Location}</small>
                        <small class="d-block">${(!!d.SubTypeDescription && !!d.TypeDescription)?d.TypeDescription+' - '+d.SubTypeDescription:d.TypeDescription}</small>
                        `;
                    }
                },
                {
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
                    title: "Schedule", width: 250,
                    template: function (d) {
                        let startTime = standarizeTime(d.Schedule.Start);
                        let finishTime = standarizeTime(d.Schedule.Finish);
                        let strStartDate = standarizeDate(d.Schedule.Start);                        
                        let strFinishDate = standarizeDate(d.Schedule.Finish);
                        let strStartDateTime = standarizeDateTime(d.Schedule.Start);
                        let strFinishDateTime = standarizeDateTime(d.Schedule.Finish);
                        let strSchedule = "";

                        if(startTime == "00:00") strStartDateTime = strStartDate;
                        if(finishTime == "00:00") strFinishDateTime = strFinishDate;

                        if (strStartDate == strFinishDate) {
                            var strStartTime = standarizeTime(d.Schedule.Start);
                            var strFinishTime = standarizeTime(d.Schedule.Finish);

                            if (strStartDateTime == strFinishDateTime) {
                                strSchedule = `${strStartDate} at ${strStartTime}`;
                            }else{
                                strSchedule = `${strStartDate} at ${strStartTime} - ${strFinishTime}`;
                            }
                        }
                        else {
                            strSchedule =   `${strStartDateTime}
                                            <br/>
                                            ${strFinishDateTime}`;
                        }
                        return strSchedule;
                    }
                },                               
                // {
                //     field: "Location",
                //     title: "Location",
                // },
                // {
                //     headerAttributes: {
                //         "class": "text-center",
                //     },
                //     attributes: {
                //         "class": "text-center",
                //     },
                //     title: "Type", 
                //     template: function (d) {                        
                //         if(!!d.SubTypeDescription && !!d.TypeDescription){
                //             return `${d.TypeDescription} - ${d.SubTypeDescription}`;
                //         }else{
                //             return `${d.TypeDescription}`;
                //         }                    
                //     }
                // },                
                {
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
                    field: "RegistrationDeadline",
                    title: "Deadline", 
                    template: function (d) {                        
                        return standarizeDateTime(d.RegistrationDeadline);
                    }
                },
                {
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },                           
                    template: function (data) {        
                        var status = data.TrainingStatus;
                        let statusClass = "secondary";
                        let statusLabel = camelToTitle(data.TrainingStatusDescription);
                        
                        switch (status) {
                            case 3:                                    
                                statusClass = "warning";
                                break;
                            case 1:
                            case 4:
                                statusClass = "info";
                                break;
                            case 2:
                                statusClass = "danger";
                                break;                                
                        };
                        return `<span class="badge badge-${statusClass}">${statusLabel.toLowerCase()}</span>`;                            
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
                    template: function (data) {                          
                        if (!!data.TrainingRegistration) {
                            var status = data.TrainingRegistration.RegistrationStatus;
                            let statusClass = "secondary";
                            let statusLabel = camelToTitle(data.TrainingRegistration.RegistrationStatusDescription);
                            
                            switch (status) {
                                case 0:                                    
                                    statusClass = "warning";
                                    break;
                                case 1:
                                    statusClass = "info";
                                    break;
                                case 2:
                                    statusClass = "success";
                                    break;
                                case 3:
                                    statusClass = "success";
                                    break;
                                case 4:
                                    statusClass = "warning";
                                    break;
                                case 5:
                                    statusClass = "danger";
                                    break;
                                case 6:
                                    break;
                            };
                            return `<span class="badge badge-${statusClass}">${statusLabel.toLowerCase()}</span>`;
                        }else {
                            var disabled = (data.TrainingStatus != 1 && data.TrainingStatus != 4);
                            return `                            
                            <button class="btn btn-xs ${(disabled)? 'btn-outline-secondary': 'btn-outline-primary'}" onclick="model.action.requestJoin('${data.uid}'); return false;" ${(disabled)?'disabled':''}>
                                <i class="mdi mdi-google-circles-group"></i> Join
                            </button>
                        `; 
                        }                        
                    },
                    width: 100,
                },                
            ]
        });
    }
}

model.render.gridHistory = function () {
    let self = model;
    let $el = $("#gridHistory");
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
                        url: "/ESS/Training/GetHistory",
                        dataType: "json",
                        type: "POST",
                        contentType: "application/json",
                    },
                    parameterMap: function (data, type) {
                        data.EmployeeID = ""
                        var _start = model.data.StartDate();
                        var _end = model.data.EndDate();

                        if(_start > _end){
                            data.Range = {
                                Start: _end,
                                Finish: _start
                            }
                        }else{
                            data.Range = {
                                Start: _start,
                                Finish: _end
                            }
                        }
                        
                        data.Status = model.data.trainingStatus() || -1;
                        for(var i in data)
                        return JSON.stringify(data);
                    }
                },
                schema: {
                    data: function (res) {

                        if (res.StatusCode !== 200 && res.Status !== '') {
                            swalFatal("Fatal Error", `Error occured while fetching training(s)\n${res.Message}`)
                            return []
                        }
                        return res.Data || [];
                    },
                    total: "Total",
                },
                error: function (e) {
                    swalFatal("Fatal Error", `Error occured while fetching training(s)\n${e.xhr.responseText}`)
                },
                // sort: { field: "Schedule.StartDate", dir: "desc" },                
            },            
            pageable: {
                previousNext: false,
                info: false,
                numeric: false,
                refresh: true
            },
            noRecords: {
                template: "No training  data available."
            },
            columns: [                
                {
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
                    field: "TrainingID",
                    title: "#",
                    width: 100,                    
                },
                {
                    field: "Name",                    
                    title: "Training",                    
                    width: 300,
                    template:function(d){
                        var str = `
                        <strong class="d-block">${d.Name}</strong>                        
                        <small class="d-block">${d.Location}</small>
                        `;
                        if(!!d.SubTypeDescription || !!d.TypeDescription){
                            str += `<small class="d-block">${(!!d.SubTypeDescription && !!d.TypeDescription)?d.TypeDescription+' - '+d.SubTypeDescription:d.TypeDescription}</small>`;
                        }
                        return str;
                    }
                },
                {
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
                    title: "Schedule", width: 250,
                    template: function (d) {
                        let startTime = standarizeTime(d.Schedule.Start);
                        let finishTime = standarizeTime(d.Schedule.Finish);
                        let strStartDate = standarizeDate(d.Schedule.Start);                        
                        let strFinishDate = standarizeDate(d.Schedule.Finish);
                        let strStartDateTime = standarizeDateTime(d.Schedule.Start);
                        let strFinishDateTime = standarizeDateTime(d.Schedule.Finish);
                        let strSchedule = "";

                        if(startTime == "00:00") strStartDateTime = strStartDate;
                        if(finishTime == "00:00") strFinishDateTime = strFinishDate;

                        if (strStartDate == strFinishDate) {
                            var strStartTime = standarizeTime(d.Schedule.Start);
                            var strFinishTime = standarizeTime(d.Schedule.Finish);

                            if (strStartDateTime == strFinishDateTime) {
                                strSchedule = `${strStartDate} at ${strStartTime}`;
                            }else{
                                strSchedule = `${strStartDate} at ${strStartTime} - ${strFinishTime}`;
                            }
                        }
                        else {
                            strSchedule =   `${strStartDateTime}
                                            <br/>
                                            ${strFinishDateTime}`;
                        }
                        return strSchedule;
                    }
                },                               
                // {
                //     field: "Location",
                //     title: "Location",
                // },
                // {
                //     headerAttributes: {
                //         "class": "text-center",
                //     },
                //     attributes: {
                //         "class": "text-center",
                //     },
                //     title: "Type", 
                //     template: function (d) {                        
                //         if(!!d.SubTypeDescription && !!d.TypeDescription){
                //             return `${d.TypeDescription} - ${d.SubTypeDescription}`;
                //         }else{
                //             return `${d.TypeDescription}`;
                //         }                    
                //     }
                // },                
                // {
                //     headerAttributes: {
                //         "class": "text-center",
                //     },
                //     attributes: {
                //         "class": "text-center",
                //     },
                //     field: "RegistrationDeadline",
                //     title: "Deadline", 
                //     template: function (d) {                        
                //         return standarizeDateTime(d.RegistrationDeadline);
                //     }
                // },
                {
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },                           
                    template: function (data) {        
                        var status = data.TrainingStatus;
                        let statusClass = "secondary";
                        let statusLabel = camelToTitle(data.TrainingStatusDescription);
                        
                        switch (status) {
                            case 3:                                    
                                statusClass = "warning";
                                break;
                            case 1:
                            case 4:
                                statusClass = "info";
                                break;
                            case 2:
                                statusClass = "danger";
                                break;                                
                        };
                        return `<span class="badge badge-${statusClass}">${statusLabel.toLowerCase()}</span>`;                            
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
                    template: function (data) {        
                        data.TrainingRegistration = data.TrainingRegistration || {};
                        var status = data.TrainingRegistration.RegistrationStatus;
                        let statusClass = "secondary";
                        let statusLabel = camelToTitle(data.TrainingRegistration.RegistrationStatusDescription || "NotAvailable");
                        
                        switch (status) {
                            case 0:                                    
                                statusClass = "warning";
                                break;
                            case 1:
                                statusClass = "info";
                                break;
                            case 2:
                                statusClass = "success";
                                break;
                            case 3:
                                statusClass = "success";
                                break;
                            case 4:
                                statusClass = "warning";
                                break;
                            case 5:
                                statusClass = "danger";
                                break;
                            case 6:
                                break;
                        };
                        return `<span class="badge badge-${statusClass}">${statusLabel.toLowerCase()}</span>`;
                         
                    },
                    width: 100,
                },                
            ]
        });
    }
}

let _gridCache = null;
model.render.gridReference = function () {
    let $el = $("#gridReference");    

    if ($el) {
        let $grid = $el.getKendoGrid();
        if (!!$grid) {
            $grid.destroy();
        }
        $el.kendoGrid({
            dataSource: {
                transport: {
                    read: function (opt) {
                        if (!_gridCache) {
                            $.ajax({
                                url: "/ESS/Training/GetReferences",
                                dataType: "json",
                                type: "POST",
                                contentType: "application/json",
                                success: function (result) {
                                    _gridCache = _.cloneDeep(result);
                                    opt.success(result);
                                },
                                error: function (result) {
                                    opt.error(result);
                                }
                            });
                        } else {
                            opt.success(_.cloneDeep(_gridCache));
                        }       
                    },
                },
                schema: {
                    data: 
                        function (res) {
                        console.log("###", res.Data);
                        if (res.StatusCode !== 200 && res.Status !== '') {
                            swalFatal("Training References", `Error occured while fetching training reference(s)\n${res.Message}`)
                            return []
                        }                        
                        return res.Data || [];
                    },
                    model: {
                        id: "AXID"
                    }
                },
                error: function (e) {
                    swalFatal("Training References", `Error occured while fetching training reference(s)\n${e.xhr.responseText}`)
                },
                group: {
                    field: "TypeDescription",
                    dir: "desc"
                },
                sort: {
                    field: "Description",
                    dir: "asc"
                },
            },
            pageable: {
                previousNext: false,
                info: false,
                numeric: false,
                refresh: true
            },
            filterable: {
                operators: {
                    string: {
                        eq: "Is Equal to",
                        contains: "Contains",
                    }
                },
                extra: false
            },
            persistSelection: true,
            change: model.on.gridReferenceChange,
            noRecords: {
                template: "No training  data available."
            },
            columns: [
                { selectable: true, width: "50px" },
                {
                    field: "Description",
                    title: "Description",
                    filterable: true,
                },
                {
                    title: "Validity",
                    template: function (d) {
                        if (!d.Validity) return 'N/A';

                        let vStart = standarizeDate(d.Validity.Start)
                        let vEnd = standarizeDate(d.Validity.Finish)
                        let vFull = "";

                        if (vStart == "-" && vEnd == "-") {
                            vFull = "-";
                        } else if (vStart == "-") {
                            vFull = `N/A - ${vEnd}`;
                        } else if (vEnd == "-") {
                            vFull = `${vStart} - N/A`;                            
                        } else {
                            vFull = `${vStart} - ${vEnd}`;
                        }

                        return vFull;
                    },
                },
            ],
            dataBound: function (e) {
                var $grid = this;
                var collapse = false;
                $grid.content.find(".k-grouping-row").each(function (e) {
                    $grid.collapseGroup(this);                    
                });
            }
        });
    }
}

model.on.renderFormTraining = function () {
    let self = model;    
    self.render.gridReference();
};

model.on.gridReferenceChange = function (d) {
    var self = model;
    var data = this.dataSource.data() || [];
    var AXIDs = this.selectedKeyNames();
    var selectedData = data.filter(x => AXIDs.indexOf(x.AXID + "") > -1);   
    self.data.TrainingRegistration().References(selectedData);
}

model.action.requestJoin = function (uid) {
    let self = model;

    self.data.TrainingRegistration(model.newTrainingRegistration());
    let dataGrid = $("#gridTraining").data("kendoGrid").dataSource.getByUid(uid);
    if (dataGrid) {
        self.data.Training(model.newTraining(dataGrid));
        $("#modalTraining").modal("show");
    }
}

model.action.refreshGridTraining = function (uiOnly = false) {
    var $grid = $("#gridTraining").data("kendoGrid");
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

model.action.join = async function () {
    let self = model;
    let training = ko.toJS(self.data.Training());
    let registration = ko.toJS(self.data.TrainingRegistration());

    registration.TrainingID = training.TrainingID;
    registration.Training = training;
    let confirmResult = await swalConfirm("Training Registration", `Are you sure registering yourself into training ${training.Name || training.TrainingID} ?`);
    if (confirmResult.value) {
        try {
            isLoading(true);           
            $model = $("#modalTraining");
            ajaxPost("/ESS/Training/Register", registration, function (data) {
                if (data.StatusCode == 200) {
                    $model.modal("hide");
                    swalSuccess("Training Registration", data.Message);
                    model.action.refreshGridTraining();
                } else {
                    $model.modal("hide");
                    swalError("Training Registration", data.Message);
                }
                isLoading(false);
            }, function (data) {
                isLoading(false);
                $model.modal("hide");
                swalFatal("Training Registration", data.Message);
            })
        } catch (e) {
            swalFatal("Medical Benefit", e);
            isLoading(false);
        }
    }
}

model.init.training = function () {
    var self = model;

    let activatedTabMap = {}; https://localhost:8003/ESS/Employee/Profile
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
                    model.is.filterHidden(false);
                    model.render.gridTraining();                    
                    break;
                case '#history':
                    model.is.filterHidden(true);
                    model.render.gridHistory();
                    break;                
                default:
                    break;
            };

            activatedTabMap[target] = true;
        }else{
            switch (target) {
                case '#open':
                    model.is.filterHidden(false);                    
                    break;
                case '#history':
                    model.is.filterHidden(true);                    
                    break;                
                default:
                    break;
            };
        }
    })
    $('a[data-toggle="tab"]:first-child').tab('show');
}
