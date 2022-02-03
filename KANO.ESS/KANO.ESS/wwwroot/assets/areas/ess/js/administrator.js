model.newDocumentRequest = function (obj) {
  if (obj && typeof obj == "object") {
    return ko.mapping.fromJS(Object.assign(_.clone(this.proto.DocumentRequest), obj));
  }
  return ko.mapping.fromJS(this.proto.DocumentRequest);
};

//Method UI Component Render
model.render = {};

model.render.gridUserGroup = function () {
    let $el = $("#gridUserGroup");
    if (!!$el) {
        let $grid = $el.getKendoGrid();

        if (!!$grid) {
            $grid.destroy();
        }

        $el.kendoGrid({
            dataSource: {
                transport: {
                    read: URL_GET_USER_GROUP
                },
                schema: {
                    data: "Data",
                    total: "Total",
                }
            },
            //filterable: true,
            sortable: true,
            pageable: true,
            noRecords: {
                template: "No group data available."
            },
            columns: [
                {
                    field: "Group",
                    title: "User Group",
                },
                {
                    field: "TotalUser",
                    title: "Total User",
                    width: "150px"
                },
            ]
        });
    }
}

model.render.gridCreateUserGroup = function () {
    let $el = $("#gridCreateUserGroup");
    if (!!$el) {
        let $grid = $el.getKendoGrid();

        if (!!$grid) {
            $grid.destroy();
        }

        $el.kendoGrid({
            dataSource: {
                transport: {
                    read: URL_GET_MENU
                },
                schema: {
                    data: "Data",
                    total: "Total",
                }
            },
            //filterable: true,
            //sortable: true,
            //pageable: true,
            noRecords: {
                template: "No group data available."
            },
            columns: [
                {
                    field: "Module",
                    title: "Module",
                },
                {
                    field: "Menu",
                    title: "Menu",
                },
                {
                    field: "Privileages",
                    title: "Privileages",
                },
            ]
        });
    }
}

model.render.gridUserManagement = function () {
    let $el = $("#gridUserManagement");
    if (!!$el) {
        let $grid = $el.getKendoGrid();

        if (!!$grid) {
            $grid.destroy();
        }

        $el.kendoGrid({
            dataSource: {
                transport: {
                    read: URL_GET_USER_MANAGEMENT
                },
                schema: {
                    data: "Data",
                    total: "Total",
                }
            },
            //filterable: true,
            sortable: true,
            pageable: true,
            noRecords: {
                template: "No user data available."
            },
            columns: [
                {
                    field: "Username",
                    title: "User login",
                },
                {
                    field: "FullName",
                    title: "Employee Name",
                },
                // {
                //     field: "UserGroup",
                //     title: "User Group",
                // },
                // {
                //     field: "Authentication",
                //     title: "Authentication",
                // },
                {
                    field: "LastUpdate",
                    title: "Latest Connection",
                },
            ]
        });
    }
}

//Actions
model.action = {};

//Page UserGroup
model.action.openCreateUserGroup = function () {
    model.render.gridCreateUserGroup()
    $("#modalUserGroup").modal("show");
};

model.action.saveUserGroup = function () {
    $("#modalUserGroup").modal("hide");
};

//Page UserManagement
model.action.openCreateUserManagement = function () {
    $("#modalUserManagement").modal("show");
};

model.action.saveUserManagement = function () {
    $("#modalUserManagement").modal("hide");
};


//Data
model.data = {}

//Page UserManagement
model.data.valueEmployeeName = ko.observable();
model.data.valueEmployeeEmail = ko.observable();
model.data.valueUsername = ko.observable();
model.data.valueUserGroup = ko.observable();
model.data.valueAuthentication = ko.observable();

model.data.employeeName = ko.observableArray([]);
model.data.userGroup = ko.observableArray([]);
model.data.authentication = ko.observableArray([]);

// Update Config Password
model.data.ConfigPassword = ko.observable(_.cloneDeep(model.proto.ConfigPassword));
model.render.ConfigPassword = function () {
  const url = URL_GET_CONFIG_PASSWORD;
  ajax(url, "GET", {}, function (res) {
    model.data.ConfigPassword(res.Data);
  });
}

model.action.updateConfig = function () {
  let formData = new FormData()
  formData.append("JsonData", JSON.stringify(ko.mapping.toJS(model.data.ConfigPassword())))
  isLoading(true)
  ajaxPostUpload("/ESS/Administrator/UpdateConfigPassword", formData, function (res) {
    console.log(res)
    isLoading(false)
    if (res.StatusCode == 200) {
      swalSuccess(`Config Password`, res.Message);
    } else {
      swalFatal(dialogTitle, res.Message)
    }
  }, function (res) {
    isLoading(false)
    swalFatal(dialogTitle, res.Message)
  })
}

//MobileVersion
model.data.androidmobile = ko.observable(_.cloneDeep(model.proto.Mobile))
model.data.iosmobile = ko.observable(_.cloneDeep(model.proto.Mobile))
model.render.mobileversion = () => {
  ajax(URL_GET_MOBILE_VERSION, "GET", {}, function (res) {
    res.Data.forEach((d) => {
      if (d.Type == "android") {
        model.data.androidmobile(d)
      }
      if (d.Type == "ios") {
        model.data.iosmobile(d)
      }
    })
  })
}

