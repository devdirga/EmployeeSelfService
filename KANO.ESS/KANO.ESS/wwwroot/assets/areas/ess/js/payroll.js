/*
model.newLoan = function () {
    return ko.mapping.fromJS(this.proto.LoanRequest);
};
model.data.loan = ko.observable(model.newLoan());
*/

model.newLoan = function (data) {
    if (data) {
        return ko.mapping.fromJS(Object.assign(_.clone(this.proto.LoanRequest), _.clone(data)));
    }
    return ko.mapping.fromJS(this.proto.LoanRequest);
};
model.data.loan = ko.observable(model.newLoan());

//model.data.loan = ko.observable(ko.mapping.fromJS(ko.mapping.toJS(model.proto.LoanRequest)));
model.data.loanTypeSelected = ko.observable();
model.data.loanTypeDetailSelected = ko.observable();
model.data.maxloanallowedtext = ko.observable();
model.data.maxperiodeallowedtext = ko.observable();
model.data.masakerja = ko.observable();
model.data.minimumloan = ko.observable(0);

model.list.loanPeriode = ko.observableArray();

model.is.enableAdd = ko.observable(false);
model.is.enableSave = ko.observable(false);

model.action.downloadPayslip = function (uid) {
    var dataGrid = $("#gridPayslip").data("kendoGrid").dataSource.getByUid(uid);
    
    if (dataGrid) {
        window.open(`/ESS/Payroll/DownloadPayslip/${dataGrid.ProcessID}/${dataGrid.Filename}`);
    }
}

