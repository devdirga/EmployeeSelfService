//var currentId = 1
var today = new Date()
model.data.StartDate = ko.observable(moment().subtract(7, "days").toDate())
model.data.EndDate = ko.observable(today)

model.newTicket = function (obj) {
    let Ticket = _.clone(this.proto.Ticket);
    if (obj && typeof obj == "object") {
        Ticket = Object.assign(Ticket, _.clone(obj));
    }
    Ticket.Category = Ticket.Category || _.clone(this.proto.TicketCategory || {});
    Ticket.EmailTo = Ticket.EmailTo || [];

    if (Ticket.EmailCC !== '' && Ticket.EmailCC !== null) {
        Ticket.EmailCC = JSON.parse(Ticket.EmailCC)
        Ticket.EmailCC = Ticket.EmailCC.join(", ")
    }

    if (Ticket.EmailTo !== '' && Ticket.EmailTo !== null) {       
        Ticket.EmailTo = Ticket.EmailTo.join(", ")
    }

    return ko.mapping.fromJS(Ticket);
}

model.data.Ticket = ko.observable(model.newTicket())

model.list.TicketType = ko.observableArray([])
model.list.TicketStatus = ko.observableArray([])
model.list.TicketMedia = ko.observableArray([])
model.list.TicketCategories = ko.observableArray([])

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

