let blazorRef;

export function initialize(ref) {
    blazorRef = ref;
}

export function confirmCharge(amount) {
    return confirm(`Charge customer $${amount.toFixed(2)}?`);
}

export function showNotification(title, body) {
    if (Notification.permission === "granted") {
        new Notification(title, { body: body });
        return true;
    }
    return false;
}

export function requestNotificationPermission() {
    return Notification.requestPermission();
}

export function getTerminalInfo() {
    return {
        userAgent: navigator.userAgent,
        language: navigator.language,
        screenWidth: window.screen.width,
        screenHeight: window.screen.height,
        url: window.location.href,
        online: navigator.onLine
    };
}

export function addOnlineStatusListener() {
    window.addEventListener("online", () => {
        if (blazorRef) {
            blazorRef.invokeMethodAsync("OnConnectionStatusChanged", true);
        }
    });
    window.addEventListener("offline", () => {
        if (blazorRef) {
            blazorRef.invokeMethodAsync("OnConnectionStatusChanged", false);
        }
    });
}

export function saveCashierName(name) {
    localStorage.setItem("bikepos_cashier", name);
}

export function loadCashierName() {
    return localStorage.getItem("bikepos_cashier");
}
