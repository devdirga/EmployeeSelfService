//Method Creating New Models
model.newActivation = function () {
    return ko.mapping.fromJS(this.proto.activation);
};

model.newActivationRequest = function () {
    return ko.mapping.fromJS(this.proto.activationRequest);
};

model.newLogin = function () {
    return ko.mapping.fromJS(this.proto.login);
};

model.newResetPasswordRequest = function () {
    return ko.mapping.fromJS(this.proto.resetPasswordRequest);
};

model.newResetPassword = function () {
    return ko.mapping.fromJS(this.proto.resetPassword);
};

//Model Data
model.data = {};

model.data.activation = ko.observable(model.newActivation());

model.data.activationRequest = ko.observable(model.newActivationRequest());

model.data.login = ko.observable(model.newLogin());

model.data.resetPasswordRequest = ko.observable(model.newResetPasswordRequest());

model.data.resetPassword = ko.observable(model.newResetPassword());

//Actions
model.action = {};

model.action.doActivation = function () {
    var data = ko.mapping.toJS(model.data.activation);    

    if (!(data.Password || "").trim()) {
        swal({ title: "Activation", text: 'Password is required.', type: "warning" });
        return;
    }

    if ((data.Password  || "") != (data.RePassword || "")) {
        swal({ title: "Activation", text: 'Password confirmation must match new password.', type: "warning" });
        return;
    }

    if (!(data.Token || "").trim()) {
        swal({ title: "Activation", text: 'Request is invalid.', type: "error" });
        return;
    }

    isLoading(true)
    ajaxPost("/Site/Auth/DoActivate", data, function (data) {        
        if (data.StatusCode == 200) {
            setTimeout(function () {
                var redirectTo = "";                
                if (!!data.Data) {
                    redirectTo = data.Data.Data;
                }

                location.href = redirectTo || "/Site/Auth/Login";
            }, 1000);
        } else {
            swal({ title: "Activation", text: data.Message, type: "warning" });
            isLoading(false)
        }
    }, function (data) {
        swal({ title: "Activation", text: data.Message, type: "warning" });
        isLoading(false)
    });
}

model.action.requestActivation = function () {
    var data = ko.mapping.toJS(model.data.activationRequest);

    if (!(data.Email || "").trim() || !(data.EmployeeID || "").trim()) {
        swal({ title: "Activation", text: 'Email and employee id is required.', type: "warning" });
        return;
    }

    isLoading(true);
    ajaxPost("/Site/Auth/RequestActivation", data, function (data) {
        if (data.StatusCode == 200) {
            swal({ title: "Activation", html: "Activation request has been sent, please check your email", type: "success" });
            setTimeout(function () {
                location.href = "/Site/Auth/";
            }, 1000);
        } else {
            swal({ title: "Activation", text: data.Message, type: "error" });
            isLoading(false);
        }
    }, function (data) {
            swal({ title: "Activation", text: data.Message, type: "error" });
            isLoading(false);
    });
};

model.action.onKeyPressLogin = function (data, event) {
    if (event.which == 13) {
        model.action.doLogin();
    }
    return true;
}
model.action.onKeyPressActivate = function (data, event) {
    if (event.which == 13) {
        model.action.doActivation();
    }
    return true;
}
model.action.onKeyPressActivateUser = function (data, event) {
    if (event.which == 13) {
        model.action.requestActivation();
    }
    return true;
}
model.action.onKeyPressRequestResetPassword = function (data, event) {
    if (event.which == 13) {
        model.action.requestForgotPassword();
    }
    return true;
}
model.action.onKeyPressResetPassword = function (data, event) {
    if (event.which == 13) {
        model.action.doReset();
    }
    return true;
}
model.action.onKeyPressTestMail = function (data, event) {
    if (event.which == 13) {
        model.action.TestSend();
    }
    return true;
}

model.action.doLogin = function () {
    var data = ko.mapping.toJS(model.data.login);
data.Email = data.EmployeeID
    if (!(data.EmployeeID || "").trim()) {
        swal({ title: "Login", text: 'Employee ID is required.', type: "warning" });
        return;
    }


    if (!(data.Password || "").trim()) {
        swal({ title: "Login", text: 'Password is required.', type: "warning" });
        return;
    }

    // var validator = $("#loginForm").data("kendoValidator");
    //console.log("chafid");
    // if(validator==undefined){
    //    validator= $("#loginForm").kendoValidator().data("kendoValidator");
    // }
    // if (validator.validate()) {
    isLoading(true)
    ajaxPost("/Site/Auth/DoLogin", data, function (data) {
        if (data.StatusCode == 200) {
            if (data.Message == "ACTIVATE") {
                swal({ title: "Login", text: "Your user is not activated yet. Please open 'Activate your acount?' in order to activate your account.", type: "warning" });
            } else {
                setTimeout(function () {
                    var redirectTo = data.Data.Data;
                    if (model.redirectTo.indexOf("_") > -1) {
                        model.redirectTo = "";
                    }

                    location.href = redirectTo || model.redirectTo || "/ESS/Dashboard";
                }, 1000);
            }
        } else {
            swal({ title: "Login", text: data.Message, type: "warning" });
            isLoading(false)
        }
    }, function (data) {
            swal({ title: "Login", text: data.Message, type: "warning" });
            isLoading(false)
    }); 
};

