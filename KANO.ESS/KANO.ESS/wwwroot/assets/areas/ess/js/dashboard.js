model.data.StartDate = ko.observable(moment().subtract(8, "days").toDate());
model.data.EndDate = ko.observable(moment().subtract(1, "days").toDate());
model.data.Travel = ko.observableArray([]);
model.data.Agenda = ko.observableArray([]);
model.data.DetailAgenda = ko.observable();

var _niceWords = [
    "Nice", "Good job", "Great", "Well done"
];

model.proto.AttendanceInfo = {
    Absent: true,
    ClockIn: "-",
    ClockOut: "-",
    Productivity: "",
    Good: false,
};

model.newLeaveInfo = function () {
    return ko.mapping.fromJS({ Remainder: 0, Percentage: 0 });
};

model.newAttendanceInfo = function () {
    return ko.mapping.fromJS(this.proto.AttendanceInfo);
};

model.data.todayAttendance = ko.observable(model.newAttendanceInfo());
model.data.leaveInfo = ko.observable(model.newLeaveInfo());
model.data.travelTimelines = ko.observableArray([]);
model.data.canteenInfo = ko.observable({
    VoucherRemaining: 0,
    VoucherUsed: 0,
    VoucherExpired: 0,
    VoucherAlmostExpired: 0
});

model.get.todayAttendance = async function () {

    let response = await ajax("/ESS/TimeManagement/GetAbsenceImported", "GET");
    if (response.StatusCode == 200 && response.Data && response.Data.length > 0) {
        return response.Data;
    }

    console.error(response.StatusCode, ":", response.Message)
    return Object.assign({}, model.proto.AttendanceInfo);
};

model.map.absenceCode = {};
model.get.absenceCode = async function () {

    let response = await ajax("/ESS/TimeManagement/GetAbsenceCodeAll", "GET");
    if (response.StatusCode == 200 && response.Data && response.Data.length > 0) {
        return response.Data || [];
    }

    console.error(response.StatusCode, ":", response.Message)
    return Object.assign({}, model.proto.AttendanceInfo);
};

model.get.leaveInfo = async function () {
    let response = await ajax("/ESS/Leave/GetInfo", "GET");
    if (response.StatusCode == 200 && response.Data && response.Data.length > 0) {
        return response.Data;
    }

    console.error(response.StatusCode, ":", response.Message)
    return Object.assign({}, model.proto.LeaveInfo);
};

model.get.agenda = async function (dateStart, dateFinish) {
    if (!dateStart && !dateFinish) {
        dateStart = moment().subtract(1, "days").toDate();
        dateFinish = moment().add(8, "days").toDate();
    }

    let response = await ajaxPost(`/ESS/Agenda/Get`, {
        Range: {
            Start: dateStart,
            Finish: dateFinish,
        },
    });

    if (response.StatusCode == 200) {
        return response.Data || [];
    }

    console.error("Error while getting agenda(s)", response.StatusCode, ":", response.Message)
    return [];
};

model.app.data.canteenInfo.subscribe(function (data) {
    model.data.canteenInfo(data);
});

//model.render.barChart = function () {
//    if ($("#visit-sale-chart").length) {
//        Chart.defaults.global.legend.labels.usePointStyle = true;
//        var ctx = document.getElementById('visit-sale-chart').getContext("2d");

//        var gradientStrokeViolet = ctx.createLinearGradient(0, 0, 0, 181);
//        gradientStrokeViolet.addColorStop(0, 'rgba(218, 140, 255, 1)');
//        gradientStrokeViolet.addColorStop(1, 'rgba(154, 85, 255, 1)');
//        var gradientLegendViolet = 'linear-gradient(to right, rgba(218, 140, 255, 1), rgba(154, 85, 255, 1))';

//        var gradientStrokeBlue = ctx.createLinearGradient(0, 0, 0, 360);
//        gradientStrokeBlue.addColorStop(0, 'rgba(54, 215, 232, 1)');
//        gradientStrokeBlue.addColorStop(1, 'rgba(177, 148, 250, 1)');
//        var gradientLegendBlue = 'linear-gradient(to right, rgba(54, 215, 232, 1), rgba(177, 148, 250, 1))';

