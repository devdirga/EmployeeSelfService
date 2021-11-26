var notificationAudio;
let identificationLabelMap = {
    "no ktp": "NIK",
    "kartu tanda penduduk": "NIK",
    "bpjs ketenagakerjaan": "BPJS NAKER",
    "no jamsostek": "Jamsostek",
    "jaminan sosial dan kesehatan": "Jamsostek",
    "kartu keluarga": "KK",
    "nomor pokok wajib pajak": "NPWP",
    "no induk kopelindo": "KOPELINDO",
    "alien/admission no.": "Admission"
};

let approvedElectronicAddress = [1, 2];
let _DEFAULT_DATE = "0001-01-01T07:00:00+07:00";

model.is.readonly = ko.observable(false);

// SignalR connection init
model.app.socket = new signalR.HubConnectionBuilder()
    .withUrl(`/Notification/Hub?s=${model.app.config.employeeID}`)
    .withAutomaticReconnect()
    .build();

model.app.socket.start().then(function () {
    console.log("Web socket is started");
}).catch(function (err) {
    return console.error("Web socket is error : "+err.toString());
});

model.app.socket.on("ReceiveNotification", function(notification) {
    try {
        model.app.action.addNotification(notification);
        notificationAudio.play();
    } catch (e) {
        // Ignore
    }    
});

model.app.newChangePassword = function () {
    var form = Object.assign({ ReNewPassword: "" }, this.proto.ChangePassword);
    return ko.mapping.fromJS(form);
};

model.app.newRedeem = function (data) {
    if (data) {
        return ko.mapping.fromJS(Object.assign(_.clone(this.proto.Redeem), _.clone(data), { PICName: "" }));
    }
    return ko.mapping.fromJS(Object.assign(_.clone(this.proto.Redeem), { PICName: "" }));
};

model.app.data = {};
model.app.data.changePassword = ko.observable(model.app.newChangePassword());
model.app.data.taskCounter = ko.observable(0);
model.app.data.redeem = ko.observable(model.app.newRedeem());
model.app.data.updateRequest = ko.observableArray([]);
model.app.data.notificationCounter = ko.observableArray(0);
model.app.data.canteenInfo = ko.observable({
    VoucherRemaining: 0,
    VoucherUsed: 0,
    VoucherExpired: 0,
    VoucherAlmostExpired: 0
});
model.app.data.isneedchangepassword = ko.observable(false);
model.app.data.changepasswordcountdays = ko.observable(0);

model.app.data.isneedchangepassword(calculateChangePassword(model.app.config.lastchangepassword, model.app.config.thresholdChangePassword))
model.app.data.changepasswordcountdays(calculateChangePasswordCount(model.app.config.lastchangepassword))

model.app.is = {};
model.app.is.canteenUnselect = ko.observable(true);
model.app.is.canteenSelect = ko.observable(false);
model.app.is.subscribed = ko.observable(false);
model.app.is.subscriptionChecked = ko.observable(false);
model.app.list = {};
model.app.list.canteen = ko.observableArray([]);

model.app.action = {};

