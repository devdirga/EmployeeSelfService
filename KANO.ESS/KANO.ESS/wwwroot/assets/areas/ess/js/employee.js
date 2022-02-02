const DEFAULT_DATE = new Date(1900, 0, 1);
const DEFAULT_DATE_FORMAT = "yyyy-MM-dd'T'HH:mm:ss.SSS'Z'";

//Method Creating New Models
model.newEmployee = function (obj) {
    var o = _.clone(this.proto.Employee);
    if (!o) return;

    if (obj && typeof obj == "object") {
        o = Object.assign(_.clone(this.proto.Employee), obj);
    }

    let eAttachment = o.IsExpartriateAttachment || _.clone(this.proto.FieldAttachment);
    if (eAttachment) {
        o.IsExpartriateAttachment = ko.observable(ko.mapping.fromJS(eAttachment));
        o.IsExpartriateAttachment().OldData(o.IsExpatriate);
    }

    let mAttachment = o.MaritalStatusAttachment || _.clone(this.proto.FieldAttachment);
    if (mAttachment) {
        o.MaritalStatusAttachment = ko.observable(ko.mapping.fromJS(mAttachment));
        o.MaritalStatusAttachment().OldData(o.MaritalStatus);
    }

    return ko.mapping.fromJS(o);
};

model.newFamily = function (obj) {
    if (obj && typeof obj == "object") {
        return ko.mapping.fromJS(Object.assign(_.clone(this.proto.Family), obj));
    }
    return ko.mapping.fromJS(this.proto.Family);
};

model.newDocument = function (obj) {
    if (obj && typeof obj == "object") {
        return ko.mapping.fromJS(Object.assign(_.clone(this.proto.Document), obj));
    }
    return ko.mapping.fromJS(this.proto.Document);
};

model.newDocumentRequest = function (obj) {
    if (obj && typeof obj == "object") {
        return ko.mapping.fromJS(Object.assign(_.clone(this.proto.DocumentRequest), obj));
    }
    return ko.mapping.fromJS(this.proto.DocumentRequest);
};

model.newUpdateRequest = function () {
    return ko.mapping.fromJS(this.proto.UpdateRequest);
};

model.newCertificate = function (obj) {
    let certificate = _.clone(this.proto.Certificate);

    if (certificate) {
        certificate.Validity = _.clone(this.proto.DateRange || {});
        certificate.Validity.Start = new Date();
        certificate.Validity.Finish = new Date();
    }
    if (obj && typeof obj == "object") {
        return ko.mapping.fromJS(Object.assign(certificate, obj));
    }
    return ko.mapping.fromJS(certificate);
};

model.newField = function (data, label, category, info = "") {
    return ko.mapping.fromJS(Object.assign(data, {
        Label: label,
        Category: category,
        UID: kendo.guid(),
        Info: info,
    }));
};

//Model Data
model.list.familyRelationship = ko.observableArray([]);

model.list.documentType = ko.observableArray([]);

model.list.documentRequestType = ko.observableArray([]);

model.list.religion = ko.observableArray([]);

model.list.maritalStatus = ko.observableArray([]);

model.list.gender = ko.observableArray([]);

model.list.city = ko.observableArray([]);

model.list.certificateType = ko.observableArray([]);

model.list.city.subscribe(function () {
    setTimeout(() => {
        model.data.employee().Address.City.valueHasMutated();
    });
});

// Model maps
model.map.gender = [];
model.map.religion = [];
model.map.maritalStatus = [];
model.map.certificateType = {};
model.map.familyRelationship = {};
model.map.fieldCategory = {
    "ID": '/ESS/Employee/SaveIdentification',
    "BankAccount": '/ESS/Employee/SaveBankAccount',
    "ElectronicAddress": '/ESS/Employee/SaveElectronicAddress',
    "Tax": '/ESS/Employee/SaveTax',
};

//Model Data
model.data.employeeFields = ko.observableArray([]);

model.data.employee = ko.observable(model.newEmployee());

model.data.originalEmployee = ko.observable(model.newEmployee());

model.data.family = ko.observable(model.newFamily());

model.data.document = ko.observable(model.newDocument());

model.data.documentRequest = ko.observable(model.newDocumentRequest());

model.data.updateRequest = ko.observable(model.newUpdateRequest());

model.data.certificate = ko.observable(model.newCertificate());

model.data.employeeFieldsDivider = ko.observable(ko.mapping.fromJS({
    Left: 0,
    Right: 0
}));

//Method Flags
model.is.renderingEmployeeForm = ko.observable(true);
model.is.requestActive = ko.observable(false);
model.is.requestNOTActive = ko.observable(true);
model.is.requestActive.subscribe(function (v) {
    model.is.requestNOTActive(!v);
    var $grid = $("#gridFamily").data("kendoGrid");
    if ($grid) {
        if (v) {
            $grid.hideColumn(0);
            $grid.hideColumn($grid.columns.length - 1);
        } else {
            $grid.showColumn(0);
            $grid.showColumn($grid.columns.length - 1);
        }
    }

});

//Method Get Data
model.get.employee = async function (params = {}) {
    let response = await ajax("/ESS/Employee/Get", "GET");
    if (response.StatusCode == 200) {
        return response.Data;
    }

    console.error(response.StatusCode, ":", response.Message)
    return Object.assign({}, model.proto.Employee);
};

model.get.bankAccounts = async function (params = {}) {
    let response = await ajax("/ESS/Employee/GetBankAccounts", "GET");
    if (response.StatusCode == 200) {
        return response.Data || [];
    }

    console.error(response.StatusCode, ":", response.Message)
    return Object.assign({}, model.proto.Employee);
};

model.get.taxes = async function (params = {}) {
    let response = await ajax("/ESS/Employee/GetTaxes", "GET");
    if (response.StatusCode == 200) {
        return response.Data || [];
    }

    console.error(response.StatusCode, ":", response.Message)
    return Object.assign({}, model.proto.Employee);
};

model.get.electronicAddresses = async function (params = {}) {
    let response = await ajax("/ESS/Employee/GetElectronicAddresses", "GET");
    if (response.StatusCode == 200) {
        return response.Data || [];
    }

    console.error(response.StatusCode, ":", response.Message)
    return Object.assign({}, model.proto.Employee);
};

model.get.identifications = async function (params = {}) {
    let response = await ajax("/ESS/Employee/GetIdentifications", "GET");
    if (response.StatusCode == 200) {
        return response.Data || [];
    }

    console.error(response.StatusCode, ":", response.Message)
    return Object.assign({}, model.proto.Employee);
};

model.get.identificationType = async function () {
    let response = await ajax("/ESS/Employee/GetIdentificationType", "GET");
    if (response.StatusCode == 200) {
        return response.Data || [];
    }

    console.error(response.StatusCode, ":", response.Message)
    return [];
};

model.get.address = async function (params = {}) {
    let response = await ajax("/ESS/Employee/GetAddress", "GET");
    if (response.StatusCode == 200) {
        return response.Data;
    }

    console.error(response.StatusCode, ":", response.Message)
    return Object.assign({}, model.proto.Employee);
};

model.get.familyRelationship = async function () {
    let response = await ajax("/ESS/Employee/GetFamilyRelationship", "GET");
    if (response.StatusCode == 200) {
        return response.Data;
    }

    console.error(response.StatusCode, ":", response.Message)
    return [];
};

model.get.certificateType = async function () {
    let response = await ajax("/ESS/Employee/GetCertificateType", "GET");
    if (response.StatusCode == 200) {
        return response.Data;
    }

    console.error(response.StatusCode, ":", response.Message)
    return [];
};

model.get.documentType = async function () {
    let response = await ajax("/ESS/Employee/GetDocumentType", "GET");
    if (response.StatusCode == 200) {
        return response.Data;
    }

    console.error(response.StatusCode, ":", response.Message)
    return [];
};

model.get.documentRequestType = async function () {
    let response = await ajax("/ESS/Employee/GetDocumentRequestType", "GET");
    if (response.StatusCode == 200) {
        return response.Data;
    }

    console.error(response.StatusCode, ":", response.Message)
    return [];
};

model.get.religion = async function () {
    let response = await ajax("/ESS/Employee/GetReligion", "GET");
    if (response.StatusCode == 200) {
        return response.Data;
    }

    console.error(response.StatusCode, ":", response.Message)
    return [];
};

model.get.maritalStatus = async function () {
    let response = await ajax("/ESS/Employee/GetMaritalStatus", "GET");
    if (response.StatusCode == 200) {
        return response.Data;
    }

    console.error(response.StatusCode, ":", response.Message)
    return [];
};

model.get.electronicAddressType = async function () {
    let response = await ajax("/ESS/Employee/GetElectronicAddressType", "GET");
    if (response.StatusCode == 200) {
        return response.Data;
    }

    console.error(response.StatusCode, ":", response.Message)
    return [];
};

model.get.gender = async function () {
    let response = await ajax("/ESS/Employee/GetGender", "GET");
    if (response.StatusCode == 200) {
        return response.Data;
    }

    console.error(response.StatusCode, ":", response.Message)
    return [];
};

model.get.city = async function () {
    let response = await ajax("/ESS/Employee/GetCity", "GET");
    if (response.StatusCode == 200) {
        return response.Data;
    }

    console.error(response.StatusCode, ":", response.Message)
    return [];
};

model.get.isRequestActive = async function () {
    let response = await ajax("/ESS/Employee/IsRequestActive", "GET");
    if (response.StatusCode == 200) {
        return response.Data;
    }

    console.error(response.StatusCode, ":", response.Message)
    return {};
};

