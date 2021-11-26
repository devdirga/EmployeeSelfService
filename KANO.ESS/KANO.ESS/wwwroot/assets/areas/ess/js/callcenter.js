model.init.CallCenter = function () {
  setTimeout(async function () {
    model.render.gridCallCenter()
  })
}
model.render.gridCallCenter = function () {
    let dataNadia = JSON.parse('{"StatusCode":200,"Message":null,"Data": [{"Group":"IT Group","Extention":"2002", "Dept":"HR","User":"HR"},{"Group":"","Extention":"2003", "Dept":"HR","User":"HR ARIF C"},{"Group":"","Extention":"2004","Dept":"PR","User":"Humas Gita"},{"Group":"","Extention":"2005", "Dept":"HR","User":"HR MANAJER"},{"Group":"","Extention":"2006", "Dept":"HSSE","User":"HSSE MANAGER"},{"Group":"","Extention":"2007", "Dept":"HR","User":"HR EKO"},{"Group":"","Extention":"2008", "Dept":"HR","User":"HR SHOLEH"},{"Group":"","Extention":"2009", "Dept":"IT","User":"IT DAVID"},{"Group":"","Extention":"2010", "Dept":"OPS","User":"SHIFT MANAGER"},{"Group":"","Extention":"2011", "Dept":"PR","User":"Kamsiati"},{"Group":"","Extention":"2012", "Dept":"IT","User":"IT PUNGKAS"}]   ,"Total":15}')
  let $el = $("#gridCallCenter");
  if ($el) {
    let $grid = $el.getKendoGrid();
    if (!!$grid) {
      $grid.destroy();
    }
    $el.kendoGrid({
      //dataSource: {
      //  transport: {
      //    read: {
      //      url: "/ESS/Dashboard/Ping",
      //      dataType: "json",
      //      type: "POST",
      //      contentType: "application/json",
      //    }
      //  },
      //  schema: {
      //    data: function (response) {
      //      let res = JSON.parse('{"StatusCode":200,"Message":null,"Data": [{"Group":"IT Group","Extention":"2002", "Dept":"HR","User":"HR"},{"Group":"","Extention":"2003", "Dept":"HR","User":"HR ARIF C"},{"Group":"","Extention":"2004","Dept":"PR","User":"Humas Gita"},{"Group":"","Extention":"2005", "Dept":"HR","User":"HR MANAJER"},{"Group":"","Extention":"2006", "Dept":"HSSE","User":"HSSE MANAGER"},{"Group":"","Extention":"2007", "Dept":"HR","User":"HR EKO"},{"Group":"","Extention":"2008", "Dept":"HR","User":"HR SHOLEH"},{"Group":"","Extention":"2009", "Dept":"IT","User":"IT DAVID"},{"Group":"","Extention":"2010", "Dept":"OPS","User":"SHIFT MANAGER"},{"Group":"","Extention":"2011", "Dept":"PR","User":"Kamsiati"},{"Group":"","Extention":"2012", "Dept":"IT","User":"IT PUNGKAS"}]   ,"Total":15}')
      //      if (res.StatusCode !== 200 && res.Status !== '') {
      //        swalFatal("Fatal Error", `Error occured while fetching travel request(s)\n${res.Message}`)
      //        return []
      //      }
      //      return res.Data || [];
      //    },
      //    total: "Total",
      //  },
      //      pageSize: 5,
      //  error: function (e) {swalFatal("Fatal Error", `Error occured while fetching travel request(s)\n${e.xhr.responseText}`)}
      //},
    dataSource: dataNadia.Data,
      noRecords: {template: "No call denterdata available."},
      sortable: true,
      filterable: {
        operators: {string: {eq: "Is Equal to",contains: "Contains"}},
        extra: false
      },
        //sortable: true,
        pageable: {
            pageSize: 5
        },
      columns: [
        {field: "Group",title: "Group"},
        {field: "Extention",title: "Extention"},
        {field: "Dept",title: "Dept"},
        {field: "User",title: "User"}
      ]
    })
  }
}