model.app.action.openChangePasswordModal = function () {
    model.app.data.changePassword(model.app.newChangePassword());

    var container = $("#formChangePassword");
    kendo.init(container);
    container.kendoValidator({
        rules: {
            ruleCurrentPassword: function (input) {
                if (input.is("[name=currentPassword]")) {
                    if (input.val().length < 6) {
                        return true;
                    }
                }
                return true;
            },
            ruleNewPassword: function (input) {
                if (input.is("[name=newPassword]")) {
                    if (input.val().length < 6) {
                        return false;
                    }
                }
                return true;
            },
            ruleReNewPassword: function (input) {
                if (input.is("[name=reNewPassword]")) {
                    if (input.val().length < 6) {
                        return false;
                    }
                }
                return true;
            },
            ruleReNewPasswordNotMatch: function (input) {
                if (input.is("[name=reNewPassword]")) {
                    if (model.app.data.changePassword().NewPassword() != model.app.data.changePassword().ReNewPassword()) {
                        return false;
                    }
                    return true;
                }
                return true;
            },
        },
        messages: {
            ruleCurrentPassword: "Password at least 6 character.",
            ruleNewPassword: "Password at least 6 character.",
            ruleReNewPassword: "Password at least 6 character.",
            ruleReNewPasswordNotMatch: "Password mot match."
        }
    });

    $("#modalChangePassword").modal("show");
}
model.app.action.changePassword = async function () {
    let dialogTitle = "Change Password";
    let self = model;
    let param = ko.mapping.toJS(self.app.data.changePassword());

    if ((param.NewPassword || "") != (param.ReNewPassword || "")) {
        swalError( dialogTitle, 'Password confirmation must match new password.');
        return;
    }

    let confirmResult = await swalConfirm(dialogTitle, "Are you sure to change your password ?");
    if (confirmResult.value) {
        try {
            isLoading(true);
            let response = await ajax("/Site/Auth/ChangePassword", "POST", JSON.stringify(param));
            isLoading(false);
            if (response.StatusCode == 200) {
                if (response.Data.Success) {
                    swalSuccess(dialogTitle, response.Data.Message);
                    $("#modalChangePassword").modal("hide");
                } else {
                    swalError(dialogTitle, response.Data.Message);
                }                                
                return;
            }
            swalError(dialogTitle, (response.Data) ? response.Data.Message: response.Message);
        } catch (e) {
            isLoading(false);
            swalError(dialogTitle, e);
        }

    }
};

model.app.get = {};
model.app.get.notification = async function (limit = model.app.config.maxNotification, offset = 0, filter = "unread") {
    let self = model;
    let response = await ajax("/ESS/Notification/Get", "POST", JSON.stringify({ Limit: limit, Offset: offset, Filter : filter }));
    if (response.StatusCode == 200) {
        let data = response.Data || [];
        let total = response.Total || 0;
        return {
            Data: data,
            Total: total 
        };

        return result;
    }
    console.error(response.StatusCode, ":", response.Message);
    return [];
};

model.app.get.task = async function (limit = model.app.config.maxNotification, offset = 0, range = null, activeOnly=false) {
    let self = model;
    if (!range) range = { Start: _DEFAULT_DATE, Finish: _DEFAULT_DATE };
    if (!range.Start) range.Start = _DEFAULT_DATE;
    if (!range.Finish) range.Finish = _DEFAULT_DATE;

    let response = await ajaxPost("/ESS/Task/GetRange", { Limit: limit, Offset: offset, Range: range, ActiveOnly:activeOnly });
    if (response.StatusCode == 200) {
        let data = response.Data || [];
        let total = response.Total || 0;
        return {
            Data: data,
            Total: total
        };

        return result;
    }
    console.error(response.StatusCode, ":", response.Message);
    return []
};

model.app.get.taskActive = async function (limit = model.app.config.maxNotification, offset = 0, range = null) {
    let self = model;
    let response = await ajax("/ESS/Task/GetActive", "POST", JSON.stringify({ Limit: limit, Offset: offset, Range: range }));
    if (response.StatusCode == 200) {
        let data = response.Data || [];
        let total = response.Total || 0;
        return {
            Data: data,
            Total: total
        };

        return result;
    }
    console.error(response.StatusCode, ":", response.Message);
    return []
};

model.app.get.taskCounter = async function () {
    let self = model;
    let response = await ajax("/ESS/Task/CountActive", "GET");
    if (response.StatusCode == 200) {
        let data = response.Data || [];
        let total = response.Total || 0;
        return {
            Data: data,
            Total: total
        };

        return result;
    }
    //console.error(response.StatusCode, ":", response.Message);
    return [];
};

model.app.action.addNotification = function (notification) {
    let n = ko.mapping.fromJS(ko.mapping.toJS(notification));
    model.app.notifications.unshift(n);

    let nCount = model.app.notifications().length;
    if (nCount > model.app.config.maxNotification) {
        model.app.notifications.splice(nCount - 1, 1);
    }
    var counter = model.app.data.notificationCounter();
    model.app.data.notificationCounter(counter+1);
};