//Method UI Component Render
model.render.formEmployee = async function () {
    var self = model;
    var fields = [];
    var addressResult = {};
    self.is.renderingEmployeeForm(true);

    await Promise.all([
        // Get Employee
        new Promise(async (resolve) => {
            let data = await self.get.employee({});
            if (data) {
                self.data.employee(self.newEmployee(data.Employee));
                if (data.UpdateRequest != null) {
                    self.data.employee(self.newEmployee(data.UpdateRequest));
                    self.data.originalEmployee(self.newEmployee(data.Employee));
                } else {
                    self.data.employee(self.newEmployee(data.Employee));
                    self.data.originalEmployee(self.newEmployee(data.Employee));
                }
            }
            resolve(true);
        }),

        // Get Bank Accounts
        new Promise(async (resolve) => {
            let bankAccounts = await self.get.bankAccounts();
            bankAccounts.forEach((bankAccount, i) => {
                let ba = {};
                if (bankAccount.UpdateRequest) {
                    ba = Object.assign(_.cloneDeep(bankAccount.BankAccount || {}), _.cloneDeep(bankAccount.UpdateRequest || {}));
                } else {
                    ba = Object.assign(_.cloneDeep(bankAccount.UpdateRequest || {}), _.cloneDeep(bankAccount.BankAccount || {}));
                }

                let label = `Bank Account${(bankAccounts.length > 1) ? " #" + (i + 1) : ""}`,
                    info = ba.Name;

                ba.NewData = (bankAccount.UpdateRequest) ? bankAccount.UpdateRequest.AccountNumber : ((bankAccount.BankAccount) ? bankAccount.BankAccount.AccountNumber : "");
                ba.OldData = (bankAccount.BankAccount) ? (bankAccount.BankAccount.AccountNumber || "") : "";

                fields.push(self.newField(ba, label, "BankAccount", info));
            });
            resolve(true);
        }),

        // Get Tax
        new Promise(async (resolve) => {
            let taxes = await self.get.taxes();
            taxes.forEach((tax, i) => {
                let ba = {};
                if (tax.UpdateRequest) {
                    ba = Object.assign(_.cloneDeep(tax.Tax || {}), _.cloneDeep(tax.UpdateRequest || {}));
                } else {
                    ba = Object.assign(_.cloneDeep(tax.UpdateRequest || {}), _.cloneDeep(tax.Tax || {}));
                }

                let label = `NPWP${(taxes.length > 1) ? " #" + (i + 1) : ""}`,
                    info = ba.Name;

                ba.NewData = (tax.UpdateRequest) ? tax.UpdateRequest.NPWP : ((tax.Tax) ? tax.Tax.NPWP : "");
                ba.OldData = (tax.Tax) ? (tax.Tax.NPWP || "") : "";

                fields.push(self.newField(ba, label, "Tax", info));
            });
            resolve(true);
        }),

        // Get Identification
        new Promise(async (resolve) => {
            let identifications = [];
            let idTypes = [];
            let employeeIdsMap = {};

            await Promise.all([
                new Promise(async (resolve) => {
                    identifications = await self.get.identifications();
                    resolve(true);
                }),

                new Promise(async (resolve) => {
                    idTypes = await model.get.identificationType();
                    resolve(true);
                }),
            ]);

            identifications.forEach(identification => {
                let id = {};
                if (identification.UpdateRequest) {
                    id = Object.assign(_.cloneDeep(identification.Identification || {}), _.cloneDeep(identification.UpdateRequest || {}));
                } else {
                    id = Object.assign(_.cloneDeep(identification.UpdateRequest || {}), _.cloneDeep(identification.Identification || {}));
                }

                employeeIdsMap[id.Type] = id;
                employeeIdsMap[id.Type].NewData = (identification.UpdateRequest) ? identification.UpdateRequest.Number : ((identification.Identification) ? identification.Identification.Number : "");
                employeeIdsMap[id.Type].OldData = (identification.Identification) ? (identification.Identification.Number || "") : "";
            });


            idTypes.forEach(idType => {
                let data = Object.assign(_.clone(model.proto.Identification), idType, employeeIdsMap[idType.Type]);
                // Flag new field to be assigned 
                if (!employeeIdsMap[idType.Type] && data.AXID >= 0) {
                    data.AXID *= -1;
                }

                let label = idType.Description || idType.Type,
                    info = data.IssuingAggency;
                label = identificationLabelMap[label.toLowerCase()] || label;

                if (label == "NPWP") {
                    return;
                }

                fields.push(self.newField(data, label, "ID", info));
            });
            resolve(true);
        }),

        // Get Electronic Address
        new Promise(async (resolve) => {
            let addresses = [];
            let addressType = [];
            let mapCounterType = {};
            let mapTotalType = {};
            let addressFields = []

            await Promise.all([
                new Promise(async (resolve) => {
                    addresses = await self.get.electronicAddresses();
                    resolve(true);
                }),

                new Promise(async (resolve) => {
                    addressType = await self.get.electronicAddressType();
                    resolve(true);
                }),
            ]);

            addressType.forEach((type) => {
                mapCounterType[type.Type] = 0;
                mapTotalType[type.Type] = 0;
            });

            // Tally up .. tally up
            addresses.forEach((address) => {
                let ba = {};
                if (address.UpdateRequest) {
                    ba = Object.assign(_.cloneDeep(address.ElectronicAddress || {}), _.cloneDeep(address.UpdateRequest || {}));
                } else {
                    ba = Object.assign(_.cloneDeep(address.UpdateRequest || {}), _.cloneDeep(address.ElectronicAddress || {}));
                }

                if (mapTotalType[ba.Type]) {
                    mapTotalType[ba.Type]++;
                }
            });

            // Render existing data
            addresses.forEach((address, i) => {
                let ba = {};
                if (address.UpdateRequest) {
                    ba = Object.assign(_.cloneDeep(address.ElectronicAddress || {}), _.cloneDeep(address.UpdateRequest || {}));
                } else {
                    ba = Object.assign(_.cloneDeep(address.UpdateRequest || {}), _.cloneDeep(address.ElectronicAddress || {}));
                }

                if (approvedElectronicAddress.indexOf(parseInt(ba.Type)) < 0) {
                    return;
                }

                mapCounterType[ba.Type]++;
                let label = `${ba.TypeDescription} ${(mapTotalType[ba.Type] > 1) ? " #" + mapCounterType[ba.Type] : ""}`,
                    info = "";

                ba.NewData = (address.UpdateRequest) ? address.UpdateRequest.Locator : ((address.ElectronicAddress) ? address.ElectronicAddress.Locator : "");
                ba.OldData = (address.ElectronicAddress) ? (address.ElectronicAddress.Locator || "") : "";

                addressFields.push(self.newField(ba, label, "ElectronicAddress", info));
            });

            // Render the rest of types
            addressType.forEach((type) => {
                type.TypeDescription = type.Description;
                if (!mapCounterType[type.Type] && approvedElectronicAddress.indexOf(parseInt(type.Type)) > -1) {
                    let data = Object.assign(_.clone(model.proto.ElectronicAddress), type);

                    if (data.AXID >= 0) {
                        data.AXID *= -1;
                    }

                    let label = type.Description || type.Type,
                        info = "";
                    label = identificationLabelMap[type.Type.toLowerCase()] || label;
                    addressFields.push(self.newField(data, label, "ElectronicAddress", info));
                }

            });

            addressFields.sort((x, y) => {
                return (x.Label() > y.Label()) ? 1 : -1;
            });

            addressFields.forEach(d => {
                fields.push(d);
            });

            resolve(true);
        }),

        // Get Address
        new Promise(async (resolve) => {
            addressResult = await self.get.address();
            resolve(true);
        }),
    ]);

    // Render Address
    if (addressResult.Address || addressResult.UpdateRequest) {
        var data = addressResult.UpdateRequest || addressResult.Address;

        for (var i in data) {
            var d = data[i];
            if (self.data.employee().Address.hasOwnProperty(i) && typeof self.data.employee().Address[i] == "function") {
                self.data.employee().Address[i](d);
            }
        }

        let oldData = `${addressResult.Address.Street}___SEPARATOR___${addressResult.Address.City}`;
        self.data.employee().Address.OldData(oldData);

        setTimeout(() => {
            self.data.employee().Address.Street.subscribe(function (x) {
                let city = self.data.employee().Address.City();
                self.data.employee().Address.NewData(`${x}___SEPARATOR___${city}`);
            });

            self.data.employee().Address.City.subscribe(function (x) {
                let street = self.data.employee().Address.Street();
                self.data.employee().Address.NewData(`${street}___SEPARATOR___${x}`);
            });

            self.data.employee().Address.City.valueHasMutated();
        });
    }

    // Render Additional Fields
    fields.sort((x, y) => {
        return (x.Category() > y.Category()) ? 1 : -1;
    });
    model.data.employee().Fields(fields);

    // Calculate form divider
    let totalLeftFields = $("#resume #employeeResumeForm .form-group.row:not(.additional)").length;
    let totalAdditionalField = self.data.employee().Fields().length;
    let divider = Math.round((totalLeftFields + totalAdditionalField) / 2);

    self.data.employeeFieldsDivider().Left(Math.abs(totalLeftFields - divider));
    self.data.employeeFieldsDivider().Right(Math.abs(totalAdditionalField - Math.abs(totalLeftFields - divider + 1)));

    self.is.renderingEmployeeForm(false);
};

model.render.gridFamily = function () {
    let self = model;
    let $el = $("#gridFamily");

    let _marker = (data, fieldname, fn) => {
        if (typeof fn != "function") {
            fn = (x) => { return x };
        }

        let realValue = (data.Family) ? fn(data.Family[fieldname]) : undefined;
        let newValue = (data.UpdateRequest) ? fn(data.UpdateRequest[fieldname]) : null;

        if (data.UpdateRequest && data.Family) {
            if (data.UpdateRequest) {
                if (data.UpdateRequest.Action <= 1) {
                    if (realValue !== newValue)
                        return `<del class="text-muted">${realValue}</del> ${newValue}`
                } else {
                    return `<del class="text-danger">${realValue}</del>`
                }

            }
            return fn(data.Family[fieldname]);
        } else {
            var d = data.Family || data.UpdateRequest;
            return fn(d[fieldname]);
        }
    };

    if (!!$el) {
        let $grid = $el.getKendoGrid();

        if (!!$grid) {
            $grid.destroy();
        }

        $el.kendoGrid({
            dataSource: {
                transport: {
                    read: "/ESS/Employee/GetFamilies"
                },
                schema: {
                    data: function (res) {
                        if (res.StatusCode !== 200 && res.Status !== '') {
                            swalFatal("Fatal Error", `Error occured while fetching famili(es)\n${res.Message}`)
                            return []
                        }

                        return res.Data || [];
                    },
                    total: "Total",
                },
                error: function (e) {
                    swalFatal("Fatal Error", `Error occured while fetching famili(es)\n${e.xhr.responseText}`)
                }
            },
            pageable: {
                previousNext: false,
                info: false,
                numeric: false,
                refresh: true,

            },
            noRecords: {
                template: "No family data available."
            },
            columns: [
                {
                    attributes: {
                        "class": "text-center",
                    },
                    template: function (data) {
                        if (data.UpdateRequest) {
                            return `<button class="btn btn-xs btn-outline-warning" onclick="model.action.discardFamilyChange('${data.uid}'); return false;">
                                    <i class="fa mdi mdi-delete"></i>
                                </button>`;
                        }
                        return '';

                    },
                    hidden: true,
                    width: 50,
                },
                {
                    field: "NIK",
                    title: "NIK",
                    template: function (d) {
                        return _marker(d, "NIK");
                    },
                    width: 100,
                },
                {
                    field: "Name",
                    title: "Family Name",
                    template: function (d) {
                        return _marker(d, "Name");
                    },
                    width: 200,
                },
                {
                    field: "Gender",
                    title: "Gender",
                    template: function (d) {
                        return _marker(d, "Gender", (v) => {
                            return self.map.gender[v] || v
                        });
                    },
                    width: 75,
                },
                {
                    field: "Religion",
                    title: "Religion",
                    template: function (d) {
                        return _marker(d, "Religion", (v) => {
                            return self.map.religion[v] || v
                        });
                    },
                    width: 100,
                },
                {
                    field: "Relationship",
                    title: "Relationship",
                    template: function (d) {
                        return _marker(d, "Relationship", (v) => {
                            var relationship = self.map.familyRelationship[v];
                            return (!!relationship) ? relationship.Description : v;
                        });
                    },
                    width: 115,
                },
                {
                    field: "Birthdate",
                    title: "Birth Date",
                    template: function (d) {
                        return _marker(d, "Birthdate", standarizeDate);
                    },
                    width: 130,
                },
                {
                    field: "Birthplace",
                    title: "Birth Place",
                    template: function (d) {
                        return _marker(d, "Birthplace");
                    },
                    width: 130,
                },
                {
                    attributes: {
                        "class": "text-center",
                    },
                    template: function (data) {
                        var remove = ``
                        var edit = `
                                <button class="btn btn-xs btn-outline-info" onclick="model.action.editFamily('${data.uid}'); return false;">
                                    <i class="fa mdi mdi-pencil"></i>
                                </button>
                            `;
                        var remove = `
                                <button class="btn btn-xs btn-outline-danger" onclick="model.action.removeFamily('${data.uid}'); return false;">
                                    <i class="mdi mdi-close-box"></i>
                                </button>
                               `;

                        if (!!data.UpdateRequest) {
                            return "";
                        }

                        return edit + remove;

                    },
                    width: 75,
                },
                {
                    hidden: false,
                    attributes: {
                        "class": "text-center",
                    },
                    template: function (data) {
                        var disabled = false;
                        var document = data.Family,
                            documentUpdateRequest = data.UpdateRequest;

                        disabled = !((document && document.Filename && document.Accessible) || (documentUpdateRequest && documentUpdateRequest.Filename));

                        return `<button class="btn btn-xs ${disabled ? `btn-outline-dark` : `btn-outline-success`}" onclick="model.action.downloadFamilyDocument('${data.uid}'); return false;" ${disabled ? `disabled` : ``}>
                                    <i class="fa mdi mdi-download"></i>
                                </button>`;

                    },
                    width: 50,
                },
            ],
        });
    }
};

