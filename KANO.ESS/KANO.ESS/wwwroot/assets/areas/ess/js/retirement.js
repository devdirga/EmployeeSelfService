model.newRetirement = function(obj){
    let proto = _.clone(this.proto.Retirement);
    
    if (typeof obj != "object") {
        obj = {};        
    }

    
    if (!obj.MPPDate) {
        obj.MPPDate = _.clone(this.proto.DateRange);
        obj.MPPDate.Start = _DEFAULT_DATE;
        obj.MPPDate.Finish = _DEFAULT_DATE;
    }
    
    if (!obj.CBDate) {
        obj.CBDate = _.clone(this.proto.DateRange);
        obj.CBDate.Start = _DEFAULT_DATE;
        obj.CBDate.Finish = _DEFAULT_DATE;
    }

    return ko.mapping.fromJS(Object.assign(proto, obj));
};

model.data = {}
model.data.Attachments = []
model.data.Attachments = []
model.data.Retirement = ko.observable(model.newRetirement());

model.get.retirement = async function(){
    let response = await ajax("/ESS/Retirement/Get", "GET");    
    if (response.StatusCode == 200) {
        return response.Data || null;
    }else{
        throw response.Message || "Error occursed while fetching retirement data";
    }
    return null;
};

model.render = {}
model.render.retirementForm = async function(){
    let self = model;

    try{
        kendo.ui.progress($("#retirementForm"), true);    
        var data = await self.get.retirement();

        model.data.Retirement(model.newRetirement(data));

    }catch(e){
        console.error(e);
        swalAlert("Retirement", "Error occured while fetching data");
    }finally{
        kendo.ui.progress($("#retirementForm"), false);
    }
    
};

model.action = {}
model.action.request = async function () {        
    let self = model;
    let dialogTitle = "Retirement";
    let data = ko.mapping.toJS(model.data.Retirement());  

    var result = await swalConfirm(dialogTitle, `Are you sure requesting to retire (MPP/CB) ?`);    
    if (!result.value) return

     

     if (data.MPPType === 0) {
         swalAlert(dialogTitle, 'MPP type is required.');
         return;
    }

    if (data.CBType === 0) {
        swalAlert(dialogTitle, 'CB type is required.');
        return;
    }

    let formData = new FormData();
    formData.append("JsonData", JSON.stringify(data));
    
    var files = $('#Filepath').getKendoUpload().getFiles();
    if (files.length > 0) {
        formData.append("FileUpload", files[0].rawFile);
    } 

    try {
        kendo.ui.progress($("#retirementForm"), true);
        ajaxPostUpload("/ESS/Retirement/Request", formData, function (data) {
            kendo.ui.progress($("#retirementForm"), false);
            if (data.StatusCode == 200) {
                swalSuccess(dialogTitle, data.Message);
                model.render.retirementForm();
            } else {
                swalFatal(dialogTitle, data.Message);
            }
        }, function (data) {
            kendo.ui.progress($("#retirementForm"), false);
            swalFatal(dialogTitle, data.Message);
        })


    } catch (e) {
        kendo.ui.progress($("#retirementForm"), false);
    }

    return false;
}

model.action.downloadAttachment = () => {
    console.log('download')
}

model.action.onChangeMPPType = function () {
    model.data.Retirement().MPPDate.Start(
        moment.parseZone(model.data.Retirement().MPPDate.Finish()).subtract(this.value(), 'months').format('YYYY-MM-DDTHH:mm:ssZ')
    )
    model.data.Retirement().CBDate.Finish(
        moment.parseZone(model.data.Retirement().MPPDate.Finish()).subtract(this.value(), 'months').format('YYYY-MM-DDTHH:mm:ssZ')
    )
}

model.action.onChangeCBType = function () {
    model.data.Retirement().CBDate.Start(
        moment.parseZone(model.data.Retirement().MPPDate.Start()).subtract(this.value(), 'months').format('YYYY-MM-DDTHH:mm:ssZ')
    )
}

model.init.retirement = function(){
    let self = model;
    self.render.retirementForm();
};