model.app.setCounter = function(counter){
    counter = parseInt(counter);
    if(counter > 99){
        return "99+";
    }
    return counter+"";
}; 

var _markingAsRead = false;
model.app.action.markNotificationAsRead = function (e, o) {
    e.stopPropagation();        
    if (!_markingAsRead) {
        _markingAsRead = true;

        var unreadNotifications = [];
        if (o && o.dataset.nid) {
            unreadNotifications = model.app.notifications().filter(x => {
                return x.Id() == o.dataset.nid;
            }).map(x => {
                return ko.mapping.toJS(x);
            });
        } else {
            unreadNotifications = model.app.notifications().filter(x => {
                return !x.Read();
            }).map(x => {
                return ko.mapping.toJS(x);
            });
        }      

        if (unreadNotifications.length > 0) {
            model.app.socket.invoke("MarkNotificationsAsRead", unreadNotifications)
                .catch(function (err) {
                    return console.error(err.toString());
                })
                .then(function (r) {
                    let readNotificationsMap = {};
                    for (var i in unreadNotifications) {
                        readNotificationsMap[unreadNotifications[i].Id] = unreadNotifications[i];
                    }

                    model.app.notifications().forEach(x => {
                        if (readNotificationsMap[x.Id()]) {
                            x.Read(true);
                        }
                    });
                
                    _markingAsRead = false;
                });
        } else {
            _markingAsRead = false;
        }
    }
};

model.app.action.markAllNotificationAsRead = function (e, o) {
    e.stopPropagation();
    if (!_markingAsRead) {
        _markingAsRead = true;

        model.app.socket.invoke("MarkAllNotificationsAsRead")
            .catch(function (err) {
                return console.error(err.toString());
            })
            .then(function (r) {
                let readNotificationsMap = {};                
                model.app.notifications().forEach(x => {
                        x.Read(true);
                });
                model.app.data.notificationCounter(0);
                _markingAsRead = false;
            });        
    }
};

model.app.init = {};
model.app.init.notification = async function () {
    var self = model;    
    
    try {
        notificationAudio = new Audio('/assets/sounds/notification.wav');
    } catch (e) {
        console.warn("unable to load notification sound");
    }

    setTimeout(async function () {
        var data = await self.app.get.taskCounter();
        self.app.data.taskCounter(data.Total);
    });
    
    
    setTimeout(async function () { 
        var notifications = await self.app.get.notification(model.app.config.maxNotification, 0, "unread");
        self.app.data.notificationCounter(notifications?notifications.Total : 0);
        for (var i in notifications.Data) {
            let n = notifications.Data[i];
            self.app.notifications.push(ko.mapping.fromJS(n));
        }        
    }, 0)

    $('#employeeQRModal').on('show.bs.modal', function (e) {
        model.app.init.employeeQR();
    })
};

model.app.parseStepTrackingStatus = function (stepTrackingStatus, inverted = false) {
    if(inverted){
        switch (stepTrackingStatus) {
            case 2:
                return "Rejected";
            case 3:
                return "Cancelled";
            case 4:
                return "Approved";
            default:
                return "-";
        }    
    }else{
        switch (stepTrackingStatus) {
            case 2:
                return "Approved";
            case 3:
                return "Cancelled";
            case 4:
                return "Rejected";
            default:
                return "-";
        }    
    }
    
};

model.app.parseTrackingStatus = function (trackingStatus, inverted = false) {
    if(inverted){
        switch (trackingStatus) {
            case 0:
                return "In Review";
            case 1:
                return "Rejected";
            case 2:
                return "Cancelled";
            case 3:
                return "Approved";
            default:
                return "-";
        }    
    }else{
        switch (trackingStatus) {
            case 0:
                return "In Review";
            case 1:
                return "Approved";
            case 2:
                return "Cancelled";
            case 3:
                return "Rejected";
            default:
                return "-";
        }    
    }
};

