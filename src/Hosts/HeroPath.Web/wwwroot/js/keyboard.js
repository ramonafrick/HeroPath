const arrows = new Set(["ArrowUp", "ArrowDown", "ArrowLeft", "ArrowRight"]);
const typing = "input, textarea, select, [contenteditable], [role=dialog], dialog";
const owners = "button, a[href], summary";

let listener = null;

export function start(callback) {
    stop();
    listener = event => {
        if (!isShortcut(event)) {
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

function isShortcut(event) {
    if (event.repeat || event.isComposing || event.ctrlKey || event.altKey || event.metaKey || !isHandled(event)) {
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

// Keep in sync with ShortcutKeys.Resolve (HeroPath.Modules.Game).
function isHandled(event) {
    const key = event.key;
    if (arrows.has(key)) {
        return !event.shiftKey || key === "ArrowUp" || key === "ArrowDown";
    }

    return key === "Delete" || key === "Backspace" || key === "Escape" || key === " "
        || key === "d" || key === "D" || key === "r" || key === "R";
}
