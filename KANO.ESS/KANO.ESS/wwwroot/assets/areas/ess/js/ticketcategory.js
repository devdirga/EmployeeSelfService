model.form = {
    Data: ko.observable(),
    NewTicketCategory: (obj) => {
        let TicketCategory = _.clone(model.proto.TicketCategory)
        if (obj && typeof obj == "object") {
            return ko.mapping.fromJS(Object.assign(TicketCategory, obj))
        }
        return ko.mapping.fromJS(TicketCategory)
    },
    TicketCategory: ko.observable(ko.mapping.fromJS(_.cloneDeep(model.proto.TicketCategory))),
    TicketCategoryContact: () => {
        var proto = _.clone(model.proto.TicketCategoryContact)
        return ko.mapping.fromJS(proto)
    },
    Data: ko.observable(),
    GetDataGrid: () => {
        ajax(URL_GET_TICKET_CATEGORIES, "GET", {}, function (res) {
            model.form.Data(ko.mapping.toJS(res.Data))
            model.form.InitGrid()
        })
    },
    InitGrid: () => {
        $('#gridMain').html("");
        $('#gridMain').kendoGrid({
            dataSource: {
                data: model.form.Data(),
                type: "json",
                pageSize: 10,
            },
            filterable: false,
            pageable: {
                refresh: false,
                pageSizes: 10,
                buttonCount: 5
            },
            noRecords: {
                template: "No ticket category data available."
            },
            columns: [
                {
                    field: "Name",
                    title: "Name",
                    width: 200
                },
                {
                    field: "Description",
                    title: "Description",
                    width: 200
                },
                {
                    field: "Email",
                    title: "Contacts",
                    template: (e) => {
                        let Email = []
                        e.Contacts.forEach((elm) => {
                            Email.push(elm.Email)
                        });
                        return Email.join(',');
                    },
                    width: 300
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
                    template: (e) => {
                        return `<button class="btn btn-xs btn-outline-info" onclick="model.form.Edit('${e.uid}'); return false;"><i class="fa mdi mdi-pencil"></i></button>
                        <button class="btn btn-xs btn-outline-danger" onclick="model.form.Delete('${e.uid}'); return false;"><i class="fa mdi mdi-delete"></i></button>`;
                    },

                },
            ],
        });
    },
    Add: () => {
        model.form.TicketCategory(model.form.NewTicketCategory())
        $("#ModalTicketCategory").modal("show")
    },
    Save: () => {
        let DialogTitle = 'Ticket category'
        if (model.form.TicketCategory().Name() == null) {
            swalAlert(DialogTitle, 'Category name is required.')
            return
        }
        if (model.form.TicketCategory().Description() == null) {
            swalAlert(DialogTitle, 'Category description is required.')
            return
        }
        if (model.form.TicketCategory().Contacts().length == 0) {
            swalAlert(DialogTitle, 'Category contacts is required.')
            return;
        }
        swal({
            title: "Are you sure?",
            text: "Yu will store new data to database !",
            type: "warning",
            confirmButtonColor: '#d9534f',
            confirmButtonText: 'Confirm',
            cancelButtonText: "Cancel",
            showCancelButton: true,
        })
            .then((will) => {
                if (will) {
                    ajaxPost(URL_SAVE_TICKET_CATEGORY, model.form.TicketCategory(), function (res) {
                        swal({ title: "", text: res.Message, type: "success" })
                        model.form.GetDataGrid()
                        $("#ModalTicketCategory").modal("hide")
                    });
                }
            });
    },
    Edit: (uid) => {
        dataGrid = $("#gridMain").data("kendoGrid").dataSource.getByUid(uid)
        model.form.TicketCategory(model.form.NewTicketCategory(ko.mapping.fromJS(dataGrid._raw())))
        $("#ModalTicketCategory").modal("show")
    },
    Delete: (uid) => {
        dataGrid = $("#gridMain").data("kendoGrid").dataSource.getByUid(uid)
        model.form.TicketCategory(model.form.NewTicketCategory(ko.mapping.fromJS(dataGrid._raw())))
        model.form.TicketCategory().Name('')
        swal({
            title: "Are you sure?",
            text: "Yu will delete data to database !",
            type: "warning",
            confirmButtonColor: '#d9534f',
            confirmButtonText: 'Confirm',
            cancelButtonText: "Cancel",
            showCancelButton: true,
        })
            .then((will) => {
                if (will) {
                    ajaxPost(URL_SAVE_TICKET_CATEGORY, model.form.TicketCategory(), (res) => {
                        swal({ title: "", text: res.Message, type: "success" })
                        model.form.GetDataGrid()
                        $("#ModalTicketCategory").modal("hide")
                    });
                }
            });
    },
    Action: {
        AddContact: () => {
            model.form.TicketCategory().Contacts.push(model.form.TicketCategoryContact())
        },
        DeleteContact: (id) => {
            model.form.TicketCategory().Contacts.remove(id)
        }
    }
}