//        var gradientStrokeRed = ctx.createLinearGradient(0, 0, 0, 300);
//        gradientStrokeRed.addColorStop(0, 'rgba(255, 191, 150, 1)');
//        gradientStrokeRed.addColorStop(1, 'rgba(254, 112, 150, 1)');
//        var gradientLegendRed = 'linear-gradient(to right, rgba(255, 191, 150, 1), rgba(254, 112, 150, 1))';

//        var myChart = new Chart(ctx, {
//            type: 'bar',
//            data: {
//                labels: ['JAN', 'FEB', 'MAR', 'APR', 'MAY', 'JUN', 'JUL', 'AUG'],
//                datasets: [
//                    {
//                        label: "CHN",
//                        borderColor: gradientStrokeViolet,
//                        backgroundColor: gradientStrokeViolet,
//                        hoverBackgroundColor: gradientStrokeViolet,
//                        legendColor: gradientLegendViolet,
//                        pointRadius: 0,
//                        fill: false,
//                        borderWidth: 1,
//                        fill: 'origin',
//                        data: [20, 40, 15, 35, 25, 50, 30, 20]
//                    },
//                    {
//                        label: "USA",
//                        borderColor: gradientStrokeRed,
//                        backgroundColor: gradientStrokeRed,
//                        hoverBackgroundColor: gradientStrokeRed,
//                        legendColor: gradientLegendRed,
//                        pointRadius: 0,
//                        fill: false,
//                        borderWidth: 1,
//                        fill: 'origin',
//                        data: [40, 30, 20, 10, 50, 15, 35, 40]
//                    },
//                    {
//                        label: "UK",
//                        borderColor: gradientStrokeBlue,
//                        backgroundColor: gradientStrokeBlue,
//                        hoverBackgroundColor: gradientStrokeBlue,
//                        legendColor: gradientLegendBlue,
//                        pointRadius: 0,
//                        fill: false,
//                        borderWidth: 1,
//                        fill: 'origin',
//                        data: [70, 10, 30, 40, 25, 50, 15, 30]
//                    }
//                ]
//            },
//            options: {
//                responsive: true,
//                legend: false,
//                legendCallback: function (chart) {
//                    var text = [];
//                    text.push('<ul>');
//                    for (var i = 0; i < chart.data.datasets.length; i++) {
//                        text.push('<li><span class="legend-dots" style="background:' +
//                            chart.data.datasets[i].legendColor +
//                            '"></span>');
//                        if (chart.data.datasets[i].label) {
//                            text.push(chart.data.datasets[i].label);
//                        }
//                        text.push('</li>');
//                    }
//                    text.push('</ul>');
//                    return text.join('');
//                },
//                scales: {
//                    yAxes: [{
//                        ticks: {
//                            display: false,
//                            min: 0,
//                            stepSize: 20,
//                            max: 80
//                        },
//                        gridLines: {
//                            drawBorder: false,
//                            color: 'rgba(235,237,242,1)',
//                            zeroLineColor: 'rgba(235,237,242,1)'
//                        }
//                    }],
//                    xAxes: [{
//                        gridLines: {
//                            display: false,
//                            drawBorder: false,
//                            color: 'rgba(0,0,0,1)',
//                            zeroLineColor: 'rgba(235,237,242,1)'
//                        },
//                        ticks: {
//                            padding: 20,
//                            fontColor: "#9c9fa6",
//                            autoSkip: true,
//                        },
//                        categoryPercentage: 0.5,
//                        barPercentage: 0.5
//                    }]
//                }
//            },
//            elements: {
//                point: {
//                    radius: 0
//                }
//            }
//        })
//        $("#visit-sale-chart-legend").html(myChart.generateLegend());
//    }
//};

//model.render.donutChart = function () {
//    if ($("#traffic-chart").length) {
//        var ctx = document.getElementById('traffic-chart').getContext("2d");
//        var gradientStrokeBlue = ctx.createLinearGradient(0, 0, 0, 181);
//        gradientStrokeBlue.addColorStop(0, 'rgba(54, 215, 232, 1)');
//        gradientStrokeBlue.addColorStop(1, 'rgba(177, 148, 250, 1)');
//        var gradientLegendBlue = 'linear-gradient(to right, rgba(54, 215, 232, 1), rgba(177, 148, 250, 1))';

