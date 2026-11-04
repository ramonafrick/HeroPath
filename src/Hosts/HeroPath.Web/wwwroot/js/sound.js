const volume = 0.08;
const resumeTimeoutMs = 250;

const effects = {
    success: { wave: "triangle", length: 0.2, notes: [[523.25, 0], [659.25, 0.11], [783.99, 0.22]] },
    error: { wave: "sine", length: 0.26, notes: [[220, 0], [164.81, 0.17]] },
    item: { wave: "sine", length: 0.11, notes: [[1174.66, 0]] },
};

let context = null;

export async function play(name) {
    try {
        const effect = effects[name];
        const AudioContextType = window.AudioContext || window.webkitAudioContext;
        if (!effect || !AudioContextType) {
            return;
        }

        context ??= new AudioContextType();
        if (context.state !== "running") {
            await Promise.race([
                context.resume().catch(() => { }),
                new Promise(resolve => setTimeout(resolve, resumeTimeoutMs)),
            ]);
        }

        if (context.state !== "running") {
            return;
        }

        const start = context.currentTime + 0.02;
        for (const [frequency, offset] of effect.notes) {
            tone(frequency, start + offset, effect.length, effect.wave);
        }
    } catch {
        // Audio is a bonus: blocked or unavailable audio stays silent.
    }
}

function tone(frequency, time, length, wave) {
    const oscillator = context.createOscillator();
    const gain = context.createGain();
    oscillator.type = wave;
    oscillator.frequency.setValueAtTime(frequency, time);
    gain.gain.setValueAtTime(0.0001, time);
    gain.gain.exponentialRampToValueAtTime(volume, time + 0.015);
    gain.gain.exponentialRampToValueAtTime(0.0001, time + length);
    oscillator.connect(gain);
    gain.connect(context.destination);
    oscillator.start(time);
    oscillator.stop(time + length + 0.02);
}
