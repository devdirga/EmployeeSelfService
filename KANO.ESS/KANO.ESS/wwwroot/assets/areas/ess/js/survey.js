model.newSurvey = function (obj) {
  let proto = _.clone(this.proto.Survey || {});

  if (typeof obj != "object") {
    obj = {};
  }

  var data = Object.assign(proto, obj);

  if (!data.Schedule) {
    data.Schedule = _.clone(this.proto.DateRange);
    data.Schedule.Start = _.clone(_DEFAULT_DATE);
    data.Schedule.Finish = _.clone(_DEFAULT_DATE);
    }
  return ko.mapping.fromJS(data);
};

model.newSurveySummary = function (obj) {
    let proto = _.clone(this.proto.SurveySummary || {})
    if (typeof obj != "object") {
        obj = {}
    }
    var data = Object.assign(proto, obj)
    return ko.mapping.fromJS(data)
}

model.data.title = ko.observable();
model.data.dateFill = ko.observable();
model.data.StartDate = ko.observable(moment().subtract(7, "days").toDate());
model.data.EndDate = ko.observable(new Date());
model.data.survey = ko.observable(model.newSurvey());
model.data.surveysummary = ko.observable(model.newSurveySummary());
model.data.percentsurvey = ko.observable();
model.data.today = ko.observable(new Date());
model.data.detailFill = ko.observable();
model.is.buttonLoading = ko.observable(true);
model.is.buttonExport = ko.observable(true);

model.list.surveyRecurrences = ko.observableArray([]);
model.list.surveyRecurrence = ko.observableArray([]);
model.list.participantTypes = ko.observableArray([]);
model.list.recruitmentTypes = ko.observableArray([]);
model.list.jobs = ko.observableArray([]);
model.list.positions = ko.observableArray([]);
model.list.summary = ko.observableArray([]);
model.list.departments = ko.observableArray([]);

model.map.jobs = {};
model.map.positions = {};

model.data.kendoformat = ko.observable();
model.data.kendodepth = ko.observable();
model.data.kendoenabled = ko.observable();
model.data.kendodateval = ko.observable(0);
model.data.ispickerweekly = ko.observable(false);
model.data.employees = ko.observableArray([]);