model.render.gridDocument = function () {
    let $el = $("#gridDocument");

    let _marker = (data, fieldname, fn) => {
        if (typeof fn != "function") {
            fn = (x) => { return x };
        }

        let realValue = (data.Document) ? fn(data.Document[fieldname]) : undefined;
        let newValue = (data.UpdateRequest) ? fn(data.UpdateRequest[fieldname]) : undefined;

        if (data.UpdateRequest && data.Document) {
            if (data.UpdateRequest) {
                if (data.UpdateRequest.Action <= 1) {
                    if (realValue && realValue !== newValue)
                        return `<del class="text-muted">${realValue}</del> ${newValue}`
                } else {
                    return `<del class="text-danger">${realValue}</del>`
                }

            }
            return fn(data.Document[fieldname]);
        } else {
            var d = data.Document || data.UpdateRequest;
            return fn(d[fieldname]);
        }
    };

    if (!!$el) {
        let $grid = $el.getKendoGrid();

        if (!!$grid) {
            $grid.destroy();
        }

        $el.kendoGrid({
            dataSource: {
                transport: {
                    read: "/ESS/Employee/GetDocuments"
                },
                schema: {
                    data: function (res) {
                        if (res.StatusCode !== 200 && res.Status !== '') {
                            swalFatal("Fatal Error", `Error occured while fetching document(s)\n${res.Message}`)
                            return []
                        }

                        return res.Data || [];
                    },
                    total: "Total",
                },
                error: function (e) {
                    swalFatal("Fatal Error", `Error occured while fetching document(s)\n${e.xhr.responseText}`)
                }
            },
            pageable: {
                previousNext: false,
                info: false,
                numeric: false,
                refresh: true
            },
            noRecords: {
                template: "No document data available."
            },
            columns: [
                {
                    attributes: {
                        "class": "text-center",
                    },
                    template: function (data) {
                        if (data.UpdateRequest) {
                            return `<button class="btn btn-xs btn-outline-warning" onclick="model.action.discardUploadDocument('${data.uid}'); return false;">
                                    <i class="fa mdi mdi-delete"></i>
                                </button>`;
                        }

                        return '';

                    },
                    width: 50,
                    hidden: model.app.config.readonly,
                },
                {
                    field: "DocumentType",
                    title: "Document Type",
                    template: function (d) {
                        return _marker(d, "DocumentType");
                    },
                    width: 100,

                },
                {
                    field: "Description",
                    title: "Description",
                    template: function (d) {
                        return _marker(d, "Description");
                    },
                    width: 200,
                },
                {
                    field: "UploadDate",
                    title: "Uploaded Date",
                    template: function (d) {
                        if (d.UpdateRequest) {
                            return standarizeDate(d.UpdateRequest.CreatedDate)
                        } else {
                            return standarizeDate(d.Document.CreatedDate)
                        }
                    },
                    width: 100,
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
                        let status = 0;
                        let statusClass = {
                            0: {
                                class: "badge badge-warning",
                                text: "Need Verification",
                            },
                            1: {
                                class: "badge badge-success",
                                text: "Verified",
                            },
                            2: {
                                class: "badge badge-danger",
                                text: "Rejected",
                            },
                        };


                        if (d.Document && !d.UpdateRequest) {
                            status = 1;
                        } else if (d.UpdateRequest) {
                            status = d.UpdateRequest.Status;
                        } else {
                            status = 0;
                        }

                        if (statusClass[status]) {
                            return `<span class="${statusClass[status].class}">${statusClass[status].text}</span>`;
                        }
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
                        var remove = ``
                        var edit = `
                                <button class="btn btn-xs btn-outline-info" onclick="model.action.editDocument('${data.uid}'); return false;">
                                    <i class="fa mdi mdi-pencil"></i>
                                </button>                           
                            `
                        if (data.Document) {
                            remove = `
                                        <button class="btn btn-xs btn-outline-danger" onclick="model.action.removeUploadDocument('${data.uid}'); return false;">
                                            <i class="mdi mdi-close-box"></i>
                                        </button>  
                                    `
                        }

                        if ((data.UpdateRequest && data.UpdateRequest.Action < 2) || !data.UpdateRequest) {
                            return edit + remove
                        }
                        return "";
                    },
                    width: 75,
                    hidden: model.app.config.readonly,
                },
                {
                    attributes: {
                        "class": "text-center",
                    },
                    template: function (data) {
                        var disabled = false;
                        var document = data.Document,
                            documentUpdateRequest = data.UpdateRequest;

                        disabled = !((document && document.Filename && document.Accessible) || (documentUpdateRequest && documentUpdateRequest.Filename));

                        return `<button class="btn btn-xs ${disabled ? `btn-outline-dark` : `btn-outline-success`}" onclick="model.action.downloadDocument('${data.uid}'); return false;" ${disabled ? `disabled` : ``}>
                                    <i class="fa mdi mdi-download"></i>
                                </button>`;
                        return '';

                    },
                    width: 50,
                },
            ]
        });
    }
};

model.render.gridDocumentRequest = function () {
  let $el = $("#gridDocumentRequest");
  if (!!$el) {
    let $grid = $el.getKendoGrid();

    if (!!$grid) {
      $grid.destroy();
    }

    $el.kendoGrid({
      dataSource: {
        transport: {
          read: "/ESS/Employee/GetDocumentRequests"
        },
        schema: {
          data: function (res) {
            console.log(res)
            if (res.StatusCode !== 200 && res.Status !== '') {
              swalFatal("Fatal Error", `Error occured while fetching document request(s)\n${res.Message}`)
              return []
            }

            return res.Data || [];
          },
          total: "Total",
        },
        error: function (e) {
          swalFatal("Fatal Error", `Error occured while fetching document request(s)\n${e.xhr.responseText}`)
        }
      },
      pageable: {
        previousNext: false,
        info: false,
        numeric: false,
        refresh: true
      },
      noRecords: {
        template: "No document request data available."
      },
      columns: [
        {
          field: "Id",
          title: "Request ID",
          width: 150,
        },
        {
          field: "CreatedDate",
          title: "Created Date",
          template: (d) => {
            return standarizeDate(d.CreatedDate);
          },
          width: 125,
        },
        {
          field: "EmployeeID",
          title: "Created By",
          width: 125,
        },
        {
          field: "DocumentType",
          title: "Doc. Type",
          width: 100,
        },

        {
          field: "Description",
          title: "Description",
          width: 170,
        },
        //{
        //    headerAttributes: {
        //        "class": "text-center",
        //    },
        //    attributes: {
        //        "class": "text-center",
        //    },
        //    field: "ValidDate.Finish",
        //    title: "Valid Until",
        //    width: 125,
        //    template: (d) => {
        //        return standarizeDate((d.ValidDate) ? d.ValidDate.Finish : null);
        //    },
        //},
        {
          hidden: false,
          headerAttributes: {
            "class": "text-center",
          },
          attributes: {
            "class": "text-center",
          },
          title: "Status",
          template: function (data) {
            let status = data.Status
            let nstatus = 0;
            if (data.Attachment.Filename == null) {
              nstatus = 1;
            } else {
              nstatus = 2;
            }
            let statusClass = {
              0: {
                class: "badge badge-warning",
                text: "Waiting for Approval",
              },
              1: {
                class: "badge badge-success",
                text: "Open",
              },
              2: {
                class: "badge badge-success",
                text: "Completed",
              },
            };
            return `<span class="${(statusClass[nstatus].class)}">${statusClass[nstatus].text}</span>`;
          },
          width: 150,
        },
        {
          headerAttributes: {
            "class": "text-center",
          },
          attributes: {
            "class": "text-center",
          },
          template: function (data) {
            return `
                                <button class="btn btn-xs btn-outline-info" onclick="model.action.editDocumentRequest('${data.uid}'); return false;">
                                    <i class="fa mdi mdi-pencil"></i>
                                </button>
                            `
          },
          width: 75,
          //hidden: model.app.config.readonly,
          hidden: false
        },
        {
          attributes: {
            "class": "text-center",
          },
          template: function (data) {
            //var disabled = (data.Status == 1 && !!data.ValidDate) ? '' : 'disabled';
            var disabled = '';
            return `<button class="btn btn-xs ${(disabled) ? 'btn-outline-dark' : 'btn-outline-success'}" onclick="model.action.download('${data.uid}');" ${disabled}>
                                    <i class="fa mdi mdi-download"></i>
                                </button>`;

            return '';

          },
          width: 50,
          //hidden: model.app.config.readonly,
          hidden: true
        },
      ]
    });
  }
};