model.action.updateAndroidVersion_ = () => {
  let dialogTitle = `Mobile Version`
  let formData = new FormData()
  formData.append(`JsonData`, JSON.stringify(ko.mapping.toJS(model.data.androidmobile())))
  var files = $('#Filepath').getKendoUpload().getFiles()
  if (files.length > 0) {
    formData.append(`FileUpload`, files[0].rawFile)
  } else {
    swalAlert(dialogTitle, `Document attachment could not be empty`)
    return
  }
  try {
    isLoading(true)
    ajaxPostUpload(`/ESS/Administrator/UpdateAndroidVersion`, formData, function (data) {
      isLoading(false);
      if (data.StatusCode == 200) {
        swalSuccess(dialogTitle, data.Message)
      } else {
        swalFatal(dialogTitle, data.Message)
      }
    }, (data) => {
      isLoading(false);
      swalFatal(dialogTitle, data.Message)
    })
  } catch (e) {
    isLoading(false)
  }
  return false
}

model.action.updateIOSVersion_ = () => {
  let dialogTitle = `Mobile Version`
  let formData = new FormData()
  formData.append(`JsonData`, JSON.stringify(ko.mapping.toJS(model.data.iosmobile())))
  var files = $('#Filepath2').getKendoUpload().getFiles()
  if (files.length > 0) {
    formData.append(`FileUpload`, files[0].rawFile)
  } else {
    swalAlert(dialogTitle, `Document attachment could not be empty`)
    return
  }
  try {
    isLoading(true);
    ajaxPostUpload(`/ESS/Administrator/UpdateIOSVersion`, formData, function (data) {
      isLoading(false)
      if (data.StatusCode == 200) {
        swalSuccess(dialogTitle, data.Message)
      } else {
        swalFatal(dialogTitle, data.Message)
      }
    }, (data) => {
      isLoading(false)
      swalFatal(dialogTitle, data.Message)
    })
  } catch (e) {
    isLoading(false)
  }
  return false
}

// Document Request
model.list.documentType = ko.observableArray([]);
model.list.documentRequestType = ko.observableArray([]);
model.data.documentRequest = ko.observable(model.newDocumentRequest());
model.init.employeeDocumentRequest = function () {
  var self = model;
  self.render.gridDocumentRequest();
  setTimeout(async function () {
    let documentType = await self.get.documentRequestType();
    self.list.documentRequestType(documentType);
  });
};
model.action.RefreshDocumentRequest = function (UIOnly = false) {
  var $grid = $("#gridDocumentRequest").data("kendoGrid")
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
            console.log(res);
            if (res.StatusCode !== 200 && res.Status !== '') {
              swalFatal("Fatal Error", `Error occured while fetching document request(s)\n${res.Message}`)
              return []
            }

            return res.Data || [];
          },
          total: "Total",
          sort: ({ field: "CreatedDate", dir: "desc" }),
        },
        error: function (e) {
          swalFatal("Fatal Error", `Error occured while fetching document request(s)\n${e.xhr.responseText}`)
        }
      },
      sortable: true,
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
            return (d.CreatedDate);
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
        {
          headerAttributes: {
            "class": "text-center",
          },
          attributes: {
            "class": "text-center",
          },
          field: "ValidDate.Finish",
          title: "Valid Until",
          width: 125,
          template: (d) => {
            return standarizeDate((d.ValidDate) ? d.ValidDate.Finish : null);
          },
          hidden: true
        },
        {
          hidden: true,
          headerAttributes: {
            "class": "text-center",
          },
          attributes: {
            "class": "text-center",
          },
          field: "Status",
          title: "Approval Status",
          template: function (data) {
            let status = data.Status;
            let statusClass = {
              0: {
                class: "badge badge-warning",
                text: "Waiting for Approval",
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

            return `<span class="${(statusClass[status].class)}">${statusClass[status].text}</span>`;
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
            var disabled = (data.Status == 1 && !!data.ValidDate) ? '' : 'disabled';
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

model.action.editDocumentRequest = function (uid) {
  model.data.documentRequest(model.newDocumentRequest());
  dataGrid = $("#gridDocumentRequest").data("kendoGrid").dataSource.getByUid(uid);

  if (dataGrid) {
    model.data.documentRequest(model.newDocumentRequest(dataGrid));
    $("#modalFormDocumentRequest").modal("show");
  }
}

model.get.documentRequestType = async function () {
  let response = await ajax("/ESS/Employee/GetDocumentRequestType", "GET");
  if (response.StatusCode == 200) {
    return response.Data;
  }
  return [];
};

model.action.updateDocumentRequest = function () {
  let dialogTitle = "Update Document Request";
  let data = ko.mapping.toJS(model.data.documentRequest());
  let formData = new FormData();
  formData.append("JsonData", JSON.stringify(data));
  if ($('#fileDocumentRequest').length > 0) {
    let files = $('#fileDocumentRequest').getKendoUpload().getFiles();
    if (files.length > 0) {
      formData.append("FileUpload", files[0].rawFile);
    } else {
      swalAlert(dialogTitle, "Document attachment could not be empty");
      return;
    }
  }
  try {
    isLoading(true);
    ajaxPostUpload("/ESS/Employee/UpdateDocumentRequest", formData, function (data) {
      isLoading(false);
      if (data.StatusCode == 200) {
        model.action.RefreshDocumentRequest();
        swalSuccess(dialogTitle, data.Message);
        $("#modalFormDocumentRequest").modal("hide");
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
