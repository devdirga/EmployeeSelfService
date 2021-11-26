model.list.survey = ko.observableArray([]);
model.list.surveyRecurrences = ko.observableArray([]);
model.data.StartDate = ko.observable(moment().subtract(7, "days").toDate());
model.data.EndDate = ko.observable(new Date());

model.init.surveyEmployee = function () {
    let self = model;
    let recurrent = _.clone(model.proto.Recurrent);
    for (let i in recurrent)
        self.list.surveyRecurrences.push({ text: recurrent[i], value: i });

    //self.render.surveyEmployee();
}

model.render.surveyEmployee = function () {
    displayLoading("#gridSurvey", true);
    ajaxPost("/ESS/SurveyESS/GetSurvey", {}, function (res) {
        ///let email = document.getElementById("EmployeeEmail").innerHTML;
        //console.log(email)
        //let data = JSON.parse(res);
        let data = res.Data;
        displayLoading("#gridSurvey", false);

        $("#gridSurvey").kendoGrid({
            noRecords: {
                template: "No survey available."
            },
            columns: [
                {
                    field: "Title",
                    title: "Title / Description",
                    template: function (d) {
                        let title = d.Title;
                        var str = `<i class="mdi mdi-file-document-edit-outline"></i> Start Survey`;
                        //console.log("-->",d)
                        //var result = str.link(d.public_url + "?email=" + email);
                        //let link = d.public_url + "?email=" + email;
                        let link = d.SurveyUrl;
                        if (d.AlreadyFilled) {
                            return `<strong>${title}</strong><br/><span class="text-success">Finish <i class="fa mdi mdi-check-circle text-success mdi-24px"></i></span>`;
                        } else {
                            return `<strong>${title}</strong><br/><br/><a class="text-danger" href="${link}" target="_blank">${str}</a>`;
                        }
                        
                    },
                    width: 140
                },
                {
                    field: "Schedule",
                    title: "Schedule / Public Link",
                    template: function (d) {
                        let strSchedule = `${standarizeDate(d.Schedule.Start)} until ${moment(d.Schedule.Finish).format("DD/MMM/YYYY")}<br />
                            <small class="text-info">Public Link: ${d.SurveyUrl}</small>`
                        return `${strSchedule}`;
                    },
                    width: 220,
                },
                {
                    field: "Recurrent",
                    title: "Recurrent",
                    template: function (d) {
                        return model.proto.Recurrent[d.Recurrent] || "-";
                    },
                    width: 120,
                },
                //{
                //    field: "SurveyUrl",
                //    title: "Link",
                //    attributes: {
                //        "class": "table-cell text-danger",
                //        //style: "text-align: right; font-size: 14px"
                //    },
                //    template: function (d) {
                //        var str = "Start Survey";
                //        console.log(d)
                //        //var result = str.link(d.public_url + "?email=" + email);
                //        //let link = d.public_url + "?email=" + email;
                //        let link = d.SurveyUrl;
                //        if (d.AlreadyFilled) {
                //            return '<span class="text-success">Finish <i class="fa mdi mdi-check-circle text-success"></i></span>';
                //        } else {
                //            return '<a href="' + link + '" target="_blank">' + str + '</a>';
                //        }
                //    },
                //    width: 150,
                //}
            ],
            dataSource: {
                //data: data["survey.survey"]
                data: data
            }
        });
    })
}

model.get.surveyResult = function () {
    ajaxPost("/ESS/SurveyESS/Employee", {}, function (res) {
        console.log(res)
    })
}

model.get.surveyHistory = function () {
    ajaxPost("/ESS/SurveyESS/History", {}, function (res) {
        console.log(res)
    })
}