model.render.gridApplication = function () {
    let $el = $("#gridApplication");
    if (!!$el) {
        let $grid = $el.getKendoGrid();

        if (!!$grid) {
            $grid.destroy();
        }

        $el.kendoGrid({
            dataSource: {
                transport: {
                    read: "/ESS/Employee/GetApplicants"
                },
                schema: {
                    data: function (res) {
                        if (res.StatusCode !== 200 && res.Status !== '') {
                            swalFatal("Fatal Error", `Error occured while fetching applicant(s)\n${res.Message}`)
                            return []
                        }

                        return res.Data || [];
                    },
                    total: "Total",
                },
                error: function (e) {
                    swalFatal("Fatal Error", `Error occured while fetching applicant(s)\n${e.xhr.responseText}`)
                }
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
            noRecords: {
                template: "No applicants data available."
            },
            columns: [
                {
                    field: "RecruitmentID",
                    title: "Recruitment ID",
                    width: 150,
                },
                {
                    field: "ApplicationID",
                    title: "Application ID",
                    width: 150,
                },
                //{
                //    field: "Name",
                //    title: "Employee Name",
                //},
                {
                    field: "JobID",
                    title: "Job",
                },
                {
                    field: "ScheduleDate",
                    title: "Scheduled Date",
                    template: (e) => {
                        var start = standarizeDate(e.Schedule.Start);
                        var finish = standarizeDate(e.Schedule.Finish);
                        return (start == finish) ? start : `${start} - ${finish}`;
                    },
                    width: 225,
                },
                //{
                //    field: "RecruitmentCycle",
                //    title: "Recruitment Cycle",
                //},
                //{
                //    field: "Venue",
                //    title: "Venue ",
                //},
                {
                    field: "StatusDescription",
                    title: "Status",
                    width: 150,
                    //template: function (data) {
                    //    let status = (data.Status || "").toLowerCase();
                    //    let statusClass = {
                    //        '': "text-warning",
                    //        '': "text-success",
                    //        '': "text-danger",
                    //        '': "text-primary",
                    //    };
                    //    return `<span class="${(statusClass[status] || "text-muted")}">${data.Status}</span>`;
                    //},
                },
            ]
        });
    }
};

model.render.gridCertificate = function () {
    let $el = $("#gridCertificate");

    let _marker = (data, fieldname, fn) => {
        if (typeof fn != "function") {
            fn = (x) => { return x };
        }
        let realValue = (data.Certificate) ? fn(data.Certificate[fieldname]) : undefined;
        let newValue = (data.UpdateRequest) ? fn(data.UpdateRequest[fieldname]) : undefined;

        if (data.UpdateRequest && data.Certificate) {
            if (data.UpdateRequest) {
                if (data.UpdateRequest.Action <= 1) {
                    if (realValue && realValue !== newValue)
                        return `<del class="text-muted">${realValue}</del> ${newValue}`
                } else {
                    return `<del class="text-danger">${realValue}</del>`
                }

            }
            return fn(data.Certificate[fieldname]);
        } else {
            var d = data.Certificate || data.UpdateRequest;
            return fn(d[fieldname]);
        }
    };

    if (!!$el) {
        let $grid = $el.getKendoGrid();

        if (!!$grid) {
            $grid.destroy();
        }

        $el.kendoGrid({
            dataSource: {
                transport: {
                    read: "/ESS/Employee/GetCertificates"
                },
                schema: {
                    data: function (res) {
                        if (res.StatusCode !== 200 && res.Status !== '') {
                            swalFatal("Fatal Error", `Error occured while fetching certificate(s)\n${res.Message}`)
                            return []
                        }

                        return res.Data || [];
                    },
                    total: "Total",
                },
                error: function (e) {
                    swalFatal("Fatal Error", `Error occured while fetching certificate(s)\n${e.xhr.responseText}`)
                }
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
            noRecords: {
                template: "No certificate data available."
            },
            columns: [
                {
                    attributes: {
                        "class": "text-center",
                    },
                    template: function (data) {
                        if (data.UpdateRequest) {
                            return `<button class="btn btn-xs btn-outline-warning" onclick="model.action.discardCertificate('${data.uid}'); return false;">
                                    <i class="fa mdi mdi-delete"></i>
                                </button>`;
                        }

                        return '';

                    },
                    width: 50,
                    hidden: true,
                },
                {
                    field: "TypeDescription",
                    title: "Certificate Type",
                    template: function (data) {
                        return _marker(data, "TypeDescription");
                    },
                    width: 150,
                },
                {
                    field: "Note",
                    title: "Note",
                    template: function (d) {
                        return _marker(d, "Note");
                    },
                    width: 150,
                },
                {
                    //field: "Validity",
                    title: "Start Date",
                    template: function (d) {
                        let _standarizeDateStart = function (d) {
                            return (d) ? standarizeDate(d.Start) : "";
                        };
                        return _marker(d, "Validity", _standarizeDateStart);
                    },
                    width: 150,
                },

                {
                    //field: "Validity",
                    title: "End Date",
                    template: function (d) {
                        let _standarizeDateFinish = function (d) {
                            return (d) ? standarizeDate(d.Finish) : "";
                        };
                        return _marker(d, "Validity", _standarizeDateFinish);
                    },
                    width: 150,
                },
                {
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
                    field: "ReqRenew",
                    title: "Request Renewal",
                    template: function (data) {
                        return _marker(data, "ReqRenew", function (d) {
                            return (d) ? 'Yes' : 'No';
                        });
                    },
                    width: 150,
                },
                {
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
                    template: function (data) {
                        var edit = `
                                <button class="btn btn-xs btn-outline-info" onclick="model.action.editCertificate('${data.uid}'); return false;">
                                    <i class="fa mdi mdi-pencil"></i>
                                </button>                           
                            `;

                        var remove = `
                                        <button class="btn btn-xs btn-outline-danger" onclick="model.action.removeCertificate('${data.uid}'); return false;">
                                            <i class="mdi mdi-close-box"></i>
                                        </button>  
                                    `;

                        if (!!data.UpdateRequest) {
                            return "";
                        }

                        return edit + remove;
                    },
                    width: 75,
                },
                {
                    attributes: {
                        "class": "text-center",
                    },
                    template: function (data) {
                        var disabled = false;
                        var document = data.Document,
                            updateRequest = data.UpdateRequest;

                        disabled = !((document && document.Filename && document.Accessible) || (updateRequest && updateRequest.Filename));

                        return `<button class="btn btn-xs ${disabled ? `btn-outline-dark` : `btn-outline-success`}" onclick="model.action.downloadCertificate('${data.uid}'); return false;" ${disabled ? `disabled` : ``}>
                                    <i class="fa mdi mdi-download"></i>
                                </button>`;

                    },
                    width: 50,
                },
            ]
        });
    }
};

model.render.gridEmployment = function () {
    let $el = $("#gridEmployment");
    if (!!$el) {
        let $grid = $el.getKendoGrid();

        if (!!$grid) {
            $grid.destroy();
        }

        $el.kendoGrid({
            dataSource: {
                transport: {
                    read: "/ESS/Employee/GetEmployments"
                },
                schema: {
                    data: function (res) {
                        if (res.StatusCode !== 200 && res.Status !== '') {
                            swalFatal("Fatal Error", `Error occured while fetching employment(s)\n${res.Message}`)
                            return []
                        }

                        return res.Data || [];
                    },
                    errors: function (response) {
                        console.log("errors as function", response)
                        return response.errors;
                    },
                    total: "Total",
                },
                error: function (e) {
                    swalFatal("Fatal Error", `Error occured while fetching employment(s)\n${e.xhr.responseText}`)
                }
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
            noRecords: {
                template: "No employment data available."
            },
            columns: [
                {
                    hidden: true,
                    field: "Position",
                    title: "Position",
                    width: 150,
                },
                {
                    field: "Description",
                    title: "Description",
                    width: 150,
                },
                {
                    field: "AssigmentDate",
                    title: "Assignment Start",
                    template: function (data) {
                        return standarizeDate(data.AssigmentDate.Start);
                    },
                    width: 150,
                },
                {
                    field: "AssigmentDate",
                    title: "Assignment End",
                    template: function (data) {
                        return standarizeDate(data.AssigmentDate.Finish);
                    },
                    width: 150,
                },
                {
                    field: "PrimaryPosition",
                    title: "Primary Position",
                    template: function (data) {
                        return data.PrimaryPosition ? "Yes" : "No";
                    },
                    width: 150,
                },
            ]
        });
    }
};

model.render.gridChildren = function () {
    let $el = $("#gridChildren");
    if (!!$el) {
        let $grid = $el.getKendoGrid();

        if (!!$grid) {
            $grid.destroy();
        }

        $el.kendoGrid({
            dataSource: {
                data: [],
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
            noRecords: {
                template: "No children data available."
            },
            columns: [
                {
                    field: "Name",
                    title: "Child Name",
                },
                {
                    field: "NIK",
                    title: "Child NIK",
                },
                {
                    field: "Birthplace",
                    title: "Birth Place",
                },
                {
                    field: "Birthdate",
                    title: "Birth Date",
                },
                {
                    field: "Sex",
                    title: "Sex",
                },
                {
                    field: "Job",
                    title: "Job",
                },
            ]
        });
    }
};

model.render.gridInstallment = function () {
    let $el = $("#gridInstallment");
    if (!!$el) {
        let $grid = $el.getKendoGrid();

        if (!!$grid) {
            $grid.destroy();
        }

        $el.kendoGrid({
            dataSource: {
                transport: {
                    read: "/ESS/Employee/GetInstallments"
                },
                schema: {
                    data: function (res) {
                        if (res.StatusCode !== 200 && res.Status !== '') {
                            swalFatal("Fatal Error", `Error occured while fetching installment(s)\n${res.Message}`)
                            return []
                        }

                        return res.Data || [];
                    },
                    total: "Total",
                },
                error: function (e) {
                    swalFatal("Fatal Error", `Error occured while fetching installment(s)\n${e.xhr.responseText}`)
                }
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
            noRecords: {
                template: "No employee installment data available."
            },
            columns: [
                {
                    field: "LoanID",
                    title: "Loan ID",
                },
                {
                    field: "Description",
                    title: "Description",
                },
                {
                    field: "Amount",
                    title: "Amount",
                },
                {
                    field: "Instalment",
                    title: "Installment",
                },
                {
                    field: "LoanSchedule",
                    title: "Start",
                },
                {
                    field: "LoanSchedule",
                    title: "End",
                },
                {
                    field: "Balance",
                    title: "Balance",
                },
            ]
        });
    }
};

model.render.gridMedicalRecord = function () {
    let $el = $("#gridMedicalRecord");
    if (!!$el) {
        let $grid = $el.getKendoGrid();

        if (!!$grid) {
            $grid.destroy();
        }

        $el.kendoGrid({
            dataSource: {
                transport: {
                    read: "/ESS/Employee/GetMedicalRecords"
                },
                schema: {
                    data: function (res) {
                        if (res.StatusCode !== 200 && res.Status !== '') {
                            swalFatal("Fatal Error", `Error occured while fetching medical record(s)\n${res.Message}`)
                            return []
                        }

                        return res.Data || [];
                    },
                    total: "Total",
                },
                error: function (e) {
                    swalFatal("Fatal Error", `Error occured while fetching medical record(s)\n${e.xhr.responseText}`)
                }
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
            noRecords: {
                template: "No applicants data available."
            },
            columns: [
                {
                    field: "RecordDate",
                    title: "Date",
                    template: function (d) {
                        return standarizeDateTime(d.RecordDate)
                    }
                },
                {
                    field: "Description",
                    title: "Description",
                    template: function (d) {
                        let result = d.Description;
                        if (d.Notes) {
                            result += `<br/><span class="text-muted">${d.Notes}</span>`
                        }
                        return result;
                    }
                },
                //{
                //    attributes: {
                //        "class": "text-center",
                //    },
                //    template: function (data) {
                //        var disabled = false;

                //        disabled = !(data && data.Filename && data.Accessible);

                //        return `<button class="btn btn-xs ${disabled ? `btn-outline-dark` : `btn-outline-success`}" onclick="model.action.downloadMedicalRecord('${data.uid}'); return false;" ${disabled ? `disabled` : ``}>
                //                    <i class="fa mdi mdi-download"></i>
                //                </button>`;
                //        return '';

                //    },
                //    width: 50,
                //},
            ],
            detailTemplate: function (d) {
                if (d.Documents.length > 0) {
                    let list = [];
                    d.Documents.forEach(document => {
                        let downloader = `<button class="btn btn-xs btn-secondary" disabled><i class="mdi mdi-file-document"></i>${document.Filename}</button>`;
                        if (document.Accessible) {
                            downloader = `
                                <input type="hidden" value="${encodeURIComponent(document.Filehash)}" name="token" />
                                <button class="btn btn-xs btn-info" type="submit" class="btn btn-sm mt-1" title="Download ${document.Filename}">
                                    <i class="mdi mdi-file-document"></i> ${document.Filename}
                                </button>
                            `
                        }

                        list.push(`<li>
                            <form target="_blank" name="download" action="/ESS/Employee/DownloadMedicalRecord" method="post">
                                ${downloader}                                
                            </form>
                            
                        </li>`);
                    });
                    return `<ol class="attachments">${list.join("")}</ol>`;
                }
                return `<span class="text-muted">No medical records attached</span>`;
            }
        });
    }
};

model.render.gridWarningLetter = function () {
    let $el = $("#gridWarningLetter");
    if (!!$el) {
        let $grid = $el.getKendoGrid();

        if (!!$grid) {
            $grid.destroy();
        }

        $el.kendoGrid({
            dataSource: {
                transport: {
                    read: "/ESS/Employee/GetWarningLetters"
                },
                schema: {
                    data: function (res) {
                        if (res.StatusCode !== 200 && res.Status !== '') {
                            swalFatal("Fatal Error", `Error occured while fetching warning letter(s)\n${res.Message}`)
                            return []
                        }

                        return res.Data || [];
                    },
                    total: "Total",
                },
                error: function (e) {
                    swalFatal("Fatal Error", `Error occured while fetching warning letter(s)\n${e.xhr.responseText}`)
                }
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
            noRecords: {
                template: "No applicants data available."
            },
            columns: [
                {
                    field: "Worker",
                    title: "Letter",
                    width: 200,
                },
                {
                    field: "CodeSP",
                    title: "Code",
                    width: 75,
                },
                {
                    field: "Description",
                    title: "Description",

                },
                {
                    field: "Schedule",
                    title: "Date Start",
                    template: function (e) {
                        return standarizeDate(e.Schedule.Start)
                    },
                    width: 150,
                },
                {
                    field: "Schedule",
                    title: "Date End",
                    template: function (e) {
                        return standarizeDate(e.Schedule.Finish)
                    },
                    width: 150,

                },
                {
                    attributes: {
                        "class": "text-center",
                    },
                    template: function (data) {
                        var disabled = false;

                        disabled = !(data && data.Filename && data.Accessible);

                        return `<button class="btn btn-xs ${disabled ? `btn-outline-dark` : `btn-outline-success`}" onclick="model.action.downloadWarningLetter('${data.uid}'); return false;" ${disabled ? `disabled` : ``}>
                                    <i class="fa mdi mdi-download"></i>
                                </button>`;
                        return '';

                    },
                    width: 50,
                },
            ]
        });
    }
};

model.render.dropdownRelationship = function () {
    let $el = $("#dropdownRelationship");
    if (!!$el) {
        let $grid = $el.getKendoGrid();

        if (!!$grid) {
            $grid.destroy();
        }
        $el.kendoDropDownList({
            dataSource: {
                transport: {
                    read: {
                        dataType: "json",
                        url: URL_GET_FAMILY_RELATIONSHIP,
                    }
                },
                schema: {
                    data: (d) => { return d.Data || [] },
                }
            }
        });
    }
};

model.render.dropdownType = function () {
    let $el = $("#dropdownType");
    if (!!$el) {
        let $grid = $el.getKendoGrid();

        if (!!$grid) {
            $grid.destroy();
        }
        $el.kendoDropDownList({
            dataSource: {
                transport: {
                    read: {
                        dataType: "json",
                        url: URL_GET_DOCUMENT_TYPE,
                    }
                },
                schema: {
                    data: (d) => { return d.Data || [] },
                }
            }
        });
    }
};

var $test = new kendo.data.DataSource({
    serverFiltering: true,
    transport: {
        read: {
            url: "/ESS/Employee/GetCity",
            dataType: "json"
        }
    },
    schema: {
        data: function (res) {
            console.log(res);
            if (res.StatusCode !== 200 && res.Status !== '') {
                swalFatal("Fatal Error", `Error occured while fetching cities\n${res.Message}`)
                return []
            }

            return res.Data || [];
        },
    }
});

model.data.preProcessResume = function (data) {
    let param = ko.mapping.toJS(data);
    param.Fields.forEach(f => {
        switch (f.Category) {
            case "BankAccount":
                f.AccountNumber = f.NewData;
                param.BankAccounts.push(f);
                break;
            case "Tax":
                f.NPWP = f.NewData;
                param.BankAccounts.push(f);
                break;
            case "ElectronicAddress":
                f.Locator = f.NewData;
                param.ElectronicAddresses.push(f);
                break;
            case "ID":
                f.Number = f.NewData;
                param.Identifications.push(f);
                break;
            default:
                console.warn(`unable to find category "${f.Category}"\n${ko.mapping.toJS(f)}`)
                break;
        }
    });
    param.Fields = [];
    return param
};

model.data.validateResume = function (data) {
    let param = ko.mapping.toJS(data);
    let result = param.Fields.filter(f => {
        return !f.Filename && f.OldData != f.NewData && f.Category != "ElectronicAddress";
    });

    console.log(result);
    result = result || [];

    if (!param.Address.Filename && param.Address.OldData != param.Address.NewData) {
        result.push(param.Address);
    }

    return result;
};

//Actions
model.action.saveAsDraft = async function (noConfirmation = false) {
    let dialogTitle = "Employee";
    let self = model;
    let param = model.data.preProcessResume(model.data.employee());
    let confirmResult = {
        value: true,
    };

    if (!noConfirmation) {
        confirmResult = await swalConfirm(dialogTitle, "Are you sure to save your data as draft?");
    }

    if (confirmResult.value) {
        try {
            if (!noConfirmation) isLoading(true);
            let response = await ajax("/ESS/Employee/Update", "POST", ko.mapping.toJSON(param));
            if (!noConfirmation) isLoading(false);
            if (response.StatusCode == 200) {
                if (!noConfirmation) {
                    swalSuccess(dialogTitle, response.Message);
                    await self.render.formEmployee();
                }

                return true;
            }
            swalError(dialogTitle, response.Message);
        } catch (e) {
            if (!noConfirmation) isLoading(false);
        }
    }
    return false;
};

model.action.requestUpdate = async function () {
    let self = model;
    let dialogTitle = "Employee";

    let validation = model.data.validateResume(model.data.employee());
    if (validation.length > 0) {
        swalAlert(dialogTitle, `There are <b>${validation.length} changed field(s)</b> that has no attachment`);
        return;
    }

    let confirmResult = await swalConfirmText(
        dialogTitle,
        `Are you sure updating your data ?<br/>Please specify the reason of your update request below`,
        `Data updation reason`
    );

    if (confirmResult.hasOwnProperty("value")) {
        let reason = confirmResult.value;
        if (!reason) {
            swalAlert(dialogTitle, "Employe update request reason could not be empty");
            return;
        }
        isLoading(true);
        var result = await self.action.saveAsDraft(true);
        if (result) {
            try {
                // Saving employee update request
                // Committing to AX
                let param = ko.toJS(model.data.updateRequest());
                param.Notes = reason;
                response = await ajax("/ESS/Employee/UpdateRequestResume", "POST", ko.toJSON(param));
                self.action.refreshRequestStatus();
                await self.render.formEmployee();
                isLoading(false);

                if (response.StatusCode == 200) {
                    swalSuccess(dialogTitle, response.Message);
                    return;
                }
                swalError(dialogTitle, response.Message);
            } catch (e) {
                isLoading(false);
                swalError(dialogTitle, e);
            }
        }

    }
};

model.action.discardUpdate = async function () {
    var dialogTitle = "Change Request";
    var self = model;
    //dataGrid = $("#gridFamily").data("kendoGrid").dataSource.getByUid(uid);

    var result = await swalConfirm(dialogTitle, `Are you sure discarding your data ?`);
    if (result.value) {
        try {
            isLoading(true)
            ajaxPost("/ess/employee/discard", {}, async function (data) {
                isLoading(false)
                if (data.StatusCode == 200) {
                    swalSuccess(dialogTitle, data.Message);

                    isLoading(true);
                    await self.render.formEmployee();
                    isLoading(false);
                } else {
                    swalError(dialogTitle, data.Message);
                }
            }, function (data) {
                swalError(dialogTitle, data.Message);
            });
        } catch (e) {
            isLoading(false)
        }
    }
};

model.on.uploadEmployeeAttachment = function (e) {
    var self = model;
    var data = ko.mapping.toJS(self.data.employee());
    var field = e.sender.element.data("field");
    data.IsExpartriateAttachment.NewData = data.IsExpatriate;
    data.MaritalStatusAttachment.NewData = data.MaritalStatus;
    console.log(">>>", data);
    if (e.files.length > 0) {
        e.formData = new FormData();
        e.formData.append("Field", field);
        e.formData.append("JsonData", ko.mapping.toJSON(data));
        e.formData.append("FileUpload", e.files[0].rawFile);
    }
};

model.on.uploadEmployeeAttachmentSelected = function (e) {
    var self = model;
    var field = e.sender.element.data("field");

    var valid = self.app.data.validateKendoUpload(e, "Employee");
    if (valid) {

        var formGroup = $(`#${field}Field`);
        if (formGroup.length > 0) {
            formGroup.addClass("loading");
        }
    } else {
        e.preventDefault();
    }

};

model.on.uploadEmployeeAttachmentProgress = function (e) {
    var self = model;
    var field = e.sender.element.data("field");

    var formGroup = $(`#${field}Field`);
    if (formGroup.length > 0) {
        formGroup.addClass("loading");
    }
};

model.on.uploadEmployeeAttachmentSuccess = function (e) {
    var self = model;
    var field = e.sender.element.data("field");

    var formGroup = $(`#${field}Field`);
    if (formGroup.length > 0) {
        formGroup.removeClass("error");
        formGroup.removeClass("loading");
    }

    if (e.response.StatusCode == 200) {
        switch (field) {
            case "IsExpartiarte":
                self.data.employee().IsExpartriateAttachment(ko.mapping.fromJS(e.response.Data.IsExpartriateAttachment));
                break;
            case "MaritalStatus":
                self.data.employee().MaritalStatusAttachment(ko.mapping.fromJS(e.response.Data.MaritalStatusAttachment));
                break;
            default:
                break;
        }
    } else {
        swalFatal("Employee", `Error occured while uploading "Expartriarte" attachment:\n${JSON.stringify(e.response.Message)}`);
    }
};

model.on.uploadEmployeeAttachmentError = function (e) {
    var self = model;
    var field = e.sender.element.data("field");

    var formGroup = $(`#${field}Field`);
    if (formGroup.length > 0) {
        formGroup.addClass("error");
        formGroup.removeClass("loading");
    }

    this.clearAllFiles();
    swalFatal("Employee", `Error occured while uploading "Expartriarte" attachment:\n${JSON.stringify(e.XMLHttpRequest)}`);
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

model.action.attachExpartriarteFile = function (data) {
    var d = ko.mapping.toJS(data);
    var uploader = $(`#expartriarteUploader`).getKendoUpload();
    if (uploader) {
        uploader.element.click();
        return;
    }
    console.warn("unable to find field attachment uploader :\n", d);
};

model.action.attachMaritalStatusFile = function (data) {
    var d = ko.mapping.toJS(data);
    var uploader = $(`#maritalStatusUploader`).getKendoUpload();
    if (uploader) {
        uploader.element.click();
        return;
    }
    console.warn("unable to find field attachment uploader :\n", d);
};

model.action.attachAddressFile = function (data) {
    var d = ko.mapping.toJS(data);
    var uploader = $(`#addressUploader`).getKendoUpload();
    if (uploader) {
        uploader.element.click();
        return;
    }
    console.warn("unable to find field attachment uploader :\n", d);
};

model.action.attachFieldFile = function (data) {
    var d = ko.mapping.toJS(data);
    var uploader = $(`#Uploader${d.UID}`).getKendoUpload();
    if (uploader) {
        uploader.element.click();
        return;
    }
    console.warn("unable to find field attachment uploader :\n", d);
};

model.action.openFormFamily = function () {
    model.data.family(model.newFamily());
    $("#modalFormFamily").modal("show");
};

model.action.openFormCertificate = function () {
    model.data.certificate(model.newCertificate());
    $("#modalFormCertificate").modal("show");
};

model.action.refreshRequestStatus = async function () {
    var self = model;
    var response = await self.get.isRequestActive();
    var isRequestActive = false;
    if (response && response.Status == 0) {
        isRequestActive = true;
    }
    self.is.requestActive(isRequestActive);
};

model.action.refreshGridCertificate = function (uiOnly = false) {
    var $grid = $("#gridCertificate").data("kendoGrid");
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

model.action.refreshGridFamily = function (uiOnly = false) {
    var $grid = $("#gridFamily").data("kendoGrid");
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

model.action.refreshGridDocument = function (uiOnly = false) {
    var $grid = $("#gridDocument").data("kendoGrid");
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

model.action.refreshGridDocumentRequest = function (uiOnly = false) {
    var $grid = $("#gridDocumentRequest").data("kendoGrid");
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

model.action.refreshGridCertificate = function (uiOnly = false) {
    var $grid = $("#gridCertificate").data("kendoGrid");
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

model.action.saveFamily = async function () {
    var data = ko.mapping.toJS(model.data.family);
    var fileUpload = $("#familyDocumentVerification").getKendoUpload();
    var files;

    if (fileUpload) {
        files = fileUpload.getFiles();
    }

    var dialogTitle = "Family";

    if (!(data.FirstName || "").trim()) {
        swalAlert(dialogTitle, 'First name is required.');
        return;
    }

    if (!(data.LastName || "").trim()) {
        swalAlert(dialogTitle, 'Last name is required.');
        return;
    }

    if (data.Gender == "") {
        swalAlert(dialogTitle, 'Gender is required.');
        return;
    }
    if (data.Relationship == "") {
        swalAlert(dialogTitle, 'Relationship is required.');
        return;
    }
    //if (!(data.Birthdate || "").trim()) {
    //    swalAlert(dialogTitle, 'Birthdate is required.');
    //    return;
    //}
    if (!(data.Birthplace || "").trim()) {
        swalAlert(dialogTitle, 'Birth place is required.');
        return;
    }

    if (!(data.NIK || "").trim()) {
        swalAlert(dialogTitle, 'NIK is required.');
        return;
    }

    if ((!files || (!!files && files.length == 0)) && !data.Filename) {
        swalAlert(dialogTitle, 'Document attachment is required.');
        return;
    }

    let confirmResult = await swalConfirmText(
        dialogTitle,
        `Are you sure saving your data ?<br/>Please specify the reason of your update request below`,
        `Reason`
    );
    console.log('b', data);
    console.log('data', JSON.stringify(data))
    data.Birthdate = new Date(moment(data.Birthdate).format("YYYY-MM-DD"));

    console.log('json', ko.mapping.toJSON(data));
    if (confirmResult.hasOwnProperty("value")) {
        let reason = confirmResult.value;
        if (!reason) {
            swalAlert(dialogTitle, "Family update request reason could not be empty");
            return;
        }

        try {
            // Encapsulate Upload Data
            var formData = new FormData();
            formData.append("Reason", reason);
            formData.append("JsonData", ko.mapping.toJSON(data));
            if (fileUpload) {
                let file = fileUpload.getFiles()[0];
                if (file) {
                    formData.append("FileUpload", file.rawFile);
                }
            }

            let $modal = $("#modalFormFamily");

            isLoading(true);
            $modal.modal("hide");
            ajaxPostUpload("/ESS/Employee/SaveFamily", formData, function (data) {
                isLoading(false)
                if (data.StatusCode == 200) {
                    model.action.refreshGridFamily();
                    swalSuccess(dialogTitle, data.Message);
                } else {
                    $modal.modal("show");
                    swalError(dialogTitle, data.Message);
                }
            }, function (data) {
                $modal.modal("show");
                swalError(dialogTitle, data.Message);
            });
        } catch (e) {
            isLoading(false);
        }
    }
};

let familyReadonlyFile;
model.action.editFamily = function (uid) {
    dataGrid = $("#gridFamily").data("kendoGrid").dataSource.getByUid(uid);
    model.data.family(model.newFamily());
    if (dataGrid.UpdateRequest) {
        model.data.family(model.newFamily(dataGrid.UpdateRequest));

        if (!familyReadonlyFile)
            familyReadonlyFile = kendo.template($("#fileTemplateReadonly").html());

        if (dataGrid.UpdateRequest.Filename)
            model.data.family().HTMLFile(familyReadonlyFile(dataGrid.UpdateRequest));
        else
            model.data.family().HTMLFile("");
    } else {
        model.data.family(model.newFamily(dataGrid.Family));
        model.data.family().Old(dataGrid.Family);
    }

    $("#modalFormFamily").modal("show");
}

model.action.discardFamilyChange = async function (uid) {
    var dialogTitle = "Family";
    dataGrid = $("#gridFamily").data("kendoGrid").dataSource.getByUid(uid);

    var result = await swalConfirm(dialogTitle, `Are you sure discarding "${dataGrid.UpdateRequest.Name || dataGrid.Family.Name}" ?`);
    if (result.value) {
        var Id = (dataGrid.UpdateRequest) ? dataGrid.UpdateRequest.Id : "";

        if (Id) {
            isLoading(true)
            ajaxPost("/ess/employee/discardfamilychange/" + Id, {}, function (data) {
                isLoading(false)
                if (data.StatusCode == 200) {
                    model.action.refreshGridFamily();
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

model.action.removeFamily = async function (uid) {
    var dialogTitle = "Family";
    var dataGrid = $("#gridFamily").data("kendoGrid").dataSource.getByUid(uid);
    var family = dataGrid.Family || dataGrid.UpdateRequest;
    var result = await swalConfirmText(dialogTitle, `Are you sure deleting "${family.Name}" ?<br/>Please specify the reason of your update request below`, "Reason");

    if (result.hasOwnProperty("value")) {
        let reason = result.value;
        if (!reason) {
            swalAlert(dialogTitle, "Family update request reason could not be empty");
            return;
        }

        var Id = (dataGrid.Family) ? dataGrid.Family.AXID : "";

        try {
            isLoading(true)
            ajaxPost("/ess/employee/removefamily/" + Id, { Id: Id, Reason: reason }, function (data) {
                isLoading(false)
                if (data.StatusCode == 200) {
                    model.action.refreshGridFamily();
                    swalSuccess(dialogTitle, data.Message);
                } else {
                    swalError(dialogTitle, data.Message);
                }
            }, function (data) {
                swalError(dialogTitle, data.Message);
                isLoading(false);
            });
        } catch (e) {
            isLoading(false);
        }

    }
}

model.action.openDocumentRequest = function () {
    model.data.documentRequest(model.newDocumentRequest());
    $("#modalFormDocumentRequest").modal("show");
};

model.action.saveDocumentRequest = async function () {
  var dialogTitle = "Document Request";
  var result = await swalConfirm(dialogTitle, 'Are you sure to request document ?');

  if (result.value) {
    try {
      isLoading(true);
      var data = ko.mapping.toJS(model.data.documentRequest);
      var files;
      var $uploader = $("#fileDocumentRequest").getKendoUpload();
      if ($uploader) files = $uploader.getFiles();

      if (!(data.DocumentType || "").trim()) {
        isLoading(false);
        swalAlert(dialogTitle, 'Document type is required.');
        return;
      }

      if (!(data.Description || "").trim()) {
        isLoading(false);
        swalAlert(dialogTitle, 'Description is required.');
        return;
      }



      let formData = new FormData();
      formData.append("JsonData", ko.mapping.toJSON(data));
      if (files[0]) {
        formData.append("FileUpload", files[0].rawFile);
      }

      let $modal = $("#modalFormDocumentRequest");

      $modal.modal("hide");
      ajaxPostUpload("/ESS/Employee/SaveDocumentRequest", formData, function (data) {
        isLoading(false)
        if (data.StatusCode == 200) {
          model.action.refreshGridDocumentRequest();
          swalSuccess(dialogTitle, data.Message);
        } else {
          $modal.modal("show");
          swalError(dialogTitle, data.Message);
        }
      }, function (data) {
        $modal.modal("show");
        swalError(dialogTitle, data.Message);
      });
    } catch (e) {
      console.error(e);
      isLoading(false);
    }
  }
};

let documentRequestReadOnlyFile;
model.action.editDocumentRequest = function (uid) {
  model.data.family(model.newDocumentRequest());
  dataGrid = $("#gridDocumentRequest").data("kendoGrid").dataSource.getByUid(uid);

  if (dataGrid) {
    model.data.documentRequest(model.newDocumentRequest(dataGrid));

    if (!documentRequestReadOnlyFile)
      documentRequestReadOnlyFile = kendo.template($("#fileTemplateReadonly").html());

    model.data.documentRequest().HTMLFile(documentRequestReadOnlyFile(dataGrid));
    $("#modalFormDocumentRequest").modal("show");
  }
}

model.action.removeDocumentRequest = async function (uid) {
    var dialogTitle = "Document Request";
    var dataGrid = $("#gridDocumentRequest").data("kendoGrid").dataSource.getByUid(uid);
    var result = await swalConfirm(dialogTitle, `Are you sure deleting "${dataGrid.Id}" ?`);
    if (result.value && dataGrid) {
        var Id = (dataGrid) ? dataGrid.Id : "";

        try {
            isLoading(true)
            ajaxPost("/ess/employee/removeDocumentRequest/" + Id, {}, function (data) {
                isLoading(false)
                if (data.StatusCode == 200) {
                    model.action.refreshGridDocumentRequest();
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

//employee certificate
model.action.saveCertificate = async function () {
    var certificate = ko.mapping.toJS(model.data.certificate())
    var files = [];
    var $uploader = $("#fileCertificate").getKendoUpload();
    if ($uploader) files = $uploader.getFiles();

    certificate.TypeDescription = model.map.certificateType[certificate.TypeID];

    if (!certificate.TypeID) return swalAlert("Certificate", "Certificate type is required")
    if (new Date(certificate.Validity.Start).getYear() == 0) return swalAlert("Certificate", "Issuing date is required")

    var files = $uploader.getFiles();
    if (!(files.length > 0)) {
        swalAlert("Certificate", "Document attachment is required.");
        return;
    }

    let confirmResult = await swalConfirmText(
        "Certificate",
        `Are you sure saving your data ?<br/>Please specify the reason of your update request below`,
        `Reason`
    );

    if (confirmResult.hasOwnProperty("value")) {
        let reason = confirmResult.value;
        if (!reason) {
            swalAlert(dialogTitle, "Certificate update request reason could not be empty");
            return;
        }

        try {
            let formData = new FormData();
            formData.append("Reason", reason);
            formData.append("JsonData", ko.mapping.toJSON(certificate));
            if (files[0]) {
                formData.append("FileUpload", files[0].rawFile);
            }

            let $modal = $("#modalFormCertificate");
            isLoading(true);
            $modal.modal("hide");
            ajaxPostUpload("/ESS/Employee/SaveCertificate", formData, function (data) {
                isLoading(false);
                if (data.StatusCode == 200) {
                    swalSuccess("Certificate", data.Message);
                    $("#gridCertificate").data("kendoGrid").dataSource.read();
                } else {
                    $modal.modal("show");
                    swalError("Certificate", data.Message);
                }
            }, function (data) {
                $modal.modal("show");
                swalError("Certificate", data.Message);
                isLoading(false);
            })
        } catch (e) {
            isLoading(false);
        }
    }
}

let certificateReadOnlyFile;
model.action.editCertificate = function (uid) {
    dataGrid = $("#gridCertificate").data("kendoGrid").dataSource.getByUid(uid);
    model.data.certificate(model.newCertificate());
    if (dataGrid.UpdateRequest) {
        model.data.certificate(model.newCertificate(dataGrid.UpdateRequest));

        if (!certificateReadOnlyFile)
            certificateReadOnlyFile = kendo.template($("#fileTemplateReadonly").html());

        model.data.certificate().HTMLFile(certificateReadOnlyFile(dataGrid.UpdateRequest));
    } else {
        model.data.certificate(model.newCertificate(dataGrid.Certificate));
        model.data.certificate().Old(dataGrid.Certificate);
    }

    $("#modalFormCertificate").modal("show");
}

model.action.discardCertificate = async function (uid) {
    var dialogTitle = "Certificate";
    dataGrid = $("#gridCertificate").data("kendoGrid").dataSource.getByUid(uid);

    var result = await swalConfirm(dialogTitle, `Are you sure discarding "${dataGrid.UpdateRequest.Description || dataGrid.Certificate.Description}" ?`);
    if (result.value) {
        var Id = (dataGrid.UpdateRequest) ? dataGrid.UpdateRequest.Id : "";

        if (Id) {
            isLoading(true)
            ajaxPost("/ess/employee/discardcertificatechange/" + Id, {}, function (data) {
                isLoading(false)
                if (data.StatusCode == 200) {
                    model.action.refreshGridCertificate();
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

model.action.removeCertificate = async function (uid) {
    var dialogTitle = "Certificate";
    var dataGrid = $("#gridCertificate").data("kendoGrid").dataSource.getByUid(uid);

    var notes = (dataGrid.Certificate.Note) ? dataGrid.Certificate.TypeDescription + ' - ' + dataGrid.Certificate.Note : dataGrid.Certificate.TypeDescription;
    var result = await swalConfirmText(dialogTitle, `Are you sure deleting "${notes}" ?<br/>Please specify the reason of your update request below`, "Reason");

    if (result.hasOwnProperty("value")) {
        let reason = result.value;
        if (!reason) {
            swalAlert(dialogTitle, "Employe update request reason could not be empty");
            return;
        }

        var Id = (dataGrid.Certificate) ? dataGrid.Certificate.AXID : "";

        try {
            isLoading(true)
            ajaxPost("/ess/employee/removecertificate", { Id: Id, Reason: reason }, function (data) {
                isLoading(false)
                if (data.StatusCode == 200) {
                    model.action.refreshGridCertificate();
                    swalSuccess(dialogTitle, data.Message);
                } else {
                    swalError(dialogTitle, data.Message);
                }
            }, function (data) {
                swalError(dialogTitle, data.Message);
                isLoading(false);
            });
        } catch (e) {
            isLoading(false);
        }

    }
}

model.action.downloadCertificate = function (uid) {
    var dataGrid = $("#gridCertificate").data("kendoGrid").dataSource.getByUid(uid);
    var certificateRequestUpdate = dataGrid.UpdateRequest;
    var certificate = dataGrid.Certificate;
    var certificateID = 0;
    var certificateName = "";

    if (!!certificateRequestUpdate && !!certificateRequestUpdate.Filename) {
        certificateID = certificateRequestUpdate.AXID;
        certificateName = certificateRequestUpdate.Filename;
    } else {
        certificateID = certificate.AXID;
        certificateName = certificate.Filename;
    }

    if (certificateID != -1) {
        window.open(`/ESS/Employee/DownloadCertificate/${certificateID}/${certificateName}`);
    }
}

model.action.downloadWarningLetter = function (uid) {
    var dataGrid = $("#gridWarningLetter").data("kendoGrid").dataSource.getByUid(uid);

    if (dataGrid && dataGrid.AXID > 0) {
        window.open(`/ESS/Employee/DownloadWarningLetter/${dataGrid.AXID}/${dataGrid.Filename}`);
    }
}

model.action.downloadMedicalRecord = function (uid) {
    var dataGrid = $("#gridMedicalRecord").data("kendoGrid").dataSource.getByUid(uid);

    if (dataGrid && dataGrid.AXID > 0) {
        window.open(`/ESS/Employee/DownloadMedicalRecord/${dataGrid.AXID}/${dataGrid.Filename}`);
    }
}

model.action.downloadFieldFile = function (data) {
    var d = ko.mapping.toJS(data);
    console.log(d)
    if (d.Filename) {
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

model.action.downloadAddressFile = function (data) {
    var d = ko.mapping.toJS(data);
    if (data.hasOwnProperty("Address")) {
        d = ko.mapping.toJS(data.Address);
    }

    if (d.Filename) {
        window.open(`/ESS/Employee/DownloadAddress/${d.AXID}/${d.Filename}`);
    }
}

//employeedocument
model.action.openFormDocument = function () {
    model.data.listFileName([])
    model.data.document(model.newDocument());
    $("#modalFormDocument").modal("show");
};

model.action.openDocumentUpload = function () {
    $("#documentUpload").click();
};

model.action.saveDocumentUpload = function () {
    var dialogTitle = "Document";
    var document = ko.mapping.toJS(model.data.document())
    var files;
    var $uploader = $("#fileDocument").getKendoUpload();
    if ($uploader) files = $uploader.getFiles();

    if (!document.DocumentType) return swalAlert(dialogTitle, "Document type is required")
    if (!document.Description) return swalAlert(dialogTitle, "Document description is required")
    if (files.length < 1 && !document.Filename) { return swalAlert(dialogTitle, "File is required") }

    let formData = new FormData();
    formData.append("JsonData", ko.mapping.toJSON(document));
    if (files && files.length > 0) {
        formData.append("FileUpload", files[0].rawFile);
    }

    try {
        let $modal = $("#modalFormDocument");
        isLoading(true);
        $modal.modal("hide");
        ajaxPostUpload("/ESS/Employee/SaveDocument", formData, function (data) {
            isLoading(false);
            if (data.StatusCode == 200) {
                swalSuccess(dialogTitle, data.Message);
                $("#gridDocument").data("kendoGrid").dataSource.read();
            } else {
                $modal.modal("show");
                swalError(dialogTitle, data.Message);
            }
        }, function (data) {
            $modal.modal("show");
            swalError(dialogTitle, data.Message);
        })
    } catch (e) {
        isLoading(false);
    }
}

let documentReadOnlyFile;
model.action.editDocument = function (uid) {
    model.data.listFileName([])
    dataGrid = $("#gridDocument").data("kendoGrid").dataSource.getByUid(uid);

    var data = {};

    if (dataGrid.UpdateRequest) {
        data = dataGrid.UpdateRequest;
    } else {
        data = dataGrid.Document;
    }
    model.data.document(model.newDocument(data));

    if (!documentReadOnlyFile)
        documentReadOnlyFile = kendo.template($("#fileTemplateReadonly").html());

    model.data.document().HTMLFile(documentReadOnlyFile(data));
    $("#modalFormDocument").modal("show");
}
model.action.deleteFiles = function () {
    $(".k-upload-files.k-reset").find("li")[0].remove();
    model.data.listFileName([])
}
model.action.discardUploadDocument = async function (uid) {
    dataGrid = $("#gridDocument").data("kendoGrid").dataSource.getByUid(uid);
    var Id = ""

    if (dataGrid.UpdateRequest) {
        Id = dataGrid.UpdateRequest.Id
    } else {
        Id = dataGrid.Document.Id
    }
    var dialog = "Document"
    let confirmResult = await swalConfirm(dialog, "Are you sure to discard your data ?");
    if (confirmResult.value) {
        try {
            isLoading(true)
            ajaxPost("/ess/employee/discarddocumentchange/" + Id, {}, function (data) {
                isLoading(false)
                if (data.StatusCode == 200) {
                    swalSuccess(dialog, data.Message)
                    $("#gridDocument").data("kendoGrid").dataSource.read();
                } else {
                    swalError(dialog, data.Message)
                }
            }, function (data) {
                swalError(dialogTitle, data.Message);
            });
        } catch (e) {
            isLoading(false)
        }
    }
}
model.action.removeUploadDocument = async function (uid) {
    dataGrid = $("#gridDocument").data("kendoGrid").dataSource.getByUid(uid);
    var Id = ""
    if (dataGrid.Document) {
        Id = dataGrid.Document.AXID
    } else {
        Id = dataGrid.UpdateRequest.AXID
    }

    var dialog = "Document"
    let confirmResult = await swalConfirm(dialog, "Are you sure to remove your data ?");
    if (confirmResult.value) {
        try {
            isLoading(true)
            ajaxPost("/ESS/Employee/RemoveDocument/" + Id, {}, function (data) {
                isLoading(false)
                if (data.StatusCode == 200) {
                    swalSuccess(dialog, data.Message)
                    $("#gridDocument").data("kendoGrid").dataSource.read();
                } else {
                    swalError(dialog, data.Message)
                }
            }, function (data) {
                swalError(dialogTitle, data.Message);
            });
        } catch (e) {
            isLoading(false)
        }
    }
}
model.action.downloadFamilyDocument = function (uid) {
    var dataGrid = $("#gridFamily").data("kendoGrid").dataSource.getByUid(uid);
    var familyRequestUpdate = dataGrid.UpdateRequest;
    var familyID = 0;
    var filename = "";

    if (!!familyRequestUpdate && !!familyRequestUpdate.Filename) {
        familyID = familyRequestUpdate.AXID;
        filename = familyRequestUpdate.Filename;
    } else {
        console.warn(`unable to find attached file on ${uid}\n:${dataGrid}`);
        return;
    }

    if (familyID != -1) {
        window.open(`/ESS/Employee/DownloadFamilyDocument/${familyID}/${filename}`);
    }
}

model.action.downloadDocument = function (uid) {
    var dataGrid = $("#gridDocument").data("kendoGrid").dataSource.getByUid(uid);
    var documentRequestUpdate = dataGrid.UpdateRequest;
    var document = dataGrid.Document;
    var documentID = 0;
    var filename = "";

    if (!!documentRequestUpdate && !!documentRequestUpdate.Filename) {
        documentID = documentRequestUpdate.AXID;
        filename = documentRequestUpdate.Filename;
    } else {
        documentID = document.AXID;
        filename = document.Filename;
    }

    if (documentID != -1) {
        window.open(`/ESS/Employee/DownloadDocument/${documentID}/${filename}`);
    }
}

model.action.selectedDropdownMenu = function (e) {
    let title = $(e).text();
    let target = $(e).attr("href");
    $("#dropdownMenuButtons > a.active").removeClass('active');
    if (target) {
        let $tab = $(`#employeeTab li a[href="${target}"]`);
        if ($tab.length > 0) {
            $tab.tab('show');
            return;
        }

        $(e).addClass('active');
    }
}

model.on.uploadFieldAttachment = function (e) {
    var self = model;
    var uid = e.sender.element.data("uid");
    var field = self.data.employee().Fields().find(x => {
        return x.UID() == uid
    });

    if (e.files.length > 0) {
        switch (field.Category()) {
            case "BankAccount":
                field.AccountNumber(field.NewData());
                break;
            case "Tax":
                field.NPWP(field.NewData());
                break;
            case "ElectronicAddress":
                field.Locator(field.NewData());
                break;
            case "ID":
                field.Number(field.NewData());
                break;
            default:
                console.warn(`unable to find category "${field.Category()}"\n${ko.mapping.toJS(field)}`)
                break;
        }

        e.formData = new FormData();
        e.formData.append("JsonData", ko.mapping.toJSON(field));
        e.formData.append("FileUpload", e.files[0].rawFile);
    }
};

model.on.uploadFieldAttachmentSelected = function (e) {
    var self = model;

    var valid = self.app.data.validateKendoUpload(e, "Employee");
    if (valid) {
        var uid = e.sender.element.data("uid");

        var formGroup = $(`#additionalField${uid}`);
        if (formGroup.length > 0) {
            formGroup.addClass("loading");
        }
    } else {
        e.preventDefault();
    }

};

model.on.uploadFieldAttachmentProgress = function (e) {
    var self = model;
    var uid = e.sender.element.data("uid");

    var formGroup = $(`#additionalField${uid}`);
    if (formGroup.length > 0) {
        formGroup.addClass("loading");
    }
};

model.on.uploadFieldAttachmentSuccess = function (e) {
    var self = model;
    var uid = e.sender.element.data("uid");

    var formGroup = $(`#additionalField${uid}`);
    if (formGroup.length > 0) {
        formGroup.removeClass("error");
        formGroup.removeClass("loading");
    }

    var field = self.data.employee().Fields().find(x => {
        return x.UID() == uid
    });

    if (e.response.StatusCode == 200) {
        if (field) {
            for (var name in e.response.Data) {
                if (typeof field[name] == "function") {
                    field[name](e.response.Data[name]);
                }
            }
        }
    } else {
        swalFatal("Employee", `Error occured while uploading "${field.Label()}" attachment:\n${JSON.stringify(e.response.Message)}`);
    }
};

model.on.uploadFieldAttachmentError = function (e) {
    var self = model;
    var uid = e.sender.element.data("uid");

    var field = self.data.employee().Fields().find(x => {
        return x.UID() == uid
    });

    var formGroup = $(`#additionalField${uid}`);
    if (formGroup.length > 0) {
        formGroup.addClass("error");
        formGroup.removeClass("loading");
    }

    this.clearAllFiles();
    swalFatal("Employee", `Error occured while uploading "${field.Label()}" attachment:\n${JSON.stringify(e.XMLHttpRequest)}`);
};

model.on.uploadAddressAttachment = function (e) {
    var self = model;
    var field = self.data.employee().Address;


    if (e.files.length > 0) {
        e.formData = new FormData();
        e.formData.append("JsonData", ko.mapping.toJSON(field));
        e.formData.append("FileUpload", e.files[0].rawFile);
    }
};

model.on.uploadAddressAttachmentSelected = function (e) {
    var self = model;

    var valid = self.app.data.validateKendoUpload(e, "Employee");
    if (valid) {
        var uid = e.sender.element.data("uid");

        var formGroup = $(`#addressField`);
        if (formGroup.length > 0) {
            formGroup.addClass("loading");
        }
    } else {
        e.preventDefault();
    }

};

model.on.uploadAddressAttachmentProgress = function (e) {
    var self = model;
    var uid = e.sender.element.data("uid");

    var formGroup = $(`#addressField`);
    if (formGroup.length > 0) {
        formGroup.addClass("loading");
    }
};

model.on.uploadAddressAttachmentSuccess = function (e) {
    var self = model;
    var uid = e.sender.element.data("uid");

    var formGroup = $(`#addressField`);
    if (formGroup.length > 0) {
        formGroup.removeClass("error");
        formGroup.removeClass("loading");
    }

    if (e.response.StatusCode == 200) {
        for (var name in e.response.Data) {
            var d = e.response.Data[name];
            if (self.data.employee().Address.hasOwnProperty(name) && typeof self.data.employee().Address[name] == "function") {
                self.data.employee().Address[name](d);
            }
        }
    } else {
        swalFatal("Employee", `Error occured while uploading "Address" attachment:\n${JSON.stringify(e.response.Message)}`);
    }
};

model.on.uploadAddressAttachmentError = function (e) {
    var self = model;
    var uid = e.sender.element.data("uid");

    var field = self.data.employee().Fields().find(x => {
        return x.UID() == uid
    });

    var formGroup = $(`#addressField`);
    if (formGroup.length > 0) {
        formGroup.addClass("error");
        formGroup.removeClass("loading");
    }

    this.clearAllFiles();
    swalFatal("Employee", `Error occured while uploading "Address" attachment:\n${JSON.stringify(e.XMLHttpRequest)}`);
};

let familyDocumentTemp = "";
model.on.familyDocumentSelect = function (e) {
    let self = model;
    var valid = self.app.data.validateKendoUpload(e, "Family");
    if (valid) {
        familyDocumentTemp = self.data.family().HTMLFile();
        self.data.family().HTMLFile("");
    } else {
        e.preventDefault();
    }
}

model.on.familyDocumentRemove = function (e) {
    let self = model;
    self.data.family().HTMLFile(familyDocumentTemp);
}

let certificateDocumentTemp = "";
model.on.certificateDocumentSelect = function (e) {
    let self = model;
    var valid = self.app.data.validateKendoUpload(e, "Certificate");
    if (valid) {
        certificateDocumentTemp = self.data.certificate().HTMLFile();
        self.data.certificate().HTMLFile("");
    } else {
        e.preventDefault();
    }
}

model.on.certificateDocumentRemove = function (e) {
    let self = model;
    self.data.certificate().HTMLFile(certificateDocumentTemp);
}

let documentRequestTemp = "";
model.on.documentRequestSelect = function (e) {
    let self = model;
    var valid = self.app.data.validateKendoUpload(e, "Document Request");
    if (valid) {
        documentRequestTemp = self.data.documentRequest().HTMLFile();
        self.data.documentRequest().HTMLFile("");
    } else {
        e.preventDefault();
    }
}

model.on.documentRequestRemove = function (e) {
    let self = model;
    self.data.documentRequest().HTMLFile(documentRequestTemp);
}

let documentTemp = "";
model.on.documentSelect = function (e) {
    let self = model;
    var valid = self.app.data.validateKendoUpload(e, "Document");
    if (valid) {
        documentTemp = self.data.document().HTMLFile();
        self.data.document().HTMLFile("");
    } else {
        e.preventDefault();
    }
}

model.on.documentRemove = function (e) {
    let self = model;
    self.data.document().HTMLFile(documentTemp);
}

// Init
model.init.employeeProfile = async function () {
    let self = model;

    //isLoading(true);
    await self.action.refreshRequestStatus();

    setTimeout(async function () {
        let city = await self.get.city();
        self.list.city(city);
    });

    setTimeout(async function () {
        let maritalStatus = await self.get.maritalStatus();
        self.list.maritalStatus(strArray2DropdownList(maritalStatus));
        self.map.maritalStatus = maritalStatus;
    });

    setTimeout(async function () {
        try {
            await model.render.formEmployee();
            //isLoading(false);
        } catch (e) {
            console.error("error occured while rendering form employee :", e)
            isLoading(false);
        }
    });
};

model.init.employeeFamily = function () {
    var self = model;

    model.render.gridFamily();
};

model.init.employeeDocument = function () {
    var self = model;
    self.render.gridDocument();
    setTimeout(async function () {
        let documentType = await self.get.documentType();
        self.list.documentType(documentType);
        self.action.refreshGridDocument(true);
    });
};

model.init.employeeDocumentRequest = function () {
    var self = model;
    self.render.gridDocumentRequest();
    setTimeout(async function () {
        let documentType = await self.get.documentRequestType();
        self.list.documentRequestType(documentType);
    });
};

model.init.certificate = function () {
    let self = model;

    setTimeout(async function () {
        let type = await self.get.certificateType();

        type.forEach(t => {
            self.map.certificateType[t.TypeID] = t.Description;
            if (!t.Description) {
                t.Description = t.TypeID;
            }
        });

        self.list.certificateType(type);
    });

    self.render.gridCertificate();
};

model.init.employee = async function () {
    let self = model;

    setTimeout(async function () {
        let gender = await self.get.gender();
        self.list.gender(strArray2DropdownList(gender));
        self.map.gender = gender;
        self.action.refreshGridFamily(true);
    });

    setTimeout(async function () {
        let familyRelationship = await self.get.familyRelationship();
        self.list.familyRelationship(familyRelationship);
        familyRelationship.forEach(d => {
            self.map.familyRelationship[d.TypeID] = d;
        });

        self.action.refreshGridFamily(true);
    });

    setTimeout(async function () {
        let religion = await self.get.religion();
        self.list.religion(strArray2DropdownList(religion));
        self.map.religion = religion;
        self.action.refreshGridFamily(true);
    });

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
                case '#resume':
                    await self.init.employeeProfile();
                    break;
                case '#family':
                    await self.init.employeeFamily();
                    break;
                case '#documents':
                    await self.init.employeeDocument();
                    break;
                case '#employments':
                    self.render.gridEmployment();
                    break;
                case '#certificate':
                    self.init.certificate();
                    break;
                case '#installment':
                    self.render.gridInstallment();
                    break;
                case '#warning-letter':
                    self.render.gridWarningLetter();
                    break;
                case '#medical-record':
                    self.render.gridMedicalRecord();
                    break;
                default:
                    break;
            };

            activatedTabMap[target] = true;
        }
    })

    let target = window.location.hash;
    if (target) {
        let $tab = $(`#employeeTab li a[href="${target}"]`);
        if ($tab.length > 0) {
            $tab.tab('show');
            return;
        }
    }

    $('#employeeTab li:first-child a').tab('show')
};