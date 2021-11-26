model.newLocation = function (data) {
    if (data) {
        return ko.mapping.fromJS(Object.assign(_.clone(this.proto.Location), _.clone(data)));
    }
    return ko.mapping.fromJS(this.proto.Location);
    
};

//model.newLocation = function (data) {
//    let canteen = _.clone(this.proto.Location);

//    if (!canteen) return;

//    if (data) {
//        canteen = Object.assign(canteen, _.clone(data));
//    }
//    var result = ko.mapping.fromJS(canteen);
//    return result;
//};

model.data.location = ko.observable(model.newLocation());
model.data.unit = ko.observable("m");
model.map.visible = ko.observable(false);

model.init.location = function () {
    let param = {}
    displayLoading("#gridLocation", true);

    ajaxPost("/ESS/MobileAttendance/GetLocation", param, function (res) {
        //let result = JSON.parse(`${res.Data}`);
        let result = res;
        //console.log(result)
        displayLoading("#gridLocation", false);
        $("#gridLocation").kendoGrid({
            columns: [
                {
                    headerAttributes: {
                        class: "text-center",
                    },
                    field: "Code",
                    title: "Code",
                    width: 100
                },
                {
                    headerAttributes: {
                        class: "text-center",
                    },
                    field: "Name",
                    title: "Name",
                    width: 220
                },
                {
                    headerAttributes: {
                        class: "text-center",
                    },
                    field: "Address",
                    title: "Address",
                    width: 260
                },
                {
                    headerAttributes: {
                        class: "text-center",
                    },
                    field: "",
                    title: "Coordinate",
                    template: function (d) {
                        let lat = "Lat: " + d.Latitude;
                        let lng = "Lng:" + d.Longitude;
                        return lat + "<br />" + lng;
                    },
                    width: 200
                },
                {
                    headerAttributes: {
                        class: "text-center",
                    },
                    attributes: {
                        class: "text-center",
                    },
                    field: "Radius",
                    title: "Radius (m)",
                    width: 120
                },
                {
                    headerAttributes: {
                        class: "text-center",
                    },
                    attributes: {
                        class: "text-center",
                    },
                    width: 90,
                    field: "IsVirtual",
                    title: "Virtual<br />Office",
                    template: function (d) {
                        return (d.IsVirtual) ? "Yes" : "No"
                    }
                },
                {
                    headerAttributes: {
                        class: "text-center",
                    },
                    attributes: {
                        class: "text-center",
                    },
                    field: "",
                    title: "Action",
                    template: function (d) {
                        var btnEdit = `
                                <button type="button" class="btn btn-xs btn-outline-info mr-2" onclick="model.action.editLocation('${d.uid}')">
                                        <i class="fa mdi mdi-pencil"></i>
                                </button>`;

                        //var btnDelete = `
                        //        <button type="button" class="btn btn-xs btn-outline-danger" onclick="model.action.deleteCanteen('${d.uid}')">
                        //                <i class="fa mdi mdi-delete"></i>
                        //        </button>`;

                        return btnEdit;
                    },
                    width: 100,
                },
            ],
            dataSource: {
                data: result
            }
        })
    });
};

model.action.editLocation = function (uid) {
    var grid = $("#gridLocation").data("kendoGrid");
    var data = grid.dataSource.getByUid(uid);
    
    data.Tags = data.Tags[0];
    console.log(data)
    $("#locationModal").modal("show");
    model.data.location(model.newLocation(data));
    //$("#locationModal").on('shown.bs.modal', function () {
        
    //});
}

model.action.showModal = function () {
    model.data.location(model.newLocation());
    $("#locationModal").modal('show');
}

model.action.showMap = function () {
    //$("#mapModal").modal('show');
    model.map.visible(!model.map.visible());
}

model.init.map = function () {
    mapboxgl.accessToken = 'pk.eyJ1IjoiYXlpZXh6MjIiLCJhIjoiY2s4cW5ndzZsMDVybTNucTg5dGl6M2dlcCJ9.X3Z5LULu6famrzySGByLoQ';;
    var coordinates = document.getElementById('coordinates');
    var map = new mapboxgl.Map({
        container: 'map',
        style: 'mapbox://styles/mapbox/streets-v11',
        center: [112.7334468, -7.2877575],
        zoom: 13
    });

    var marker = new mapboxgl.Marker({
        draggable: true
    })
        .setLngLat([112.7334468, -7.2877575])
        .addTo(map);

    function onDragEnd() {
        var lngLat = marker.getLngLat();
        coordinates.style.display = 'block';
        coordinates.innerHTML =
            'Longitude: ' + lngLat.lng + '<br />Latitude: ' + lngLat.lat;
        model.data.location().Latitude(lngLat.lat);
        model.data.location().Longitude(lngLat.lng);
    }

    marker.on('dragend', onDragEnd);

    map.addControl(
        new MapboxGeocoder({
            accessToken: mapboxgl.accessToken,
            mapboxgl: mapboxgl
        })
    );

    var scale = new mapboxgl.ScaleControl({
        maxWidth: 80,
        unit: 'metric'
    });
    map.addControl(scale);

    //scale.setUnit('metric');

    map.addControl(new mapboxgl.NavigationControl(), 'top-left');
    map.addControl(new mapboxgl.FullscreenControl(), 'top-left');

}

model.action.getLocation = function() {
    if (navigator.geolocation) {
        navigator.geolocation.getCurrentPosition(showPosition);
    } else {
        swalError("Error", "Geolocation is not supported by this browser.")
    }
}

function showPosition(position) {
    model.data.location().Latitude(position.coords.latitude);
    model.data.location().Longitude(position.coords.longitude)
}

model.action.save = async function () {
    let data = ko.mapping.toJS(model.data.location());
    let name = `${data.Name}`;
    let param = {}
    console.log("data:", data)
    let confirmResult = await swalConfirm("User", `Are you sure saving Location ${name} ?`);

    param.Id = data.Id;
    param.Code = data.Code;
    param.Name = data.Name;
    param.Address = data.Address;
    param.Latitude = data.Latitude;
    param.Longitude = data.Longitude;
    param.Radius = data.Radius;
    param.IsVirtual = data.IsVirtual;
    param.Tags = [data.Tags];

    let unit = $("#Unit").data("kendoDropDownList");
    if (unit.value() == "km") {
        param.Radius = param.Radius * 1000;
    }
    //console.log("param:", unit.value(), param)

    //return false;
    if (confirmResult.value) {
        ajaxPost("/ESS/MobileAttendance/SaveLocation", param, function (res) {
            //console.log(res)
            if (res.StatusCode == 200) {
                swalSuccess("Success", res.Message);
                model.init.location();
                $("#locationModal").modal('hide');
            }
        }, function (err) {
            swalError("Error", err)
        })
    }
}
