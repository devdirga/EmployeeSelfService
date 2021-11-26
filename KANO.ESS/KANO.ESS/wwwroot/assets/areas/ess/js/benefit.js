var today = new Date();
today.setDate(today.getDate());
var days30ago = new Date();
days30ago.setDate(days30ago.getDate() - 45);



//Method Creating New Models
model.newMedicalBenefit = function (obj) {
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

model.newVoucher = function () {
    return ko.mapping.fromJS(this.proto.Voucher);
};

//Model Data
model.data = {};
model.list.medicalType = ko.observableArray([]);
model.list.family = ko.observableArray([]);
model.list.documentType = ko.observableArray([]);
model.list.requestFor = ko.observableArray([{ AXID: 0, Name: "SELF" }]);
model.list.status = ko.observableArray([]);
model.data.medicalBenefit = ko.observable(model.newMedicalBenefit());
model.data.document = ko.observable(model.newMedicalBenefitDetail());

//limit claim
model.data.limit = {
    CreditLimitAmount: 0
};

model.map.medicalType = {};
model.map.documentType = {};
model.map.family = {};
model.is.forFamily = ko.observable(false);
model.is.requestActive = ko.observable(false);
model.is.buttonLoading = ko.observable(false);

model.data.attachmentfiles = ko.observableArray([]);
model.data.recordreimburse = ko.observable();

//filter medical benefit
model.data.voucher = ko.observable(model.newVoucher());
model.data.StartDate = ko.observable(moment().subtract(30, "days").toDate());
model.data.EndDate = ko.observable(today);
model.data.Status = ko.observable("");
//end filter

//Method Get Data
model.get = {};

//Method UI Component Render
model.render = {};

//model.render.formEmployee = async function () {
//    let data = await model.get.employee({});
//    model.data.employee(ko.mapping.fromJS(data));
//};

//Method get medical type
model.get.medicalType = async function () {
    let response = await ajax("/ESS/Benefit/GetMedicalType", "GET");
    if (response.StatusCode == 200) {
        let result = response.Data || []
        return result;
    }
    return [];
};

//Method get family
model.get.family = async function () {
    let response = await ajax("/ESS/Benefit/GetFamilies", "GET");

    if (response.StatusCode == 200) {
        return response.Data || [];
    }
    return [];
}

//Method get document type
model.get.documentType = async function () {
    let response = await ajax("/ESS/Benefit/GetDocumentType", "GET");
    if (response.StatusCode == 200) {
        let result = response.Data || []
        return result;
    }
    return [];
}

//Render grid
model.render.gridReimburseHistory = function () {
    let $el = $("#gridReimburseHistory");
    if (!!$el) {
        let $grid = $el.getKendoGrid();
        if (!!$grid) {
            $grid.destroy();
        }
        $el.kendoGrid({
            dataSource: {
                data: [],
                transport: {
                    read: {
                        url: "/ESS/Benefit/GetReimburse",
                        dataType: "json",
                        type: "POST",
                        contentType: "application/json",
                    },
                    parameterMap: function (data, type) {
                        data.Range = {
                            Start: model.data.StartDate(),
                            Finish: model.data.EndDate(),
                        }
                        data.Status = model.data.Status() || -1;
                        return JSON.stringify(data);
                    },
                },
                schema: {
                    data: function (res) {
                        if (res.StatusCode !== 200 && res.Status !== '') {
                            swalFatal("Fatal Error", `Error occured while fetching reimburses(s)\n${res.Message}`)
                            return []
                        }
                        return res.Data || [];
                    },
                    total: "Total",
                },
                error: function (e) {
                    swalFatal("Fatal Error", `Error occured while fetching reimburse(s)\n${e.xhr.responseText}`)
                }
            },
            filterable: false,
            sortable: false,
            pageable: {
                previousNext: false,
                info: false,
                numeric: false,
                refresh: true
            },
            noRecords: {
                template: "No Reimburse data available."
            },
            columns: [
                {
                    field: "RequestID",
                    title: "Request No.",
                    width: '150px',
                    template: function (d) {
                        return d.RequestID || "-";
                    },
                },
                {
                    field: "Request For",
                    template: function (d) {
                        if (!d.Family || (!!d.Family && d.Family.AXID == -1)) {
                            return "Self";
                        }

                        return d.Family.Name
                    },
                    width: '150px'
                },
                {
                    field: "RequestDate",
                    title: "Request Date",
                    template: function (d) {
                        return moment(d.RequestDate).format("DD MMM YYYY")
                    },
                    width: '150px'
                },
                {
                    field: "TypeID",
                    title: "Jenis",
                    template: function (d) {
                        return (camelToTitle(model.map.medicalType[d.TypeID]) || {}) || '-';
                    },
                    width: '100px'
                },
                {
                    field: "TotalAmount",
                    title: "Total Amount",
                    template: function (d) {
                        return kendo.toString(d.TotalAmount, 'c2');
                    },
                    width: '150px'
                },
                {
                    field: "Status",
                    title: "Status",
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
                    template: function (d) {

                        let statusClass = "secondary";
                        let statusLabel = "undefined";
                        if (d.RequestStatus > 0) {
                            switch (d.RequestStatus) {
                                case 1:
                                    statusClass = "info";
                                    statusLabel = "in review";
                                    break;
                                case 2:
                                    statusClass = "success";
                                    statusLabel = "approved";
                                    break;
                                case 3:
                                    statusClass = "primary";
                                    statusLabel = "paid";
                                    break;
                                default:
                                    break;
                            }
                        } else {
                            switch (d.Status) {
                                case 0:
                                    statusLabel = "in review";
                                    break;
                                case 1:
                                    statusClass = "success";
                                    statusLabel = "approved";
                                    break;
                                case 2:
                                    statusClass = "warning";
                                    statusLabel = "cancelled";
                                    break;
                                case 3:
                                    statusClass = "rejected";
                                    statusLabel = "rejected";
                                    break;
                                default:
                                    break;
                            }
                        }
                        return `
                            <a href="#" onclick="model.app.action.trackTask('${d.AXRequestID}'); return false;">
                                <span class="badge badge-${statusClass}">${statusLabel}</span>
                            </a>`
                    },
                    width: '100px'
                },
                {
                    field: "LastUpdate",
                    title: "Last Status Update",
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
                    template: function (d) {
                        return relativeDate(d.LastUpdate);
                    },
                    width: '200px'
                },
                {
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
                    template: function (d) {
                        return `<button class="btn btn-xs btn-outline-info" onclick="model.action.openBenefit('${d.uid}')"><i class="fa mdi mdi-eye"></i></button></br>`;
                        //var htmlreturn = '';
                        //d.Details.forEach(function (elm, i) {
                        //    htmlreturn += `<button class="btn btn-xs btn-outline-dark" onclick="model.action.openBenefit('${d.uid}', '${d.Details[i].Attachment.Filename}')"><i class="fa mdi mdi-download"></i></button>${elm.Attachment.Filename}</br>`;
                        //});
                        //return htmlreturn
                    },

                },

            ]
        });
    }

};

//Actions
model.action = {};

model.action.addNotes = function (data) {
    data.isNoteExists(true);
};

model.action.attachFile = function (data) {
    var data = ko.toJS(data);
    var uploader = $(`[data-guid='${data.guid}']`).getKendoUpload();
    if (uploader) {
        uploader.element.click();
        return;
    }
}

//action medical
model.action.refreshMedical = function (uiOnly = false) {

    if (model.data.StartDate() > model.data.EndDate()) {
        swalError("Warning", "start date is greater than the end date");
        return false;
    };

    var $grid = $("#gridReimburseHistory").data("kendoGrid");
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
model.action.refreshMedicalMonthly = function (uiOnly = false) {
    model.data.StartDate(moment().startOf('month').format("MM/DD/YYYY"));
    model.data.EndDate(today);
    var $grid = $("#gridReimburseHistory").data("kendoGrid");
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
model.action.refreshMedicalYearly = function (uiOnly = false) {
    model.data.StartDate(moment().startOf('year').format("MM/DD/YYYY"));
    model.data.EndDate(today);
    var $grid = $("#gridReimburseHistory").data("kendoGrid");
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
model.action.addBenefit = function () {
    model.data.medicalBenefit(model.newMedicalBenefit());
    model.data.medicalBenefitDetails([]);
    $("#modalFormReimburseRequest").modal("show");

    // model.data.limit = model.get.limit();
};

model.action.openBenefit = function (uid) {
    var dataGrid = $("#gridReimburseHistory").data("kendoGrid").dataSource.getByUid(uid);
    model.data.medicalBenefit(model.newMedicalBenefit(dataGrid));
    model.data.medicalBenefitDetails([]);
    dataGrid.Details.forEach(d => {
        model.data.medicalBenefitDetails.push(model.newMedicalBenefitDetail(d));
    });
    $("#modalBenefitReadonly").modal("show");
}

model.action.saveMedicalBenefit = async function () {
    var result = await swalConfirm('Medical benefit', 'Are you sure to request medical benefit?');
    if (result.value) {
        try {
            isLoading(true);
            model.is.requestActive(false);
            $model = $("#modalFormReimburseRequest");            

            var attachment = ko.mapping.toJS(model.data.medicalBenefitDetails());
            var fileAttach = []
            for (var i = 0; i < attachment.length; i++) {
                var filename = attachment[i].Attachment.Filename

                if (filename == null || filename == "") {
                    fileAttach.push(i)
                }
            }
            if (fileAttach.length > 0) {
                isLoading(false)
                swalAlert("Medical Benefit", "Please attach file into your medical detail");
                return;
            }

            model.data.medicalBenefit().Details = model.data.medicalBenefitDetails();
            var reimburse = ko.mapping.toJS(model.data.medicalBenefit());

            var family = model.data.medicalBenefit().Family;

            var reqfor = $("#requestfor").getKendoDropDownList();
            if (reqfor.value() != 0) {
                reimburse.Family = _.find(model.list.requestFor(), function (m) {
                    return m.AXID == reqfor.value();
                });
            } else {
                reimburse.Family = family;
            }

            let formData = new FormData();
            formData.append("JsonData", JSON.stringify(reimburse));
            try {
                $model.modal("hide");
                ajaxPostUpload("/ESS/Benefit/SaveReimburse", formData, function (data) {
                    if (data.StatusCode == 200) {
                        swalSuccess("Medical Benefit", data.Message);
                        model.action.refreshRequestStatus();
                        model.action.refreshMedical();
                    } else {
                        swalError("Medical Benefit", data.Message);
                    }
                    isLoading(false);
                }, function (data) {
                    isLoading(false);
                    swalFatal("Medical Benefit", data.Message);
                })
            } catch (e) {
                swalFatal("Medical Benefit", e)
                isLoading(false)
            }
        } catch (e) {
            isLoading(false);
            console.error(e);
        }

    }

}
//end action medical

//pagging
//model is
model.is.renderingUsed = ko.observable(false);
model.is.renderingUnused = ko.observable(false);
model.is.renderingNA = ko.observable(false);
//end model is

//

//pagging unused
//model data
model.data.Unused = ko.observableArray([]);
model.data.totalUnused = ko.observable(0);
model.data.pageUnused = ko.observable(1);
model.data.counterUnused = ko.observable(0);
//model data end

model.get.Unused = async function (limit = 10, offset = 0) {
    let response = await ajax("/ESS/Benefit/GetVoucher", "POST", JSON.stringify({ Limit: limit, Offset: offset }));
    if (response.StatusCode == 200) {
        let data = response.Data || [];
        let total = response.Total || 0;
        return {
            Data: data,
            Total: total
        };

        return result;
    }
    return [];
};
model.data.preProcessUnused = function (data) {
    let self = model;
    for (var i in data) {
        var d = data[i];
        self.data.Unused.push(ko.mapping.fromJS(d));
    }
};
let _limitUnused = 10;
model.render.UnusedList = function (limit = _limitUnused, offset = 0) {
    let self = model;
    return new Promise((resolve, reject) => {
        //
        let currentPage = self.data.pageUnused();
        setTimeout(async function () {
            self.is.renderingUnused(true);
            let Unused = await model.get.Unused(limit, offset);
            console.log("unused:", Unused)
            self.data.totalUnused(Unused.Total);
            self.data.preProcessUnused(Unused.Data);
            self.data.counterUnused((currentPage - 1) * limit + Unused.Data.length);
            self.is.renderingUnused(false);
            resolve(true);
        });
    });
}
model.action.prevUnusedPage = function () {
    let self = model;

    if (!self.is.renderingUnused()) {
        let currentPage = self.data.pageUnused();
        let counter = self.data.counterUnused();
        if (counter > _limitUnused) {
            currentPage--;
            let offset = (currentPage - 1) * _limitUnused;
            self.data.pageUnused(currentPage);
            self.render.UnusedList(_limitUnused, offset);
        }
    }
};
model.action.nextUnusedPage = function () {
    let self = model;

    if (!self.is.renderingUnused()) {
        let currentPage = self.data.pageUnused();
        let total = self.data.totalUnused();
        let counter = self.data.counterUnused();
        if (total > counter) {
            currentPage++;
            let offset = (currentPage - 1) * _limitUnused;
            self.data.pageUnused(currentPage);
            self.render.UnusedList(_limitUnused, offset);
        }
    }
};
//pagging unused end

//

//pagging used
//model data
model.data.Used = ko.observableArray([]);
model.data.totalUsed = ko.observable(0);
model.data.pageUsed = ko.observable(1);
model.data.counterUsed = ko.observable(0);
//model data end

model.get.Used = async function (limit = 10, offset = 0) {
    let response = await ajax("/ESS/Benefit/GetVoucher", "POST", JSON.stringify({ Limit: limit, Offset: offset }));
    if (response.StatusCode == 200) {
        let data = response.Data || [];
        let total = response.Total || 0;
        return {
            Data: data,
            Total: total
        };

        return result;
    }
    return [];
};
model.data.preProcessUsed = function (data) {
    let self = model;
    for (var i in data) {
        var d = data[i];
        self.data.Used.push(ko.mapping.fromJS(d));
    }
};
let _limitUsed = 10;
model.render.UsedList = function (limit = _limitUsed, offset = 0) {
    let self = model;
    return new Promise((resolve, reject) => {
        //
        let currentPage = self.data.pageUsed();
        setTimeout(async function () {
            self.is.renderingUsed(true);
            let Used = await model.get.Used(limit, offset);
            self.data.totalUsed(Used.Total);
            self.data.preProcessUsed(Used.Data);
            self.data.counterUsed((currentPage - 1) * limit + Used.Data.length);
            self.is.renderingUsed(false);
            resolve(true);
        });
    });
}
model.action.prevUsedPage = function () {
    let self = model;

    if (!self.is.renderingUsed()) {
        let currentPage = self.data.pageUsed();
        let counter = self.data.counterUsed();
        if (counter > _limitUsed) {
            currentPage--;
            let offset = (currentPage - 1) * _limitUsed;
            self.data.pageUsed(currentPage);
            self.render.UsedList(_limitUsed, offset);
        }
    }
};
model.action.nextUsedPage = function () {
    let self = model;

    if (!self.is.renderingUsed()) {
        let currentPage = self.data.pageUsed();
        let total = self.data.totalUsed();
        let counter = self.data.counterUsed();
        if (total > counter) {
            currentPage++;
            let offset = (currentPage - 1) * _limitUsed;
            self.data.pageUsed(currentPage);
            self.render.UsedList(_limitUsed, offset);
        }
    }
};
//pagging used end

//

//pagging used
//model data
model.data.NA = ko.observableArray([]);
model.data.totalNA = ko.observable(0);
model.data.pageNA = ko.observable(1);
model.data.counterNA = ko.observable(0);
//model data end

model.get.NA = async function (limit = 10, offset = 0) {
    let response = await ajax("/ESS/Benefit/GetVoucher", "POST", JSON.stringify({ Limit: limit, Offset: offset }));
    if (response.StatusCode == 200) {
        let data = response.Data || [];
        let total = response.Total || 0;
        return {
            Data: data,
            Total: total
        };

        return result;
    }
    return [];
};
model.data.preProcessNA = function (data) {
    let self = model;
    for (var i in data) {
        var d = data[i];
        self.data.NA.push(ko.mapping.fromJS(d));
    }
};
let _limitNA = 10;
model.render.NAList = function (limit = _limitNA, offset = 0) {
    let self = model;
    return new Promise((resolve, reject) => {
        //
        let currentPage = self.data.pageNA();
        setTimeout(async function () {
            self.is.renderingNA(true);
            let NA = await model.get.NA(limit, offset);
            self.data.totalNA(NA.Total);
            self.data.preProcessNA(NA.Data);
            self.data.counterNA((currentPage - 1) * limit + NA.Data.length);
            self.is.renderingNA(false);
            resolve(true);
        });
    });
}
model.action.prevNAPage = function () {
    let self = model;

    if (!self.is.renderingNA()) {
        let currentPage = self.data.pageNA();
        let counter = self.data.counterNA();
        if (counter > _limitNA) {
            currentPage--;
            let offset = (currentPage - 1) * _limitNA;
            self.data.pageNA(currentPage);
            self.render.NAList(_limitNA, offset);
        }
    }
};
model.action.nextNAPage = function () {
    let self = model;

    if (!self.is.renderingNA()) {
        let currentPage = self.data.pageNA();
        let total = self.data.totalNA();
        let counter = self.data.counterNA();
        if (total > counter) {
            currentPage++;
            let offset = (currentPage - 1) * _limitNA;
            self.data.pageNA(currentPage);
            self.render.NAList(_limitNA, offset);
        }
    }
};
//pagging used end

//pagging

model.list.medicalType.subscribe(function () {
    model.action.refreshMedical(true);
});

model.init.medicalBenefit = function () {
    model.data.medicalBenefit(model.newMedicalBenefit());
    model.data.document(model.newMedicalBenefitDetail());
    var self = model;
    /*
    setTimeout(async function () {
        var data = self.proto.RequestFor || [];
        data.forEach((d, i) => {
            self.list.requestFor.push({
                text: d,
                value: i,
            });
        });

    });
    */
    setTimeout(async function () {
        var data = self.proto.Status || [];
        data.forEach((d, i) => {
            model.list.status.push({
                text: d,
                value: i,
            });
        });

    });

    setTimeout(async function () {
        model.is.buttonLoading(true);
        await Promise.all([
            new Promise(async (resolve) => {
                let medicalType = await model.get.medicalType();
                medicalType.forEach((d, i) => {
                    model.map.medicalType[i] = d;
                });

                model.list.medicalType(strArray2DropdownList(medicalType, camelToTitle));

                resolve(true);
            }),
            new Promise(async (resolve) => {
                let family = await model.get.family();
                model.list.family(family);

                model.list.requestFor([].concat(model.list.requestFor(), family));
                resolve(true);
            }),
            new Promise(async (resolve) => {
                let documentType = await model.get.documentType();
                documentType.forEach(d => {
                    model.map.documentType[d.TypeID] = d;
                })
                model.list.documentType(documentType);

                resolve(true);
            }),
            new Promise(async (resolve) => {
                model.render.gridReimburseHistory();
                resolve(true);
            }),
            //new Promise(async (resolve) => {
            //    model.get.limit();
            //    resolve(true);
            //}),
            new Promise(async (resolve) => {
                model.action.refreshRequestStatus();
                resolve(true);
            }),
        ]);

        model.is.buttonLoading(false);
    });
}

model.data.medicalBenefitDetails = ko.observableArray();
model.action.document = function () {
    var self = this;
    self.typeid = ko.observable();
    self.file = ko.observable();
}
model.action.addDocument = function () {
    model.data.medicalBenefitDetails.push(model.newMedicalBenefitDetail());
}
model.action.deleteDocument = function (typeid) {
    model.data.medicalBenefitDetails.remove(typeid);
}
model.action.saveDocument = function () {
    console.log(model.data.medicalBenefitDetails());
}

/*
model.on.typeChange = function (e) {
    model.is.forFamily((this.value() != 0));
}
*/
model.on.familyChange = function (e) {
    model.data.medicalBenefit().Family = model.map.family[this.value()];
}

model.on.uploadMedicalAttachment = function (e) {
    var self = model;
    var data = ko.mapping.toJS(self.data.document());
    var field = e.sender.element.data("field");

    if (e.files.length > 0) {
        e.formData = new FormData();
        e.formData.append("Field", field);
        e.formData.append("JsonData", ko.mapping.toJSON(data));
        e.formData.append("FileUpload", e.files[0].rawFile);
    }
};

model.on.uploadMedicalAttachmentSelected = function (e) {
    var self = model;

    var valid = self.app.data.validateKendoUpload(e, "Medical Benefit");
    if (valid) {
        var guid = e.sender.element.data("guid");
        var data = self.data.medicalBenefitDetails().find(d => { return d.guid() == guid });
        if (data) {
            data.isUploading(true);
        } else {
            e.preventDefault();
        }
    } else {
        e.preventDefault();
    }
}

model.on.uploadMedicalAttachmentProgress = function (e) {
    var self = model;
    var guid = e.sender.element.data("guid");
    var data = self.data.medicalBenefitDetails().find(d => { return d.guid() == guid });
    if (data) {
        data.isUploading(true);
    } else {
        e.preventDefault();
    }

};

model.on.uploadMedicalAttachmentSuccess = function (e) {
    var self = model;
    var guid = e.sender.element.data("guid");
    var data = self.data.medicalBenefitDetails().find(d => { return d.guid() == guid });
    data.isUploading(false);
    if (e.response.StatusCode == 200) {
        for (var i in data.Attachment) {
            if (typeof data.Attachment[i] == 'function') data.Attachment[i](e.response.Data.Attachment[i]);
        }
    } else {
        data.Attachment(ko.mapping.fromJS(self.proto.Attachment))
        this.clearAllFiles();
        swalFatal("Medical benefit", `Error occured while uploading medical attachment:\n${JSON.stringify(e.response.Message)}`);
    }
};

model.on.uploadMedicalAttachmentError = function (e) {
    var self = model;
    var guid = e.sender.element.data("guid");
    var data = self.data.medicalBenefitDetails().find(d => { return d.guid() == guid });
    data.isUploading(false);
    for (var i in data.Attachment) {
        if (typeof data.Attachment[i] == 'function') data.Attachment[i](e.response.Data.Attachment[i]);
    }
    this.clearAllFiles();
    swalFatal("Medical benefit", `Error occured while uploading medical attachment:\n${JSON.stringify(e.XMLHttpRequest)}`);
};

model.on.amountChange = function () {
    let self = model;
    setTimeout(() => {
        var total = self.data.calculateTotalAmount();
    }, 300);

};

model.data.calculateTotalAmount = function () {
    let self = model;
    var data = ko.mapping.toJS(self.data.medicalBenefitDetails) || [];
    let total = 0;

    data.forEach(d => {
        total += d.Amount;
    });

    self.data.medicalBenefit().TotalAmount(total);
    return total;
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

model.get.limitList = async function () {
    let response = await ajax("/ESS/Benefit/GetCreditLimitList", "GET");
    if (response.StatusCode == 200) {
        let result = response.Data || []
        return result;
    }
    return [];
}

model.get.limit = async function () {
    //kendo.ui.progress($("#modalFormReimburseRequest .modal-body"), true);
    let response = await ajax("/ESS/Benefit/GetCreditLimit", "GET");
    if (response.StatusCode == 200) {
        let result = response.Data
        return result;
    }
    return [];
    //kendo.ui.progress($("#modalFormReimburseRequest .modal-body"), false);
}

model.get.isRequestActive = async function () {
    let response = await ajax("/ESS/Benefit/IsRequestActive", "GET");
    if (response.StatusCode == 200) {
        return response.Data;
    }

    return [];
}

model.action.refreshRequestStatus = async function () {
    var self = model;

    var response = await self.get.isRequestActive();
    self.is.requestActive(!!response);
};