model.app.parseIcon = function (d) {
    var module;
    var d = ko.mapping.toJS(d);
    
    if (typeof d == "object") {        
        if (d.Module == undefined) {
            module = d
        } else {
            module = (d.Module || "").toLowerCase();
        }
    } else {
        module = d.toLowerCase();
    }
    
    switch (module) {
        case 'dashboard':
            return `mdi mdi-home`;
            break;
        case 'certificate':
            return `mdi mdi-clipboard-text`;
            break;
        case 'family':
            return `mdi mdi-human-male-female`;
            break;
        case 'auth':
            return `mdi mdi-lock`;
            break;
        case 'hcm':
        case 'employee':
            return `mdi mdi-account-box`;
            break;
        case 'tm':
        case 'time-management':
            return `mdi mdi-calendar-clock`;
            break;
        case 'payroll':
            return `mdi mdi-coins`;
            break;
        case 'benefit':
            return `mdi mdi-home`;
            break;
        case 'lm':
        case 'leave':
            return `mdi mdi-bag-personal`;
            break;
        case 'te':
        case 'tereq':
        case 'travel':
            return `mdi mdi-wallet-travel`;
            break;
        case 'cr':
        case 'benefit':
            return `mdi mdi-hospital-building`;
            break;
        case 'retirement':
        case 'retirement':
            return `mdi mdi-beach`;
            break;
        case 'training_registration':
            return `mdi mdi-run`;
            break;
        default:
            return `mdi mdi-information`;
            break;
    }
};

model.app.parseLink = function (d) {
    return '#'
    //let action = (d.Action() || "").toLowerCase();
    //switch (action) {
    //    case 'none':
    //    case 'default':
    //        return `#`;
    //        break;
    //    case 'open_employee_profile':
    //        return `/ESS/Employee/Profile`;
    //        break;
    //    case 'open_employee_document':
    //        return `/ESS/Employee/Profile#documents`;
    //        break;
    //    case 'open_document_request':
    //        return `/ESS/Employee/DocumentRequest`;
    //        break;
    //    case 'open_leave':
    //        return `/ESS/Leave/`;
    //        break;
    //    case 'open_loan_request':
    //        return `/ESS/Payroll/LoanRequest`;
    //        break;        
    //    default:
    //        return `#`;
    //        break;
    //}
};

model.app.profilePicture = ko.computed(function () {
    var userData = model.app.config.userData;
    if (userData.profilePicture) {
        return `url('/employee/${userData.profilePicture}'), url('/assets/img/blank-user.png')`;
    }else{

    }
    return `url('/employee/${model.app.config.employeeID}.jpg'), url('/assets/img/blank-user.png')`;
});

model.app.getProfilePicture = function (employeeID = "") {
    var imagePath = "";
    if (!employeeID) {
        var userData = model.app.config.userData;
        imagePath = userData.profilePicture;
    } else {
        imagePath = `${employeeID}.jpg`;
    }

    if (imagePath) {
        return `url('/employee/${imagePath}'), url('/assets/img/blank-user.png')`;
    }
    return `url('/assets/img/blank-user.png')`;
};

model.app.hasSubordinate = ko.computed(function () {
    var userData = model.app.config.userData;
    return (userData.hasSubordinate == "True");
});

model.app.unreadNotificationExists = ko.computed(function () {
    return model.app.notifications().filter(x => { return !x.Read() }).length > 0;
});

model.app.unreadNotifications = ko.computed(function () {
    return model.app.notifications().filter(x => { return !x.Read() });
});

model.app.init.sidebarListener = function () {
    $(".navbar-toggler:not(.navbar-toggler-right)").click((e) => {
        localStorage.setItem("sidebar-collapsed", ($('.sidebar').width() > 100));
    });
};

model.app.data.validateKendoUpload = function (e, dialogTitle) {
    let valid = true;
    let validationOption = e.sender.options.validation;
    e.files = e.files || [];
    e.files.forEach(f => {
        if (f.validationErrors && f.validationErrors.length > 0 && valid) {
            valid = false;
            f.validationErrors.forEach(err => {
                switch (err) {
                    case "invalidMaxFileSize":
                        swalError(dialogTitle, `File upload size could not be more than ${humanizeBytes(validationOption.maxFileSize)}`);
                        return;
                        break;
                    case "invalidFileExtension":
                        swalError(dialogTitle, `File upload extension should be ${validationOption.allowedExtensions.join(" / ")}`);
                        return;
                        break;
                    default:
                        break;
                }
            });
        }
    });

    return valid;
}

