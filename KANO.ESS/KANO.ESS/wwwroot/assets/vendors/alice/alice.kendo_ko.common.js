function KendoPagingRequest() {
    this.initialize.apply(this, arguments);
}

KendoPagingRequest.prototype.initialize = function(options) {
    options = options || {};
    options.pageSize = options.pageSize || 20;

    this.type = "json";
    this.transport = {
        read: {
            url: options.url,
            contentType: "application/json",
            type: "POST",
            dataType: "json"
        },
        parameterMap: function (o) {
            var param = {};
            param.Take = o.take;
            param.Skip = o.skip;
            param.Page = o.page;
            param.Sort = o.sort;
            param.PageSize = options.pageSize;
            param.Filter = typeof options.filter === "object" ? options.filter : typeof options.filter === "function" ? options.filter() : null;
            return kendo.stringify(param);
        }
    };
    this.schema = {
        data: "Data",
        total: "Total"
    };
    this.pageSize = options.pageSize;
    this.serverPaging = options.serverPaging !== undefined ? options.serverPaging === true : true;
    this.serverFiltering = options.serverFiltering !== undefined ? options.serverFiltering === true : true;
    this.serverSorting = options.serverSorting !== undefined ? options.serverSorting === true : true;
};