

model.action.saveCanteenFile = function () {
    isLoading(true);
    try {
        var dialogTitle = "Save Canteen";
        var $modal = $("#ModalCanteen");
        var data = ko.mapping.toJS(model.data.canteen);
        console.log("param: ", data);

        var formData = new FormData();
        formData.append("JsonData", ko.mapping.toJSON(data));
        var fileUpload = $("#additionalDocument").getKendoUpload();
        if (fileUpload) {
            let file = fileUpload.getFiles()[0];
            if (file) {
                formData.append("FileUpload", file.rawFile);
            }
        }
        ajaxPostUpload("/ESS/Canteen/SaveCanteenx", formData, function (data) {
            isLoading(false);
            if (data.StatusCode == 200) {
                swalSuccess(dialogTitle, data.Message);
                model.render.gridCanteen();
                $modal.modal('hide');
            } else {
                $modal.modal('show');
                swalError(dialogTitle, data.Message);
            }
            isLoading(false);
        }, function (data) {
            $modal.modal('show');
                swalError(dialogTitle, data.Message);
                isLoading(false);
        });
    } catch (e) {
        isLoading(false);
    }
}






$(document).ready(function () {
    model.init.managecanteen();
})