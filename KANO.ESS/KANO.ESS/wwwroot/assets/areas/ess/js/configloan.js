/*
model.newConfig = function (data) {
    if (data) {
        console.log("init:", data);
        return ko.mapping.fromJS(Object.assign(_.clone(this.proto.ConfigLoan), _.clone(data)));
    }
    console.log("initzzz:", data);
    return ko.mapping.fromJS(this.proto.ConfigLoan);
};
model.data.configLoan = ko.observable(model.newConfig());
*/
model.data.configLoan = ko.observable(ko.mapping.fromJS(ko.mapping.toJS(model.proto.ConfigLoan)));
model.data.configTemplate = ko.observable(ko.mapping.fromJS(ko.mapping.toJS(model.proto.ConfigTemplate)));

model.list.configLoan = ko.observableArray();
model.list.configLoanDetail = ko.observableArray();
model.list.methode = ko.observableArray([
    {
        Methode: 1,
        MethodeName: "Normal"
    }, {
        Methode: 2,
        MethodeName: "Compensation"
    }
]);

model.is.enableAdd = ko.observable(false);
model.is.editConfig = ko.observable();
model.is.additionalSetting = ko.observable(false);

model.init.config = function () {
    setTimeout(async function () {
        model.render.grid();
        //isLoading(true);
        await Promise.all([
            new Promise(async (resolve) => {
                await model.get.mailtemplate();
                //var d = await model.get.configLoan();
                resolve(true);
            }),
        ]);
    });
}

model.render.grid = async function () {
    var el = $("#grid");
    var grid = el.getKendoGrid();

    if (grid) {
        grid.destroy();
    }
    var grid = await el.kendoGrid({
        dataSource: {
            transport: {
                read: "/ESS/ConfigLoan/GetConfig"
            },
            schema: {
                data: (d) => {
                    model.is.enableAdd(true);
                    console.log("datagrid:", d.Data);
                    model.list.configLoan(d.Data);
                    return d.Data || [];
                },
                total: "Total",
            },
            error: function (e) {
                swalFatal("Fatal Error", `Error occured while fetching loan(s)\n${e.xhr.responseText}`)
            }
            //pageSize: 6,
            //serverPaging: true,
            //serverSorting: true
        },
        //height: 600,
        //sortable: true,
        //pageable: true,
        detailInit: detailInit,
        //dataBound: function () {
            //this.expandRow(this.tbody.find("tr.k-master-row").first());
        //},
        detailExpand: function (e) {
            var grid = e.sender;
            var rows = grid.element.find(".k-master-row").not(e.masterRow);

            rows.each(function (e) {
                grid.collapseRow(this);
            });
        },
        columns: [
            {
                field: "Name",
                title: "",
                width: 80
            }, {
                field: "MaximumRangePeriode",
                title: "Max Periode (month)",
                width: 70
            }, {
                field: "MaximumLoan (Rp)",
                title: "Max Loan",
                width: 60,
                template: function (d) {
                    return formatNumber(d.MaximumLoan);
                }
            }, {
                field: "Email",
                title: "Email To",
                width: 100,
                template: function (d) {
                    if (d.Email) {
                        return d.Email.join(";");
                    } else {
                        return "-";
                    }
                }
            }, {
                field: "Id",
                title: "Actions",
                width: 60,
                attributes: {
                    "class": "table-cell",
                    style: "text-align: center;"
                },
                template: function (d) {
                    return '<button class="btn btn-gradient-primary btn-sm" onclick="model.on.editModalConfig(\'' + d.uid + '\')">Edit</button>';
                }
            }
        ]
    });
}

function detailInit(e) {
    $("<div/>").appendTo(e.detailCell).kendoGrid({
        dataSource: {
            transport: {
                read: "/ESS/ConfigLoan/GetConfig"
            },
            schema: {
                data: (k) => {
                    console.log("datagrid:", k.Data);
                    //return k.Data || [];
                    var arr = [];
                    k.Data.map(function (x) {
                        x.Detail.map(function (y) {
                            arr.push({
                                IdLoan: x.Id,
                                PeriodeName: y.PeriodeName,
                                Methode: y.Methode,
                                MethodeName: y.MethodeName,
                                MinimumRangePeriode: y.MinimumRangePeriode,
                                MaximumRangePeriode: y.MaximumRangePeriode,
                                Interest: y.Interest
                            });
                        });
                    });
                    return arr || [];
                },
                total: "Total",
            },
            //serverPaging: true,
            //serverSorting: true,
            //serverFiltering: true,
            //pageSize: 10,
            filter: { field: "IdLoan", operator: "eq", value: e.data.Id }
        },
        scrollable: false,
        //sortable: true,
        //pageable: true,
        columns: [
            {
                field: "PeriodeName", title: "Name", width: "80px",
                template: function (z) {
                    return z.PeriodeName;
                }
            },
            {
                field: "MaximumRangePeriode", title: "Maximum Periode", width: "80px",
                template: function (z) {
                    return z.MaximumRangePeriode + " Months";
                }
            },
            {
                field: "Percent", width: "80px",
                template: function (z) {
                    return z.Interest * 100 + " %";
                }
            },
        ]
    });
}

