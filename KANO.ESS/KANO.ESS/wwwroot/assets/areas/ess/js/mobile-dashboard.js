model.init.dashboard = async function () {
    let self = model;
    await Promise.all([
        new Promise(async (resolve) => {
            await self.get.location();
            resolve(true);
        }),

        new Promise(async (resolve) => {
            await self.render.activityLog();
            await self.get.getActivityLogAll();
            resolve(true);
        }),

        new Promise(async (resolve) => {
            await model.init.map();

            resolve(true);
        })
    ]);
}
model.data.StartDate = ko.observable(moment().subtract(7, "days").toDate());
model.data.EndDate = ko.observable(new Date());

model.data.toDay = ko.observableArray();
model.is.buttonLoading = ko.observable(true);
model.list.location = ko.observableArray([]);
model.get.location = function () {
    let param = {}
    ajaxPost("/ESS/MobileAttendance/GetLocation", param, function (res) {
        //let result = JSON.parse(`${res}`);
        let result = res;
        console.log("res1", result)
        model.list.location(result)
    });
}

model.get.getActivityLogAll = function () {
    let param = {
        Start: model.data.StartDate(),
        Finish: model.data.EndDate()
    }
    ajaxPost("/ESS/MobileAttendance/GetActivityLogAll", param, function (res) {
        console.log("activity log all:", res);
    });
}

model.list.activitylog = ko.observableArray([]);

model.action.filter = function () {
    model.render.activityLog()
}

