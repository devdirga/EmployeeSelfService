if (al) {
    al.swal = {};

    al.swal.Confirm = function (msg, cbOK, cbCancel) {
        swal({
            title: "",
            html: true,
            text: msg,
            type: "warning",
            showCancelButton: true,
            confirmButtonColor: '#DD6B55',
            confirmButtonText: 'Confirm',
            cancelButtonText: "Cancel",
            closeOnConfirm: true,
            closeOnCancel: true
        }, function (ok) {
            if (ok) {
                if (typeof cbOK === "function") {
                    cbOK();
                }
            } else {
                if (typeof cbCancel === "function") {
                    cbCancel();
                }
            }
        });
    };
}