model.action.showModalConfig = function () {
    //model.data.configLoan(model.newConfig());
    model.data.configLoan(ko.mapping.fromJS(this.proto.ConfigLoan));
    $("#modalConfig").modal("show");
}

var arrDetail = [];
function AddDetailConfig() {
    if (model.data.configLoan().Detail()) {
        arrDetail = model.data.configLoan().Detail();
    } else {
        arrDetail = [];
    }
    arrDetail.push({
        IdDetail: ko.observable(),
        IdLoan: ko.observable(),
        LoanTypeName: ko.observable(),
        PeriodeName: ko.observable(),
        MinimumRangePeriode: ko.observable(0),
        MaximumRangePeriode: ko.observable(0),
        Methode: ko.observable(),
        MethodeName: ko.observable(),
        Interest: ko.observable(),
        MinimumRangeLoanPeriode: ko.observable(0),
        MaximumRangeLoanPeriode: ko.observable(0),
        MaximumLoad: ko.observable(0),
    });
    model.is.editConfig(false);
    model.data.configLoan().Detail(arrDetail);
}

model.on.editModalConfig = function (uid) {
    var grid = $("#grid").data("kendoGrid");
    var data = grid.dataSource.data().find(x => x.uid == uid);
    console.log("edit:", data.Email);

    
    data.Detail.map(function (x, i) {
        data.Detail[i].Interest = x.Interest * 100;
    });

    if (data) {
        //model.data.configLoan(ko.mapping.toJS(data));
        model.data.configLoan(ko.mapping.fromJSON(ko.mapping.toJSON(data)));
        $("#modalConfig").modal("show");
        $("#EmailTo").val(data.Email.join("; "));
    } else {
        swalAlert("Configuration", "Unable to find configuration data");
    }
}

model.action.saveConfigLoan = () => {
    var dialogTitle = "Configuration"
    var data = ko.mapping.toJS(model.data.configLoan());
    console.log("data save:", data);
    var arr = [];
    $("#EmailTo").val().trim().replace(" ", "").split(";").map(function (m) {
        if (m != "") {
            arr.push(m);
        }
    });
    data.Email = arr;

    data.Detail.map(function (x, i) {
        data.Detail[i].MethodeName = model.list.methode().find(y => y.Methode == x.Methode).MethodeName;
        data.Detail[i].Interest = x.Interest / 100;
    });
    try {
        isLoading(true)
        ajaxPost("/ESS/ConfigLoan/SaveConfig", data, function (res) {
            isLoading(false);
            if (res.StatusCode == 200) {
                swalSuccess(dialogTitle, res.Message);
                $("#grid").data("kendoGrid").dataSource.read()
                $("#modalConfig").modal("hide");
            } else {
                swalError(dialogTitle, res.Message);
            }
        }, function (err) {
            swalError(dialogTitle, err.Message);
        });
    } catch (e) {
        isLoading(false);
    }
}

model.action.removeDetail = function (data) {
    console.log("remove:",data);
}

model.action.showModalTemplate = function () {
    if (model.data.configTemplate() == null) {
        model.data.configTemplate(ko.mapping.fromJS(this.proto.ConfigTemplate));
    } else {
        model.data.configTemplate(ko.mapping.fromJSON(ko.mapping.toJSON(model.data.configTemplate())));
    }
    $("#modalTemplate").modal("show");    
}

model.action.saveConfigTemplate = () => {
    var dialogTitle = "Template";
    var data = ko.mapping.toJS(model.data.configTemplate());
    console.log("data save:", data);

    if (data.Subject == "" || data.Subject == null) {
        swalError("Error", "Please fill subject, do not empty");
    }

    try {
        isLoading(true)
        ajaxPost("/ESS/ConfigLoan/SaveConfigTemplate", data, function (res) {
            isLoading(false);
            if (res.StatusCode == 200) {
                swalSuccess(dialogTitle, res.Message);
                $("#modalTemplate").modal("hide");
            } else {
                swalError(dialogTitle, res.Message);
            }
            isLoading(false);
        }, function (err) {
            swalError(dialogTitle, err.Message);
            isLoading(false);
        });
    } catch (e) {
        isLoading(false);
    }
}

model.get.mailtemplate = async function () {
    var res = await ajax("/ESS/ConfigLoan/GetTemplate", "GET");
    if (res.StatusCode == 200) {
        model.data.configTemplate(res.Data);
        return res.Data || [];
    }
    return [];
    console.log(res.StatusCode, ":", res.Message)
}

function formatNumber(num) {
    return num.toString().replace(/(\d)(?=(\d{3})+(?!\d))/g, '$1,')
}