model.render.activityLog = function () {
    let $el = $("#gridCheckIn");
    if (!!$el) {
        let $grid = $el.getKendoGrid();

        if (!!$grid) {
            $grid.destroy();
        }

        $el.kendoGrid({
            dataSource: {
                transport: {
                    read: {
                        url: "/ESS/MobileAttendance/GetActivityLog",  // /ESS/Survey/GetRange
                        dataType: "json",
                        type: "POST",
                        contentType: "application/json",
                    },
                    parameterMap: function (data, type) {
                        return JSON.stringify({
                            Start: model.data.StartDate(),
                            Finish: model.data.EndDate(),
                        });
                    },
                },
                schema: {
                    data: function (res) {
                        //let res = JSON.parse('{"StatusCode":200,"Message":null,"Data":[{"Id":"5efd63541e44273afc50e2a7","OdooID":"1399596506","Title":"Covid 19","Schedule":{"TrueMonthly":0.032258064516129031,"Month":0.0,"Days":0.0,"Hours":0.0,"Seconds":0.0,"Start":"2020-07-03T00:00:00+07:00","Finish":"2020-09-15T00:00:00+07:00"},"Recurrent":2,"Mandatory":false,"ParticipantType":3,"Participants":["7312020022"],"Department":null,"Published":true,"SurveyDate":"2020-07-02T00:00:00","Done":true,"LastUpdate":"2020-07-02T04:32:24.046Z","UpdateBy":null,"CreatedDate":"2020-07-02T04:32:20.826Z","CreatedBy":"7312020022"},{"Id":"5efd63541e44273afc50e2a7","OdooID":"1399596506","Title":"Covid 19","Schedule":{"TrueMonthly":0.032258064516129031,"Month":0.0,"Days":0.0,"Hours":0.0,"Seconds":0.0,"Start":"2020-07-03T00:00:00+07:00","Finish":"2020-09-15T00:00:00+07:00"},"Recurrent":2,"Mandatory":false,"ParticipantType":3,"Participants":["7312020022"],"Department":null,"Published":true,"SurveyDate":"2020-07-03T00:00:00","Done":true,"LastUpdate":"2020-07-02T04:32:24.046Z","UpdateBy":null,"CreatedDate":"2020-07-02T04:32:20.826Z","CreatedBy":"7312020022"},{"Id":"5efd63541e44273afc50e2a7","OdooID":"1399596506","Title":"Covid 19","Schedule":{"TrueMonthly":0.032258064516129031,"Month":0.0,"Days":0.0,"Hours":0.0,"Seconds":0.0,"Start":"2020-07-03T00:00:00+07:00","Finish":"2020-09-15T00:00:00+07:00"},"Recurrent":2,"Mandatory":false,"ParticipantType":3,"Participants":["7312020022"],"Department":null,"Published":true,"SurveyDate":"2020-07-04T00:00:00","Done":false,"LastUpdate":"2020-07-02T04:32:24.046Z","UpdateBy":null,"CreatedDate":"2020-07-02T04:32:20.826Z","CreatedBy":"7312020022"},{"Id":"5efd63541e44273afc50e2a7","OdooID":"1399596506","Title":"Covid 19","Schedule":{"TrueMonthly":0.032258064516129031,"Month":0.0,"Days":0.0,"Hours":0.0,"Seconds":0.0,"Start":"2020-07-03T00:00:00+07:00","Finish":"2020-09-15T00:00:00+07:00"},"Recurrent":2,"Mandatory":false,"ParticipantType":3,"Participants":["7312020022"],"Department":null,"Published":true,"SurveyDate":"2020-07-05T00:00:00","Done":false,"LastUpdate":"2020-07-02T04:32:24.046Z","UpdateBy":null,"CreatedDate":"2020-07-02T04:32:20.826Z","CreatedBy":"7312020022"},{"Id":"5efd63541e44273afc50e2a7","OdooID":"1399596506","Title":"Covid 19","Schedule":{"TrueMonthly":0.032258064516129031,"Month":0.0,"Days":0.0,"Hours":0.0,"Seconds":0.0,"Start":"2020-07-03T00:00:00+07:00","Finish":"2020-09-15T00:00:00+07:00"},"Recurrent":2,"Mandatory":false,"ParticipantType":3,"Participants":["7312020022"],"Department":null,"Published":true,"SurveyDate":"2020-07-06T00:00:00","Done":true,"LastUpdate":"2020-07-02T04:32:24.046Z","UpdateBy":null,"CreatedDate":"2020-07-02T04:32:20.826Z","CreatedBy":"7312020022"},{"Id":"5efd63541e44273afc50e2a7","OdooID":"1399596506","Title":"Covid 19","Schedule":{"TrueMonthly":0.032258064516129031,"Month":0.0,"Days":0.0,"Hours":0.0,"Seconds":0.0,"Start":"2020-07-03T00:00:00+07:00","Finish":"2020-09-15T00:00:00+07:00"},"Recurrent":2,"Mandatory":false,"ParticipantType":3,"Participants":["7312020022"],"Department":null,"Published":true,"SurveyDate":"2020-07-07T00:00:00","Done":false,"LastUpdate":"2020-07-02T04:32:24.046Z","UpdateBy":null,"CreatedDate":"2020-07-02T04:32:20.826Z","CreatedBy":"7312020022"}],"Total":0}')
                        model.is.buttonLoading(false);
                        if (res.statusCode != 200) {
                            swalFatal(
                                "Fatal Error",
                                `Error occured while fetching survey(s)\n`
                            );
                            return [];
                        }
                        //console.log(moment(res.data[0].timeAbsence).format("DDMMYYYY"))
                        if (res.data.length > 0) {
                            if (moment(res.data[0].timeAbsence).format("DDMMYYYY") == moment().format("DDMMYYYY")) {
                                model.data.toDay(res.data[0].total);
                            } else {
                                model.data.toDay(0);
                            }
                        } else {
                            model.data.toDay(0);
                        }
                        
                        return res.data || [];
                    },
                    total: "Total",
                },
                error: function (e) {
                    swalFatal(
                        "Fatal Error",
                        `Error occured while fetching survey(s)\n`
                    );
                },
                sort: { field: "Id", dir: "asc" },
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
            detailInit: detailInit,
            dataBound: function (e) {
                //this.expandRow(this.tbody.find("tr.k-master-row").first());
                var grid = e.sender;

                grid.tbody.find("tr.k-master-row").click(function (e) {
                    var target = $(e.target);
                    if ((target.hasClass("k-i-expand")) || (target.hasClass("k-i-collapse"))) {
                        return;
                    }

                    var row = target.closest("tr.k-master-row");
                    var icon = row.find(".k-i-expand");

                    if (icon.length) {
                        grid.expandRow(row);
                    } else {
                        grid.collapseRow(row);
                    }
                })
            },

            columns: [
                {
                    headerAttributes: {
                        class: "text-center",
                    },
                    attributes: {
                        class: "text-center",
                    },
                    field: "timeAbsence",
                    title: "Time",
                    template: function (d) {
                        if (d.timeAbsence) {
                            //console.log("===>",d)
                            return moment(d.timeAbsence).format("dddd, DD MMM YYYY")
                        } else {
                            return "";
                        }
                        
                    },
                    width: 220
                },
                {
                    headerAttributes: {
                        class: "text-center",
                    },
                    attributes: {
                        class: "text-center",
                    },
                    field: "total",
                    title: "Total",
                    width: 160
                },
            ],
        });
    }
}

function detailInit(e) {
    //console.log("-->",e)
    $("<div/>").appendTo(e.detailCell).kendoGrid({
        dataSource: {
            //type: "odata",
            transport: {
                read: {
                    url: `/ESS/MobileAttendance/GetActivityLogDetail`,  // /ESS/Survey/GetRange
                    dataType: "json",
                    type: "POST",
                    contentType: "application/json",
                },
                parameterMap: function (data, type) {
                    console.log("grid:",data);
                    //return JSON.stringify({
                    //    temp: e.data.timeAbsence
                    //});
                    data.temp = e.data.timeAbsence;
                    return JSON.stringify(data);
                },
            },
            schema: {
                data: function (res) {
                    if (res.length == 0) {
                        swalFatal("Fatal Error", `no data`)
                        return []
                    }
                    return res.data || [];
                },
                total: "total",
            },
            error: function (e) {
                swalFatal(
                    "Fatal Error",
                    `Error occured while fetching detail survey(s)\n`
                );
            },
            serverPaging: true,
            serverSorting: true,
            serverFiltering: true,
            pageSize: 10,
            //filter: {
            //    field: "DateTime",
            //    operator: "eq",
            //    value: e.data.timeAbsence
            //}
        },
        pageable: {
            previousNext: true,
            info: true,
            numeric: true,
            refresh: false,
        },
        noRecords: {
            template: "No survey data available.",
        },
        scrollable: false,
        sortable: true,
        //pageable: true,
        columns: [
            {
                field: "timeAbsence",
                title: "Time",
                width: 110,
                template: function (d) {
                    return moment(d.timeAbsence).format("DD MMM YYYY, HH:mm:ss")
                },
            },
            {
                field: "locationName",
                title: "Location",
                width: 130
            },
            {
                field: "employeeID",
                title: "Employee",
                width: 180,
                template: function (d) {
                    return `${d.employeeID}<br />${d.fullName}`
                } 
            }
        ]
    });
}

model.init.map = async function () {
    mapboxgl.accessToken = 'pk.eyJ1IjoiYXlpZXh6MjIiLCJhIjoiY2s4cW5ndzZsMDVybTNucTg5dGl6M2dlcCJ9.X3Z5LULu6famrzySGByLoQ';
    var map = new mapboxgl.Map({
        container: 'map',
        style: 'mapbox://styles/mapbox/streets-v11',
        center: [112.74176, -7.248337],
        zoom: 10,
        markerType: 'people',
        //markerColor: {
        //    people: this.randomMaterialColor('', 'darken'),
        //    transaction: this.randomMaterialColor('', 'darken'),
        //    hours: this.randomMaterialColor('', 'darken'),
        //},
    });

    var scale = new mapboxgl.ScaleControl({
        maxWidth: 80,
        unit: 'imperial'
    });
    map.addControl(scale);

    scale.setUnit('metric');

    map.addControl(new mapboxgl.NavigationControl());

    map.on('load', function () {
        let ini = model;
        console.log("map loaded location:", ini.list.location());
        ini.list.location().map(function (m) {
            if (!m.isVirtual) {
                //console.log(m.longitude, m.latitude)
                new mapboxgl.Marker()
                    .setLngLat([m.Longitude, m.Latitude])
                    .addTo(map);
                //new mapboxgl.Popup({ closeOnClick: false })
                //    .setLngLat([m.longitude, m.latitude])
                //    .setHTML(m.name)
                //    .addTo(map);
            }
        });
        
    });
}