model.render.gridSurvey = function () {
  let $el = $("#gridSurvey");
  if (!!$el) {
    let $grid = $el.getKendoGrid();

    if (!!$grid) {
      $grid.destroy();
    }

    $el.kendoGrid({
      dataSource: {
        transport: {
          read: {
            url: "/ESS/Survey/GetRange",
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
          field: "Title",
          title: "Title",
            template: function (d) {
                let publicLink = d.SurveyUrl.split("?")[0];
                if (d.Schedule) {
                    let strStartDate = standarizeDate(d.Schedule.Start);
                    let strFinishDate = standarizeDate(d.Schedule.Finish);
                    let strStartDateTime = standarizeDateTime(d.Schedule.Start);
                    let strFinishDateTime = standarizeDateTime(d.Schedule.Finish);
                    var strStartTime = standarizeTime(d.Schedule.Start);
                    var strFinishTime = standarizeTime(d.Schedule.Finish);
                    let strSchedule = "";
                    if (strStartDate == strFinishDate) {

                        if (strStartDateTime == strFinishDateTime) {
                            strSchedule = `${strStartDate} at ${strStartTime}`;
                        } else {
                            strSchedule = `${strStartDate} at ${strStartTime} - ${strFinishTime}`;
                        }
                    } else {
                        strSchedule = `Start ${strStartTime == '00:00' ? strStartDate : strStartDateTime} - until ${strFinishTime == '00:00' ? strFinishDate : strFinishDateTime}`;
                    }
                    return `<strong>${d.Title}</strong><br/><small>${strSchedule}</small><br /><small class="text-info">Public: ${publicLink}</small>`;
                    //return d.Title;
                } else {
                    return `<strong>${d.Title}</strong><br><small class="text-danger">Not yet setting</small>`;
                }
              },
            width: 240
        },
        {
          headerAttributes: {
            class: "text-center",
          },
          attributes: {
            class: "text-center",
          },
          title: "Participant",
          field: "ParticipantType",
            template: function (d) {
            //if (d.ParticipantType == "2") {
            //  return d.Department || "-";
            //}
                let type = model.proto.ParticipantType[d.ParticipantType] || "-"
                if (d.ParticipantType == 2) {
                    return type + '<br /><small class="text-info">' + d.Departments.join(", ")
                }
                return type + '<br /><small class="text-info">' + d.Participants.length + ' participants</small>';
            },
          width: 160
        },
        {
            headerAttributes: {
                class: "text-center",
            },
            attributes: {
                class: "text-center",
            },
            title: "Recurrent",
            field: "SurveyRecurrent",
            template: function (d) {
                return model.proto.SurveyRecurrent[d.Recurrent] || "-";
            },
            width: 100,
        },
        {
            headerAttributes: {
                class: "text-center",
            },
            attributes: {
                class: "text-center",
            },
            title: "Setting",
            template: function (d) {
                let cls = (!d.DBId) ? "btn btn-sm btn-warning" : "btn btn-sm btn-success";
                if (d.Published) {
                    //return `<button type="button" class="` + cls + `" onclick="model.action.setting('${d.uid}')">Setting</button>`;
                    return `<i class="fa mdi mdi-check-circle text-success"></i>`;
                } else {
                    return `<button type="button" class="btn btn-default" onclick="model.action.setting('${d.uid}')"><i class="mdi mdi-settings"></i></buttton>`;
                }
                //return `<button type="button" class="btn btn-default" onclick="model.action.setting('${d.uid}')"><i class="mdi mdi-settings"></i></buttton>`;
            },
            width: 110
        },
        {
          headerAttributes: {
            class: "text-center",
          },
          attributes: {
            class: "text-center",
            },
          title: "Publish",
          template: function (d) {
              return `<button onclick="${d.Published ? "model.action.unpublish('" + d.uid + "')" : "model.action.publish('" + d.uid + "')"}" 
                class="btn btn-default ${
                  d.Published ? "text-danger" : "text-success"
                }">            
                ${d.Published ? '<i class="mdi mdi-cloud-download-outline mdi-24px"></i>' : '<i class="mdi mdi-cloud-upload-outline mdi-24px"></i>'}
                </button>`;
          },
          width: 120,
        },
        //{
        //  headerAttributes: {
        //    class: "text-center",
        //  },
        //  attributes: {
        //    class: "text-center",
        //  },
        //  template: function (d) {
        //    return `<button type="button" class="btn btn-xs btn-primary" onclick="model.action.detailSurvey('${d.uid}')"><i class="mdi mdi-cards"></i></button>`;
        //  },
        //  width: 50,
        //},
        {
          headerAttributes: {
            class: "text-center",
          },
          attributes: {
            class: "text-center",
          },
          template: function (d) {
            return `<button type="button" class="btn btn-xs btn-info" onclick="model.action.testSurvey('${d.uid}')"><i class="mdi mdi-pencil"></i></button>`;
          },
          width: 50,
        },
      ],
    });
  }
};

model.init.surveyHistory = function () {
    model.is.buttonExport(false);
    model.render.gridSurveyHistory();
}

model.render.gridSurveyHistory = function () {
    let $el = $("#gridSurveyHistory");
    if (!!$el) {
        let $grid = $el.getKendoGrid();

        if (!!$grid) {
            $grid.destroy();
        }

        $el.kendoGrid({
                dataSource: {
                    transport: {
                        read: {
                            url: "/ESS/survey/GetSurveyOdoo",  // /ESS/Survey/GetRange
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
                            if (res.error) {
                                swalFatal(
                                    "Fatal Error",
                                    `Error occured while fetching survey(s)\n${res.Message}`
                                );
                                return [];
                            }
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
                });

                var items = e.sender.items();
                items.each(function () {
                    var row = $(this);
                    var dataItem = e.sender.dataItem(row);
                    //if (!dataItem.hasChildren) {
                    //    row.find(".k-hierarchy-cell").html("");
                    //}
                    //console.log("-->", dataItem)
                    if (dataItem.answer_count == 0) {
                        row.find(".k-hierarchy-cell").html("");
                    }
                });
            },

            columns: [
                {
                    headerAttributes: {
                        class: "text-center",
                    },
                    attributes: {
                        class: "text-center",
                    },
                    field: "Id",
                    title: "Id",
                    width: 70,
                },
                {
                    headerAttributes: {
                        class: "text-center",
                    },
                    field: "Title",
                    title: "Title",
                    template: function (d) {
                        if (d.Schedule) {
                            return `${d.Title}<br />${moment(d.Schedule.Start).format('DD MMM YYYY')} Until ${moment(d.Schedule.End).format('DD MMM YYYY')}`
                        } else {
                            return `${d.Title}<br /><span class="text-danger">Not yet setting</span>`;
                        }
                        
                    },
                    width: 280
                },
                {
                    headerAttributes: {
                        class: "text-center",
                    },
                    field: "Description",
                    title: "Description",
                    template: function (d) {
                        return stripHtml(d.Description)
                    },
                },
                {
                    headerAttributes: {
                        class: "text-center",
                    },
                    attributes: {
                        class: "text-center",
                    },
                    field: "State",
                    title: "Status",
                    width: 90,
                    template: function (d) {
                        return d.State == "open" ? `<span class="text-success">Open</span>` : `<span class="text-danger">Close</span>`;
                    }
                },
                {
                    headerAttributes: {
                        class: "text-center",
                    },
                    attributes: {
                        class: "text-center",
                    },
                    field: "answer_done_count",
                    title: "Total Answered",
                    width: 145
                },
                //{
                //    headerAttributes: {
                //        class: "text-center",
                //    },
                //    attributes: {
                //        class: "text-center",
                //    },
                //    title: "Participant / Answered<br />Today",
                //    width: 190,
                //    template: function (d) {
                //        return `${d.Participants.length} / ${d.TodayAnswers}`
                //    }
                //},
                //{
                //    headerAttributes: {
                //        class: "text-center",
                //    },
                //    attributes: {
                //        class: "text-center",
                //    },
                //    title: "Action",
                //    template: function (d) {
                //        let cls = (!d.DBId) ? "btn btn-sm btn-warning" : "btn btn-sm btn-success";
                //        if (d.answer_done_count > 0) {
                //            return `<button type="button" class="` + cls + `" onclick="model.action.detailHistory('${d.uid}')">Detail</button>`;
                //        } else {
                //            return '<span>no one has answered yet</span>'
                //        }
                //    },
                //    width: 100
                //}
            ],
        });
    }
};


async function detailInit(row) {
    //console.log("--->",row)
    let dateInMonth = getDaysInMonth(parseInt(moment().format("MM"))-1, parseInt(moment().format("YYYY")));
    let arrColumns = [
        {
            field: "EmployeeID",
            title: "Employee ID",
            width: 130
        }, {
            field: "FullName",
            title: "Name",
            width: 210
        }, {
            field: "Department",
            title: "Department",
            width: 210
        }
    ];

    dateInMonth.forEach(function (dt) {
        //console.log(dt);
        arrColumns.push({
            headerAttributes: {
                class: "text-center",
            },
            attributes: {
                class: "text-center",
            },
            title: moment(dt).format("DD"),
            width: 85,
            template: function (d) {
                //console.log("==>", dt, ": ", d["D" + moment(dt).format("DD")]);
                let score = "";
                if (d["D" + moment(dt).format("DD")] !== undefined) {
                    if (d["D" + moment(dt).format("DD")].PassedCategory == "npassed" || d["D" + moment(dt).format("DD")].PassedCategory == "passed") {

                        if (d)
                            if (d["D" + moment(dt).format("DD")].ScoringType == "no_scoring") {
                                score = d["D" + moment(dt).format("DD")].Score;
                            } else {
                                score = d["D" + moment(dt).format("DD")].Score + "%";
                            }

                    } else {
                        if (row.data.passing_score_type == "1") {
                            score = d["D" + moment(dt).format("DD")].Score + "%";
                        } else {
                            score = d["D" + moment(dt).format("DD")].PassingGradeCategory;
                        }
                    }
                    return d["D" + moment(dt).format("DD")] != "" ?
                        `<a href="javascript:;" 
                            onclick="showDetailFill(${d["D" + moment(dt).format("DD")].Ids})">${score}</a>` : "";
                } else {
                    return "-";
                }
                
                
            }
        })
    });

    let dataSource = new kendo.data.DataSource({
        transport: {
            read: {
                url: "/ESS/survey/GetSurveyRecordById",  // /ESS/Survey/GetRange
                dataType: "json",
                type: "POST",
                contentType: "application/json",
                complete: function (res, status) {
                    //console.log(status)
                    if (status === "success") {
                        //console.log("#########:", res.responseJSON)
                        //dataku = res.responseJSON.data;
                    }

                }
            },
            parameterMap: function (data, type) {
                data.Id = row.data.Id;
                data.Type = row.data.passing_score_type;
                //return JSON.stringify({
                //    Id: 2
                //});
                return JSON.stringify(data);
            },
        },
        schema: {
            data: function (res) {
                if (res.Grid.length == 0) {
                    swalFatal("Fatal Error", `no data`)
                    return [];
                }
                return res.Grid || [];
            },
            total: "TotalSurvey",
        },
        requestStart: function () {
            //kendo.ui.progress($("#loading"), true);
            
        },
        requestEnd: function (e) {
            //kendo.ui.progress($("#loading"), false);
            console.log("End: ", e.response)
            $("#toexcel-" + row.data.Id).attr("file", e.response.ExcelFile);
            $("#topdf-" + row.data.Id).attr("file", e.response.PDFFile);
        },
        serverPaging: true,
        serverSorting: true,
        serverFiltering: true,
        pageSize: 20,
        filter: {
            field: "survey_id[0]",
            operator: "eq",
            value: row.data.Id
        }
    });
    $("<div id='toexcel-" + row.data.Id + "' class='btn btn-sm m-2 btn-info' onclick='downloadExcel(this)' file=''><i class='mdi mdi-file-excel'> Excel</i></div>").appendTo(row.detailCell);
    $("<div id='topdf-" + row.data.Id + "' class='btn btn-sm m-2 btn-success' onclick='model.action.showPDF(this)' file=''><i class='mdi mdi-file-pdf'></i> PDF</div>").appendTo(row.detailCell);
    var grid = $("<div id='grid-" + row.data.Id + "' class='detail' style='width: 100%;overflow-x: scroll;' />").appendTo(row.detailCell).kendoGrid({
        //toolbar: ["pdf"],
        //pdf: {
        //    allPages: true,
        //    fileName: "Survey List.pdf",
        //    proxyURL: "/ESS/survey/GetSurveyRecordById"
        //},
        dataSource: dataSource,
        scrollable: true,
        sortable: true,
        pageable: true,
        columns: arrColumns
    }).data("kendoGrid");
}

model.data.totalScore = ko.observable();
function showDetailFill(ids) {
    $("#detailFillModal").modal("show");
    displayLoading("#AnswerSurvey", true);
    ajaxPost("/ESS/Survey/GetListAnswer", { Id: ids }, function (res) {
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

function downloadExcel(e) {
    let fileLocation = $(e).attr("file");
    //console.log("file: ",fileLocation);
    window.location = fileLocation;
    return false;

    //var element = document.createElement('a');
    //element.setAttribute('href', fileLocation);
    //element.setAttribute('download', fileLocation);

    //element.style.display = 'none';
    //document.body.appendChild(element);

    //element.click();

    //document.body.removeChild(element);
}

let _defaultStart = undefined,
  _defaultEnd = undefined;
let _selectedStart = undefined,
    _selectedEnd = undefined;

model.render.schedule = (start, end) => {
    let $el = $("#survey-schedule");

    _defaultStart = start;
    _defaultEnd = end;

  if (!!$el) {
    let $grid = $el.getKendoDateRangePicker();

    if (!!$grid) {
      $grid.destroy();
    }

    $el.kendoDateRangePicker({
      messages: {
        startLabel: "Schedule Start",
        endLabel: "Schedule Finish",
      },
      format: "dddd, MMMM dd, yyyy",
      range: {
        start: start,
        end: end,
      },
      min: new Date(),
      change(e) {
        let range = this.range();
        _selectedStart = range.start;
        _selectedEnd = range.end;
      },
      close(e) {
        setTimeout(async () => {
          if (!_selectedStart) {
            await swalWarning(
              "Survey",
              "Survey schedule start cannot be empty"
            );
            e.sender.open();
          } else if (!_selectedEnd) {
            await swalWarning(
              "Survey",
              "Survey schedule finish cannot be empty"
            );
            e.sender.open();
          }
        }, 500);
      },
    });
  }
};

model.action = {};

model.action.refreshSurvey = function (uiOnly = false) {
  var $grid = $("#gridSurvey").data("kendoGrid");
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

model.action.publish = async function (uid) {
    let dataGrid = $("#gridSurvey").data("kendoGrid").dataSource.getByUid(uid);
    if (!dataGrid.DBId) {
        swalWarning("Publish", "Please make setting first.");
        return false;
    }
    if (!!dataGrid) {
    var dialogTitle = "Survey";
    var result = await swalConfirm(
        dialogTitle,
        `Are you sure publishing survey "${dataGrid.Title}" ?`
    );    
    if (result.value) {
        isLoading(true);
        try {
        ajax(
            `/ESS/Survey/Publish/${dataGrid.Id}`, "GET",{},
            function (data) {
            model.action.refreshSurvey()
                isLoading(false);
                if (data.StatusCode == 200) {
                    swalSuccess(dialogTitle, data.Message);
                } else {
                    swalError(dialogTitle, data.Message);
                }
            },
            function (data) {
                isLoading(false);
                swalError(dialogTitle, data.Message);
            }
        );
        } catch (e) {
            console.error(e);
            isLoading(false);
        }
    }
    } else {
        swalAlert(dialogTitle, "unable to find survey :" + uid);
    }
};

model.action.unpublish = async function (uid) {
  let dataGrid = $("#gridSurvey").data("kendoGrid").dataSource.getByUid(uid);
  if (!!dataGrid) {
    var dialogTitle = "Survey";
    var result = await swalConfirm(
      dialogTitle,
      `Are you sure un-publishing survey "${dataGrid.Title}" ?`
    );    
    if (result.value) {
      isLoading(true);
      try {
        ajax(
          `/ESS/Survey/Unpublish/${dataGrid.Id}`, "GET",{},
          function (data) {
            model.action.refreshSurvey()
            isLoading(false);
            if (data.StatusCode == 200) {
              swalSuccess(dialogTitle, data.Message);
            } else {
              swalError(dialogTitle, data.Message);
            }
          },
          function (data) {
            isLoading(false);
            swalError(dialogTitle, data.Message);
          }
        );
      } catch (e) {
        console.error(e);
        isLoading(false);
      }
    }
  } else {
    swalAlert(dialogTitle, "unable to find survey :" + uid);
  }
};

model.action.surveysummary = async function (id) {
    let response = await ajax("/ESS/Survey/SummaryById/" + id, "GET");
    if (response.StatusCode == 200) {
        return response.Data || [];
    }
    return [];
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

model.action.saveSurvey = async function () {
  var dialogTitle = "Survey";
  var result = await swalConfirm(dialogTitle, "Are you sure to save survey?");
  if (result.value) {
    try {
      model.is.buttonLoading(true);
      isLoading(true);
      var data = ko.mapping.toJS(model.data.survey());

      if (!data.Title) {
        model.is.buttonLoading(false);
        isLoading(false);
        return swalAlert(dialogTitle, "Survey title cannot be empty");
      }

      var schedule = $("#survey-schedule").getKendoDateRangePicker();
      if (schedule) {
        let range = schedule.range();
        if (!range.start) {
          model.is.buttonLoading(false);
          isLoading(false);
          return swalAlert(dialogTitle, "Schedule start cannot be empty");
        }
        data.Schedule.Start = range.start;

        if (!range.end) {
          model.is.buttonLoading(false);
          isLoading(false);
          return swalAlert(dialogTitle, "Schedule finish cannot be empty");
        }
        data.Schedule.Finish = range.end;
      } else {
        model.is.buttonLoading(false);
        isLoading(false);
        return swalAlert(dialogTitle, "Schedule is undefined");
      }

      if (!data.Recurrent) {
        model.is.buttonLoading(false);
        isLoading(false);
        return swalAlert(dialogTitle, "Recurrent type cannot be empty");
      }

      if (!data.ParticipantType) {
        model.is.buttonLoading(false);
        isLoading(false);
        return swalAlert(dialogTitle, "Participant type cannot be empty");
      }

        // bisa diganti mas
        // send command to odoo to save
        try {
            let odooResult = await model.action.sendOdooMessageAndWaitResult(
                "save"
            );
            //// Do something from odooResult;
            data.OdooID = odooResult.param.SurveyID;
        } catch (e) {
            console.warn("###", e);
        }

        data.Participants = [];
        let part = $("#participants").data("kendoMultiSelect");
        part.dataItems().forEach(function (d) {
            data.Participants.push(d.EmployeeID)
        });        
        data.Departments = [];
        if (data.ParticipantType == 2) {
            let dp = $("#departments").data("kendoMultiSelect");
            let listEmp = [];
            let listDept = dp.dataItems();
            listDept.map(function (x) {
                console.log(x)
                data.Departments.push(x.name);
                x.employee.map(function (y) {
                    listEmp.push(y.Id)
                });
            });
            data.Participants = listEmp;
        }
        
        console.log("param: ", data);
        //return false;
        try {
            ajaxPost(
                "/ESS/Survey/Save",
                data,
            function (data) {
                model.is.buttonLoading(false);
                isLoading(false);
                if (data.StatusCode == 200) {
                    swalSuccess(dialogTitle, data.Message);
                    location.href = "/ESS/Survey";
                } else {
                    swalError(dialogTitle, data.Message);
                }
                },
                function (data) {
                    isLoading(false);
                    model.is.buttonLoading(false);
                    swalError(dialogTitle, data.Message);
                }
            );
        } catch (e) {
            model.is.buttonLoading(false);
            isLoading(false);
        }
    } catch (e) {
      model.is.buttonLoading(false);
      isLoading(false);
      console.error(e);
    }
  }
};

model.action.sendOdooMessage = function (type, param) {
  console.log("sending message");
  var iframe = document.getElementById("survey-form-container");
  var uid = kendo.guid();
  iframe.contentWindow.postMessage({ type: type, param: param, uid: uid }, "*");
  return uid;
};

let _resultOdoo = {};
model.action.sendOdooMessageAndWaitResult = async function (
  type,
  param,
  timeout = 10
) {
  var uid = kendo.guid();
  //console.log("BBBBBBB");

  try {
    console.log("???", uid);
    await Promise.all([
      new Promise(function (resolve, reject) {
        try {
          var iframe = document.getElementById("survey-form-container");
          iframe.contentWindow.postMessage(
            { type: type, param: param, uid: uid },
            "*"
          );
          resolve(true);
        } catch (e) {
          reject(e);
        }
      }),
      new Promise(async function (resolve, reject) {
        try {
          var startDate = new Date();
          while (true) {
            if (_resultOdoo[uid]) break;

            let endDate = new Date();
            let seconds = (endDate.getTime() - startDate.getTime()) / 1000;
            if (seconds >= timeout) {
              reject("timeout");
              return;
            }

            await delay(50);
          }

          resolve(true);
        } catch (e) {
          reject(e);
        }
      }),
    ]);
    return _resultOdoo[uid];
  } catch (error) {
    return error;
  }
};

model.on.documentSelect = (e) => {
  console.log("select file :)");
};

model.on.renderForm = (e) => {
    let data = ko.toJS(model.data.survey());
    model.render.schedule(data.Schedule.Start, data.Schedule.Finish);
};

model.on.scheduleChange = (e) => {
  let range = this.range();
  console.log(range);
};

model.init.odooMessageListener = function () {
  let receiverFunction = function (e) {
    console.log("✔ odoo message received :", e);
    let type = e.data.type,
      param = e.data.param,
      uid = e.data.uid;
    _resultOdoo[uid] = e.data;
    switch (type) {
      default:
        console.log(param);
        break;
    }
  };

  removeEventListener("message", receiverFunction, false);
  addEventListener("message", receiverFunction, false);
};

model.get.employees = async function () {
  let response = await ajax("/ESS/Employee/GetEmployees", "GET");
  if (response.StatusCode == 200) {
    return response.Data || [];
  }
  return [];
}

model.get.departments = async function () {
    let response = await ajax("/ESS/Employee/GetDepartments", "GET");
    if (response.StatusCode == 200) {
        //console.log(response.Data);
        response.Data.map(function (emp) {
            model.data.employees.push({
                EmployeeID: emp.Id,
                EmployeeName: emp.EmployeeName,
                Department: emp.DepartmentName
            });
        })
        let departments = _.chain(response.Data)
            .groupBy("DepartmentName")
            .map((value, key) => ({ name: key, employee: value }))
            .value();
        return departments || [];
    }
    return [];
    //let response = await ajax("/assets/data/departments.json", "GET");
    //if (response.statusCode == 200) {
    //    console.log(response.data);
    //    let departments = _.chain(response.data)
    //        .groupBy("name")
    //        .map((value, key) => ({ name: key, employee: value }))
    //        .value();
    //    return departments || [];
    //}
    //return [];
}

model.get.surveysummary = async function (surveyid) {
    let param = { Id: surveyid, Picker: model.data.kendodateval()}
    let res = await ajaxPost("/ESS/Survey/SummaryId", param);
    console.log('SummaryId', res)
    if (res.StatusCode === 200 && res.Data.Id != null) {
        model.data.surveysummary(res.Data)
        switch (model.data.surveysummary().Recurrent) {
            case 1:
                model.data.kendoenabled(false)
                break;
            case 2:
                model.data.kendoenabled(true)
                model.data.kendoformat('dd MM yyyy')
                model.data.kendodepth('month')
                break;
            case 3:
                model.data.ispickerweekly(true)
                model.data.kendoenabled(true)
                model.data.kendoformat('dd MM yyyy')
                model.data.kendodepth('month')
                break;
            case 4:
                model.data.kendoenabled(true)
                model.data.kendoformat('MMM yyyy')
                model.data.kendodepth('year')
                break;
            case 5:
                model.data.kendoenabled(true)
                model.data.kendoformat('yyyy')
                model.data.kendodepth('decade')
                break;
        }
        // set startdate and endDate
        model.data.StartDate(model.data.surveysummary().Schedule.Start)
        model.data.EndDate(model.data.surveysummary().Schedule.Finish)
    }
    return res
}

model.render.surveysummary = async (id) => {
    var self = model;
    return new Promise(async function (resolve, reject) {
        try {
            kendo.ui.progress($("#summaryContainer"), true);
            var res = await self.get.surveysummary(id);
            if (!res) reject(false);
            if (res.StatusCode == 200 && res.Data.Id != null ) {
                let p = (model.data.surveysummary().TotalParticipantsTakenSurvey / model.data.surveysummary().TotalParticipants) * 100;
                model.data.percentsurvey(p)
                model.render.summaryDatePicker()
            } else {
                console.log("failed")
            }
            resolve(true);
        } catch (e) {
            reject(false);
        } finally {
            kendo.ui.progress($("#summaryContainer"), false);
        }
    })
}

model.render.gridparticipant = (surveyid) => {
    let $el = $("#gridParticipant")
    if ($el) {
        let $grid = $el.getKendoGrid();
        if (!!$grid) {
            $grid.destroy();
        }
        $el.kendoGrid({
            dataSource: {
                transport: {
                    read: {
                        url: "/ESS/Survey/Participant",
                        dataType: "json",
                        type: "POST",
                        contentType: "application/json",
                    },
                    parameterMap: function (data, type) {
                        data.Id = surveyid
                        data.Picker = model.data.kendodateval()
                        return JSON.stringify(data);
                    }
                },
                schema: {
                    data: function (res) {
                        if (res.StatusCode !== 200 ) {
                            swalFatal("Fatal Error", `Error occured while fetching participant request(s)\n${res.Message}`)
                            return []
                        }
                        return res.Data.Participants || [];
                    },
                    total: "Total",
                },
                error: function (e) {
                    swalFatal("Fatal Error", `Error occured while fetching participant request(s)\n${e.xhr.responseText}`)
                }
            },
            pageable: {
                previousNext: false,
                info: false,
                numeric: false,
                refresh: true
            },
            noRecords: {
                template: "No data"
            },
            columns: [
                {
                    field: "EmployeeID",
                    title: "Employee ID",
                    template: function (d) {
                        return d.EmployeeID;
                    }
                },
                {
                    field: "EmployeeName",
                    title: "Employee Name",
                    template: function (d) {
                        return d.EmployeeName;
                    }
                },
                {
                  field: "Department",
                  title: "Department",
                  template: function (d) {
                      return d.Department || '-';
                  }
              },
                {
                    field: "Done",
                    title: "Done",
                    template: function (d) {
                        if (d.Done) {
                            return `<i class="fa mdi mdi-check-circle text-success"></i>`
                        } else {
                            return `<i class="fa mdi mdi-check-circle text-secondary"></i>`
                        }

                    }
                }
            ]
        })
    }
}

model.init.survey = async () => {
    let self = model;

    self.render.gridSurvey();

    let recurrent = _.clone(model.proto.SurveyRecurrent);
    for (let i in recurrent)
        self.list.surveyRecurrences.push({ text: recurrent[i], value: i });

    let participantTypes = _.clone(model.proto.ParticipantType);
    for (let i in participantTypes)
        self.list.participantTypes.push({ text: participantTypes[i], value: i });

    let start = new Date();
    let end = moment().add(1, "months").toDate();
    model.render.schedule(start, end);

    await model.get.dataAll();
    
    self.is.buttonLoading(false);
};

model.get.dataAll = async function () {
    setTimeout(async function () {
        await Promise.all([
            //new Promise(async (resolve) => {
            //    let employees = await model.get.employees();
            //    //console.log('list employee', employees);
            //    model.data.employees(employees);
            //    resolve(true);
            //}),
            new Promise(async (resolve) => {
                let departments = await model.get.departments();
                //console.log(departments)
                model.list.departments(departments);
                resolve(true);
            }),
        ]);
    });
}

model.init.form = () => {
    let self = model;
    model.init.odooMessageListener();

    let recurrent = _.clone(model.proto.SurveyRecurrent);
    for (let i in recurrent)
        self.list.surveyRecurrences.push({ text: recurrent[i], value: i });
    
    let participantTypes = _.clone(model.proto.ParticipantType);
    for (let i in participantTypes)
        self.list.participantTypes.push({ text: participantTypes[i], value: i });

    let start = new Date();
    let end = moment().add(1, "months").toDate();
    model.render.schedule(start, end);

    setTimeout(async function () {
        let employees = await self.get.employees()
        //console.log('response', employees)
        model.data.employees(employees)
    })

    model.is.buttonLoading(false)
};

model.init.summary = () => {
    let self = model
    let recurrent = _.clone(model.proto.SurveyRecurrent);
    for (let i in recurrent)
        self.list.surveyRecurrences.push({ text: recurrent[i], value: i });

    let rec = []
    for (let i in recurrent)
        rec[i] = recurrent[i]
    self.list.surveyRecurrence(rec)

    self.render.surveysummary(model.data.surveyid())
    self.render.gridparticipant(model.data.surveyid())
}

model.action.onChange = function () {
    switch (model.data.surveysummary().Recurrent) {
        case 1:
        case 2:
            model.data.kendodateval(this.value().getDay())
            break;
        case 3:
            let DateWeek = new Date(moment(this.value()));
            model.data.kendodateval(DateWeek.getWeek());
            break;
        case 4:
            model.data.kendodateval(this.value().getMonth() + 1)
            break;
        case 5:
            model.data.kendodateval(this.value().getFullYear())
            break;
        default:
            model.data.kendodateval(this.value().getDay())
            break;
    }
    model.render.surveysummary(model.data.surveyid())
    model.render.gridparticipant(model.data.surveyid())
}

model.render.summaryDatePicker = () => {
    let filter = $("#summaryDatePicker").getKendoDateRangePicker();
    if (!!filter) {
        filter.destroy();
    }
    $("#summaryDatePicker").kendoDatePicker({
        min: model.data.StartDate(),
        max: model.data.EndDate(),
        value: model.data.StartDate(),
        format: model.data.kendoformat(),
        start: model.data.kendodepth(),
        depth: model.data.kendodepth(),
        enabled: model.data.kendoenabled(),
        weekNumber: model.data.ispickerweekly(),
        change: model.action.onChange
    }).data('kendoDatePicker').enable(model.data.kendoenabled())
}

model.action.detailSurvey = (uid) => {
    data = $("#gridSurvey").data("kendoGrid").dataSource.getByUid(uid);
    if (!!data) {
        window.open(`/ESS/Survey/Summary/${data.Id}`);
    }
}

Date.prototype.getWeek = function () {
    var onejan = new Date(this.getFullYear(), 0, 1);
    var today = new Date(this.getFullYear(), this.getMonth(), this.getDate());
    var dayOfYear = ((today - onejan + 86400000) / 86400000);
    return Math.ceil(dayOfYear / 7)
}

let currentId = 1;
model.action.onDataBound = function () {
  $('.k-multiselect .k-input').unbind('keyup');
  $('.k-multiselect .k-input').on('keyup', model.action.onClickEnter);
}
model.action.onClickEnter = function (e) {
  if (e.keyCode === 13) {
    var widget = $('#participants').getKendoMultiSelect();
    var dataSource = widget.dataSource;
    var input = $('.k-multiselect .k-input');
    var value = input.val().trim();
    if (!value || value.length === 0) {
      return;
    }
    var newItem = {
      ProductID: currentId++,
      ProductName: value
    };
    dataSource.add(newItem);
    var newValue = newItem.ProductID;
    widget.value(widget.value().concat([newValue]));
  }
}

model.action.setting = function (uid) {
    let data = $("#gridSurvey").data("kendoGrid").dataSource.getByUid(uid);
    console.log('setting:', data);
    //window.location.replace("/ESS/Survey/New");
    data.Description = stripHtml(data.Description);
    
    if (data.DBId == null) {
        data.OdooID = data.Id;
        data.Id = null;
    } else {
        data.OdooID = data.Id;
        data.Id = data.DBId;        
    }

    //console.log("-->", data)    
    model.data.survey(model.newSurvey(data));

    let daterangepicker = $("#survey-schedule").data("kendoDateRangePicker");
    //let val = daterangepicker.getKendoDateRangePicker()
    if (data.Schedule) {
        let range = { start: new Date(data.Schedule.Start), end: new Date(data.Schedule.Finish) };
        daterangepicker.range(range);
    }

    if (data.ParticipantType == 2) {
        $("#departments").data("kendoMultiSelect").value(data.Departments);
    }
    if (data.ParticipantType == 3) {
        $("#participants").data("kendoMultiSelect").value(data.Participants);
    }
    
    //model.data.survey().Title(data.Title);
    //model.data.survey().Description(data.Description);
    $("#PublicLink").val(data.SurveyUrl.split("?")[0]);
    $("#settingModal").modal('show');
}

model.action.changeType = function () {
    //$("#participants").data("kendoMultiSelect").value([])
    //$("#departments").data("kendoMultiSelect").value([])
}

model.action.testSurvey = function (uid) {
    let data = $("#gridSurvey").data("kendoGrid").dataSource.getByUid(uid);
    let url = data.SurveyUrl;
    window.open(url, '_blank');
}

model.action.detailHistory = function () {
    location.href = "/ESS/Dashboard"
}

model.init.surveySummary = function () {
    model.render.gridSurveySummary();
}

model.render.gridSurveySummary = function () {
    let $el = $("#gridSurveySummary");
    if (!!$el) {
        let $grid = $el.getKendoGrid();

        if (!!$grid) {
            $grid.destroy();
        }

        $el.kendoGrid({
            dataSource: {
                transport: {
                    read: {
                        url: "/ESS/survey/GetSurveyOdoo",  // /ESS/Survey/GetRange
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
                        //model.is.buttonLoading(false);
                        if (res.error) {
                            swalFatal(
                                "Fatal Error",
                                `Error occured while fetching survey(s)\n${res.Message}`
                            );
                            return [];
                        }
                        model.list.summary(res)
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
            detailTemplate: kendo.template($("#template").html()),
            detailInit: detailSummary,
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
                });

                var items = e.sender.items();
                items.each(function () {
                    var row = $(this);
                    var dataItem = e.sender.dataItem(row);
                    //console.log("bound:", dataItem)
                    //if (!dataItem.hasChildren) {
                    //    row.find(".k-hierarchy-cell").html("");
                    //}
                    if (dataItem.answer_count == 0) {
                        row.find(".k-hierarchy-cell").html("");
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
                    field: "Id",
                    title: "Id",
                    width: 70,
                },
                {
                    headerAttributes: {
                        class: "text-center",
                    },
                    field: "Title",
                    title: "Title",
                    template: function (d) {
                        if (d.Schedule) {
                            return `${d.Title}<br />${moment(d.Schedule.Start).format('DD MMM YYYY')} Until ${moment(d.Schedule.End).format('DD MMM YYYY')}`;
                        } else {
                            return `${d.Title}<br /><span class="text-danger">Not yet setting</span>`;
                        }
                        
                    },
                    width: 280
                },
                {
                    headerAttributes: {
                        class: "text-center",
                    },
                    field: "Description",
                    title: "Description",
                    template: function (d) {
                        return stripHtml(d.Description)
                    },
                },
                {
                    headerAttributes: {
                        class: "text-center",
                    },
                    attributes: {
                        class: "text-center",
                    },
                    field: "State",
                    title: "Status",
                    width: 90,
                    template: function (d) {
                        return d.State == "open" ? `<span class="text-success">Open</span>` : `<span class="text-danger">Close</span>`;
                    }
                },
                //{
                //    headerAttributes: {
                //        class: "text-center",
                //    },
                //    attributes: {
                //        class: "text-center",
                //    },
                //    title: "Participant / Answered <br /> Today",
                //    width: 190,
                //    template: function (d) {
                //        return `${d.Participants.length} / ${d.TodayAnswers}`
                //    }
                //},
            ],
        });
    }    
};

function detailSummary(row) {
    console.log("row:", row)
    let dateInMonth = getDaysInMonth(parseInt(moment().format("MM"))-1, parseInt(moment().format("YYYY")));
    let arrColumns = [
        {
            field: "Tanggal",
            title: "Tanggal",
            width: 180
        }, 
    ];

    dateInMonth.forEach(function (dt) {
        //console.log(dt);
        arrColumns.push({
            headerAttributes: {
                class: "text-center",
            },
            attributes: {
                class: "text-center",
            },
            title: moment(dt).format("DD"),
            width: 85,
            template: function (d) {
                //console.log("-->>>", d)
                return d["D" + moment(dt).format("DD")] != "" ? `${d["D" + moment(dt).format("DD")]}` : "";
            }
        })
    });

    var detailRow = row.detailRow;
    //console.log("Summary detail:", detailRow);
    //detailRow.find(".detailTabstrip").kendoTabStrip({
    //    animation: {
    //        open: { effects: "fadeIn" }
    //    }
    //});

    displayLoading(".detailSumary", true);
    let param = {
        Id: row.data.Id
    };
    ajaxPost("/ESS/survey/GetSumaryById", param, function (res) {
        console.log("result:", res.Grid)
        if (res.StatusCode == 400 || res.Grid.length == 0) {
            //alert("no data")
            displayLoading(".detailSumary", false);
            return false;
        }
        generateChart(detailRow.find(".chartSumary"), res.Grid, row);
        var grid = detailRow.find(".detailSumary").kendoGrid({
            toolbar: kendo.template($("#toolbarSummary").html())(row.data),
            //toolbar: [
            //    {
            //        template: '<button class="btn btn-dark btn-sm" onclick="model.action.ExportToExcel()">Export</button>',
            //    }
            //],
            pdf: {
                allPages: true,
                fileName: "Survey List.pdf",
                proxyURL: "/ESS/survey/GetSumaryById"
            },
            dataSource: res.Grid,
            scrollable: false,
            sortable: true,
            //pageable: true,
            dataBinding: function () {
                //console.log("finish")
            },
            dataBound: function (col) {
                for (var i = 0; i < this.columns.length; i++) {
                    //if (i > 3) {
                    //    this.columns.title = "X-";
                    //}
                    this.autoFitColumn(i);
                }
            },
            columns: arrColumns
        }).data("kendoGrid");

        detailRow.find(".detailSumary").data("kendoGrid").wrapper.find(".k-grid-header-wrap").off("scroll.kendoGrid");

        $(".excel-" + row.data.Id).attr("file", res.Excel).show();
        $(".pdf-" + row.data.Id).attr("file", res.PDF).show();
    });
}

function generateChart(el, data, row) {
    //console.log("chart:", data)
    let i = 0;
    let categories = [];
    let totalPartisipan = [];
    let totalIsian = [];
    for (var k in data[0]) {
        if (data[0].hasOwnProperty(k) && i >= 1) {
            //user[k] = data[k];
            categories.push(k.slice(-2))
            totalPartisipan.push(data[0][k]);
            totalIsian.push(data[1][k]);
        }
        i++;
    }
    el.kendoChart({
        title: {
            text: row.data.Title
        },
        legend: {
            position: "bottom"
        },
        seriesDefaults: {
            type: "line"
        },
        series: [{
            name: "Partisipan Survey",
            data: totalPartisipan
        }, {
            name: "Mengisi Survey",
            data: totalIsian,
        }],
        valueAxis: {
            labels: {
                //format: "{0}%"
                format: "{0}"
            }
        },
        categoryAxis: {
            categories: categories
        }
    });

    displayLoading(".detailSumary", false);
}

model.action.ExportToExcel = function (Id) {
    displayLoading(".detailSumary", true);
    ajaxPost("/ESS/Survey/ExportSummary?id="+Id, {}, function (res) {
        displayLoading(".detailSumary", false);
        //console.log(res);
        model.is.buttonExport(false);
        if (res.StatusCode == 200) {
            swalSuccess("Export", "Export Success");
            $(".excel-" + Id).attr("file", res.Data.Excel).show();
            $(".pdf-" + Id).attr("file", res.Data.PDF).show();
        }
    });
}

model.action.showExcel = function (e) {
    let fileLocation = $(e).attr("file");
    ////$("#spreadsheet").kendoSpreadsheet();
    //var spreadsheet = $("#spreadsheet").data("kendoSpreadsheet");
    //spreadsheet.fromFile(fileLocation);

    $("#Excel-form").modal("show");
    window.location = fileLocation;
    return false;
    //var element = document.createElement('a');
    //element.setAttribute('href', fileLocation);
    //element.setAttribute('download', fileLocation);

    //element.style.display = 'none';
    //document.body.appendChild(element);

    //element.click();

    //document.body.removeChild(element);
}

model.action.showPDF = function (e) {
    let fileLocation = $(e).attr("file");
    //console.log("file", fileLocation);
    //$("#viewPDF").kendoPDFViewer({
    //    pdfjsProcessing: {
    //        file: fileLocation
    //    },
    //    width: "100%",
    //    height: 600
    //});
    var pdfViewer = $("#pdfViewer").data("kendoPDFViewer");
    if (!pdfViewer) {
        pdfViewer = $("#pdfViewer").kendoPDFViewer({
            pdfjsProcessing: {
                file: ""
            },
            width: "100%",
            height: 600
        }).data("kendoPDFViewer");
    }

    pdfViewer.fromFile(fileLocation);

    $("#PDF-form").modal("show");
}

function stripHtml(html) {
    let tmp = document.createElement("DIV");
    tmp.innerHTML = html;
    return tmp.textContent || tmp.innerText || "";
}

function getDaysInMonth(month, year) {
    var date = new Date(year, month, 1);
    var days = [];
    while (date.getMonth() === month) {
        days.push(new Date(date));
        date.setDate(date.getDate() + 1);
    }
    return days;
}