model.render.gridPayslip = function () {
    let $el = $("#gridPayslip");
    if (!!$el) {
        let $grid = $el.getKendoGrid();

        if (!!$grid) {
            $grid.destroy();
        }

        $el.kendoGrid({
            dataSource: {
                data: [],
                transport: {
                    read: "/ESS/Payroll/GetPaySlip"
                },
                schema: {
                    data: (d) => {
                        var data = d.Data || [];                        
                        return data.map(x => {
                            x.MonthYear = new Date(x.Year, x.Month-1, 1);
                            return x;
                        });
                    },
                    total: "Total",
                },
                 group: {
                    field: "Year",
                    dir: "desc"
                },
                sort: { field: "MonthYear", dir: "desc" },
                error: function (e) {
                    swalFatal("Fatal Error", `Error occured while fetching payslip(s)\n${e.xhr.responseText}`)
                }
            },
            noRecords: {
                template: "No payslip data available."
            },
            //filterable: true,
            //sortable: true,
            //pageable: true,
            pageable: {
                previousNext: false,
                info: false,
                numeric: false,
                refresh: true
            },
            columns: [
                {
                    field: "ProcessID",
                    title: "Process ID",
                    width: 200,
                },
                {
                    field: "CycleTimeDescription",
                    title: "Type",
                    width: 200,
                },
                {
                    title: "Period",
                    template: function (e) {
                        return moment(e.MonthYear).format("MMMM YYYY")
                    },
                    width:200,
                },                
                //{
                //    field: "Amount",
                //    title: "Amount",
                //    width: 200,
                //},
                {
                    attributes: {
                        "class": "text-center",
                    },                    
                    template: function (data) {
                        var disabled = false;
                        disabled = !(data && data.Filename && data.Accessible);

                        return `<button class="btn btn-xs ${disabled ? `btn-outline-dark` : `btn-outline-success`}" onclick="model.action.downloadPayslip('${data.uid}'); return false;" ${disabled ? `disabled` : ``}>
                                    <i class="fa mdi mdi-download"></i>
                                </button>`;
                    },
                    width: 50,
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
model.render.gridLoanRequest = function () {
    let $el = $("#gridLoanRequest");
    if (!!$el) {
        let $grid = $el.getKendoGrid();

        if (!!$grid) {
            $grid.destroy();
        }

        $el.kendoGrid({
            dataSource: {
                data: [],
                transport: {
                    read: "/ESS/Payroll/GetLoanRequests"
                },
                schema: {
                    data: (d) => { console.log("datagrid:", d.Data); return d.Data || [] },
                    total: "Total",
                },
                error: function (e) {
                    swalFatal("Fatal Error", `Error occured while fetching loan(s)\n${e.xhr.responseText}`)
                }
            },
            //filterable: true,
            //sortable: true,
            //pageable: true,
            noRecords: {
                template: "No document request data available."
            },
            columns: [
                {
                    field: "IdSimulation",
                    title: "ID Simulation",
                    width: 180,
                    template: function (d) {
                        return d.IdSimulation;
                    }
                },
                {
                    field: "RequestDate",
                    title: "Request Date",
                    width: 100,
                    template: function (d) {
                        //return moment(d.RequestDate).format("DD MMM YYYY");
                        return standarizeDate(d.RequestDate)
                    }
                },
                {
                    field: "LoanValue",
                    title: "Loan Value",
                    width: 120,
                    template: function (d) {
                        return "Rp. " + formatNumber(d.LoanValue);
                    }
                },
                {
                    field: "PeriodeLength",
                    title: "Length",
                    width: 80,
                    template: function (d) {
                        return d.PeriodeLength + " months";
                    }
                },
                {
                    field: "NetIncome",
                    title: "Net Income",
                    width: 120,
                    template: function (d) {
                        return "Rp. " + formatNumber(d.NetIncome);
                    }
                },
                {
                    field: "InstallmentValue",
                    title: "Installment Value",
                    width: 120,
                    template: function (d) {
                        return "Rp. " + formatNumber(d.InstallmentValue);
                    }
                },
                {
                    field: "IncomeAfterInstallment",
                    title: "Income After Installment",
                    width: 140,
                    template: function (d) {
                        return "Rp. " + formatNumber(d.IncomeAfterInstallment);
                    }
                },
                //{
                //    headerAttributes: {
                //        "class": "text-center",
                //    },
                //    attributes: {
                //        "class": "text-center",
                //    },
                //    template: function (d) {
                //        console.log("grid:", d);
                //        return '<button class="btn btn-sm btn-warning">Resend email</button>';
                //    }
                //}
                //{
                //    headerAttributes: {
                //        "class": "text-center",
                //    },
                //    attributes: {
                //        "class": "text-center",
                //    },
                //    template: function (data) {
                //        return `
                //                <button class="btn btn-xs btn-outline-info" onclick="model.action.editLoanRequest('${data.uid}'); return false;">
                //                    <i class="fa mdi mdi-pencil"></i>
                //                </button>
                //                <button class="btn btn-xs btn-outline-danger" onclick="model.action.removeLoanRequest('${data.uid}'); return false;">
                //                    <i class="mdi mdi-close-box"></i>
                //                </button>                             
                //            `
                //    },
                //    width: 75,
                //}
            ]
        });
    }
};

model.action.addLoan = () => {
    model.data.loan(model.newLoan());
    //model.data.loan(ko.mapping.fromJS(model.proto.LoanRequest));
    $("#modalFormLoanRequest").modal("show")
}

model.action.saveLoanRequest = () => {
    var dialogTitle = "Loan";
    var data = ko.mapping.toJS(model.data.loan());

    data.PeriodeLength = periodeLength;
    data.Type = loanTypeSelected;
    data.Type.Detail = loanTypeDetailSelected;
    //console.log("data:", data.Type.Email);
    //if (data.Type.Email == null || data.Type.Email == "") {
    //    swalError(dialogTitle, 'EmailRequest could not be sent, due to empty email configuration, please contact your administrator');
    //    return;
    //}
    if (data.PeriodeLength > maxPeriodeAllowed) {
        swalError(dialogTitle, 'Maximimum loan length is not allowed. Maximum length allowed ' + periodeLength);
        return;
    }
    //if (data.LoanValue() > maxLoanAllowed) {
    //    swalError("Error", "Maximimum loan length is not allowed, Maximum valuew allowed " + maxLoanAllowed);
    //    return false;
    //}
    console.log("data:", data);
    try {
        isLoading(true)
        ajaxPost("/ESS/Payroll/SaveLoanRequest", data, function (res) {
            isLoading(false);
            if (res.StatusCode == 200) {
                $("#gridLoanRequest").data("kendoGrid").dataSource.read();
                swalSuccess(dialogTitle, res.Message);
                $("#modalFormLoanRequest").modal("hide");
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

model.action.editLoanRequest = (uid) => {
    dataGrid = $("#gridLoanRequest").data("kendoGrid").dataSource.getByUid(uid);
    newData = ko.mapping.toJS(model.data.loan(model.newLoan()))
    newData.Id = dataGrid.Id
    newData.EmployeeID = dataGrid.EmployeeID
    newData.Description = dataGrid.Description
    newData.Amount = dataGrid.Amount
    newData.RequestDate = dataGrid.RequestDate
    newData.LoanSchedule = dataGrid.LoanSchedule

    model.data.loan(ko.mapping.fromJS(newData))
    $("#modalFormLoanRequest").modal("show")
}

model.action.removeLoanRequest = async (uid) => {
    var dialogTitle = "Loan";
    var dataGrid = $("#gridLoanRequest").data("kendoGrid").dataSource.getByUid(uid);

    var result = await swalConfirm(dialogTitle, `Are you sure deleting "${dataGrid.Description}" ?`);
    if (result.value) {
        var Id = (dataGrid.Id) ? dataGrid.Id : "";

        try {
            isLoading(true)
            ajaxPost("/ess/payroll/RemoveLoanRequest/" + Id, {}, function (data) {
                isLoading(false)
                if (data.StatusCode == 200) {
                    $("#gridLoanRequest").data("kendoGrid").dataSource.read()
                    swalSuccess(dialogTitle, data.Message);
                } else {
                    swalError(dialogTitle, data.Message);
                }
            }, function (data) {
                swalError(dialogTitle, data.Message);
            });
        } catch (e) {
            isLoading(false);
        }

    }
}

model.list.ConfigLoan = function () {
    $.getJSON("/ESS/ConfigLoan/GetConfig", function (res) {
        console.log("config:", res.Data);
        model.list.loantype(res.Data);
    });
    //$.getJSON("/ESS/Payroll/ConfigurationLoans", function (res) {
    //    console.log("config:", res);
    //    model.list.loantype(res);
    //});
}

model.list.loantype = ko.observableArray();
model.list.loantypename = ko.observableArray();
model.list.listLoanType = function() {
    $.getJSON("/ESS/Payroll/ListLoanType", function (res) {
        console.log(res);
        model.list.loantype(res);
        model.list.loantypename(res.map(x => x.Name));
    });
}

model.list.loantypedetail = ko.observableArray();
model.list.methode = ko.observableArray();
model.list.listLoanTypeDetail = function () {
    $.getJSON("/ESS/Payroll/ListLoanTypeDetail", function (res) {
        console.log(res);
        model.list.loantypedetail(res);
        model.list.methode(_.unionBy(res, "MethodeName"));
        //model.list.loantypedetail(res.map(x => x.LoanTypeName));
    });
}

model.on.openMoalLoanCalculate = function () {
    model.data.loan().RequestDate(moment().format("MM/DD/YYYY"));
    /*
    model.data.loan().EmployeeID(model.data.employee().EmployeeID);
    model.data.loan().EmployeeName(model.data.employee().EmployeeName);
    model.data.loan().Department(model.data.employee().Department);
    model.data.loan().Position(model.data.employee().Position);
    */
    model.data.loan().NetIncome(employeePay.AmountNetto);
    model.action.activeKendoDropDownList($("#LoanMethode"), false);
    model.action.activeKendoDropDownList($("#LoanPeriode"), false);
}

$(document).on('hide.bs.modal', '#modalFormLoanRequest', function () {
    model.data.maxloanallowedtext("");
});

model.data.employee = ko.observable();
model.get.employee = async function () {
    var res = await ajax("/ESS/Payroll/GetEmployee", "GET");

    if (res.StatusCode == 200) {
        model.data.employee(res.Data);
        return res.Data || [];
    }
    return [];    
    console.log(res.StatusCode, ":", res.Message)
}

model.list.payslip = ko.observableArray();
model.get.payslip = async function () {
    var res = await ajax("/ESS/Payroll/GetLatestPaySlip", "GET");
    if (res.StatusCode == 200) {
        console.log("ajax payslip");
        model.list.payslip([res.Data]);
        return res.Data || [];
    }
    return [];
    console.log(res.StatusCode, ":", res.Message)
}

model.init.loan = function () {
    model.render.gridLoanRequest();
    
    setTimeout(async function () {
        //isLoading(true);
        await Promise.all([
            new Promise(async (resolve) => {
                model.list.ConfigLoan();
                //model.list.listLoanType();
                //model.list.listLoanTypeDetail();
                model.is.enableAdd(true);
                resolve(true);
            }),
            /*
            new Promise(async (resolve) => {
                var emp = await model.get.employee();
                console.log("emp:", emp);

                //model.is.enableAdd(true);
                resolve(true);
            }),
            */
            new Promise(async (resolve) => {
                var pay = await model.get.payslip();
                console.log("dt pay:", pay);
                getLastPayslip();
                resolve(true);
            }),
        ]);
        
    });
}

model.action.activeKendoDropDownList = function (el, bool) {
    el.data("kendoDropDownList").enable(bool);
    console.log("el:", el, bool)
}

model.action.resetValue = function () {
    model.data.loan().PeriodeLength(0);
    model.data.loan().CompensationValue(0);
    model.data.loan().InstallmentValue(0);
    model.data.loan().IncomeAfterInstallment(0);
}

model.action.processCalculate = function () {
    loanCalculate();
}

var employeePay = 0;
function getLastPayslip() {
    var p = [];
    model.list.payslip().map(function (x) {
        p.push({
            AmountNetto: x.AmountNetto,
            Year: x.Year,
            Month: x.Month,
            MonthYear: x.ProcessID.split('-')[1],
        });
    });
    console.log("p:", p);
    employeePay = _.maxBy(p, "MonthYear");

    console.log("Emp pay:", employeePay);
}

var loanTypeId, loanMethodeId, periodeLength, maxLoanAllowed = 0, MinimumLimitLoan = 0, loanTypeSelected, loanTypeDetailSelected;
model.data.calculateUse = ko.observableArray();
function typeCalculate(e) {
    model.action.resetValue();

    var dataItem = e.sender.dataItem();
    loanTypeId = dataItem.Id;
    console.log("loan type collect: ", dataItem);
    model.data.loanTypeSelected(dataItem);
    loanTypeSelected = dataItem;
    if (dataItem && parseInt($("#PeriodeLengthNormal").val()) !== NaN) {
        loanCalculate();
    }
    maxLoanAllowed = dataItem.MaximumLoan;
    if (dataItem.MinimumLimitLoan) {
        MinimumLimitLoan = dataItem.MinimumLimitLoan;
        model.data.minimumloan(MinimumLimitLoan);
    } else {
        MinimumLimitLoan = 0;
        model.data.minimumloan(0);
    }
    
    /*
     * cek max loan detail
    var det = [];
    dataItem.Detail.map(function (x) {
        if (parseInt(x.MaximumRangeLoanPeriode) > 0) {
            det.push(x);
        }
    });
    if (det.length > 0) {
        model.data.maxloanallowedtext("Maximum loan allowed Rp." + formatNumber(maxLoanAllowed));
    } else {
        model.data.maxloanallowedtext("Maximum loan allowed Rp." + formatNumber(maxLoanAllowed));
    }
    */

    renderMethode(dataItem);
    model.data.maxloanallowedtext("Maximum loan allowed Rp." + formatNumber(maxLoanAllowed));
    model.data.loan().LoanValue(maxLoanAllowed);
    model.list.loantypedetail(dataItem.Detail);
    model.action.activeKendoDropDownList($("#LoanMethode"));

    model.is.enableSave(false);
};

function renderMethode(data) {
    model.list.methode(_.unionBy(data.Detail, "MethodeName"));
    var el = $("#LoanMethode").data("kendoDropDownList");
    el.setDataSource(model.list.methode());
    el.value("");
    $("#LoanPeriode").data("kendoDropDownList").value("");
}

function methodeCalculate(e) {
    model.action.resetValue();

    var dataItem = e.sender.dataItem();
    loanMethodeId = dataItem.Methode;
    if (dataItem.Methode == 1) {
        //$("#PeriodeLengthNormal").attr("readonly", false);
        //$("#PeriodeLengthCompensation").attr("readonly", true);
        //$("#CompensationValue").attr("readonly", true);
        var compensationValue = $("#CompensationValue").data("kendoNumericTextBox");
        compensationValue.readonly();

        //periodeLength = parseInt($("#PeriodeLengthNormal").val());
        //loanCalculate();

    } else if (dataItem.Methode == 2) {
        //$("#PeriodeLengthNormal").attr("readonly", true);
        //$("#PeriodeLengthCompensation").attr("readonly", false);
        //$("#CompensationValue").attr("readonly", false);
        var compensationValue = $("#CompensationValue").data("kendoNumericTextBox");
        compensationValue.enable();

        //periodeLength = parseInt($("#PeriodeLengthCompensation").val());
        //loanCalculate();
    }
    //var x = model.list.loantypedetail().filter(item => item.IdLoanType === loanTypeId && item.Methode === loanMethodeId);
    //console.log("methode collect:", x);
    var x = model.list.loantype().filter(item => item.Id === loanTypeId);    
    var y = x[0].Detail.filter(item => item.Methode === loanMethodeId);
    console.log("methode collect:", y);

    model.list.loanPeriode(y);
    model.action.activeKendoDropDownList($("#LoanPeriode"), true);
    var p = $("#LoanPeriode").data("kendoDropDownList");
    p.setDataSource(y);
    p.value("");
    model.is.enableSave(false);
}

var maxPeriodeAllowed = 0;
function periodeCalculate(e) {
    model.action.resetValue();

    var dataItem = e.sender.dataItem();
    console.log("periode select:", dataItem);
    model.data.loanTypeDetailSelected(dataItem);
    loanTypeDetailSelected = dataItem;
    maxPeriodeAllowed = dataItem.MaximumRangePeriode;
    if (loanTypeId == 3) {
        maxLoanAllowed = dataItem.MaximumLoad
    }
    model.data.loan().PeriodeLength(dataItem.MaximumRangeLoanPeriode == 0 ? dataItem.MaximumRangePeriode : dataItem.MaximumRangeLoanPeriode);
    model.data.maxperiodeallowedtext("Maximum periode allowed: " + dataItem.MaximumRangePeriode + " months");
}

model.data.installment = ko.observable();
function loanCalculate() {
    var status = true;
    /*
    if (loanMethodeId == 1) {
        periodeLength = parseInt($("#PeriodeLengthNormal").val());
    } else if (loanMethodeId == 2) {
        periodeLength = parseInt($("#PeriodeLengthCompensation").val());
    }
    */
    periodeLength = model.data.loan().PeriodeLength();
    if (periodeLength > maxPeriodeAllowed) {
        swalError("Error", "Loan length not allowed, maximum: " + maxPeriodeAllowed);
        model.data.loan().PeriodeLength(maxPeriodeAllowed);
        model.is.enableSave(false);
        status = false;
        return false;
    } 
    if (model.data.loan().LoanValue() > maxLoanAllowed) {
        swalError("Error", "Loan Value not allowed, maximum: " + formatNumber(maxLoanAllowed));
        model.is.enableSave(false);
        status = false;
        return false;
    }
    //console.log("methode:", loanMethodeId);
    //console.log("periode:", parseInt(periodeLength));
    //var x = model.list.loantypedetail().filter(item => item.IdLoanType === loanTypeId && item.Methode === loanMethodeId);
    //console.log("formula", x);

    var angsuran = 0; bunga = 0, pmt = 0;
    var c = model.list.loantypedetail().find(item =>item.Methode === loanMethodeId && (item.MinimumRangePeriode < periodeLength && item.MaximumRangePeriode >= periodeLength));
    console.log("formula by periode:",c);
    //if (periodeLength > c.MaximumRangePeriode) {
    //    periodeLength = c.MaximumRangePeriode;
    //}
    if (c) {
        bunga = c.Interest;
        console.log("Income:", model.data.loan().NetIncome());
        console.log("Bunga:", bunga);
        console.log("Loan:", model.data.loan().LoanValue());
        console.log("Compensation:", model.data.loan().CompensationValue());
        if (loanTypeId == "Conf-01" && loanMethodeId == 1) {
            console.log("1:", bunga * parseInt($("#PeriodeLengthNormal").val()));
            console.log("2:", 1 + (bunga * parseInt($("#PeriodeLengthNormal").val())));
            console.log("3:", ((1 + (bunga * parseInt($("#PeriodeLengthNormal").val()))) / parseInt($("#PeriodeLengthNormal").val())));
            angsuran = model.data.loan().LoanValue() * (((1 + (bunga * periodeLength)) / periodeLength));
            console.log("angsuran:", angsuran);
            model.data.loan().InstallmentValue(Math.round(angsuran));
            model.data.loan().IncomeAfterInstallment(model.data.loan().NetIncome() - Math.round(angsuran));

        } else if (loanTypeId == "Conf-01" && loanMethodeId == 2) {
            console.log("kompensasi");
            if (parseInt($("#PeriodeLengthCompensation").val()) > c.MaximumRangePeriode) {
                $("#PeriodeLengthCompensation").val(c.MaximumRangePeriode);
            }
            angsuran = model.data.loan().LoanValue() * (((1 + (bunga * periodeLength)) / periodeLength));
            console.log("angsuran:", angsuran);
            model.data.loan().InstallmentValue(Math.round(angsuran));
            model.data.loan().IncomeAfterInstallment(model.data.loan().NetIncome() + model.data.loan().CompensationValue() - Math.round(angsuran));

        } else if (loanTypeId == "Conf-02" && loanMethodeId == 1) {
            if (parseInt($("#PeriodeLengthNormal").val()) > c.MaximumRangePeriode) {
                $("#PeriodeLengthNormal").val(c.MaximumRangePeriode);
            }
            angsuran = model.data.loan().LoanValue() * (((1 + (bunga * parseInt($("#PeriodeLengthNormal").val())))) / parseInt($("#PeriodeLengthNormal").val()));
            console.log("angsuran:", angsuran);
            model.data.loan().InstallmentValue(Math.round(angsuran));
            model.data.loan().IncomeAfterInstallment(model.data.loan().NetIncome() - Math.round(angsuran));

        } else if (loanTypeId == "Conf-02" && loanMethodeId == 2) {
            if (parseInt($("#PeriodeLengthCompensation").val()) > c.MaximumRangePeriode) {
                $("#PeriodeLengthCompensation").val(c.MaximumRangePeriode);
            }
            angsuran = model.data.loan().LoanValue() * (((1 + (bunga * parseInt($("#PeriodeLengthNormal").val())))) / parseInt($("#PeriodeLengthNormal").val()));
            console.log("angsuran:", angsuran);
            model.data.loan().InstallmentValue(Math.round(angsuran));
            model.data.loan().IncomeAfterInstallment(model.data.loan().NetIncome() + model.data.loan().CompensationValue() - Math.round(angsuran));

        } else if (loanTypeId == "Conf-03" && loanMethodeId == 1) {
            angsuran = PMTCalculate(bunga, periodeLength, model.data.loan().LoanValue());
            console.log("angsuran:", parseInt(model.data.loan().NetIncome()), Math.round(angsuran));
            model.data.loan().InstallmentValue(Math.round(angsuran));
            model.data.loan().IncomeAfterInstallment(parseInt(model.data.loan().NetIncome()) + Math.round(angsuran));

        } else if (loanTypeId == "Conf-04" && loanMethodeId == 2) {
            angsuran = PMTCalculate(bunga, periodeLength, model.data.loan().LoanValue());
            console.log("angsuran:", parseInt(model.data.loan().NetIncome()), Math.round(angsuran));
            model.data.loan().InstallmentValue(Math.round(angsuran));
            model.data.loan().IncomeAfterInstallment(parseInt(model.data.loan().NetIncome()) + parseInt(model.data.loan().CompensationValue()) + Math.round(angsuran));
        }

        //console.log(model.data.loan().InstallmentValue() + " - " + MinimumLimitLoan)
        if (model.data.loan().InstallmentValue() >= MinimumLimitLoan) {
            model.is.enableSave(true);
        } else {
            model.is.enableSave(false);
        }
    }
}

function PMTCalculate(rate_per_period, number_of_payments, present_value, future_value, type) {
    future_value = typeof future_value !== 'undefined' ? future_value : 0;
    type = typeof type !== 'undefined' ? type : 0;

    if (rate_per_period != 0.0) {
        // Interest rate exists
        var q = Math.pow(1 + rate_per_period, number_of_payments);
        return -(rate_per_period * (future_value + (q * present_value))) / ((-1 + q) * (1 + rate_per_period * (type)));

    } else if (number_of_payments != 0.0) {
        // No interest rate, but number of payments exists
        return -(future_value + present_value) / number_of_payments;
    }
    
    return 0;
}

function formatNumber(num) {
    return num.toString().replace(/(\d)(?=(\d{3})+(?!\d))/g, '$1,')
}