model.app.init.employeeQR = function () {
    $("#employeeQRCode").html("");
    new QRCode(document.getElementById("employeeQRCode"), `${model.app.config.employeeID}%${(new Date()).getTime() * Math.random() * 20}%${(new Date()).getTime()}`);
};

model.app.init.canteen = async function () {
    let self = model;
    await Promise.all([
        new Promise(async (resolve) => {
            if ($("#canteenContainer").length > 0) kendo.ui.progress($("#canteenContainer"), true);
            
            var data = await self.app.get.voucher();
            self.app.data.canteenInfo(data);
            self.app.data.redeem().CurrentTotal(data.VoucherRemaining);
            if ($("#canteenContainer").length > 0) kendo.ui.progress($("#canteenContainer"), false);
            resolve(true);
        }),
        new Promise(async (resolve) => {
            var data = await self.app.get.canteen();
            model.app.list.canteen(data);    
            resolve(true);
        }),
    ])        
}

model.app.action.trackTask = async function ($data) {
    let dialogTitle = "Task Tracking";
    let data = ko.mapping.toJS($data);
    let instanceID = (typeof data == "object") ? data.InstanceId : data;

    model.app.data.updateRequest([]);
    isLoading(true);
    var updateRequest = await model.app.get.updateRequestByInstanceID(instanceID);
    isLoading(false);
    if (updateRequest) {
        model.app.data.updateRequest([ko.mapping.fromJS(updateRequest)])
        $("#updateRequestModal").modal("show");
        $("#updateRequestAccordion .card-header").click();
    } else {
        swalAlert(dialogTitle, "Unable to find update request record tracking ESS server");
    }
};

model.app.action.trackTaskEmployee = async function (instanceID, employeeID) {
    let dialogTitle = "Task Tracking";    
    model.app.data.updateRequest([]);
    isLoading(true);
    var updateRequest = await model.app.get.updateRequestByEmployeeInstanceID(instanceID,employeeID);
    isLoading(false);
    if (updateRequest) {
        model.app.data.updateRequest([ko.mapping.fromJS(updateRequest)])
        $("#updateRequestModal").modal("show");
        $("#updateRequestAccordion .card-header").click();
    } else {
        swalAlert(dialogTitle, "Unable to find update request record tracking ESS server");
    }
};

model.app.action.openRedeem = function() {
    $("#modalRedeemGlobal").modal("show");
    model.app.is.canteenSelect(false);
    model.app.is.canteenUnselect(true);
}

model.app.action.cancelRedeem = function () {
    model.app.is.canteenSelect(false);
    model.app.is.canteenUnselect(true);
    model.app.data.redeem().RedeemedVoucherTotal(0);
}

model.app.action.canteenSelect = function (data) {
    var d = ko.mapping.toJS(data);
    model.app.data.redeem().CanteenID(d.Id);
    model.app.data.redeem().CanteenName(d.Name);
    model.app.data.redeem().PICName(d.PICName);
    //model.app.data.redeem(ko.mapping.toJS(data));
    model.app.is.canteenSelect(true);
    model.app.is.canteenUnselect(false);
}

model.app.get.voucher = async function () {
    let response = await ajax("/ESS/Canteen/GetVoucher", "GET");
    if (response.StatusCode == 200 && response.Data) {
        return response.Data || {};
    }
    console.error(response.StatusCode, ":", response.Message)
    return Object.assign({}, model.proto.Employee);
}

model.app.get.canteen = async function () {
    let response = await ajax("/ESS/Canteen/Get", "GET");
    if (response.StatusCode == 200) {
        return response.Data || [];
    }
    console.error(response.StatusCode, ":", response.Message)
    return Object.assign({}, model.proto.Employee);
}

model.app.get.updateRequestByInstanceID = async function (instanceID = "") {
    let response = await ajax(`/ESS/UpdateRequest/GetByInstanceID/${instanceID}`, "GET");
    if (response.StatusCode == 200) {
        return response.Data || null;
    }
    //console.error(response.StatusCode, ":", response.Message);
    return [];
};

