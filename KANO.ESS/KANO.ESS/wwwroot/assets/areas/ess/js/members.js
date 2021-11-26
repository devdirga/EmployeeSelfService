model.newUser = function (data) {
    if (data) {
        return ko.mapping.fromJS(Object.assign(_.clone(this.proto.User), _.clone(data)));
    }
    return ko.mapping.fromJS(this.proto.User);
}

model.data.user = ko.observable(model.newUser());
model.list.user = ko.observableArray([]);
model.list.location = ko.observable([]);
model.data.email = ko.observable("");
model.data.search = ko.observable();
model.list.user = ko.observableArray([]);
model.list.userlocations = ko.observableArray([])
model.render = {}
var savedPageSize = localStorage.getItem("listview-pagesize");
var dataSourcePageSize = savedPageSize !== "undefined" ? (savedPageSize || 5) : undefined;
model.list.isSelfieAuth = ko.observableArray([{ Name: 'Yes', Value: "yes" }, { Name: 'No', Value: "no" }]);

model.render.users = function () {
    let $el = $("#gridMember");
    if (!!$el) {
        let $grid = $el.getKendoGrid();

        if (!!$grid) {
            $grid.destroy();
        }

        $el.kendoGrid({
            dataSource: {
                transport: {
                    read: {
                        url: URL_GET_USER_MANAGEMENT,
                        dataType: "json",
                        type: "POST",
                        contentType: "application/json",
                    },
                    parameterMap: function (data, type) {
                        //return JSON.stringify({
                        //    Start: model.data.StartDate(),
                        //    Finish: model.data.EndDate(),
                        //});
                        //console.log("param: ", data)
                        return JSON.stringify(data);
                    },
                },
                schema: {
                    data: function (res) {
                        if (res.StatusCode !== 200 && res.Status !== "") {
                            swalFatal(
                                "Fatal Error",
                                `Error occured while fetching survey(s)\n${res.Message}`
                            );
                            return [];
                        }
                        return res.Data || [];
                    },
                    total: "Total",
                },
                error: function (e) {
                    swalFatal(
                        "Fatal Error",
                        `Error occured while fetching survey(s)\n${e.xhr.responseText}`
                    );
                },
                serverPaging: true,
                serverSorting: true,
                serverFiltering: true,
                pageSize: 10,
                //sort: { field: "RecruitmentID", dir: "asc" },
            },
            pageable: {
                previousNext: false,
                info: false,
                numeric: false,
                refresh: true,
            },
            noRecords: {
                template: "No member data available.",
            },
            filterable: false,
            scrollable: true,
            sortable: true,
            pageable: true,
            columns: [
                { width: 130, field: "Id", title: "EmployeeID" },

                {
                    headerAttributes: {
                        class: "text-center",
                    },
                    field: "FullName",
                    title: "Name",
                    width: 200
                },
                {
                    headerAttributes: {
                        class: "text-center",
                    },
                    field: "Email", title: "Email",
                    width: 220
                },
                {
                    headerAttributes: {
                        class: "text-center",
                    },
                    width: 130,
                    field: "Status",
                    title: "Role",
                    template: function (d) { return d.Roles[0] }
                },
                {
                    headerAttributes: {
                        class: "text-center",
                    },
                    field: "Location",
                    title: "Location",
                    template: function (d) {
                        let temp = []
                        if (d.Location) {
                            d.Location.map(function (x) { temp.push(x.Name) })
                            return temp.join(", ")
                        }
                        return "-";
                    },
                    width: 200
                },
                {
                    headerAttributes: {
                        class: "text-center",
                    },
                    attributes: {
                        class: "text-center",
                    },
                    field: "IsSelfieAuth",
                    title: "Selfie",
                    width: 90
                },
                {
                    headerAttributes: {
                        class: "text-center",
                    },
                    attributes: {
                        class: "text-center",
                    },
                    width: 90, field: "", title: "Action",
                    template: function (d) {
                        return `<button type="button" class="btn btn-xs btn-outline-info mr-2" onclick="model.action.editMember('${d.uid}')"><i class="fa mdi mdi-pencil"></i></button>`
                    }
                },
            ],
        });
    }
}

model.action.search = function () {
    var grid = $('#gridMember').data('kendoGrid');
    var field = 'FullName';
    var operator = 'contains';
    var value = model.data.search();
    grid.dataSource.filter({ field: field, operator: operator, value: value })
}

model.get.location = function () {
    let param = {}
    ajaxPost("/ESS/MobileAttendance/GetLocation", param, async function (res) {
        //let result = JSON.parse(`${res}`)
        let result = res;
        result.map(function (v, i) {
            result[i].Name = v.Name
            result[i].Id = v.Id
        })
        model.list.location(result)
    })
}

model.action.editMember = async function (uid) {
    var grid = $("#gridMember").data("kendoGrid")
    var data = grid.dataSource.getByUid(uid)
    console.log(data)
    if (data) {
        model.data.user(model.newUser(data));
        if (data.Location) {
            data.Location.forEach((l) => {
                model.list.userlocations.push(ko.mapping.toJS(l))
            })
        }
    }
    $("#memberModal").modal('show')
}

model.action.save = function () {
    let dt = ko.mapping.toJS(model.data.user());
    let k = $("#LocationMember").data("kendoMultiSelect");
    let param = {
      Id: dt.Id,
      Location: k.dataItems(),
      IsSelfieAuth: dt.IsSelfieAuth
    }
    ajaxPost("/ESS/MobileAttendance/SaveLocationMember", param, async function (res) {
        $("#memberModal").modal('hide')
        model.init.Member(res.Data)
    })
}


model.init.Member = function () {
    model.render.users()
}