//        var gradientStrokeRed = ctx.createLinearGradient(0, 0, 0, 50);
//        gradientStrokeRed.addColorStop(0, 'rgba(255, 191, 150, 1)');
//        gradientStrokeRed.addColorStop(1, 'rgba(254, 112, 150, 1)');
//        var gradientLegendRed = 'linear-gradient(to right, rgba(255, 191, 150, 1), rgba(254, 112, 150, 1))';

//        var gradientStrokeGreen = ctx.createLinearGradient(0, 0, 0, 300);
//        gradientStrokeGreen.addColorStop(0, 'rgba(6, 185, 157, 1)');
//        gradientStrokeGreen.addColorStop(1, 'rgba(132, 217, 210, 1)');
//        var gradientLegendGreen = 'linear-gradient(to right, rgba(6, 185, 157, 1), rgba(132, 217, 210, 1))';

//        var trafficChartData = {
//            datasets: [{
//                data: [30, 30, 40],
//                backgroundColor: [
//                    gradientStrokeBlue,
//                    gradientStrokeGreen,
//                    gradientStrokeRed
//                ],
//                hoverBackgroundColor: [
//                    gradientStrokeBlue,
//                    gradientStrokeGreen,
//                    gradientStrokeRed
//                ],
//                borderColor: [
//                    gradientStrokeBlue,
//                    gradientStrokeGreen,
//                    gradientStrokeRed
//                ],
//                legendColor: [
//                    gradientLegendBlue,
//                    gradientLegendGreen,
//                    gradientLegendRed
//                ]
//            }],

//            // These labels appear in the legend and in the tooltips when hovering different arcs
//            labels: [
//                'Search Engines',
//                'Direct Click',
//                'Bookmarks Click',
//            ]
//        };
//        var trafficChartOptions = {
//            responsive: true,
//            animation: {
//                animateScale: true,
//                animateRotate: true
//            },
//            legend: false,
//            legendCallback: function (chart) {
//                var text = [];
//                text.push('<ul>');
//                for (var i = 0; i < trafficChartData.datasets[0].data.length; i++) {
//                    text.push('<li><span class="legend-dots" style="background:' +
//                        trafficChartData.datasets[0].legendColor[i] +
//                        '"></span>');
//                    if (trafficChartData.labels[i]) {
//                        text.push(trafficChartData.labels[i]);
//                    }
//                    text.push('<span class="float-right">' + trafficChartData.datasets[0].data[i] + "%" + '</span>')
//                    text.push('</li>');
//                }
//                text.push('</ul>');
//                return text.join('');
//            }
//        };
//        var trafficChartCanvas = $("#traffic-chart").get(0).getContext("2d");
//        var trafficChart = new Chart(trafficChartCanvas, {
//            type: 'doughnut',
//            data: trafficChartData,
//            options: trafficChartOptions
//        });
//        $("#traffic-chart-legend").html(trafficChart.generateLegend());
//    }
//};