model.app.get.updateRequestByEmployeeInstanceID = async function (instanceID = "", employeeID = "") {
    let response = await ajax(`/ESS/UpdateRequest/GetByEmployeeInstanceID/${employeeID}/${instanceID}`, "GET");
    if (response.StatusCode == 200) {
        return response.Data || null;
    }
    //console.error(response.StatusCode, ":", response.Message);
    return [];
};


model.app.action.addReedemVoucher = function (data) {
    var total = parseInt(data.RedeemedVoucherTotal());
    if (total == parseInt(data.CurrentTotal())) {
        return;
    }
    data.RedeemedVoucherTotal(total + 1);
}

model.app.action.substractReedemVoucher = function (data) {
    var total = parseInt(data.RedeemedVoucherTotal());
    if (total == 0) {
        return;
    }
    data.RedeemedVoucherTotal(total - 1);
}

model.app.action.saveRedeem = async function() {
    isLoading(true);
    var param = ko.mapping.toJS(model.app.data.redeem);
    if (param.CurrentTotal < param.RedeemedVoucherTotal) {
        swalAlert("Voucher Redeem", "Voucher should not exceed the rest");
        return false;
    }

    if (param.RedeemedVoucherTotal == 0) {
        swalAlert("Voucher Redeem", "Total redeem voucher should be more than 0");
        return false;
    }

    let strCanteen = ` for ${param.CanteenName}`    
    let confirmResult = await swalConfirm("Voucher Redeem", `Are you sure to redeem <b>${param.RedeemedVoucherTotal} of ${param.CurrentTotal}</b> your voucher${strCanteen} ?`);
    if (!confirmResult.value) {
        return;
    }
    
    isLoading(true);
    try {
        ajaxPost("/ESS/Canteen/Redeem", param, async function (res) {
            console.log(res);
            if (res.StatusCode == 200) {
                isLoading(false);
                swalSuccess("Order", res.Message);
                $("#modalRedeemGlobal").modal("hide");
                model.app.is.canteenSelect(false);
                model.app.is.canteenUnselect(true);

                isLoading(true);
                await Promise.all([
                    new Promise(async (resolve) => {
                        if (typeof model.render.info == "function") await model.render.info();
                        resolve(true);
                    }),
                    new Promise(async (resolve) => {
                        if (typeof model.action.refreshGridHistory == "function") model.action.refreshGridHistory();                        
                        resolve(true);
                    }),
                    new Promise(async (resolve) => {
                        await model.app.init.canteen();    
                        resolve(true);
                    }),
                ])
                isLoading(false);
                
            } else {
                swalError("Voucher Redeem", res.Message);
            }
            isLoading(false);
        });
    } catch (e) {
        isLoading(false);
    }

}

model.app.action.launchkara = async function () {
    var result = await swalInfo(
        `Download Kara Apps`,
        `Uninstall aplikasi KARA anda sebelum meng-install baru`
    );
    if (result.value) {
      //window.open(`https://drive.google.com/file/d/17vJin52Szpk7eep7LNNvDsg4xQjb97wo/view?usp=sharing`, '_blank')
      window.open(`https://drive.google.com/file/d/1QPHPMpFjoFlIeiWYFU28XeYGlDtEuTEg/view?usp=sharing`, '_blank')
    }
};


//notification
model.app.showNotification = function (notification) {
    var desc = notification.Message;
    var title = titleCase(notification.Module);
    if (Notification.permission === 'granted') {
        model.app.createNotification(title, desc);
    }
}
model.app.createNotification = function (title, desc) {
    let img = '/assets/img/tps-small.png';
    let text = desc;
    let notification = new Notification(title, { body: text, icon: img });
}

model.app.askNotificationPermission = function () {
    function handlePermission(permission) {
        if (!('permission' in Notification)) {
            Notification.permission = permission;
        }
    }

    if (!"Notification" in window) {
        console.log("This browser does not support notifications.");
    } else {
        if (model.app.checkNotificationPromise()) {
            Notification.requestPermission()
                .then((permission) => {
                    handlePermission(permission);
                })
        } else {
            Notification.requestPermission(function (permission) {
                handlePermission(permission);
            })
        }
    }
}