model.render.GridTicket = function () {
    let self = model;
    let $el = $("#gridListTicket");

    if ($el) {
        let $grid = $el.getKendoGrid();
        if (!!$grid) {
            $grid.destroy();
        }
        $el.kendoGrid({
            dataSource: {
                transport: {
                    read: {
                        url: "/ESS/Complaint/Get",
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
                        data.Status = -1
                        return JSON.stringify(data)
                    }
                },
                schema: {
                    data: function (res) {

                        if (res.StatusCode !== 200 && res.Status !== '') {
                            swalFatal("Fatal Error", `Error occured while fetching complaint request(s)\n${res.Message}`)
                            return []
                        }
                        return res.Data || [];
                    },
                    total: "Total",
                },
                pageSize: 10,
                serverPaging: true,
                serverFiltering: true,
                sort: { field: "CreatedDate", dir: "asc" },
                serverSorting: true,
                error: function (e) {
                    swalFatal("Fatal Error", `Error occured while fetching complaint request(s)\n${e.xhr.responseText}`)
                }                
            },
            pageable: {
                input: false,
                numeric: true,
                previousNext: true,
                butonCount: 5,
                pageSizes: [10, 50, 100],
                refresh: true,
            },
            sortable: true,
            filterable: {
                operators: {
                    string: {
                        eq: "Is Equal to",
                        contains: "Contains",
                    }
                },
                extra: false
            },
            noRecords: {
                template: "No ticket(s) data available."
            },
            columns: [
                {
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
                    field: "TicketID",
                    title: "#",
                    width: 150
                },                
                {                    
                    field: "Subject",
                    title: "Subject",
                    width: 200,
                    template: function(d){
                        return `${d.Subject}
                        <small class="d-block">
                            <strong>Category: </strong>${d.Category.Name}
                        </small>                        
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
                    field: "TicketType",
                    title: "Type",
                    width: 125,
                    filterable: {
                        ui: model.action.TicketTypeFilter
                    },
                    template: function (d) {    
                        // Ticket Status                    
                        var ticketTypeString = camelToTitle(model.map.TicketType[d.TicketType] || "");
                        ticketStatusClass =model.map.ticketStatusCSS[d.TicketType];                        

                        var badgeTicketStatus = `<span class="badge badge-${ticketStatusClass}">${ticketTypeString.toLowerCase()}</span>`;
                        if(d.TicketType == 0){
                            badgeTicketStatus = `<a href="#" onclick="model.app.action.trackTaskEmployee('${d.AXRequestID}','${d.EmployeeID}'); return false;">${badgeTicketStatus}</a>`;                            
                        }

                        // Request Status
                        var statusString = camelToTitle(d.InvertedStatusDescription || "");
                        statusClass = model.map.StatusLabelCSS[d.InvertedStatus];

                        var badgeStatus = `<span class="badge badge-${statusClass}">${statusString.toLowerCase()}</span>`;
                        if(d.TicketType == 0){
                            badgeStatus= `<a href="#" onclick="model.app.action.trackTaskEmployee('${d.AXRequestID}','${d.EmployeeID}'); return false;">${badgeStatus}</a>`;
                        }
                        
                        if(d.TicketType == 0){
                            return `${badgeTicketStatus}<br/>${badgeStatus}`;
                        }else{
                            return badgeTicketStatus;
                        }
                    }
                },
                {
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
                    field: "TicketStatus",
                    title: "Status",
                    width: 100,
                    filterable: {
                        ui: model.action.TicketStatusFilter
                    },
                    template: function (d) {
                        let statusClass = "secondary";
                        let statusLabel = (self.map.TicketStatus[d.TicketStatus]) ? self.map.TicketStatus[d.TicketStatus] : "-";
                        switch (d.TicketStatus) {
                            case "0":
                            case 0:
                                statusClass = "info";
                                break;
                            case "1":
                            case 1:
                                statusClass = "primary";
                                break;
                            case "2":
                            case 2:
                                statusClass = "success";
                                break;
                            default:
                                break;
                        }
                        var badge = `<span class="badge badge-${statusClass}">${statusLabel.toLowerCase()}</span>`;
                        if(d.TicketType == 0){
                            return `<a href="#" onclick="model.app.action.trackTaskEmployee('${d.AXRequestID}','${d.EmployeeID}'); return false;">${badge}</a>`;
                        }
                        return badge;
                    }
                },
                {
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
                    field: "CreatedDate",
                    title: "Open ticket date",
                    width: 150,
                    filterable: {
                        ui: function (element) {
                            element.kendoDatePicker({
                                format: "MMM dd, yyyy"
                            });
                        }
                    },
                    template: function (d) {
                        return standarizeDateTime(d.CreatedDate, true);
                    }
                },
                {
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
                    field: "ClosedDate",
                    title: "Close ticket date",
                    width: 150,
                    template: function (d) {
                        return standarizeDateTime(d.ClosedDate, true)
                    }
                },
                {
                    template: function (d) {
                        return `<button class="btn btn-xs btn-outline-info" onclick="model.action.OpenTicketDetail('${d.uid}'); return false;">
                                    <i class="fa mdi mdi-eye"></i>
                                </button>`
                    },
                    width: 50,
                }
            ]
        })
    }
}


model.action.TicketTypeFilter = function (element) {
    element.kendoDropDownList({
        dataSource: model.list.TicketType(),
        dataTextField: 'text',
        dataValueField: 'value',
        optionLabel: "Select Type"
    });
}

model.action.TicketStatusFilter = function (element) {
    element.kendoDropDownList({
        dataSource: model.list.TicketStatus(),
        dataTextField: 'text',
        dataValueField: 'value',
        optionLabel: "Select Status"
    });
}

model.on.categoryChange = function(e){
    var val = this.value();
    var data = this.dataSource.data();
    var selectedData = data.find(d=>{
        return d.Id == val;
    });

    if(selectedData){
        selectedData = selectedData._raw();
        var emails = selectedData.Contacts.map(d=>{return d.Email});
        model.data.Ticket().EmailTo(emails);
    }else{
        model.data.Ticket().EmailTo([]);
    }
}

model.action.RefreshTicket = function (UIOnly = false) {
    var $grid = $("#gridListTicket").data("kendoGrid")
    if ($grid) {
        if ($($grid.content).find(".k-loading-mask").length > 0) {
            return true
        }
        if (UIOnly) {
            $grid.refresh()
        } else {
            $grid.dataSource.read()
        }
    }
}

model.action.FilterTicketMonthly = function (UIOnly = false) {
    model.data.StartDate(moment().startOf('month').format("MM/DD/YYYY"));
    model.data.EndDate(today)
    var $grid = $("#gridListTicket").data("kendoGrid")
    if ($grid) {
        if ($($grid.content).find(".k-loading-mask").length > 0) {
            return true
        }
        if (UIOnly) {
            $grid.refresh()
        } else {
            $grid.dataSource.read()
        }
    }
}

model.action.FilterTicketYearly = function (UIOnly = false) {
    model.data.StartDate(moment().startOf('year').format("MM/DD/YYYY"))
    model.data.EndDate(today)
    var $grid = $("#gridListTicket").data("kendoGrid")
    if ($grid) {
        if ($($grid.content).find(".k-loading-mask").length > 0) {
            return true
        }
        if (UIOnly) {
            $grid.refresh()
        } else {
            $grid.dataSource.read();
        }
    }
}

model.action.OpenTicket = function () {
    let self = model
    self.data.Ticket(model.newTicket())
    self.data.Ticket().EmailFrom(model.app.config.employeeEmail)
    self.data.Ticket().FullName(model.app.config.employeeName)
    $("#OpenTicketModal").modal("show")
}

model.action.TicketRequest = function () {
    let self = model;
    let dialogTitle = "Ticket Request";    

    let data = ko.mapping.toJS(model.data.Ticket());
    data.TicketDate = new Date();
    
    if(!!data.Category && !!data.Category.Id){
        data.Category = model.map.TicketCategories[data.Category.Id];
    }else{
        swalAlert(dialogTitle, 'Category is required.');
        return;
    }    

    if (!(data.TicketType || "").trim() && data.TicketType != 0) {
        swalAlert(dialogTitle, 'Type is required.');
        return;
    }

    if (!(data.TicketMedia || "").trim() && data.TicketMedia != 0) {
        swalAlert(dialogTitle, 'Media is required.');
        return;
    }

    if (!(data.Subject || "").trim()) {
        swalAlert(dialogTitle, 'Subject is required.');
        return;
    }

    if (!(data.EmailFrom || "").trim()) {
        swalAlert(dialogTitle, 'Email from is required.');
        return;
    }

    // if (!(data.EmailCC || "").trim() || data.EmailCC) {
    //     swalAlert(dialogTitle, 'Email CC is required.');
    //     return;
    // }

    if (!(data.FullName || "").trim()) {
        swalAlert(dialogTitle, 'FullName is required.');
        return;
    }

    if (!(data.Description || "").trim()) {
        swalAlert(dialogTitle, 'Description is required.');
        return;
    }

    let formData = new FormData();
    formData.append("JsonData", JSON.stringify(data));

    var files = $('#Filepath').getKendoUpload().getFiles();

    if (files.length > 0) {
        formData.append("FileUpload", files[0].rawFile);
    } else {
        swalAlert(dialogTitle, "Document attachment could not be empty");
        return;
    }

    try {
        isLoading(true);
        ajaxPostUpload("/ESS/Complaint/Request", formData, function (data) {
            isLoading(false);
            if (data.StatusCode == 200) {
                swalSuccess(dialogTitle, data.Message);
                $("#OpenTicketModal").modal("hide");
                model.action.RefreshTicket();
            } else {
                swalFatal(dialogTitle, data.Message);
            }
        }, function (data) {
            isLoading(false);
            swalFatal(dialogTitle, data.Message);
        })
    } catch (e) {
        isLoading(false);
    }

    return false;
}

model.action.OpenTicketDetail = function (uid) {
    dataGrid = $("#gridListTicket").data("kendoGrid").dataSource.getByUid(uid)
    if (dataGrid) {
        dataGrid = dataGrid._raw()
        model.data.Ticket(model.newTicket(dataGrid))
        $("#OpenTicketDetail").modal("show")
    }
}
///

var currentId = 1;
model.action.onDataBound = function () {
    //console.log('asda')
    $('.k-multiselect .k-input').unbind('keyup');
    $('.k-multiselect .k-input').on('keyup', model.action.onClickEnter);
    $('.k-multiselect .k-input').unbind('focusout');
    $('.k-multiselect .k-input').on('focusout', model.action.inputCC);
}
model.action.onClickEnter = function (e) {
    if (e.keyCode === 13) {
        model.action.inputCC(e);
    }
}

model.action.inputCC = function(e){
    var widget = $('#products').getKendoMultiSelect();
        var dataSource = widget.dataSource;
        var $result = $("#resultEmail");
        //console.log('asdad', dataSource)
        var input = $('.k-multiselect .k-input');
        var value = input.val().trim();
        if (!value || value.length === 0) {
            return;
        }
        if (model.action.validateEmail(value)) {
            var newItem = {
                ProductID: value,
                ProductName: value
            };

            dataSource.add(newItem);
            var newValue = newItem.ProductID;
            widget.value(widget.value().concat([newValue]));
            model.data.Ticket().EmailCC(JSON.stringify(widget.value()));    
            $result.text("");
            //$result.css("color", "green");
            //resultEmail
        } else {
            swalAlert("Complaint", `"${value}" is not valid email format`);
            // $result.text(value + " is not valid");
            // $result.css("color", "red");
        }
}

model.action.validateEmail = function (email) {
    var re = /^(([^<>()[\]\\.,;:\s@\"]+(\.[^<>()[\]\\.,;:\s@\"]+)*)|(\".+\"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/;
    return re.test(email);
}
//
model.action.print = function () {
    isLoading(true);
    $("#Pdf").removeClass("lang-en").addClass("lang-id");
    kendo.drawing.drawDOM($("#Pdf"), {
        scale: 0.65,
        paperSize: "A4",
        landscape: false,
        margin: { left: "0.5cm", top: "3.8cm", right: "0.5cm", bottom: "0.5cm" },
        template: $("#page-template").html()
    }).then(function (group) {
        kendo.drawing.pdf.saveAs(group, "Agrement.pdf");
        $("#Pdf").removeClass("lang-id").addClass("lang-en");
        isLoading(false);
    });
}


model.init.Ticket = function () {
    var self = model

    setTimeout(async function () {
        var data = await self.get.TicketType()
        data.forEach((d) => {
            model.map.TicketType[d.value] = d.text
        })
        model.list.TicketType(data)
    });

    setTimeout(async function () {
        var data = await self.get.TicketStatus()
        data.forEach((d) => {
            model.map.TicketStatus[d.value] = d.text
        })
        model.list.TicketStatus(data)
    });

    setTimeout(async function () {
        var data = await self.get.TicketMedia()
        data.forEach((d) => {
            model.map.TicketMedia[d.value] = d.text
        })
        model.list.TicketMedia(data)
    });

    setTimeout(async function () {
        var data = await self.get.TicketStatusLabelCss()
        data.forEach((d) => {
            model.map.TicketStatusLabelCss[d.value] = d.text
        })
    });

    setTimeout(async function () {
        var data = await self.get.TicketCategories()
        model.list.TicketCategories(data)
        data.forEach((d) => {
            model.map.TicketCategories[d.Id] = d;
        })
    });


    setTimeout(async function () {
        self.action.RefreshTicket(true)
        model.render.GridTicket()
    })
}