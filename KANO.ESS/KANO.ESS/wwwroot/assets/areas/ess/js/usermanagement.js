model.form = {
    action: ko.observable("detail"),
    page: ko.observable("grid"),
    DetailData: ko.observable(_.cloneDeep(model.proto.user)),
    Detail: ko.mapping.fromJS(_.cloneDeep(model.proto.user)),
    Data: ko.observable(),
    DataFilter: ko.observable(),
    GroupId: ko.observable(),
    getFilter: function(ParentId) {
        const url = URL_GET_GROUP_MANAGEMENT
        ajax(url, "GET", {}, function (res) {
            model.form.DataFilter(ko.mapping.toJS(res.Data));

            $("#GroupId").kendoDropDownList({
                dataSource: model.form.DataFilter(),
                dataTextField: "Name",
                dataValueField: "Id",
                optionLabel: "Select Group",
            });
            setTimeout(function () {
                $("#GroupId").data("kendoDropDownList").value(ParentId);
            }, 300);
        });
    },
    GetDataGrid: function () {
        // const url = URL_GET_USER_MANAGEMENT
        // ajax(url, "GET", {}, function (res) {
        //     model.form.Data(ko.mapping.toJS(res.Data));
        //     model.form.initGrid();
        // });
        model.form.getDataGroup();
        model.form.initGrid();
    },
    getDataGroup: function() {
        const url = URL_GET_GROUP_MANAGEMENT
        ajax(url, "GET", {}, function (res) {
            var ds = []
            var data = ko.mapping.toJS(res.Data);
            data.forEach(function (e) {
                ds.push({
                    "text": e.Name,
                    "id": e.Id
                });
            });
            model.form.DataGroup(ds)
        });
    },
    DataGroup: ko.observable(),
    refreshGrid: function (uiOnly = false) {
        var $grid = $("#gridMain").data("kendoGrid");
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
    },
    initGrid: function() {
        //var url = "UserManagements/GetUserManagements";
        $('#gridMain').html("");
        $('#gridMain').kendoGrid({
            dataSource: {
                transport: {
                    read: {
                        url: URL_GET_USER_MANAGEMENT,
                        dataType: "json",
                        type: "POST",
                        contentType: "application/json",
                    },
                    parameterMap: function(data, type) {
                        //viewModel.isLoading(true)
                        console.log("xxx:",data)
                        if (data.filter != undefined) {
                            var dt = data.filter.filters
                            dt.forEach(function (e) {
                                if (e.field == "RoleDescription") {
                                    e.field = "Roles"
                                    e.value = e.value
                                }
                            })
                        }

                        //console.log(data)
                        return JSON.stringify(data);
                    }
                },
                schema: {
                    data: function (res) {
                        if (res.StatusCode !== 200 && res.Status !== '') {
                            swalFatal("Fatal Error", `Error occured while fetching user(s)\n${res.Message}`)
                            return []
                        }
                        return res.Data || [];
                    },
                    total: "Total",
                },
                error: function (e) {
                    swalFatal("Fatal Error", `Error occured while fetching user(s)\n${e.xhr.responseText}`)
                },
                sort: { field: "Fullname", dir: "asc" },
                pageSize: 10,
                serverPaging: true,
                serverFiltering: true,
                serverSorting: true,
            },            
            pageable: {
                refresh: true,
                pageSizes: 10,
                buttonCount: 5
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
                template: "No user data available."
            },
            columns: [
                {
                    field: "Username",
                    title: "Username",
                    template: function (e) {
                        return `<a href="#" onclick="model.form.ShowDetail('${e.Username}','detail'); return false;">${e.Username}</a>`;
                    },
                    width: 200
                },
                {
                    field: "FullName",
                    title: "Employee Name",
                    width: 250
                },
                {
                    field: "Email",
                    title: "Email",
                    width: 250
                },                
                {
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                       "class": "text-center",
                    },
                    field: "RoleDescription",
                    title: "Role",
                    width: 150,
                    filterable: {
                        ui: model.form.roleFilter
                    }             
                },
                {
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                       "class": "text-center",
                    },
                    field: "LastUpdate",
                    title: "Last Updated",
                    template: function (e) {
                        return `<div title="${standarizeDateTime(e.LastUpdate)}">${relativeDate(e.LastUpdate)}</div>`;
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
                    field: "UpdateBy",
                    title: "Updated By",
                    template: function (e) {
                        return e.UpdateBy || '-';
                    },
                    width: 250
                },
                {
                    field: "", title: "",
                    width: 75,
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
                    template: function (e) {
                        return `<button class="btn btn-xs btn-outline-warning" onclick="model.form.ShowDetail('${e.Username}','edit'); return false;"><i class="fa mdi mdi-pencil"></i></button>`;
                    },

                },
            ],
        });

    },
    roleFilter: function (element) {
        element.kendoDropDownList({
            dataSource: model.form.DataGroup(),
            optionLabel: "Select Role",
            dataTextField: "text",
            dataValueField: "id"
        });
    },
    ShowDetail: function(id, action) {
        let param = {
            Username: id.toString()
        };
        isLoading(true);
        const url = URL_GET_DETAILS_USER_MANAGEMENT
        ajax(url, "GET", param, function (res) {
            model.form.action(action);
            model.form.DetailData(ko.mapping.toJS(res.Data[0]));
            var Group = model.form.DetailData().Roles[0];
            model.form.GroupId(Group)
            isLoading(false);
            if (model.form.DetailData().Enable) {
                $("#Enable").prop("checked", true);
            }
            $("#modalUserManagement").modal("show");
            model.form.getFilter(Group);            
        }, function (data) {
            isLoading(false);
            swalFatal("User", data.Message);
        });
    },
    Save: async function () {
        var dialogTitle = "User";
        var param = model.form.DetailData;      
        if (!model.form.DetailData().FullName) {
            return swalAlert(dialogTitle, "Full Name could not be empty");
        }
        if (!model.form.DetailData().Email) {
            return swalAlert(dialogTitle, "Email could not be empty");
        }
        if(model.form.action() == 'new'){
            if (!model.form.DetailData().Password) {
                return swalAlert(dialogTitle, "Password could not be empty");
            }
        }
        if (!model.form.GroupId()) {
            return swalAlert(dialogTitle, "Group could not be empty");
        }

        var name = `${param().FullName} (${param().Username})`;        
        let confirmResult = await swalConfirm("User", `Are you sure saving user ${name} ?`);
        if (confirmResult.value) {        
            isLoading(true);
            const url = URL_SAVE_USER
            model.form.DetailData().Roles = [model.form.GroupId()]
            var param = ko.toJS(param);
            console.log(param);
            ajaxPost(url, param, function (res) {
                $("#modalUserManagement").modal("hide");
                model.form.refreshGrid();
                isLoading(false);
                swalSuccess("User", `User ${name} has been saved successfully`)                                
            }, function (data) {
                isLoading(false);
                swalFatal("User", data.Message);
                $("#modalUserManagement").modal("show");
            });        
        }
    },
    Add: () => {
        model.form.getFilter()
        model.form.DetailData({})
        model.form.DetailData().Enable = true;
        $("#Enable").prop("checked", true);
        model.form.action('new');
        $("#modalUserManagement").modal("show");
    }
}
