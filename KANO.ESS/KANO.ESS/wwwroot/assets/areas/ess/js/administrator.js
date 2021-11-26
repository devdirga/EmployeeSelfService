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
