model.newRedeem = function (data) {
    if (data) {
        return ko.mapping.fromJS(Object.assign(_.clone(this.proto.Redeem), _.clone(data)));
    }
    return ko.mapping.fromJS(this.proto.Redeem);
};

model.newCanteen = function (data) {
    let canteen = _.clone(this.proto.Canteen);

    if (!canteen) return;

    if (data) {
        canteen = Object.assign(canteen, _.clone(data));
    }
    var user = _.clone(canteen.User || this.proto.User);
    canteen.User = null;
    var result = ko.mapping.fromJS(canteen);
    result.User(ko.mapping.fromJS(user));
    return result;
};

model.newMenuMerchant = function (obj) {
  var proto = _.clone(this.proto.MenuMerchant);
  return ko.mapping.fromJS(proto);
}

model.is.buttonLoading = ko.observable(false);
model.is.buttonVisible = ko.observable(true);
model.is.userInfoLoading = ko.observable(false);
model.is.showUserInfo = ko.observable(false);
model.is.btnSaveClaim = ko.observable(false);
model.is.btnSavePaid = ko.observable(false);

model.data.StartDate = ko.observable(moment().subtract(1, "days").toDate());
//model.data.StartDate = ko.observable(moment().subtract(1, "days").toDate());
model.data.StartDate = ko.observable(moment().startOf('month').toDate());
model.data.EndDate = ko.observable(new Date());
model.data.TotalRedeemed = ko.observable(0);
model.data.TotalEmployee = ko.observable(0);
model.data.TotalTransaction = ko.observable(0);
model.data.canteen = ko.observable(model.newCanteen());
model.data.redeem = ko.observable(model.newRedeem());
model.data.info = ko.observable({
    VoucherRemaining:0,
    VoucherUsed:0,
    VoucherExpired:0,
    VoucherAlmostExpired: 0
});

model.data.infoClaim = ko.observable({
    TotalClaimed: ko.observable(),
    TotalPaid: ko.observable(),
});
model.data.statusClaim = ko.observable();

model.list.voucherUsed = ko.observableArray([]);
model.list.canteen = ko.observableArray([]);
model.list.canteenUserType = ko.observableArray([]);
model.list.user = new kendo.data.DataSource({
    type: "json",
    serverFiltering: true,
    transport: {
        read: {
            url: "/ESS/User/GetData",
            dataType: "json",
            type: "POST",
            contentType: "application/json",
        },
        parameterMap: function(data, type) {
            data = Object.assign(data||{}, {"take":20,"skip":0})
            if(!!data.filter && !!data.filter.filters && data.filter.filters.length >0){
                var filters = data.filter.filters;
                var id = filters.findIndex(x=>{
                    return x.field == "_text";
                });
                if(id > -1){
                    var oldFilter = _.clone(filters[id]);
                    var fields = ["FullName","Username"];
                    filters = [];
                    fields.forEach(d=>{                        
                        filters.push(Object.assign(_.clone(oldFilter), {field:d}));
                    });
                    data.filter.filters = filters;                    
                    data.filter.logic = "or";
                }
                
            }            
            return JSON.stringify(data);
        }        
    },
    schema: {
        data: function (res) {
            if (res.StatusCode !== 200 && res.Status !== '') {
                swalFatal("Fatal Error", `Error occured while fetching user(s)\n${res.Message}`)
                return []
            }
            var data = res.Data || [];
            data.forEach(d=>{
                d._text = `${d.FullName} (${d.Username})`;
            });

            return data;
        },
        total: "Total",
    },
    error: function (e) {
        swalFatal("Fatal Error", `Error occured while fetching user(s)\n${e.xhr.responseText}`)
    },
    sort: { field: "Fullname", dir: "asc" },
});
model.list.redeembydate = ko.observableArray([]);
model.list.statusClaim = ko.observableArray([{ Name: 'Claimed', Value: 1 }, { Name: 'Paid', Value: 2 }]);

model.map.canteen = {};
model.data.menus = ko.observableArray()

model.on.canteenUserTypeChange = function(e){
    var self = model;
    var value = this.value();
    // self.data.canteen().User(ko.mapping.fromJS(self.proto.User));
};

model.on.roleChange = function(e){
    var self = model;
    var value = this.value();
    self.data.canteen().User().Roles([value]);
};

model.on.userChange = function(e){
    var self = model;
    var value = this.value();
    
    var result = this.dataSource.data().find(x=>{
        return x.Id == value;
    });

    if(!!result){
        var user = result._raw();
        var role = (user.Roles || [])[0];
        user.RoleDescription = !!role ? ((self.map.role[role]||{}).Name || '-') : '-';
        self.data.canteen().User(ko.mapping.fromJS(user));      
    }
};

model.get.canteen = async function () {
    let response = await ajax("/ESS/Canteen/Get", "GET");
    if (response.StatusCode == 200) {        
        return response.Data || [];
    }
    console.error(response.StatusCode, ":", response.Message)
    return Object.assign({}, model.proto.Redeem);
}

model.get.role = async function () {
    let response = await ajax("/ESS/Group/GetData", "GET");
    if (response.StatusCode == 200) {        
        return response.Data || [];
    }
    console.error(response.StatusCode, ":", response.Message)
    return Object.assign({}, model.proto.Employee);
}

model.get.user = async function (canteenID) {
    let response = await ajax("/ESS/Canteen/GetUserDetail/"+canteenID, "GET");
    if (response.StatusCode == 200) {        
        return response.Data || [];
    }
    console.error(response.StatusCode, ":", response.Message)
    return Object.assign({}, model.proto.Employee);
}