model.app.checkNotificationPromise = function () {
    try {
        Notification.requestPermission().then();
    } catch (e) {
        return false;
    }

    return true;
}
//
model.app.loadServiceWorker = async function () {
    model.app.is.subscriptionChecked(false);

    if ('serviceWorker' in navigator) {
        navigator.serviceWorker.register('/sw.js').then(function (registration) {
            console.log('Service worker registration succeeded');
        }, function (error) {
            console.error("Service worker registration failed :", error)
        })
        //check
        navigator.serviceWorker.ready.then(function (registration) {
            console.log('Service worker registered');
            return registration.pushManager.getSubscription();
        }).then(async function (subscription) {        
            if (subscription) {                
                var stringify = JSON.stringify(subscription);
                var newPush = JSON.parse(stringify);
                var param = {
                    Receiver: model.app.config.employeeID,
                    Subscription: {
                        EndPoint: newPush.endpoint,
                        ExpirationTime: 0,
                        Keys: {
                            P256DH: newPush.keys.p256dh,
                            Auth: newPush.keys.auth
                        }
                    }
                }
                
                let serverSubscriptionCheck= await ajaxPost("/ESS/Notification/SubscriptionCheck", param); 
                let serverSubscribed = (!!serverSubscriptionCheck) ? (serverSubscriptionCheck.Data || false) : false;

                // if broswser is already subscribed
                console.log('already subscribed :', subscription.endpoint);

                // but if it is not stored in server yet
                // then unsubscribe and re-subscribe
                if(!serverSubscribed){
                    console.log('re-subscribing ...')
                    // await model.app.unsubscribe(true);                    
                    await model.app.subscribe(true);
                }else{
                    model.app.is.subscribed(true);
                }

                model.app.is.subscriptionChecked(true);                
            } else {
                // if broswser is not subscribed yet
                model.app.is.subscribed(false);
                model.app.is.subscriptionChecked(true);                
            }
        });
    } else {
        console.error('Service workers are not supported.');
    }
}

model.app.subscribe = async function (force = false) {
    var key = model.app.config.vpbk;
    if(!!key){
        
        let confirmation = force;
        if(!force){
            let confirmResult = await swalConfirm("Notification", "Are you sure subscribing ESS push-notification ?");
            confirmation = confirmResult.value;
        }
        
        if (confirmation) {            
            try {
                if(!force) isLoading(true);           
                let sw = await navigator.serviceWorker.ready;
                let push = await sw.pushManager.subscribe({
                    userVisibleOnly: true,
                    applicationServerKey: key
                });

                var stringify = JSON.stringify(push);
                var newPush = JSON.parse(stringify);
                var param = {
                    Receiver: model.app.config.employeeID,
                    Subscription: {
                        EndPoint: newPush.endpoint,
                        ExpirationTime: 0,
                        Keys: {
                            P256DH: newPush.keys.p256dh,
                            Auth: newPush.keys.auth
                        }
                    }
                }          

                var url = "/ESS/Notification/Subscribe";        
                await ajaxPost(url, param);
                model.app.is.subscribed(true);   

                if(!force) isLoading(false);                
                if(!force) swalSuccess("Notification", `You have subscribed successfully`)                
            } catch (error) {
                model.app.is.subscribed(false);                  
                if(!force) swalAlert("Notification", "Message" in error ? error.Message:error);
                if(!force) isLoading(false); 
            } 
        }
    }else{
        console.error("check your vapid public key");
    }    
}

