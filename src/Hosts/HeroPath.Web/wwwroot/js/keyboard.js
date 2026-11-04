const typing = "input, textarea, select, [contenteditable], [role=dialog], dialog";
const owners = "button, a[href], summary";

let listener = null;

// keys and shifts are parallel arrays from the app (ShortcutKeys.Handled): every key and Shift state that has an action.
export function start(callback, keys, shifts) {
    stop();
    const handled = new Set(keys.map((key, index) => chordId(key, shifts[index])));
    listener = event => {
        if (!isShortcut(event, handled)) {
            return;
        }

        event.preventDefault();
        callback.invokeMethodAsync("OnKey", event.key, event.shiftKey).catch(() => { });
    };
    document.addEventListener("keydown", listener);
}

export function stop() {
    if (listener) {
        document.removeEventListener("keydown", listener);
        listener = null;
    }
}

function chordId(key, shift) {
    return (shift ? "S+" : "-+") + key;
}

function isShortcut(event, handled) {
    if (event.repeat || event.isComposing || event.ctrlKey || event.altKey || event.metaKey || !handled.has(chordId(event.key, event.shiftKey))) {
        return false;
    }

    const elements = [event.target, document.activeElement];
    if (elements.some(element => element instanceof Element && element.closest(typing))) {
        return false;
    }

    if (event.key === " " && elements.some(element => element instanceof Element && element.closest(owners))) {
        return false;
    }

    return true;
}