model.action.refreshReport = function (uiOnly = false) {

    if (model.data.StartDate() > model.data.EndDate()) {
        swalError("Warning", "start date is greater than the end date");
        return false;
    };

    var $grid = $("#gridReportCanteen").data("kendoGrid");
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

model.action.redeemVoucher = function () {
    // Redeem Form
    let self = model;
    self.data.redeem(model.newRedeem());        
    self.data.redeem().CurrentTotal(self.data.info().VoucherRemaining);            
    $("#ModalOrder").modal("show");
}

model.action.addReedemVoucher = function (data) {
    var total = parseInt(data.RedeemedVoucherTotal());
    if (total == parseInt(data.CurrentTotal())) {
        return;
    }
    data.RedeemedVoucherTotal(total+1);
}

model.action.substractReedemVoucher = function (data) {
    var total = parseInt(data.RedeemedVoucherTotal());
    if (total == 0) {
        return;
    }
    data.RedeemedVoucherTotal(total - 1);
}

model.list.canteen.subscribe(function () {
    model.action.refreshGridHistory(true);
});

model.render.gridHistory = function () {
    var x = $("#gridHistory").data("kendoGrid");
    if (x) {
        x.destroy();
    }
    var grid = $("#gridHistory").data("kendoGrid");
    if (grid) {
        grid.destroy();
        
    }
    var options = {
        dataSource: {
            //serverPaging: true,
            //serverSorting: true,
            //pageSize: 5,
            transport: {
                read: {
                    url: "/ESS/Canteen/EmployeeRedeemHistory",
                    dataType: "json",
                    type: "POST",
                    contentType: "application/json",
                },
                parameterMap: function (data, type) {
                    //data.EmployeeID = ""
                    //data.Range = {
                    //    Start: model.data.StartDate(),
                    //    Finish: model.data.EndDate()
                    //}
                    return JSON.stringify(data);
                }
            },
            schema: {
                data: function (res) {
                    if (res.StatusCode !== 200 && res.Status !== '') {
                        swalFatal("Fatal Error", `Error occured while fetching redeem history\n${res.Message}`)
                        return []
                    }

                    return res.Data || [];
                },
                total: "Total",
            },
        },
        pageable: {
            previousNext: false,
            info: false,
            numeric: false,
            refresh: true
        },
        noRecords: {
            template: "No redeem history data available."
        },
        //sortable: true,
        columns: [
            {
                title: "Canteen",
                template: function (data) {
                    var canteen = model.map.canteen[data.CanteenID];
                    if (!canteen) {
                        return data.CanteenName;
                    } else {
                        return canteen.Name;
                    }
                    
                },
                width: 200
            },
            {
                field: "RedeemedVoucherTotal",
                title: "Total Redeem",
                headerAttributes: {
                    "class": "text-center",
                },
                attributes: {
                    "class": "text-center",
                },
                width: 200
            },
            //{ field: "CurrentTotal", title: "Current Total", witdh: 120 },
            {
                field: "RedeemedAt", title: "Date", template: function (data) {
                    return standarizeDateTime(data.RedeemedAt);
                },
                width: 200
            }
        ],
    }
    $("#gridHistory").kendoGrid(options);
}

model.render.gridCanteen = function () {
    var grid = $("#gridCanteen").data("kendoGrid");
    if (grid) {
        grid.destroy();
    }

    var options = {
        dataSource: {
            //serverPaging: true,
            //serverSorting: true,
            //pageSize: 5,
            transport: {
                read: {
                    url: "/ESS/Canteen/Get",
                    dataType: "json",
                    type: "POST",
                    contentType: "application/json",
                },
                parameterMap: function (data, type) {
                    //data.EmployeeID = ""
                    //data.Range = {
                    //    Start: model.data.StartDate(),
                    //    Finish: model.data.EndDate()
                    //}
                    return JSON.stringify(data);
                }
            },
            schema: {
                data: function (res) {
                    if (res.StatusCode !== 200 && res.Status !== '') {
                        swalFatal("Fatal Error", `Error occured while fetching canteen(s)\n${res.Message}`)
                        return []
                    }
                    
                    return res.Data || [];
                },
                total: "Total",
            },
        },
        noRecords: {
            template: "No canteen data available."
        },
        pageable: {
            previousNext: false,
            info: false,
            numeric: false,
            refresh: true
        },
        //sortable: true,
        columns: [
            {                
                headerAttributes: {
                    "class": "text-center",
                },
                attributes: {
                    "class": "text-center",
                },
                template: function (x) {                    
                    var fallbackImage = "/assets/img/blank-user.png";
                    var img = `<div class="canteen-image" style="width:48px; height:48px; display:block; margin:auto; background: url('/ESS/Canteen/Image/${x.Id}/${x.Filename}') no-repeat, url('${fallbackImage}') no-repeat; background-size:cover, contain"></div>`;
                    return img;
                },
                width: 100
            },
            {
                field: "Name",
                title: "Name",
                width: 250
            },
            {
                field: "Address",
                title: "Address",
                width: 250
            },
            {
                field: "Phone",
                title: "Phone",
                width: 200
            },
            {
                field: "PICName",
                title: "PICName",
                width: 150
            },
            // {
            //     field: "Email",
            //     title: "Email",
            // },
            // {
            //     field: "Username",
            //     title: "Username",
            // },
            {
                headerAttributes: {
                    "class": "text-center",
                },
                attributes: {
                    "class": "text-center",
                },
                template: function (d) {
                    var btnEdit = `
                    <button type="button" class="btn btn-xs btn-outline-info" onclick="model.action.editCanteen('${d.uid}')">
                            <i class="fa mdi mdi-pencil"></i>
                    </button>`;

                        var btnDelete = `
                    <button type="button" class="btn btn-xs btn-outline-danger" onclick="model.action.deleteCanteen('${d.uid}')">
                            <i class="fa mdi mdi-delete"></i>
                    </button>`;

                    return btnEdit + " " + btnDelete;
                },
                width: 100
            }
        ]
    };
    $("#gridCanteen").kendoGrid(options);    
}

model.render.gridReport = function () {
    var options = {
        dataSource: {
            //serverPaging: true,
            //serverSorting: true,
            //pageSize: 5,
            transport: {
                read: {
                    url: "/ESS/Canteen/CanteenRedeemHistory",
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
                    var data = res.Data || [];
                    var total = 0;
                    var employees = [];
                    data.forEach(d => {
                        total += (d.RedeemedVoucherTotal || 0);
                        if (employees.indexOf(d.EmployeeID)) employees.push(d.EmployeeID);
                    })
                    model.data.TotalRedeemed(total);
                    model.data.TotalEmployee(employees.length);
                    model.data.TotalTransaction(data.length);
                    return data;
                },
                total: "Total",
            },
            aggregate: [{ field: "RedeemedVoucherTotal", aggregate: "sum" }]
        },
        //sortable: true,
        columns: [
            {
                title: "#",
                width: 80,
                headerAttributes: {
                    "class": "text-center",
                },
                attributes: {
                    "class": "text-center",
                },
                template: function (data) {                    
                    return `<div class="grid-img" style="background:${model.app.getProfilePicture(data.EmployeeID)}"></div>`;
                },
            },
            {
                field: "EmployeeID",
                title: "Employee ID",
                width: 200
            },
            {
                field: "EmployeeName",
                title: "Name",
                width: 200
            },
            {
                field: "RedeemedVoucherTotal",
                title: "Voucher",
                width: 150,
                headerAttributes: {
                    "class": "text-center",
                },
                attributes: {
                    "class": "text-center",
                },
                aggregates: ["sum"],
                footerTemplate: function (d) {
                    return d.RedeemedVoucherTotal.sum;
                },
                footerAttributes: {
                    "class": "table-footer-cell text-center",
                }
            },
            //{ field: "CurrentTotal", title: "Current Total", witdh: 120 },
            {
                field: "RedeemedAt",
                width: 100,
                title: "Date",
                template: function (data) {
                    return standarizeDateTime(data.RedeemedAt);
                },
            }
        ],
        pageable: {
            previousNext: false,
            info: false,
            numeric: false,
            refresh: true
        },
        noRecords: {
            template: "No redeem history data available."
        },
    }
    $("#gridReportCanteen").kendoGrid(options);
}

model.action.refreshGridCanteen = function (uiOnly = false) {
    var $grid = $("#gridCanteen").data("kendoGrid");
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

model.action.refreshGridHistory = function (uiOnly = false) {
    var $grid = $("#gridHistory").data("kendoGrid");
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

model.action.redeem = async function () {
    let self = model;
    let dialogTitle = "Voucher Redeem";
    let param = ko.mapping.toJS(self.data.redeem);    
    
    if (!param.CanteenID) {
        swalAlert(dialogTitle, "Select canteen please");
        return;
    }

    if (parseInt(param.RedeemedVoucherTotal) <= 0) {
        swalAlert(dialogTitle, "Total redeem voucher should be more than 0");
        return;
    }

    let canteen = self.map.canteen[param.CanteenID];
    let strCanteen = "";
    if (canteen) {
        param.CanteenName = canteen.Name;
        strCanteen = ` for ${param.CanteenName}`
    }

    let confirmResult = await swalConfirm(dialogTitle, `Are you sure to redeem <b>${param.RedeemedVoucherTotal} of ${param.CurrentTotal}</b> your voucher${strCanteen} ?`);
    if (!confirmResult.value) {
        return;
    }

    isLoading(true);
    try {
        ajaxPost("/ESS/Canteen/Redeem", param, async function (res) {            
            if (res.StatusCode == 200) {                
                swalSuccess(dialogTitle, res.Message);

                isLoading(true);
                await Promise.all([                    
                    new Promise(async (resolve) => {
                        await self.render.info();
                        resolve(true);
                    }),
                    new Promise(async (resolve) => {
                        self.action.refreshGridHistory();
                        resolve(true);
                    }),
                ])
                isLoading(false);
                $("#ModalOrder").modal("hide");                
            } else {
                isLoading(false);
                $("#ModalOrder").modal("show");
                swalFatal(dialogTitle, res.Message);
            }
        });
    } catch (e) {
        isLoading(false);
        swalFatal(dialogTitle, e);
    }    
}

model.action.saveCanteen = function () {
    isLoading(true);
    let dialogTitle = "Canteen";
    var data = ko.mapping.toJS(model.data.canteen);
    //validation

    if (!data.Name) {
        isLoading(false);
        return swalAlert(dialogTitle, "Canteen Name could not be empty");
    }
    if (!data.PICName) {
        isLoading(false);
        return swalAlert(dialogTitle, "Name could not be empty");
    }
    // if (!data.Email) {
    //     isLoading(false);
    //     return swalAlert(dialogTitle, "Email could not be empty");
    // }
    // if (!data.Username) {
    //     isLoading(false);
    //     return swalAlert(dialogTitle, "Username could not be empty");
    // }
    let formData = new FormData();
    formData.append("JsonData", JSON.stringify(data));
    var files = $('#Filepath').getKendoUpload().getFiles();
    if (files.length > 0) {
        formData.append("FileUpload", files[0].rawFile);
    } 

    try {

        ajaxPostUpload("/ESS/Canteen/Save", formData, function (res) {
            if (res.StatusCode == 200) {
                isLoading(false);
                swalSuccess(dialogTitle, res.Message);
                model.action.refreshGridCanteen();
                $("#ModalCanteen").modal("hide");
                console.log("Canteen Password ", res.Message)
            } else {
                isLoading(false);
                $("#ModalCanteen").modal("show");
                swalFatal(dialogTitle, res.Message);
            }
        });
    } catch (e) {
        isLoading(false);
        swalFatal(dialogTitle, e);
    }
}

model.action.addCanteen = function () {
    model.data.canteen(model.newCanteen());
    model.data.canteen().HTMLFile("");
    $("#ModalCanteen").modal("show");
}

var readonlyFile
model.action.editCanteen = function (uid) {
    var grid = $("#gridCanteen").data("kendoGrid");
    var data = grid.dataSource.getByUid(uid);
    if (data) {        
        model.data.canteen(model.newCanteen(data));

        if (!readonlyFile)
            readonlyFile = kendo.template($("#fileTemplateReadonly").html());
        if (data.Filename)
            model.data.canteen().HTMLFile(readonlyFile(data));

        setTimeout(async function(){
            try {
                model.is.userInfoLoading(true);                            
                var user = await model.get.user(data.Id);                
                model.data.canteen().User(ko.mapping.fromJS(user));
            } catch (error) {
                swalFatal("Canteen", error.Message);
            }finally{
                model.is.userInfoLoading(false);
            }

        });
        $("#ModalCanteen").modal("show");
    } else {
        swalAlert("Canteen", "Unable to find canteen data");
    }
}

model.action.deleteCanteen = async function (uid) {
    var grid = $("#gridCanteen").data("kendoGrid");
    var data = grid.dataSource.getByUid(uid);
    if (data) {
        var confirm = await swalConfirm("Delete", "Do you wan to delete <b>" + data.Name +"</b>");
        if (confirm.value) {
            isLoading(true);
            ajax(`/ESS/Canteen/Delete/${data.Id}`, 'GET', {}, function (res) {
                isLoading(false);
                if (res.StatusCode == 200) {
                    swalSuccess("Canteen", "Canteen <b>" + data.Name + "</b> has been deleted");
                    model.action.refreshGridCanteen();
                };

            });
        }
    } else {
        swalAlert("Canteen", "Unable to find canteen data");
    }
}

model.render.info = async function () {
    let data = await model.get.voucher();
    // Summary Data
    model.data.info(data);
};

model.map.role = {};
model.list.role = ko.observableArray([]);
model.init.manageCanteen = function () {
    let self=model;

    setTimeout(async function(){
        try {
            self.is.buttonLoading(true);
            let roles = await self.get.role();                
            roles.forEach(d=>{
                self.map.role[d.Id] = d;
            });
            self.list.role(roles);
        } catch (error) {
            self.is.buttonVisible(false);
            swalFatal("Canteen", error.Message);
        }finally{
            self.is.buttonLoading(false);   
        }

        
    });

    var canteenUserType = _.clone(model.proto.CanteenUserType);
    for(var i in canteenUserType){
        self.list.canteenUserType.push({value:i,text:camelToTitle(canteenUserType[i])});
    }
  self.render.gridCanteen();
  self.render.canteenProfile();
}

model.get.voucher = async function () {
    let response = await ajax("/ESS/Canteen/GetVoucher", "GET");
    if (response.StatusCode == 200 && response.Data) {        
        return response.Data || {};
    }
    console.error(response.StatusCode, ":", response.Message)
    return Object.assign({}, model.proto.Employee);
}

model.init.canteen = async function () {
    let self = model;
    isLoading(true);
    await Promise.all([
        new Promise(async (resolve) => {
            let data = await self.get.canteen();
            data.forEach(d => {
                self.map.canteen[d.Id] = d;
            });

            self.list.canteen(data);

            resolve(true);
        }),
        new Promise(async (resolve) => {
            await self.render.info();

            resolve(true);
        }),
        new Promise(async (resolve) => {
            self.render.gridHistory();
            resolve(true);
        }),
    ]);
    isLoading(false);
}

model.init.report = function () {
    model.render.gridReport();
}


model.init.claimVoucher = function () {
    model.render.gridClaim();
    model.data.statusClaim(1);
    setTimeout(async function () {
        //isLoading(true);
        await Promise.all([
            new Promise(async (resolve) => {
                var infoclaim = await model.get.claimInfo();
                console.log(infoclaim);
                model.data.infoClaim().TotalClaimed(infoclaim.TotalClaimed);
                model.data.infoClaim().TotalPaid(infoclaim.TotalPaid);
                resolve(true);
            }),
        ]);
    });
}

model.render.gridClaim = function () {
    console.log("render gridclaim");
    var grid = $("#gridClaim").data("kendoGrid");
    if (grid) {
        grid.destroy();
    }
    var options = {
        toolbar: ["excel"],
        excel: {
            fileName: "ClaimVoucher.xlsx",
            allPages: true
        },
        excelExport: function (e) {
            var sheet = e.workbook.sheets[0];
            var template = kendo.template(this.columns[2].template);

            for (var i = 1; i < sheet.rows.length-1; i++) {
                var row = sheet.rows[i];
                /*
                var dataItem = {
                    DataRedeem: row.cells[2].value
                };

                row.cells[1].value = template(dataItem);
                */
                var subtotal = _.sumBy(row.cells[2].value, function (o) { return o.SubTotal; });

                delete sheet.columns[1].width;
                sheet.columns[1].autoWidth = true;
                row.cells[1].value = (row.cells[1].value) ? moment(row.cells[1].value).format("dddd, DD-MM-YYY") : "-";                
                row.cells[2].value = subtotal;
                delete sheet.columns[3].width;
                sheet.columns[3].autoWidth = true;
                row.cells[3].value = (row.cells[3].value) ? moment(row.cells[3].value).format("dddd, DD-MM-YYY") : "-";
            }
        },
        /*
        excelExport: function (e) {
            var g = $("#gridClaim").data("kendoGrid");
            var sheet = e.workbook.sheets[0];
            var data = this.dataSource.view();
            var gridColumns = this.columns;
            var columns = gridColumns.map(function (col) {
                return {
                    value: col.title ? col.title : col.field,
                    autoWidth: true,
                    background: "#7a7a7a",
                    color: "#fff"
                };
            });

            
            var rows = [{ cells: columns, type: "header" }];

            for (var i = 0; i < data.length; i++) {
                var rowCells = [];
                for (var j = 0; j < gridColumns.length - 1; j++) {
                    console.log(">>", gridColumns[j]);
                    if (gridColumns[j].field == "DataRedeem") {
                        console.log("template:", gridColumns[j].template())
                    }
                    var cellValue = data[i][gridColumns[j].field];
                    rowCells.push({ value: cellValue });
                }
                rows.push({ cells: rowCells, type: "data" });
            }
            sheet.rows = rows;
        },
        */
        /*
        excelExport: function (e) {
            var rows = e.workbook.sheets[0].rows;

            for (var ri = 0; ri < rows.length; ri++) {
                var row = rows[ri];

                if (row.type == "group-footer" || row.type == "footer") {
                    for (var ci = 0; ci < row.cells.length; ci++) {
                        var cell = row.cells[ci];
                        if (cell.value) {
                            cell.value = $(cell.value).text();
                            // Set the alignment
                            cell.hAlign = "right";
                        }
                    }
                }
            }
        },
        */        
        //excelExport: exportGridWithTemplatesContent,
        //excelExport: excelHandler,  //footerTemplate
        dataSource: {
            //serverPaging: true,
            //serverSorting: true,
            //pageSize: 5,
            transport: {
                read: {
                    url: "/ESS/Canteen/HistoryClaim",
                    dataType: "json",
                    type: "POST",
                    contentType: "application/json",
                },
                parameterMap: function (data, type) {
                    //data.EmployeeID = ""
                    data.Range = {
                        Start: model.data.StartDate(),
                        Finish: model.data.EndDate()
                    }
                    data.Status = $("#filterStatus").data("kendoDropDownList").dataItem().Value
                    return JSON.stringify(data);
                }
            },
            schema: {
                data: function (res) {
                    if (res.StatusCode !== 200 && res.Status !== '') {
                        //console.log("xx>>",res.Data);
                        model.list.redeembydate(res.Data);
                        swalFatal("Fatal Error", `Error occured while fetching redeem history\n${res.Message}`)
                        return []
                    }

                    return res.Data || [];
                },
                total: "Total",
            },
            aggregate: [{ field: "SubTotal", aggregate: "sum" }]
        },
        noRecords: {
            template: "No claim history data available."
        },
        columns: [
            {
                headerAttributes: {
                    "class": "text-center",
                },
                field: "Id",
                title: "Id",
                width: 150
            },
            {
                headerAttributes: {
                    "class": "text-center",
                },
                field: "ClaimDate",
                title: "Claim Date",
                template: function (data) {
                    return standarizeDateFull(data.ClaimDate);
                },
                width: 250
            },
            {
                headerAttributes: {
                    "class": "text-center",
                },
                attributes: {
                    "class": "text-center",
                },
                field: "DataRedeem", title: "Total",
                width: 150,
                template: function (d) {
                    var total = 0;
                    var total = _.sumBy(d.DataRedeem, function (o) { return o.SubTotal; });
                    return total;
                },
                footerTemplate: function (d) {
                    var arr = $("#gridClaim").data("kendoGrid").dataSource.view();
                    var total = 0;
                    arr.map(function (x) {
                        x.DataRedeem.map(function (y) {
                            total = total + y.SubTotal;
                        });

                    })
                    return total;
                },
                footerAttributes: {
                    "class": "table-footer-cell text-center",
                },
            },
            {
                headerAttributes: {
                    "class": "text-center",
                },
                field: "DatePaid",
                title: "Date Paid",
                template: function (d) {
                    return standarizeDateFull(d.DatePaid);
                },
                width: 250
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
                template: function (d) {
                    var arr = $("#filterStatus").data("kendoDropDownList").dataSource.view();
                    //console.log(status)
                    //var status = _.find(arr, function (o) {
                    //    return o.Value == d.Status;
                    //});
                    //return status.Name;
                    var status = _.result(_.find(arr, function (o) {
                        return o.Value === d.Status;
                    }), 'Name');
                    return status;
                },
                width: 150
            },
            {
                attributes: {
                    "class": "text-center",
                },
                template: function (d) {
                    return '<button type="button" class="btn btn-primary btn-sm" onclick="openDetailClaim(\''+ d.uid + '\')">Detail</button>';
                },
                width: 110
            }
        ]
    }
    var grid = $("#gridClaim").kendoGrid(options);
}

function openDetailClaim(uid) {
    var source = $("#gridClaim").data("kendoGrid").dataSource.view();
    var data = source.find(function (o) {
        return o.uid === uid;
    });
    console.log(data);
    $("#Modal-Detail").modal("show");

    var options = {
        dataSource: data.DataRedeem,
        columns: [
            {
                field: "RedeemDate",
                title: "Redeem Date",
                template: function (r) {
                    return standarizeDate(new Date(r.RedeemDate));
                }
            },
            {
                title: "Sub Total",
                field: "SubTotal",
                footerTemplate: function () {
                    var total = 0;
                    data.DataRedeem.map(function (m) {
                        total = total + m.SubTotal;
                    });
                    return total;
                }
            }
        ]
    };
    $("#gridDetailClaim").kendoGrid(options);
}

model.render.gridRedeem = function () {
    var retData = [];
    var grid = $("#gridRedeem").data("kendoGrid");
    if (grid) {
        grid.destroy();

    }
    var options = {
        dataSource: {
            //serverPaging: true,
            //serverSorting: true,
            //pageSize: 5,
            transport: {
                read: {
                    url: "/ESS/Canteen/CanteenClaim",
                    dataType: "json",
                    type: "POST",
                    contentType: "application/json",
                },
                parameterMap: function (data, type) {
                    //data.EmployeeID = ""
                    data.Range = {
                        Start: model.data.StartDate(),
                        Finish: model.data.EndDate()
                    }
                    return JSON.stringify(data);
                }
            },
            schema: {
                data: function (res) {
                    console.log("redeem:", res.Data);
                    retData = res.Data;
                    //if (retData.length == 0) {
                    //    model.is.btnSaveClaim(false);
                    //} else {
                    //    model.is.btnSaveClaim(true);
                    //}
                    if (res.StatusCode !== 200 && res.Status !== '') {
                        model.list.redeembydate(res.Data);
                        swalFatal("Fatal Error", `Error occured while fetching redeem history\n${res.Message}`)
                        return []
                    }

                    return res.Data || [];
                },
                total: "Total",
            },
            aggregate: [{ field: "SubTotal", aggregate: "sum" }]
        },
        pageable: {
            previousNext: false,
            info: false,
            numeric: false,
            refresh: false
        },
        noRecords: {
            template: "No redeem history data available."
        },
        //sortable: true,
        columns: [
            {
                headerAttributes: {
                    "class": "text-center",
                },
                field: "RedeemDate",
                title: "Redeem Date",
                attributes: {
                    "class": "text-center",
                },
                template: function (data) {
                    return standarizeDateFull(data.RedeemDate);
                },
                width: 200
            },
            {
                field: "SubTotal",
                title: "Total Voucher",
                headerAttributes: {
                    "class": "text-center",
                },
                attributes: {
                    "class": "text-center",
                },
                aggregates: ["sum"],
                footerTemplate: function (d) {
                    return d.SubTotal.sum;
                },
                footerAttributes: {
                    "class": "table-footer-cell text-center",
                },
                width: 140
            },
            {
                headerAttributes: {
                    "class": "text-center",
                },
                attributes: {
                    "class": "text-center",
                },
                title: "Claim",
                width: 80,
                template: function (d) {
                    return '<div class="custom-control custom-checkbox">' +
                        '<input type="checkbox" class="custom-control-input checked-claim" id="' + d.uid + '" onchange="CheckClaim()" />' +
                        '<label class="custom-control-label" for="' + d.uid + '">&nbsp;</label>' +
                        '</div >';
                },
            }
        ],
    }
    $("#gridRedeem").kendoGrid(options);
    return retData;
}

function CheckClaim() {
    var d = $(".checked-claim:checked").length;
    if (d == 0) {
        model.is.btnSaveClaim(false);
    } else {
        model.is.btnSaveClaim(true);
    }
}

model.action.refreshClaim = function (uiOnly = false) {
    if (model.data.StartDate() > model.data.EndDate()) {
        swalError("Warning", "start date is greater than the end date");
        return false;
    };

    var $grid = $("#gridClaim").data("kendoGrid");
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

model.action.refreshClaimModal = function () {
   $("#gridRedeem").data("kendoGrid").dataSource.read()
}

model.action.saveClaim = async function () {
    var arr = [];
    
    var data = $("#gridRedeem").data("kendoGrid").dataSource.view();
    $(".checked-claim:checked").map(function (d) {
        arr.push(this.id);
    });
    if (arr.length == 0) {
        swalAlert("Save Claim", "Minimal one claim");
        return false;
    }

    var confirmResult = await swalConfirm("Save Claim", `Are you sure to claim ?`);
    if (!confirmResult.value) {
        return;
    }

    var redeem = _.filter(data, (v) => _.indexOf(arr, v.uid) >= 0);
    
    var param = {
        DataRedeem: redeem,
    }
    isLoading(true);
    ajaxPost("/ESS/Canteen/SaveClaim", param, function (res) {
        isLoading(false);
        if (res.StatusCode == 200) {
            model.render.gridClaim();
            swalSuccess("Save claim", res.Message);
            $("#Modal-Claim").modal("hide");
            
        }
    });
}

model.action.openClaim = () => {
    $("#Modal-Claim").modal("show");
}

model.get.claimInfo = async function () {
    let response = await ajax("/ESS/Canteen/GetClaimInfo", "GET");
    if (response.StatusCode == 200) {
        return response.Data || [];
    }
    console.log(response.StatusCode, ":", response.Message);
}

$(document).on('show.bs.modal', '#Modal-Claim', function () {
    model.render.gridRedeem();
});

model.init.paymentClaim = function () {
    setTimeout(async function () {
        await Promise.all([
            new Promise(async (resolve) => {
                model.render.gridPaymentCanteen();
                model.render.gridRedeemCanteen();
                resolve(true);
            }),
            new Promise(async (resolve) => {
                let data = await model.get.canteen();
                $("#filterCanteen").data("kendoDropDownList").setDataSource(data);
                model.list.canteen(data);
                
                resolve(true);
            })
        ]);
        
    });
}

model.render.gridPaymentCanteen = async function () {
    var grid = $("#gridPaymentCanteen").data("kendoGrid");
    if (grid) {
        grid.destroy();
    }
    var options = {
        toolbar: ["excel"],
        excel: {
            fileName: "PaymentClaim.xlsx",
            allPages: true
        },
        excelExport: function (e) {
            var sheet = e.workbook.sheets[0];

            for (var i = 1; i < sheet.rows.length-1; i++) {
                var row = sheet.rows[i];
                var subtotal = _.sumBy(row.cells[3].value, function (o) { return o.SubTotal; });
                delete sheet.columns[0].width;
                sheet.columns[0].autoWidth = true;
                delete sheet.columns[1].width;
                sheet.columns[1].autoWidth = true;
                row.cells[1].value = moment(row.cells[1].value).format("DD-MMM-YYYY");
                delete sheet.columns[3].width;
                sheet.columns[3].autoWidth = true;
                row.cells[3].value = subtotal;
                row.cells[4].value = model.list.statusClaim().find(function (o) { return o.Value == row.cells[4].value }).Name;
                delete sheet.columns[5].width;
                sheet.columns[5].autoWidth = true;
                row.cells[5].value = (row.cells[5].value) ? moment(row.cells[5].value).format("DD-MMM-YYYY") : "-";
            }
        },
        dataSource: {
            //serverPaging: true,
            //serverSorting: true,
            //pageSize: 5,
            transport: {
                read: {
                    url: "/ESS/Canteen/HistoryPaymentClaim",
                    dataType: "json",
                    type: "POST",
                    contentType: "application/json",
                },
                parameterMap: function (data, type) {
                    data.CanteenUserID = $("#filterCanteen").data("kendoDropDownList").dataItem().UserID;
                    data.Range = {
                        Start: model.data.StartDate(),
                        Finish: model.data.EndDate()
                    }
                    data.Status = $("#filterStatus").data("kendoDropDownList").dataItem().Value;
                    return JSON.stringify(data);
                }
            },
            schema: {
                data: function (res) {
                    if (res.StatusCode !== 200 && res.Status !== '') {
                        console.log(res.Data);
                        model.list.redeembydate(res.Data);
                        swalFatal("Fatal Error", `Error occured while fetching redeem history\n${res.Message}`)
                        return []
                    }

                    return res.Data || [];
                },
                total: "Total",
            },
        },
        pageable: {
            previousNext: false,
            info: false,
            numeric: false,
            refresh: true
        },
        noRecords: {
            template: "No claim paid data available."
        },
        columns: [
            {
                headerAttributes: {
                    "class": "text-center",
                },
                field: "Id",
                title: "Claim Id",
                width: 150
            },
            {
                headerAttributes: {
                    "class": "text-center",
                },
                field: "ClaimDate",
                title: "Claim Date",
                template: function (data) {
                    return standarizeDateFull(data.ClaimDate);
                },
                width: 250
            },
            {
                headerAttributes: {
                    "class": "text-center",
                },
                field: "CanteenName",
                title: "Canteen Name",
                width: 150
            },
            {
                headerAttributes: {
                    "class": "text-center",
                },
                attributes: {
                    "class": "text-center",
                },
                field: "DataRedeem", title: "Total", width: 100,
                template: function (d) {
                    //var total = _.sumBy(d.DataRedeem, function (o) {
                    //    return o.SubTotal
                    //});
                    var total = _.sumBy(d.DataRedeem, function (o) { return o.SubTotal; });
                    return total;
                },
                footerTemplate: function (d) {
                    var arr = $("#gridPaymentCanteen").data("kendoGrid").dataSource.view();
                    var total = 0;
                    arr.map(function (x) {
                        x.DataRedeem.map(function (y) {
                            total = total + y.SubTotal;
                        });
                        
                    })
                    return total;
                },
                footerAttributes: {
                    "class": "table-footer-cell text-center",
                },
            },
            {
                headerAttributes: {
                    "class": "text-center",
                },
                attributes: {
                    "class": "text-center",
                },
                field: "Status", title: "Status", width: 100,
                template: function (d) {
                    var arr = $("#filterStatus").data("kendoDropDownList").dataSource.view();
                    //console.log(status)
                    //var status = _.find(arr, function (o) {
                    //    return o.Value == d.Status;
                    //});
                    //return status.Name;
                    var status = _.result(_.find(arr, function (o) {
                        return o.Value === d.Status;
                    }), 'Name');
                    return status;
                },
                footerAttributes: {
                    "class": "table-footer-cell text-center",
                },
            },
            {
                headerAttributes: {
                    "class": "text-center",
                },
                field: "DatePaid",
                title: "Date Paid",
                template: function (d) {
                    return standarizeDateFull(d.DatePaid);
                },
                width: 250
            },
            {
                headerAttributes: {
                    "class": "text-center",
                },
                attributes: {
                    "class": "text-center",
                },
                title: "Paid",
                width: 80,
                template: function (d) {
                    if (d.Status == 2) {
                        return '<div class="custom-control custom-checkbox">' +
                            '<input type="checkbox" class="custom-control-input checked-paid" id="' + d.uid + '" checked="true" disabled />' +
                            '<label class="custom-control-label" for="' + d.uid + '">&nbsp;</label>' +
                            '</div >';
                    } else {
                        return '<div class="custom-control custom-checkbox">' +
                            '<input type="checkbox" class="custom-control-input checked-paid" id="' + d.uid + '" onchange="CheckPaid()" />' +
                            '<label class="custom-control-label" for="' + d.uid + '">&nbsp;</label>' +
                            '</div >';
                    }
                    
                },
            }
        ]
    }
    $("#gridPaymentCanteen").kendoGrid(options);
}

function CheckPaid() {
    var c = $(".checked-paid:checked").length;
    if (c == 0) {
        model.is.btnSavePaid(false);
    } else {
        model.is.btnSavePaid(true);
    }
}

model.render.gridRedeemCanteen = async function () {
    var grid = $("#gridRedeemCanteen").data("kendoGrid");
    if (grid) {
        grid.destroy();
    }
    var options = {
        toolbar: ["excel"],
        excel: {
            fileName: "ReportRedeemCanteen.xlsx",
            allPages: true
        },
        excelExport: function (e) {
            var sheet = e.workbook.sheets[0];

            for (var i = 1; i < sheet.rows.length; i++) {
                var row = sheet.rows[i];
                //console.log(i, row.type);
                //console.log(i, row);
                
                if (row.type == "data") {
                    delete sheet.columns[1].width;
                    sheet.columns[1].autoWidth = true;
                    row.cells[1].value = moment(row.cells[1].value).format("DD-MMM-YYYY");
                    delete sheet.columns[5].width;
                    sheet.columns[5].autoWidth = true;
                    row.cells[5].value = moment(row.cells[5].value).format("DD-MMM-YYYY");
                    delete sheet.columns[6].width;
                    sheet.columns[6].autoWidth = true;
                    row.cells[6].value = (row.cells[6].value) ? moment(row.cells[6].value).format("DD-MMM-YYYY") : "-";
                }
            }
        },
        dataSource: {
            serverPaging: true,
            serverSorting: true,
            pageSize: 10,
            transport: {
                read: {
                    url: "/ESS/Canteen/GetHistoryRedeem",
                    dataType: "json",
                    type: "POST",
                    contentType: "application/json",
                },
                parameterMap: function (data, type) {
                    data.CanteenUserID = $("#filterCanteen").data("kendoDropDownList").dataItem().UserID;
                    data.Range = {
                        Start: model.data.StartDate(),
                        Finish: model.data.EndDate()
                    }
                    data.Status = $("#filterStatus").data("kendoDropDownList").dataItem().Value;
                    return JSON.stringify(data);
                }
            },
            schema: {
                data: function (res) {
                    console.log("res:", res);
                    if (res.StatusCode !== 200 && res.Status !== '') {
                        console.log(res.Data);
                        model.list.redeembydate(res.Data);
                        swalFatal("Fatal Error", `Error occured while fetching redeem history\n${res.Message}`)
                        return []
                    }

                    return res.Data || [];
                },
                total: "Total",
            },
            group: {
                field: "ClaimId", aggregates: [
                    { field: "ClaimId", aggregate: "count" },
                    { field: "RedeemedVoucherTotal", aggregate: "sum" },
                ]
            },
            aggregate: [
                { field: "ClaimId", aggregate: "count" },
                { field: "RedeemedVoucherTotal", aggregate: "sum" },
            ],
            /*
            group: {
                field: "ClaimId", aggregates: [
                    { field: "Id", aggregate: "count" },
                    { field: "UnitPrice", aggregate: "sum" },
                    { field: "UnitsOnOrder", aggregate: "average" },
                    { field: "UnitsInStock", aggregate: "count" }
                ]
            },
            aggregate: [
                { field: "Id", aggregate: "count" },
                { field: "UnitPrice", aggregate: "sum" },
                { field: "UnitsOnOrder", aggregate: "average" },
                { field: "UnitsInStock", aggregate: "min" },
                { field: "UnitsInStock", aggregate: "max" }
            ]
            */
        },
        noRecords: {
            template: "No claim paid data available."
        },
        sortable: true,
        pageable: {
            previousNext: false,
            info: false,
            numeric: false,
            refresh: true
        },
        //pageable: {
        //    pageSizes: true
        //},
        groupable: false,
        resizable: true,
        columns: [
            {
                headerAttributes: {
                    "class": "text-center",
                },
                field: "RedeemedAt",
                title: "Date Redeem",
                width: 220,
                template: function (d) {
                    return standarizeDateFull(d.RedeemedAt);
                },
                groupFooterTemplate: "Count: #=ClaimId.count#",
                footerTemplate: "Total Count: #=ClaimId.count#"
            },
            {
                headerAttributes: {
                    "class": "text-center",
                },
                field: "CanteenName",
                title: "CanteenName",
                width: 180
            },
            {
                headerAttributes: {
                    "class": "text-center",
                },
                attributes: {
                    "class": "text-center",
                },
                field: "RedeemedVoucherTotal",
                title: "Voucher", width: 100,
                groupFooterTemplate: " #=sum#",
                footerTemplate: "Sum: #=sum#",
                groupFooterAttributes: {
                    "class": "table-footer-cell text-center",
                },
                footerAttributes: {
                    "class": "table-footer-cell text-center",
                }
            },
            {
                field: "EmployeeName",
                title: "Employee Name",
                width: 250,
            },
            {
                headerAttributes: {
                    "class": "text-center",
                },
                field: "ClaimDate", title: "Date Claim", width:220,
                template: function (d) {
                    return standarizeDateFull(d.ClaimDate);
                }
            },
            {
                headerAttributes: {
                    "class": "text-center",
                },
                attributes: {
                    "class": "text-center",
                },
                title: "Status",
                width: 100,
                template: function (d) {
                    //if (d.Status == 2) {
                    //    return "Paid";
                    //} else {
                    //    return "Claimed";
                    //}
                    return model.list.statusClaim().find(function (f) {
                        return f.Value == d.Status
                    }).Name;
                },
            },
            {
                headerAttributes: {
                    "class": "text-center",
                },
                field: "DatePaid", title: "Date Paid", width: 220,
                template: function (d) {
                    return standarizeDateFull(d.DatePaid);
                }
            },
        ]
    }
    $("#gridRedeemCanteen").kendoGrid(options);
}

model.action.refreshGridClaim = function () {
    var id = $(".tab-content .active").attr('id');
    if (id == "payment") {
        model.action.refreshPaymentClaim();
    } else {
        model.action.refreshRedeemClaim();
    }
}

model.action.refreshPaymentClaim = function (uiOnly = false) {
    if (model.data.StartDate() > model.data.EndDate()) {
        swalError("Warning", "start date is greater than the end date");
        return false;
    };

    var $grid = $("#gridPaymentCanteen").data("kendoGrid");
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

model.action.refreshRedeemClaim = function (uiOnly = false) {
    if (model.data.StartDate() > model.data.EndDate()) {
        swalError("Warning", "start date is greater than the end date");
        return false;
    };

    var $grid = $("#gridRedeemCanteen").data("kendoGrid");
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

model.action.savePaid = async function () {
    var arr = [];
    var data = $("#gridPaymentCanteen").data("kendoGrid").dataSource.view();
    $(".checked-paid:checked").map(function (d) {
        arr.push(this.id);
    });
    if (arr.length == 0) {
        swalAlert("Save Claim", "Minimal one claim");
        return false;
    }
    var redeem = _.filter(data, (v) => _.indexOf(arr, v.uid) >= 0);

    var confirmResult = await swalConfirm("Save payment claim", `Are you sure to pay claim ?`);
    if (!confirmResult.value) {
        return;
    }
    isLoading(true);
    ajaxPost("/ESS/Canteen/SavePaid", redeem, function (res) {
        console.log(res);
        if (res.StatusCode == 200) {
            //model.render.gridClaim();
            swalSuccess("Save claim", res.Message);
            $("#Modal-Claim").modal("hide");
            model.is.btnSavePaid(false);
            
            $("#gridPaymentCanteen").data("kendoGrid").dataSource.read()
        }
        isLoading(false);
    });
}

model.action.requestVoucher = function () {
    isLoading(true);
    ajaxPost("/ESS/Canteen/RequestVoucher", {}, function (res) {
        console.log(res);
        if (res.StatusCode == 200) {
            //model.render.gridClaim();
            if (res.Data.length == 0) {
                swalAlert("Request", "No data voucher requested");
            } else {
                swalSuccess("Request", "Voucher has been requested", res.Message);
            }
        }
        isLoading(false);
    });
}

//function exportGridWithTemplatesContent(e) {
//    var data = e.data;
//    var gridColumns = e.sender.columns;
//    var sheet = e.workbook.sheets[0];
//    var visibleGridColumns = [];
//    var columnTemplates = [];
//    var dataItem;
//    // Create element to generate templates in.
//    var elem = document.createElement('div');

//    // Get a list of visible columns
//    for (var i = 0; i < gridColumns.length; i++) {
//        if (!gridColumns[i].hidden) {
//            visibleGridColumns.push(gridColumns[i]);
//        }
//    }

//    // Create a collection of the column templates, together with the current column index
//    for (var i = 0; i < visibleGridColumns.length; i++) {
//        if (visibleGridColumns[i].template) {
//            columnTemplates.push({ cellIndex: i, template: kendo.template(visibleGridColumns[i].template) });
//        }
//    }

//    // Traverse all exported rows.
//    for (var i = 1; i < sheet.rows.length; i++) {
//        var row = sheet.rows[i];
//        // Traverse the column templates and apply them for each row at the stored column position.

//        // Get the data item corresponding to the current row.
//        var dataItem = data[i - 1];
//        for (var j = 0; j < columnTemplates.length; j++) {
//            var columnTemplate = columnTemplates[j];
//            console.log(columnTemplate)
//            // Generate the template content for the current cell.
//            elem.innerHTML = columnTemplate.template(dataItem);
//            if (row.cells[columnTemplate.cellIndex] != undefined)
//                // Output the text content of the templated cell into the exported cell.
//                row.cells[columnTemplate.cellIndex].value = elem.textContent || elem.innerText || "";
//        }
//    }
//}

function exportGridWithTemplatesContent(e) {
    var data = e.data;
    var gridColumns = e.sender.columns;
    var sheet = e.workbook.sheets[0];
    var visibleGridColumns = [];
    var columnTemplates = [];
    var dataItem;
    // Create element to generate templates in.
    var elem = document.createElement('div');

    // Get a list of visible columns
    for (var i = 0; i < gridColumns.length; i++) {
        if (!gridColumns[i].hidden) {
            visibleGridColumns.push(gridColumns[i]);
        }
    }

    // Create a collection of the column templates, together with the current column index
    for (var i = 0; i < visibleGridColumns.length; i++) {
        if (visibleGridColumns[i].template) {
            columnTemplates.push({ cellIndex: i, template: kendo.template(visibleGridColumns[i].template) });
        }
    }

    // Traverse all exported rows.
    for (var i = 1; i < sheet.rows.length; i++) {
        var row = sheet.rows[i];
        // Traverse the column templates and apply them for each row at the stored column position.

        // Get the data item corresponding to the current row.
        var dataItem = data[i - 1];
        for (var j = 0; j < columnTemplates.length; j++) {
            var columnTemplate = columnTemplates[j];
            // Generate the template content for the current cell.
            elem.innerHTML = columnTemplate.template(dataItem);
            if (row.cells[columnTemplate.cellIndex] != undefined)
                // Output the text content of the templated cell into the exported cell.
                row.cells[columnTemplate.cellIndex].value = elem.textContent || elem.innerText || "";
        }
    }
}

function excelHandler(e) {
    var workbook = e.workbook;
    var sheet = workbook.sheets[0];
    var columnValueMap = {},
        i,
        j,
        visibleColumns = $.grep(e.sender.columns, function (col) {
            return !col.hidden;
        });

    for (i = 0; i < visibleColumns.length; i++) {
        if (visibleColumns[i].values) {
            columnValueMap[i] = {};
            var values = visibleColumns[i].values;
            for (j = 0; j < values.length; j++) {
                columnValueMap[i][values[j].value] = values[j].text;
            }
        }
    }

    for (i = 1; i < sheet.rows.length; i++) {
        for (var columnIndex in columnValueMap) {
            if (columnValueMap.hasOwnProperty(columnIndex)) {
                var value = sheet.rows[i].cells[columnIndex].value;
                if (value) {
                    sheet.rows[i].cells[columnIndex].value = columnValueMap[columnIndex][value];
                }
            }
        }
    }
}

// Canteen Profile
model.render.canteenProfile = () => {
  let grid = $("#canteenProfile").data("kendoGrid");
  if (grid) {
    grid.destroy()
  }
  let options = {
    dataSource: {
      transport: {
        read: {
          url: "/ESS/Canteen/GetProfile",
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
            swalFatal("Fatal Error", `Error occured while fetching canteen(s)\n${res.Message}`)
            return []
          }
          return res.Data || []
        },
        total: "Total",
      },
    },
    noRecords: {
      template: "No canteen data available."
    },
    pageable: {
      previousNext: false,
      info: false,
      numeric: false,
      refresh: true
    },
    columns: [
      {
        headerAttributes: {
          "class": "text-center",
        },
        attributes: {
          "class": "text-center",
        },
        template: function (x) {
          var fallbackImage = "/assets/img/blank-user.png";
          var img = `<div class="canteen-image" style="width:48px; height:48px; display:block; margin:auto; background: url('/ESS/Canteen/Image/${x.Id}/${x.Filename}') no-repeat, url('${fallbackImage}') no-repeat; background-size:cover, contain"></div>`;
          return img;
        },
        width: 100
      },
      {
        field: "Name",
        title: "Name",
        width: 250
      },
      {
        field: "Phone",
        title: "Phone",
        width: 200
      },
      {
        field: "PICName",
        title: "PICName",
        width: 150
      },
      {
        headerAttributes: {
          "class": "text-center",
        },
        attributes: {
          "class": "text-center",
        },
        template: function (d) {
          return `<button type="button" class="btn btn-xs btn-outline-info" onclick="model.action.editCanteenProfile('${d.uid}')"><i class="fa mdi mdi-pencil"></i></button>`
        },
        width: 100
      }
    ]
  }
  $("#canteenProfile").kendoGrid(options)
}

model.action.refreshGridCanteenProfile = (uiOnly = false) => {
  var $grid = $("#canteenProfile").data("kendoGrid")
  if ($grid) {
    if ($($grid.content).find(".k-loading-mask").length > 0) {
      return true
    }
    if (uiOnly) {
      $grid.refresh()
    } else {
      $grid.dataSource.read()
    }
  }
}

model.action.editCanteenProfile = (uid) => {
  var grid = $("#canteenProfile").data("kendoGrid")
  var data = grid.dataSource.getByUid(uid)

  if (data) {
    model.data.canteen(model.newCanteen(data))
    model.data.menus([])
    var menus = ko.mapping.toJS(model.data.canteen().Menu)
    menus.forEach(d => {
      model.data.menus.push(d)
    })

    if (!readonlyFile)
      readonlyFile = kendo.template($("#fileTemplateReadonly").html())
    if (data.Filename)
      model.data.canteen().HTMLFile(readonlyFile(data))

    setTimeout(async function () {
      try {
        model.is.userInfoLoading(true)
        var user = await model.get.user(data.Id)
        model.data.canteen().User(ko.mapping.fromJS(user))
      } catch (error) {
        swalFatal("Canteen", error.Message)
      } finally {
        model.is.userInfoLoading(false)
      }
    })

    $("#ModalCanteenProfile").modal("show")
  } else {
    swalAlert("Canteen", "Unable to find canteen profile data")
  }
}

model.action.saveCanteenProfile = () => {
  isLoading(true)
  let dialogTitle = "Canteen Profile"
  model.data.canteen().Menu = ko.mapping.toJS(model.data.menus())
  let data = ko.mapping.toJS(model.data.canteen())
  let formData = new FormData()
  formData.append("JsonData", JSON.stringify(data))
  var files = $('#Filepath').getKendoUpload().getFiles()
  if (files.length > 0) {
    formData.append("FileUpload", files[0].rawFile)
  }
  try {
    ajaxPostUpload("/ESS/Canteen/SaveProfile", formData, function (res) {
      if (res.StatusCode == 200) {
        isLoading(false)
        swalSuccess(dialogTitle, res.Message)
        model.action.refreshGridCanteenProfile()
        $("#ModalCanteenProfile").modal("hide")
      } else {
        isLoading(false);
        $("#ModalCanteenProfile").modal("show")
        swalFatal(dialogTitle, res.Message)
      }
    })
  } catch (e) {
    isLoading(false)
    swalFatal(dialogTitle, e)
  }
}

model.action.addMenu = () => {
  model.data.menus.push(model.newMenuMerchant());
}

model.action.deleteMenu = (Id) => {
  model.data.menus.remove(Id);
}