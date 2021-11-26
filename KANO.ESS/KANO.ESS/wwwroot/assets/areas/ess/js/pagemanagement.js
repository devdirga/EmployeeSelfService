model.form = {
    action: ko.observable("detail"),
    page: ko.observable("grid"),
    DetailData: ko.mapping.fromJS(model.proto.DetailData),
    Detail: model.proto.Detail,
    Data: ko.observable(),
    InitParentMenu: function (ParentId) {
        $("#ParentId").kendoDropDownList({
            dataSource: model.form.Data(),
            dataTextField: "Title",
            dataValueField: "PageCode",
            optionLabel: "Select Parent",
        });
        setTimeout(function () {
            $("#ParentId").data("kendoDropDownList").value(ParentId);
        }, 300);
    },
    Add: function () {
        model.form.action("edit");
        $("#modalAddMenu").modal("show");
        $("#modalAddMenu input:text").val("");
        $("#modalAddMenu input:checkbox").removeAttr('checked');
        var newData = ko.mapping.toJS(model.form.Detail)
        //console.log(newData)
        model.form.DetailData(newData);
        model.form.InitParentMenu("");
    },
    GetDataGrid: function () {
        const url = URL_GET_PAGE
        ajax(url, "GET", {}, function (res) {
            
        });
    },
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
        $('#gridMain').html("");
        $('#gridMain').kendoGrid({
            dataSource: {
                transport: {
                    read: {
                        url: URL_GET_PAGE,
                        dataType: "json",
                        cache:false,
                        type: "GET",
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
                            swalFatal("Fatal Error", `Error occured while fetching page(s)\n${res.Message}`)
                            return []
                        }

                        for (var k in res.Data) {
                            var data = res.Data[k];
                            var k = "";
                            if (data.Level > 0) {
                                k += `<i class="text-info mdi mdi-arrow-right"></i>&nbsp;`;
                                data.BuiltTitle = k + data.Title;
                            }else{
                                data.BuiltTitle = `<strong>${data.Title}</strong>`;
                            }
                            
                        }
                        model.form.Data(ko.mapping.toJS(res.Data));
            
                        setTimeout(function () {
                            // model.form.initGrid();
                            model.form.InitParentMenu("");
                        }, 300)

                        return res.Data || [];
                    },
                    total: "Total",
                },
                error: function (e) {
                    swalFatal("Fatal Error", `Error occured while fetching page(s)\n${e.xhr.responseText}`)
                },
                type: "json",
                //sort: { field: "Index", dir: "asc" }
            },
            filterable: false,
            pageable: {
                previousNext: false,
                info: false,
                numeric: false,
                refresh: true
            },
            noRecords: {
                template: "No page data available."
            },
            scrollable:true,
            columns: [
                //{
                //    field: "PageCode",
                //    title: "Code"
                //},
                //{
                //    field: "Category",
                //    title: "Category"
                //},
                {
                    field: "Title",
                    title: "Title",
                    template: function (e) {
                        return `<a href="#" onclick="model.form.ShowDetail('${e.Id}','detail'); return false;">${e.BuiltTitle}</a>`;
                    },
                    width:250
                },
                {
                    field: "Url",
                    title: "Url",
                    width: 250
                },
                {
                    field: "Icon",
                    title: "Icon",
                    template: function (e) {
                        return (e.Icon) ? `<i class="${e.Icon}">${e.Icon}</i>` : '-';
                    },
                    width: 200
                },
                // {
                //     field: "ParentId",
                //     title: "Parent",
                //     template: function (e) {
                //         return e.ParentId || '-';
                //     },
                // },
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
                    headerAttributes: {
                        "class": "text-center icon-large",
                    },
                    attributes: {
                        "class": "text-center icon-large",
                    },                    
                    template: function (e) {
                        var output = [];
                        if(e.Enabled){
                            output.push(`<i class="text-success mdi mdi-checkbox-marked-circle" title="Enabled"></i>`);
                        }else{
                            output.push(`<i class="text-secondary mdi mdi-check-circle-outline" title="Disabled"></i>`);
                        }
                        
                        if(e.ShowAsMenu){
                            output.push(`<i class="text-info mdi mdi-link-variant" title="Show as Menu ON"></i>`);
                        }else{
                            output.push(`<i class="text-secondary mdi mdi-link-variant-off" title="Show as Menu OFF"></i>`);
                        }
                        
                        if(e.ForWhomHasSubordinate){
                            output.push(`<i class="text-primary mdi mdi-account-multiple" title="Only for who has subordinate"></i>`);
                        }else{
                            output.push(`<i class="text-secondary mdi mdi-account-multiple-outline" title="For All"></i>`);
                        }
                        
                        return output.join("&nbsp;");
                    },
                    width: 250
                },
                {
                    width: 75,
                    headerAttributes: {
                        "class": "text-center",
                    },
                    attributes: {
                        "class": "text-center",
                    },
                    template: function (e) {
                        return `<button class="btn btn-xs btn-outline-warning" onclick="model.form.ShowDetail('${e.Id}','edit'); return false;"><i class="fa mdi mdi-pencil"></i></button>`;
                    },

                },
            ],
        });

    },
    ShowDetail: function(id, action) {        
        isLoading(true);
        const url = URL_GET_DETAIL_USER_MANAGEMENT+`/${id}`
        ajax(url, "GET", {}, function (res) {
            isLoading(false);
            model.form.action(action);
            model.form.DetailData(ko.mapping.fromJS(res.Data[0]));
            setTimeout(function () {
                model.form.InitParentMenu(model.form.DetailData().ParentId());
                $("#modalAddMenu").modal("show");
            }, 300)
        }, function (data) {
            isLoading(false);
            swalFatal("Page", data.Message);
        });
    },
    Save: async function () {
        var param = ko.mapping.toJS(model.form.DetailData);
        let result = await swalConfirm("Page", `Are you sure saving page ${param.Title} to database ?`);
        if (result.value) {
            const url = URL_SAVE_USER_MANAGEMENT
            
            isLoading(true);
            ajaxPost(url, param, function (res) {
                swalSuccess('Page', `Page ${param.Title} has been saved successfully`);
                $('#modalAddMenu').modal('hide');
                model.form.refreshGrid();
                isLoading(false);
            }, function (res) {
                $('#modalAddMenu').modal('show');
                swalFatal("Page", res.Message);
                isLoading(false);
            });

        }
    }

}