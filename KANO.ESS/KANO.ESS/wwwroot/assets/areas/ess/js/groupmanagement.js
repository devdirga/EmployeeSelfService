var ignoreCheckAll = false;
model.checkAll = function (id) {
    if (ignoreCheckAll) return;
    var val = $("#cbAll_" + id).prop("checked");
    $("#row_" + id + " .cb-perm").prop("checked", val ? true : false);
}
model.checkIdv = function (id) {
    var checkedAll = true;
    $("#row_" + id + " .cb-perm").each(() => {
        if (!$(this).prop("checked"))
        checkedAll = false;
    });
    ignoreCheckAll = true;
    $("#cbAll_" + id).prop("checked", checkedAll);
    ignoreCheckAll = false;
}
model.getGrant = function (id) {
    var gr = model.form.Detail.Grant();
    var f = _.find(gr, (a) => a.PageID() == id);
    return f ? f.Actions() : 0;
}
model.getSA = function (id) {
    var gr = model.form.Detail.Grant();
    var f = _.find(gr, (a) => a.PageID() == id);
    var sa = f ? f.SpecialActions() : [];
    return sa;
}
model.initPermission = function () {
    var data = model.form.PageDetail();
    for (var k in data) {
        var ele = data[k];
        var grant = model.getGrant(ele.Id);
        $("#cbAll_" + ele.Id).prop("checked", grant == 255);
        $("#cbView_" + ele.Id).prop("checked", (grant & 2) > 0);
        $("#cbCreate_" + ele.Id).prop("checked", (grant & 1) > 0);
        $("#cbUpdate_" + ele.Id).prop("checked", (grant & 4) > 0);
        $("#cbDelete_" + ele.Id).prop("checked", (grant & 8) > 0);
        $("#cbUpload_" + ele.Id).prop("checked", (grant & 16) > 0);
        $("#cbDownload_" + ele.Id).prop("checked", (grant & 32) > 0);
        var sa = model.getSA(ele.Id);
        $("#row_" + ele.Id + " .cbSA").each((i, e) => {
            $(e).prop("checked", sa.includes($(e).attr("data-sa")));
        });
    }
}

