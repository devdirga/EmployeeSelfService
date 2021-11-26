model.newEvent = function (data) {
    if (data) {
        return ko.mapping.fromJS(Object.assign(_.clone(this.proto.Event), _.clone(data)));
    }
    return ko.mapping.fromJS(this.proto.Event);
};

model.data.event = ko.observable(model.newEvent());
model.data.attendees = ko.observableArray([]);
model.list.user = ko.observableArray([]);
model.list.locations = ko.observableArray([]);

model.init.event = function () {
    model.render.user();
    model.init.locations();
    model.render.event();
}

model.render.event = function () {
    let $el = $("#gridEvent");
    if (!!$el) {
        let $grid = $el.getKendoGrid();

        if (!!$grid) {
            $grid.destroy();
        }

        $el.kendoGrid({
            dataSource: {
                transport: {
                    read: {
                        url: URL_GET_EVENT,
                        dataType: "json",
                        type: "POST",
                        contentType: "application/json",
                    },
                    parameterMap: function (data, type) {
                        //return JSON.stringify({
                        //    Start: model.data.StartDate(),
                        //    Finish: model.data.EndDate(),
                        //});
                        return JSON.stringify(data);
                    },
                },
                schema: {
                    data: function (res) {
                        if (res.statusCode !== 200) {
                            swalFatal(
                                "Fatal Error",
                                `Error occured while fetching data`
                            );
                            return [];
                        }
                        return res.data || [];
                    },
                    total: "total",
                },
                error: function (e) {
                    swalFatal(
                        "Fatal Error",
                        `Error occured while fetching data`
                    );
                },
                sort: { field: "RecruitmentID", dir: "asc" },
            },
            pageable: {
                previousNext: false,
                info: false,
                numeric: false,
                refresh: true,
            },
            noRecords: {
                template: "No survey data available.",
            },
            //filterable: true,
            //sortable: true,
            //pageable: true,
            columns: [
                {
                    headerAttributes: {
                        class: "text-center",
                    },
                    title: "Name",
                    field: "name",
                    width: 160
                },
                {
                    headerAttributes: {
                        class: "text-center",
                    },
                    title: "Description",
                    field: "description",
                    width: 260,
                },
                {
                    headerAttributes: {
                        class: "text-center",
                    },
                    title: "Time",
                    template: function (d) {
                        return `Start ${moment(d.startTime).format('DD MMM YYYY, HH:mm')} <br />End ${moment(d.endTime).format('DD MMM YYYY, HH:mm')}`;
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
                    title: "Location",
                    field: "locationName",
                    width: 200
                },
                {
                    headerAttributes: {
                        class: "text-center",
                    },
                    attributes: {
                        class: "text-center",
                    },
                    width: 90,
                    field: "",
                    title: "Action",
                    template: function (d) {
                        var btnEdit = `
                                <button type="button" class="btn btn-xs btn-outline-info mr-2" onclick="model.action.editEvent('${d.uid}')">
                                        <i class="fa mdi mdi-pencil"></i>
                                </button>`;

                        var btnDelete = `
                                <button type="button" class="btn btn-xs btn-outline-danger" onclick="model.action.deleteEvent('${d.uid}')">
                                        <i class="fa mdi mdi-delete"></i>
                                </button>`;

                        return btnEdit + " " + btnDelete;
                    }
                },
            ],
        });
    }
}

model.init.locations = function () {
    let param = {}
    ajaxPost("/ESS/MobileAttendance/GetLocation", param, function (res) {
        //let result = JSON.parse(`${res}`);
        let result = res;
        //console.log(result);
        model.list.locations(result);
    });
}

model.render.user = function () {
    let param = {}
    ajaxPost(URL_GET_USER_MANAGEMENT, param, async function (res) {
        console.log("-->", res);
        model.list.user(res.Data);
    })
    //$("#Attendees").kendoMultiSelect({
    //    placeholder: "Select products...",
    //    dataTextField: "FullName",
    //    dataValueField: "Id",
    //    dataSource: {
    //        type: "odata",
    //        serverFiltering: true,
    //        transport: {
    //            read: {
    //                url: URL_GET_USER_MANAGEMENT,
    //                data: function (p) {
    //                    console.log(p)
    //                    return {
    //                        text: $("#Attendees").data('kendoMultiSelect').input.val()
    //                    };
    //                }
    //            }
    //        }
    //    }
    //});
}

model.on.filtering = (e) => {
    //console.log("sasa:", e);
    if (e.filter) {
        let param = {
            Field: e.filter.field,
            Value: e.filter.value,
            Operator: e.filter.operator
        };

        ajaxPost(URL_GET_USER_MANAGEMENT, param, async function (res) {
            //console.log("-->", res);
            model.list.user(res.Data);
        });
    }
}

model.action.showModal = function () {
    model.data.event(model.newEvent());
    $("#eventModal").modal('show');
}

model.action.save = async function () {
    isLoading(true);
    let data = ko.mapping.toJS(model.data.event());
    console.log("data:", data)
    let param = {
        Attendees: []
    };
    let name = `${data.Name}`;
    let confirmResult = await swalConfirm("User", `Are you sure saving event ${name} ?`);
    //let x = $("#LocationID").data("kendoDropDownList");
    //param.Attendees = x.dataItems();
    if (data.Id.substr(0, 3) == "000") {
        delete param["Id"];
    } else {
        param.Id = data.Id
    }
    
    //param.Id = null;
    param.LocationID = data.LocationID;
    param.LocationName = $("#LocationID").data("kendoDropDownList").text();
    //param.EntityID = x.dataItem();
    param.Name = name;
    param.Description = data.Description;
    param.StartTime = data.StartTime;
    param.EndTime = data.EndTime;

    var x = $("#Attendees").data("kendoMultiSelect")
    x.dataItems().map(function (y) {
        param.Attendees.push({
            UserID: y.UserID,
            FullName: y.FullName,
            Email: y.Email
        });
    });
    console.log("save:", param)
    //return false;
    if (confirmResult.value) {
        ajaxPost(URL_SAVE, param, function (res) {
            //console.log("===>", res)
            if (res.StatusCode == 200) {
                swalSuccess("Success", res.Message);
                model.render.event();
                $("#eventModal").modal('hide');
            } else {

            }
            isLoading(false);
        }, function (err) {
            swalError("Error", err)
        })
    }
}

model.action.editEvent = async function (uid) {
    let dataGrid = $("#gridEvent").data("kendoGrid").dataSource.getByUid(uid);
    console.log("edit:", dataGrid);
    //model.data.event(model.newEvent(dataGrid));
    model.data.event().Id(dataGrid.id);
    model.data.event().Name(dataGrid.name);
    model.data.event().Description(dataGrid.description);
    let locationEvent = $("#LocationID").data("kendoDropDownList");
    locationEvent.value(dataGrid.locationID);
    model.data.event().LocationID(dataGrid.locationID);
    model.data.event().LocationName(locationEvent.dataItem().name);

    let attendances = $("#Attendees").data("kendoMultiSelect");
    //attendances.setDataSource(dataGrid.attendances);
    //model.list.user(dataGrid.attendances)
    let dtSource = [];
    attendances.dataSource.data().map(function (item) {
        dtSource.push({
            UserID: item.UserID,
            FullName: item.FullName,
            Email: item.Email
        })
    })

    let val = [];
    dataGrid.attendees.forEach(function (item, index) {
        val.push(item.userID);
        dtSource.push({
            UserID: item.userID,
            FullName: item.fullName,
            Email: item.email
        });
    });
    dtSource = _.uniqBy(dtSource, 'UserID');
    //console.log(dtSource)
    attendances.dataSource.data(dtSource);
    //_.uniqBy(attendances.dataSource.data(), function (e) {
    //    return e.UserID;
    //});
    //console.log(val)
    attendances.value(val)

    var startTime = $("#StartTime").data("kendoDateTimePicker");
    startTime.value(dataGrid.startTime);
    model.data.event().StartTime(dataGrid.startTime);
    var endTime = $("#EndTime").data("kendoDateTimePicker");
    endTime.value(dataGrid.endTime);
    model.data.event().EndTime(dataGrid.endTime);
    $("#eventModal").modal("show");
}

model.action.deleteEvent = async function (uid) {
    let dataGrid = $("#gridEvent").data("kendoGrid").dataSource.getByUid(uid);
    //console.log(dataGrid);
    let confirmResult = await swalConfirm("User", `Are you sure saving event ${name} ?`);

    if (confirmResult.value) {
        ajaxPost(`/ESS/MobileAttendance/Event/Delete/${dataGrid.id}`, {}, function (res) {
            if (res.StatusCode == 200) {
                swalSuccess("Success", res.Message);
                model.render.event();
                $("#eventModal").modal('hide');
            }
        })
    }
}