model.action.refreshGridAttendance = function (uiOnly = false) {
    var $grid = $("#gridAttendance").data("kendoGrid");
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


model.render.gridAttendance = function () {
    let self = model;
    let $el = $("#gridAttendance");
    if ($el) {
        let $grid = $el.getKendoGrid();

        if (!!$grid) {
            $grid.destroy();
        }

        $el.kendoGrid({
            dataSource: {
                transport: {
                    read: {
                        url: "/ESS/TimeManagement/Get",
                        dataType: "json",
                        type: "POST",
                        contentType: "application/json",
                    },
                    parameterMap: function (data, type) {
                        data.EmployeeID = ""
                        data.Range = {
                            Start: model.data.StartDate(),
                            Finish: model.data.EndDate()
                        }
                        return JSON.stringify(data);
                    }
                },
                schema: {
                    data: function (res) {
                        if (res.StatusCode !== 200 && res.Status !== '') {
                            swalFatal("Fatal Error", `Error occured while fetching time attendance(s)\n${res.Message}`)
                            return []
                        }

                        return res.Data || [];
                    },
                    total: "Total",
                },
                sort: { field: "TimeAttendance.LoggedDate", dir: "desc" },
                error: function (e) {
                    swalFatal("Fatal Error", `Error occured while fetching t.ime attendance(s)\n${e.xhr.responseText}`)
                }
            },
            noRecords: {
                template: "No attendance data available."
            },
            //filterable: true,
            //sortable: true,
            //pageable: true,
            columns: [
                {
                    field: "loggedDate",
                    title: "Date",
                    template: function (e) {
                        return moment(e.TimeAttendance.LoggedDate).format("DD MMM YYYY")
                    },
                    width: 150,
                },
                {
                    field: "loggedDate",
                    title: "Day",
                    template: function (e) {
                        return moment(e.TimeAttendance.LoggedDate).format("dddd")
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
                    title: "Schedule",
                    template: function (e) {
                        var start = moment(e.TimeAttendance.ScheduledDate.Start).format("HH:mm")
                        var end = moment(e.TimeAttendance.ScheduledDate.Finish).format("HH:mm")
                        var result = start + ' - ' + end
                        return result;
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
                    title: "Clock In/Out",
                    template: function (e) {
                        if (e.Absent) {
                            return '-';
                        }

                        var start = moment(e.TimeAttendance.ActualLogedDate.Start).format("HH:mm")
                        var end = moment(e.TimeAttendance.ActualLogedDate.Finish).format("HH:mm")
                        var result = start + ' - ' + end
                        return result;
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
                    field: "TimeAttendance.AbsenceCode",
                    template: function (e) {
                        var ac = e.TimeAttendance.AbsenceCode;
                        //var strAC = self.map.absenceCode[ac];
                        return ac;
                    },
                    title: "Absence",
                    width: 150,
                },
            ]
        });
    }
};

model.render.todayAttendance = function () {
    var self = model;
    return new Promise(async function (resolve, reject) {
        try {
            kendo.ui.progress($("#attendanceContainer"), true);
            var data = await self.get.todayAttendance();
            if (!data) reject(false);
            console.log(data);
            if (data.length > 0) {
                var attendanceInfo = model.newAttendanceInfo();

                var outs = data.filter(x => {
                    return x.InOutField == "OUT";
                });

                var ins = data.filter(x => {
                    return x.InOutField == "IN";
                });

                if (outs.length > 0) {
                    outs = _.orderBy(outs, x => {
                        return x.Clock;
                    }, "desc");

                    let clockOut = moment(outs[0].Clock);
                    attendanceInfo.ClockOut(clockOut.format("HH:mm"));
                }

                if (ins.length > 0) {
                    ins = _.orderBy(ins, x => {
                        return x.Clock;
                    }, "asc");

                    let clockIn = moment(ins[0].Clock);
                    attendanceInfo.ClockIn(clockIn.format("HH:mm"));
                }

                self.data.todayAttendance(attendanceInfo);
            }
            resolve(true);
        } catch (e) {
            reject(false)
        } finally {
            kendo.ui.progress($("#attendanceContainer"), false);
        }
    });
};

model.render.leaveInfo = function () {
    var self = model;
    return new Promise(async function (resolve, reject) {
        try {
            kendo.ui.progress($("#leaveContainer"), true);
            var infos = await self.get.leaveInfo();

            if (!infos) reject(false);
            var totalReminder = 0;
            var totalRights = 0;
            var info = {};
            infos.forEach((i) => {
                totalReminder += i.Remainder;
                totalRights += i.Rights;
            });
            info.Percentage = kendo.toString((Math.abs(totalRights - totalReminder) / totalRights), "p0");
            info.Remainder = totalReminder;
            if (info) {
                model.data.leaveInfo(info);
            }
            resolve(true);
        } catch (e) {
            reject(false);
        } finally {
            kendo.ui.progress($("#leaveContainer"), false);
        }
    });
};

model.data.parseAgendaIcon = function (agendaType) {
    switch (agendaType) {
        default:
        case 1:
            return "mdi mdi-calendar";
        case 2:
            return "mdi mdi-wallet-travel";
    }
};

model.action.showAgenda = function ($data) {
    var data = ko.mapping.toJS($data);
    model.data.DetailAgenda(data);
    $("#ModalDetailAgenda").modal("show");
};

model.get.event = async function () {
    let res = await ajaxPost(`/ESS/MobileAttendance/Event/Employee`, {});
    //let response = JSON.parse(res);
    let response = res;
    if (response.statusCode == 200) {
        return response.data || [];
    }

    console.error("Error while getting event(s)", response.statusCode, ":", response.message)
    return [];
};

model.render.travelTimeline = function () {
    var self = model;
    return new Promise(async function (resolve, reject) {
        try {
            kendo.ui.progress($("#agendaContainer"), true);
            var timelines = [];
            var timelinesMap = {};
            var events = await self.get.event();
            events.forEach(event => {
                //console.log(event)
                let date = standarizeDateFull(event.startTime);
                let dateFinish = standarizeDateFull(event.entTime);
                var clock = standarizeTime(event.startTime);
                var clockFinish = standarizeTime(event.endTime);

                if (!timelinesMap[date]) {
                    timelinesMap[date] = [];
                }

                var scheduleDescription = "";
                var scheduleFull = "";
                if (date == dateFinish) {
                    if (clock != clockFinish) {
                        scheduleDescription = `${clock} - ${clockFinish}`;
                        scheduleFull = `${date} ${clock} - ${clockFinish}`;
                    }
                } else {
                    scheduleDescription = `${standarizeDate(event.startTime)} ${clock} - ${standarizeDate(event.endTime)} ${clockFinish}`;
                    scheduleFull = scheduleDescription;
                }

                timelinesMap[date].push(Object.assign({
                    Issuer: "Event",
                    Name: event.name,
                    Notes: event.description,
                    Description: "",
                    Location: event.locationName,
                    AgendaType: 1,
                    Schedule: {
                        Start: event.startTime,
                        Finish: event.endTime
                    },
                    Attachments: []
                }
                    , { "_scheduleID": "", "_clock": clock, "_schedule": scheduleDescription, "_scheduleFull": scheduleFull }));

            });
            console.log(timelinesMap)

            //for (var date in timelinesMap) {
            //    console.log(date)
            //    timelines.push({ "_date": date, "_data": timelinesMap[date] });
            //}

            var infos = await self.get.agenda();
            infos.forEach(info => {
                ["Start"].forEach(scheduleID => {
                    var date = standarizeDateFull(info.Schedule[scheduleID]);
                    var dateFinish = standarizeDateFull(info.Schedule["Finish"]);
                    var clock = standarizeTime(info.Schedule[scheduleID]);
                    var clockFinish = standarizeTime(info.Schedule["Finish"]);

                    if (!timelinesMap[date]) {
                        timelinesMap[date] = [];
                    }

                    var scheduleDescription = "";
                    var scheduleFull = "";
                    if (date == dateFinish) {
                        if (clock != clockFinish) {
                            scheduleDescription = `${clock} - ${clockFinish}`;
                            scheduleFull = `${date} ${clock} - ${clockFinish}`;
                        }
                    } else {
                        scheduleDescription = `${standarizeDate(info.Schedule[scheduleID])} ${clock} - ${standarizeDate(info.Schedule["Finish"])} ${clockFinish}`;
                        scheduleFull = scheduleDescription;
                    }

                    timelinesMap[date].push(Object.assign(info, { "_scheduleID": scheduleID, "_clock": clock, "_schedule": scheduleDescription, "_scheduleFull": scheduleFull }));
                });

            });

            for (var date in timelinesMap) {
                timelines.push({ "_date": date, "_data": timelinesMap[date] });
            }

            self.data.travelTimelines(timelines);
            resolve(true);
        } catch (e) {
            console.error(e);
            reject(false);
        } finally {
            kendo.ui.progress($("#agendaContainer"), false);
        }
    });
};

model.init = function () {
    var self = model;
    if (self.app.hasAccess('MyAttendance')) {
        self.render.gridAttendance();
        setTimeout(async () => {
            self.action.refreshGridAttendance(true);
        });
        self.render.todayAttendance();
    }
    if (self.app.hasAccess('Leave')) {
        self.render.leaveInfo();
    }
    if (self.app.hasAccess('Travel')) {
        self.render.travelTimeline();
    }
};