model.init.surveyHistory = function () {
    let activatedTabMap = {};
    let self = model;
    $('a[data-toggle="tab"]').on('shown.bs.tab', async function (e) {
        let title = $(e.target).text();
        let target = $(e.target).attr('href');
        let relatedTarget = $(e.relatedTarget).attr('href');

        //var breadcrumbs = self.breadcrumbs();
        //if (breadcrumbs.length > 3) {
        //    breadcrumbs.splice(breadcrumbs.length - 1, 1)
        //}
        //breadcrumbs.push({
        //    Title: title,
        //    URL: "#",
        //});
        //self.breadcrumbs(breadcrumbs);

        if (!activatedTabMap[target]) {
            switch (target) {
                case '#survey':
                    await self.render.surveyEmployee()
                    break;
                case '#history':
                    await self.render.surveyHistory();
                    break;
                default:
                    break;
            };

            activatedTabMap[target] = true;
        }
    });

    let target = window.location.hash;
    if (target) {
        let $tab = $(`#employeeTab li a[href="${target}"]`);
        if ($tab.length > 0) {
            $tab.tab('show');
            return;
        }
    }

    $('#employeeTab li:first-child a').tab('show')
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

model.action.filterHistory = function () {
    model.render.surveyHistory();
}

model.render.surveyHistory = function () {
    //model.get.surveyHistory();
    let $el = $("#gridHistory");
    if (!!$el) {
        let $grid = $el.getKendoGrid();

        if (!!$grid) {
            $grid.destroy();
        }

        $el.kendoGrid({
            dataSource: {
                transport: {
                    read: {
                        url: "/ESS/SurveyESS/History",
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
                        //if (res.length == 0) {
                        //    swalFatal(
                        //        "Fatal Error",
                        //        `Error occured while fetching survey(s)\n${res.Message}`
                        //    );
                        //    return [];
                        //}
                        return res || [];
                    },
                    total: "Total",
                },
                error: function (e) {
                    swalFatal(
                        "Fatal Error",
                        `Error occured while fetching survey(s)\n${e.xhr.responseText}`
                    );
                },
                //sort: { field: "RecruitmentID", dir: "asc" },
            },
            pageable: {
                previousNext: false,
                info: false,
                numeric: false,
                refresh: true,
            },
            noRecords: {
                template: "No history data available.",
            },
            //filterable: true,
            //sortable: true,
            //pageable: true,
            height: 640,
            columns: [
                {
                    headerAttributes: {
                        class: "text-center",
                    },
                    field: "survey_id[1]",
                    title: "Title",
                    width: 200
                },
                {
                    headerAttributes: {
                        class: "text-center",
                    },
                    attributes: {
                        class: "text-center",
                    },
                    field: "create_date",
                    title: "Date Survey",
                    template: function (d) {
                        return standarizeDate(d.create_date);
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
                    field: "quizz_score",
                    title: "Score",
                    width: 110
                },
                {
                    headerAttributes: {
                        class: "text-center",
                    },
                    attributes: {
                        class: "text-center",
                    },
                    template: function (d) {
                        return `<button type="button" class="btn btn-xs btn-info" onclick="model.action.detailHistory('${d.uid}')"><i class="mdi mdi-pencil"></i></button>`;
                    },
                    width: 50,
                },
            ],
        });
    }
}

model.data.totalScore = ko.observable();
model.data.detailFill = ko.observable();
model.data.title = ko.observable();
model.data.dateFill = ko.observable();
model.action.detailHistory = function (uid) {
    $("#detailFillModal").modal("show");
    let data = $("#gridHistory").data("kendoGrid").dataSource.getByUid(uid);
    console.log(data)
    displayLoading("#AnswerSurvey", true);
    ajaxPost("/ESS/Survey/GetListAnswer", { Id: data.id }, function (res) {
        console.log("answer: ", res)
        let total = 0;
        res.Data.forEach(function (x) {
            total += x.answer_score;
        });
        model.data.totalScore(total);
        model.data.detailFill(res.Data);
        model.data.title(res.Data[0].survey_id[1]);
        model.data.dateFill(moment(res.Data[0].create_date).format("DD MMM YYYY, HH:mm"));
        displayLoading("#AnswerSurvey", false);
    })
}