model.app.unsubscribe = async function (force = false) {
    let confirmation = force;
    if(!force){
        let confirmResult = await swalConfirm("Notification", "Are you sure unsubscribing ESS push-notification ?");
        confirmation =  confirmResult.value;
    }
    
    if (confirmation) {       
        try {
            if(!force) isLoading(true); 
            let sw = await navigator.serviceWorker.ready;
            let subscription = await sw.pushManager.getSubscription();
            await subscription.unsubscribe();
    
            var stringify = JSON.stringify(subscription);
            var newPush = JSON.parse(stringify);
            var param = {
                Receiver: model.app.config.employeeID,
                Subscription: {
                    EndPoint: newPush.endpoint,
                    ExpirationTime: 0,
                    Keys: {
                        P256DH: newPush.keys.p256dh,
                        Auth: newPush.keys.auth
                    }
                }
            }
    
            var url = "/ESS/Notification/Unubscribe"
            await ajaxPost(url, param);
            model.app.is.subscribed(false);
            if(!force) isLoading(false);
            if(!force) swalSuccess("Notification", `You have  successfully unsubscribed`)

        } catch (error) {
            model.app.is.subscribed(false);                  
            if(!force) swalAlert("Notification", "Message" in error ? error.Message:error);
            if(!force) isLoading(false); 
            
        }    
    }
}

model.app.autoSubscribe = async function () {
    var key = model.app.config.vpbk;
    if (!!key) {
        try {
            let sw = await navigator.serviceWorker.ready;
            let push = await sw.pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: key
            });
            var stringify = JSON.stringify(push);
            var newPush = JSON.parse(stringify);
            var param = {
                Receiver: model.app.config.employeeID,
                Subscription: {
                    EndPoint: newPush.endpoint,
                    ExpirationTime: 0,
                    Keys: {
                        P256DH: newPush.keys.p256dh,
                        Auth: newPush.keys.auth
                    }
                }
            }
            var url = "/ESS/Notification/Subscribe";
            ajaxPost(url, param, function (res) {
                model.app.is.subscribed(true);
                console.log("You have subscribed successfully");
            }, function (data) {
                model.app.is.subscribed(false);
                console.log("err: ", data.Message);
            });             
        } catch (error) {
            model.app.is.subscribed(false);
            console.error(error);
        }
    } else {
        console.error("check your vapid public key");
    }
}

model.app.logout = async function () {
    try {
        isLoading(true);
        await navigator.serviceWorker.ready
        .then(function (registration) {
            return registration.pushManager.getSubscription();
        }).then(function (subscription) {
            return subscription.unsubscribe()
                .then(function () {
                    var stringify = JSON.stringify(subscription);
                    var newPush = JSON.parse(stringify);
                    var param = {
                        Receiver: model.app.config.employeeID,
                        Subscription: {
                            EndPoint: newPush.endpoint,
                            ExpirationTime: 0,
                            Keys: {
                                P256DH: newPush.keys.p256dh,
                                Auth: newPush.keys.auth
                            }
                        }
                    }

                    var url = "/ESS/Notification/Unubscribe"
                    return ajaxPost(url, param, function (res) {
                        model.app.is.subscribed(false);
                        console.log("You have  successfully unsubscribed");
                    }, function (data) {
                        model.app.is.subscribed(true);
                        console.log("err: ",data.Message);
                    });
                })
        }).catch((e) => {
            model.app.is.subscribed(true);
            console.error(e);
        });   
    } catch (e) {
        console.error(e)
    } finally {        
        location.href = "/Site/Auth/DoLogout"
        isLoading(false);
        console.log("href");
    }    

    
}

model.app.hasAccess = function(accessKey){
    if(!!accessKey){
        try {
            if(model.app.config.access.hasOwnProperty(accessKey)){
                var access = model.app.config.access[accessKey];
                return access["CanRead"] || false;
            }
        } catch (error) {
            console.error(error);    
        }
    }
    return false;
};

$(function () {
    setTimeout(function () {
        model.app.init.employeeQR();
    });

    $('[data-toggle="tooltip"]').tooltip();

    $("#logout").click(function () {
        setTimeout(function () {
            console.log("Logout binded");
            window.onbeforeunload = function () { return "Your work will be lost."; };
        }, 500);
    });

    model.app.init.sidebarListener();    
    model.app.init.notification();
    model.app.init.canteen();
    model.app.loadServiceWorker();
});

function displayLoading(target, val) {
    var element = $(target);
    kendo.ui.progress(element, val);
}