model.action.requestForgotPassword = function () {
    var data = ko.mapping.toJS(model.data.resetPasswordRequest);
    var a = "";
    if (!(data.Email || "").trim()) {
        swal({ title: "Forgot Password", text: 'Email is required.', type: "warning" });
        return;
    } 

    isLoading(true)
    ajaxPost("/Site/Auth/RequestResetPassword", data, function (data) {        
        if (data.StatusCode == 200) {
            swal({ title: "Forgot Password", text: data.Message, type: "success" });
            setTimeout(function () {                
                location.href = "/Site/Auth/";
            }, 1000);
        } else {
            swal({ title: "Forgot Password", text: data.Message, type: "warning" });
            isLoading(false);
        }
    }, function (data) {
            swal({ title: "Forgot Password", text: data.Message, type: "error" });
            isLoading(false);
    });

};

model.action.doReset = function () {
    var data = ko.mapping.toJS(model.data.resetPassword);
    if (!(data.Password || "").trim()) {
        swal({ title: "Forgot Password", text: 'New Password is required.', type: "warning" });
        return;
    }

    if ((data.Password  || "")!= (data.RePassword || "")) {
        swal({ title: "Forgot Password", text: 'Password mot match.', type: "warning" });
        return;
    }

    if (!(data.Token || "").trim()) {
        swal({ title: "Forgot Password", text: 'Request is invalid.', type: "warning" });
        return;
    }

    isLoading(true);
    ajaxPost("/Site/Auth/DoReset", data, function (data) {
        if (data.StatusCode == 200) {
            swal({ title: "Forgot Password", text: data.Message, type: "success" });            
            setTimeout(function () {                
                location.href = "/Site/Auth/";
            }, 1000);
        } else {
            swal({ title: "Forgot Password", text: data.Message, type: "warning" });
            isLoading(false);
        }
    }, function (data) {
            swal({ title: "Forgot Password", text: data.Message, type: "error" });
            isLoading(false);
    });

};

// render function
model.render = {};

model.render.displayPassword = function () {
    $('.show-pass').click(function () {
        if ($(this).find("i").hasClass("fa-eye")) {
            $(this).find("i").addClass("fa-eye-slash")
            $(this).find("i").removeClass("fa-eye")
            $("#" + $(this).data("id")).attr("type", "text");
        } else {
            $(this).find("i").addClass("fa-eye")
            $(this).find("i").removeClass("fa-eye-slash")
            $("#" + $(this).data("id")).attr("type", "password");
        }
    });
};

model.action.TestSend = function () {
    var data = ko.mapping.toJS(model.data.resetPasswordRequest);
    var a = "";
    if (!(data.Email || "").trim()) {
        swal({ title: "Forgot Password", text: 'Email is required.', type: "warning" });
        return;
    }
    isLoading(true)
    ajaxPost("/Site/Auth/TestSendEmail", data, function (data) {
        if (data.StatusCode == 200) {
            swal({ title: "Send Mail", text: data.Message, type: "success" });
            setTimeout(function () {
                //location.href = "/Site/Auth/";
            }, 1000);
        } else {
            swalFatal("Error send email",data.Message);
            
        }
        isLoading(false);
    });

};

model.action.showPassword = function (v, e) {
    var $el = $(e.currentTarget);
    var id = $el.data("id");
    var displayed = $el.hasClass("password-displayed");

    if (!id) {
        console.error("unable to find data-id on :\n" + e);
        return;
    }

    var $id = $(`#${id}`);
    if ($id.length == 0) {
        console.error("unable to find DOM with id : " + id);
        return;
    }
    
    if (!!displayed) {        
        $id.attr("type", "password");
        $el.removeClass("password-displayed");
    } else {
        $id.attr("type", "text");
        $el.addClass("password-displayed");
    }
};

var container = $("#formUserAuth");
kendo.init(container);
container.kendoValidator({
    rules: {
        rule1: function (input) {
            if (input.is("[name=Password]")) {
                if (input.val().length < 6) {
                    return false;
                }
                return true;
            }
            return true;
        },
        rule2: function (input) {
            if (input.is("[name=RePassword]")) {
                if (input.val().length < 6) {
                    return false;
                }
                return true;
            }
            return true;
        },
        rule3: function (input) {
            if (input.is("[name=RePassword]")) {
                if (model.data.activation().Password() != model.data.activation().RePassword()) {
                    return false;
                }
                return true;
            }
            return true;
        }
    },
    messages: {
        rule1: "Password at least 6 character.",
        rule2: "Password at least 6 character.",
        rule3: "Password mot match."
    }
});