model.form = {
    EmptyData: model.proto.EmptyData,
    Data: model.proto.Data,
    Detail: model.proto.Detail,
    PageDetail: model.proto.PageDetail,
    Save: async function () {
        var validator = $("#form-Group").kendoValidator().data("kendoValidator");
        var param = ko.mapping.toJS(model.form.Detail);
        if (!validator.validate()) return;
        let confirmResult = await swalConfirm("Group", `Are you sure saving group ${param.Name} ?`);
        if (confirmResult.value) {        
            isLoading(true);
            

            param.Grant = [];
            var data = model.form.PageDetail();
            for (var k in data) {
                var pg = data[k];
                var obj = {
                    PageID: pg.Id,
                    PageCode: pg.PageCode,
                    PageTitle: pg.Title,
                    Actions: 0,
                    SpecialActions: []
                };
                if ($("#cbView_" + pg.Id).prop("checked")) obj.Actions += 2;
                if ($("#cbCreate_" + pg.Id).prop("checked")) obj.Actions += 1;
                if ($("#cbUpdate_" + pg.Id).prop("checked")) obj.Actions += 4;
                if ($("#cbDelete_" + pg.Id).prop("checked")) obj.Actions += 8;
                if ($("#cbUpload_" + pg.Id).prop("checked")) obj.Actions += 16;
                if ($("#cbDownload_" + pg.Id).prop("checked")) obj.Actions += 32;
                if ($("#cbAll_" + pg.Id).prop("checked")) obj.Actions = 255;
                $("#row_" + pg.Id + " .cbSA").each((i, e) => {
                    if ($(e).prop("checked"))
                    obj.SpecialActions.push($(e).attr("data-sa"))
                });
                param.Grant.push(obj);
            }        

            const url = URL_SAVE_USER_MANAGEMENT
            ajaxPost(url, param, function (res) {
                isLoading(false);
                if (res.StatusCode == 200) {
                    swalSuccess("Group", `Group has been saved successfully`);
                    $("#modalAddGroup").modal('hide');
                    model.form.Get();
                } 
            }, function (data) {
                isLoading(false);
                swalFatal("Group", data.Message);
                $("#modalAddGroup").modal("show");
            });
        }
    },
    Add: function () {
        var validator = $("#form-Group").kendoValidator().data("kendoValidator");
        //validator.hideMessages();
        $("#modalAddGroup").modal("show");
        ko.mapping.fromJS(model.form.EmptyData, model.form.Detail);
        model.initPermission();
    },
    GetPage: function () {
        const url = URL_GET_PAGE;
        let param = {};
        ajax(url,"GET", param, function (res) {
        //ajaxPost('@Url.Action("FindTiered", "PageManagement")', {}, function (res) {
            if (res.StatusCode == 200) {
                for (var k in res.Data) {
                    var data = res.Data[k];
                    var k = "";
                    for (var i = 0; i < data.Level; i++)
                    //k += "&nbsp;&nbsp;&nbsp;&nbsp;"
                    if (data.Level > 0)
                    k += "►&nbsp;";
                    data.BuiltTitle = k + data.Title;
                }
                model.form.PageDetail(res.Data);
            } else {
                model.form.PageDetail([]);
            }
        });
    },
    Get: function () {
        //ajaxPost('@Url.Action("Find")', {}, function (res) {
            //  model.form.Data(res.Data);
            //setTimeout(function () {
                //  model.form.InitGrid();
            //   }, 300)
        // });
        model.form.InitGrid();

        const url = URL_GET_GROUP_MANAGEMENT
        ajax(url,"GET", {}, function (res) {
            model.form.Data(ko.mapping.toJS(res.Data));            
        });
    },
    Edit: function (id) {
        var validator = $("#form-Group").kendoValidator().data("kendoValidator");
        //validator.hideMessages();
        model.initPermission();

        var d = _.find(model.form.Data(), (a) => a.Id == id);
        if (d) {
            ko.mapping.fromJS(d, model.form.Detail);
        }
        $("#modalAddGroup").modal("show");
        model.initPermission();
    },
    DeleteData: async function (id) {
        let confirmResult = await swalConfirm("group", `Are you sure deleting this data ?`);
        if (confirmResult.value) {  
        }        
    },
    InitGrid: function () {
        $('#GridGroups').kendoGrid({
            dataSource: {
                transport: {
                    read: {
                        url: URL_GET_GROUP_MANAGEMENT,
                        dataType: "json",
                        type: "POST",
                        contentType: "application/json",
                    },
                    parameterMap: function(data, type) {
                        //viewModel.isLoading(true)
                        return JSON.stringify(data);
                    }
                },
                schema: {
                    data: function (res) {
                        if (res.StatusCode !== 200 && res.Status !== '') {
                            swalFatal("Fatal Error", `Error occured while fetching group(s)\n${res.Message}`)
                            return []
                        }
                        return res.Data || [];
                    },
                    total: "Total",
                },
                error: function (e) {
                    swalFatal("Fatal Error", `Error occured while fetching group(s)\n${e.xhr.responseText}`)
                },
                type: "json",
            },
            pageable: {
                previousNext: false,
                info: false,
                numeric: false,
                refresh: true
            },
            sortable: true,
            //pageable: {
            //    refresh: false,
            //    pageSizes: 10,
            //    buttonCount: 5
            //},
            noRecords: {
                template: "No group data available."
            },
            columns: [
                // {
                //     title: "No",
                //     template: "#= ++record #",
                //     editable: false,
                //     width: 70
                // },
                {
                    field: "Name",
                    title: "Group Name",
                    width: 200,
                },
                {
                    field: "Enable",
                    width: 90,
                    title: "Enable",
                    template: function (e) {
                        return (!!e.Enable) ? "Yes" : "No";
                    },
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
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
                    width: 150,
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
                    width: 250,
                },
                {
                    title: "",
                    width: 120,
                    attributes: {
                        class: "text-center"
                    },
                    template: function (e) {
                        var btnDelete = ''; //"<button class='btn btn-xs btn-danger' data-tooltipster='Remove' style='min-width: 3px;' onclick='model.form.DeleteData(\"" + e.Id + "\")'><i class='fa fa-trash'></i></button>"
                        var btnEdit = "<button class='btn btn-xs btn-outline-warning' data-tooltipster='Update' onclick='model.form.Edit(\"" + e.Id + "\")' style='min-width: 3px;'><i class='mdi mdi-pencil'></i></button>"

                        return btnEdit + '&nbsp' + btnDelete
                    },
                }
            ],
            dataBinding: function () {
                record = (this.dataSource.page() - 1) * this.dataSource.pageSize();
            },
        });
    },
    openPage: function (pageName, elmnt, color) {
        // Hide all elements with class="tabcontent" by default */
        var i, tabcontent, tablinks;
        tabcontent = document.getElementsByClassName("tabcontent");
                for (i = 0; i < tabcontent.length; i++) {
            tabcontent[i].style.display = "none";
        }

        // Remove the background color of all tablinks/buttons
        tablinks = document.getElementsByClassName("tablink");
                for (i = 0; i < tablinks.length; i++) {
            tablinks[i].style.backgroundColor = "";
        }

        // Show the specific tab content
        document.getElementById(pageName).style.display = "block";

        // Add the specific color to the button used to open the tab content
        elmnt.style.backgroundColor = color;
    }
}

//$(document).ready(function () {
//    model.form.Get();
//    model.form.GetPage();
//});