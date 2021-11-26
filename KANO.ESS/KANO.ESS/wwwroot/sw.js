function titleCase(str) {
    str = str + "";
    return str.split(" ").map(x => {
        return x.charAt(0).toUpperCase() + x.slice(1);
    }).join(" ");
}

self.addEventListener('push', function (e) {
    console.log("Push event!! ", e);

    if (e.data) {
        var jsonString = e.data.text();
        console.log(jsonString)
        if (!!jsonString) {
            var data = JSON.parse(jsonString);
            var options = {
                body: data.Message,
                icon: '/assets/img/tps-notification.png',
                vibrate: [100, 50, 100],
                data: {
                    dateOfArrival: Date.now(),
                    primaryKey: '2'
                },
                actions: []
            };

            e.waitUntil(self.registration.showNotification(titleCase(data.Module || ""), options));
        }
    } else {
        body = 'Push message no payload';
        console.log("NotificationWorker : ", body);
    }
});
self.addEventListener('install', function (event) {
    self.skipWaiting();
    console.log("Skip Waiting!! ");
});